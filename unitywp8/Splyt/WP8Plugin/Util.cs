using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Phone.Info;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace Splyt
{
	public class Util
	{
		private const string LOG_TAG = "com.rsb.splyt: ";
		private static bool sLogEnabled = false;
		private static IDictionary<string, object> sDeviceAndAppInfo = new Dictionary<string, object>();
        internal static IDictionary<string, object> getDeviceAndAppInfo() { return sDeviceAndAppInfo; }
        internal static string getSDKName(string namePrefix) { return namePrefix + "-windowsphone"; }

        private static ISet<string> sValidCurrencyCodes = new HashSet<string>();
        private static IDictionary<string, ISet<string>> sCurrencyCodesBySymbol = new Dictionary<string, ISet<string>>();

        // See http://msdn.microsoft.com/en-us/library/windowsphone/develop/system.globalization.cultureinfo%28v=vs.105%29.aspx
        private static readonly string[] CULTURE_NAMES_ALL = { "af-ZA", "sq-AL", "ar-DZ", "ar-BH", "ar-EG", "ar-IQ", "ar-JO", "ar-KW", "ar-LB", "ar-LY",
                                                               "ar-MA", "ar-OM", "ar-QA", "ar-SA", "ar-SY", "ar-TN", "ar-AE", "ar-YE", "hy-AM", "az-Cyrl-AZ",
                                                               "az-Latn-AZ", "eu-ES", "be-BY", "bg-BG", "ca-ES", "zh-HK", "zh-MO", "zh-CN", "zh-Hans", "zh-SG",
                                                               "zh-TW", "zh-Hant", "hr-BA", "hr-HR", "cs-CZ", "da-DK", "dv-MV", "nl-BE", "nl-NL", "en-AU",
                                                               "en-BZ", "en-CA", "en-029", "en-IE", "en-JM", "en-NZ", "en-PH", "en-ZA", "en-TT", "en-GB", 
                                                               "en-US", "en-ZW", "et-EE", "fo-FO", "fa-IR", "fi-FI", "fr-BE", "fr-CA", "fr-FR", "fr-LU", 
                                                               "fr-MC", "fr-CH", "gl-ES", "ka-GE", "de-AT", "de-DE", "de-DE_phoneb", "de-LI", "de-LU", "de-CH", 
                                                               "el-GR", "gu-IN", "he-IL", "hi-IN", "hu-HU", "is-IS", "id-ID", "it-IT", "it-CH", "ja-JP", 
                                                               "kn-IN", "kk-KZ", "kok-IN","ko-KR", "ky-KG", "lv-LV", "lt-LT", "mk-MK", "ms-BN", "ms-MY", 
                                                               "mr-IN", "mn-MN", "nb-NO", "nn-NO", "pl-PL", "pt-BR", "pt-PT", "pa-IN", "ro-RO", "ru-RU", 
                                                               "sa-IN", "sr-Cyrl-CS", "sr-Latn-CS", "sk-SK", "sl-SI", "es-AR", "es-BO", "es-CL", "es-CO", "es-CR", 
                                                               "es-DO", "es-EC", "es-SV", "es-GT", "es-HN", "es-MX", "es-NI", "es-PA", "es-PY", "es-PE", 
                                                               "es-PR", "es-ES", "es-ES_tradnl", "es-UY", "es-VE", "sw-KE", "sv-FI", "sv-SE", "syr-SY", "ta-IN", 
                                                               "tt-RU", "te-IN", "th-TH", "tr-TR", "uk-UA", "ur-PK", "uz-Cyrl-UZ", "uz-Latn-UZ", "vi-VN" };

		public static void setLogEnabled(bool value)
		{
			sLogEnabled = value;
		}

		// Internal logging.  These can be enabled by calling Util.setLogEnabled(true)
		public static void logDebug(string msg)
		{
            if (sLogEnabled)
            {
                Debug.WriteLine(LOG_TAG + msg);
            }
		}
		
		public static void logError(Exception e)
		{
			if (sLogEnabled)
			{
                Debug.WriteLine(LOG_TAG + e.Message);
			}
		}
		
		public static void logError(string msg)
		{
			if (sLogEnabled)
			{
				Debug.WriteLine(LOG_TAG + msg);
			}
		}

		internal static void cacheDeviceAndAppInfo()
		{
			// Clear out any previously set data
			sDeviceAndAppInfo.Clear();
			
			// Set the "platform" 
			sDeviceAndAppInfo.Add("splyt.platform", "windowsphone");
			
			// Get the rest of the information about the device
            sDeviceAndAppInfo.Add("splyt.deviceinfo.manufacturer", DeviceStatus.DeviceManufacturer);
            sDeviceAndAppInfo.Add("splyt.deviceinfo.model", DeviceStatus.DeviceName);
			sDeviceAndAppInfo.Add("splyt.deviceinfo.osversion", Environment.OSVersion.ToString());

			// On Windows phone 8, pull the app version from the manifest
			string Version = XDocument.Load("WMAppManifest.xml").Root.Element("App").Attribute("Version").Value;
			sDeviceAndAppInfo.Add("splyt.appinfo.versionName", Version);
		}

        internal static void cacheCurrencyInfo()
        {
            // Clear out any previously set data
            sValidCurrencyCodes.Clear();
            sCurrencyCodesBySymbol.Clear();

            // Cache a set of valid ISO 4217 currency codes and a map of valid currency symbols to ISO 4217 currency codes
            foreach (string cultureName in CULTURE_NAMES_ALL)
            {
                try
                {
                    RegionInfo ri = new RegionInfo(cultureName);
                    sValidCurrencyCodes.Add(ri.ISOCurrencySymbol);

                    if (sCurrencyCodesBySymbol.ContainsKey(ri.CurrencySymbol))
                    {
                        sCurrencyCodesBySymbol[ri.CurrencySymbol].Add(ri.ISOCurrencySymbol);
                    }
                    else
                    {
                        ISet<string> codes = new HashSet<string>();
                        codes.Add(ri.ISOCurrencySymbol);
                        sCurrencyCodesBySymbol.Add(ri.CurrencySymbol, codes);
                    }
                }
                catch (ArgumentException)
                {
                    // Not a valid culture name.  Ok, move along....
                }
            }
        }

        // Given an input currency string, return a string that is valid currency string.
        // This can be either a valid ISO 4217 currency code or a currency symbol (e.g., for real currencies),  or simply any other ASCII string (e.g., for virtual currencies)
        // If one cannot be determined, this method returns "unknown"
        public static string getValidCurrencyString(string currency)
        {
            string validCurrencyStr;

            // First check if the string is already a valid ISO 4217 currency code (i.e., it's in the list of known codes)
            if (sValidCurrencyCodes.Contains(currency.ToUpper()))
            {
                // It is, just return it
                validCurrencyStr = currency.ToUpper();
            }
            else
            {
                // Not a valid currency code, is it a currency symbol?
                ISet<string> possibleCodes;
                if (sCurrencyCodesBySymbol.TryGetValue(currency.ToUpper(), out possibleCodes))
                {
                    // It's a valid symbol

                    // If there is only one associated currency code, use it
                    if (1 == possibleCodes.Count)
                    {
                        using (IEnumerator<string> iter = possibleCodes.GetEnumerator())
                        {
                            iter.MoveNext();
                            validCurrencyStr = iter.Current;
                        }
                    }
                    else
                    {
                        // Ok, more than one code associated with this symbol
                        // We make a best guess as to the actual currency code based on the user's locale.
                        RegionInfo ri = RegionInfo.CurrentRegion;
                        if (possibleCodes.Contains(ri.ISOCurrencySymbol))
                        {
                            // The locale currency is in the list of possible codes
                            // It's pretty likely that this currency symbol refers to the locale currency, so let's assume that
                            // This is not a perfect solution, but it's the best we can do until Google and Amazon start giving us more than just currency symbols
                            validCurrencyStr = ri.ISOCurrencySymbol;
                        }
                        else
                        {
                            // We have no idea which currency this symbol refers to, so just set it to "unknown"
                            validCurrencyStr = "unknown";
                        }
                    }
                }
                else
                {
                    // This is not a known currency symbol, so it must be a virtual currency
                    // Strip out any non-ASCII characters
                    validCurrencyStr = Regex.Replace(currency, @"[^\u0000-\u007F]", string.Empty);
                }
            }

            return validCurrencyStr;
        }

		/// <summary>
		/// Unix timestamp
		/// </summary>
		internal static double Timestamp()
		{
			TimeSpan span = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return span.TotalSeconds;
		}
	}
}