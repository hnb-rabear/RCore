using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace RCore.Editor
{
	public class RCorePackagesManager : EditorWindow
	{
		public enum PackageCategory
		{
			Core,
			Services,
			Tools
		}

		/// <summary>
		/// How the package is installed in the current project
		/// </summary>
		public enum InstallMode
		{
			NotInstalled,
			Source,   // Code lives directly in Assets/ (source/dev project)
			Embedded, // Package folder in Packages/ (embedded)
			Git,      // Installed via git URL in manifest
			Local,    // Installed via file: reference
			Registry  // From Unity registry
		}

		[Serializable]
		public class PackageData
		{
			public string displayName;
			public string packageName;
			public string gitUrl;
			public PackageCategory category;
			public string[] dependencies;
			public string description;
			public string documentationUrl;
			public string changelogUrl;
			/// <summary>
			/// Relative path under Assets/ to detect source mode (e.g. "RCore/Main")
			/// </summary>
			public string localAssetsPath;
			/// <summary>
			/// Raw GitHub URL to package.json for remote version checking
			/// </summary>
			public string remotePackageJsonUrl;

			public PackageData(string displayName, string packageName, string gitUrl,
				PackageCategory category = PackageCategory.Core, string[] dependencies = null,
				string localAssetsPath = null, string description = null,
				string documentationUrl = null, string changelogUrl = null,
				string remotePackageJsonUrl = null)
			{
				this.displayName = displayName;
				this.packageName = packageName;
				this.gitUrl = gitUrl;
				this.category = category;
				this.dependencies = dependencies ?? Array.Empty<string>();
				this.localAssetsPath = localAssetsPath;
				this.description = description;
				this.documentationUrl = documentationUrl;
				this.changelogUrl = changelogUrl;
				this.remotePackageJsonUrl = remotePackageJsonUrl;
			}
		}

		[Serializable]
		public class SampleData
		{
			public string displayName;
			public string description;
			/// <summary>
			/// Relative path inside Samples~ folder (e.g. "Examples")
			/// </summary>
			public string sampleFolder;
			/// <summary>
			/// Package that owns this sample
			/// </summary>
			public string ownerPackage;

			public SampleData(string displayName, string description, string sampleFolder, string ownerPackage)
			{
				this.displayName = displayName;
				this.description = description;
				this.sampleFolder = sampleFolder;
				this.ownerPackage = ownerPackage;
			}
		}

		/// <summary>
		/// Resolved info about a package's current state
		/// </summary>
		private class ResolvedPackageInfo
		{
			public InstallMode mode;
			public string version;
			public string remoteVersion; // from GitHub, null if not checked
			public PackageInfo upmInfo;

			public bool HasUpdate =>
				!string.IsNullOrEmpty(version) &&
				!string.IsNullOrEmpty(remoteVersion) &&
				version != remoteVersion &&
				CompareVersions(remoteVersion, version) > 0;

			private static int CompareVersions(string a, string b)
			{
				try
				{
					var va = new Version(a);
					var vb = new Version(b);
					return va.CompareTo(vb);
				}
				catch
				{
					return string.Compare(a, b, StringComparison.Ordinal);
				}
			}
		}

		private const string GITHUB_RAW_BASE = "https://raw.githubusercontent.com/hnb-rabear/RCore/main/Assets/";
		private const string CACHE_PREFIX = "RCorePackagesManager_RemoteVersion_";
		private const string CACHE_TIME_KEY = "RCorePackagesManager_LastCheckTime";

		private static readonly List<PackageData> m_Packages = new List<PackageData>()
		{
			// Core
			new PackageData("RCore Main", "com.rabear.rcore.main",
				"https://github.com/hnb-rabear/RCore.git?path=Assets/RCore/Main",
				PackageCategory.Core, null, "RCore/Main",
				"Core framework with Configuration, Audio, Event systems, Module Factory, and Data management.",
				"https://github.com/hnb-rabear/RCore/tree/main/Assets/RCore/Main",
				"https://github.com/hnb-rabear/RCore/blob/main/Assets/RCore/Main/CHANGELOG.md",
				GITHUB_RAW_BASE + "RCore/Main/package.json"),
			new PackageData("UniTask", "com.cysharp.unitask",
				"https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
				PackageCategory.Core, null, null,
				"Efficient async/await integration for Unity. Required by RCore Main.",
				"https://github.com/Cysharp/UniTask"),

			// Tools
			new PackageData("SheetX", "com.rabear.rcore.sheetx",
				"https://github.com/hnb-rabear/RCore.git?path=Assets/RCore.SheetX",
				PackageCategory.Tools,
				new[] { "com.rabear.rcore.main" },
				"RCore.SheetX",
				"Excel/Google Sheets to Unity data pipeline with code generation.",
				"https://github.com/hnb-rabear/RCore/tree/main/Assets/RCore.SheetX",
				"https://github.com/hnb-rabear/RCore/blob/main/Assets/RCore.SheetX/CHANGELOG.md",
				GITHUB_RAW_BASE + "RCore.SheetX/package.json"),

			// Services
			new PackageData("Ads", "com.rabear.rcore.services.ads",
				"https://github.com/hnb-rabear/RCore.git?path=Assets/RCore/Services/Ads",
				PackageCategory.Services,
				new[] { "com.rabear.rcore.main" },
				"RCore/Services/Ads",
				"Unified ads integration supporting AdMob, IronSource, and MAX.",
				"https://github.com/hnb-rabear/RCore/tree/main/Assets/RCore/Services/Ads",
				null,
				GITHUB_RAW_BASE + "RCore/Services/Ads/package.json"),
			new PackageData("Firebase", "com.rabear.rcore.services.firebase",
				"https://github.com/hnb-rabear/RCore.git?path=Assets/RCore/Services/Firebase",
				PackageCategory.Services,
				new[] { "com.rabear.rcore.main" },
				"RCore/Services/Firebase",
				"Firebase integration for Analytics, Remote Config, and Crashlytics.",
				"https://github.com/hnb-rabear/RCore/tree/main/Assets/RCore/Services/Firebase",
				null,
				GITHUB_RAW_BASE + "RCore/Services/Firebase/package.json"),
			new PackageData("Game Services", "com.rabear.rcore.services.gameservices",
				"https://github.com/hnb-rabear/RCore.git?path=Assets/RCore/Services/GameServices",
				PackageCategory.Services,
				new[] { "com.rabear.rcore.main" },
				"RCore/Services/GameServices",
				"Google Play Games / Apple Game Center integration.",
				"https://github.com/hnb-rabear/RCore/tree/main/Assets/RCore/Services/GameServices",
				null,
				GITHUB_RAW_BASE + "RCore/Services/GameServices/package.json"),
			new PackageData("IAP", "com.rabear.rcore.services.iap",
				"https://github.com/hnb-rabear/RCore.git?path=Assets/RCore/Services/IAP",
				PackageCategory.Services,
				new[] { "com.rabear.rcore.main" },
				"RCore/Services/IAP",
				"In-App Purchase wrapper for Unity IAP with receipt validation.",
				"https://github.com/hnb-rabear/RCore/tree/main/Assets/RCore/Services/IAP",
				null,
				GITHUB_RAW_BASE + "RCore/Services/IAP/package.json"),
			new PackageData("Notification", "com.rabear.rcore.services.notifications",
				"https://github.com/hnb-rabear/RCore.git?path=Assets/RCore/Services/Notification",
				PackageCategory.Services,
				new[] { "com.rabear.rcore.main" },
				"RCore/Services/Notification",
				"Local push notification scheduling for iOS and Android.",
				"https://github.com/hnb-rabear/RCore/tree/main/Assets/RCore/Services/Notification",
				null,
				GITHUB_RAW_BASE + "RCore/Services/Notification/package.json"),
		};

		private static readonly List<SampleData> m_Samples = new List<SampleData>()
		{
			new SampleData("General Examples",
				"Animation, DataStorage, Prefab, Scene, and Script examples for RCore.",
				"Examples", "com.rabear.rcore.main"),
			new SampleData("Spine Integration",
				"Helper class for Spine animation integration.",
				"SpineIntegration", "com.rabear.rcore.main"),
		};

		private const string SAMPLES_IMPORT_ROOT = "Assets/RCore.Main.Samples";

		private ListRequest m_ListRequest;
		private AddRequest m_AddRequest;
		private RemoveRequest m_RemoveRequest;
		private Dictionary<string, PackageInfo> m_InstalledPackages = new Dictionary<string, PackageInfo>();
		private Dictionary<string, ResolvedPackageInfo> m_ResolvedInfos = new Dictionary<string, ResolvedPackageInfo>();
		private bool m_IsLoading;
		private string m_StatusMessage = "";
		private Vector2 m_ScrollPosition;
		private Queue<PackageData> m_InstallQueue = new Queue<PackageData>();

		// Remote version check
		private bool m_IsCheckingRemote;
		private int m_RemoteChecksPending;
		private List<UnityWebRequestAsyncOperation> m_PendingWebRequests = new List<UnityWebRequestAsyncOperation>();

		// UI state
		private HashSet<string> m_ExpandedPackages = new HashSet<string>();

		// Styles
		private GUIStyle m_HeaderStyle;
		private GUIStyle m_VersionStyle;
		private GUIStyle m_CategoryStyle;
		private GUIStyle m_BadgeSourceStyle;
		private GUIStyle m_BadgeGitStyle;
		private GUIStyle m_BadgeEmbeddedStyle;
		private GUIStyle m_BadgeLocalStyle;
		private GUIStyle m_UpdateBadgeStyle;
		private GUIStyle m_DescriptionStyle;
		private GUIStyle m_LinkStyle;
		private bool m_StylesInitialized;

		private static readonly Vector2 WINDOW_SIZE = new Vector2(520, 700);

		[MenuItem("RCore/Packages Manager")]
		public static void ShowWindow()
		{
			var window = GetWindow<RCorePackagesManager>(true, "RCore Packages");
			window.minSize = WINDOW_SIZE;
			window.maxSize = WINDOW_SIZE;
			window.Show();
		}

		private void InitStyles()
		{
			if (m_StylesInitialized) return;

			m_HeaderStyle = new GUIStyle(EditorStyles.boldLabel)
			{
				fontSize = 14,
				margin = new RectOffset(4, 4, 8, 4)
			};

			m_VersionStyle = new GUIStyle(EditorStyles.miniLabel)
			{
				normal = { textColor = new Color(0.4f, 0.7f, 1f) },
				fontStyle = FontStyle.Bold
			};

			m_CategoryStyle = new GUIStyle(EditorStyles.boldLabel)
			{
				fontSize = 11,
				normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
				margin = new RectOffset(4, 4, 10, 2)
			};

			m_BadgeSourceStyle = CreateBadgeStyle(new Color(1f, 0.75f, 0.2f));
			m_BadgeGitStyle = CreateBadgeStyle(new Color(0.4f, 0.8f, 1f));
			m_BadgeEmbeddedStyle = CreateBadgeStyle(new Color(0.6f, 1f, 0.6f));
			m_BadgeLocalStyle = CreateBadgeStyle(new Color(0.8f, 0.6f, 1f));

			m_UpdateBadgeStyle = new GUIStyle(EditorStyles.miniLabel)
			{
				normal = { textColor = new Color(1f, 0.9f, 0.2f) },
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleLeft
			};

			m_DescriptionStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
			{
				fontSize = 11,
				normal = { textColor = new Color(0.65f, 0.65f, 0.65f) },
				margin = new RectOffset(8, 8, 2, 4)
			};

			m_LinkStyle = new GUIStyle(EditorStyles.miniLabel)
			{
				normal = { textColor = new Color(0.4f, 0.7f, 1f) },
				hover = { textColor = new Color(0.6f, 0.85f, 1f) },
				margin = new RectOffset(8, 4, 0, 2)
			};

			m_StylesInitialized = true;
		}

		private static GUIStyle CreateBadgeStyle(Color textColor)
		{
			return new GUIStyle(EditorStyles.miniLabel)
			{
				normal = { textColor = textColor },
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.MiddleCenter
			};
		}

		private void OnEnable()
		{
			minSize = WINDOW_SIZE;
			maxSize = WINDOW_SIZE;
			LoadCachedRemoteVersions();
			RefreshPackages();
		}

		private void OnDisable()
		{
			// Clean up pending web requests
			foreach (var op in m_PendingWebRequests)
			{
				if (op?.webRequest != null && !op.isDone)
					op.webRequest.Abort();
			}
			m_PendingWebRequests.Clear();
		}

		private void RefreshPackages()
		{
			m_ListRequest = Client.List(true);
			m_IsLoading = true;
			m_StatusMessage = "Fetching installed packages...";
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
						m_InstalledPackages.Clear();
						foreach (var package in m_ListRequest.Result)
						{
							m_InstalledPackages[package.name] = package;
						}
						ResolveAllPackages();
					}
					else if (m_ListRequest.Status >= StatusCode.Failure)
					{
						Debug.LogError($"[RCorePackagesManager] List packages failed: {m_ListRequest.Error?.message ?? "Unknown error"}");
					}

					m_ListRequest = null;
					m_IsLoading = m_AddRequest != null || m_RemoveRequest != null;
					if (!m_IsLoading) m_StatusMessage = "";
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
						ProcessNextInQueue();
					}
					else if (m_AddRequest.Status >= StatusCode.Failure)
					{
						Debug.LogError($"[RCorePackagesManager] Install failed: {m_AddRequest.Error?.message ?? "Unknown error"}");
						m_InstallQueue.Clear();
						m_IsLoading = false;
						m_StatusMessage = "";
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
						m_StatusMessage = "";
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

		#region Resolve Package Info

		private void ResolveAllPackages()
		{
			m_ResolvedInfos.Clear();
			foreach (var pkg in m_Packages)
			{
				m_ResolvedInfos[pkg.packageName] = ResolvePackage(pkg);
			}
		}

		private ResolvedPackageInfo ResolvePackage(PackageData pkg)
		{
			var info = new ResolvedPackageInfo();

			// Load cached remote version
			info.remoteVersion = EditorPrefs.GetString(CACHE_PREFIX + pkg.packageName, null);
			if (string.IsNullOrEmpty(info.remoteVersion))
				info.remoteVersion = null;

			// 1. Check UPM first (covers Git, Embedded, Local, Registry)
			if (m_InstalledPackages.TryGetValue(pkg.packageName, out var upmInfo))
			{
				info.upmInfo = upmInfo;
				info.version = upmInfo.version;
				info.mode = ResolveUpmSource(upmInfo);
				return info;
			}

			// 2. Check local Assets/ path (source project mode)
			if (!string.IsNullOrEmpty(pkg.localAssetsPath))
			{
				string localPackageJson = Path.Combine(Application.dataPath, pkg.localAssetsPath, "package.json");
				if (File.Exists(localPackageJson))
				{
					info.mode = InstallMode.Source;
					info.version = ReadVersionFromPackageJson(localPackageJson);
					return info;
				}
			}

			// 3. Not installed
			info.mode = InstallMode.NotInstalled;
			info.version = null;
			return info;
		}

		private static InstallMode ResolveUpmSource(PackageInfo upmInfo)
		{
			switch (upmInfo.source)
			{
				case PackageSource.Git: return InstallMode.Git;
				case PackageSource.Embedded: return InstallMode.Embedded;
				case PackageSource.Local: return InstallMode.Local;
				case PackageSource.Registry: return InstallMode.Registry;
				default: return InstallMode.Git;
			}
		}

		private static string ReadVersionFromPackageJson(string path)
		{
			try
			{
				string json = File.ReadAllText(path);
				var wrapper = JsonUtility.FromJson<PackageJsonVersion>(json);
				return wrapper?.version;
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[RCorePackagesManager] Failed to read {path}: {e.Message}");
				return null;
			}
		}

		[Serializable]
		private class PackageJsonVersion
		{
			public string version;
		}

		#endregion

		#region Remote Version Check

		private void CheckRemoteVersions()
		{
			if (m_IsCheckingRemote) return;

			m_IsCheckingRemote = true;
			m_RemoteChecksPending = 0;
			m_PendingWebRequests.Clear();

			foreach (var pkg in m_Packages)
			{
				if (string.IsNullOrEmpty(pkg.remotePackageJsonUrl)) continue;

				m_RemoteChecksPending++;
				var request = UnityWebRequest.Get(pkg.remotePackageJsonUrl);
				var op = request.SendWebRequest();
				m_PendingWebRequests.Add(op);

				// Capture packageName for closure
				string packageName = pkg.packageName;
				op.completed += _ => OnRemoteVersionReceived(packageName, request);
			}

			if (m_RemoteChecksPending == 0)
			{
				m_IsCheckingRemote = false;
			}
			else
			{
				m_StatusMessage = $"Checking for updates... ({m_RemoteChecksPending} packages)";
			}
		}

		private void OnRemoteVersionReceived(string packageName, UnityWebRequest request)
		{
			m_RemoteChecksPending--;

			if (request.result == UnityWebRequest.Result.Success)
			{
				try
				{
					var wrapper = JsonUtility.FromJson<PackageJsonVersion>(request.downloadHandler.text);
					if (!string.IsNullOrEmpty(wrapper?.version))
					{
						// Cache to EditorPrefs
						EditorPrefs.SetString(CACHE_PREFIX + packageName, wrapper.version);

						// Update resolved info
						if (m_ResolvedInfos.TryGetValue(packageName, out var info))
						{
							info.remoteVersion = wrapper.version;
						}
					}
				}
				catch (Exception e)
				{
					Debug.LogWarning($"[RCorePackagesManager] Failed to parse remote version for {packageName}: {e.Message}");
				}
			}

			request.Dispose();

			if (m_RemoteChecksPending <= 0)
			{
				m_IsCheckingRemote = false;
				m_StatusMessage = "";
				EditorPrefs.SetString(CACHE_TIME_KEY, DateTime.UtcNow.ToString("o"));
				Repaint();
			}
		}

		private void LoadCachedRemoteVersions()
		{
			foreach (var pkg in m_Packages)
			{
				string cached = EditorPrefs.GetString(CACHE_PREFIX + pkg.packageName, null);
				if (!string.IsNullOrEmpty(cached) && m_ResolvedInfos.TryGetValue(pkg.packageName, out var info))
				{
					info.remoteVersion = cached;
				}
			}
		}

		private string GetLastCheckTimeDisplay()
		{
			string timeStr = EditorPrefs.GetString(CACHE_TIME_KEY, null);
			if (string.IsNullOrEmpty(timeStr)) return "Never";

			try
			{
				var lastCheck = DateTime.Parse(timeStr).ToLocalTime();
				var elapsed = DateTime.Now - lastCheck;
				if (elapsed.TotalMinutes < 1) return "Just now";
				if (elapsed.TotalHours < 1) return $"{(int)elapsed.TotalMinutes}m ago";
				if (elapsed.TotalDays < 1) return $"{(int)elapsed.TotalHours}h ago";
				return $"{(int)elapsed.TotalDays}d ago";
			}
			catch
			{
				return "Unknown";
			}
		}

		#endregion

		#region GUI

		private void OnGUI()
		{
			InitStyles();

			EditorGUILayout.Space(4);

			// Header with update count
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("RCore Packages Manager", m_HeaderStyle);
			int updateCount = m_ResolvedInfos.Values.Count(r => r.HasUpdate);
			if (updateCount > 0)
			{
				GUILayout.Label($"⬆ {updateCount} update(s)", m_UpdateBadgeStyle, GUILayout.Width(100));
			}
			EditorGUILayout.EndHorizontal();

			// Status bar
			if (m_IsLoading || m_IsCheckingRemote)
			{
				EditorGUILayout.HelpBox(m_StatusMessage, MessageType.Info);
			}

			// Toolbar
			EditorGUILayout.BeginHorizontal();
			GUI.enabled = !m_IsLoading;

			if (GUILayout.Button("Refresh", GUILayout.Height(24)))
			{
				RefreshPackages();
			}
			if (GUILayout.Button("Check Updates", GUILayout.Height(24)))
			{
				CheckRemoteVersions();
			}
			if (GUILayout.Button("Update All", GUILayout.Height(24)))
			{
				UpdateAllInstalled();
			}

			GUI.enabled = true;
			EditorGUILayout.EndHorizontal();

			// Last check time
			EditorGUILayout.LabelField($"Last checked: {GetLastCheckTimeDisplay()}", EditorStyles.miniLabel);

			EditorGUILayout.Space(2);

			// Package list
			m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

			DrawPackageGroup(PackageCategory.Core, "── Core ──");
			DrawPackageGroup(PackageCategory.Services, "── Services ──");
			DrawPackageGroup(PackageCategory.Tools, "── Tools ──");

			// Samples section
			DrawSamplesSection();

			EditorGUILayout.EndScrollView();
		}

		private void DrawPackageGroup(PackageCategory category, string label)
		{
			var packages = m_Packages.Where(p => p.category == category).ToList();
			if (packages.Count == 0) return;

			EditorGUILayout.LabelField(label, m_CategoryStyle);

			foreach (var pkg in packages)
			{
				DrawPackageRow(pkg);
			}
		}

		private void DrawPackageRow(PackageData pkg)
		{
			m_ResolvedInfos.TryGetValue(pkg.packageName, out var resolved);
			var mode = resolved?.mode ?? InstallMode.NotInstalled;
			string version = resolved?.version;
			bool hasUpdate = resolved?.HasUpdate ?? false;

			// Main row
			EditorGUILayout.BeginHorizontal("box");

			// Package info
			EditorGUILayout.BeginVertical(GUILayout.MinWidth(180));

			// Row 1: Name + Mode badge + Update badge
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(pkg.displayName, EditorStyles.boldLabel, GUILayout.MinWidth(100));
			DrawModeBadge(mode);
			if (hasUpdate)
			{
				GUILayout.Label("⬆ NEW", m_UpdateBadgeStyle, GUILayout.Width(45));
			}
			EditorGUILayout.EndHorizontal();

			// Row 2: Package ID + Version
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(pkg.packageName, EditorStyles.miniLabel, GUILayout.MinWidth(140));
			if (!string.IsNullOrEmpty(version))
			{
				string versionText = hasUpdate
					? $"v{version} → v{resolved.remoteVersion}"
					: $"v{version}";
				EditorGUILayout.LabelField(versionText, m_VersionStyle, GUILayout.MinWidth(80));
			}
			else
			{
				EditorGUILayout.LabelField("—", EditorStyles.miniLabel, GUILayout.Width(60));
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();

			GUILayout.FlexibleSpace();

			// Action buttons (vertically centered)
			EditorGUILayout.BeginVertical();
			GUILayout.FlexibleSpace();
			EditorGUILayout.BeginHorizontal();
			GUI.enabled = !m_IsLoading;
			DrawActionButtons(pkg, mode);
			GUI.enabled = true;
			EditorGUILayout.EndHorizontal();
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndVertical();

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space(1);
		}

		private void DrawDetailPanel(PackageData pkg, ResolvedPackageInfo resolved)
		{
			EditorGUILayout.Space(2);

			// Description
			if (!string.IsNullOrEmpty(pkg.description))
			{
				EditorGUILayout.LabelField(pkg.description, m_DescriptionStyle);
			}

			// Links
			EditorGUILayout.BeginHorizontal();
			if (!string.IsNullOrEmpty(pkg.documentationUrl))
			{
				if (GUILayout.Button("📖 Documentation", m_LinkStyle))
					Application.OpenURL(pkg.documentationUrl);
			}
			if (!string.IsNullOrEmpty(pkg.changelogUrl))
			{
				if (GUILayout.Button("📋 Changelog", m_LinkStyle))
					Application.OpenURL(pkg.changelogUrl);
			}
			if (!string.IsNullOrEmpty(pkg.gitUrl))
			{
				// Extract GitHub repo URL from git URL
				string repoUrl = pkg.gitUrl.Split('?')[0].Replace(".git", "");
				if (GUILayout.Button("🔗 GitHub", m_LinkStyle))
					Application.OpenURL(repoUrl);
			}
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();

			// Version details
			if (resolved != null)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField($"  Installed: {resolved.version ?? "—"}  |  Mode: {resolved.mode}", EditorStyles.miniLabel);
				if (!string.IsNullOrEmpty(resolved.remoteVersion))
				{
					EditorGUILayout.LabelField($"  Latest: v{resolved.remoteVersion}", EditorStyles.miniLabel, GUILayout.Width(120));
				}
				EditorGUILayout.EndHorizontal();
			}

			// Dependencies
			if (pkg.dependencies != null && pkg.dependencies.Length > 0)
			{
				string deps = string.Join(", ", pkg.dependencies.Select(d =>
				{
					var depPkg = m_Packages.FirstOrDefault(p => p.packageName == d);
					return depPkg?.displayName ?? d;
				}));
				EditorGUILayout.LabelField($"  Requires: {deps}", EditorStyles.miniLabel);
			}

			EditorGUILayout.Space(2);
		}

		private void DrawModeBadge(InstallMode mode)
		{
			switch (mode)
			{
				case InstallMode.Source:
					GUILayout.Label("[Source]", m_BadgeSourceStyle, GUILayout.Width(55));
					break;
				case InstallMode.Git:
					GUILayout.Label("[Git]", m_BadgeGitStyle, GUILayout.Width(55));
					break;
				case InstallMode.Embedded:
					GUILayout.Label("[Embed]", m_BadgeEmbeddedStyle, GUILayout.Width(55));
					break;
				case InstallMode.Local:
					GUILayout.Label("[Local]", m_BadgeLocalStyle, GUILayout.Width(55));
					break;
				case InstallMode.Registry:
					GUILayout.Label("[Registry]", m_BadgeGitStyle, GUILayout.Width(55));
					break;
				default:
					GUILayout.Label("", GUILayout.Width(55));
					break;
			}
		}

		private void DrawActionButtons(PackageData pkg, InstallMode mode)
		{
			switch (mode)
			{
				case InstallMode.Source:
					GUI.enabled = false;
					GUILayout.Button("In Assets", GUILayout.Width(70), GUILayout.Height(28));
					GUI.enabled = !m_IsLoading;
					break;

				case InstallMode.Git:
				case InstallMode.Registry:
					GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
					if (GUILayout.Button("Update", GUILayout.Width(70), GUILayout.Height(28)))
						Install(pkg);
					GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
					if (GUILayout.Button("Uninstall", GUILayout.Width(70), GUILayout.Height(28)))
						UninstallWithConfirmation(pkg);
					GUI.backgroundColor = Color.white;
					break;

				case InstallMode.Embedded:
				case InstallMode.Local:
					GUI.enabled = false;
					GUILayout.Button("Embedded", GUILayout.Width(70), GUILayout.Height(28));
					GUI.enabled = !m_IsLoading;
					GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
					if (GUILayout.Button("Uninstall", GUILayout.Width(70), GUILayout.Height(28)))
						UninstallWithConfirmation(pkg);
					GUI.backgroundColor = Color.white;
					break;

				case InstallMode.NotInstalled:
					GUI.backgroundColor = new Color(0.4f, 1f, 0.5f);
					if (GUILayout.Button("Install", GUILayout.Width(70), GUILayout.Height(28)))
						InstallWithDependencyCheck(pkg);
					GUI.backgroundColor = Color.white;
					break;
			}
		}

		#endregion

		#region Install / Uninstall Logic

		private void InstallWithDependencyCheck(PackageData pkg)
		{
			if (IsBusy()) return;

			var missingDeps = GetMissingDependencies(pkg);
			if (missingDeps.Count > 0)
			{
				string depNames = string.Join(", ", missingDeps.Select(d => d.displayName));
				bool installDeps = EditorUtility.DisplayDialog(
					"Missing Dependencies",
					$"\"{pkg.displayName}\" requires: {depNames}\n\nInstall dependencies first?",
					"Install All", "Cancel");

				if (!installDeps) return;

				foreach (var dep in missingDeps)
					m_InstallQueue.Enqueue(dep);
				m_InstallQueue.Enqueue(pkg);
				ProcessNextInQueue();
				return;
			}

			Install(pkg);
		}

		private void UninstallWithConfirmation(PackageData pkg)
		{
			if (IsBusy()) return;

			var dependents = GetDependentPackages(pkg);
			string message = $"Uninstall \"{pkg.displayName}\"?";

			if (dependents.Count > 0)
			{
				string depNames = string.Join(", ", dependents.Select(d => d.displayName));
				message += $"\n\n⚠ Warning: These installed packages depend on it: {depNames}";
			}

			if (EditorUtility.DisplayDialog("Confirm Uninstall", message, "Uninstall", "Cancel"))
			{
				Uninstall(pkg);
			}
		}

		private void UpdateAllInstalled()
		{
			if (IsBusy()) return;

			var updatable = m_Packages.Where(p =>
			{
				m_ResolvedInfos.TryGetValue(p.packageName, out var info);
				return info != null && (info.mode == InstallMode.Git || info.mode == InstallMode.Registry);
			}).ToList();

			if (updatable.Count == 0)
			{
				Debug.Log("[RCorePackagesManager] No updatable packages found.");
				return;
			}

			if (!EditorUtility.DisplayDialog("Update All",
				    $"Update {updatable.Count} package(s)?\n(Only Git/Registry packages will be updated)",
				    "Update All", "Cancel"))
				return;

			foreach (var pkg in updatable)
				m_InstallQueue.Enqueue(pkg);
			ProcessNextInQueue();
		}

		private void ProcessNextInQueue()
		{
			if (m_InstallQueue.Count > 0)
			{
				var next = m_InstallQueue.Dequeue();
				m_StatusMessage = $"Installing {next.displayName}... ({m_InstallQueue.Count} remaining)";
				m_IsLoading = true;
				m_AddRequest = Client.Add(next.gitUrl);
				EditorApplication.update += OnUpdate;
			}
			else
			{
				m_StatusMessage = "";
				RefreshPackages();
			}
		}

		private void Install(PackageData pkg)
		{
			if (IsBusy()) return;

			m_IsLoading = true;
			m_StatusMessage = $"Installing {pkg.displayName}...";
			m_AddRequest = Client.Add(pkg.gitUrl);
			EditorApplication.update += OnUpdate;
		}

		private void Uninstall(PackageData pkg)
		{
			if (IsBusy()) return;

			m_IsLoading = true;
			m_StatusMessage = $"Uninstalling {pkg.displayName}...";
			m_RemoveRequest = Client.Remove(pkg.packageName);
			EditorApplication.update += OnUpdate;
		}

		private bool IsBusy()
		{
			if (m_AddRequest != null || m_RemoveRequest != null || m_ListRequest != null)
			{
				Debug.LogWarning("[RCorePackagesManager] Operation in progress, please wait.");
				return true;
			}
			return false;
		}

		#endregion

		#region Dependency Helpers

		private List<PackageData> GetMissingDependencies(PackageData pkg)
		{
			var missing = new List<PackageData>();
			if (pkg.dependencies == null) return missing;

			foreach (string depName in pkg.dependencies)
			{
				m_ResolvedInfos.TryGetValue(depName, out var info);
				if (info == null || info.mode == InstallMode.NotInstalled)
				{
					var depPkg = m_Packages.FirstOrDefault(p => p.packageName == depName);
					if (depPkg != null)
						missing.Add(depPkg);
				}
			}
			return missing;
		}

		private List<PackageData> GetDependentPackages(PackageData pkg)
		{
			return m_Packages
				.Where(p => p.dependencies != null
				            && p.dependencies.Contains(pkg.packageName)
				            && m_ResolvedInfos.TryGetValue(p.packageName, out var info)
				            && info.mode != InstallMode.NotInstalled)
				.ToList();
		}

		#endregion

		#region Samples

		private void DrawSamplesSection()
		{
			// Only show if owner package is installed/source
			bool hasAnySample = false;
			bool isSourceMode = false;
			foreach (var sample in m_Samples)
			{
				m_ResolvedInfos.TryGetValue(sample.ownerPackage, out var ownerInfo);
				if (ownerInfo != null && ownerInfo.mode != InstallMode.NotInstalled)
				{
					hasAnySample = true;
					if (ownerInfo.mode == InstallMode.Source)
						isSourceMode = true;
				}
			}
			if (!hasAnySample) return;

			EditorGUILayout.LabelField("── Samples ──", m_CategoryStyle);

			// Source mode: Expose/Hide Samples~ toggle
			if (isSourceMode)
			{
				DrawExposeSamplesToggle();
			}

			foreach (var sample in m_Samples)
			{
				m_ResolvedInfos.TryGetValue(sample.ownerPackage, out var ownerInfo);
				if (ownerInfo == null || ownerInfo.mode == InstallMode.NotInstalled) continue;

				DrawSampleRow(sample, ownerInfo);
			}
		}

		/// <summary>
		/// Draws Expose/Hide toggle for Samples~ folder. Source mode only.
		/// Expose: renames Samples~ → Samples (visible in Unity for editing).
		/// Hide: renames Samples → Samples~ (hidden, UPM-compatible).
		/// </summary>
		private void DrawExposeSamplesToggle()
		{
			string rootPath = Path.Combine(Application.dataPath, "RCore/Main");
			string hiddenPath = Path.Combine(rootPath, "Samples~");
			string visiblePath = Path.Combine(rootPath, "Samples");

			bool isExposed = Directory.Exists(visiblePath);
			bool isHidden = Directory.Exists(hiddenPath);

			EditorGUILayout.BeginHorizontal("box");

			EditorGUILayout.BeginVertical(GUILayout.MinWidth(200));
			EditorGUILayout.LabelField("Edit Samples (Dev)", EditorStyles.boldLabel);
			if (isExposed)
				EditorGUILayout.LabelField("Samples~ → Samples (visible, editable in Unity)", EditorStyles.miniLabel);
			else
				EditorGUILayout.LabelField("Samples~ is hidden (UPM-compatible)", EditorStyles.miniLabel);
			EditorGUILayout.EndVertical();

			GUILayout.FlexibleSpace();

			EditorGUILayout.BeginVertical();
			GUILayout.FlexibleSpace();

			if (isExposed)
			{
				GUI.backgroundColor = new Color(1f, 0.75f, 0.2f);
				if (GUILayout.Button("Hide", GUILayout.Width(70), GUILayout.Height(28)))
				{
					try
					{
						Directory.Move(visiblePath, hiddenPath);
						if (File.Exists(visiblePath + ".meta"))
							File.Move(visiblePath + ".meta", hiddenPath + ".meta");
						AssetDatabase.Refresh();
						Debug.Log("[RCorePackagesManager] Samples hidden (Samples → Samples~)");
					}
					catch (Exception e)
					{
						Debug.LogError($"[RCorePackagesManager] Failed to hide samples: {e.Message}");
					}
				}
				GUI.backgroundColor = Color.white;
			}
			else if (isHidden)
			{
				GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
				if (GUILayout.Button("Expose", GUILayout.Width(70), GUILayout.Height(28)))
				{
					try
					{
						Directory.Move(hiddenPath, visiblePath);
						if (File.Exists(hiddenPath + ".meta"))
							File.Move(hiddenPath + ".meta", visiblePath + ".meta");
						AssetDatabase.Refresh();
						Debug.Log("[RCorePackagesManager] Samples exposed (Samples~ → Samples)");
					}
					catch (Exception e)
					{
						Debug.LogError($"[RCorePackagesManager] Failed to expose samples: {e.Message}");
					}
				}
				GUI.backgroundColor = Color.white;
			}

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndVertical();

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space(1);
		}

		private void DrawSampleRow(SampleData sample, ResolvedPackageInfo ownerInfo)
		{
			bool isImported = IsSampleImported(sample, ownerInfo);

			EditorGUILayout.BeginHorizontal("box");

			// Info
			EditorGUILayout.BeginVertical(GUILayout.MinWidth(200));
			EditorGUILayout.LabelField(sample.displayName, EditorStyles.boldLabel);
			EditorGUILayout.LabelField(sample.description, EditorStyles.miniLabel);
			EditorGUILayout.EndVertical();

			GUILayout.FlexibleSpace();

			// Buttons (vertically centered)
			EditorGUILayout.BeginVertical();
			GUILayout.FlexibleSpace();
			EditorGUILayout.BeginHorizontal();

			if (isImported)
			{
				GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
				if (GUILayout.Button("Remove", GUILayout.Width(70), GUILayout.Height(28)))
				{
					RemoveSample(sample, ownerInfo);
				}
				GUI.backgroundColor = Color.white;
			}
			else
			{
				GUI.backgroundColor = new Color(0.4f, 1f, 0.5f);
				if (GUILayout.Button("Import", GUILayout.Width(70), GUILayout.Height(28)))
				{
					ImportSample(sample, ownerInfo);
				}
				GUI.backgroundColor = Color.white;
			}

			EditorGUILayout.EndHorizontal();
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndVertical();

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space(1);
		}

		/// <summary>
		/// Checks if sample is imported. Uses UPM Sample API for UPM packages,
		/// or checks the UPM-compatible directory for Source mode.
		/// </summary>
		private bool IsSampleImported(SampleData sample, ResolvedPackageInfo ownerInfo)
		{
			// UPM mode: use Sample API
			if (ownerInfo.mode != InstallMode.Source)
			{
				var upmSample = FindUpmSample(sample, ownerInfo);
				if (upmSample.HasValue)
					return upmSample.Value.isImported;
			}

			// Source mode: check UPM-compatible path
			string destDir = GetSourceModeSamplePath(sample, ownerInfo);
			return Directory.Exists(destDir);
		}

		/// <summary>
		/// Imports sample. UPM mode delegates to Sample.Import(),
		/// Source mode copies to UPM-compatible path.
		/// </summary>
		private void ImportSample(SampleData sample, ResolvedPackageInfo ownerInfo)
		{
			// UPM mode: use Sample.Import()
			if (ownerInfo.mode != InstallMode.Source)
			{
				var upmSample = FindUpmSample(sample, ownerInfo);
				if (upmSample.HasValue)
				{
					if (upmSample.Value.Import())
					{
						Debug.Log($"[RCorePackagesManager] Imported \"{sample.displayName}\" via UPM to {upmSample.Value.importPath}");
					}
					else
					{
						Debug.LogError($"[RCorePackagesManager] UPM import failed for \"{sample.displayName}\"");
					}
					return;
				}
			}

			// Source mode: manually copy to UPM-compatible path
			ImportSampleSourceMode(sample, ownerInfo);
		}

		/// <summary>
		/// Removes imported sample. Finds the import path and deletes it.
		/// </summary>
		private void RemoveSample(SampleData sample, ResolvedPackageInfo ownerInfo)
		{
			if (!EditorUtility.DisplayDialog("Remove Sample",
				    $"Remove \"{sample.displayName}\"?\n\nThis will delete the imported files.",
				    "Remove", "Cancel"))
				return;

			string importPath = null;

			// UPM mode: get importPath from Sample API
			if (ownerInfo.mode != InstallMode.Source)
			{
				var upmSample = FindUpmSample(sample, ownerInfo);
				if (upmSample.HasValue && upmSample.Value.isImported)
				{
					importPath = upmSample.Value.importPath;
				}
			}

			// Source mode or fallback
			if (string.IsNullOrEmpty(importPath))
			{
				importPath = GetSourceModeSamplePath(sample, ownerInfo);
			}

			try
			{
				if (Directory.Exists(importPath))
				{
					Directory.Delete(importPath, true);
					string metaFile = importPath.TrimEnd('/', '\\') + ".meta";
					if (File.Exists(metaFile))
						File.Delete(metaFile);

					// Clean up version folder if empty
					string versionDir = Path.GetDirectoryName(importPath);
					if (versionDir != null && Directory.Exists(versionDir) && Directory.GetFileSystemEntries(versionDir).Length == 0)
					{
						Directory.Delete(versionDir);
						string versionMeta = versionDir.TrimEnd('/', '\\') + ".meta";
						if (File.Exists(versionMeta))
							File.Delete(versionMeta);

						// Clean up package folder if empty
						string packageDir = Path.GetDirectoryName(versionDir);
						if (packageDir != null && Directory.Exists(packageDir) && Directory.GetFileSystemEntries(packageDir).Length == 0)
						{
							Directory.Delete(packageDir);
							string packageMeta = packageDir.TrimEnd('/', '\\') + ".meta";
							if (File.Exists(packageMeta))
								File.Delete(packageMeta);
						}
					}
				}

				AssetDatabase.Refresh();
				Debug.Log($"[RCorePackagesManager] Removed sample \"{sample.displayName}\"");
			}
			catch (Exception e)
			{
				Debug.LogError($"[RCorePackagesManager] Failed to remove sample: {e.Message}");
			}
		}

		#region Sample Helpers

		/// <summary>
		/// Find the UPM Sample object for a sample.
		/// </summary>
		private UnityEditor.PackageManager.UI.Sample? FindUpmSample(SampleData sample, ResolvedPackageInfo ownerInfo)
		{
			if (ownerInfo.upmInfo == null) return null;

			try
			{
				var samples = UnityEditor.PackageManager.UI.Sample.FindByPackage(
					ownerInfo.upmInfo.name, ownerInfo.upmInfo.version);

				foreach (var s in samples)
				{
					if (s.displayName == sample.displayName)
						return s;
				}
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[RCorePackagesManager] Failed to find UPM sample: {e.Message}");
			}

			return null;
		}

		/// <summary>
		/// Returns UPM-compatible import path for Source mode:
		/// Assets/Samples/{packageDisplayName}/{version}/{sampleDisplayName}
		/// </summary>
		private string GetSourceModeSamplePath(SampleData sample, ResolvedPackageInfo ownerInfo)
		{
			var ownerPkg = m_Packages.FirstOrDefault(p => p.packageName == sample.ownerPackage);
			string packageDisplayName = ownerPkg?.displayName ?? sample.ownerPackage;
			string version = ownerInfo.version ?? "0.0.0";

			return Path.Combine(Application.dataPath, "Samples", packageDisplayName, version, sample.displayName);
		}

		/// <summary>
		/// Source mode import: copies from Samples~/ to UPM-compatible path.
		/// </summary>
		private void ImportSampleSourceMode(SampleData sample, ResolvedPackageInfo ownerInfo)
		{
			var ownerPkg = m_Packages.FirstOrDefault(p => p.packageName == sample.ownerPackage);
			if (ownerPkg == null || string.IsNullOrEmpty(ownerPkg.localAssetsPath))
			{
				Debug.LogError($"[RCorePackagesManager] Cannot find source path for sample \"{sample.displayName}\"");
				return;
			}

			string sourcePath = Path.Combine(Application.dataPath, ownerPkg.localAssetsPath, "Samples~", sample.sampleFolder);
			if (!Directory.Exists(sourcePath))
			{
				EditorUtility.DisplayDialog("Error",
					$"Cannot find sample source at:\n{sourcePath}",
					"OK");
				return;
			}

			string destPath = GetSourceModeSamplePath(sample, ownerInfo);

			try
			{
				CopyDirectoryRecursive(sourcePath, destPath);
				AssetDatabase.Refresh();
				Debug.Log($"[RCorePackagesManager] Imported \"{sample.displayName}\" to Assets/Samples/");
			}
			catch (Exception e)
			{
				Debug.LogError($"[RCorePackagesManager] Failed to import sample: {e.Message}");
			}
		}

		private static void CopyDirectoryRecursive(string sourceDir, string destDir)
		{
			Directory.CreateDirectory(destDir);

			foreach (string file in Directory.GetFiles(sourceDir))
			{
				string fileName = Path.GetFileName(file);
				File.Copy(file, Path.Combine(destDir, fileName), true);
			}

			foreach (string dir in Directory.GetDirectories(sourceDir))
			{
				string dirName = Path.GetFileName(dir);
				CopyDirectoryRecursive(dir, Path.Combine(destDir, dirName));
			}
		}

		#endregion

		#endregion
	}
}
