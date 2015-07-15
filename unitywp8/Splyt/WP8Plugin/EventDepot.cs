using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Runtime.Serialization;
using Splyt.External.MiniJSON;
using Polenter.Serialization;
using Polenter.Serialization.Core;
using Nito.AsyncEx;
using Microsoft.Phone.Shell;

namespace Splyt
{
    internal static class EventDepot
    {
        private static string sAbsoluteUri;
        private static int sReqTimeout;
        private static HttpRequest.Listener sRequestListener;
        private static AsyncProducerConsumerQueue<Runnable> sJobQueue;
        private static bool sPaused;
        private static bool sInitialized;
        private static bool sSendBinEnabled;

        // Used to calculate the period at which we process the bins
        private const int PROCESSBIN_MIN_PERIOD = 5000;    // In ms
        private const int PROCESSBIN_MAX_PERIOD = 30000;   // In ms
        private static int sCurProcessBinPeriod = PROCESSBIN_MIN_PERIOD;

        // A constant representing the maximum number of events we allow in a single bin
        // This constraint is here to limit our memory footprint
        // Since we have 2 bins of events in memory at any given time, the maximum number of events resident in memory is 2x this number
        private const int MAX_EVENTS_PER_BIN = 50;

        private const string STATE_FILENAME = "splyt_depotState";

        private const string BIN_ARCHIVE_FILE_PREFIX = "splyt_binArchive";
        private const int BIN_ARCHIVES_SIZE = 201; // 200 archived bins -> a maximum of 10k events

        class Bin
        {
            public string AbsoluteUri { get; set; }
            public List<object> Events { get; set; }

            public Bin()
            {
                Events = new List<object>();
            }

            public Bin(string absoluteUri) : this()
            {
                AbsoluteUri = absoluteUri;
            }
 
            public void ClearEvents()
            {
                if (null != Events) Events.Clear();
            }
        }

        private static class State
        {
            // The depot state has the following properties:
            // ResendBin:     A bin of events that we would like to re-send (previous sends failed to reach the data collector).
            //                Under normal operating conditions this bin is empty.
            // HoldingBin:    A bin of events that are being held and waiting to either be sent immediately to the data collector
            //                or archived to "disk" and sent at some later point in time
            // ArchiveStart:  An index representing the start of the "circular buffer" of bins of events that have been archived to "disk"
            // ArchiveEnd:    An index representing the end of the "circular buffer" of bins of events that have been archived to "disk"
            class Data
            {
                public Bin ResendBin { get; set; }
                public Bin HoldingBin { get; set; }

                public int ArchiveStart { get; set; }
                public int ArchiveEnd { get; set; }

                public Data()
                {
                    ResendBin = new Bin(sAbsoluteUri);
                    HoldingBin = new Bin(sAbsoluteUri);
                }
            }
            private static Data sData = null;

            internal static Bin HoldingBin() { return sData.HoldingBin;  }
            internal static Bin ResendBin() { return sData.ResendBin; }

            internal static int ArchiveStart() { return sData.ArchiveStart; }
            internal static void setArchiveStart(int newValue) { sData.ArchiveStart = newValue; }
            internal static int ArchiveEnd() { return sData.ArchiveEnd; }
            internal static void setArchiveEnd(int newValue) { sData.ArchiveEnd = newValue; }

            internal static string HoldingBinAbsoluteUri() { return sData.HoldingBin.AbsoluteUri; }
            internal static void setHoldingBinAbsoluteUri(string absoluteUri) { sData.HoldingBin.AbsoluteUri = absoluteUri; }

            private static void reset()
            {
                sData = new Data();
            }

            internal static void restore()
            {
                sData = LocalStorage.Load<Data>(STATE_FILENAME, true);

                if (null == sData)
                {
                    // No state data available, so create some
                    reset();
                }

                // If there is supposed to be no data archived to disk, make sure there is none (i.e., clean up).
                // We do this in case the state somehow got out of sync with what had been written to internal storage
                // If this happens, then all of the events in these archived files are lost, but it would be sent out of order now anyhow and just
                if (sData.ArchiveEnd == sData.ArchiveStart)
                {
                    try
                    {
                        using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            string[] staleArchives = storage.GetFileNames(BIN_ARCHIVE_FILE_PREFIX + "*");

                            if (null != staleArchives)
                            {
                                // Delete any archive files found
                                foreach (string archive in staleArchives)
                                {
                                    storage.DeleteFile(archive);
                                }
                            }
                        }
                    }
                    catch (Exception) {}
                }
            }

            internal static void save()
            {
                LocalStorage.Save(STATE_FILENAME, sData);

                // State data is saved, so clear the in-memory data
                reset();
            }
        }

        private interface Runnable
        {
            void run();
        }

        /**
         * Initialize the event depot.
         *
         * @param host          The host name of the data collector
         * @param queryParams   Query parameters to send along with the request
         * @param reqTimeout    A timeout, in milliseconds, representing the maxmimum amount of time one should wait for Splyt network requests to complete.
         */
   
        internal static void init(string host, string queryParams, int reqTimeout)
        {
            if (!sInitialized)
            {
                // Save off the parameters needed to submit requests to send the events to the data collector
                sReqTimeout = reqTimeout;
                sAbsoluteUri = host + "/isos-personalization/ws/interface/datacollector_batch" + queryParams;
                sRequestListener = new SendEventRequestListener();

                // Create the job queue and start up the job consumer in another thread
                sJobQueue = new AsyncProducerConsumerQueue<Runnable>();

                // Start a background thread to execute the request
                Thread t = new Thread(consumeJobs); 
                t.IsBackground = true; 
                t.Start();

                // Queue up the DepotInitJob
                sJobQueue.Enqueue(new DepotInitJob());

                // Hook into the Application events raised when the user navigates away from and to the application
                PhoneApplicationService app = PhoneApplicationService.Current;
                app.Deactivated += pause;
                app.Activated += resume;

                sSendBinEnabled = true;
                sInitialized = true;
            }
        }


        /**
         * Store an event in the depot.
         *
         * @param eventData The event we wish to store
         */
        internal static Error store(IDictionary<string, object> eventData)
        {
            Error ret = Error.Success;

            if (null != sJobQueue)
            {
                // Add the job to the queue
                sJobQueue.Enqueue(new StoreEventJob(eventData));
            }
            else
            {
                ret = Error.NotInitialized;
            }

            return ret;
        }

        internal static void pause(object sender, DeactivatedEventArgs e)
        {
            // Disable any more sending of bin data as we need any jobs in the queue that might perform this to get flushed ASAP to make way for the pause job to run.
            sSendBinEnabled = false;

            if (null != sJobQueue)
            {
                // Add the job to the queue
                sJobQueue.Enqueue(new PauseDepotJob());
            }

            // Give the job queue time to flush which in most cases will allow the pause job to complete.
            // Unfortunately we cannot guarantee anything with regards to the pause job actually completing, but we give it a chance.
            // If it doesn't complete in time, we risk losing some data, but so be it.
            // See http://msdn.microsoft.com/en-us/library/windowsphone/develop/microsoft.phone.shell.phoneapplicationservice.deactivated%28v=vs.105%29.aspx for why we use 2000 ms
            Thread.Sleep(2000);
        }

        internal static void resume(object sender, ActivatedEventArgs e)
        {
            if (null != sJobQueue)
            {
                // Add the job to the queue
                sJobQueue.Enqueue(new ResumeDepotJob());
            }

            // Re-enabled sending of bin data
            sSendBinEnabled = true;
        }

        #region private helper classes
        private class SendEventRequestListener : HttpRequest.Listener
        {
            void HttpRequest.Listener.onComplete(HttpRequest.Result result)
            {
                // At least log the error response
                logErrorResponse(result.Response);
            }
        }
        #endregion

        #region private helper functions
        private static void logErrorResponse(string responseStr)
        {
            if (null != responseStr)
            {
                try
                {
                    Dictionary<string, object> response = Json.Deserialize(responseStr) as Dictionary<string, object>;
                    if (response.ContainsKey("error"))
                    {
                        Error err = (Error)Enum.ToObject(typeof(Error), response["error"]);
                        if (Error.Success != err)
                        {
                            Util.logError("Top-level error [" + err.ToString() + "] returned from data collector");
                        }
                        else
                        {
                            // Got a successful top-level response, now check the datacollector_batch context
                            if (response.ContainsKey("data"))
                            {
                                Dictionary<string, object> data = response["data"] as Dictionary<string, object>;

                                if (data.ContainsKey("datacollector_batch"))
                                {
                                    Dictionary<string, object> context = data["datacollector_batch"] as Dictionary<string, object>;
                                    if (context.ContainsKey("error"))
                                    {
                                        Error contextErr = (Error)Enum.ToObject(typeof(Error), context["error"]);
                                        if (Error.Success != contextErr)
                                        {
                                            Util.logError("datacollector_batch error [" + contextErr.ToString() + "] returned from data collector");
                                        }
                                    }
                                    else
                                    {
                                        Util.logError("Unexpected response returned from data collector, context error missing");
                                    }
                                }
                                else
                                {
                                    Util.logError("Unexpected response returned from data collector, context missing");
                                }
                            }
                            else
                            {
                                Util.logError("Unexpected response returned from data collector, data missing");
                            }
                        }
                    }
                    else
                    {
                        Util.logError("Unexpected response returned from data collector, error missing");
                    }
                }
                catch (Exception)
                {
                    Util.logError("Exception parsing server response: " + responseStr);
                }
            }
        }

        // Main job consumer function that's run in the background thread
        private static void consumeJobs()
        {
            while (true)
            {
                AsyncProducerConsumerQueue<Runnable>.DequeueResult nextJob = sJobQueue.TryDequeue();
                if (nextJob.Success)
                {
                    nextJob.Item.run();
                }
            }
        }

        private static bool sendBin(Bin binData)
        {
            bool binSent = false;

            if (sSendBinEnabled)
            {
                // Build up the data object
                List<object> allArgs = new List<object>(2);
                allArgs.Add(Util.Timestamp());
                allArgs.Add(binData.Events);

                // Create a new request to send the data synchronously
                HttpRequest.Result result = HttpRequest.executeSync(new Uri(binData.AbsoluteUri), sReqTimeout, Json.Serialize(allArgs));

                if (Error.Success == result.ErrorCode)
                {
                    // We got "some" response from the server
                    // Note that we won't attempt to re-send this bin of events regardless of the response

                    // If we were throttling the sends because of a connectivity issue, let's throttle back up now that we're getting responses
                    if (PROCESSBIN_MIN_PERIOD != sCurProcessBinPeriod)
                    {
                        int decrementBy = Math.Max((sCurProcessBinPeriod - PROCESSBIN_MIN_PERIOD) / 5, 500);
                        sCurProcessBinPeriod = Math.Max(sCurProcessBinPeriod - decrementBy, PROCESSBIN_MIN_PERIOD);
                    }

                    // Now, let's check for errors in the data returned from the server so that we can at least log them
                    logErrorResponse(result.Response);
                }
                else
                {
                    // Some IO or timeout error occurred, so we might have some connectivity issue
                    // Let's throttle the timer in order to attempt sending bins of events less often in case the user is not connected
                    if (PROCESSBIN_MAX_PERIOD != sCurProcessBinPeriod)
                    {
                        sCurProcessBinPeriod = Math.Min(sCurProcessBinPeriod + 500, PROCESSBIN_MAX_PERIOD);
                    }
                }

                binSent = (Error.Success == result.ErrorCode);
            }

            return binSent;
        }

        private static void processBins(bool flushHoldingBin)
        {
            // First, let's try and send a bin of events to the data collector
            Bin rb = State.ResendBin();
            Bin hb = State.HoldingBin();
            Util.logDebug("Resend Bin Count [" + rb.Events.Count + "]");
            Util.logDebug("Holding Bin Count [" + hb.Events.Count + "]");
            Util.logDebug("Archive Infos [" + State.ArchiveStart() + ", " + State.ArchiveEnd() + "]");
            if (rb.Events.Count > 0)
            {
                // We have events in the re-send bin.  These are our first priority, so let's try and send them
                if (sendBin(rb))
                {
                    // Successful send, clear the events from the bin
                    rb.ClearEvents();
                }
            }
            else if (State.ArchiveEnd() != State.ArchiveStart())
            {
                // Nothing in the re-send bin, but we have some data archived to disk.  These are our second priority as we must send events in timestamp order
                String archiveFileName = BIN_ARCHIVE_FILE_PREFIX + State.ArchiveStart().ToString();

                try
                {
                    using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        try
                        {
                            using (IsolatedStorageFileStream fs = storage.OpenFile(archiveFileName, System.IO.FileMode.Open))
                            {
                                if (fs != null)
                                {
                                    Bin diskData = (Bin)new SharpSerializer(true).Deserialize(fs);

                                    if (!sendBin(diskData))
                                    {
                                        // Failed to send the bin of events.  Dump them into the re-send bin so we can try again next time
                                        rb.Events.AddRange(diskData.Events);
                                        rb.AbsoluteUri = diskData.AbsoluteUri;
                                    }
                                }
                            }
                        }
                        catch (DeserializingException de)
                        {
                            // Problem deserializing.  This typically happens if one of the objects serialized does not have a public, standard (parameterless) constructor 
                            Util.logError(de);
                        }
                        catch (Exception)
                        {
                            // Some other error occurred reading the state data file. We can handle this situation, so carry on
                        }

                        // Remove the archive file and update the start index
                        storage.DeleteFile(archiveFileName);
                        State.setArchiveStart((State.ArchiveStart() + 1) % BIN_ARCHIVES_SIZE);
                    }
                }
                catch (IsolatedStorageException)
                {
                    // Some error occurred operatin on the archive data file.  This is unexpected, so we log it
                    // But it's safe to carry on
                    Util.logError("IsolatedStorageException loading file [" + archiveFileName + "].  Skipping...");
                }
            }
            else if (hb.Events.Count > 0)
            {
                // Noting in the re-send bin and we have no data archived to disk, so let's attempt to send what's in the holding bin
                if (!sendBin(hb))
                {
                    // Failed to send the bin of events.  Dump them into the re-send bin so we can try again next time
                    rb.Events.AddRange(hb.Events);
                    rb.AbsoluteUri = hb.AbsoluteUri;
                }

                // Clear the events from the holding bin
                hb.ClearEvents();
            }

            // Handle bin overflow
            while ((hb.Events.Count >= MAX_EVENTS_PER_BIN) || (flushHoldingBin && hb.Events.Count > 0) )
            {
                // Our holding bin is full, so rotate out a chunk of events to disk
                try
                {
                    using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        String archiveFileName = BIN_ARCHIVE_FILE_PREFIX + State.ArchiveEnd().ToString();

                        State.setArchiveEnd((State.ArchiveEnd() + 1) % BIN_ARCHIVES_SIZE);

                        if (State.ArchiveEnd() == State.ArchiveStart())
                        {
                            // We've reached the max archives we wish to store, so purge the oldest one
                            storage.DeleteFile(BIN_ARCHIVE_FILE_PREFIX + State.ArchiveStart().ToString());

                            State.setArchiveStart((State.ArchiveStart() + 1) % BIN_ARCHIVES_SIZE);
                        }

                        using (IsolatedStorageFileStream fs = storage.OpenFile(archiveFileName, System.IO.FileMode.Create))
                        {
                            if (fs != null)
                            {
                                Bin binToArchive = new Bin(State.HoldingBinAbsoluteUri());
                                binToArchive.Events.AddRange(hb.Events.GetRange(0, MAX_EVENTS_PER_BIN));
                                new SharpSerializer(true).Serialize(binToArchive, fs);

                                // Now that we've archived the data, clear it from the holding bin
                                hb.Events.RemoveRange(0, MAX_EVENTS_PER_BIN);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // Delete, save, or serialization failed, log it and move along
                    Util.logError(e);
                }
            }
        }

        private static void StartBinProcessor()
        {
            // NOTE:  The nice thing about using the DispatcherTimer is that pause and resume happen for "free"
            // That is, the timer itself pauses when the app goes into the background and resumes when it resumes.

            // Create the timer and assign the event handler that will be triggered at regular intervals
            DispatcherTimer dt = new DispatcherTimer();

            dt.Tick += delegate(object sender, EventArgs e)
            {
                // Add the job to the queue to process the bins
                sJobQueue.Enqueue(new ProcessBinsJob());

                // Update the timer interval, in case it changed
                ((DispatcherTimer)sender).Interval = TimeSpan.FromMilliseconds(sCurProcessBinPeriod);
            };
 
            // Start up a timer to process bins after a specified amount of time
            dt.Interval = TimeSpan.FromMilliseconds(sCurProcessBinPeriod);
            dt.Start();
        }
        #endregion

        #region job implementations
        private class DepotInitJob : Runnable
        {
            public void run()
            {
                // Restore any state data that may have been saved off
                State.restore();

                // initially, if the Uri has changed, we need to flush the old stuff out of the holding bin
                bool flushHoldingBin = sAbsoluteUri != State.HoldingBinAbsoluteUri();
                processBins(flushHoldingBin);
                State.setHoldingBinAbsoluteUri(sAbsoluteUri);
            
                // Invoke a method on the main thread that will start the periodic bin processing
                Deployment.Current.Dispatcher.BeginInvoke(StartBinProcessor);
            }
        }

        private class StoreEventJob : Runnable
        {
            object mEventData; // The event data to store

            public StoreEventJob(object eventData)
            {
                mEventData = eventData;
            }

            public void run()
            {
                // We have an event to store
                if (sPaused)
                {
                    // The system has been paused, so process this event on demand

                    // Restore any state data that may have been saved off
                    State.restore();

                    // Add the event to the holding bin
                    State.HoldingBin().Events.Add(mEventData);

                    // Process bins immediately
                    processBins(false);

                    // Save off the state
                    State.save();
                }
                else
                {
                    Bin hb = State.HoldingBin();
                    hb.Events.Add(mEventData);
                    if (hb.Events.Count >= MAX_EVENTS_PER_BIN)
                    {
                        // We've reached the maximum desired batch size, so process the bins immediately
                        processBins(false);
                    }
                }
            }
        }


        private class ProcessBinsJob : Runnable
        {
            public void run()
            {
                // Process the bins
                processBins(false);
            }
        }

        private class PauseDepotJob : Runnable
        {
            public void run()
            {
                if (!sPaused)
                {
                    // Save off the state
                    State.save();

                    sPaused = true;
                }
            }
        }

        private class ResumeDepotJob : Runnable
        {
            public void run()
            {
                if (sPaused)
                {
                    // Restore the state
                    State.restore();

                    // Reset the period for the bin processing
                    sCurProcessBinPeriod = PROCESSBIN_MIN_PERIOD;

                    sPaused = false;
                }
            }
        }
        #endregion
    }
}
