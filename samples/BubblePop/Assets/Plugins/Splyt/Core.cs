using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Splyt.External.MiniJSON; 

namespace Splyt 
{
	/// <summary>
	/// The most central pieces of the Splyt Framework.
	/// </summary>
	public class Core 
	{
				private const string SDK_NAME_PREFIX = "unity";
				private const string SDK_VERSION = "5.0.0";
		internal const string HUB_OBJECT = "Splyt";

		/// <summary>
		/// Gets the registered id for the currently active user
		/// </summary>
		/// <value>The user id</value>
		public static string userId 
		{ 
			get 
			{ 
				#if UNITY_IPHONE && !UNITY_EDITOR
				return splyt_core_getuserid();
				#elif UNITY_ANDROID && !UNITY_EDITOR
				return callNative<string>("splyt_core_getuserid", null);
								#else
								return CoreSubsystem.UserId;
				#endif
			} 
		}

		/// <summary>
		/// Gets the registered id for the device
		/// </summary>
		/// <value>The device id</value>
		public static string deviceId 
		{ 
			get 
			{ 
				#if UNITY_IPHONE && !UNITY_EDITOR
				return splyt_core_getdeviceid();
				#elif UNITY_ANDROID && !UNITY_EDITOR
				return callNative<string>("splyt_core_getdeviceid", null);
								#else
								return CoreSubsystem.DeviceId;
				#endif
			} 
		}

		/// <summary>
		/// Initializes Splyt Framework for use, including instrumentation and tuning.
		/// </summary>
		/// <param name="initParams">Initialization parameters</param>
		/// <param name="cb">Application defined callback which will occur on completion</param>
		public static void init(InitParams initParams, Callback cb) 
		{
			Debug.Log ("Core.init()");
			// this should only be enabled during android development!!!
			//AndroidJNIHelper.debug = true;

			Error error = Error.Success;

			// Enable/disable logging
						Util.setLogEnabled(initParams.logEnabled);

			if(null == initParams) 
			{
				Util.logError("No init parameters provided");
				error = Error.InvalidArgs;
			}
			else if (null == cb) 
			{
				Util.logError("Please provide a valid Splyt.Callback");
				error = Error.InvalidArgs;
			}
			else if (Constants.ENTITY_TYPE_USER != initParams.userInfo.type) 
			{
				Util.logError("To provide intitial user settings, be sure to use createUserInfo");
				error = Error.InvalidArgs;
			}
			else if (Constants.ENTITY_TYPE_DEVICE != initParams.deviceInfo.type) 
			{
				Util.logError("To provide intitial device settings, be sure to use createDeviceInfo");
				error = Error.InvalidArgs;
			}

			#if !UNITY_WP8
			_initHub(initParams.OnNotification);
			#endif

						if (Error.Success == error) 
			{
				#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
								string userProperties = Json.Serialize(initParams.userInfo.properties);
				string deviceProperties = Json.Serialize(initParams.deviceInfo.properties);

								registerCallback("onSplytInitComplete", cb);
				#endif

				#if UNITY_IPHONE && !UNITY_EDITOR
				splyt_core_init(initParams.customerId, initParams.userInfo.entityId, userProperties, initParams.deviceInfo.entityId, deviceProperties, initParams.requestTimeout, initParams.host, initParams.logEnabled, SDK_NAME_PREFIX, SDK_VERSION, initParams.notificationHost, initParams.notificationDisableAutoClear, HUB_OBJECT);
				#elif UNITY_ANDROID && !UNITY_EDITOR
				object[] parms = new object[] {_getActivity(), initParams.customerId, initParams.userInfo.entityId, userProperties, initParams.deviceInfo.entityId, deviceProperties, initParams.requestTimeout, initParams.host, initParams.logEnabled, SDK_NAME_PREFIX, SDK_VERSION, initParams.notificationHost, initParams.notificationSmallIcon, initParams.notificationAlwaysPost, initParams.notificationDisableAutoClear, HUB_OBJECT};
				Debug.Log("Calling into splyt android.... " + parms.Length);
				callNativeAsync("splyt_core_init", parms);
				#else
				// Native Unity/Windows Phone 8
				InstrumentationSubsystem.init();

				// Builds targeting the web player need to be handled specially due to the security model
				// Unfortunately, there is no good way to determine that at run time within the plugin.
				#if UNITY_WEBPLAYER
				const bool isWebPlayer = true;
				#else
				const bool isWebPlayer = false;
				#endif

				TuningSubsystem.init(delegate(Error err)
								{
					CoreSubsystem.init(initParams.customerId, new TuningSubsystem.Updater(), initParams.userInfo.entityId, initParams.userInfo.properties, initParams.deviceInfo.entityId, initParams.deviceInfo.properties, initParams.requestTimeout, initParams.host, initParams.logEnabled, SDK_NAME_PREFIX, SDK_VERSION, cb, HUB_OBJECT, isWebPlayer);
								});
				#endif
			}
						else if (null != cb)
						{
								// Some argument error, just call the callback immediately
								cb(error);
						}
		}

		/// <summary>
		/// Register a user with Splyt and make them the currently active user.  This can be done at any point when a new user is interacted with by 
		/// the application. Note that if the active user is known at startup, it is generally ideal to provide their info directly to Splyt.Core.init instead
		/// </summary>
		/// <param name="userInfo">An EntityInfo created with InitParams.createUserInfo</param>
		/// <param name="cb">Application defined callback which will occur on completion</param>
		public static void registerUser(EntityInfo userInfo, Callback cb) 
		{
			Error error = Error.Success;

			if (null == cb) 
			{
				Util.logError("Please provide a valid Splyt.Callback");
				error = Error.InvalidArgs;
			}
			else if (Constants.ENTITY_TYPE_USER != userInfo.type) 
			{
				Util.logError("To provide user settings, be sure to use createUserInfo");
				error = Error.InvalidArgs;
			}

						if (Error.Success == error) 
			{
				#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
				string userProperties = Json.Serialize(userInfo.properties);

								registerCallback("onSplytRegisterUserComplete", cb);
				#endif

				#if UNITY_IPHONE && !UNITY_EDITOR
				splyt_core_registeruser(userInfo.entityId, userProperties, HUB_OBJECT);
				#elif UNITY_ANDROID && !UNITY_EDITOR
				callNativeAsync("splyt_core_registeruser", new object[] {userInfo.entityId, userProperties, HUB_OBJECT});
								#else
								CoreSubsystem.registerUser(userInfo.entityId, userInfo.properties, new TuningSubsystem.Updater(), cb);
				#endif
			}
						else if (null != cb)
						{
								// Some argument error, just call the callback immediately
								cb(error);
						}
		}

		/// <summary>
		/// Explicitly sets the active user id. Generally only required when multiple concurrent users are required/supported, since
		/// init() and registerUser() both activate the provided user by default.
		/// </summary>
		/// <returns>An error code</returns>
		/// <param name="userId">The user id, which had been previously registered with Splyt.Core.registerUser</param>
		public static Error setActiveUser(string userId) 
		{
			#if UNITY_IPHONE && !UNITY_EDITOR
			return splyt_core_setactiveuser(userId).toSplytError();
			#elif UNITY_ANDROID && !UNITY_EDITOR
			return callNative<string>("splyt_core_setactiveuser", new object[] {userId}).toSplytError();
						#else
						return CoreSubsystem.setActiveUser(userId);
			#endif
		}

		/// <summary>
		/// Useful when the logged in user logs out of the application.  Clearing the active user allows Splyt to provide non user-specific 
		/// tuning values and report telemetry which is not linked to a user
		/// </summary>
		/// <returns>An error code</returns>
		public static Error clearActiveUser() 
		{
			#if UNITY_IPHONE && !UNITY_EDITOR
			return splyt_core_clearactiveuser().toSplytError();
			#elif UNITY_ANDROID && !UNITY_EDITOR
			return callNative<string>("splyt_core_clearactiveuser", null).toSplytError();
						#else
						return CoreSubsystem.setActiveUser(null);
			#endif
		}

		/// <summary>
		/// Pause Splyt.  This causes Splyt to save off its state to Internal Storage and stop checking for events to send.
		/// One would typically call this whenever the application is sent to the background.
		/// 
		/// <b>Note:</b> On some platforms, one can still make calls to Splyt functions even when it's paused, but doing so will trigger 
		/// reads and writes to Internal Storage, so it should be done judiciously
		/// </summary>
		public static void pause() 
		{
			#if UNITY_IPHONE && !UNITY_EDITOR
			splyt_core_pause();
			#elif UNITY_ANDROID && !UNITY_EDITOR
			callNative("splyt_core_pause", null);
						#else
						CoreSubsystem.pause();
			#endif
		}

		/// <summary>
		/// Resume Splyt.  This causes Splyt read its last known state from Internal Storage and restart polling for events to send.
		/// One would typically call this whenever the application is brought to the foreground.
		/// </summary>
		public static void resume() 
		{
			#if UNITY_IPHONE && !UNITY_EDITOR
			splyt_core_resume();
			#elif UNITY_ANDROID && !UNITY_EDITOR
			callNative("splyt_core_resume", null);
						#else
						CoreSubsystem.resume();
			#endif
		}

		#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
				internal static void registerCallback(string action, Callback cb)
		{
			if(null != _hub)
				_hub.registerCallback(action, cb);
		}

		internal static void makeCallback(string action, Error error)
		{
			if(null != _hub)
				_hub.makeCallback(action, ((int)error).ToString());
		}
				#endif

		#if UNITY_ANDROID && !UNITY_EDITOR
		internal static void callNative(string method, object[] args) 
		{
			using(AndroidJavaClass wrapper = new AndroidJavaClass("com.rsb.splyt.SplytWrapper")) 
			{
				wrapper.CallStatic(method, args);
			}
		}
		
		internal static T callNative<T>(string method, object[] args) 
		{
			T result;
			using(AndroidJavaClass wrapper = new AndroidJavaClass("com.rsb.splyt.SplytWrapper")) 
			{
				result = wrapper.CallStatic<T>(method, args);
			}
			return result;
		}
		
		internal static void callNativeAsync(string method, object[] args) 
		{
			_getActivity().Call("runOnUiThread", new AndroidJavaRunnable(() =>
			{
				using(AndroidJavaClass wrapper = new AndroidJavaClass("com.rsb.splyt.SplytWrapper")) 
				{
					Debug.Log("Calling SplytWrapper." + method + "(" + args.Length + " args");
					wrapper.CallStatic(method, args);
				}
			}));
		}

		private static AndroidJavaObject _activityPlsCallGetter;
		private static AndroidJavaObject _getActivity()
		{
			if(null == _activityPlsCallGetter)
			{
				using(AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
				{
					_activityPlsCallGetter = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
				}
			}
			return _activityPlsCallGetter;
		}

		private static AndroidJavaObject _contextPlsCallGetter;
		private static AndroidJavaObject _getContext()
		{
			if(null == _contextPlsCallGetter) 
			{
				_contextPlsCallGetter = _getActivity().Call<AndroidJavaObject>("getApplicationContext");
			}
			return _contextPlsCallGetter;
		}
		#endif

		#if !UNITY_WP8
			#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
					private static SplytCallbackHub _hub;
			#endif
		private static void _initHub(NotificationListener notificationListener)
		{
			// make a persistent game object to handle upward calls from native API
			GameObject go = GameObject.Find(HUB_OBJECT);
			if(null == go) go = new GameObject(HUB_OBJECT);
			GameObject.DontDestroyOnLoad(go);

			#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
			// put the splytcallback component on it, if it's not already there
			_hub = go.GetComponent<SplytCallbackHub>();
			if(null == _hub) _hub = go.AddComponent<SplytCallbackHub>();
			_hub.setNotificationListener(notificationListener);
			#endif
        }
        #endif

		#if UNITY_IPHONE && !UNITY_EDITOR
		[DllImport("__Internal")]
		private static extern string splyt_core_getuserid();
		[DllImport("__Internal")]
		private static extern string splyt_core_getdeviceid();
		[DllImport("__Internal")]
		private static extern void splyt_core_init(string customerId, string userId, string userProperties, string deviceId, string deviceProperties, int reqTimeout, string host, bool logEnabled, string sdkNamePre, string sdkVersion, string notificationHost, bool notificationDisableAutoClear, string callbackObj);
		[DllImport("__Internal")]
		private static extern void splyt_core_registeruser(string userId, string userProperties, string callbackObj);
		[DllImport("__Internal")]
		private static extern string splyt_core_setactiveuser(string userId);
		[DllImport("__Internal")]
		private static extern string splyt_core_clearactiveuser();
		[DllImport("__Internal")]
		private static extern string splyt_core_pause();
		[DllImport("__Internal")]
		private static extern string splyt_core_resume();
		#endif
	}

	#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
		internal class SplytCallbackHub : MonoBehaviour
	{
		Dictionary<string, Callback> _callbacks = new Dictionary<string, Callback>();
		NotificationListener _notificationListener;

		internal void registerCallback(string action, Callback cb)
		{
			_callbacks[action] = cb;
		}

		internal void makeCallback(string action, string error)
		{
			if(_callbacks.ContainsKey(action))
			{
				Splyt.Error splytError = error.toSplytError();
				_callbacks[action](splytError);
			}

			_callbacks.Remove(action);
		}

		internal void setNotificationListener(NotificationListener listener) {
			_notificationListener = listener;
		}

		void onSplytNotificationReceived(string message) {
			bool wasLaunchedBy = message.StartsWith("1");
			string outMsg = null;
			if (message.Length > 1) {
                outMsg = message.Substring(1);
			}
			Debug.Log("Splyt Notification " + (wasLaunchedBy ? "was launched" : "was not launched") + ": " + outMsg);
			if (_notificationListener != null) {
				_notificationListener(outMsg, wasLaunchedBy);
			}
		}
		
		void onSplytInitComplete(string err) { makeCallback("onSplytInitComplete", err); }
		void onSplytRegisterUserComplete(string err) { makeCallback("onSplytRegisterUserComplete", err); }
		void onSplytRefreshComplete(string err) { makeCallback("onSplytRefreshComplete", err); }
	}
		#endif
}
