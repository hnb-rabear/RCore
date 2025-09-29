using Newtonsoft.Json;
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
	public interface IRemoteConfig
	{
		Dictionary<string, object> GetDefaultValues();
		void LoadRemoteValues();
	}

	public static class RFirebaseRemote
	{
		private const string BACK_UP_KEY = "RemoteConfig.BackUp";

		public static bool Fetched;
		public static event Action OnFetched;

		private static Dictionary<string, object> m_DefaultData = new();
		private static Dictionary<string, double> m_CacheNumberValues = new();
		private static Dictionary<string, string> m_CacheStringValues = new();
		private static Dictionary<string, bool> m_CacheBoolValues = new();
		private static Dictionary<string, object> m_BackUpValues = new();
		private static bool m_Changed;

		public static void Init(IRemoteConfig remoteConfig)
		{
			Init(remoteConfig.GetDefaultValues(), _ => remoteConfig.LoadRemoteValues());
		}
		public static void Init(Dictionary<string, object> pDefaultData, Action<bool> pOnFetched)
		{
#if FIREBASE_REMOTE_CONFIG
			if (!RFirebase.Initialized)
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
			string key = pKey.ToString();
			if (m_CacheNumberValues.TryGetValue(key, out double numberValue))
				return numberValue;
			var value = FirebaseRemoteConfig.DefaultInstance.GetValue(key).DoubleValue;
			m_CacheNumberValues[key] = value;
			BackUp(key, value);
			return value;
#else
            return Convert.ToDouble(m_DefaultData[pKey.ToString()].ToString());
#endif
		}

		public static string GetStringValue(object pKey)
		{
#if FIREBASE_REMOTE_CONFIG
			string key = pKey.ToString();
			if (m_CacheStringValues.TryGetValue(key, out string value))
				return value;
			value = FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue;
			m_CacheStringValues[key] = value;
			BackUp(key, value);
			return value;
#else
            return m_DefaultData[pKey.ToString()].ToString();
#endif
		}

		public static bool GetBoolValue(object pKey)
		{
#if FIREBASE_REMOTE_CONFIG
			string key = pKey.ToString();
			if (m_CacheBoolValues.TryGetValue(key, out bool value))
				return value;
			value = FirebaseRemoteConfig.DefaultInstance.GetValue(key).BooleanValue;
			m_CacheBoolValues[key] = value;
			BackUp(key, value);
			return value;
#else
            return Convert.ToBoolean(m_DefaultData[pKey.ToString()]);
#endif
		}

		public static T GetGenericValue<T>(object pKey)
		{
#if FIREBASE_REMOTE_CONFIG
			string key = pKey.ToString();
			if (m_CacheStringValues.TryGetValue(key, out string serializedValue))
			{
				return GetReturnValue<T>(serializedValue);
			}
			var value = FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue;
			m_CacheStringValues[key] = value;
			BackUp(key, value);
			return GetReturnValue<T>(value);
#else
            return (T)Convert.ChangeType(m_DefaultData[pKey.ToString()], typeof(T));
#endif
		}

		public static T GetReturnValue<T>(string serializedValue)
		{
			if (string.IsNullOrEmpty(serializedValue))
			{
				Debug.LogWarning("Serialized value is null or empty.");
				return default;
			}
			try
			{
				// Json.NET can directly handle primitives, objects, arrays, and lists.
				return JsonConvert.DeserializeObject<T>(serializedValue);
			}
			catch (JsonException jsonEx)
			{
				// Specific exception for JSON parsing errors
				Debug.LogError($"Failed to deserialize JSON to type {typeof(T)}: {jsonEx.Message}");
			}
			catch (Exception ex)
			{
				// General exceptions
				Debug.LogException(ex);
			}
			return default;
		}

		public static T GetObjectValue<T>(object pKey)
		{
			var json = "";
#if FIREBASE_REMOTE_CONFIG
			json = GetStringValue(pKey);
#else
            json = m_DefaultData[pKey.ToString()].ToString();
#endif
			return JsonUtility.FromJson<T>(json);
		}

		private static void SetDefaultData(Dictionary<string, object> pDefaultData)
		{
			var backUpData = PlayerPrefs.GetString(BACK_UP_KEY);
			if (!string.IsNullOrEmpty(backUpData))
			{
				try
				{
					var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(backUpData);
					foreach (var kvp in data)
						pDefaultData[kvp.Key] = kvp.Value; // Override defaultData values with data values
				}
				catch (Exception ex)
				{
					Debug.LogError("Failed to deserialize or merge data: " + ex.Message);
				}
			}
			m_DefaultData = pDefaultData;

#if FIREBASE_REMOTE_CONFIG
			FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(pDefaultData).ContinueWithOnMainThread(task =>
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
		private static void FetchDataAsync(Action<bool> pOnFetched)
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
							Fetched = true;
							OnFetched?.Invoke();
							Debug.Log($"Remote data loaded and ready (last fetch time {info.FetchTime}).");
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

		private static void BackUp(string key, object value)
		{
			if (!m_BackUpValues.TryAdd(key, value))
				m_BackUpValues[key] = value;
			m_Changed = true;
		}

		public static string GetBackUpValue(string key)
		{
			string content = PlayerPrefs.GetString(BACK_UP_KEY);
			if (string.IsNullOrEmpty(content))
				return null;
			var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
			if (data.TryGetValue(key, out object value))
				return value.ToString();
			return null;
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
			UnityEngine.Debug.Log(log);
#endif
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Initialize()
		{
			Application.quitting += OnApplicationQuit;
			Application.focusChanged += OnFocusChanged;
		}

		private static void OnFocusChanged(bool focus)
		{
			if (m_Changed && !focus)
				BackUp();
		}

		private static void OnApplicationQuit()
		{
			if (m_Changed)
				BackUp();
		}

		public static void BackUp()
		{
			string content = JsonConvert.SerializeObject(m_BackUpValues);
			PlayerPrefs.SetString(BACK_UP_KEY, content);
			m_Changed = false;
		}

		public static void RegisterOnFetchedEvent(Action listener)
		{
			if (listener != null)
				OnFetched += listener;
			if (Fetched)
				listener?.Invoke();
		}

		public static void UnregisterOnFetchedEvent(Action listener)
		{
			if (listener != null)
				OnFetched -= listener;
		}
	}
}