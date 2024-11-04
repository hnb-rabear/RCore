using System.Collections.Generic;
using UnityEngine;
#if FIREBASE_REMOTE_CONFIG
using Firebase;
using Firebase.RemoteConfig;
using Firebase.Extensions;
#endif
using System;

namespace RCore.Service
{
    public static class RFirebaseRemote
    {
        public static Dictionary<string, object> defaultData = new Dictionary<string, object>();
        public static bool fetched;

        public static void Initialize(Dictionary<string, object> pDefaultData, Action<bool> pOnFetched)
        {
#if FIREBASE_REMOTE_CONFIG
            if (!FirebaseManager.initialized)
            {
                FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
                {
                    bool success = !task.IsCanceled && !task.IsFaulted;
                    if (success)
                    {
                        SetDefaultData(pDefaultData);
                        FetchDataAsync(pOnFetched);
                    }
                });
            }
            else
            {
                SetDefaultData(pDefaultData);
                FetchDataAsync(pOnFetched);
            }
#else
            SetDefaultData(pDefaultData);
            FetchDataAsync(pOnFetched);
#endif
        }

        /// <summary>
        /// Get the currently loaded data. If fetch has been called, this will be the data fetched from the server. Otherwise, it will be the defaults.
        /// Note: Firebase will cache this between sessions, so even if you haven't called fetch yet, if it was called on a previous run of the program, you will still have data from the last time it was run.
        /// </summary>
        public static double GetNumberValue(object pKey)
        {
#if FIREBASE_REMOTE_CONFIG
            return FirebaseRemoteConfig.DefaultInstance.GetValue(pKey.ToString()).DoubleValue;
#else
            return Convert.ToDouble(defaultData[pKey.ToString()].ToString());
#endif
        }

        public static string GetStringValue(object pKey)
        {
#if FIREBASE_REMOTE_CONFIG
            return FirebaseRemoteConfig.DefaultInstance.GetValue(pKey.ToString()).StringValue;
#else
            return defaultData[pKey.ToString()].ToString();
#endif
        }

        public static bool GetBoolValue(object pKey)
        {
#if FIREBASE_REMOTE_CONFIG
            return FirebaseRemoteConfig.DefaultInstance.GetValue(pKey.ToString()).BooleanValue;
#else
            return Convert.ToBoolean(defaultData[pKey.ToString()]);
#endif
        }

        public static bool HasKey(object pKey)
        {
            return defaultData.ContainsKey(pKey.ToString());
        }

        public static T GetObjectValue<T>(object pKey)
        {
            var json = "";
#if FIREBASE_REMOTE_CONFIG
            try
            {

                json = FirebaseRemoteConfig.DefaultInstance.GetValue(pKey.ToString()).StringValue;
            }
            catch
            {
                json = defaultData[pKey.ToString()].ToString();
            }
#else
            json = defaultData[pKey.ToString()].ToString();
#endif
            return JsonUtility.FromJson<T>(json);
        }

        private static void SetDefaultData(Dictionary<string, object> pDefaultData)
        {
            defaultData = pDefaultData;
#if FIREBASE_REMOTE_CONFIG
            FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaultData).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                    Debug.Log("SetDefaultsAsync canceled.");
                else if (task.IsFaulted)
                    Debug.Log("SetDefaultsAsync encountered an error.");
                else if (task.IsCompleted)
                    Debug.Log("SetDefaultsAsync completed successfully!");
                else
                    Debug.Log("RemoteConfig configured and ready!");
            });
#endif
        }

        /// <summary>
        /// Fetch new data if the current data is older than the provided timespan. 
        /// Otherwise it assumes the data is "recent enough", and does nothing.
        /// By default the timespan is 12 hours, and for production apps, this is a good number. 
        /// For this example though, it's set to a timespan of zero, so that
        /// changes in the console will always show up immediately.
        /// </summary>
        public static void FetchDataAsync(Action<bool> pOnFetched)
        {
#if FIREBASE_REMOTE_CONFIG
            FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero).ContinueWithOnMainThread((task) =>
            {
                if (task.IsCanceled)
                {
                    Debug.Log("Fetch canceled.");
                }
                else if (task.IsFaulted)
                {
                    Debug.Log("Fetch encountered an error.");
                }
                else if (task.IsCompleted)
                {
                    Debug.Log("Fetch completed successfully!");
                }

                var info = FirebaseRemoteConfig.DefaultInstance.Info;
                switch (info.LastFetchStatus)
                {
                    case LastFetchStatus.Success:
                        FirebaseRemoteConfig.DefaultInstance.ActivateAsync().ContinueWithOnMainThread(task2 =>
                        {
                            fetched = true;
                            Debug.Log(string.Format("Remote data loaded and ready (last fetch time {0}).", info.FetchTime));
                        });
                        break;

                    case LastFetchStatus.Failure:
                        switch (info.LastFetchFailureReason)
                        {
                            case FetchFailureReason.Error:
                                Debug.Log("Fetch failed for unknown reason");
                                break;
                            case FetchFailureReason.Throttled:
                                Debug.Log("Fetch throttled until " + info.ThrottledEndTime);
                                break;
                        }
                        break;

                    case LastFetchStatus.Pending:
                        Debug.Log("Latest Fetch call still pending.");
                        break;
                }

                pOnFetched?.Invoke(!task.IsCanceled && !task.IsFaulted);
            });
#else
            pOnFetched?.Invoke(false);
#endif
        }

        public static void LogFetchedData()
        {
#if FIREBASE_REMOTE_CONFIG
            string log = "";
            var result = new Dictionary<string, ConfigValue>();
            var keys = FirebaseRemoteConfig.DefaultInstance.Keys;
            foreach (string key in keys)
            {
                var value = FirebaseRemoteConfig.DefaultInstance.GetValue(key);
                result.Add(key, value);
                log += $"Key:{key}, StringValue:{value.StringValue}, Source:{value.Source}\n";
            }
            Debug.Log(log);
#endif
        }
    }
}