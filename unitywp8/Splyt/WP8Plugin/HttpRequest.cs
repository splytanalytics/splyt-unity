using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace Splyt
{
    internal class HttpRequest
    {
        // A URI that identifies the Internet resource (i.e., the URL)
        private Uri mUri;

        // Timeout, in milliseconds, for the request
        private int mTimeout;

        // Data to send (optional)
        private string mSendData;

        private ManualResetEvent mAllDone; 

        internal HttpRequest(Uri uri, int requestTimeout, string sendData)
        {
            // Set all of the member variables
            mUri = uri;
            mTimeout = requestTimeout;
            mSendData = sendData;
            mAllDone = new ManualResetEvent(false);
        }

        private Result execute()
        {
            State state = new State(mUri);

            try
            {
                // Set the event to nonsignaled state. 
                mAllDone.Reset();

                // Start the asynchronous request for a stream object to use to write data
                IAsyncResult result = (IAsyncResult)state.WebRequest.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallback), state);

                bool signalReceived = mAllDone.WaitOne(mTimeout);

                if (!signalReceived)
                {
                    // mAllDone wasn't signaled by GetResponseCallback; the request timed out
                    // Note that we don't Abort the request at this point because the Abort() call can get stuck waiting if the user pauses the app at the right (or wrong) time.
                    // And if this happens, the EventDepot queue can get stuck waiting for this request to finish.
                    // So, to avoid that situation we just return the error and let the request finish "normally" and ignore any response
                    // The request will eventually get resent anyway, so this is ok.
                    state.Result.ErrorCode = Splyt.Error.RequestTimedOut;
                }
            }
            catch (WebException e)
            {
                state.Result.ErrorCode = Splyt.Error.InvalidArgs;
                state.Result.Response = "WebException: " + e.Message;
            }
            catch (Exception e)
            {
                state.Result.ErrorCode = Splyt.Error.Generic;
                state.Result.Response = "Exception: " + e.Message;
            }

            return state.Result;
        }

        /// <summary> 
        /// AsyncCallback delegate that is invoked when the request for the stream object is complete  
        /// </summary> 
        /// <param name="asynchronousResult"></param> 
        private void GetRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                State state = (State)asynchronousResult.AsyncState;

                // End the operation
                Stream postStream = state.WebRequest.EndGetRequestStream(asynchronousResult);

                // Convert the string into a byte array. 
                byte[] byteArray = Encoding.UTF8.GetBytes(mSendData);

                // Write to the request stream.
                postStream.Write(byteArray, 0, mSendData.Length);
                postStream.Close();

                // Start the asynchronous request for a response from the server
                state.WebRequest.BeginGetResponse(new AsyncCallback(GetResponseCallback), state);
            }
            catch (Exception e)
            {
                // Some exception occurred, just catch it, log it, and let the triggering thread just assume the request timed out
                Util.logError(e);
            }
        }

        private void GetResponseCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                State state = (State)asynchronousResult.AsyncState;

                HttpWebRequest req = state.WebRequest as HttpWebRequest;
                if ((null != req) && req.HaveResponse)
                {
                    // End the operation and set the response from the server
                    HttpWebResponse response = (HttpWebResponse)req.EndGetResponse(asynchronousResult);
                    Stream streamResponse = response.GetResponseStream();
                    StreamReader streamRead = new StreamReader(streamResponse);
                    state.Result.Response = streamRead.ReadToEnd();

                    // Close the stream object
                    streamResponse.Close();
                    streamRead.Close();

                    // Release the HttpWebResponse
                    response.Close();

                    // Signal the main thread to continue. 
                    mAllDone.Set(); 
                }
            }
            catch (Exception e)
            {
                // Some exception occurred, just catch it, log it, and let the triggering thread just assume the request timed out
                Util.logError(e);
            }
        }

        #region "public" methods
        internal static void init(string hubObjName, bool isWebPlayer)
        {
            // unused in this implementation
        }

        // For the timeout implementation see http://code.msdn.microsoft.com/wpapps/Timeout-for-httpwebrequest-cb28dd22
        // Also see http://msdn.microsoft.com/en-us/library/system.net.httpwebrequest.begingetrequeststream%28v=vs.110%29.aspx
        internal static void executeAsync(Uri uri, int requestTimeout, string sendData, Listener listener)
        {
            // Start a background thread to execute the request and call the listener's onComplete method when finished
            Thread t = new Thread(new ThreadStart(delegate()
            {
                Result result = executeSync(uri, requestTimeout, sendData);

                if (null != listener)
                {
                    listener.onComplete(result);
                }
            }));
            t.IsBackground = true;
            t.Start();
        }

        internal static Result executeSync(Uri uri, int requestTimeout, string sendData)
        {
            // Create a request and execute it
            return new HttpRequest(uri, requestTimeout, sendData).execute();
        }
        #endregion

        #region helper classes/interfaces
        internal class Result
        {
            internal Error ErrorCode { get; set; } // Result error code
            internal string Response { get; set; } // Server response (null if no response)

            internal Result()
            {
                // Assume Success
                ErrorCode = Error.Success;
            }
        }

        internal interface Listener
        {
            void onComplete(Result result);
        }

        private class State
        {
            internal WebRequest WebRequest { get; set; }
            internal Result Result { get; set; }

            internal State(Uri uri)
            {
                // Create a Webrequest object to the desired URL. 
                WebRequest = WebRequest.Create(uri);

                WebRequest.ContentType = "application/x-www-form-urlencoded; charset=utf-8";

                // Set the Method property to 'POST' to post data to the URI.
                WebRequest.Method = "POST";

                WebRequest.Headers["ssf-use-positional-post-params"] = "true";
                WebRequest.Headers["ssf-contents-not-url-encoded"] = "true";

                Result = new Result();
            }
        }
        #endregion
    }
}
