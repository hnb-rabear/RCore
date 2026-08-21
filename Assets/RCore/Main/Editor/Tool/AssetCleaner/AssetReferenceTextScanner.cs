using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RCore.Editor.AssetCleaner
{
	public static class AssetReferenceTextScanner
	{
		public struct ScanResult
		{
			public readonly List<string> paths;
			public readonly List<string> skippedPaths;

			public ScanResult(List<string> pPaths, List<string> pSkippedPaths)
			{
				paths = pPaths;
				skippedPaths = pSkippedPaths;
			}
		}

		public struct ObjectReferenceTarget
		{
			public readonly string id;
			public readonly string guid;
			public readonly long localFileId;
			public readonly bool requireLocalFileId;

			public ObjectReferenceTarget(string pId, string pGuid, long pLocalFileId, bool pRequireLocalFileId)
			{
				id = pId;
				guid = pGuid;
				localFileId = pLocalFileId;
				requireLocalFileId = pRequireLocalFileId;
			}
		}

		public struct AllTargetScanResult
		{
			public readonly Dictionary<string, List<string>> pathsByTargetId;
			public readonly List<string> skippedPaths;

			public AllTargetScanResult(Dictionary<string, List<string>> pPathsByTargetId, List<string> pSkippedPaths)
			{
				pathsByTargetId = pPathsByTargetId;
				skippedPaths = pSkippedPaths;
			}
		}

		private static readonly Regex ObjectReferenceRegex = new Regex(
			@"\{[^{}]*?\bfileID:\s*(-?\d+)\s*,\s*guid:\s*([^,\s}]+)[^{}]*?\}",
			RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

		public static ScanResult ScanGuidContentReferences(
			IReadOnlyList<string> pPaths,
			string pGuid,
			int pPathsPerBatch = 128,
			Action<int, int> pOnBatchCompleted = null,
			string pBasePath = null)
		{
			if (string.IsNullOrEmpty(pGuid))
				return new ScanResult(new List<string>(), new List<string>());

			return ScanPaths(pPaths, pPathsPerBatch, pOnBatchCompleted, pBasePath, content => content.IndexOf(pGuid, StringComparison.OrdinalIgnoreCase) >= 0);
		}

		public static ScanResult ScanObjectReferences(
			IReadOnlyList<string> pPaths,
			string pGuid,
			long pLocalFileId,
			bool pRequireLocalFileId,
			int pPathsPerBatch = 128,
			Action<int, int> pOnBatchCompleted = null,
			string pBasePath = null)
		{
			if (string.IsNullOrEmpty(pGuid) || (pRequireLocalFileId && pLocalFileId == 0))
				return new ScanResult(new List<string>(), new List<string>());

			return ScanPaths(pPaths, pPathsPerBatch, pOnBatchCompleted, pBasePath, content => ContainsObjectReference(content, pGuid, pLocalFileId, pRequireLocalFileId));
		}

		public static AllTargetScanResult ScanAllObjectReferences(
			IReadOnlyList<string> pPaths,
			IReadOnlyList<ObjectReferenceTarget> pTargets,
			int pPathsPerBatch = 128,
			Action<int, int> pOnBatchCompleted = null,
			string pBasePath = null)
		{
			var exactTargetIds = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
			var fallbackTargetIds = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
			foreach (var target in pTargets)
			{
				if (string.IsNullOrEmpty(target.id) || string.IsNullOrEmpty(target.guid) ||
					(target.requireLocalFileId && target.localFileId == 0))
					continue;

				var lookup = target.requireLocalFileId ? exactTargetIds : fallbackTargetIds;
				var lookupKey = target.requireLocalFileId
					? GetObjectReferenceKey(target.guid, target.localFileId)
					: target.guid;
				if (!lookup.TryGetValue(lookupKey, out var targetIds))
				{
					targetIds = new List<string>();
					lookup.Add(lookupKey, targetIds);
				}
				if (!targetIds.Contains(target.id))
					targetIds.Add(target.id);
			}

			var matchesByTargetId = new ConcurrentDictionary<string, ConcurrentDictionary<string, byte>>(StringComparer.Ordinal);
			var skippedPaths = new ConcurrentBag<string>();
			var batchSize = Math.Max(1, pPathsPerBatch);
			for (var batchStart = 0; batchStart < pPaths.Count; batchStart += batchSize)
			{
				var batchEnd = Math.Min(batchStart + batchSize, pPaths.Count);
				Parallel.For(batchStart, batchEnd, pathIndex =>
				{
					var path = pPaths[pathIndex];
					try
					{
						var fullPath = string.IsNullOrEmpty(pBasePath) || Path.IsPathRooted(path)
							? path
							: Path.Combine(pBasePath, path);
						var content = File.ReadAllText(fullPath);
						var matchedTargetIds = new HashSet<string>(StringComparer.Ordinal);
						foreach (Match match in ObjectReferenceRegex.Matches(content))
						{
							if (!long.TryParse(match.Groups[1].Value, out var localFileId))
								continue;
							var guid = match.Groups[2].Value;
							if (exactTargetIds.TryGetValue(GetObjectReferenceKey(guid, localFileId), out var exactIds))
								matchedTargetIds.UnionWith(exactIds);
							if (fallbackTargetIds.TryGetValue(guid, out var fallbackIds))
								matchedTargetIds.UnionWith(fallbackIds);
						}

						foreach (var targetId in matchedTargetIds)
							matchesByTargetId.GetOrAdd(targetId, _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal)).TryAdd(path, 0);
					}
					catch (Exception ex)
					{
						skippedPaths.Add($"{path} ({ex.Message})");
					}
				});
				pOnBatchCompleted?.Invoke(batchEnd, pPaths.Count);
			}

			var pathsByTargetId = new Dictionary<string, List<string>>(StringComparer.Ordinal);
			foreach (var pair in matchesByTargetId)
			{
				var paths = new List<string>(pair.Value.Keys);
				paths.Sort(StringComparer.Ordinal);
				pathsByTargetId[pair.Key] = paths;
			}
			var resultSkippedPaths = new List<string>(skippedPaths);
			resultSkippedPaths.Sort(StringComparer.Ordinal);
			return new AllTargetScanResult(pathsByTargetId, resultSkippedPaths);
		}

		private static ScanResult ScanPaths(IReadOnlyList<string> pPaths, int pPathsPerBatch, Action<int, int> pOnBatchCompleted, string pBasePath, Func<string, bool> pIsMatch)
		{
			var matches = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
			var skippedPaths = new ConcurrentBag<string>();
			var batchSize = Math.Max(1, pPathsPerBatch);

			for (int batchStart = 0; batchStart < pPaths.Count; batchStart += batchSize)
			{
				var batchEnd = Math.Min(batchStart + batchSize, pPaths.Count);
				Parallel.For(batchStart, batchEnd, pathIndex =>
				{
					var path = pPaths[pathIndex];
					try
					{
						var fullPath = string.IsNullOrEmpty(pBasePath) || Path.IsPathRooted(path)
							? path
							: Path.Combine(pBasePath, path);
						if (pIsMatch(File.ReadAllText(fullPath)))
							matches.TryAdd(path, 0);
					}
					catch (Exception ex)
					{
						skippedPaths.Add($"{path} ({ex.Message})");
					}
				});
				pOnBatchCompleted?.Invoke(batchEnd, pPaths.Count);
			}

			var resultPaths = new List<string>(matches.Keys);
			resultPaths.Sort(StringComparer.Ordinal);
			var resultSkippedPaths = new List<string>(skippedPaths);
			resultSkippedPaths.Sort(StringComparer.Ordinal);
			return new ScanResult(resultPaths, resultSkippedPaths);
		}

		private static string GetObjectReferenceKey(string pGuid, long pLocalFileId)
		{
			return $"{pGuid}:{pLocalFileId}";
		}

		private static bool ContainsObjectReference(string pContent, string pGuid, long pLocalFileId, bool pRequireLocalFileId)
		{
			foreach (Match match in ObjectReferenceRegex.Matches(pContent))
			{
				if (!string.Equals(match.Groups[2].Value, pGuid, StringComparison.OrdinalIgnoreCase))
					continue;
				if (!pRequireLocalFileId)
					return true;
				if (long.TryParse(match.Groups[1].Value, out var localFileId) && localFileId == pLocalFileId)
					return true;
			}
			return false;
		}
	}
}
