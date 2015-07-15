package com.rsb.splyt;

import java.lang.reflect.Type;
import java.util.Map;

import com.unity3d.player.UnityPlayer;

import com.rsb.gson.Gson;
import com.rsb.gson.reflect.TypeToken;
import com.rsb.splyt.Splyt.Core;
import com.rsb.splyt.Splyt.Instrumentation;
import com.rsb.splyt.Splyt.Tuning;

import android.app.Activity;
import android.os.Bundle;
import android.util.Log;

public class SplytWrapper {
    static private Map<String, Object> toDictionary(String string) {
        Gson gson = new Gson();
        Type bagType = new TypeToken<Map<String, Object>>() {}.getType();
        Map<String, Object> dic = gson.fromJson(string, bagType);
        return dic;
    }
    
    static private Splyt.Instrumentation.Transaction toTransaction(String category, String transactionId, String txnProperties) {
        return Instrumentation.Transaction(category, transactionId).setProperties(toDictionary(txnProperties));
    }
    
    static public String splyt_core_getuserid() {
        return Core.getUserId();
    }
    
    static public String splyt_core_getdeviceid() {
        return Core.getDeviceId();
    }
    
    static public void splyt_core_init(Activity activity, String customerId, String userId, String userProperties, String deviceId, String deviceProperties, int reqTimeout, String host, boolean logEnabled, String sdkNamePre, String sdkVersion, String notificationHost, int notificationSmallIcon, boolean notificationAlwaysPost, boolean notificationDisableAutoClear, String callbackObj) {
        Log.i("Unity", "in SplytWrapper.splyt_core_init");
        Core.EntityInfo userInfo = Core.createUserInfo(userId).setProperties(toDictionary(userProperties));
        Core.EntityInfo deviceInfo = Core.createDeviceInfo().overrideId(deviceId).setProperties(toDictionary(deviceProperties));

        Core.InitParams initParams = Core.createInitParams(activity, customerId)
            .setUserInfo(userInfo)
            .setDeviceInfo(deviceInfo)
            .setRequestTimeout(reqTimeout)
            .setHost(host)
            .setLogEnabled(logEnabled)
            .setSDKName(sdkNamePre + "-android")
            .setSDKVersion(sdkVersion);

        final String cbObj = callbackObj;

        initParams.Notification
            .setHost(notificationHost)
            .setSmallIcon(notificationSmallIcon)
            .setAlwaysPost(notificationAlwaysPost)
            .setDisableAutoClear(notificationDisableAutoClear)
            .setReceivedListener(new SplytNotificationReceivedListener() {
                @Override
                public void onReceived(Bundle info, boolean wasLaunchedBy) {
                    android.util.Log.i("Unity", "NotificationListener.onReceived: " + wasLaunchedBy + info);
                    String unityMessage = (wasLaunchedBy ? "1" : "0");
                    String message = info.getString("message");
                    if (message != null) {
                        unityMessage += message;
                    }
                    UnityPlayer.UnitySendMessage(cbObj, "onSplytNotificationReceived", unityMessage);
                }
            });
        
        Log.i("Unity", "calling Splyt.Core.init");
        Splyt.Core.init(initParams, new SplytListener() {
            @Override
            public void onComplete(SplytError error) {
                UnityPlayer.UnitySendMessage(cbObj, "onSplytInitComplete", String.valueOf(error.getValue()));
            }
        });
    }
    
    static public void splyt_core_registeruser(String userId, String userProperties, String callbackObj) {
        Core.EntityInfo userInfo = Core.createUserInfo(userId).setProperties(toDictionary(userProperties));
        
        final String cbObj = callbackObj;
        Core.registerUser(userInfo, new SplytListener() {
            @Override
            public void onComplete(SplytError error) {
                UnityPlayer.UnitySendMessage(cbObj, "onSplytRegisterUserComplete", String.valueOf(error.getValue()));
            }
        });
    }
    
    static public String splyt_core_setactiveuser(String userId) {
        SplytError error = Core.setActiveUser(userId);
        return String.valueOf(error.getValue());
    }

    static public String splyt_core_clearactiveuser() {
        SplytError error = Core.clearActiveUser();
        return String.valueOf(error.getValue());
    }

    static public void splyt_core_pause(Object nullObj) {
        Core.pause();
    }

    static public void splyt_core_resume(Object nullObj) {
        Core.resume();
    }

    static public void splyt_transaction_begin(String category, String transactionId, String txnProperties, double timeout, int timeoutMode) {
        String mode = (timeoutMode == 0) ? "TXN" : "ANY";
        
        toTransaction(category, transactionId, txnProperties).begin(mode, timeout);
    }

    static public void splyt_transaction_update(String category, String transactionId, String txnProperties, int progress) {
        toTransaction(category, transactionId, txnProperties).update(progress);
    }

    static public void splyt_transaction_end(String category, String transactionId, String txnProperties, String result) {
        toTransaction(category, transactionId, txnProperties).end(result);
    }

    static public void splyt_transaction_beginandend(String category, String transactionId, String txnProperties, String result) {
        toTransaction(category, transactionId, txnProperties).beginAndEnd(result);
    }

    static public void splyt_instrumentation_updatedevicestate(String deviceProperties) {
        Instrumentation.updateDeviceState(toDictionary(deviceProperties));
    }

    static public void splyt_instrumentation_updateuserstate(String userProperties) {
        Instrumentation.updateUserState(toDictionary(userProperties));
    }

    static public void splyt_instrumentation_updatecollection(String name, double balance, double balanceModification, boolean isCurrency) {
        Instrumentation.updateCollection(name, balance, balanceModification, isCurrency);
    }

    static public void splyt_tuning_refresh(String callbackObj) {
        final String cbObj = callbackObj;
        Tuning.refresh(new SplytListener() {
            @Override
            public void onComplete(SplytError error) {
                UnityPlayer.UnitySendMessage(cbObj, "onSplytRefreshComplete", String.valueOf(error.getValue()));
            }
        });
    }

    static public String splyt_tuning_getvar(String varName, String defaultValue) {
        return Tuning.getVar(varName, defaultValue);
    }

    static public String splyt_util_getvalidcurrencystring(String currency) {
        return Util.getValidCurrencyString(currency);
    }

}
