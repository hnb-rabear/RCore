using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using RCore.Config;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	public class AssetCatalogWindow : EditorWindow
	{
		[Serializable]
		public class UsageResult
		{
			public string prefabPath;
			public string componentType;
		}

		[Serializable]
		public class UsageCacheEntry
		{
			public string key;
			public string assetType;
			public List<UsageResult> usages = new List<UsageResult>();
			public List<string> skippedPrefabs = new List<string>();
		}

		[Serializable]
		public class DirectUsageIndexEntry
		{
			public string assetType;
			public string targetGuid;
			public long targetLocalFileId;
			public bool requiresLocalFileId;
			public string targetPath;
			public List<string> assetPaths = new List<string>();
		}

		[Serializable]
		public class DirectUsageIndex
		{
			public int scannerVersion;
			public string candidateManifest;
			public string targetSnapshotSignature;
			public bool isComplete;
			public string lastScanTimestamp;
			public List<DirectUsageIndexEntry> entries = new List<DirectUsageIndexEntry>();
			public List<string> skippedAssets = new List<string>();
		}

		private struct DirectUsageTarget
		{
			public string assetType;
			public string guid;
			public long localFileId;
			public bool requireLocalFileId;
			public string path;
			public string id;
		}

		[Serializable]
		private class UsageCacheFile
		{
			public string lastScanTimestamp;
			public bool hasCompleteUsageIndex;
			public List<UsageCacheEntry> entries = new List<UsageCacheEntry>();
			public List<string> skippedPrefabs = new List<string>();
			public DirectUsageIndex directUsageIndex;
		}

		private AssetCatalog m_Catalog;
		private int m_ActiveTabIndex = 0;
		private IAssetCatalogPanel[] m_Panels;
		private string[] m_TabLabels;

		private const int DIRECT_USAGE_SCANNER_VERSION = 4;

		private static int s_DirectUsageProjectChangeVersion;
		private static bool s_directUsageBridgeResolved;
		private static bool s_directUsageBridgeAvailable;
		private static bool s_directUsageBridgeErrorReported;
		private static MethodInfo s_buildCacheMethod;
		private static MethodInfo s_getCachedReferencingAssetsMethod;
		private static ConstructorInfo s_objectReferenceTargetConstructor;
		private static MethodInfo s_scanAllObjectReferencesMethod;
		private static FieldInfo s_pathsByTargetIdField;
		private static FieldInfo s_skippedPathsField;

		private Dictionary<string, UsageCacheEntry> m_UsageCache = new Dictionary<string, UsageCacheEntry>();
		private DirectUsageIndex m_DirectUsageIndex;
		private int m_DirectUsageIndexValidatedProjectChangeVersion = -1;
		private string m_LastScanTimestamp = string.Empty;
		private List<string> m_CacheSkippedPrefabs = new List<string>();
		private bool m_HasCompleteUsageIndex;

		public AssetCatalog Catalog => m_Catalog;
		public Dictionary<string, UsageCacheEntry> UsageCache => m_UsageCache;
		public bool HasCompleteUsageIndex => m_HasCompleteUsageIndex;

		public void InvalidateDirectUsageIndex()
		{
			s_DirectUsageProjectChangeVersion++;
		}

		public string LastScanTimestamp
		{
			get => m_LastScanTimestamp;
			set => m_LastScanTimestamp = value;
		}

		public List<string> CacheSkippedPrefabs
		{
			get => m_CacheSkippedPrefabs;
			set => m_CacheSkippedPrefabs = value;
		}

		[MenuItem("RCore/Asset Catalog Editor")]
		public static void Open()
		{
			var window = GetWindow<AssetCatalogWindow>("Asset Catalog");
			window.Show();
		}

		private void OnEnable()
		{
			m_Catalog = AssetCatalog.Instance;
			m_Panels = new IAssetCatalogPanel[]
			{
				new AssetGridPanel(),
				new UsageReportPanel(),
				new RelinkPanel(),
			};
			m_TabLabels = new string[m_Panels.Length];
			for (int i = 0; i < m_Panels.Length; i++)
			{
				m_TabLabels[i] = m_Panels[i].Title;
				m_Panels[i].OnEnable(this);
			}
			LoadUsageCache();
		}

		private void OnProjectChange()
		{
			s_DirectUsageProjectChangeVersion++;
		}

		private void OnDisable()
		{
			if (m_Panels == null)
				return;

			foreach (var panel in m_Panels)
				panel.OnDisable();
		}

		private void OnGUI()
		{
			if (m_Catalog == null)
			{
				EditorGUILayout.HelpBox("No AssetCatalog found in Resources.", MessageType.Error);
				if (GUILayout.Button("Retry"))
					m_Catalog = AssetCatalog.Instance;
				return;
			}

			const float TAB_HEIGHT = 20f;
			const float CONTENT_TOP_PADDING = 6f;

			var newTab = GUILayout.Toolbar(m_ActiveTabIndex, m_TabLabels, GUILayout.Height(TAB_HEIGHT));
			if (newTab != m_ActiveTabIndex)
			{
				m_ActiveTabIndex = newTab;
				GUIUtility.keyboardControl = 0;
			}

			var contentY = TAB_HEIGHT + CONTENT_TOP_PADDING;
			var contentRect = new Rect(0f, contentY, position.width, Mathf.Max(0f, position.height - contentY));
			if (m_Panels != null && m_ActiveTabIndex >= 0 && m_ActiveTabIndex < m_Panels.Length)
				m_Panels[m_ActiveTabIndex].OnGUI(contentRect);
		}

		public void SwitchToTab(int pIndex)
		{
			if (m_Panels != null && pIndex >= 0 && pIndex < m_Panels.Length)
			{
				m_ActiveTabIndex = pIndex;
				Repaint();
			}
		}

		public static string GetCacheKey(string pAssetType, string pKey)
		{
			return $"{pAssetType}:{pKey}";
		}

		private static string GetDirectUsageTargetId(string pAssetType, string pGuid, long pLocalFileId, bool pRequireLocalFileId)
		{
			return $"{pAssetType}:{pGuid}:{pLocalFileId}:{pRequireLocalFileId}";
		}

		private string GetCacheAssetPath()
		{
			if (m_Catalog == null)
				return string.Empty;

			var assetPath = AssetDatabase.GetAssetPath(m_Catalog);
			if (string.IsNullOrEmpty(assetPath))
				return string.Empty;

			var dir = Path.GetDirectoryName(assetPath);
			return Path.Combine(dir ?? string.Empty, "AssetCatalog_Usages.json").Replace('\\', '/');
		}

		private string GetCacheFullPath()
		{
			var cacheAssetPath = GetCacheAssetPath();
			if (string.IsNullOrEmpty(cacheAssetPath))
				return string.Empty;

			var projectRoot = Directory.GetParent(Application.dataPath).FullName;
			return Path.Combine(projectRoot, cacheAssetPath);
		}

		public void LoadUsageCache()
		{
			m_UsageCache.Clear();
			m_DirectUsageIndex = null;
			m_DirectUsageIndexValidatedProjectChangeVersion = -1;
			m_CacheSkippedPrefabs.Clear();
			m_LastScanTimestamp = string.Empty;
			m_HasCompleteUsageIndex = false;

			var path = GetCacheFullPath();
			if (string.IsNullOrEmpty(path) || !File.Exists(path))
				return;

			try
			{
				var json = File.ReadAllText(path);
				var cacheFile = JsonUtility.FromJson<UsageCacheFile>(json);
				if (cacheFile == null)
					return;

				m_LastScanTimestamp = cacheFile.lastScanTimestamp;
				m_HasCompleteUsageIndex = cacheFile.hasCompleteUsageIndex;
				m_CacheSkippedPrefabs = cacheFile.skippedPrefabs ?? new List<string>();
				var entries = cacheFile.entries ?? new List<UsageCacheEntry>();
				foreach (var entry in entries)
				{
					if (entry == null || string.IsNullOrEmpty(entry.key) || string.IsNullOrEmpty(entry.assetType))
						continue;

					entry.usages = entry.usages ?? new List<UsageResult>();
					entry.skippedPrefabs = entry.skippedPrefabs ?? new List<string>();
					var cacheKey = GetCacheKey(entry.assetType, entry.key);
					m_UsageCache[cacheKey] = entry;
				}

				var directUsageIndex = cacheFile.directUsageIndex;
				if (directUsageIndex != null && directUsageIndex.scannerVersion == DIRECT_USAGE_SCANNER_VERSION)
				{
					directUsageIndex.entries = directUsageIndex.entries ?? new List<DirectUsageIndexEntry>();
					directUsageIndex.skippedAssets = directUsageIndex.skippedAssets ?? new List<string>();
					foreach (var entry in directUsageIndex.entries)
					{
						if (entry == null)
							continue;
						entry.assetPaths = entry.assetPaths ?? new List<string>();
					}
					m_DirectUsageIndex = directUsageIndex;
					ValidateLoadedDirectUsageIndex();
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[AssetCatalogWindow] Failed to load usage cache: {ex.Message}");
			}
		}

		private void ValidateLoadedDirectUsageIndex()
		{
			if (m_DirectUsageIndex == null)
				return;

			var targets = GetDirectUsageTargets();
			if (m_DirectUsageIndex.targetSnapshotSignature != GetDirectUsageTargetSnapshotSignature(targets))
			{
				m_DirectUsageIndex = null;
				return;
			}

			// Delay Reference Finder cache loading until direct results are requested.
			m_DirectUsageIndexValidatedProjectChangeVersion = -1;
		}

		public void SaveUsageCache()
		{
			var entries = new List<UsageCacheEntry>(m_UsageCache.Values);
			entries.Sort((pA, pB) =>
			{
				int typeComparison = string.Compare(pA.assetType, pB.assetType, StringComparison.Ordinal);
				return typeComparison != 0 ? typeComparison : string.Compare(pA.key, pB.key, StringComparison.Ordinal);
			});

			var cacheFile = new UsageCacheFile
			{
				lastScanTimestamp = m_LastScanTimestamp,
				hasCompleteUsageIndex = m_HasCompleteUsageIndex,
				entries = entries,
				skippedPrefabs = m_CacheSkippedPrefabs,
				directUsageIndex = m_DirectUsageIndex,
			};
			try
			{
				var fullPath = GetCacheFullPath();
				if (string.IsNullOrEmpty(fullPath))
					return;

				var json = JsonUtility.ToJson(cacheFile, false);
				File.WriteAllText(fullPath, json);
				var cacheAssetPath = GetCacheAssetPath();
				if (cacheAssetPath.StartsWith("Assets/"))
					AssetDatabase.ImportAsset(cacheAssetPath, ImportAssetOptions.Default);
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[AssetCatalogWindow] Failed to save usage cache: {ex.Message}");
			}
		}

		public void FindUsagesForAllAssets()
		{
			if (m_Catalog == null)
				return;

			var catalogKeysByAssetType = new Dictionary<string, HashSet<string>>
			{
				["Sprite"] = GetCatalogKeys(m_Catalog.EditorSprites, pEntry => pEntry.key),
				["Texture2D"] = GetCatalogKeys(m_Catalog.EditorTextures, pEntry => pEntry.key),
				["AudioClip"] = GetCatalogKeys(m_Catalog.EditorAudioClips, pEntry => pEntry.key),
			};
			var newCache = CreateUsageCache(catalogKeysByAssetType);
			var linkersByScriptGuid = GetLinkersByScriptGuid();
			if (linkersByScriptGuid == null)
				return;

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			var prefabPaths = GetPrefabPaths();
			var usages = new ConcurrentBag<AssetCatalogUsageScanner.Usage>();
			var skippedPrefabs = new ConcurrentBag<string>();
			try
			{
				const int PREFABS_PER_PROGRESS_UPDATE = 64;
				var prefabCount = prefabPaths.Count;
				if (prefabCount == 0)
					EditorUtility.DisplayProgressBar("Finding All Asset Usages", "No prefab files found", 1f);
				else
					EditorUtility.DisplayProgressBar("Finding All Asset Usages", $"Scanning 0/{prefabCount} prefab files", 0f);
				var prefabsRequiringFallback = new ConcurrentBag<string>();
				for (int batchStart = 0; batchStart < prefabCount; batchStart += PREFABS_PER_PROGRESS_UPDATE)
				{
					var batchEnd = Math.Min(batchStart + PREFABS_PER_PROGRESS_UPDATE, prefabCount);
					Parallel.For(batchStart, batchEnd, prefabIndex =>
					{
						var prefabPath = prefabPaths[prefabIndex];
						var scanResult = AssetCatalogUsageScanner.ScanPrefabFile(
							prefabPath,
							linkersByScriptGuid,
							catalogKeysByAssetType,
							out var error);
						if (!string.IsNullOrEmpty(error))
							skippedPrefabs.Add($"{prefabPath} ({error})");
						else if (scanResult.requiresPrefabContentsScan)
							prefabsRequiringFallback.Add(prefabPath);
						else
						{
							foreach (var usage in scanResult.usages)
								usages.Add(usage);
						}
					});

					EditorUtility.DisplayProgressBar(
						"Finding All Asset Usages",
						$"Scanning {batchEnd}/{prefabCount} prefab files",
						0.95f * batchEnd / prefabCount);
				}

				var fallbackCount = prefabsRequiringFallback.Count;
				var fallbackIndex = 0;
				foreach (var prefabPath in prefabsRequiringFallback)
				{
					fallbackIndex++;
					EditorUtility.DisplayProgressBar(
						"Finding All Asset Usages",
						$"Resolving nested prefabs {fallbackIndex}/{fallbackCount}",
						0.95f + 0.05f * fallbackIndex / fallbackCount);
					var fallbackUsages = ScanPrefabContents(prefabPath, catalogKeysByAssetType, out var error);
					if (!string.IsNullOrEmpty(error))
						skippedPrefabs.Add($"{prefabPath} ({error})");
					else
					{
						foreach (var usage in fallbackUsages)
							usages.Add(usage);
					}
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}

			foreach (var usage in usages)
			{
				if (newCache.TryGetValue(GetCacheKey(usage.assetType, usage.key), out var cacheEntry))
					cacheEntry.usages.Add(new UsageResult { prefabPath = usage.prefabPath, componentType = usage.componentType });
			}
			foreach (var cacheEntry in newCache.Values)
			{
				cacheEntry.usages.Sort((pLeft, pRight) =>
				{
					var pathComparison = string.Compare(pLeft.prefabPath, pRight.prefabPath, StringComparison.Ordinal);
					return pathComparison != 0
						? pathComparison
						: string.Compare(pLeft.componentType, pRight.componentType, StringComparison.Ordinal);
				});
			}

			m_UsageCache = newCache;
			m_CacheSkippedPrefabs = new List<string>(skippedPrefabs);
			m_CacheSkippedPrefabs.Sort(StringComparer.Ordinal);
			m_HasCompleteUsageIndex = m_CacheSkippedPrefabs.Count == 0;
			m_LastScanTimestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
			SaveUsageCache();
			Repaint();
		}

		public Dictionary<string, UsageCacheEntry> FindUsagesForSelectedKeys(
			string pAssetType,
			IReadOnlyCollection<string> pKeys,
			out List<string> pSkippedPrefabs)
		{
			pSkippedPrefabs = new List<string>();
			var catalogKeys = new HashSet<string>(pKeys ?? Array.Empty<string>(), StringComparer.Ordinal);
			var catalogKeysByAssetType = new Dictionary<string, HashSet<string>>
			{
				[pAssetType] = catalogKeys,
			};
			var newCache = CreateUsageCache(catalogKeysByAssetType);
			if (string.IsNullOrEmpty(pAssetType) || catalogKeys.Count == 0)
				return newCache;

			var linkersByScriptGuid = GetLinkersByScriptGuid();
			if (linkersByScriptGuid == null)
			{
				pSkippedPrefabs.Add("Unable to resolve Asset Catalog linker script GUIDs.");
				return newCache;
			}

			var allPrefabPaths = GetPrefabPaths();
			var prefabPaths = new List<string>();
			foreach (var path in allPrefabPaths)
			{
				if (path.StartsWith("Assets/", StringComparison.Ordinal))
					prefabPaths.Add(path);
			}

			var usages = new ConcurrentBag<AssetCatalogUsageScanner.Usage>();
			var skippedPrefabs = new ConcurrentBag<string>();
			try
			{
				const int PREFABS_PER_PROGRESS_UPDATE = 64;
				var prefabCount = prefabPaths.Count;
				EditorUtility.DisplayProgressBar(
					"Checking Selected Asset Usages",
					prefabCount == 0 ? "No prefab files found" : $"Scanning 0/{prefabCount} prefab files",
					0f);

				for (int batchStart = 0; batchStart < prefabCount; batchStart += PREFABS_PER_PROGRESS_UPDATE)
				{
					var batchEnd = Math.Min(batchStart + PREFABS_PER_PROGRESS_UPDATE, prefabCount);
					Parallel.For(batchStart, batchEnd, prefabIndex =>
					{
						var prefabPath = prefabPaths[prefabIndex];
						var scanResult = AssetCatalogUsageScanner.ScanPrefabFile(
							prefabPath,
							linkersByScriptGuid,
							catalogKeysByAssetType,
							out var error);
						if (!string.IsNullOrEmpty(error))
							skippedPrefabs.Add($"{prefabPath} ({error})");
						else if (scanResult.requiresPrefabContentsScan)
							skippedPrefabs.Add($"{prefabPath} (nested prefab requires contents scan)");
						else
						{
							foreach (var usage in scanResult.usages)
								usages.Add(usage);
						}
					});

					EditorUtility.DisplayProgressBar(
						"Checking Selected Asset Usages",
						$"Scanning {batchEnd}/{prefabCount} prefab files",
						0.95f * batchEnd / Mathf.Max(1, prefabCount));
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}

			foreach (var usage in usages)
			{
				if (newCache.TryGetValue(GetCacheKey(usage.assetType, usage.key), out var cacheEntry))
					cacheEntry.usages.Add(new UsageResult
					{
						prefabPath = usage.prefabPath,
						componentType = usage.componentType,
					});
			}
			foreach (var cacheEntry in newCache.Values)
			{
				cacheEntry.usages.Sort((pLeft, pRight) =>
				{
					var pathComparison = string.Compare(pLeft.prefabPath, pRight.prefabPath, StringComparison.Ordinal);
					return pathComparison != 0
						? pathComparison
						: string.Compare(pLeft.componentType, pRight.componentType, StringComparison.Ordinal);
				});
			}

			pSkippedPrefabs.AddRange(skippedPrefabs);
			pSkippedPrefabs.Sort(StringComparer.Ordinal);
			return newCache;
		}

		public bool TryGetDirectUsages(UnityEngine.Object pAsset, string pAssetType, out List<string> pAssetPaths, out List<string> pSkippedAssets)
		{
			pAssetPaths = new List<string>();
			pSkippedAssets = new List<string>();
			if (!TryGetDirectUsageTarget(pAsset, pAssetType, out var target) || !TryGetValidDirectUsageIndex(out var index))
				return false;

			foreach (var entry in index.entries)
			{
				if (entry == null || entry.assetType != target.assetType || entry.targetGuid != target.guid ||
					entry.targetLocalFileId != target.localFileId || entry.requiresLocalFileId != target.requireLocalFileId)
					continue;

				pAssetPaths.AddRange(entry.assetPaths);
				pSkippedAssets.AddRange(index.skippedAssets);
				return true;
			}
			return false;
		}

		public bool FindDirectUsages(UnityEngine.Object pAsset, string pAssetType, out List<string> pAssetPaths, out List<string> pSkippedAssets)
		{
			pAssetPaths = new List<string>();
			pSkippedAssets = new List<string>();
			if (!TryGetDirectUsageTarget(pAsset, pAssetType, out _))
				return false;

			if (!EnsureDirectUsageIndex())
				return false;

			return TryGetDirectUsages(pAsset, pAssetType, out pAssetPaths, out pSkippedAssets);
		}

		public bool EnsureDirectUsageIndex()
		{
			return TryGetValidDirectUsageIndex(out _) || BuildDirectUsageIndex();
		}

		public bool RefreshDirectUsageIndex()
		{
			return BuildDirectUsageIndex();
		}

		private bool TryGetValidDirectUsageIndex(out DirectUsageIndex pIndex)
		{
			pIndex = null;
			if (m_DirectUsageIndex == null || m_DirectUsageIndex.scannerVersion != DIRECT_USAGE_SCANNER_VERSION)
				return false;

			var targets = GetDirectUsageTargets();
			if (m_DirectUsageIndex.targetSnapshotSignature != GetDirectUsageTargetSnapshotSignature(targets))
				return false;
			if (m_DirectUsageIndexValidatedProjectChangeVersion == s_DirectUsageProjectChangeVersion)
			{
				pIndex = m_DirectUsageIndex;
				return true;
			}

			if (!TryUseRAssetFilter(targets, false, out var candidatePaths, out _, out _))
				return false;
			if (m_DirectUsageIndex.candidateManifest != GetDirectUsageManifest(candidatePaths))
				return false;

			m_DirectUsageIndexValidatedProjectChangeVersion = s_DirectUsageProjectChangeVersion;
			pIndex = m_DirectUsageIndex;
			return true;
		}

		private bool BuildDirectUsageIndex()
		{
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			var targets = GetDirectUsageTargets();
			if (!TryUseRAssetFilter(targets, true, out var candidatePaths, out var pathsByTargetId, out var skippedPaths))
				return false;

			var candidateManifest = GetDirectUsageManifest(candidatePaths);
			var targetSnapshotSignature = GetDirectUsageTargetSnapshotSignature(targets);

			var entries = new List<DirectUsageIndexEntry>();
			foreach (var target in targets)
			{
				pathsByTargetId.TryGetValue(target.id, out var paths);
				entries.Add(new DirectUsageIndexEntry
				{
					assetType = target.assetType,
					targetGuid = target.guid,
					targetLocalFileId = target.localFileId,
					requiresLocalFileId = target.requireLocalFileId,
					targetPath = target.path,
					assetPaths = paths ?? new List<string>(),
				});
			}
			entries.Sort((pA, pB) => string.Compare(pA.targetPath, pB.targetPath, StringComparison.Ordinal));
			m_DirectUsageIndex = new DirectUsageIndex
			{
				scannerVersion = DIRECT_USAGE_SCANNER_VERSION,
				candidateManifest = candidateManifest,
				targetSnapshotSignature = targetSnapshotSignature,
				isComplete = skippedPaths.Count == 0,
				lastScanTimestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
				entries = entries,
				skippedAssets = skippedPaths,
			};
			m_DirectUsageIndexValidatedProjectChangeVersion = s_DirectUsageProjectChangeVersion;
			SaveUsageCache();
			return true;
		}

		private List<DirectUsageTarget> GetDirectUsageTargets()
		{
			var targets = new Dictionary<string, DirectUsageTarget>(StringComparer.Ordinal);
			AddDirectUsageTargets(m_Catalog.EditorSprites, pEntry => pEntry.asset, "Sprite", targets);
			AddDirectUsageTargets(m_Catalog.EditorTextures, pEntry => pEntry.asset, "Texture2D", targets);
			AddDirectUsageTargets(m_Catalog.EditorAudioClips, pEntry => pEntry.asset, "AudioClip", targets);
			var result = new List<DirectUsageTarget>(targets.Values);
			result.Sort((pA, pB) => string.Compare(pA.id, pB.id, StringComparison.Ordinal));
			return result;
		}

		private static void AddDirectUsageTargets<T>(IEnumerable<T> pEntries, Func<T, UnityEngine.Object> pGetAsset, string pAssetType, IDictionary<string, DirectUsageTarget> pTargets)
		{
			foreach (var entry in pEntries)
			{
				if (!TryGetDirectUsageTarget(pGetAsset(entry), pAssetType, out var target))
					continue;
				pTargets[target.id] = target;
			}
		}

		private static string GetDirectUsageTargetSnapshotSignature(IEnumerable<DirectUsageTarget> pTargets)
		{
			var signature = new System.Text.StringBuilder();
			foreach (var target in pTargets)
				signature.Append(target.id).Append('|').Append(target.path).Append('\n');
			return signature.ToString();
		}

		private static bool TryGetDirectUsageTarget(UnityEngine.Object pAsset, string pAssetType, out DirectUsageTarget pTarget)
		{
			pTarget = default;
			if (pAsset == null || string.IsNullOrEmpty(pAssetType) ||
				!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(pAsset, out var guid, out long localFileId) ||
				string.IsNullOrEmpty(guid))
				return false;

			pTarget.assetType = pAssetType;
			pTarget.guid = guid;
			pTarget.localFileId = localFileId;
			pTarget.path = AssetDatabase.GetAssetPath(pAsset);
			pTarget.requireLocalFileId = pAssetType == "Sprite" || localFileId != 0;
			pTarget.id = GetDirectUsageTargetId(pTarget.assetType, pTarget.guid, pTarget.localFileId, pTarget.requireLocalFileId);
			return !string.IsNullOrEmpty(pTarget.path) && (!pTarget.requireLocalFileId || localFileId != 0);
		}

		private static string GetDirectUsageManifest(IEnumerable<string> pAssetPaths)
		{
			var projectRoot = Directory.GetParent(Application.dataPath).FullName;
			var manifest = new System.Text.StringBuilder();
			foreach (var assetPath in pAssetPaths)
			{
				var fullPath = Path.Combine(projectRoot, assetPath);
				try
				{
					var info = new FileInfo(fullPath);
					manifest.Append(assetPath).Append('|').Append(info.Length).Append('|').Append(info.LastWriteTimeUtc.Ticks).Append('\n');
				}
				catch (Exception ex)
				{
					manifest.Append(assetPath).Append("|error|").Append(ex.GetType().FullName).Append('\n');
				}
			}
			return manifest.ToString();
		}

		private static bool TryUseRAssetFilter(
			IReadOnlyList<DirectUsageTarget> pTargets,
			bool pScan,
			out List<string> pCandidatePaths,
			out Dictionary<string, List<string>> pPathsByTargetId,
			out List<string> pSkippedPaths)
		{
			pCandidatePaths = new List<string>();
			pPathsByTargetId = new Dictionary<string, List<string>>(StringComparer.Ordinal);
			pSkippedPaths = new List<string>();

			try
			{
				if (!s_directUsageBridgeResolved)
				{
					s_directUsageBridgeResolved = true;
					foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
					{
						if (assembly.GetName().Name != "RCore.RAssetFilter.Editor")
							continue;

						var filterType = assembly.GetType("RCore.RAssetFilter.Editor.RAssetFilter");
						var scannerType = assembly.GetType("RCore.RAssetFilter.Editor.AssetReferenceTextScanner");
						var objectReferenceTargetType = scannerType?.GetNestedType("ObjectReferenceTarget", BindingFlags.Public);
						var resultType = scannerType?.GetNestedType("AllTargetScanResult", BindingFlags.Public);
						s_buildCacheMethod = filterType?.GetMethod("BuildCache", BindingFlags.Public | BindingFlags.Static);
						s_getCachedReferencingAssetsMethod = filterType?.GetMethod("GetCachedReferencingAssets", BindingFlags.Public | BindingFlags.Static);
						s_objectReferenceTargetConstructor = objectReferenceTargetType?.GetConstructor(new[] { typeof(string), typeof(string), typeof(long), typeof(bool) });
						s_scanAllObjectReferencesMethod = scannerType?.GetMethod("ScanAllObjectReferences", BindingFlags.Public | BindingFlags.Static);
						s_pathsByTargetIdField = resultType?.GetField("pathsByTargetId", BindingFlags.Public | BindingFlags.Instance);
						s_skippedPathsField = resultType?.GetField("skippedPaths", BindingFlags.Public | BindingFlags.Instance);
						s_directUsageBridgeAvailable = s_buildCacheMethod != null && s_getCachedReferencingAssetsMethod != null &&
							s_objectReferenceTargetConstructor != null && s_scanAllObjectReferencesMethod != null &&
							s_pathsByTargetIdField != null && s_skippedPathsField != null;
						break;
					}
				}

				if (!s_directUsageBridgeAvailable)
					throw new InvalidOperationException("RAsset Filter is required for Direct Usage. Install com.rabear.rcore.assetfilter.");

				var targetPaths = new List<string>();
				foreach (var target in pTargets)
					if (!string.IsNullOrEmpty(target.path))
						targetPaths.Add(target.path);

				s_buildCacheMethod.Invoke(null, null);
				pCandidatePaths = s_getCachedReferencingAssetsMethod.Invoke(null, new object[] { targetPaths }) as List<string> ?? new List<string>();
				if (!pScan || pCandidatePaths.Count == 0)
					return true;

				var targetType = s_objectReferenceTargetConstructor.DeclaringType;
				var scanTargets = Array.CreateInstance(targetType, pTargets.Count);
				for (var i = 0; i < pTargets.Count; i++)
				{
					var target = pTargets[i];
					scanTargets.SetValue(s_objectReferenceTargetConstructor.Invoke(new object[]
					{
						target.id, target.guid, target.localFileId, target.requireLocalFileId,
					}), i);
				}

				Action<int, int> progress = (completed, total) => EditorUtility.DisplayProgressBar(
					"Finding Direct Asset References",
					$"Scanning {completed}/{total} RAsset Filter candidates",
					total == 0 ? 1f : (float)completed / total);
				try
				{
					var result = s_scanAllObjectReferencesMethod.Invoke(null, new object[]
					{
						pCandidatePaths,
						scanTargets,
						128,
						progress,
						Directory.GetParent(Application.dataPath).FullName,
					});
					var sourcePaths = s_pathsByTargetIdField.GetValue(result) as Dictionary<string, List<string>>;
					var sourceSkippedPaths = s_skippedPathsField.GetValue(result) as List<string>;
					if (sourcePaths != null)
						foreach (var pair in sourcePaths)
							pPathsByTargetId[pair.Key] = new List<string>(pair.Value);
					if (sourceSkippedPaths != null)
						pSkippedPaths.AddRange(sourceSkippedPaths);
					return true;
				}
				finally
				{
					EditorUtility.ClearProgressBar();
				}
			}
			catch (Exception ex)
			{
				if (!s_directUsageBridgeErrorReported)
				{
					s_directUsageBridgeErrorReported = true;
					Debug.LogWarning($"[AssetCatalogWindow] Direct Usage unavailable: {ex.Message}");
				}
				return false;
			}
		}

		private static List<AssetCatalogUsageScanner.Usage> ScanPrefabContents(
			string pPrefabPath,
			IReadOnlyDictionary<string, HashSet<string>> pCatalogKeysByAssetType,
			out string pError)
		{
			var usages = new List<AssetCatalogUsageScanner.Usage>();
			pError = null;
			GameObject root = null;
			try
			{
				root = PrefabUtility.LoadPrefabContents(pPrefabPath);
				if (root == null)
				{
					pError = "Prefab load failed.";
					return usages;
				}

				AddPrefabContentsUsages(root.GetComponentsInChildren<GeneralSpriteLinker>(true), pPrefabPath, "Sprite", nameof(GeneralSpriteLinker), pCatalogKeysByAssetType, usages);
				AddPrefabContentsUsages(root.GetComponentsInChildren<GeneralSpriteRendererLinker>(true), pPrefabPath, "Sprite", nameof(GeneralSpriteRendererLinker), pCatalogKeysByAssetType, usages);
				AddPrefabContentsUsages(root.GetComponentsInChildren<GeneralTextureLinker>(true), pPrefabPath, "Texture2D", nameof(GeneralTextureLinker), pCatalogKeysByAssetType, usages);
				AddPrefabContentsUsages(root.GetComponentsInChildren<GeneralAudioLinker>(true), pPrefabPath, "AudioClip", nameof(GeneralAudioLinker), pCatalogKeysByAssetType, usages);
			}
			catch (Exception ex)
			{
				pError = ex.Message;
			}
			finally
			{
				if (root != null)
				{
					try { PrefabUtility.UnloadPrefabContents(root); }
					catch (Exception ex)
					{
						pError = string.IsNullOrEmpty(pError) ? $"Unload failed: {ex.Message}" : pError;
					}
				}
			}
			return usages;
		}

		private static void AddPrefabContentsUsages<T>(
			IEnumerable<T> pLinkers,
			string pPrefabPath,
			string pAssetType,
			string pComponentType,
			IReadOnlyDictionary<string, HashSet<string>> pCatalogKeysByAssetType,
			ICollection<AssetCatalogUsageScanner.Usage> pUsages)
			where T : Component
		{
			if (!pCatalogKeysByAssetType.TryGetValue(pAssetType, out var catalogKeys))
				return;

			foreach (var linker in pLinkers)
			{
				string key;
				switch (linker)
				{
					case GeneralSpriteLinker spriteLinker: key = spriteLinker.Key; break;
					case GeneralSpriteRendererLinker spriteRendererLinker: key = spriteRendererLinker.Key; break;
					case GeneralTextureLinker textureLinker: key = textureLinker.Key; break;
					case GeneralAudioLinker audioLinker: key = audioLinker.Key; break;
					default: continue;
				}

				if (catalogKeys.Contains(key))
					pUsages.Add(new AssetCatalogUsageScanner.Usage(pPrefabPath, pAssetType, key, pComponentType));
			}
		}

		private static HashSet<string> GetCatalogKeys<T>(IEnumerable<T> pEntries, Func<T, string> pGetKey)
		{
			var keys = new HashSet<string>();
			foreach (var entry in pEntries)
			{
				var key = pGetKey(entry);
				if (!string.IsNullOrEmpty(key))
					keys.Add(key);
			}
			return keys;
		}

		private static Dictionary<string, UsageCacheEntry> CreateUsageCache(IReadOnlyDictionary<string, HashSet<string>> pCatalogKeysByAssetType)
		{
			var cache = new Dictionary<string, UsageCacheEntry>();
			foreach (var pair in pCatalogKeysByAssetType)
			{
				foreach (var key in pair.Value)
					cache[GetCacheKey(pair.Key, key)] = new UsageCacheEntry { key = key, assetType = pair.Key };
			}
			return cache;
		}

		private static List<string> GetPrefabPaths()
		{
			var guids = AssetDatabase.FindAssets("t:Prefab");
			var prefabPaths = new List<string>(guids.Length);
			foreach (var guid in guids)
				prefabPaths.Add(AssetDatabase.GUIDToAssetPath(guid));
			return prefabPaths;
		}

		private static Dictionary<string, AssetCatalogUsageScanner.LinkerInfo> GetLinkersByScriptGuid()
		{
			var linkers = new Dictionary<string, AssetCatalogUsageScanner.LinkerInfo>();
			if (!AddLinkerScriptGuid<GeneralSpriteLinker>(linkers, "Sprite", nameof(GeneralSpriteLinker)) ||
				!AddLinkerScriptGuid<GeneralSpriteRendererLinker>(linkers, "Sprite", nameof(GeneralSpriteRendererLinker)) ||
				!AddLinkerScriptGuid<GeneralTextureLinker>(linkers, "Texture2D", nameof(GeneralTextureLinker)) ||
				!AddLinkerScriptGuid<GeneralAudioLinker>(linkers, "AudioClip", nameof(GeneralAudioLinker)))
			{
				EditorUtility.DisplayDialog("Find All Asset Usages", "Cannot resolve Asset Catalog linker script GUIDs.", "OK");
				return null;
			}
			return linkers;
		}

		private static bool AddLinkerScriptGuid<T>(IDictionary<string, AssetCatalogUsageScanner.LinkerInfo> pLinkers, string pAssetType, string pComponentType)
			where T : MonoBehaviour
		{
			var gameObject = new GameObject("AssetCatalogUsageScanner");
			try
			{
				var component = gameObject.AddComponent<T>();
				var script = MonoScript.FromMonoBehaviour(component);
				if (script == null)
					return false;

				var scriptPath = AssetDatabase.GetAssetPath(script);
				var scriptGuid = AssetDatabase.AssetPathToGUID(scriptPath);
				if (string.IsNullOrEmpty(scriptGuid))
					return false;

				pLinkers.Add(scriptGuid, new AssetCatalogUsageScanner.LinkerInfo(pAssetType, pComponentType));
				return true;
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(gameObject);
			}

		}
	}

}
