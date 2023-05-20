using UnityEngine;

namespace RCore.Common
{
    public static class Vibration
    {
        public static AndroidJavaClass unityPlayer;
        public static AndroidJavaObject vibrator;
        public static AndroidJavaObject currentActivity;
        public static AndroidJavaClass vibrationEffectClass;
        public static int defaultAmplitude;

        /*
         * "CreateOneShot": One time vibration
         * "CreateWaveForm": Waveform vibration
         * 
         * Vibration Effects class (Android API level 26 or higher)
         * Milliseconds: long: milliseconds to vibrate. Must be positive.
         * Amplitude: int: Strenght of vibration. Between 1-255. (Or default value: -1)
         * Timings: long: If submitting a array of amplitudes, then timings are the duration of each of these amplitudes in millis.
         * Repeat: int: index of where to repeat, -1 for no repeat
         */

        static Vibration()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");

        if (AndroidVersion.SDK_INT >= 26)
        {
            vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
            defaultAmplitude = vibrationEffectClass.GetStatic<int>("DEFAULT_AMPLITUDE");
        }
#endif
        }

        //Works on API > 25
        public static void CreateOneShot(long milliseconds)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        //If Android 8.0 (API 26+) or never use the new vibrationeffects
        if (AndroidVersion.SDK_INT >= 26)
        {
            CreateOneShot(milliseconds, defaultAmplitude);
        }
        else
        {
            OldVibrate(milliseconds);
        }
#elif UNITY_IOS
        //If not android do simple solution for now
        Handheld.Vibrate();
#endif
        }

        public static void CreateOneShot(long milliseconds, int amplitude)
        {

#if UNITY_ANDROID && !UNITY_EDITOR
        //If Android 8.0 (API 26+) or never use the new vibrationeffects
        if (AndroidVersion.SDK_INT >= 26)
        {
            CreateVibrationEffect("createOneShot", new object[] { milliseconds, amplitude });
        }
        else
        {
            OldVibrate(milliseconds);
        }
#elif UNITY_IOS
        //If not android do simple solution for now
        Handheld.Vibrate();
#endif
        }

        //Works on API > 25
        public static void CreateWaveform(long[] timings, int repeat)
        {
            //Amplitude array varies between no vibration and default_vibration up to the number of timings

#if UNITY_ANDROID && !UNITY_EDITOR
        //If Android 8.0 (API 26+) or never use the new vibrationeffects
        if (AndroidVersion.SDK_INT >= 26)
        {
            CreateVibrationEffect("createWaveform", new object[] { timings, repeat });
        }
        else
        {
            OldVibrate(timings, repeat);
        }
#elif UNITY_IOS
        //If not android do simple solution for now
        Handheld.Vibrate();

#endif
        }

        public static void CreateWaveform(long[] timings, int[] amplitudes, int repeat)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        //If Android 8.0 (API 26+) or never use the new vibrationeffects
        if (AndroidVersion.SDK_INT >= 26)
        {
            CreateVibrationEffect("createWaveform", new object[] { timings, amplitudes, repeat });
        }
        else
        {
            OldVibrate(timings, repeat);
        }
#elif UNITY_IOS
        //If not android do simple solution for now
        Handheld.Vibrate();
#endif

        }

        //Handels all new vibration effects
        private static void CreateVibrationEffect(string function, params object[] args)
        {
            AndroidJavaObject vibrationEffect = vibrationEffectClass.CallStatic<AndroidJavaObject>(function, args);
            vibrator.Call("vibrate", vibrationEffect);
        }

        //Handles old vibration effects
        private static void OldVibrate(long milliseconds)
        {
            vibrator.Call("vibrate", milliseconds);
        }
        private static void OldVibrate(long[] pattern, int repeat)
        {
            vibrator.Call("vibrate", pattern, repeat);
        }

        public static bool HasVibrator()
        {
            return vibrator.Call<bool>("hasVibrator");
        }

        public static bool HasAmplituideControl()
        {
            if (AndroidVersion.SDK_INT >= 26)
            {
                return vibrator.Call<bool>("hasAmplitudeControl"); //API 26+ specific
            }
            else
            {
                return false; //If older than 26 then there is no amplitude control at all
            }
        }

        public static void Cancel()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            vibrator.Call("cancel");
#endif
        }
    }
}