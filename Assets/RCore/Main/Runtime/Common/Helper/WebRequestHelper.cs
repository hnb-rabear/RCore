using Cysharp.Threading.Tasks;
using SimpleJSON;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RCore
{
	/// <summary>
	/// A serializable struct to hold geographic and timezone information derived from an IP address.
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
	/// A static helper class for handling common web requests.
	/// It provides utilities for fetching server time, retrieving IP-based location info,
	/// and maintaining a general online status for the application.
	/// </summary>
	public static class WebRequestHelper
	{
		public const string WORLD_TIME_API = "https://worldtimeapi.org/api/timezone/Etc/UTC";
		public const string FC_TIME_API = "https://farmcityer.com/gettime.php";

		/// <summary>
		/// An event that is invoked whenever the application's perceived online status changes.
		/// The boolean parameter is true if online, false if offline.
		/// </summary>
		public static Action<bool> OnlineStatusChanged;

		private static bool m_RequestingServerTime;
		private static float m_GetServerTimeAt;
		private static DateTime m_ServerTime;
		private static bool m_IsOnline;
		
		/// <summary>
		/// Gets a value indicating whether the application is currently online.
		/// This combines the last successful web request status with the current internet reachability reported by Unity.
		/// </summary>
		public static bool IsOnline => m_IsOnline && Application.internetReachability != NetworkReachability.NotReachable;
		
		/// <summary>
		/// Holds the IP information after a successful request to `RequestIpInfo`.
		/// </summary>
		public static IPInfo ipInfo;
		
		private static bool m_RequestingIpInfo;
		// A counter to control how many connection checks are performed at startup.
		private static int m_ConnectionChecks = 2;

		/// <summary>
		/// Asynchronously requests geographic information based on the user's public IP address from ipinfo.io.
		/// The result is stored in the static `ipInfo` field.
		/// </summary>
		public static async void RequestIpInfo()
		{
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				SetOnlineStatus(false);
				return;
			}
			// Prevent redundant requests.
			if (m_RequestingIpInfo || !string.IsNullOrEmpty(ipInfo.ip))
				return;
			
			const string uri = "https://ipinfo.io/json";
			using var w = UnityWebRequest.Get(uri);
			m_RequestingIpInfo = true;
			await w.SendWebRequest();
			m_RequestingIpInfo = false;
			
			bool requestSuccess = w.result == UnityWebRequest.Result.Success;
			if (!requestSuccess)
				Debug.LogError($"WebRequestHelper: Error fetching IP info: {w.error}");
			else
			{
				SetOnlineStatus(true);
				ipInfo = JsonUtility.FromJson<IPInfo>(w.downloadHandler.text);
				m_ConnectionChecks--; // Decrement check counter on success.
			}
		}
		
		/// <summary>
		/// Asynchronously requests the current server time from the custom FC_TIME_API endpoint.
		/// </summary>
		/// <param name="renew">If true, a new request will be sent even if a server time has already been fetched.</param>
		public static async void RequestFCTime(bool renew = false)
		{
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				SetOnlineStatus(false);
				return;
			}
			// Prevent redundant requests unless 'renew' is specified.
			if (m_RequestingServerTime || (m_GetServerTimeAt > 0 && !renew))
				return;
				
			m_RequestingServerTime = true;
			var request = await UnityWebRequest.Get(FC_TIME_API).SendWebRequest();
			m_RequestingServerTime = false;
			
			if (request.result == UnityWebRequest.Result.Success && request.responseCode == 200)
			{
				SetOnlineStatus(true);
				var text = request.downloadHandler.text;
				if (int.TryParse(text, out int timestamp))
				{
					// Store the fetched time and the local time of the fetch.
					m_ServerTime = TimeHelper.UnixTimestampToDateTime(timestamp);
					m_GetServerTimeAt = Time.unscaledTime;
					m_ConnectionChecks--; // Decrement check counter on success.
				}
			}
		}

		/// <summary>
		/// Asynchronously requests the current UTC time from the public WORLD_TIME_API.
		/// </summary>
		/// <param name="renew">If true, a new request will be sent even if a server time has already been fetched.</param>
		public static async void RequestUtcTime(bool renew = false)
		{
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				SetOnlineStatus(false);
				return;
			}
			// Prevent redundant requests unless 'renew' is specified.
			if (m_RequestingServerTime || (m_GetServerTimeAt > 0 && !renew))
				return;
				
			m_RequestingServerTime = true;
			var request = await UnityWebRequest.Get(WORLD_TIME_API).SendWebRequest();
			m_RequestingServerTime = false;
			
			if (request.result == UnityWebRequest.Result.Success && request.responseCode == 200)
			{
				SetOnlineStatus(true);
				var jsonParse = JSON.Parse(request.downloadHandler.text);
				if (jsonParse != null)
				{
					var timestamp = jsonParse.AsObject["unixtime"].AsInt;
					// Store the fetched time and the local time of the fetch.
					m_ServerTime = TimeHelper.UnixTimestampToDateTime(timestamp);
					m_GetServerTimeAt = Time.unscaledTime;
				}
			}
		}

		/// <summary>
		/// Gets the current, estimated UTC server time.
		/// This is calculated by taking the last successfully fetched server time and adding the local unscaled time that has passed since the fetch.
		/// </summary>
		/// <returns>A nullable DateTime representing the current server time. Returns null if no server time has been fetched yet.</returns>
		public static DateTime? GetServerTimeUtc()
		{
			if (m_GetServerTimeAt > 0)
				return m_ServerTime.AddSeconds(Time.unscaledTime - m_GetServerTimeAt);
			return null;
		}

		/// <summary>
		/// An async loop that runs at startup to periodically attempt to establish an internet connection
		/// by fetching time and IP info. It stops after a few successful checks.
		/// </summary>
		private static async UniTaskVoid UpdateInternetStateAsync()
		{
			while (m_ConnectionChecks > 0)
			{
				RequestFCTime();
				RequestIpInfo();
				// Wait for 3 seconds before retrying.
				await UniTask.Delay(3000);
			}
		}

		/// <summary>
		/// This method is automatically called by Unity when the game starts, after the first scene is loaded.
		/// It kicks off the initial internet state check.
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Initialize()
		{
			UpdateInternetStateAsync().Forget();
		}

		/// <summary>
		/// Sets the internal online status and invokes the `OnlineStatusChanged` event if the state has changed.
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