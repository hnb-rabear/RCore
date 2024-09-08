/***
*  Created by hnim.
*  Copyright (c) 2017 RedAntz. All rights reserved.
*/
using System;

namespace RCore.Common
{
    public class RNative
    {
#if UNITY_EDITOR || (!UNITY_ANDROID && !UNITY_IPHONE)
        // get millis since boot
        public static long getMillisSinceBoot()
        {
            var ticks = System.Diagnostics.Stopwatch.GetTimestamp();
            var uptime = (double)ticks / System.Diagnostics.Stopwatch.Frequency;
            var uptimeSpan = TimeSpan.FromSeconds(uptime);
            //
            double miliSeconds = uptimeSpan.TotalMilliseconds;
            //
            return (long)miliSeconds;
        }
        // check if an app installed
        public static bool isAppInstalled(string pAppPackage)
        {
            return true;
        }
        // get market link of app
        public static string getMarketLink(string pAppPackage)
        {
            return "https://www.google.com.vn/search?q=" + pAppPackage;
        }
        // check device is online
        public static bool isOnline()
        {
            return true;
        }
        public static int getVersionCode()
        {
            return 0;
        }
        public static void showToast(string pMessage)
        {

        }

#elif UNITY_IOS && !UNITY_EDITOR

        [DllImport("__Internal")]
        private static extern long getSecondsSinceBoot();

        // get millis since boot
        public static long getMillisSinceBoot()
        {
            return getSecondsSinceBoot() * 1000;
        }

        // check if an app installed
        public static bool isAppInstalled(string pAppPackage)
        {
            return false;
        }

        // get market link of app
        public static string getMarketLink(string pAppId)
        {
            return "itms-apps://itunes.apple.com/app/id" + pAppId;
        }

        // check device is online
        public static bool isOnline()
        {
            return false;
        }

        public static void showToast(string pMessage)
        {

        }

        public static int getVersionCode()
        {
            return 0;
        }

#elif UNITY_ANDROID && !UNITY_EDITOR

        static bool adr_initialized = false;
        static IntPtr class_RNative = IntPtr.Zero;
        static IntPtr method_isAppInstalled = IntPtr.Zero;
        static IntPtr method_getMarketLink = IntPtr.Zero;
        static IntPtr method_isOnline = IntPtr.Zero;
        static IntPtr method_getBootMillis = IntPtr.Zero;
        static IntPtr method_showToast = IntPtr.Zero;
        static void config()
        {
            if (adr_initialized == false)
            {
                bool success = true;
                IntPtr local_class_RNative = AndroidJNI.FindClass("com/redantz/game/util/RNative");
                if (local_class_RNative != IntPtr.Zero)
                {
                    class_RNative = AndroidJNI.NewGlobalRef(local_class_RNative);
                    AndroidJNI.DeleteLocalRef(local_class_RNative);
                }
                else
                {
                    success = false;
                }
                if (success)
                {
                    method_isAppInstalled = AndroidJNI.GetStaticMethodID(class_RNative, "isAppInstalled", "(Ljava/lang/String;)Z");
                    method_getMarketLink = AndroidJNI.GetStaticMethodID(class_RNative, "getMarketLink", "(Ljava/lang/String;)Ljava/lang/String;");
                    method_isOnline = AndroidJNI.GetStaticMethodID(class_RNative, "isOnline", "()Z");
                    method_getBootMillis = AndroidJNI.GetStaticMethodID(class_RNative, "getMillisSinceBoot2", "()J");
                    method_showToast = AndroidJNI.GetStaticMethodID(class_RNative, "showToast", "(Ljava/lang/String;)V");
                    //
                    AndroidJavaClass class_UnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    var j_activity = class_UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    jvalue[] args = new jvalue[1];
                    args[0].l = j_activity.GetRawObject();
                    AndroidJNI.CallStaticVoidMethod(class_RNative, AndroidJNI.GetStaticMethodID(class_RNative, "config", "(Landroid/content/Context;)V"), args);
                    //
                    adr_initialized = true;
                }
            }
        }

        // check if an app installed
        public static bool isAppInstalled(string pAppPackage)
        {
            config();
            jvalue[] args = new jvalue[1];
            args[0].l = AndroidJNI.NewStringUTF(pAppPackage);
            return AndroidJNI.CallStaticBooleanMethod(class_RNative, method_isAppInstalled, args);
        }

        // get market link of app
        public static string getMarketLink(string pAppPackage)
        {
            config();
            jvalue[] args = new jvalue[1];
            args[0].l = AndroidJNI.NewStringUTF(pAppPackage);
            return AndroidJNI.CallStaticStringMethod(class_RNative, method_getMarketLink, args);
        }

        // check device is online
        public static bool isOnline()
        {
            config();
            jvalue[] args = new jvalue[0];
            return AndroidJNI.CallStaticBooleanMethod(class_RNative, method_isOnline, args);
        }

        // get millis since boot
        public static long getMillisSinceBoot()
        {
            config();
            jvalue[] args = new jvalue[0];
            return AndroidJNI.CallStaticLongMethod(class_RNative, method_getBootMillis, args);
        }

        public static int getVersionCode()
        {
            AndroidJavaClass contextCls = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = contextCls.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject packageMngr = context.Call<AndroidJavaObject>("getPackageManager");
            string packageName = context.Call<string>("getPackageName");
            AndroidJavaObject packageInfo = packageMngr.Call<AndroidJavaObject>("getPackageInfo", packageName, 0);
            return packageInfo.Get<int>("versionCode");
        }

        public static void showToast(string pMessage)
        {
            config();
            jvalue[] args = new jvalue[1];
            args[0].l = AndroidJNI.NewStringUTF(pMessage);
            AndroidJNI.CallStaticVoidMethod(class_RNative, method_showToast, args);
            return;
        }
#endif
    }
}