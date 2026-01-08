using Cysharp.Threading.Tasks;
using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RCore
{
	/// <summary>
	/// A struct to hold information about the user's IP address and location.
	/// </summary>
	[Serializable]
	public struct IPInfo
	{
		public string ip;
		public string city;
		public string region;
		public string country;
		public string timezone;
	}

	/// <summary>
	/// A static helper class for making common web requests, such as fetching the current UTC time
	/// from a server and retrieving the user's IP information. It also manages the application's
	/// online status.
	/// </summary>
	public static class WebRequestHelper
	{
		public const string WORLD_TIME_API = "https://worldtimeapi.org/api/timezone/Etc/UTC";
		public const string FC_TIME_API = "https://farmcityer.com/gettime.php";
		
		/// <summary>
		/// An action that is invoked whenever the online status changes.
		/// </summary>
		public static Action<bool> OnlineStatusChanged;
		private static int m_ServerTimestamp;
		private static bool m_RequestingServerTime;
		private static float m_GetServerTimeAt;
		private static DateTime m_ServerTime;
		private static bool m_IsOnline;
		
		/// <summary>
		/// Gets a value indicating whether the application is considered to be online.
		/// This is based on successful web requests and the device's internet reachability status.
		/// </summary>
		public static bool IsOnline => m_IsOnline && Application.internetReachability != NetworkReachability.NotReachable;
		
		/// <summary>
		/// Stores the retrieved IP and location information.
		/// </summary>
		public static IPInfo ipInfo;
		private static bool m_RequestingOnlineState;
		private static bool m_RequestingIpInfo;
		private static int m_ConnectionChecks = 2;

		/// <summary>
		/// Sends a web request to ipinfo.io to get the user's public IP address and location data.
		/// The result is stored in the static `ipInfo` field.
		/// </summary>
		public static async void RequestIpInfo()
		{
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				SetOnlineStatus(false);
				return;
			}
			if (m_RequestingIpInfo || !string.IsNullOrEmpty(ipInfo.ip))
				return;
				
			const string uri = "https://ipinfo.io/json";
			using var w = UnityWebRequest.Get(uri);
			m_RequestingIpInfo = true;
			await w.SendWebRequest();
			m_RequestingIpInfo = false;
			bool requestSuccess = w.result == UnityWebRequest.Result.Success;
			if (!requestSuccess)
				Debug.LogError(w.error);
			else
			{
				SetOnlineStatus(true);
				ipInfo = JsonUtility.FromJson<IPInfo>(w.downloadHandler.text);
				m_ConnectionChecks--;
			}
		}

		/// <summary>
		/// Sends a web request to a custom time server to get the current time as a Unix timestamp.
		/// This helps in getting a reliable server time that is not dependent on the user's device clock.
		/// </summary>
		/// <param name="renew">If true, forces a new request even if time has been fetched recently.</param>
		public static async void RequestFCTime(bool renew = false)
		{
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				SetOnlineStatus(false);
				return;
			}
			if (m_RequestingServerTime || (m_GetServerTimeAt > 0 && !renew))
				return;
				
			m_RequestingServerTime = true;
			var request = await UnityWebRequest.Get(FC_TIME_API).SendWebRequest();
			m_RequestingServerTime = false;
			if (request.result == UnityWebRequest.Result.Success)
			{
				if (request.responseCode == 200)
				{
					SetOnlineStatus(true);
					var text = request.downloadHandler.text;
					if (int.TryParse(text, out int timestamp))
					{
						m_ServerTimestamp = timestamp;
						m_ServerTime = TimeHelper.UnixTimestampToDateTime(timestamp);
						m_GetServerTimeAt = Time.unscaledTime;
						m_ConnectionChecks--;
					}
				}
			}
		}
		
		/// <summary>
		/// Sends a web request to the WorldTimeAPI to get the current UTC time.
		/// </summary>
		/// <param name="renew">If true, forces a new request even if time has been fetched recently.</param>
		public static async void RequestUtcTime(bool renew = false)
		{
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				SetOnlineStatus(false);
				return;
			}
			if (m_RequestingServerTime || (m_GetServerTimeAt > 0 && !renew))
				return;
				
			m_RequestingServerTime = true;
			var request = await UnityWebRequest.Get(WORLD_TIME_API).SendWebRequest();
			m_RequestingServerTime = false;
			if (request.result == UnityWebRequest.Result.Success)
			{
				if (request.responseCode == 200)
				{
					SetOnlineStatus(true);

					var text = request.downloadHandler.text;
					var jsonParse = JSON.Parse(text);
					if (jsonParse != null)
					{
						var timestamp = jsonParse.GetInt("unixtime");
						m_ServerTimestamp = timestamp;
						m_ServerTime = TimeHelper.UnixTimestampToDateTime(timestamp);
						m_GetServerTimeAt = Time.unscaledTime;
					}
				}
			}
		}
		
		/// <summary>
		/// Gets the current server time in UTC. This is calculated by taking the last fetched server time
		/// and adding the local unscaled time that has passed since the fetch.
		/// </summary>
		/// <returns>The current server DateTime in UTC, or null if no server time has been successfully fetched yet.</returns>
		public static DateTime? GetServerTimeUtc()
		{
			if (m_GetServerTimeAt > 0)
				return m_ServerTime.AddSeconds(Time.unscaledTime - m_GetServerTimeAt);
			return null;
		}

		public static int? GetServerTimestampUtc()
		{
			if (m_GetServerTimeAt > 0)
				return m_ServerTimestamp + (int)(Time.unscaledTime - m_GetServerTimeAt);
			return null;
		}
		
		/// <summary>
		/// An async loop that periodically checks for an internet connection at the start of the application.
		/// </summary>
		private static async UniTaskVoid UpdateInternetStateAsync()
		{
			while (m_ConnectionChecks > 0)
			{
				RequestFCTime();
				RequestIpInfo();
				await UniTask.Delay(3000);
			}
		}

		/// <summary>
		/// This method is automatically called by Unity when the application starts,
		/// initiating the internet state checks.
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Initialize()
		{
			UpdateInternetStateAsync().Forget();
		}

		/// <summary>
		/// Sets the internal online status and invokes the OnlineStatusChanged event if the status has changed.
		/// </summary>
		/// <param name="pIsOnline">The new online status.</param>
		private static void SetOnlineStatus(bool pIsOnline)
		{
			if (m_IsOnline == pIsOnline)
				return;
			m_IsOnline = pIsOnline;
			OnlineStatusChanged?.Invoke(pIsOnline);
		}
	}
}