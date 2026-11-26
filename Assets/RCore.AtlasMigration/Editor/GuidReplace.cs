using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace RCore.Editor.Tool
{
	/// <summary>Finds and rewrites "fileID: X, guid: Y" references in asset text files. A copy of rcore.main's GUID replace code, so this package needs no RCore.</summary>
	internal static class GuidReplace
	{
		private const string TOOLKIT_ASSET_TYPE = "FindAndReplaceAssetToolkit";
		private static readonly Regex s_guidRefRegex = new Regex(@"fileID: -?\d+, guid: [0-9a-f]{32}", RegexOptions.Compiled);

		/// <summary>Assets in the scope that reference an old object, with the number of references. Null when the user cancels the scan.</summary>
		public static List<(string path, int count)> Scan(IList<(Object oldObj, Object newObj)> pMappings, string[] pScopeGuids)
		{
			var refs = BuildRefs(pMappings);
			var result = new List<(string path, int count)>();
			if (refs.Count == 0)
				return result;
			var oldGuids = new HashSet<string>(refs.Keys.Select(r => r.Substring(r.Length - 32))); // a ref ends with its 32-char guid
			var seen = new HashSet<string>();
			try
			{
				for (int i = 0; i < pScopeGuids.Length; i++)
				{
					string path = AssetDatabase.GUIDToAssetPath(pScopeGuids[i]);
					if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path) || !seen.Add(path))
						continue;
					// RCore's Replace Sprite window saves its Old/New pairs in this asset; rewriting it would turn every pair into New -> New.
					// Matched by name so this package needs no reference to rcore.editor.
					if (AssetDatabase.GetMainAssetTypeAtPath(path)?.Name == TOOLKIT_ASSET_TYPE)
						continue;
					if (EditorUtility.DisplayCancelableProgressBar("Scanning references", path, (float)i / pScopeGuids.Length))
						return null; // a partial result would look complete
					if (!AssetDatabase.GetDependencies(path, false).Any(d => oldGuids.Contains(AssetDatabase.AssetPathToGUID(d))))
						continue;
					string text = File.ReadAllText(path);
					int count = ReplaceInText(ref text, refs);
					if (count > 0)
						result.Add((path, count));
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
			return result.OrderBy(r => r.path, StringComparer.Ordinal).ToList();
		}

		/// <summary>Rewrites the references in the given files. Returns "N replaced (M references) · S skipped · F failed".</summary>
		public static string Apply(IList<(Object oldObj, Object newObj)> pMappings, IList<string> pPaths)
		{
			var refs = BuildRefs(pMappings);
			int replaced = 0, skipped = 0, failed = 0, references = 0;
			AssetDatabase.SaveAssets(); // otherwise Refresh() reloads dirty assets from disk and drops their unsaved edits
			AssetDatabase.StartAssetEditing();
			try
			{
				for (int i = 0; i < pPaths.Count; i++)
				{
					string path = pPaths[i];
					EditorUtility.DisplayProgressBar("Replacing references", path, (float)i / pPaths.Count);
					try
					{
						string text = File.ReadAllText(path);
						int count = ReplaceInText(ref text, refs);
						if (count == 0)
						{
							skipped++;
							continue;
						}
						File.WriteAllText(path, text);
						replaced++;
						references += count;
					}
					catch (Exception e)
					{
						UnityEngine.Debug.LogError($"[AtlasMigration] {path}: {e.Message}");
						failed++;
					}
				}
			}
			finally
			{
				AssetDatabase.StopAssetEditing();
				EditorUtility.ClearProgressBar();
				AssetDatabase.Refresh();
			}
			return $"{replaced} replaced ({references} references) · {skipped} skipped · {failed} failed";
		}

		/// <summary>True when the asset is an open scene or the asset of the open Prefab Mode stage.</summary>
		public static bool IsOpen(string pPath)
		{
			for (int i = 0; i < SceneManager.sceneCount; i++)
				if (SceneManager.GetSceneAt(i).path == pPath)
					return true;
			var stage = PrefabStageUtility.GetCurrentPrefabStage();
			return stage != null && stage.assetPath == pPath;
		}

		private static Dictionary<string, string> BuildRefs(IEnumerable<(Object oldObj, Object newObj)> pMappings)
		{
			var refs = new Dictionary<string, string>();
			foreach (var (oldObj, newObj) in pMappings)
			{
				string oldRef = Ref(oldObj);
				string newRef = Ref(newObj);
				if (oldRef != null && newRef != null && oldRef != newRef)
					refs[oldRef] = newRef;
			}
			return refs;
		}

		private static string Ref(Object pObj)
		{
			if (pObj == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(pObj, out string guid, out long fileId))
				return null;
			return $"fileID: {fileId}, guid: {guid}";
		}

		/// <summary>Replaces every old ref in one pass (swapped pairs never chain). Returns how many refs were replaced.</summary>
		private static int ReplaceInText(ref string pContent, IReadOnlyDictionary<string, string> pRefs)
		{
			int count = 0;
			pContent = s_guidRefRegex.Replace(pContent, match =>
			{
				if (!pRefs.TryGetValue(match.Value, out var newRef))
					return match.Value;
				count++;
				return newRef;
			});
			return count;
		}
	}
}
