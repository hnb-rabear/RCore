/**
 * Editor window to test and monitor WebRequestHelper functionality.
 * Provides buttons to trigger each API call and displays live results.
 */

using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	public class WebRequestHelperTestWindow : EditorWindow
	{
		private Vector2 m_ScrollPos;
		private bool m_AutoRefresh = true;
		private string m_CustomUrl = "";
		private string m_CustomUrlResult = "";
		private bool m_CustomUrlRequesting;
		private string m_StatusLog = "";

		[MenuItem("RCore/Tools/WebRequestHelper Tester")]
		public static void ShowWindow()
		{
			var window = GetWindow<WebRequestHelperTestWindow>("WebRequest Tester");
			window.minSize = new Vector2(420, 500);
		}

		private void OnEnable()
		{
			WebRequestHelper.OnlineStatusChanged += OnOnlineStatusChanged;
			EditorApplication.update += OnEditorUpdate;
		}

		private void OnDisable()
		{
			WebRequestHelper.OnlineStatusChanged -= OnOnlineStatusChanged;
			EditorApplication.update -= OnEditorUpdate;
		}

		private void OnOnlineStatusChanged(bool isOnline)
		{
			AppendLog($"[Event] OnlineStatusChanged → {isOnline}");
			Repaint();
		}

		private void OnEditorUpdate()
		{
			if (m_AutoRefresh)
				Repaint();
		}

		private void OnGUI()
		{
			m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

			// ── Header ──
			EditorGUILayout.LabelField("WebRequestHelper Tester", EditorStyles.boldLabel);
			EditorGUILayout.Space(4);

			// ── Online Status ──
			DrawSection("Connection Status", () =>
			{
				var isOnline = WebRequestHelper.IsOnline;
				var color = isOnline ? Color.green : Color.red;
				var prevBg = GUI.backgroundColor;
				GUI.backgroundColor = color;
				EditorGUILayout.LabelField("IsOnline", isOnline ? "✅ ONLINE" : "❌ OFFLINE", EditorStyles.helpBox);
				GUI.backgroundColor = prevBg;

				EditorGUILayout.LabelField("Internet Reachability", Application.internetReachability.ToString());
			});

			EditorGUILayout.Space(4);

			// ── Server Time ──
			DrawSection("Server Time", () =>
			{
				var serverTimeUtc = WebRequestHelper.GetServerTimeUtc();
				var serverTimestamp = WebRequestHelper.GetServerTimestampUtc();

				EditorGUILayout.LabelField("Server Time (UTC)",
					serverTimeUtc.HasValue ? serverTimeUtc.Value.ToString("yyyy-MM-dd HH:mm:ss") : "(not fetched)");
				EditorGUILayout.LabelField("Server Timestamp",
					serverTimestamp.HasValue ? serverTimestamp.Value.ToString() : "(not fetched)");
				EditorGUILayout.LabelField("Local DateTime.UtcNow", System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

				if (serverTimeUtc.HasValue)
				{
					var diff = serverTimeUtc.Value - System.DateTime.UtcNow;
					EditorGUILayout.LabelField("Drift (server - local)",
						$"{diff.TotalSeconds:F2}s");
				}

				EditorGUILayout.Space(2);

				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("Request UTC Time (WorldTimeAPI)", GUILayout.Height(28)))
				{
					AppendLog("[Action] RequestUtcTime(renew: true)...");
					WebRequestHelper.RequestUtcTime(true);
				}
				if (GUILayout.Button("Request FC Time", GUILayout.Height(28)))
				{
					AppendLog("[Action] RequestFCTime(renew: true)...");
					WebRequestHelper.RequestFCTime(true);
				}
				EditorGUILayout.EndHorizontal();
			});

			EditorGUILayout.Space(4);

			// ── IP Info ──
			DrawSection("IP Info", () =>
			{
				var ip = WebRequestHelper.ipInfo;
				bool hasFetched = !string.IsNullOrEmpty(ip.ip);

				EditorGUILayout.LabelField("IP", hasFetched ? ip.ip : "(not fetched)");
				EditorGUILayout.LabelField("City", hasFetched ? ip.city : "—");
				EditorGUILayout.LabelField("Region", hasFetched ? ip.region : "—");
				EditorGUILayout.LabelField("Country", hasFetched ? ip.country : "—");
				EditorGUILayout.LabelField("Timezone", hasFetched ? ip.timezone : "—");

				EditorGUILayout.Space(2);
				if (GUILayout.Button("Request IP Info", GUILayout.Height(28)))
				{
					AppendLog("[Action] RequestIpInfo()...");
					WebRequestHelper.RequestIpInfo();
				}
			});

			EditorGUILayout.Space(4);

			// ── Custom URL Test ──
			DrawSection("Custom URL Test", () =>
			{
				EditorGUILayout.LabelField("Send a GET request to any URL:");
				m_CustomUrl = EditorGUILayout.TextField("URL", m_CustomUrl);

				EditorGUI.BeginDisabledGroup(m_CustomUrlRequesting || string.IsNullOrWhiteSpace(m_CustomUrl));
				if (GUILayout.Button(m_CustomUrlRequesting ? "Requesting..." : "Send GET Request", GUILayout.Height(28)))
				{
					SendCustomRequest(m_CustomUrl);
				}
				EditorGUI.EndDisabledGroup();

				if (!string.IsNullOrEmpty(m_CustomUrlResult))
				{
					EditorGUILayout.LabelField("Response:", EditorStyles.boldLabel);
					EditorGUILayout.TextArea(m_CustomUrlResult, GUILayout.MaxHeight(120));
				}
			});

			EditorGUILayout.Space(4);

			// ── API Endpoints Reference ──
			DrawSection("API Endpoints", () =>
			{
				EditorGUILayout.SelectableLabel($"WorldTimeAPI:  {WebRequestHelper.WORLD_TIME_API}", EditorStyles.miniLabel, GUILayout.Height(16));
				EditorGUILayout.SelectableLabel($"FC Time API:   {WebRequestHelper.FC_TIME_API}", EditorStyles.miniLabel, GUILayout.Height(16));
				EditorGUILayout.SelectableLabel($"IP Info:       https://ipinfo.io/json", EditorStyles.miniLabel, GUILayout.Height(16));
			});

			EditorGUILayout.Space(4);

			// ── Log ──
			DrawSection("Event Log", () =>
			{
				EditorGUILayout.TextArea(string.IsNullOrEmpty(m_StatusLog) ? "(no events yet)" : m_StatusLog,
					GUILayout.MinHeight(80), GUILayout.MaxHeight(160));
				if (GUILayout.Button("Clear Log", GUILayout.Height(22)))
					m_StatusLog = "";
			});

			EditorGUILayout.Space(4);

			// ── Settings ──
			m_AutoRefresh = EditorGUILayout.Toggle("Auto Refresh Display", m_AutoRefresh);

			EditorGUILayout.EndScrollView();
		}

		private void DrawSection(string title, System.Action drawContent)
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			drawContent();
			EditorGUI.indentLevel--;
			EditorGUILayout.EndVertical();
		}

		private async void SendCustomRequest(string url)
		{
			m_CustomUrlRequesting = true;
			m_CustomUrlResult = "Requesting...";
			AppendLog($"[Custom] GET {url}");
			Repaint();

			try
			{
				using var request = UnityEngine.Networking.UnityWebRequest.Get(url);
				await request.SendWebRequest();

				if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
				{
					m_CustomUrlResult = $"[{request.responseCode}] ({request.downloadHandler.data.Length} bytes)\n\n{TruncateText(request.downloadHandler.text, 2000)}";
					AppendLog($"[Custom] ✅ {request.responseCode} — {request.downloadHandler.data.Length} bytes");
				}
				else
				{
					m_CustomUrlResult = $"[Error] {request.error}\nResponse Code: {request.responseCode}";
					AppendLog($"[Custom] ❌ {request.error}");
				}
			}
			catch (System.Exception ex)
			{
				m_CustomUrlResult = $"[Exception] {ex.Message}";
				AppendLog($"[Custom] ❌ Exception: {ex.Message}");
			}
			finally
			{
				m_CustomUrlRequesting = false;
				Repaint();
			}
		}

		private void AppendLog(string message)
		{
			string time = System.DateTime.Now.ToString("HH:mm:ss");
			m_StatusLog = $"[{time}] {message}\n{m_StatusLog}";
			// Keep log from growing too large
			if (m_StatusLog.Length > 5000)
				m_StatusLog = m_StatusLog.Substring(0, 5000);
		}

		private static string TruncateText(string text, int maxLength)
		{
			if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
				return text;
			return text.Substring(0, maxLength) + "\n\n... (truncated)";
		}
	}
}
