using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
#if !TPS_BYTE_DECIMAL && !TPS_DECIMAL_BYTE
using tpsByteByte;
#elif TPS_BYTE_DECIMAL
using tpsByteDecimal;
#elif TPS_DECIMAL_BYTE
using tpsDecimalByte;
#endif

namespace RCore.Editor.Tool
{
	/// <summary>Pairs Old sprites with New sprites, fixes pivots, writes pivots to TexturePacker, and replaces Old with New in the scope.</summary>
	[Serializable]
	public class SpriteReplacer
	{
		public enum Tps
		{
			ByteByte,
			ByteDecimal,
			DecimalByte
		}

		/// <summary>One pair. spriteOriginal = Old (in use now), spriteNew = New (replacement). Names kept for saved data.</summary>
		[Serializable]
		public class Input
		{
			public Sprite spriteNew;
			public Sprite spriteOriginal;
			public Vector2 PivotNew => spriteNew != null ? MathHelper.Round(spriteNew.NormalizedPivot(), 2) : Vector2.one * 0.5f;
			public Vector2 PivotOriginal => spriteOriginal != null ? MathHelper.Round(spriteOriginal.NormalizedPivot(), 2) : Vector2.one * 0.5f;
			public Vector2 SizeNew => spriteNew != null ? spriteNew.NativeSize() : Vector2.zero;
			public Vector2 SizeOriginal => spriteOriginal != null ? spriteOriginal.NativeSize() : Vector2.zero;
			public float TrisNew => spriteNew != null ? spriteNew.triangles.Length / 3f : 0;
			public float TrisOriginal => spriteOriginal != null ? spriteOriginal.triangles.Length / 3f : 0;
			public bool IsMatched => spriteNew != null && spriteOriginal != null;

			public void Swap()
			{
				(spriteNew, spriteOriginal) = (spriteOriginal, spriteNew);
			}
		}

		private const string SCAN_FILTER = "t:GameObject t:ScriptableObject t:Scene t:AnimationClip";
		private const string TRIS_TOOLTIP = "New ÷ Old mesh triangles. Above 1 = heavier.";

		[SerializeField] private List<Input> m_inputs = new List<Input>();
		[SerializeField] private bool m_displayIcon = true;
		[SerializeField] private Object m_tpsSaveFile;
		[SerializeField] private bool m_findSameName = true;
		[SerializeField] private bool m_detectNamePattern;
		[SerializeField] private Tps m_tps;
		[SerializeField] private bool m_displayNullR;
		[SerializeField] private ReplaceScope m_scope = new ReplaceScope();
		private string m_search = "";
		private ReplaceFlow m_flow;

		public float GetTotalTrisNew() => m_inputs.Sum(x => x.TrisNew);
		public float GetTotalTrisOld() => m_inputs.Sum(x => x.TrisOriginal);

		public void Draw()
		{
			m_flow ??= new ReplaceFlow();
			m_scope ??= new ReplaceScope();
			// Steps 1 and 3 are always open; only step 2 is a foldout (HeaderFoldout starts collapsed).
			EditorGUILayout.LabelField("1 · Pairs", EditorStyles.boldLabel);
			DrawPairs();
			EditorGUILayout.Space(6);
			DrawTpsSection();
			EditorGUILayout.Space(6);
			EditorGUILayout.LabelField("3 · Replace Old with New", EditorStyles.boldLabel);
			var mappings = m_inputs.Where(x => x.IsMatched).Select(x => ((Object)x.spriteOriginal, (Object)x.spriteNew)).ToList();
			m_flow.Draw(nameof(SpriteReplacer), m_scope, ReplaceFlow.Fingerprint(m_scope, mappings),
				mappings.Count == 0 ? "Add at least one pair with both an Old and a New sprite." : null,
				() => ReplaceFlow.ScanGuidRefs(mappings, m_scope.GetGuids(SCAN_FILTER)),
				changes => ReplaceFlow.ApplyGuidRefs(mappings, changes));
		}

		private void DrawPairs()
		{
			EditorGUILayout.BeginHorizontal();
			var droppedOld = ReplaceFlow.DropZone("Drop OLD sprites\n(sprites, textures, prefabs, folders)");
			var droppedNew = ReplaceFlow.DropZone("Drop NEW sprites\n(sprites, textures, prefabs, folders)");
			EditorGUILayout.EndHorizontal();
			if (droppedOld != null)
				AddSprites(CollectSprites(droppedOld), true);
			if (droppedNew != null)
				AddSprites(CollectSprites(droppedNew), false);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Match", GUILayout.Width(45));
			m_findSameName = GUILayout.Toolbar(m_findSameName ? 0 : 1, new[] { "By name", "By order" }) == 0;
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			// Dialogs, progress bars and Refresh() break the IMGUI layout pass, so each such button ends with ExitGUI.
			if (GUILayout.Button(new GUIContent("Auto-match from scope", "Fill empty Old slots with same-name sprites from the scope folders (step 3).")))
			{
				AutoMatchFromScope();
				GUIUtility.ExitGUI();
			}
			if (GUILayout.Button("Swap Old/New"))
				foreach (var input in m_inputs)
					input.Swap();
			if (EditorHelper.ButtonColor("Clear…", Color.red, 70))
			{
				if (EditorUtility.DisplayDialog("Clear pairs", $"Remove all {m_inputs.Count} pairs?", "Clear", "Cancel"))
					m_inputs.Clear();
				GUIUtility.ExitGUI();
			}
			EditorGUILayout.EndHorizontal();

			if (m_inputs.Count == 0)
			{
				EditorGUILayout.HelpBox("Drop the sprites in use now on OLD and their replacements on NEW. Matching sprites pair up by name.", MessageType.Info);
				return;
			}

			int matched = m_inputs.Count(x => x.IsMatched);
			float trisOld = GetTotalTrisOld();
			string tris = trisOld > 0 ? $" · tris {GetTotalTrisNew() / trisOld:0.00}" : "";
			GUILayout.Label(new GUIContent($"{m_inputs.Count} pairs · {matched} matched · {m_inputs.Count - matched} unmatched{tris}", TRIS_TOOLTIP), EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();
			m_displayNullR = GUILayout.Toggle(m_displayNullR, "Unmatched only", GUILayout.Width(120));
			m_search = EditorGUILayout.TextField(m_search, EditorStyles.toolbarSearchField);
			m_displayIcon = GUILayout.Toggle(m_displayIcon, "Icons", GUILayout.Width(55));
			EditorGUILayout.EndHorizontal();

			var rows = m_inputs.Where(x => (!m_displayNullR || !x.IsMatched) && MatchesSearch(x)).ToList();
			var remove = new List<Input>();
			EditorHelper.PagesForList(rows.Count, "m_spriteReplace.inputs", i => DrawRow(rows[i], remove));
			foreach (var input in remove)
				m_inputs.Remove(input);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("All rows:", GUILayout.Width(60));
			if (GUILayout.Button("Old pivots → New"))
			{
				CopyPivots(true);
				GUIUtility.ExitGUI();
			}
			if (GUILayout.Button("New pivots → Old"))
			{
				CopyPivots(false);
				GUIUtility.ExitGUI();
			}
			if (EditorHelper.ButtonColor("Remove unmatched", Color.red))
				m_inputs.RemoveAll(x => x == null || !x.IsMatched);
			EditorGUILayout.EndHorizontal();
		}

		private bool MatchesSearch(Input pInput)
		{
			if (string.IsNullOrEmpty(m_search))
				return true;
			return (pInput.spriteOriginal != null && pInput.spriteOriginal.name.IndexOf(m_search, StringComparison.OrdinalIgnoreCase) >= 0)
				|| (pInput.spriteNew != null && pInput.spriteNew.name.IndexOf(m_search, StringComparison.OrdinalIgnoreCase) >= 0);
		}

		private void DrawRow(Input pInput, List<Input> pRemove)
		{
			EditorGUILayout.BeginHorizontal("box");
			pInput.spriteOriginal = DrawSprite(pInput.spriteOriginal, pInput.PivotOriginal, pInput.SizeOriginal);
			GUILayout.Label("→", GUILayout.Width(16));
			pInput.spriteNew = DrawSprite(pInput.spriteNew, pInput.PivotNew, pInput.SizeNew);

			EditorGUILayout.BeginVertical(GUILayout.Width(110));
			if (pInput.IsMatched)
			{
				float trisOld = pInput.TrisOriginal;
				if (trisOld > 0)
				{
					float ratio = pInput.TrisNew / trisOld;
					var style = new GUIStyle(EditorStyles.boldLabel);
					style.normal.textColor = ratio > 1.01f ? Color.red : ratio < 0.99f ? Color.green : Color.yellow;
					GUILayout.Label(new GUIContent($"{ratio:0.00} tris", TRIS_TOOLTIP), style);
				}
				if (pInput.PivotNew != pInput.PivotOriginal)
				{
					var warn = new GUIStyle(EditorStyles.miniBoldLabel);
					warn.normal.textColor = Color.red;
					GUILayout.Label("! pivot differs", warn);
					if (GUILayout.Button(new GUIContent("Use old pivot", "Copy the Old sprite's pivot and border onto the New sprite.")))
					{
						EditorHelper.CopyPivotAndBorder(pInput.spriteOriginal, pInput.spriteNew, true);
						GUIUtility.ExitGUI();
					}
					if (GUILayout.Button(new GUIContent("Use new pivot", "Copy the New sprite's pivot and border onto the Old sprite.")))
					{
						EditorHelper.CopyPivotAndBorder(pInput.spriteNew, pInput.spriteOriginal, true);
						GUIUtility.ExitGUI();
					}
				}
			}
			EditorGUILayout.EndVertical();

			if (EditorHelper.ButtonColor("x", Color.red, 23))
				pRemove.Add(pInput);
			EditorGUILayout.EndHorizontal();
		}

		private Sprite DrawSprite(Sprite pSprite, Vector2 pPivot, Vector2 pSize)
		{
			if (m_displayIcon)
				pSprite = (Sprite)EditorGUILayout.ObjectField(pSprite, typeof(Sprite), false, GUILayout.Width(60), GUILayout.Height(60));
			EditorGUILayout.BeginVertical(GUILayout.Width(150));
			if (m_displayIcon)
				GUILayout.Label(pSprite != null ? pSprite.name : "(empty)", EditorStyles.boldLabel);
			else
				pSprite = (Sprite)EditorGUILayout.ObjectField(pSprite, typeof(Sprite), false);
			if (pSprite != null)
				GUILayout.Label($"{pSize.x:0}×{pSize.y:0}  p {pPivot.x},{pPivot.y}", EditorStyles.miniLabel);
			EditorGUILayout.EndVertical();
			return pSprite;
		}

		/// <summary>Sprite = itself; texture = all its sprites; prefab = sprites its Image/SpriteRenderer use; folder = all sprites inside. Also used by ImagePropertyFixer.</summary>
		internal static List<Sprite> CollectSprites(Object[] pObjects)
		{
			var result = new List<Sprite>();
			foreach (var obj in pObjects)
			{
				string path = AssetDatabase.GetAssetPath(obj);
				if (obj is Sprite sprite)
					result.Add(sprite);
				else if (obj is Texture2D)
					result.AddRange(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>());
				else if (obj is GameObject go)
				{
					result.AddRange(go.GetComponentsInChildren<Image>(true).Select(x => x.sprite));
					result.AddRange(go.GetComponentsInChildren<SpriteRenderer>(true).Select(x => x.sprite));
				}
				else if (AssetDatabase.IsValidFolder(path))
					foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { path }))
						result.AddRange(AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)).OfType<Sprite>());
			}
			return result.Where(x => x != null).Distinct().ToList();
		}

		/// <summary>Puts each sprite into a free slot of a matching row, or a new row. Sprites already in the list are skipped.</summary>
		private void AddSprites(List<Sprite> pSprites, bool pAsOld)
		{
			EditorAssetUtil.ClearObjectFolderCaches();
			var used = new HashSet<Sprite>(m_inputs.SelectMany(x => new[] { x.spriteNew, x.spriteOriginal }).Where(x => x != null));
			foreach (var sprite in pSprites)
			{
				if (!used.Add(sprite))
					continue;
				var row = m_inputs.FirstOrDefault(x =>
				{
					var own = pAsOld ? x.spriteOriginal : x.spriteNew;
					var other = pAsOld ? x.spriteNew : x.spriteOriginal;
					return own == null && other != null && (!m_findSameName || IsNameMatch(other, sprite));
				});
				if (row == null)
				{
					row = new Input();
					m_inputs.Add(row);
				}
				if (pAsOld)
					row.spriteOriginal = sprite;
				else
					row.spriteNew = sprite;
			}
		}

		/// <summary>Same name, or one name is "{folder}-{name}" of the other (e.g. "Core-AS_227" and "Core/AS_227").</summary>
		private static bool IsNameMatch(Sprite pA, Sprite pB)
			=> pA.name == pB.name
				|| pA.name.EndsWith($"{EditorHelper.GetObjectFolderName(pB)}-{pB.name}")
				|| pB.name.EndsWith($"{EditorHelper.GetObjectFolderName(pA)}-{pA.name}");

		private void AutoMatchFromScope()
		{
			var folders = m_scope.GetFolders();
			if (folders.Length == 0)
			{
				if (!m_scope.IsEmpty)
				{
					EditorUtility.DisplayDialog("Auto-match", "Auto-match only looks in folders. Add a folder to the scope in step 3.", "OK");
					return;
				}
				if (!EditorUtility.DisplayDialog("Auto-match", "The scope is empty, so this searches every sprite in Assets/. It can be slow.", "Search", "Cancel"))
					return;
				folders = new[] { "Assets" };
			}
			var byName = new Dictionary<string, List<Sprite>>();
			foreach (var guid in AssetDatabase.FindAssets("t:Sprite", folders))
				foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)).OfType<Sprite>())
				{
					if (!byName.TryGetValue(sprite.name, out var list))
						byName[sprite.name] = list = new List<Sprite>();
					list.Add(sprite);
				}
			int filled = 0;
			foreach (var input in m_inputs)
			{
				if (input.spriteOriginal != null || input.spriteNew == null || !byName.TryGetValue(input.spriteNew.name, out var candidates))
					continue;
				input.spriteOriginal = candidates.FirstOrDefault(x => x != input.spriteNew);
				if (input.spriteOriginal != null)
					filled++;
			}
			Debug.Log($"[SpriteReplacer] Auto-match filled {filled} Old slots.");
		}

		private void CopyPivots(bool pOldToNew)
		{
			foreach (var input in m_inputs.Where(x => x.IsMatched))
			{
				if (pOldToNew)
					EditorHelper.CopyPivotAndBorder(input.spriteOriginal, input.spriteNew, false);
				else
					EditorHelper.CopyPivotAndBorder(input.spriteNew, input.spriteOriginal, false);
			}
			AssetDatabase.Refresh();
		}

		private string m_tpsError;

		private void DrawTpsSection()
		{
			if (!EditorHelper.HeaderFoldout("2 · Pivots & TexturePacker", "SpriteReplacer.tps"))
				return;
			EditorGUILayout.BeginVertical("box");
			m_tpsSaveFile = EditorGUILayout.ObjectField(".tps file", m_tpsSaveFile, typeof(Object), false);
			m_detectNamePattern = EditorGUILayout.Toggle(new GUIContent("Match numbered frames", "Walk_1 and Walk_2 count as the same name, so one custom pivot fills the whole animation."), m_detectNamePattern);

			EditorGUILayout.BeginHorizontal();
			m_tps = (Tps)EditorGUILayout.EnumPopup(new GUIContent("Format", "Number types in the .tps XML. Change it only if loading fails."), m_tps);
			if (GUILayout.Button(new GUIContent("Change", "Sets a scripting define. Scripts recompile."), GUILayout.Width(70)))
			{
				foreach (Tps val in Enum.GetValues(typeof(Tps)))
					if (val != m_tps && GetDirective(val) != "")
						EditorHelper.RemoveDirective(GetDirective(val));
				if (GetDirective(m_tps) != "")
					EditorHelper.AddDirective(GetDirective(m_tps));
			}
			EditorGUILayout.EndHorizontal();

			bool isTps = m_tpsSaveFile != null && AssetDatabase.GetAssetPath(m_tpsSaveFile).EndsWith(".tps");
			bool hasOld = m_inputs.Any(x => x.spriteOriginal != null);
			string reason = !isTps ? "Set a .tps file." : !hasOld ? "Add Old sprites in step 1." : null;
			if (reason != null)
				EditorGUILayout.HelpBox(reason, MessageType.Info);
			using (new EditorGUI.DisabledScope(reason != null))
				if (GUILayout.Button(new GUIContent("Write Old pivots to .tps…", "Copies each Old sprite's pivot into the matching sprite entry of the .tps file, then asks where to save.")))
				{
					m_tpsError = WritePivotsToTps(); // assign before ExitGUI so the error still shows
					GUIUtility.ExitGUI();
				}
			if (!string.IsNullOrEmpty(m_tpsError))
				EditorGUILayout.HelpBox(m_tpsError, MessageType.Error);
			EditorGUILayout.EndVertical();
		}

		/// <summary>Returns an error message, or null on success or cancel.</summary>
		private string WritePivotsToTps()
		{
			var oldSprites = m_inputs.Select(x => x.spriteOriginal).Where(x => x != null).ToList();
			string path = Path.GetFullPath(AssetDatabase.GetAssetPath(m_tpsSaveFile));
			data obj;
			try
			{
				obj = EditorHelper.LoadXMLFile<data>(path);
			}
			catch (Exception e)
			{
				return $"Can't read the .tps file with format {m_tps}. Try another format. ({e.Message})";
			}
			var map = FindSpriteSettings(obj);
			if (map == null)
				return "The .tps file has no 'individualSpriteSettings' list.";

			var defaultPivot = new Vector2(0.5f, 0.5f);
			var errorLogs = new List<string>();
			for (int i = 0; i < map.Items.Length - 1; i++)
			{
				if (!(map.Items[i] is dataStructMapKey key))
					continue;
				string fileName = Path.GetFileNameWithoutExtension(key.Value.Split('/').Last());
				var settings = map.Items[i + 1] as dataStructMapStruct;
				if (settings == null)
				{
					// Several keys can share one settings block. Give this key its own copy.
					var shared = map.Items.Skip(i + 2).OfType<dataStructMapStruct>().FirstOrDefault();
					if (shared != null)
					{
						settings = new dataStructMapStruct(shared);
						var list = map.Items.ToList();
						list.Insert(i + 1, settings);
						map.Items = list.ToArray();
					}
				}

				foreach (var sprite in oldSprites)
				{
					var pivot = sprite.NormalizedPivot();
					bool match = sprite.name == fileName
						|| (m_detectNamePattern && pivot != defaultPivot && RemoveEndNumber(sprite.name) == RemoveEndNumber(fileName));
					if (!match)
						continue;
					int pivotIndex = settings == null ? -1 : Array.IndexOf(settings.Items, "pivotPoint");
					if (pivotIndex >= 0 && pivotIndex + 1 < settings.Items.Length)
						settings.Items[pivotIndex + 1] = FormattableString.Invariant($"{pivot.x},{1 - pivot.y}"); // TexturePacker y goes down; invariant so vi-VN doesn't write "0,5"
					else if (pivot != defaultPivot)
						errorLogs.Add($"{fileName} x:{pivot.x},y:{pivot.y}");
					break;
				}
			}

			string savePath = EditorUtility.SaveFilePanel("Save .tps", Path.GetDirectoryName(path), Path.GetFileName(path), "tps");
			if (string.IsNullOrEmpty(savePath))
				return null;
			EditorHelper.SaveXMLFile(savePath, obj);
			if (errorLogs.Count > 0)
			{
				File.WriteAllLines($"{savePath}.error.txt", errorLogs);
				return $"{errorLogs.Count} sprites have a custom pivot but no settings block in the .tps. See {Path.GetFileName(savePath)}.error.txt.";
			}
			Debug.Log($"[SpriteReplacer] Pivots written to {savePath}");
			return null;
		}

		private static dataStructMap FindSpriteSettings(data pData)
		{
			var items = pData?.@struct?.Items;
			if (items == null)
				return null;
			int index = Array.IndexOf(items, "individualSpriteSettings");
			return index >= 0 && index + 1 < items.Length ? items[index + 1] as dataStructMap : null;
		}

		private static string RemoveEndNumber(string pInput) => Regex.Replace(pInput, @"\d+$", "");

		public static string GetDirective(Tps pType)
		{
			switch (pType)
			{
				case Tps.ByteDecimal: return "TPS_BYTE_DECIMAL";
				case Tps.DecimalByte: return "TPS_DECIMAL_BYTE";
				default: return "";
			}
		}
	}
}
