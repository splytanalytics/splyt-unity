using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using Splyt.External.MiniJSON; 

namespace Splyt
{
	/// <summary>
	/// For logging application telemetry
	/// </summary>
	public class Instrumentation
	{
		/// <summary>
		/// Factory method for invoking Splyt.Transaction methods
		/// </summary>
		/// <param name="category">The transaction category</param>
		/// <param name="transactionId">Transaction id, if applicable - this is only REQUIRED in situation where multiple transactions in the same category may exist (read: be concurrently begun)</param> 
		public static Transaction Transaction(string category, string transactionId = null)
		{
			return new Splyt.Transaction(category, transactionId);
		}

		/// <summary>
		/// Updates state information about a device
		/// </summary>
		/// <param name="state">A key-value object representing the device state we want to update. This can be a nested object structure.</param>
		public static void updateDeviceState(Dictionary<string, object> state) 
		{
			// we still want to perform the serialization when we're not using actively using it, to help in catching errors
			#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
			string stateStr = Json.Serialize(state);
			#endif

			#if UNITY_IPHONE && !UNITY_EDITOR
			splyt_instrumentation_updatedevicestate(stateStr);
			#elif UNITY_ANDROID && !UNITY_EDITOR
			Core.callNative("splyt_instrumentation_updatedevicestate", new object[] {stateStr});
            #else
            // Native Unity/Windows Phone 8
            InstrumentationSubsystem.updateDeviceState(state);
			#endif
		}

		/// <summary>
		/// Updates state information about a user
		/// </summary>
		/// <param name="state">A key-value object representing the user state we want to update. This can be a nested object structure.</param>
		public static void updateUserState(Dictionary<string, object> state) 
		{
			// we still want to perform the serialization when we're not using actively using it, to help in catching errors
			#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
			string stateStr = Json.Serialize(state);
			#endif

			#if UNITY_IPHONE && !UNITY_EDITOR
			splyt_instrumentation_updateuserstate(stateStr);
			#elif UNITY_ANDROID && !UNITY_EDITOR
			Core.callNative("splyt_instrumentation_updateuserstate", new object[] {stateStr});
            #else
            // Native Unity/Windows Phone 8
            InstrumentationSubsystem.updateUserState(state);
			#endif
		}

		/// <summary>
		/// Update a collection balance for the current entity
		/// </summary>
		/// <param name="name">The application-supplied name for the collection</param>
		/// <param name="balance">Current balance</param>
		/// <param name="balanceModification">The amount that the balance is changing by (if known)</param>
		/// <param name="isCurrency">If set to <c>true</c> the collection is treated as an in-app virtual currency</param>
		public static void updateCollection(string name, double balance, double balanceModification, bool isCurrency) 
		{
			#if UNITY_IPHONE && !UNITY_EDITOR
			splyt_instrumentation_updatecollection(name, balance, balanceModification, isCurrency);
			#elif UNITY_ANDROID && !UNITY_EDITOR
			Core.callNative("splyt_instrumentation_updatecollection", new object[] {name, balance, balanceModification, isCurrency});
            #else
            // Native Unity/Windows Phone 8
            InstrumentationSubsystem.updateCollection(name, balance, balanceModification, isCurrency);
            #endif
		}

		#if UNITY_IPHONE && !UNITY_EDITOR
		[DllImport("__Internal")]
		private static extern void splyt_instrumentation_updatedevicestate(string deviceProperties);
		[DllImport("__Internal")]
		private static extern void splyt_instrumentation_updateuserstate(string userProperties);
		[DllImport("__Internal")]
		private static extern void splyt_instrumentation_updatecollection(string name, double balance, double balanceModification, bool isCurrency);
		#endif
	}
}