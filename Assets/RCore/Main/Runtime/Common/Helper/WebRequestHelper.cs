using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RCore
{
	[Serializable]
	public struct IPInfo
	{
		public string ip;
		public string city;
		public string region;
		public string country;
		public string timezone;
	}
	
	public static class WebRequestHelper
	{
		private static bool m_RequestingServerTime;
		private static float m_GetServerTimeAt;
		private static DateTime m_ServerTime;
		private static bool m_IsOnline;
		public static bool IsOnline => m_IsOnline && Application.internetReachability != NetworkReachability.NotReachable;
		public static IPInfo ipInfo;
		private static bool m_RequestingOnlineState;
		private static bool m_RequestingIpInfo;
		public static async void RequestIpInfo()
		{
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				m_IsOnline = false;
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
				m_IsOnline = true;
				ipInfo = JsonUtility.FromJson<IPInfo>(w.downloadHandler.text);
			}
		}
		public static async void RequestServerTime(bool renew = false)
		{
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				m_IsOnline = false;
				return;
			}
			if (m_RequestingServerTime || m_GetServerTimeAt > 0 && !renew)
				return;
			const string url = "https://farmcityer.com/gettime.php";
			m_RequestingServerTime = true;
			var request = await UnityWebRequest.Get(url).SendWebRequest();
			m_RequestingServerTime = false;
			if (request.result == UnityWebRequest.Result.Success)
			{
				if (request.responseCode == 200)
				{
					m_IsOnline = true;
					var text = request.downloadHandler.text;
					if (int.TryParse(text, out int timestamp))
					{
						m_ServerTime = TimeHelper.UnixTimestampToDateTime(timestamp);
						m_GetServerTimeAt = Time.unscaledTime;
					}
				}
			}
		}
		public static DateTime? GetServerTimeUtc()
		{
			if (m_GetServerTimeAt > 0)
				return m_ServerTime.AddSeconds(Time.unscaledTime - m_GetServerTimeAt);
			return null;
		}
		public static async void CheckOnline()
		{
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				m_IsOnline = false;
				return;
			}
			if (m_RequestingOnlineState)
				return;
			var url = "https://www.google.com";
			//China banned google, we should check different url
			if (Application.systemLanguage == SystemLanguage.Chinese
			    || Application.systemLanguage == SystemLanguage.ChineseSimplified
			    || Application.systemLanguage == SystemLanguage.ChineseTraditional)
				url = "https://www.baidu.com/";

			m_RequestingOnlineState = true;
			var request = new UnityWebRequest(url);
			m_RequestingOnlineState = false;
			await request.SendWebRequest();
			m_IsOnline = request.error == null;
		}
	}
}