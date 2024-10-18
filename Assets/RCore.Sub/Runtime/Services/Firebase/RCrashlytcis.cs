using System;
namespace RCore.Service
{
    public static class RCrashlytics
    {
#if FIREBASE_CRASHLYTICS
        public static void Log(string message)
        {
            Firebase.Crashlytics.Crashlytics.Log(message);
        }
        public static void LogException(Exception exception)
        {
            Firebase.Crashlytics.Crashlytics.LogException(exception);
        }
        public static void SetCustomKey(string key, string value)
        {
            Firebase.Crashlytics.Crashlytics.SetCustomKey(key, value);
        }
        public static void SetUserId(string identifier)
        {
            Firebase.Crashlytics.Crashlytics.SetUserId(identifier);
        }
#else
        public static void Log(string message) { }
        public static void LogException(Exception exception) { }
        public static void SetCustomKey(string key, string value) { }
        public static void SetUserId(string identifier) { }
#endif
    }
}