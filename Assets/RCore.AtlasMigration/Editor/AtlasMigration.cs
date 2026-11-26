using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCore.Editor.Tool
{
	/// <summary>
	/// Moves loose sprite textures to the sprites of a packed TexturePacker atlas and keeps their pivots and 9-slice borders.
	/// Called by an AI agent through UnityMCP; see Editor/AI/unity-texturepacker-atlas-migration/SKILL.md in this package.
	/// </summary>
	public static class AtlasMigration
	{
		private const string SCAN_FILTER = "t:GameObject t:ScriptableObject t:Scene t:AnimationClip";

		/// <summary>One old sprite and the atlas sprite that replaces it.</summary>
		[Serializable] // JsonUtility.ToJson prints it; Newtonsoft loops on Vector2.normalized
		public class Pair
		{
			/// <summary>Old texture asset path.</summary>
			public string oldPath;
			/// <summary>Old sprite name.</summary>
			public string oldName;
			/// <summary>Atlas sprite name, or null.</summary>
			public string newName;
			/// <summary>The individualSpriteSettings key in the .tps, or the key WriteTps will add. Null when the key is ambiguous or the file is on another drive than the .tps.</summary>
			public string tpsKey;
			/// <summary>ok, pivot mismatch, border mismatch, size differs, missing in atlas, or a key error.</summary>
			public string status;
			/// <summary>Normalized pivots (0..1, y up).</summary>
			public Vector2 oldPivot, newPivot;
			/// <summary>Sprite rect sizes in pixels.</summary>
			public Vector2 oldSize, newSize;
			/// <summary>Unity borders (left, bottom, right, top) in pixels.</summary>
			public Vector4 oldBorder, newBorder;
			/// <summary>Pixels per unit.</summary>
			public float oldPixelsPerUnit, newPixelsPerUnit;
		}

		/// <summary>Result of every AtlasMigration call. The agent prints it with JsonUtility.ToJson(report, true).</summary>
		[Serializable]
		public class Report
		{
			/// <summary>One row per old sprite that has a single atlas match.</summary>
			public List<Pair> pairs = new List<Pair>();
			/// <summary>Old sprites with no atlas match, or a duplicate name.</summary>
			public List<string> unmatchedOld = new List<string>();
			/// <summary>Atlas sprites no old sprite pairs with.</summary>
			public List<string> unmatchedNew = new List<string>();
			/// <summary>Warnings and errors.</summary>
			public List<string> notes = new List<string>();
			/// <summary>Plan: assets that reference old sprites. Apply: assets changed.</summary>
			public List<string> assets = new List<string>();
		}

		/// <summary>Pairs old and atlas sprites, compares pivots and borders, and lists the assets that reference old sprites. Changes nothing except an AssetDatabase refresh.</summary>
		public static Report Plan(string oldFolder, string atlasTexturePath, string tpsPath, string[] scope = null)
		{
			AssetDatabase.Refresh();
			var report = new Report();
			var spritePairs = Pairs(report, oldFolder, atlasTexturePath);
			MatchKeys(report, tpsPath);
			CheckTpsheet(report, atlasTexturePath);
			var mappings = spritePairs.Select(p => ((Object)p.oldSprite, (Object)p.newSprite)).ToList();
			var changes = GuidReplace.Scan(mappings, ScopeGuids(scope, report));
			if (changes == null)
			{
				report.notes.Add("Scan cancelled; assets is incomplete. Run Plan again.");
				return report;
			}
			report.assets = changes.Select(c => c.path).ToList();
			return report;
		}

		internal static List<(Sprite oldSprite, Sprite newSprite)> Pairs(Report pReport, string pOldFolder, string pAtlasTexturePath)
		{
			s_folderNames.Clear();
			var atlas = AssetDatabase.LoadAllAssetsAtPath(pAtlasTexturePath).OfType<Sprite>().ToList();
			if (atlas.Count == 0)
				pReport.notes.Add($"No sprites at {pAtlasTexturePath}. Is the TexturePacker Importer installed and the atlas published?");
			// The atlas PNG often sits inside the old folder (farm_city: Cars/tps_cars.png beside Cars/10_mercedes_maybach/...).
			var olds = AssetDatabase.FindAssets("t:Sprite", new[] { pOldFolder })
				.Select(AssetDatabase.GUIDToAssetPath).Distinct()
				.Where(path => path != pAtlasTexturePath)
				.SelectMany(path => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
				.ToList();

			var candidates = new List<(Sprite oldSprite, Sprite newSprite)>();
			foreach (var old in olds)
			{
				string oldPath = AssetDatabase.GetAssetPath(old);
				if (AssetDatabase.LoadAllAssetsAtPath(oldPath).OfType<Sprite>().Count() > 1)
				{
					pReport.unmatchedOld.Add($"{oldPath}:{old.name} (sheet with several sprites; not supported)");
					continue;
				}
				var matches = atlas.Where(a => IsNameMatch(old, a)).ToList();
				if (matches.Count == 1)
					candidates.Add((old, matches[0]));
				else
					pReport.unmatchedOld.Add(matches.Count == 0 ? oldPath : $"{oldPath} (ambiguous: {string.Join(", ", matches.Select(m => m.name))})");
			}

			// Two old sprites claiming one atlas sprite (same name in two folders, no folder prefix): skip both.
			var result = new List<(Sprite, Sprite)>();
			foreach (var group in candidates.GroupBy(c => c.newSprite))
			{
				if (group.Count() > 1)
				{
					pReport.unmatchedOld.AddRange(group.Select(c => $"{AssetDatabase.GetAssetPath(c.oldSprite)} (shares atlas sprite {group.Key.name})"));
					continue;
				}
				var (old, nw) = group.First();
				result.Add((old, nw));
				var pair = new Pair
				{
					oldPath = AssetDatabase.GetAssetPath(old), oldName = old.name, newName = nw.name,
					oldPivot = Pivot01(old), newPivot = Pivot01(nw),
					oldSize = old.rect.size, newSize = nw.rect.size,
					oldBorder = old.border, newBorder = nw.border,
					oldPixelsPerUnit = old.pixelsPerUnit, newPixelsPerUnit = nw.pixelsPerUnit,
				};
				pair.status = Status(pair.oldSize, pair.newSize, pair.oldPivot, pair.newPivot, pair.oldBorder, pair.newBorder, true);
				if (!Mathf.Approximately(pair.oldPixelsPerUnit, pair.newPixelsPerUnit))
					pReport.notes.Add($"{old.name}: pixels per unit {pair.oldPixelsPerUnit} -> {pair.newPixelsPerUnit}; SpriteRenderers will change size.");
				pReport.pairs.Add(pair);
			}
			var used = new HashSet<Sprite>(candidates.Select(c => c.newSprite));
			pReport.unmatchedNew.AddRange(atlas.Where(a => !used.Contains(a)).Select(a => a.name));
			return result;
		}

		/// <summary>Pivot relative to the full sprite rect (0..1, y up). Not ComponentHelper.NormalizedPivot: that divides by bounds, which shrink with a Tight mesh.</summary>
		internal static Vector2 Pivot01(Sprite pSprite)
			=> new Vector2(pSprite.pivot.x / pSprite.rect.width, pSprite.pivot.y / pSprite.rect.height);

		private static readonly Dictionary<int, string> s_folderNames = new Dictionary<int, string>();

		/// <summary>Same name, or one name is "{folder}-{name}" of the other (e.g. "Core-AS_227" and "Core/AS_227"). A copy of the name match in rcore.main's Replace Sprite window.</summary>
		internal static bool IsNameMatch(Sprite pA, Sprite pB)
			=> pA.name == pB.name
				|| pA.name.EndsWith($"{FolderName(pB)}-{pB.name}")
				|| pB.name.EndsWith($"{FolderName(pA)}-{pA.name}");

		/// <summary>Name of the asset's immediate parent folder. Cached per Pairs call.</summary>
		private static string FolderName(Object pObj)
		{
			int id = pObj.GetInstanceID();
			if (!s_folderNames.TryGetValue(id, out string folder))
				s_folderNames[id] = folder = Path.GetFileName(Path.GetDirectoryName(AssetDatabase.GetAssetPath(pObj)));
			return folder;
		}

		internal static string Status(Vector2 pOldSize, Vector2 pNewSize, Vector2 pOldPivot, Vector2 pNewPivot, Vector4 pOldBorder, Vector4 pNewBorder, bool pHasNew)
		{
			if (!pHasNew)
				return "missing in atlas";
			bool pivotOk = Mathf.Abs(pOldPivot.x - pNewPivot.x) <= 0.01f && Mathf.Abs(pOldPivot.y - pNewPivot.y) <= 0.01f;
			if (Vector2.Distance(pOldSize, pNewSize) > 0.5f)
				return pivotOk ? "size differs" : "size differs, pivot mismatch";
			if (!pivotOk)
				return "pivot mismatch";
			for (int i = 0; i < 4; i++)
				if (Mathf.Abs(pOldBorder[i] - pNewBorder[i]) > 1f)
					return "border mismatch";
			return "ok";
		}

		private static void MatchKeys(Report pReport, string pTpsPath)
		{
			TpsFile tps;
			try
			{
				tps = TpsFile.Parse(File.ReadAllText(pTpsPath));
			}
			catch (Exception e)
			{
				pReport.notes.Add($"Can't read {pTpsPath}: {e.Message}");
				return;
			}
			var keys = tps.Keys;
			string tpsDir = Path.GetDirectoryName(pTpsPath);
			foreach (var pair in pReport.pairs)
			{
				pair.tpsKey = ResolveKey(keys, pair.oldPath, pair.oldName, tpsDir, false, out var error, out bool add);
				if (add && !tps.Packs(tpsDir, pair.oldPath))
				{
					pair.tpsKey = null;
					pair.status = $"{pair.status}; .tps fileList does not include this file";
				}
				else if (pair.tpsKey == null && !add)
					pair.status = $"{pair.status}; .tps {error}";
			}
		}

		private const string ANOTHER_DRIVE = "file is on another drive than the .tps";

		/// <summary>The .tps key for a sprite. Agent path: a key that points to the file, else the key WriteTps will add. Window path: a key that points to the file, else the suffix rule, else the sprite name; never a new key.</summary>
		internal static string ResolveKey(IReadOnlyList<string> pKeys, string pAssetPath, string pSpriteName, string pTpsDir, bool pMatchByName, out string pError, out bool pAdd)
		{
			pAdd = false;
			string key = TpsFile.FindKey(pKeys, pAssetPath, out pError, pTpsDir);
			if (key != null)
				return key;
			if (pMatchByName)
			{
				key = TpsFile.FindKey(pKeys, pAssetPath, out pError);
				return key ?? TpsFile.FindKeyByName(pKeys, pSpriteName, out pError);
			}
			if (!pError.StartsWith("no key", StringComparison.Ordinal))
				return null;
			key = TpsFile.RelativeKey(pTpsDir, pAssetPath);
			if (key.Contains(":"))
			{
				pError = ANOTHER_DRIVE;
				return null;
			}
			pAdd = true;
			return key;
		}

		private static void CheckTpsheet(Report pReport, string pAtlasTexturePath)
		{
			string sheet = Path.ChangeExtension(pAtlasTexturePath, ".tpsheet");
			if (!File.Exists(sheet))
			{
				pReport.notes.Add($"No {sheet}. The TexturePacker Importer reads pivots from it.");
				return;
			}
			string header = string.Join("\n", File.ReadLines(sheet).TakeWhile(l => l.StartsWith("#") || l.StartsWith(":") || l.Length == 0));
			if (header.Contains(":pivotpoints=disabled"))
				pReport.notes.Add(".tpsheet has pivot points disabled. Publish again after WriteTps.");
			if (header.Contains(":borders=disabled") && pReport.pairs.Any(p => p.oldBorder != Vector4.zero))
				pReport.notes.Add(".tpsheet has borders disabled but some old sprites have 9-slice borders. Publish again after WriteTps.");
		}

		internal static string[] ScopeGuids(string[] pScope, Report pReport)
		{
			if (pScope == null || pScope.Length == 0)
				return AssetDatabase.FindAssets(SCAN_FILTER, new[] { "Assets" });
			var guids = new HashSet<string>();
			foreach (string path in pScope)
			{
				if (AssetDatabase.IsValidFolder(path))
				{
					guids.UnionWith(AssetDatabase.FindAssets(SCAN_FILTER, new[] { path }));
					continue;
				}
				string guid = AssetDatabase.AssetPathToGUID(path);
				if (string.IsNullOrEmpty(guid))
					pReport.notes.Add($"Scope path not found: {path}");
				else
					guids.Add(guid);
			}
			return guids.ToArray();
		}

		/// <summary>Writes each old sprite's pivot and 9-slice border into the .tps. Writes nothing when no value changes.</summary>
		public static Report WriteTps(string oldFolder, string atlasTexturePath, string tpsPath)
		{
			var report = new Report();
			var olds = Pairs(report, oldFolder, atlasTexturePath).Select(p => p.oldSprite).ToList();
			report.notes.AddRange(WriteTps(tpsPath, olds, tpsPath));
			return report;
		}

		/// <summary>Writes pivots and borders of <paramref name="pOldSprites"/> into the .tps at <paramref name="pTpsPath"/> and saves to <paramref name="pOutputPath"/>. Returns notes.</summary>
		/// <param name="pMatchByName">Window only: when no key ends with the sprite's path, use the one key with the sprite's name.</param>
		internal static List<string> WriteTps(string pTpsPath, IList<Sprite> pOldSprites, string pOutputPath, bool pMatchByName = false)
		{
			var notes = new List<string>();
			TpsFile tps;
			try
			{
				tps = TpsFile.Parse(File.ReadAllText(pTpsPath));
			}
			catch (Exception e)
			{
				notes.Add($"Can't read {pTpsPath}: {e.Message}");
				return notes;
			}
			if (tps.EnsureWritePivotPoints())
				notes.Add("writePivotPoints was off; turned it on.");
			var keys = tps.Keys;
			string tpsDir = Path.GetDirectoryName(pTpsPath);
			foreach (var sprite in pOldSprites)
			{
				string path = AssetDatabase.GetAssetPath(sprite);
				string key = ResolveKey(keys, path, sprite.name, tpsDir, pMatchByName, out var error, out bool add);
				if (add && !tps.Packs(tpsDir, path))
				{
					notes.Add($"{sprite.name}: {path} is not in the .tps fileList; TexturePacker packs another copy or none. Add its folder to the .tps, or pass the copy TexturePacker packs as oldFolder.");
					continue;
				}
				if (add)
				{
					if (tps.AddKey(key, Mathf.RoundToInt(sprite.rect.width), Mathf.RoundToInt(sprite.rect.height)))
					{
						const string STALE = "ignored by TexturePacker: ";
						int stale = error.IndexOf(STALE, StringComparison.Ordinal);
						notes.Add(stale < 0 ? $"{sprite.name}: added .tps entry {key}."
							: $"{sprite.name}: added .tps entry {key}; stale key(s) ignored by TexturePacker: {error.Substring(stale + STALE.Length).TrimEnd(')')}");
					}
					else
						key = null; // no sprite map in the .tps: report the "no key" error below
				}
				else if (key == null && error == ANOTHER_DRIVE)
				{
					notes.Add($"{sprite.name}: {path} is on another drive than the .tps; add it in TexturePacker.");
					continue;
				}
				if (key == null)
				{
					notes.Add($"{sprite.name}: {error}");
					continue;
				}
				var pivot = Pivot01(sprite);
				if (!tps.SetPivot(key, pivot.x, pivot.y))
				{
					notes.Add($"{sprite.name}: key {key} has no pivot setting. Set it in TexturePacker once, save, and run again.");
					continue;
				}
				var b = sprite.border;
				if (b != Vector4.zero
					&& !tps.SetBorder(key, Mathf.RoundToInt(b.x), Mathf.RoundToInt(b.y), Mathf.RoundToInt(b.z), Mathf.RoundToInt(b.w),
						Mathf.RoundToInt(sprite.rect.width), Mathf.RoundToInt(sprite.rect.height)))
					notes.Add($"{sprite.name}: key {key} has no 9-slice settings; border not written.");
			}
			if (!tps.Changed && pOutputPath == pTpsPath)
			{
				notes.Add("No value changed; .tps not written.");
				return notes;
			}
			File.WriteAllText(pOutputPath, tps.ToText(), new System.Text.UTF8Encoding(false));
			notes.Add($"Wrote {pOutputPath}.");
			return notes;
		}

		/// <summary>Entry point for RCore's Replace Sprite window, which calls it by reflection. Writes the pivots and 9-slice borders of <paramref name="oldSprites"/> into the .tps and saves to <paramref name="outputPath"/>. Never adds entries. Returns notes. Changing this signature is a breaking change.</summary>
		public static string[] WriteTpsForWindow(string tpsPath, Sprite[] oldSprites, string outputPath)
			=> WriteTps(tpsPath, oldSprites, outputPath, true).ToArray();

		/// <summary>Replaces references to old sprites with their atlas sprites in the scope. Skips assets open in a scene or Prefab Mode.</summary>
		public static Report Apply(string oldFolder, string atlasTexturePath, string tpsPath, string[] scope = null)
		{
			AssetDatabase.Refresh();
			var report = new Report();
			var mappings = Pairs(report, oldFolder, atlasTexturePath).Select(p => ((Object)p.oldSprite, (Object)p.newSprite)).ToList();
			var changes = GuidReplace.Scan(mappings, ScopeGuids(scope, report));
			if (changes == null)
			{
				report.notes.Add("Scan cancelled; nothing replaced.");
				return report;
			}
			foreach (var open in changes.Where(c => GuidReplace.IsOpen(c.path)).ToList())
			{
				report.notes.Add($"Skipped {open.path}: open in a scene or Prefab Mode. Save, close it, and run Apply again.");
				changes.Remove(open);
			}
			if (changes.Count > 0)
				report.notes.Add(GuidReplace.Apply(mappings, changes.Select(c => c.path).ToList()));
			report.assets = changes.Select(c => c.path).ToList();
			return report;
		}
	}
}
