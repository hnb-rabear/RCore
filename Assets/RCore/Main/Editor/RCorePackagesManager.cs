using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace RCore.Editor
{
	public class RCorePackagesManager : EditorWindow
	{
		[Serializable]
		public class PackageData
		{
			public string displayName;
			public string packageName;
			public string gitUrl;

			public PackageData(string displayName, string packageName, string gitUrl)
			{
				this.displayName = displayName;
				this.packageName = packageName;
				this.gitUrl = gitUrl;
			}
		}

		private static readonly List<PackageData> m_Packages = new List<PackageData>()
		{
			new PackageData("RCore Main", "com.rabear.rcore.main", "https://github.com/hnb-rabear/RCore.git?path=Assets/RCore/Main"),
			new PackageData("UniTask", "com.cysharp.unitask", "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"),
			new PackageData("SheetX", "com.rabear.rcore.sheetx", "https://github.com/hnb-rabear/RCore.git?path=Assets/RCore.SheetX"),
			new PackageData("Ads", "com.rabear.rcore.services.ads", "https://github.com/hnb-rabear/RCore.git?path=Assets/RCore/Services/Ads"),
			new PackageData("Firebase", "com.rabear.rcore.services.firebase", "https://github.com/hnb-rabear/RCore.git?path=Assets/RCore/Services/Firebase"),
			new PackageData("Game Services", "com.rabear.rcore.services.gameservices", "https://github.com/hnb-rabear/RCore.git?path=Assets/RCore/Services/GameServices"),
			new PackageData("IAP", "com.rabear.rcore.services.iap", "https://github.com/hnb-rabear/RCore.git?path=Assets/RCore/Services/IAP"),
			new PackageData("Notification", "com.rabear.rcore.services.notifications", "https://github.com/hnb-rabear/RCore.git?path=Assets/RCore/Services/Notification"),
		};

		private ListRequest m_ListRequest;
		private AddRequest m_AddRequest;
		private RemoveRequest m_RemoveRequest;
		private Dictionary<string, bool> m_InstalledStatus = new Dictionary<string, bool>();
		private bool m_IsLoading;

		[MenuItem("RCore/Packages Manager")]
		public static void ShowWindow()
		{
			var window = GetWindow<RCorePackagesManager>("RCore Packages");
			window.minSize = new Vector2(400, 300);
			window.Show();
		}

		private void OnEnable()
		{
			RefreshPackages();
		}

		private void RefreshPackages()
		{
			m_ListRequest = Client.List(true);
			m_IsLoading = true;
			EditorApplication.update += OnUpdate;
		}

		private void OnUpdate()
		{
			if (m_ListRequest != null)
			{
				if (m_ListRequest.IsCompleted)
				{
					if (m_ListRequest.Status == StatusCode.Success)
					{
						m_InstalledStatus.Clear();
						foreach (var package in m_ListRequest.Result)
						{
							m_InstalledStatus[package.name] = true;
						}
					}
					else if (m_ListRequest.Status >= StatusCode.Failure)
					{
						Debug.LogError($"[RCorePackagesManager] List packages failed: {m_ListRequest.Error?.message ?? "Unknown error"}");
					}

					m_ListRequest = null;
					m_IsLoading = false;
					Repaint();
				}
			}

			if (m_AddRequest != null)
			{
				if (m_AddRequest.IsCompleted)
				{
					if (m_AddRequest.Status == StatusCode.Success)
					{
						Debug.Log($"[RCorePackagesManager] Installed: {m_AddRequest.Result.packageId}");
						RefreshPackages();
					}
					else if (m_AddRequest.Status >= StatusCode.Failure)
					{
						Debug.LogError($"[RCorePackagesManager] Install failed: {m_AddRequest.Error?.message ?? "Unknown error"}");
						m_IsLoading = false;
					}
					m_AddRequest = null;
					Repaint();
				}
			}

			if (m_RemoveRequest != null)
			{
				if (m_RemoveRequest.IsCompleted)
				{
					if (m_RemoveRequest.Status == StatusCode.Success)
					{
						Debug.Log($"[RCorePackagesManager] Uninstalled: {m_RemoveRequest.PackageIdOrName}");
						RefreshPackages();
					}
					else if (m_RemoveRequest.Status >= StatusCode.Failure)
					{
						Debug.LogError($"[RCorePackagesManager] Uninstall failed: {m_RemoveRequest.Error?.message ?? "Unknown error"}");
						m_IsLoading = false;
					}
					m_RemoveRequest = null;
					Repaint();
				}
			}
			
			if (m_ListRequest == null && m_AddRequest == null && m_RemoveRequest == null)
			{
				EditorApplication.update -= OnUpdate;
			}
		}

		private void OnGUI()
		{
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("RCore Packages Manager", EditorStyles.boldLabel);
			EditorGUILayout.Space();

			if (m_IsLoading)
			{
				EditorGUILayout.HelpBox("Loading...", MessageType.Info);
				return;
			}

			foreach (var pkg in m_Packages)
			{
				bool isInstalled = m_InstalledStatus.ContainsKey(pkg.packageName);

				EditorGUILayout.BeginHorizontal("box");
				
				EditorGUILayout.BeginVertical();
				EditorGUILayout.LabelField(pkg.displayName, EditorStyles.boldLabel);
				EditorGUILayout.LabelField(pkg.packageName, EditorStyles.miniLabel);
				EditorGUILayout.EndVertical();

				GUILayout.FlexibleSpace();

				if (isInstalled)
				{
					GUI.backgroundColor = Color.cyan;
					if (GUILayout.Button("Update", GUILayout.Width(80), GUILayout.Height(30)))
					{
						Install(pkg);
					}
					GUI.backgroundColor = Color.red;
					if (GUILayout.Button("Uninstall", GUILayout.Width(80), GUILayout.Height(30)))
					{
						Uninstall(pkg);
					}
					GUI.backgroundColor = Color.white;
				}
				else
				{
					GUI.backgroundColor = Color.green;
					if (GUILayout.Button("Install", GUILayout.Width(80), GUILayout.Height(30)))
					{
						Install(pkg);
					}
					GUI.backgroundColor = Color.white;
				}

				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space(2);
			}
			
			EditorGUILayout.Space();
			if (GUILayout.Button("Refresh"))
			{
				RefreshPackages();
			}
		}

		private void Install(PackageData pkg)
		{
			if (m_AddRequest != null || m_RemoveRequest != null || m_ListRequest != null)
			{
				Debug.LogWarning("Busy...");
				return;
			}

			m_IsLoading = true;
			m_AddRequest = Client.Add(pkg.gitUrl);
			EditorApplication.update += OnUpdate;
		}

		private void Uninstall(PackageData pkg)
		{
			if (m_AddRequest != null || m_RemoveRequest != null || m_ListRequest != null)
			{
				Debug.LogWarning("Busy...");
				return;
			}

			m_IsLoading = true;
			m_RemoveRequest = Client.Remove(pkg.packageName);
			EditorApplication.update += OnUpdate;
		}
	}
}
