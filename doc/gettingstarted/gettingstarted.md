Getting Started
=========

Last Updated: April 30, 2015

_Note that the code examples below assume_ <code>using Splyt;</code> _for brevity, but developers may prepend references with_ <code>Splyt.</code> _as a matter of preference or for disambiguity._

##Initialization
SPLYT initialization should be completed as early as possible in the flow of an application. This allows 
telemetry reporting and the usage of SPLYT tuned variables throughout the application. Note that the 
initialization call triggers a callback upon completion, after which point you can reliably use any of
the calls in the SPLYT SDK. Here's an example, passing [InitParams](@ref Splyt::InitParams) to [Core.init](@ref Splyt::Core::init):
~~~{.cs}
Splyt.InitParams initParams = InitParams.create(
    // customer id is the only required field
    "my-customer-id",                   // contact SPLYT if you do not have a customer id yet
    
    // if you have additional information about your user or device to report at startup, you can
    userInfo: myUserInfo,               // more below about user...
    deviceInfo: myDeviceInfo            // ...and device entities
);
Core.init(initParams, delegate(Error initError) {  
    // let application know that SPLYT is ready
});
~~~

###Notifications
To use SPLYT Push Notifications, set up a notification listener (a method with the signature ```void(String message, bool wasLaunchedBy)```) on the InitParams object.  This listener will be called when your app receives a push notification:

~~~{.cs}
initParams.OnNotification = delegate(string message, bool wasLaunchedBy)
		{
			Debug.Log("Received notification!  Was I launched by it? " + (wasLaunchedBy ? "Yes" : "No") + ". Message: " + message);
		};
~~~

For Android, make sure you have the following permissions and intents set up in the AndroidManifest.xml:

~~~{.xml}
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android" package="com.splyt.example" android:theme="@android:style/Theme.NoTitleBar" android:versionName="1.2" android:versionCode="3" android:installLocation="preferExternal">
  <supports-screens android:smallScreens="true" android:normalScreens="true" android:largeScreens="true" android:xlargeScreens="true" android:anyDensity="true" />
  <application android:icon="@drawable/app_icon" android:label="@string/app_name" android:debuggable="false">
    <activity android:name="com.unity3d.player.UnityPlayerNativeActivity" android:label="@string/app_name" android:screenOrientation="portrait" android:launchMode="singleTask" android:configChanges="mcc|mnc|locale|touchscreen|keyboard|keyboardHidden|navigation|orientation|screenLayout|uiMode|screenSize|smallestScreenSize|fontScale">
      <intent-filter>
        <action android:name="android.intent.action.MAIN" />
        <category android:name="android.intent.category.LAUNCHER" />
      </intent-filter>
      <meta-data android:name="unityplayer.UnityActivity" android:value="true" />
      <meta-data android:name="unityplayer.ForwardNativeEventsToDalvik" android:value="false" />
    </activity>
    <meta-data android:name="com.google.android.gms.version" android:value="@integer/google_play_services_version" />
    <receiver
        android:name="com.rsb.splyt.GcmBroadcastReceiver"
        android:permission="com.google.android.c2dm.permission.SEND">
        <intent-filter>
            <action android:name="com.google.android.c2dm.intent.RECEIVE"/>
            <category android:name="com.rsb.bubblepop"/>
        </intent-filter>
    </receiver>
    <receiver android:name="com.rsb.splyt.GcmUpdateReceiver">
        <intent-filter>
            <action android:name="android.intent.action.PACKAGE_REPLACED"/>
            <data android:path="com.rsb.bubblepop" android:scheme="package"/>
        </intent-filter>
        <intent-filter>
            <action android:name="android.intent.action.BOOT_COMPLETED"/>
        </intent-filter>
    </receiver>
    <service android:name="com.rsb.splyt.GcmIntentService"/>
  </application>
  <uses-permission android:name="android.permission.INTERNET" />

  <!-- ADM uses WAKE_LOCK to keep the processor from sleeping when a message is received. -->
  <uses-permission android:name="android.permission.WAKE_LOCK"/>

  <!-- This permission allows your app access to receive push notifications from ADM/GCM. -->
  <uses-permission android:name="com.amazon.device.messaging.permission.RECEIVE"/>
  <uses-permission android:name="com.google.android.c2dm.permission.RECEIVE"/>

  <!-- This permission ensures that no other application can intercept your ADM/GCM messages. -->
  <permission android:name="com.rsb.bubblepop.permission.RECEIVE_ADM_MESSAGE" android:protectionLevel="signature"/>
  <uses-permission android:name="com.rsb.bubblepop.permission.RECEIVE_ADM_MESSAGE"/>

  <permission android:name="com.rsb.bubblepop.permission.C2D_MESSAGE" android:protectionLevel="signature"/>
  <uses-permission android:name="com.rsb.bubblepop.permission.C2D_MESSAGE"/>

  <!-- This permission allows us to detect when the user performs a system update.
       Note that this is not required for push notifications to work, but it does guarantee that notifications will still
       be sent to the device after a system update, even if the user doesn't launch the app.
  -->
  <uses-permission android:name="android.permission.RECEIVE_BOOT_COMPLETED"/>
</manifest>
~~~

###Devices
SPLYT will automatically track some hardware information about your device, but if you have additional (perhaps application-specific) properties to report, you can do so at initialization time.  To do this, provide <code>deviceInfo</code> to the <code>init</code> call, using <code>[createDeviceInfo](@ref Splyt::EntityInfo::createDeviceInfo)</code>:

~~~{.cs}
EntityInfo myDeviceInfo = EntityInfo.createDeviceInfo().setProperty("aProperty", 1).setProperty("aboutMyDevice", "how_interesting");
~~~

or:

~~~{.cs}
Dictionary<string, object> myDeviceProperties = new Dictionary<string, object> { { "aProperty", 1 }, { "aboutMyDevice", "how_interesting" } };
EntityInfo myDeviceInfo = EntityInfo.createDeviceInfo(properties: myDeviceProperties);
~~~

To report any changes to the state of the device at any later point, see <code>[updateDeviceState](@ref Splyt::Instrumentation::updateDeviceState)</code>.

###Users
Many applications track individual users with some form of user ID. For such applications, if you know the user ID at startup, it is recommended to 
pass a <code>userInfo</code> to the <code>init</code> call using <code>[createUserInfo](@ref Splyt::EntityInfo::createUserInfo)</code>:

~~~{.cs}
EntityInfo myUserInfo = EntityInfo.createUserInfo("theUserId");
~~~

Note that you may also use <code>[setProperty](@ref Splyt::EntityInfo::setProperty)</code> or <code>[setProperties](@ref Splyt::EntityInfo::setProperties)</code>, as in the device case above, if you have additional state to track for the user:

~~~{.cs}
EntityInfo myUserInfo2 = EntityInfo.createUserInfo("theUserId").setProperty("level", 10);
~~~

If the user is *not* known at startup, they can be registered at a later point, by creating an <code>[EntityInfo](@ref Splyt::EntityInfo)</code> as we've just done, and providing it to <code>[registerUser](@ref Splyt::Core::registerUser)</code>:

~~~{.cs}
Core.registerUser(myUserInfo, delegate(Error registerUserError) { 
    /* application may now safely log telemetry and use tuned variables for the user */ 
});
~~~

For applications which allow multiple concurrent users, see <code>[setActiveUser](@ref Splyt::Core::setActiveUser)</code>.<br>
For applications which need to support users 'logging out', see <code>[clearActiveUser](@ref Splyt::Core::clearActiveUser).</code><br>
To report any changes to the state of the user at any later point, see <code>[updateUserState](@ref Splyt::Instrumentation::updateUserState)</code>.

##Telemetry
###Transactions
Transactions are the primary unit of telemetry in SPLYT. Reporting events with a <code>[Transaction](@ref Splyt::Transaction)</code> is simple, but powerful. Consider:

~~~{.cs}
Instrumentation.Transaction("UserAction").begin();
// time passes
Instrumentation.Transaction("UserAction").setProperty("something interesting", "about the transaction").end();
~~~

Note that properties of the transaction may be set for <code>[begin](@ref Splyt::Transaction::begin)</code> or <code>[end](@ref Splyt::Transaction::end)</code> or at any point between with
<code>[update](@ref Splyt::Transaction::update)</code>, but as a best practice they should be reported as early as their value is known or known to have changed.

To handle the somewhat common case where a transaction occurs instantaneously, use the <code>[beginAndEnd](@ref Splyt::Transaction::beginAndEnd)</code> method.

Also note that the setting of transaction properties is only persisted after a call to <code>begin</code>, <code>update</code>, <code>end</code>, or <code>beginAndEnd</code>.

###Collections
Collections in SPLYT are an abstraction for anything the user of the application might accumulate, or have a varying quantity of. Common examples of this might be
virtual currency, number of contacts, or achievements. <code>[updateCollection](@ref Splyt::Instrumentation::updateCollection)</code> can be used at any point where the quantity
of a collection is thought to have changed:

~~~{.cs}
Instrumentation.updateCollection("friends", 27, -2, false);
~~~

It is recommended to instrument all of the important collections in the application, as they will add surprising power to your data analysis through contextualization.

###Entity state
As previously mentioned, user and device (considered SPLYT entities) may have their state recorded during initialization, or through usage of
<code>[updateUserState](@ref Splyt::Instrumentation::updateUserState)</code> and <code>[updateDeviceState](@ref Splyt::Instrumentation::updateDeviceState)</code>. Reporting
changes in entity state is another great way to unlock the power of contextualization.

##Tuning
The SPLYT Tuning system provides a means for dynamically altering the behavior of the application, conducting an A/Z test, and creating customized behavior for segments
of your user base (targeting). The instrumentation is extremely simple, and the hardest part might be decided what you want to be able to tune. Upon initialization, SPLYT will retrieve
any dynamic tuning for the device or user. At any point thereafter, the application may request a value using <code>[getVar](\ref Splyt::Tuning)</code>:

~~~{.cs}
// before SPLYT
string welcomeString = "Hi there!";
double welcomeDuration =3.0;

// with SPLYT tuning variables
string welcomeString = Tuning.getVar("welcomeString", "Hi there!");
double welcomeDuration = Tuning.getVar("welcomeTime", 3.0);
~~~

Note the presence of the second parameter, which specifies a default value. It is important to provide a 'safe' default value to provide reliable behavior in the event that a 
dynamic value is not available. This also allows for the application to be safely instrumented in advance of any dynamic tuning, targeting, or A/Z test.

In addition to instrumenting key points in your code with <code>[getVar](@ref Splyt::Tuning)</code>, applications which may remain running for long periods of time are encouraged to utilize <code>[refresh](@ref Splyt::Tuning::refresh)</code> in order to make sure that the application has access to the latest tuned values at any point in time. A typical integration point for <code>refresh</code> on a mobile application might be whenever the application is brought to the foreground, and the code for handling it is quite simple:

~~~{.cs}
Tuning.refresh(delegate(Error refreshError) {
    // at this point, tuning for the device and all registered users should be refreshed
});
~~~

It is not necessary to block for the completion of this call, as is typically recommended for <code>[init](@ref Splyt::Core::init)</code> 
and <code>[registerUser](@ref Splyt::Core::registerUser)</code>, since the application should already have access to viable tuned variables prior to the call to refresh. 
However, the callback is provided, leaving it to the discretion of the integrator.

##Mobile Applications
For mobile applications, it is likely that the app may be foregrounded and backgrounded many times during it's lifetime. In addition, it is best practice to ensure that applications
can function properly under poor network conditions, or even when the device has no network connection. In order to support these characteristics, the SPLYT SDK is designed to
protect your telemetry in these situations. However, there's a small bit that needs to be done by the implementor.

When the application is backgrounded, call <code>[pause](@ref Splyt::Core::pause)</code>:
~~~{.cs}
Core.pause();
~~~

And then when it is foregrounded, call <code>[resume](@ref Splyt::Core::resume)</code>:
~~~{.cs}
Core.resume();
~~~

If necessary, you can still report telemetry while SPLYT is in a paused state, but the telemetry calls may execute more slowly, due to data being read and written from device local storage.