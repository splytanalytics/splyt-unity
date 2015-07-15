#import <Splyt/Splyt.h>

// By default mono marshals all .Net strings as UTF-8 C style strings, but ObjC wants NSString
NSString* toNSString(const char* string) {
    if(string)
        return [NSString stringWithUTF8String:string];
    else
        return nil;
}

NSDictionary* toDictionary(const char* string) {
    NSError* error;
    id properties = [NSJSONSerialization JSONObjectWithData:[NSData dataWithBytesNoCopy:(void*)string length:strlen(string) freeWhenDone:NO] options:0 error:&error];
    if(properties && [properties isKindOfClass:[NSDictionary class]])
        return properties;
    return nil;
}

SplytTransaction* toTransaction(const char* category, const char* transactionId, const char* txnProperties) {
    return [[Splyt Instrumentation] Transaction:toNSString(category) withId:toNSString(transactionId) andInitBlock:^(SplytTransaction *t) {
        [t setProperties:toDictionary(txnProperties)];
    }];
}

// By default mono string marshaler creates .Net string for returned UTF-8 C string
// and calls free for returned value, thus returned strings should be allocated on heap
char* toHeapString(NSString* str) {
    if(nil == str)
        return NULL;
    
    const char* string = [str UTF8String];
    
    char* res = (char*)malloc(strlen(string) + 1);
	strcpy(res, string);
	return res;
}

@interface SplytInitParams ()
// convince the compiler that I know about these, even though they aren't REALLY exposed
@property NSString* SDKName;
@property NSString* SDKVersion;
@end

char* splyt_core_getuserid() {
    NSString* userId = [[Splyt Core] userId];
    return toHeapString(userId);
}
char* splyt_core_getdeviceid() {
    NSString* deviceId = [[Splyt Core] deviceId];
    return toHeapString(deviceId);
}

void splyt_core_init(const char* customerId, const char* userId, const char* userProperties, const char* deviceId, const char* deviceProperties, int reqTimeout, const char* host, bool logEnabled, const char* sdkNamePre, const char* sdkVersion, const char* notificationHost, bool notificationDisableAutoClear, const char* callbackObj) {
    SplytEntityInfo* userInfo = [SplytEntityInfo createUserInfo:toNSString(userId) withInitBlock:^(SplytEntityInfo *u) {
        [u setProperties:toDictionary(userProperties)];
    }];
    SplytEntityInfo* deviceInfo = [SplytEntityInfo createDeviceInfoWithInitBlock:^(SplytEntityInfo *d) {
        if(deviceId)
            [d overrideId:toNSString(deviceId)];
        
        [d setProperties:toDictionary(deviceProperties)];
    }];

    // need to wrap this in an NSString for the async journey
    NSString* cbStr = toNSString(callbackObj);

    SplytInitParams* initParams = [SplytInitParams createWithCustomerId:toNSString(customerId) andInitBlock:^(SplytInitParams *init) {
        init.userInfo = userInfo;
        init.deviceInfo = deviceInfo;
        init.requestTimeout = reqTimeout;
        init.host = toNSString(host);
        init.logEnabled = logEnabled;
        init.SDKName = [toNSString(sdkNamePre) stringByAppendingString:@"-ios"];
        init.SDKVersion = toNSString(sdkVersion);
        init.notification.host = toNSString(notificationHost);
        init.notification.disableAutoClear = notificationDisableAutoClear;
        init.notification.receivedCallback = ^(NSDictionary* info, BOOL wasLaunchedBy) {
            NSString *message = @"";
            id rawMessage = [info objectForKey:@"message"];
            if ([rawMessage isKindOfClass:[NSString class]]) {
                message = (NSString *)rawMessage;
            }
            NSString *unityMessage = [NSString stringWithFormat:@"%d%@", wasLaunchedBy ? 1 : 0, message];
            UnitySendMessage([cbStr UTF8String], "onSplytNotificationReceived", [unityMessage cString]);
        };
    }];
    
    [[Splyt Core] init:initParams andThen:^(SplytError error) {
        UnitySendMessage([cbStr UTF8String], "onSplytInitComplete", [[@(error) description] UTF8String]);
    }];
}

void splyt_core_registeruser(const char* userId, const char* userProperties, const char* callbackObj) {
    SplytEntityInfo* userInfo = [SplytEntityInfo createUserInfo:toNSString(userId) withInitBlock:^(SplytEntityInfo *u) {
        [u setProperties:toDictionary(userProperties)];
    }];
    
    // need to wrap this in an NSString for the async journey
    NSString* cbStr = toNSString(callbackObj);
    
    [[Splyt Core] registerUser:userInfo andThen:^(SplytError error) {
        UnitySendMessage([cbStr UTF8String], "onSplytRegisterUserComplete", [[@(error) description] UTF8String]);
    }];
}

char* splyt_core_setactiveuser(const char* userId) {
    SplytError error = [[Splyt Core] setActiveUser:toNSString(userId)];
    return toHeapString([@(error) description]);
}

char* splyt_core_clearactiveuser() {
    SplytError error = [[Splyt Core] clearActiveUser];
    return toHeapString([@(error) description]);
}

void splyt_core_pause() {
    [[Splyt Core] pause];
}

void splyt_core_resume() {
    [[Splyt Core] resume];
}

void splyt_transaction_begin(const char* category, const char* transactionId, const char* txnProperties, double timeout, int timeoutMode) {
    SplytTimeoutMode mode = (SplytTimeoutMode) timeoutMode;
    
    [toTransaction(category, transactionId, txnProperties) beginWithTimeout:timeout andMode:mode];
}

void splyt_transaction_update(const char* category, const char* transactionId, const char* txnProperties, int progress) {
    [toTransaction(category, transactionId, txnProperties) updateAtProgress:progress];
}

void splyt_transaction_end(const char* category, const char* transactionId, const char* txnProperties, const char* result) {
    [toTransaction(category, transactionId, txnProperties) endWithResult:toNSString(result)];
}

void splyt_transaction_beginandend(const char* category, const char* transactionId, const char* txnProperties, const char* result) {
    [toTransaction(category, transactionId, txnProperties) beginAndEndWithResult:toNSString(result)];
}

void splyt_instrumentation_updatedevicestate(const char* deviceProperties) {
    [[Splyt Instrumentation] updateDeviceState:toDictionary(deviceProperties)];
}

void splyt_instrumentation_updateuserstate(const char* userProperties) {
    [[Splyt Instrumentation] updateUserState:toDictionary(userProperties)];
}

void splyt_instrumentation_updatecollection(const char* name, double balance, double balanceModification, bool isCurrency) {
    [[Splyt Instrumentation] updateCollection:toNSString(name) toBalance:@(balance) byAdding:@(balanceModification) andTreatAsCurrency:isCurrency];
}

void splyt_tuning_refresh(const char* callbackObj) {
    NSString* cbStr = toNSString(callbackObj);
    [[Splyt Tuning] refreshAndThen:^(SplytError error) {
        UnitySendMessage([cbStr UTF8String], "onSplytRefreshComplete", [[@(error) description] UTF8String]);
    }];
}

char* splyt_tuning_getvar(const char* varName, const char* defaultValue) {
    id val = [[Splyt Tuning] getVar:toNSString(varName) orDefaultTo:toNSString(defaultValue)];
    
    return toHeapString([val description]);
}

// Pull in the "hidden" utility function
NSString* SplytUtil_getValidCurrencyString(NSString* currency);
char* splyt_util_getvalidcurrencystring(const char* currency) {
    return toHeapString(SplytUtil_getValidCurrencyString(toNSString(currency)));
}

