using System;
using System.IO.IsolatedStorage;
using Polenter.Serialization;
using Polenter.Serialization.Core;

namespace Splyt
{
    internal static class LocalStorage
    {
        internal static T Load<T>(string fileName, bool deleteFile)
        {
            T ret = default(T);
            try
            {
                // Pull in the state data if there is any
                using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (storage.FileExists(fileName))
                    {
                        try
                        {
                            using (IsolatedStorageFileStream fs = storage.OpenFile(fileName, System.IO.FileMode.Open))
                            {
                                if (fs != null)
                                {
                                    ret = (T)new SharpSerializer(true).Deserialize(fs);
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

                        if (deleteFile)
                        {
                            // Delete the state file since it should now be in memory
                            storage.DeleteFile(fileName);
                        }
                    }
                }
            }
            catch (IsolatedStorageException) { }

            return ret;
        }

        internal static void Save(string fileName, object data)
        {
            if (null != data)
            {
                try
                {
                    using (IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        using (IsolatedStorageFileStream fs = storage.CreateFile(fileName))
                        {
                            if (fs != null)
                            {
                                new SharpSerializer(true).Serialize(data, fs);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // If the save or serialization fails, log it and move along
                    Util.logError(e);
                }
            }
        }
    }
}
