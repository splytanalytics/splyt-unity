using System;
using System.IO.IsolatedStorage;

namespace Splyt
{
    // We use the IsolatedStorageSettings class on windows phone to save or retrieve data as key/value pairs. 
    // This class is ideally suited for saving small pieces of data, such as apps that require access to settings at load time or when exiting
    // See http://msdn.microsoft.com/en-us/library/windowsphone/develop/jj714090%28v=vs.105%29.aspx
    // Note that on Windows Phone, IsolatedStorageSettings is not thread safe and throws an InvalidOperationException when the Save() method is called.
    // However you don’t have to call the Save method on Windows Phone. The data that you store in the IsolatedStorageSettings object is saved automatically.
    // See http://msdn.microsoft.com/en-us/library/windowsphone/develop/system.io.isolatedstorage.isolatedstoragesettings.save%28v=vs.105%29.aspx
    internal static class Prefs
    {
        // Adds an entry to the dictionary for the key-value pair.
        internal static void Add(string key, float value)
        {
            try
            {
                IsolatedStorageSettings.ApplicationSettings.Add(key, value);
            }
            catch (ArgumentNullException)
            {
                Util.logError("Cannot add pref for a null key");
            }
            catch (ArgumentException)
            {
                // This is thrown when the key already exists so it's not an error for us.  Move along...
            }
        }

        internal static void Add(string key, int value)
        {
            try
            {
                IsolatedStorageSettings.ApplicationSettings.Add(key, value);
            }
            catch (ArgumentNullException)
            {
                Util.logError("Cannot add pref for a null key");
            }
            catch (ArgumentException)
            {
                // This is thrown when the key already exists so it's not an error for us.  Move along...
            }
        }

        internal static void Add(string key, string value)
        {
            try
            {
                IsolatedStorageSettings.ApplicationSettings.Add(key, value);
            }
            catch (ArgumentNullException)
            {
                Util.logError("Cannot add pref for a null key");
            }
            catch (ArgumentException)
            {
                // This is thrown when the key already exists so it's not an error for us.  Move along...
            }
        }

        // Determines if the application settings dictionary contains the specified key.s
        internal static bool Contains(string key)
        {
            bool contains = false;

            try
            {
                contains =  IsolatedStorageSettings.ApplicationSettings.Contains(key);
            }
            catch (ArgumentNullException)
            {
                Util.logError("Cannot check pref for a null key");
            }

            return contains;
        }

        // Gets a value for the specified key.
        internal static bool TryGetValue(string key, out float value)
        {
            bool keyFound = false;
            value = default(float);

            try
            {
                keyFound = IsolatedStorageSettings.ApplicationSettings.TryGetValue<float>(key, out value);
            }
            catch (ArgumentNullException)
            {
                Util.logError("Cannot get pref for a null key");
            }
            catch (InvalidCastException)
            {
                Util.logError("The type of value returned cannot be implicitly cast to [float]");
            }

            return keyFound;
        }

        internal static bool TryGetValue(string key, out int value)
        {
            bool keyFound = false;
            value = default(int);

            try
            {
                keyFound = IsolatedStorageSettings.ApplicationSettings.TryGetValue<int>(key, out value);
            }
            catch (ArgumentNullException)
            {
                Util.logError("Cannot get pref for a null key");
            }
            catch (InvalidCastException)
            {
                Util.logError("The type of value returned cannot be implicitly cast to [int]");
            }

            return keyFound;
        }

        internal static bool TryGetValue(string key, out string value)
        {
            bool keyFound = false;
            value = default(string);

            try
            {
                keyFound = IsolatedStorageSettings.ApplicationSettings.TryGetValue<string>(key, out value);
            }
            catch (ArgumentNullException)
            {
                Util.logError("Cannot get pref for a null key");
            }
            catch (InvalidCastException)
            {
                Util.logError("The type of value returned cannot be implicitly cast to [string]");
            }

            return keyFound;
        }
    }
}
