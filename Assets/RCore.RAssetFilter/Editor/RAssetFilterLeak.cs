using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace RCore.RAssetFilter.Editor
{
	public enum LeakDirection
	{
		/// <summary>Asset inside the boundary is referenced by content outside it.</summary>
		LeakedIn,
		/// <summary>Asset outside the boundary is pulled in as a dependency of boundary content.</summary>
		LeakedOut,
	}

	public class LeakEntry
	{
		public string assetPath;
		public LeakDirection direction;
		/// <summary>LeakedIn: external referencers. LeakedOut: boundary assets owning the dependency.</summary>
		public List<string> relatedPaths = new List<string>();
	}

	public static class RAssetFilterLeak
	{
		/// <summary>Folders and prefabs from the current Project-window selection.</summary>
		public static List<string> GetValidSelection(out int pFolderCount, out int pPrefabCount)
		{
			pFolderCount = 0;
			pPrefabCount = 0;
			var paths = new List<string>();
			foreach (var obj in Selection.objects)
			{
				string path = AssetDatabase.GetAssetPath(obj);
				if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/"))
					continue;
				if (AssetDatabase.IsValidFolder(path))
				{
					pFolderCount++;
					paths.Add(path);
				}
				else if (path.EndsWith(".prefab"))
				{
					pPrefabCount++;
					paths.Add(path);
				}
			}
			return paths;
		}

		/// <summary>Boundary = every asset inside selected folders (recursive) + directly selected prefabs.</summary>
		public static HashSet<string> BuildBoundary(List<string> pSelectionPaths)
		{
			var boundary = new HashSet<string>();
			foreach (var path in pSelectionPaths)
			{
				if (AssetDatabase.IsValidFolder(path))
				{
					var guids = AssetDatabase.FindAssets("", new[] { path });
					foreach (var guid in guids)
					{
						string assetPath = AssetDatabase.GUIDToAssetPath(guid);
						if (!AssetDatabase.IsValidFolder(assetPath))
							boundary.Add(assetPath);
					}
				}
				else
				{
					boundary.Add(path);
				}
			}
			return boundary;
		}

		/// <summary>
		/// Requires RAssetFilter.ReferenceCache to be built (caller's responsibility).
		/// LeakedIn: boundary asset referenced from outside. LeakedOut: external dependency of a boundary asset.
		/// </summary>
		public static List<LeakEntry> DetectLeaks(HashSet<string> pBoundary)
		{
			var results = new List<LeakEntry>();
			var leakedOutMap = new Dictionary<string, LeakEntry>();
			int index = 0;
			int total = pBoundary.Count;

			try
			{
				foreach (var asset in pBoundary)
				{
					if (index % 20 == 0)
						EditorUtility.DisplayProgressBar("Scanning Leaks", Path.GetFileName(asset), (float)index / total);
					index++;

					if (!IsReportable(asset))
						continue;

					// Leaked In: who references this boundary asset from outside?
					if (RAssetFilter.ReferenceCache.TryGetValue(asset, out var referencers))
					{
						var external = referencers
							.Where(r => !pBoundary.Contains(r) && IsReportable(r))
							.OrderBy(r => r)
							.ToList();
						if (external.Count > 0)
						{
							results.Add(new LeakEntry
							{
								assetPath = asset,
								direction = LeakDirection.LeakedIn,
								relatedPaths = external,
							});
						}
					}

					// Leaked Out: which external assets does this boundary asset pull in?
					var deps = AssetDatabase.GetDependencies(asset, true);
					foreach (var dep in deps)
					{
						if (dep == asset || pBoundary.Contains(dep) || !IsReportable(dep))
							continue;
						if (!leakedOutMap.TryGetValue(dep, out var entry))
						{
							entry = new LeakEntry
							{
								assetPath = dep,
								direction = LeakDirection.LeakedOut,
							};
							leakedOutMap[dep] = entry;
							results.Add(entry);
						}
						if (!entry.relatedPaths.Contains(asset))
							entry.relatedPaths.Add(asset);
					}
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
			return results;
		}

		private static bool IsReportable(string pPath)
		{
			// Excludes built-in resources, Packages/ and ProjectSettings/ referencers
			if (!pPath.StartsWith("Assets/"))
				return false;
			string ext = Path.GetExtension(pPath).ToLower();
			foreach (var ignored in RAssetFilterSettings.Instance.leakIgnoreExtensions)
			{
				if (ext == ignored.ToLower())
					return false;
			}
			return true;
		}
	}
}
