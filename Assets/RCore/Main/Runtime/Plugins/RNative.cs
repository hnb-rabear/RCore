using System;
using UnityEngine;
using System.Runtime.InteropServices;

namespace RCore
{
	public static class RNative
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		static bool adr_initialized = false;
		static IntPtr class_RNative = IntPtr.Zero;
		static IntPtr method_isAppInstalled = IntPtr.Zero;
		static IntPtr method_getMarketLink = IntPtr.Zero;
		static IntPtr method_isOnline = IntPtr.Zero;
		static IntPtr method_getBootMillis = IntPtr.Zero;
		static IntPtr method_showToast = IntPtr.Zero;
		static void config()
		{
			if (adr_initialized)
				return;
			bool success = true;
			var local_class_RNative = AndroidJNI.FindClass("com/redantz/game/util/RNative");
			if (local_class_RNative != IntPtr.Zero)
			{
				class_RNative = AndroidJNI.NewGlobalRef(local_class_RNative);
				AndroidJNI.DeleteLocalRef(local_class_RNative);
			}
			else
			{
				success = false;
			}
			if (!success)
				return;
			method_isAppInstalled = AndroidJNI.GetStaticMethodID(class_RNative, "isAppInstalled", "(Ljava/lang/String;)Z");
			method_getMarketLink = AndroidJNI.GetStaticMethodID(class_RNative, "getMarketLink", "(Ljava/lang/String;)Ljava/lang/String;");
			method_isOnline = AndroidJNI.GetStaticMethodID(class_RNative, "isOnline", "()Z");
			method_getBootMillis = AndroidJNI.GetStaticMethodID(class_RNative, "getMillisSinceBoot2", "()J");
			method_showToast = AndroidJNI.GetStaticMethodID(class_RNative, "showToast", "(Ljava/lang/String;)V");
			//
			var class_UnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			var j_activity = class_UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
			var args = new jvalue[1];
			args[0].l = j_activity.GetRawObject();
			AndroidJNI.CallStaticVoidMethod(class_RNative, AndroidJNI.GetStaticMethodID(class_RNative, "config", "(Landroid/content/Context;)V"), args);
			//
			adr_initialized = true;
		}
		public static bool isAppInstalled(string pAppPackage)
		{
			config();
			var args = new jvalue[1];
			args[0].l = AndroidJNI.NewStringUTF(pAppPackage);
			return AndroidJNI.CallStaticBooleanMethod(class_RNative, method_isAppInstalled, args);
		}
		public static string getMarketLink(string pAppPackage)
		{
			config();
			var args = new jvalue[1];
			args[0].l = AndroidJNI.NewStringUTF(pAppPackage);
			return AndroidJNI.CallStaticStringMethod(class_RNative, method_getMarketLink, args);
		}
		public static bool isOnline()
		{
			config();
			var args = Array.Empty<jvalue>();
			return AndroidJNI.CallStaticBooleanMethod(class_RNative, method_isOnline, args);
		}
		public static long getMillisSinceBoot()
		{
			config();
			var args = Array.Empty<jvalue>();
			return AndroidJNI.CallStaticLongMethod(class_RNative, method_getBootMillis, args);
		}
		public static long getSecondsSinceBoot()
		{
			return getMillisSinceBoot() / 1000;
		}
		public static int getVersionCode()
		{
			var contextCls = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			var context = contextCls.GetStatic<AndroidJavaObject>("currentActivity");
			var packageMngr = context.Call<AndroidJavaObject>("getPackageManager");
			string packageName = context.Call<string>("getPackageName");
			var packageInfo = packageMngr.Call<AndroidJavaObject>("getPackageInfo", packageName, 0);
			return packageInfo.Get<int>("versionCode");
		}
		public static void showToast(string pMessage)
		{
			config();
			var args = new jvalue[1];
			args[0].l = AndroidJNI.NewStringUTF(pMessage);
			AndroidJNI.CallStaticVoidMethod(class_RNative, method_showToast, args);
		}
#elif UNITY_IOS && !UNITY_EDITOR
		[DllImport("__Internal")]
		public static extern long getSecondsSinceBoot();
		public static long getMillisSinceBoot()
		{
			return getSecondsSinceBoot() * 1000;
		}
		public static string getMarketLink(string pAppId)
		{
			return "itms-apps://itunes.apple.com/app/id" + pAppId;
		}
#else
		public static long getSecondsSinceBoot()
		{
			var ticks = System.Diagnostics.Stopwatch.GetTimestamp();
			var uptime = (double)ticks / System.Diagnostics.Stopwatch.Frequency;
			var uptimeSpan = TimeSpan.FromSeconds(uptime);
			double totalSeconds = uptimeSpan.TotalSeconds;
			return (long)totalSeconds;
		}
		public static long getMillisSinceBoot()
		{
			return getSecondsSinceBoot() * 1000;
		}
		public static string getMarketLink(string pAppPackage)
		{
			return "https://www.google.com.vn/search?q=" + pAppPackage;
		}
#endif
	}
}