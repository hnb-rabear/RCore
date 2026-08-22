using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace RCore.Editor
{
	internal static class RCorePackagesUpdateNotifier
	{
		private const string MUTE_UNTIL_KEY = "RCorePackagesUpdateNotifier_MuteUntil";
		private const string STARTUP_HANDLED_KEY = "RCorePackagesUpdateNotifier_StartupHandled";
		private static readonly TimeSpan CHECK_INTERVAL = TimeSpan.FromHours(6);

		private static ListRequest s_ListRequest;
		private static IReadOnlyCollection<string> s_CheckedPackageNames;

		[InitializeOnLoadMethod]
		private static void ScheduleStartupCheck()
		{
			if (SessionState.GetBool(STARTUP_HANDLED_KEY, false))
				return;

			SessionState.SetBool(STARTUP_HANDLED_KEY, true);
			EditorApplication.update += RunStartupCheck;
		}

		private static void RunStartupCheck()
		{
			EditorApplication.update -= RunStartupCheck;
			CheckForUpdates(false);
		}

		[MenuItem("RCore/Check For Updates")]
		private static void CheckForUpdatesMenuItem()
		{
			CheckForUpdates(true);
		}

		internal static bool ShouldCheck(DateTime now, DateTime? lastCheck, DateTime? muteUntil)
		{
			if (muteUntil.HasValue && muteUntil.Value > now)
				return false;

			return !lastCheck.HasValue || now - lastCheck.Value >= CHECK_INTERVAL;
		}

		internal static void CheckForUpdates(bool forceRemoteCheck)
		{
			var now = DateTime.UtcNow;
			var muteUntil = GetMuteUntil();
			if (muteUntil.HasValue && muteUntil.Value > now)
				return;

			var lastCheck = GetLastCheckTime();
			if (forceRemoteCheck || ShouldCheck(now, lastCheck, null))
			{
				RCorePackagesManager.CheckRemoteVersions(
					RCorePackagesManager.GetUpdateNotificationPackages(),
					refreshedPackageNames =>
					{
						if (refreshedPackageNames.Count > 0)
							EvaluateCachedUpdates(refreshedPackageNames);
					});
			}
			else
			{
				EvaluateCachedUpdates(null);
			}
		}

		private static void EvaluateCachedUpdates(IReadOnlyCollection<string> packageNames)
		{
			if (s_ListRequest != null)
				return;

			s_CheckedPackageNames = packageNames;
			try
			{
				s_ListRequest = Client.List(true);
				EditorApplication.update += OnListRequestUpdate;
			}
			catch
			{
				s_ListRequest = null;
				ProcessUpdates(new Dictionary<string, PackageInfo>());
			}
		}

		private static void OnListRequestUpdate()
		{
			if (s_ListRequest == null || !s_ListRequest.IsCompleted)
				return;

			EditorApplication.update -= OnListRequestUpdate;
			var request = s_ListRequest;
			s_ListRequest = null;

			var installedPackages = new Dictionary<string, PackageInfo>();
			if (request.Status == StatusCode.Success && request.Result != null)
			{
				foreach (var pkg in request.Result)
					installedPackages[pkg.name] = pkg;
			}

			ProcessUpdates(installedPackages);
		}

		private static void ProcessUpdates(Dictionary<string, PackageInfo> installedPackages)
		{
			var packageNames = s_CheckedPackageNames;
			s_CheckedPackageNames = null;

			var updates = new List<UpdateInfo>();
			foreach (var package in RCorePackagesManager.GetUpdateNotificationPackages())
			{
				if (package == null || (packageNames != null && !packageNames.Contains(package.packageName)))
					continue;

				string installedVersion = RCorePackagesManager.GetInstalledVersion(package, installedPackages);
				string remoteVersion = EditorPrefs.GetString(
					RCorePackagesManager.RemoteVersionCachePrefix + package.packageName, null);

				if (RCorePackagesManager.HasUpdate(installedVersion, remoteVersion))
					updates.Add(new UpdateInfo(package, installedVersion, remoteVersion));
			}

			if (updates.Count > 0)
				RCorePackagesUpdatePopup.ShowUpdates(updates);
		}

		private static DateTime? GetMuteUntil()
		{
			return GetUtcDateTime(MUTE_UNTIL_KEY);
		}

		private static DateTime? GetLastCheckTime()
		{
			return GetUtcDateTime(RCorePackagesManager.LastCheckTimeKey);
		}

		private static DateTime? GetUtcDateTime(string key)
		{
			string value = EditorPrefs.GetString(key, null);
			if (string.IsNullOrEmpty(value))
				return null;

			if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
				return dt.ToUniversalTime();

			return null;
		}

		private readonly struct UpdateInfo
		{
			public RCorePackagesManager.PackageData Package { get; }
			public string InstalledVersion { get; }
			public string RemoteVersion { get; }

			public UpdateInfo(RCorePackagesManager.PackageData package, string installedVersion, string remoteVersion)
			{
				Package = package;
				InstalledVersion = installedVersion;
				RemoteVersion = remoteVersion;
			}
		}

		private class RCorePackagesUpdatePopup : EditorWindow
		{
			private static RCorePackagesUpdatePopup s_Instance;

			private List<UpdateInfo> m_Updates = new List<UpdateInfo>();
			private bool m_MuteFor48Hours;
			private bool m_Closed;

			private static void ShowUpdates(List<UpdateInfo> updates)
			{
				if (s_Instance != null)
				{
					s_Instance.SetUpdates(updates);
					s_Instance.Focus();
					return;
				}

				s_Instance = CreateInstance<RCorePackagesUpdatePopup>();
				s_Instance.titleContent = new GUIContent("RCore Updates");
				s_Instance.SetUpdates(updates);
				s_Instance.ShowUtility();
			}

			private void SetUpdates(List<UpdateInfo> updates)
			{
				m_Updates = updates ?? new List<UpdateInfo>();
				var size = new Vector2(420, 160 + 24 * m_Updates.Count);
				minSize = size;
				maxSize = size;
			}

			private void OnGUI()
			{
				EditorGUILayout.Space(8);
				EditorGUILayout.LabelField("RCore Updates Available", EditorStyles.boldLabel);
				EditorGUILayout.Space(4);

				foreach (var update in m_Updates)
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField($"{update.Package.displayName}  v{update.InstalledVersion} → v{update.RemoteVersion}");
					if (GUILayout.Button("Changelog", EditorStyles.miniButton, GUILayout.Width(80)))
						Application.OpenURL(update.Package.changelogUrl);
					EditorGUILayout.EndHorizontal();
				}

				EditorGUILayout.Space(8);
				m_MuteFor48Hours = EditorGUILayout.ToggleLeft("Do not ask again for 48 hours", m_MuteFor48Hours);

				EditorGUILayout.Space(12);
				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("Open RCore Packages Manager"))
				{
					RCorePackagesManager.ShowWindow();
					Close();
				}
				if (GUILayout.Button("Later"))
				{
					Close();
				}
				EditorGUILayout.EndHorizontal();
			}

			private void OnEnable()
			{
				AssemblyReloadEvents.beforeAssemblyReload += CloseBeforeAssemblyReload;
			}

			private void CloseBeforeAssemblyReload()
			{
				m_Closed = true;
				Close();
			}

			private void OnDisable()
			{
				AssemblyReloadEvents.beforeAssemblyReload -= CloseBeforeAssemblyReload;

				if (s_Instance == this)
					s_Instance = null;

				if (m_Closed)
					return;

				m_Closed = true;
				if (m_MuteFor48Hours)
					EditorPrefs.SetString(MUTE_UNTIL_KEY, DateTime.UtcNow.AddHours(48).ToString("o"));
				else
					EditorPrefs.DeleteKey(MUTE_UNTIL_KEY);
			}
		}
	}
}
