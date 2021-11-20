using UnityEngine;

namespace RCore.Common
{
    public class AndroidVersion
    {
        static AndroidJavaClass buildVersion;
        static AndroidVersion()
        {
            buildVersion = new AndroidJavaClass("android.os.Build$VERSION");
        }
        public static string BASE_OS { get { return buildVersion.GetStatic<string>("BASE_OS"); } }
        public static string CODENAME { get { return buildVersion.GetStatic<string>("CODENAME"); } }
        public static string INCREMENTAL { get { return buildVersion.GetStatic<string>("INCREMENTAL"); } }
        public static int PREVIEW_SDK_INT { get { return buildVersion.GetStatic<int>("PREVIEW_SDK_INT"); } }
        public static string RELEASE { get { return buildVersion.GetStatic<string>("RELEASE"); } }
        public static string SDK { get { return buildVersion.GetStatic<string>("SDK"); } }
        public static int SDK_INT { get { return buildVersion.GetStatic<int>("SDK_INT"); } }
        public static string SECURITY_PATCH { get { return buildVersion.GetStatic<string>("SECURITY_PATCH"); } }
        public static string ALL_VERSION
        {
            get
            {
                string version = "BASE_OS: " + BASE_OS + "\n";
                version += "CODENAME: " + CODENAME + "\n";
                version += "INCREMENTAL: " + INCREMENTAL + "\n";
                version += "PREVIEW_SDK_INT: " + PREVIEW_SDK_INT + "\n";
                version += "RELEASE: " + RELEASE + "\n";
                version += "SDK: " + SDK + "\n";
                version += "SDK_INT: " + SDK_INT + "\n";
                version += "SECURITY_PATCH: " + SECURITY_PATCH;

                return version;
            }
        }
        public static int VERSION_CODE
        {
            get
            {
                var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                var packageMgr = context.Call<AndroidJavaObject>("getPackageManager");
                var packageName = context.Call<string>("getPackageName");
                var packageInfo = packageMgr.Call<AndroidJavaObject>("getPackageInfo", packageName, 0);
                return packageInfo.Get<int>("versionCode");
            }
        }
    }
}