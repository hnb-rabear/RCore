using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCore.RAssetFilter.Editor
{
	public static class RAssetFilter
	{
		public struct FolderStats
		{
			public int unusedFilesCount;
			public long unusedSize;
		}

		public static HashSet<string> UnusedAssetsCache = new HashSet<string>();
		public static Dictionary<string, FolderStats> FolderStatsCache = new Dictionary<string, FolderStats>();
		public static Dictionary<string, List<string>> ReferenceCache = new Dictionary<string, List<string>>();
		public static Dictionary<string, long> SizeCache = new Dictionary<string, long>();

		/// <summary>Forward map (path -> its own dependency paths) as of the last full build or incremental update. Lets a single reimported asset's edges be diffed without rescanning the whole project.</summary>
		private static readonly Dictionary<string, List<string>> DependencyCache = new Dictionary<string, List<string>>();
		private const int MAX_PENDING_CHANGES = 8192;
		private static readonly HashSet<string> PendingImportedAssets = new HashSet<string>(StringComparer.Ordinal);
		private static readonly List<string> PendingMovedFromPaths = new List<string>();
		private static readonly List<string> PendingMovedToPaths = new List<string>();

		private static bool m_cacheReady;
		private static bool m_unusedDataReady;

		/// <summary>True only after a completed cache build or a validated cache restore.</summary>
		public static bool HasReferenceData => m_cacheReady;

		/// <summary>True only after a full unused-asset scan or validated full-scan cache restore.</summary>
		public static bool HasUnusedData => m_unusedDataReady;

		/// <summary>Increases whenever cached data is built, restored or cleared. Consumers use it to drop derived state.</summary>
		public static int CacheGeneration { get; private set; }

		/// <summary>Raised once per invalidation that actually dropped cached data.</summary>
		public static event Action CacheInvalidated;

		/// <summary>Raised after an incremental update applied project changes without dropping the scan.</summary>
		public static event Action CacheChanged;

		/// <summary>Drops every cached scan result and the persisted cache file. No-op when nothing is cached.</summary>
		public static void InvalidateCache()
		{
			bool hadRuntimeData = m_cacheReady
				|| ReferenceCache.Count > 0
				|| DependencyCache.Count > 0
				|| SizeCache.Count > 0
				|| UnusedAssetsCache.Count > 0
				|| FolderStatsCache.Count > 0;
			bool hadPersistedData = File.Exists(CACHE_PATH);

			if (!hadRuntimeData && !hadPersistedData)
				return;

			if (hadPersistedData)
				DiscardCacheFile();

			PendingImportedAssets.Clear();
			PendingMovedFromPaths.Clear();
			PendingMovedToPaths.Clear();
			ClearCacheState();
			CacheInvalidated?.Invoke();
			EditorApplication.RepaintProjectWindow();
		}

		private static void ClearCacheState()
		{
			ReferenceCache.Clear();
			DependencyCache.Clear();
			SizeCache.Clear();
			UnusedAssetsCache.Clear();
			FolderStatsCache.Clear();
			m_cacheReady = false;
			m_unusedDataReady = false;
			CacheGeneration++;
		}

		private static bool HasPendingCapacity(int additional)
		{
			return PendingImportedAssets.Count + PendingMovedFromPaths.Count + additional <= MAX_PENDING_CHANGES;
		}

		private static void DropCacheState()
		{
			bool hadRuntimeData = m_cacheReady
				|| ReferenceCache.Count > 0
				|| DependencyCache.Count > 0
				|| SizeCache.Count > 0
				|| UnusedAssetsCache.Count > 0
				|| FolderStatsCache.Count > 0;
			PendingImportedAssets.Clear();
			PendingMovedFromPaths.Clear();
			PendingMovedToPaths.Clear();
			ClearCacheState();

			if (hadRuntimeData)
				CacheInvalidated?.Invoke();
			EditorApplication.RepaintProjectWindow();
		}

		private static void MarkCacheReady()
		{
			m_cacheReady = true;
			CacheGeneration++;
		}

		private static int m_internalAssetEditDepth;

		public static bool IsEditingAssetsInternally => m_internalAssetEditDepth > 0;

		public static void BeginInternalAssetEdit()
		{
			m_internalAssetEditDepth++;
		}

		public static void EndInternalAssetEdit()
		{
			if (m_internalAssetEditDepth > 0)
				m_internalAssetEditDepth--;
		}

		public static void ForgetAsset(string path)
		{
			if (string.IsNullOrEmpty(path) || !m_cacheReady || !m_unusedDataReady)
				return;

			if (!RemoveAssetFromCache(path))
				return;

			RefreshUnusedAndFolderStats();
			CacheGeneration++;
			EditorApplication.RepaintProjectWindow();
		}

		/// <summary>
		/// Batch-removes multiple deleted assets from the cache incrementally: removes each path's
		/// forward and reverse edges, refreshes unused/folder stats once, persists the cache, and
		/// fires <see cref="CacheChanged"/>. No-op when the cache is not ready.
		/// </summary>
		public static void ForgetAssets(string[] paths)
		{
			if (paths == null || paths.Length == 0 || !m_cacheReady || !m_unusedDataReady)
				return;

			bool changed = false;
			foreach (var path in paths)
			{
				if (string.IsNullOrEmpty(path))
					continue;
				changed |= RemoveAssetFromCache(path);
			}

			if (!changed)
				return;

			RefreshUnusedAndFolderStats();
			CacheGeneration++;
			SaveCache(new List<string>(UnusedAssetsCache));
			CacheChanged?.Invoke();
			EditorApplication.RepaintProjectWindow();
		}

		/// <summary>
		/// Drops the persisted cache. When the file cannot be deleted (locked, read-only) it is renamed
		/// so a stale scan can never come back on the next restore.
		/// </summary>
		private static void DiscardCacheFile()
		{
			if (!File.Exists(CACHE_PATH))
				return;

			try
			{
				File.Delete(CACHE_PATH);
				return;
			}
			catch (Exception deleteException)
			{
				try
				{
					if (File.Exists(CACHE_INVALID_PATH))
						File.Delete(CACHE_INVALID_PATH);
					File.Move(CACHE_PATH, CACHE_INVALID_PATH);
					return;
				}
				catch
				{
					Debug.LogWarning($"RAsset Filter could not discard the stale cache file: {deleteException.Message}. Run a new scan before trusting Project labels.");
				}
			}
		}

		public static List<string> FindUnusedAssets(List<string> ignorePaths)
		{
			var unusedAssets = new List<string>();
			var allAssets = AssetDatabase.GetAllAssetPaths();

			BuildCache();

			var projectAssets = allAssets.Where(p => p.StartsWith("Assets/")).ToArray();
			int index = 0;
			int total = projectAssets.Length;

			// 2. Identify Unused Assets
			foreach (var assetPath in projectAssets)


			{
				if (index % 100 == 0)
					EditorUtility.DisplayProgressBar("Scanning Assets", $"Checking usage: {Path.GetFileName(assetPath)}", (float)index / total);
				index++;

				if (AssetDatabase.IsValidFolder(assetPath)) continue;
				if (IsIgnored(assetPath, ignorePaths)) continue;

				bool isRoot = IsRootAsset(assetPath);

				// If it is NOT in the ReferenceCache, it means no one depends on it.
				if (!ReferenceCache.ContainsKey(assetPath))
				{
					if (!isRoot)
						unusedAssets.Add(assetPath);
				}
			}

			EditorUtility.ClearProgressBar();

			UnusedAssetsCache = new HashSet<string>(unusedAssets);
			CalculateFolderStats(unusedAssets);
			m_unusedDataReady = true;
			PendingImportedAssets.Clear();
			PendingMovedFromPaths.Clear();
			PendingMovedToPaths.Clear();

			return unusedAssets;
		}

		public static void BuildCache()
		{
			// Callers such as Direct Usage rebuild only dependency edges. Preserve a completed unused scan
			// by rebuilding its derived data from the new graph once that rebuild finishes.
			bool refreshUnusedData = m_unusedDataReady;
			ClearCacheState();

			// A graph-only build (unused data not ready) never persists, so any cache file from a
			// previous full scan must not survive it: LoadCache could otherwise restore that stale
			// snapshot and silently overwrite the fresher in-memory edges this build just produced.
			if (!refreshUnusedData && File.Exists(CACHE_PATH))
				DiscardCacheFile();
			var allAssets = AssetDatabase.GetAllAssetPaths();
			// Include ProjectSettings to find references like App Icon, Splash Screen, etc.
			var projectAssets = allAssets.Where(p => p.StartsWith("Assets/") || p.StartsWith("ProjectSettings/")).ToArray();
			int index = 0;
			int total = projectAssets.Length;

			try
			{
				foreach (var assetPath in projectAssets)
				{
					if (index % 50 == 0)
						EditorUtility.DisplayProgressBar("Building Cache", $"Analyzing dependencies: {Path.GetFileName(assetPath)}", (float)index / total);
					index++;

					// Skip directories
					if (AssetDatabase.IsValidFolder(assetPath)) continue;

					var dependencies = AssetDatabase.GetDependencies(assetPath, false);
					DependencyCache[assetPath] = new List<string>(dependencies);
					foreach (var dep in dependencies)
					{
						if (dep == assetPath) continue; // Self dependency

						if (!ReferenceCache.TryGetValue(dep, out var referencers))
						{
							referencers = new List<string>();
							ReferenceCache[dep] = referencers;
						}
						if (!referencers.Contains(assetPath))
							referencers.Add(assetPath);
					}
				}

				MarkCacheReady();
				if (refreshUnusedData)
				{
					RefreshUnusedAndFolderStats();
					m_unusedDataReady = true;
				}

				// This full rescan already reflects every queued path directly from AssetDatabase,
				// regardless of whether it also refreshed the unused-asset snapshot, so the queues
				// can never be left populated with no future drain path.
				PendingImportedAssets.Clear();
				PendingMovedFromPaths.Clear();
				PendingMovedToPaths.Clear();

				if (refreshUnusedData)
				{
					SaveCache(new List<string>(UnusedAssetsCache));
					CacheChanged?.Invoke();
					EditorApplication.RepaintProjectWindow();
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}


		/// <summary>Refreshes cached scan results after imported project files change by diffing only their dependency edges.</summary>
		public static void UpdateCacheForChangedAssets(List<string> importedAssets)
		{
			if (importedAssets == null)
				return;

			if (!m_cacheReady)
			{
				// Domain reload may not have restored the persisted cache yet. Queue paths and apply the
				// diff after LoadCache restores state; discarding the file here would lose a valid scan.
				var relevant = new List<string>();
				foreach (var path in importedAssets)
					if (!string.IsNullOrEmpty(path) && IsInScope(path) && !PendingImportedAssets.Contains(path))
						relevant.Add(path);

				if (!HasPendingCapacity(relevant.Count))
				{
					// An unbounded backlog (window never opened while the project churns) would hold
					// every changed path in memory forever. Drop the queues so a later scan or cache
					// restore starts clean, since the snapshot on disk is now stale anyway.
					PendingImportedAssets.Clear();
					PendingMovedFromPaths.Clear();
					PendingMovedToPaths.Clear();
					DiscardCacheFile();
					return;
				}

				foreach (var path in relevant)
					PendingImportedAssets.Add(path);
				return;
			}

			bool changesApplied = false;
			bool dependencyChanged = false;
			bool sizeChanged = false;
			for (int i = 0; i < importedAssets.Count; i++)
			{
				string path = importedAssets[i];
				if (string.IsNullOrEmpty(path) || !IsInScope(path))
					continue;
				bool assetDependencyChanged;
				bool assetSizeChanged;
				changesApplied |= UpdateAssetDependencies(path, out assetDependencyChanged, out assetSizeChanged);
				dependencyChanged |= assetDependencyChanged;
				sizeChanged |= assetSizeChanged && UnusedAssetsCache.Contains(path);
			}

			if (changesApplied)
			{
				if (m_unusedDataReady)
				{
					if (dependencyChanged)
						RefreshUnusedAndFolderStats();
					else if (sizeChanged)
						CalculateFolderStats(new List<string>(UnusedAssetsCache));
					// Only a full unused scan is a valid, persistable snapshot; a dependency-only
					// graph update (e.g. from Leak Checker) must not be written as one.
					SaveCache(new List<string>(UnusedAssetsCache));
				}
				CacheGeneration++;
				CacheChanged?.Invoke();
				EditorApplication.RepaintProjectWindow();
			}
		}

		/// <summary>Renames cached paths for moved assets without rebuilding dependency edges or discarding the scan. Returns whether a remap was applied to the live caches now (false if queued, a no-op, or invalidated).</summary>
		public static bool UpdateCacheForMovedAssets(List<string> movedFromPaths, List<string> movedPaths)
		{
			if (movedFromPaths == null || movedPaths == null || movedFromPaths.Count != movedPaths.Count || movedFromPaths.Count == 0)
				return false;

			var pathMap = new Dictionary<string, string>(StringComparer.Ordinal);
			var destinations = new HashSet<string>(StringComparer.Ordinal);
			for (int i = 0; i < movedFromPaths.Count; i++)
			{
				string fromPath = movedFromPaths[i];
				string toPath = movedPaths[i];
				if (string.IsNullOrEmpty(fromPath) || string.IsNullOrEmpty(toPath) || !IsInScope(fromPath) || !IsInScope(toPath))
				{
					InvalidateCache();
					return false;
				}
				if (fromPath == toPath)
					continue; // No-op rename (e.g. case-only on a case-preserving filesystem).

				// Folders are never cache keys (BuildCache skips them), so an exact-path rename can
				// never reach the child files a folder move actually relocates. Fall back to a rescan.
				if (AssetDatabase.IsValidFolder(toPath))
				{
					InvalidateCache();
					return false;
				}

				if (!pathMap.TryAdd(fromPath, toPath) || !destinations.Add(toPath))
				{
					InvalidateCache();
					return false;
				}
			}

			if (pathMap.Count == 0)
				return false;

			// A destination that is also another pair's source cannot be told apart, without asset
			// identity, from an unrelated move that coincidentally reused the vacated path (e.g. batch
			// has A->B and C->A). Treat any such overlap as ambiguous and rescan instead of risking
			// cached data getting misattributed to the wrong file.
			foreach (var toPath in pathMap.Values)
			{
				if (pathMap.ContainsKey(toPath))
				{
					InvalidateCache();
					return false;
				}
			}

			if (!m_cacheReady)
			{
				if (!HasPendingCapacity(pathMap.Count))
				{
					// An unbounded backlog (window never opened while the project churns) would hold
					// every moved pair in memory forever. Drop the queues so a later scan or cache
					// restore starts clean, since the snapshot on disk is now stale anyway.
					PendingImportedAssets.Clear();
					PendingMovedFromPaths.Clear();
					PendingMovedToPaths.Clear();
					DiscardCacheFile();
					return false;
				}

				foreach (var pair in pathMap)
				{
					PendingMovedFromPaths.Add(pair.Key);
					PendingMovedToPaths.Add(pair.Value);
				}
				return false;
			}

			var remappedSizes = new Dictionary<string, long>(SizeCache.Count, StringComparer.Ordinal);
			foreach (var pair in SizeCache)
				if (!TryAddRemapped(remappedSizes, RemapPath(pair.Key, pathMap), pair.Value))
				{
					InvalidateCache();
					return false;
				}

			var remappedReferences = new Dictionary<string, List<string>>(ReferenceCache.Count, StringComparer.Ordinal);
			foreach (var pair in ReferenceCache)
			{
				if (!TryRemapList(pair.Value, pathMap, out var references) ||
					!TryAddRemapped(remappedReferences, RemapPath(pair.Key, pathMap), references))
				{
					InvalidateCache();
					return false;
				}
			}

			var remappedDependencies = new Dictionary<string, List<string>>(DependencyCache.Count, StringComparer.Ordinal);
			foreach (var pair in DependencyCache)
			{
				if (!TryRemapList(pair.Value, pathMap, out var dependencies) ||
					!TryAddRemapped(remappedDependencies, RemapPath(pair.Key, pathMap), dependencies))
				{
					InvalidateCache();
					return false;
				}
			}

			SizeCache.Clear();
			foreach (var pair in remappedSizes)
				SizeCache[pair.Key] = pair.Value;
			ReferenceCache.Clear();
			foreach (var pair in remappedReferences)
				ReferenceCache[pair.Key] = pair.Value;
			DependencyCache.Clear();
			foreach (var pair in remappedDependencies)
				DependencyCache[pair.Key] = pair.Value;

			if (m_unusedDataReady)
			{
				RefreshUnusedAndFolderStats();
				SaveCache(new List<string>(UnusedAssetsCache));
			}

			PendingMovedFromPaths.Clear();
			PendingMovedToPaths.Clear();
			CacheGeneration++;
			CacheChanged?.Invoke();
			EditorApplication.RepaintProjectWindow();
			return true;
		}

		private static string RemapPath(string path, Dictionary<string, string> pathMap)
		{
			// UpdateCacheForMovedAssets rejects any pathMap where a destination is also a source,
			// so every entry here is a single, unchained rename.
			if (path == null || !pathMap.TryGetValue(path, out var remappedPath))
				return path;
			return remappedPath;
		}

		private static bool TryAddRemapped<T>(Dictionary<string, T> target, string key, T value)
		{
			return !string.IsNullOrEmpty(key) && target.TryAdd(key, value);
		}

		private static bool TryRemapList(List<string> source, Dictionary<string, string> pathMap, out List<string> remapped)
		{
			remapped = new List<string>();
			if (source == null)
				return false;

			var unique = new HashSet<string>(StringComparer.Ordinal);
			foreach (var path in source)
			{
				string remappedPath = RemapPath(path, pathMap);
				if (string.IsNullOrEmpty(remappedPath) || !unique.Add(remappedPath))
					return false;
				remapped.Add(remappedPath);
			}
			return true;
		}

		private static bool IsInScope(string path)
		{
			return path.StartsWith("Assets/") || path.StartsWith("ProjectSettings/");
		}

		/// <summary>Removes a deleted/moved-away asset from size, reference, and dependency caches. Callers must follow up with <see cref="RefreshUnusedAndFolderStats"/> to reclassify any target that lost its last referencer. Returns whether the asset was present in cache.</summary>
		private static bool RemoveAssetFromCache(string path)
		{
			bool changed = SizeCache.Remove(path);
			changed |= UnusedAssetsCache.Remove(path);
			changed |= DependencyCache.Remove(path);
			changed |= ReferenceCache.Remove(path);

			var emptyTargets = new List<string>();
			foreach (var pair in ReferenceCache)
			{
				if (pair.Value?.Remove(path) == true)
					changed = true;
				if (pair.Value == null || pair.Value.Count == 0)
					emptyTargets.Add(pair.Key);
			}
			foreach (var target in emptyTargets)
				ReferenceCache.Remove(target);

			return changed;
		}

		/// <summary>Rebuilds one asset's forward dependency edges and reports dependency-edge and cached-size changes separately.</summary>
		private static bool UpdateAssetDependencies(string path, out bool dependencyChanged, out bool sizeChanged)
		{
			dependencyChanged = false;
			sizeChanged = false;
			if (AssetDatabase.IsValidFolder(path))
				return false;

			sizeChanged = SizeCache.Remove(path);
			bool changed = sizeChanged;
			var newDependencies = AssetDatabase.GetDependencies(path, false) ?? new string[0];
			var newSet = new HashSet<string>(newDependencies, StringComparer.Ordinal);
			newSet.Remove(path);

			if (DependencyCache.TryGetValue(path, out var oldDependencies))
			{
				var oldSet = new HashSet<string>(oldDependencies, StringComparer.Ordinal);
				oldSet.Remove(path);

				foreach (var dep in oldSet)
				{
					if (newSet.Contains(dep))
						continue;
					if (ReferenceCache.TryGetValue(dep, out var refs))
					{
						refs.Remove(path);
						if (refs.Count == 0)
							ReferenceCache.Remove(dep);
					}
					changed = true;
					dependencyChanged = true;
				}

				foreach (var dep in newSet)
				{
					if (oldSet.Contains(dep))
						continue;
					if (!ReferenceCache.TryGetValue(dep, out var refs))
					{
						refs = new List<string>();
						ReferenceCache[dep] = refs;
					}
					if (!refs.Contains(path))
						refs.Add(path);
					changed = true;
					dependencyChanged = true;
				}
			}
			else
			{
				foreach (var dep in newSet)
				{
					if (!ReferenceCache.TryGetValue(dep, out var refs))
					{
						refs = new List<string>();
						ReferenceCache[dep] = refs;
					}
					if (!refs.Contains(path))
						refs.Add(path);
				}
				changed = true;
				dependencyChanged = true;
			}

			DependencyCache[path] = new List<string>(newSet);
			return changed;
		}

		private static void RefreshUnusedAndFolderStats()
		{
			var ignorePaths = RAssetFilterSettings.Instance.ignorePaths;
			var unusedAssets = new List<string>();
			var allAssets = AssetDatabase.GetAllAssetPaths();
			foreach (var path in allAssets)
			{
				if (!path.StartsWith("Assets/"))
					continue;
				if (AssetDatabase.IsValidFolder(path))
					continue;
				if (IsIgnored(path, ignorePaths))
					continue;
				if (!ReferenceCache.ContainsKey(path) && !IsRootAsset(path))
					unusedAssets.Add(path);
			}
			UnusedAssetsCache = new HashSet<string>(unusedAssets);
			CalculateFolderStats(unusedAssets);
		}

		private static void CalculateFolderStats(List<string> unusedAssets)
		{
			FolderStatsCache.Clear();
			foreach (var pair in BuildFolderStats(unusedAssets, SizeCache))
				FolderStatsCache[pair.Key] = pair.Value;
		}

		private static Dictionary<string, FolderStats> BuildFolderStats(IEnumerable<string> unusedAssets, IDictionary<string, long> sizes)
		{
			var folderStats = new Dictionary<string, FolderStats>();
			foreach (var path in unusedAssets)
			{
				if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/"))
					throw new InvalidDataException("Unused asset cache contains an invalid project path.");

				if (!sizes.TryGetValue(path, out var size))
				{
					size = ReadFileSize(path);
					sizes[path] = size;
				}

				string dir = Path.GetDirectoryName(path);
				if (string.IsNullOrEmpty(dir))
					throw new InvalidDataException("Unused asset cache path has no parent folder.");
				dir = dir.Replace("\\", "/");

				while (!string.IsNullOrEmpty(dir) && dir.StartsWith("Assets"))
				{
					folderStats.TryGetValue(dir, out var stats);
					stats.unusedFilesCount++;
					stats.unusedSize += size;
					folderStats[dir] = stats;

					dir = Path.GetDirectoryName(dir);
					if (string.IsNullOrEmpty(dir) || dir == "Assets")
						break;
					dir = dir.Replace("\\", "/");
				}
			}
			return folderStats;
		}

		private static bool IsRootAsset(string path)
		{
			// Scenes, Resources, StreamingAssets, EditorDefaultResources are roots
			if (path.EndsWith(".unity")) return true;
			if (path.Contains("/Resources/")) return true;
			if (path.Contains("/Editor/")) return true; // Editor scripts often not referenced but used
			if (path.Contains("/StreamingAssets/")) return true;
			if (path.Contains("/Plugins/")) return true; // Plugins are often entry points
			return false;
		}

		private static bool IsIgnored(string path, List<string> ignorePaths)
		{
			if (ignorePaths == null) return false;
			foreach (var ignore in ignorePaths)
			{
				if (path.Contains(ignore)) return true;
			}
			return false;
		}

		public static int GetReferenceCount(string path)
		{
			return ReferenceCache.TryGetValue(path, out var references) && references != null ? references.Count : 0;
		}

		public static List<string> GetCachedReferencingAssets(IEnumerable<string> pTargetPaths)
		{
			var candidates = new HashSet<string>(StringComparer.Ordinal);
			if (pTargetPaths == null)
				return new List<string>();

			foreach (var targetPath in pTargetPaths)
			{
				if (string.IsNullOrEmpty(targetPath) || !ReferenceCache.TryGetValue(targetPath, out var references) || references == null)
					continue;

				foreach (var reference in references)
					candidates.Add(reference);
			}

			var result = candidates.ToList();
			result.Sort(StringComparer.Ordinal);
			return result;
		}

		public static List<string> FindReferences(string targetPath, bool useCache = true)
		{
			if (useCache)
			{
				if (HasReferenceData)
				{
					return ReferenceCache.TryGetValue(targetPath, out var cachedReferences) && cachedReferences != null
						? cachedReferences
						: new List<string>();
				}

				Debug.LogWarning("Reference Cache is unavailable. Falling back to slow search.");
				useCache = false;
			}

			if (!useCache)
			{
				var references = new List<string>();
				var allAssets = AssetDatabase.GetAllAssetPaths();

				int index = 0;
				int total = allAssets.Length;

				foreach (var assetPath in allAssets)
				{
					if (index % 100 == 0) // Update progress every 100 items
						EditorUtility.DisplayProgressBar("Finding References", $"Checking: {Path.GetFileName(assetPath)}", (float)index / total);
					index++;

					if (assetPath == targetPath) continue;
					if (AssetDatabase.IsValidFolder(assetPath)) continue;

					var deps = AssetDatabase.GetDependencies(assetPath, false);
					if (deps.Contains(targetPath))
					{
						references.Add(assetPath);
					}
				}
				EditorUtility.ClearProgressBar();
				return references;
			}
			return new List<string>();
		}

		/// <summary>Returns the shortest reference chain from an asset that
		/// references targetPath (directly or transitively) up to an Addressable
		/// asset, ordered [referencer, ..., addressableAsset]. Empty when no
		/// Addressable asset reaches targetPath.</summary>
		public static List<string> FindAddressableReferenceChain(string targetPath, int maxDepth = 8)
		{
			if (string.IsNullOrEmpty(targetPath))
				return new List<string>();

			Func<string, List<string>> getReferencers;
			if (HasReferenceData)
			{
				getReferencers = path => ReferenceCache.TryGetValue(path, out var refs) && refs != null ? refs : new List<string>();
			}
			else
			{
				// Slow path: build a temporary reverse map once, then BFS over it.
				var reverse = new Dictionary<string, List<string>>(StringComparer.Ordinal);
				var allAssets = AssetDatabase.GetAllAssetPaths();
				for (int i = 0; i < allAssets.Length; i++)
				{
					var assetPath = allAssets[i];
					if (assetPath == targetPath || AssetDatabase.IsValidFolder(assetPath)) continue;
					if (i % 200 == 0)
						EditorUtility.DisplayProgressBar("Finding Addressable Chain", $"Indexing: {Path.GetFileName(assetPath)}", (float)i / allAssets.Length);
					var deps = AssetDatabase.GetDependencies(assetPath, false);
					for (int d = 0; d < deps.Length; d++)
					{
						if (!reverse.TryGetValue(deps[d], out var list))
							reverse[deps[d]] = list = new List<string>();
						list.Add(assetPath);
					}
				}
				EditorUtility.ClearProgressBar();
				getReferencers = path => reverse.TryGetValue(path, out var refs) && refs != null ? refs : new List<string>();
			}

			var queue = new Queue<List<string>>();
			queue.Enqueue(new List<string> { targetPath });
			var visited = new HashSet<string>(StringComparer.Ordinal) { targetPath };

			while (queue.Count > 0)
			{
				var chain = queue.Dequeue();
				var current = chain[chain.Count - 1];

#if ADDRESSABLES
				if (chain.Count > 1)
				{
					string guid = AssetDatabase.AssetPathToGUID(current);
					if (!string.IsNullOrEmpty(guid) && IsAddressableIncludedInBuild(guid))
						return chain.GetRange(1, chain.Count - 1);
				}
#endif
				if (chain.Count > maxDepth) continue;

				var referencers = getReferencers(current);
				for (int i = 0; i < referencers.Count; i++)
				{
					var referencer = referencers[i];
					if (string.IsNullOrEmpty(referencer) || referencer == targetPath)
						continue;
					if (AssetDatabase.IsValidFolder(referencer))
						continue;
					if (!visited.Add(referencer))
						continue;

					var next = new List<string>(chain.Count + 1);
					next.AddRange(chain);
					next.Add(referencer);
					queue.Enqueue(next);
				}
			}

			return new List<string>();
		}

		public static long GetAssetSize(string path)
		{
			// Check cache first
			if (SizeCache.TryGetValue(path, out long cachedSize))
			{
				return cachedSize;
			}

			// Calculate and cache
			long size = ReadFileSize(path);
			SizeCache[path] = size;
			return size;
		}


		private static long ReadFileSize(string path)
		{
			return File.Exists(path) ? new FileInfo(path).Length : 0;
		}

		public static string GetTotalSizeFormatted(List<string> paths)
		{
			long total = 0;
			foreach (var p in paths) total += GetAssetSize(p);
			return EditorUtility.FormatBytes(total);
		}

		public static List<string> FindReferencesByGuid(string guid)
		{
			if (string.IsNullOrEmpty(guid))
				return new List<string>();

			var allAssets = AssetDatabase.GetAllAssetPaths();
			var textExtensions = new HashSet<string>(RAssetFilterSettings.Instance.deepSearchExtensions.Select(e => e.ToLower()));
			var candidatePaths = new List<string>();
			foreach (var path in allAssets)
			{
				if (AssetDatabase.IsValidFolder(path))
					continue;
				if (textExtensions.Contains(Path.GetExtension(path).ToLower()))
					candidatePaths.Add(path);
			}
			candidatePaths.Sort(StringComparer.Ordinal);

			try
			{
				EditorUtility.DisplayProgressBar("Deep Search", $"Scanning 0/{candidatePaths.Count} text assets", 0f);
				var result = AssetReferenceTextScanner.ScanGuidContentReferences(
					candidatePaths,
					guid,
					128,
					(completed, total) => EditorUtility.DisplayProgressBar(
						"Deep Search",
						$"Scanning {completed}/{total} text assets",
						total == 0 ? 1f : (float)completed / total),
					Directory.GetParent(Application.dataPath).FullName);
				return result.paths;
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}

#if ADDRESSABLES
		private static bool IsAddressableIncludedInBuild(string pGuid)
		{
			if (string.IsNullOrEmpty(pGuid))
				return false;

			var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
			if (settings == null)
				return false;

			if (settings.FindAssetEntry(pGuid, true) == null)
				return false;

			var excludedGroup = settings.FindGroup("Excluded Content");
			return excludedGroup == null || excludedGroup.GetAssetEntry(pGuid, true) == null;
		}
#endif

#region Persistence

		private const string CACHE_PATH = "Library/RAssetFilterCache.json";
		private const string CACHE_TEMP_PATH = CACHE_PATH + ".tmp";
		private const string CACHE_INVALID_PATH = CACHE_PATH + ".invalid";
		private const int CACHE_FORMAT_VERSION = 4;

		[System.Serializable]
		private class CacheData
		{
			public int formatVersion;
			public bool cacheReady;
			public bool unusedDataReady;
			public List<string> unusedAssets;
			public List<string> sizeKeys;
			public List<long> sizeValues;
			public List<string> refKeys;
			public List<RefListWrapper> refValues;
			public List<string> dependencyKeys;
			public List<RefListWrapper> dependencyValues;
		}

		[System.Serializable]
		private class RefListWrapper
		{
			public List<string> refs;
		}

		public static bool SaveCache(List<string> unusedAssets)
		{
			if (!m_unusedDataReady || !m_cacheReady)
			{
				// Do not persist a graph-only build or partial state as a full valid scan.
				DiscardCacheFile();
				return false;
			}

			var data = new CacheData
			{
				formatVersion = CACHE_FORMAT_VERSION,
				cacheReady = m_cacheReady,
				unusedDataReady = m_unusedDataReady,
				unusedAssets = unusedAssets ?? new List<string>(),
			};

			// Serialize SizeCache
			data.sizeKeys = new List<string>(SizeCache.Keys);
			data.sizeValues = new List<long>(SizeCache.Values);

			// Serialize ReferenceCache
			data.refKeys = new List<string>(ReferenceCache.Keys);
			data.refValues = new List<RefListWrapper>();
			foreach (var val in ReferenceCache.Values)
			{
				data.refValues.Add(new RefListWrapper { refs = val ?? new List<string>() });
			}

			// Serialize DependencyCache (forward edges), used as the diff baseline for incremental updates
			data.dependencyKeys = new List<string>(DependencyCache.Keys);
			data.dependencyValues = new List<RefListWrapper>();
			foreach (var val in DependencyCache.Values)
			{
				data.dependencyValues.Add(new RefListWrapper { refs = val ?? new List<string>() });
			}

			string json = JsonUtility.ToJson(data);
			try
			{
				// Write to a temp file first so an interrupted write never leaves partial JSON as the active cache.
				Directory.CreateDirectory(Path.GetDirectoryName(CACHE_PATH));
				File.WriteAllText(CACHE_TEMP_PATH, json);
				if (File.Exists(CACHE_PATH))
					File.Replace(CACHE_TEMP_PATH, CACHE_PATH, null);
				else
					File.Move(CACHE_TEMP_PATH, CACHE_PATH);
				return true;
			}
			catch (Exception ex)
			{
				// The previous cache is still whatever it was, so drop it rather than let it outlive this scan.
				Debug.LogWarning($"RAsset Filter could not save cache: {ex.Message}");
				DiscardCacheFile();
				if (File.Exists(CACHE_TEMP_PATH))
				{
					try
					{
						File.Delete(CACHE_TEMP_PATH);
					}
					catch
					{
						// Nothing else to do: the stale temp file is harmless and gets overwritten on the next save.
					}
				}
				return false;
			}
		}

		public static List<string> LoadCache()
		{
			if (!File.Exists(CACHE_PATH))
			{
				DropCacheState();
				return null;
			}

			CacheData data;
			try
			{
				data = JsonUtility.FromJson<CacheData>(File.ReadAllText(CACHE_PATH));
			}
			catch
			{
				data = null;
			}

			if (!TryBuildCacheState(data, out var unusedAssets, out var sizes, out var references, out var dependencies, out var folderStats))
			{
				// Corrupt, truncated or legacy data must never leave older runtime state in place.
				DiscardCacheFile();
				DropCacheState();
				return null;
			}

			ClearCacheState();
			foreach (var pair in sizes)
				SizeCache[pair.Key] = pair.Value;
			foreach (var pair in references)
				ReferenceCache[pair.Key] = pair.Value;
			foreach (var pair in dependencies)
				DependencyCache[pair.Key] = pair.Value;
			UnusedAssetsCache = new HashSet<string>(unusedAssets);
			foreach (var pair in folderStats)
				FolderStatsCache[pair.Key] = pair.Value;
			m_unusedDataReady = true;
			MarkCacheReady();

			// Imports and moves that arrived before the persisted cache restored can now be applied
			// against it. Both queues must drain here: returning after only one would silently
			// orphan the other, since m_cacheReady is now true and future callbacks stop queuing.
			bool hadPending = false;
			if (PendingMovedFromPaths.Count > 0)
			{
				var from = new List<string>(PendingMovedFromPaths);
				var to = new List<string>(PendingMovedToPaths);
				PendingMovedFromPaths.Clear();
				PendingMovedToPaths.Clear();

				if (UpdateCacheForMovedAssets(from, to))
				{
					// The move migrated the graph's old-path keys. Import events queued against the
					// pre-move path would diff stale data onto the new entry and erase real edges,
					// so translate them to the post-move path before the diff.
					var moved = new Dictionary<string, string>(StringComparer.Ordinal);
					for (int i = 0; i < from.Count; i++)
						moved[from[i]] = to[i];
					var mappedImports = new List<string>(PendingImportedAssets.Count);
					foreach (var path in PendingImportedAssets)
						if (moved.TryGetValue(path, out var mapped))
							mappedImports.Add(mapped);
						else
							mappedImports.Add(path);
					PendingImportedAssets.Clear();
					foreach (var path in mappedImports)
						PendingImportedAssets.Add(path);
				}
				else if (!m_cacheReady)
				{
					// The queued batch failed validation (ambiguous reuse, folder move, out-of-scope
					// path, etc.) and UpdateCacheForMovedAssets fell back to InvalidateCache() instead
					// of applying it. The restore this method just performed is gone; report failure
					// instead of returning a hollow empty list that looks like a valid empty scan.
					return null;
				}
				hadPending = true;
			}

			if (PendingImportedAssets.Count > 0)
			{
				var pending = new List<string>(PendingImportedAssets);
				PendingImportedAssets.Clear();
				UpdateCacheForChangedAssets(pending);
				hadPending = true;
			}

			return hadPending ? new List<string>(UnusedAssetsCache) : unusedAssets;
		}

		private static bool TryBuildCacheState(
			CacheData pData,
			out List<string> pUnusedAssets,
			out Dictionary<string, long> pSizes,
			out Dictionary<string, List<string>> pReferences,
			out Dictionary<string, List<string>> pDependencies,
			out Dictionary<string, FolderStats> pFolderStats)
		{
			pUnusedAssets = null;
			pSizes = null;
			pReferences = null;
			pDependencies = null;
			pFolderStats = null;

			try
			{
				if (pData == null || pData.formatVersion != CACHE_FORMAT_VERSION || !pData.cacheReady || !pData.unusedDataReady)
					return false;
				if (pData.unusedAssets == null || pData.sizeKeys == null || pData.sizeValues == null || pData.refKeys == null || pData.refValues == null)
					return false;
				if (pData.dependencyKeys == null || pData.dependencyValues == null)
					return false;
				if (pData.sizeKeys.Count != pData.sizeValues.Count || pData.refKeys.Count != pData.refValues.Count || pData.dependencyKeys.Count != pData.dependencyValues.Count)
					return false;

				var sizes = new Dictionary<string, long>(pData.sizeKeys.Count);
				for (int i = 0; i < pData.sizeKeys.Count; i++)
				{
					string key = pData.sizeKeys[i];
					if (string.IsNullOrEmpty(key) || sizes.ContainsKey(key))
						return false;
					sizes[key] = pData.sizeValues[i];
				}

				var references = new Dictionary<string, List<string>>(pData.refKeys.Count);
				for (int i = 0; i < pData.refKeys.Count; i++)
				{
					string key = pData.refKeys[i];
					var wrapper = pData.refValues[i];
					// Live code prunes a ReferenceCache entry the moment its list goes empty (see
					// RemoveAssetFromCache/UpdateAssetDependencies), so a key present here with zero
					// entries could not have come from a normal run: restoring it would show "0 refs"
					// while RefreshUnusedAndFolderStats still treats the key's presence as "referenced",
					// hiding an actually-unused asset from the unused list.
					if (string.IsNullOrEmpty(key) || references.ContainsKey(key) || wrapper?.refs == null || wrapper.refs.Count == 0)
						return false;
					references[key] = wrapper.refs;
				}

				var dependencies = new Dictionary<string, List<string>>(pData.dependencyKeys.Count);
				for (int i = 0; i < pData.dependencyKeys.Count; i++)
				{
					string key = pData.dependencyKeys[i];
					var wrapper = pData.dependencyValues[i];
					if (string.IsNullOrEmpty(key) || dependencies.ContainsKey(key) || wrapper?.refs == null)
						return false;
					dependencies[key] = wrapper.refs;
				}

				var unusedAssets = new List<string>(pData.unusedAssets);
				if (new HashSet<string>(unusedAssets, StringComparer.Ordinal).Count != unusedAssets.Count)
					return false;
				var folderStats = BuildFolderStats(unusedAssets, sizes);
				pUnusedAssets = unusedAssets;
				pSizes = sizes;
				pReferences = references;
				pDependencies = dependencies;
				pFolderStats = folderStats;
				return true;
			}
			catch
			{
				return false;
			}
		}

#endregion
	}
}