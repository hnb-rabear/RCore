using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace RCore.Editor.Tool
{
	/// <summary>Where a replace tool looks: folders, prefabs, scenes or assets. Empty = all of Assets/.</summary>
	[Serializable]
	public class ReplaceScope
	{
		[SerializeField] private List<Object> m_items = new List<Object>();

		public bool IsEmpty => m_items.All(x => x == null);

		public string Fingerprint() => string.Join("|", m_items.Where(x => x != null).Select(x => x.GetInstanceID()));

		public string[] GetGuids(string pFilter)
		{
			var items = m_items.Where(x => x != null).ToList();
			if (items.Count == 0)
				return AssetDatabase.FindAssets(pFilter, new[] { "Assets" });
			var guids = new HashSet<string>();
			foreach (var item in items)
			{
				string path = AssetDatabase.GetAssetPath(item);
				if (AssetDatabase.IsValidFolder(path))
					guids.UnionWith(AssetDatabase.FindAssets(pFilter, new[] { path }));
				else
					guids.Add(AssetDatabase.AssetPathToGUID(path));
			}
			return guids.ToArray();
		}

		public string[] GetFolders() => m_items.Where(x => x != null).Select(AssetDatabase.GetAssetPath).Where(AssetDatabase.IsValidFolder).ToArray();

		public void Draw()
		{
			m_items ??= new List<Object>();
			EditorGUILayout.LabelField("Scope", EditorStyles.boldLabel);
			var dropped = ReplaceFlow.DropZone(m_items.Count == 0
				? "Drop folders, prefabs, scenes or assets here\n(empty = all of Assets/)"
				: $"Drop to add more ({m_items.Count} in scope)");
			if (dropped != null)
				foreach (var obj in dropped)
					if (EditorUtility.IsPersistent(obj) && !m_items.Contains(obj))
						m_items.Add(obj);
			int remove = -1;
			for (int i = 0; i < m_items.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				m_items[i] = EditorGUILayout.ObjectField(m_items[i], typeof(Object), false);
				if (EditorHelper.ButtonColor("x", Color.red, 23))
					remove = i;
				EditorGUILayout.EndHorizontal();
			}
			if (remove >= 0)
				m_items.RemoveAt(remove);
			if (m_items.Count > 0 && EditorHelper.ButtonColor("Clear scope", Color.red))
				m_items.Clear();
		}
	}

	/// <summary>One preview row: an asset that would change.</summary>
	public class ReplaceChange
	{
		public string path;
		public int count;
		public bool selected = true;
		public bool locked;
		public string status;
	}

	/// <summary>Shared Scope -> Scan -> Preview -> Apply panel.</summary>
	public class ReplaceFlow
	{
		private const string COMMIT_HINT = "Commit first: file edits can't be undone with Ctrl+Z.";
		private List<ReplaceChange> m_changes;
		private string m_fingerprint;
		private string m_summary;

		public static string Fingerprint(ReplaceScope pScope, IList<(Object oldObj, Object newObj)> pMappings)
			=> pScope.Fingerprint() + "#" + string.Join(";", pMappings.Select(m => $"{Id(m.oldObj)}>{Id(m.newObj)}"));

		private static int Id(Object pObj) => pObj != null ? pObj.GetInstanceID() : 0;

		/// <summary>Draws a drop box. Returns the raw dropped objects (folders not expanded) on the drop frame, else null.</summary>
		public static Object[] DropZone(string pLabel, float pHeight = 36)
		{
			var dropArea = GUILayoutUtility.GetRect(0, pHeight, GUILayout.ExpandWidth(true));
			GUI.Box(dropArea, pLabel, EditorStyles.helpBox);
			var evt = Event.current;
			if ((evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) || !dropArea.Contains(evt.mousePosition))
				return null;
			DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
			Object[] result = null;
			if (evt.type == EventType.DragPerform)
			{
				DragAndDrop.AcceptDrag();
				result = DragAndDrop.objectReferences.Where(x => x != null).ToArray();
			}
			evt.Use();
			return result;
		}

		/// <summary>Returns null when the user cancels the scan.</summary>
		public static List<ReplaceChange> ScanGuidRefs(IList<(Object oldObj, Object newObj)> pMappings, string[] pScopeGuids)
		{
			var refs = EditorAssetUtil.BuildGuidRefs(pMappings);
			return EditorAssetUtil.FindGuidReferences(refs, pScopeGuids)?
				// The tool's own settings asset references the Old objects; rewriting it would turn every pair into New -> New.
				.Where(kv => AssetDatabase.GetMainAssetTypeAtPath(kv.Key) != typeof(FindAndReplaceAssetToolkit))
				.OrderBy(kv => kv.Key)
				.Select(kv => new ReplaceChange { path = kv.Key, count = kv.Value })
				.ToList();
		}

		public static string ApplyGuidRefs(IList<(Object oldObj, Object newObj)> pMappings, List<ReplaceChange> pChanges)
		{
			var result = EditorAssetUtil.ReplaceGuidReferences(EditorAssetUtil.BuildGuidRefs(pMappings), pChanges.Select(c => c.path).ToList());
			return $"{result.replaced} replaced ({result.references} references) · {result.skipped} skipped · {result.failed} failed";
		}

		public void Draw(string pKey, ReplaceScope pScope, string pFingerprint, string pBlockReason,
			Func<List<ReplaceChange>> pScan, Func<List<ReplaceChange>, string> pApply)
		{
			pScope.Draw();
			EditorGUILayout.Space(4);
			bool blocked = !string.IsNullOrEmpty(pBlockReason);
			if (blocked)
				EditorGUILayout.HelpBox(pBlockReason, MessageType.Info);
			using (new EditorGUI.DisabledScope(blocked))
			{
				if (EditorHelper.ButtonColor("Scan", Color.cyan))
				{
					var scanned = pScan();
					if (scanned == null) // cancelled: keep the previous preview
						GUIUtility.ExitGUI();
					m_changes = scanned;
					foreach (var change in m_changes)
						if (!change.locked && IsOpen(change.path))
						{
							change.locked = true;
							change.selected = false;
							change.status = "open: save & close first";
						}
					m_fingerprint = pFingerprint;
					m_summary = null;
					GUIUtility.ExitGUI(); // the scan showed a progress bar; restart the layout pass
				}
			}
			if (m_summary != null)
				EditorGUILayout.HelpBox(m_summary, MessageType.Info);
			if (m_changes == null)
				return;

			bool stale = m_fingerprint != pFingerprint;
			if (stale)
				EditorGUILayout.HelpBox("Inputs changed. Scan again.", MessageType.Warning);
			using (new EditorGUI.DisabledScope(stale))
			{
				if (m_changes.Count == 0)
				{
					EditorGUILayout.HelpBox("Nothing to change in this scope.", MessageType.Info);
					return;
				}
				EditorHelper.PagesForList(m_changes.Count, $"{pKey}.changes", i =>
				{
					var change = m_changes[i];
					EditorGUILayout.BeginHorizontal();
					using (new EditorGUI.DisabledScope(change.locked))
						change.selected = EditorGUILayout.Toggle(change.selected, GUILayout.Width(18));
					if (GUILayout.Button(change.path, EditorStyles.linkLabel))
						EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(change.path));
					GUILayout.FlexibleSpace();
					GUILayout.Label(change.status ?? change.count.ToString());
					EditorGUILayout.EndHorizontal();
				});

				// Re-check IsOpen: a file opened after Scan would be overwritten by its in-memory copy on the next save.
				var picked = m_changes.Where(c => c.selected && !c.locked && !IsOpen(c.path)).ToList();
				EditorGUILayout.BeginHorizontal();
				GUILayout.Label($"{m_changes.Count} assets · {m_changes.Sum(c => c.count)} matches");
				if (GUILayout.Button("All", GUILayout.Width(40)))
					foreach (var c in m_changes)
						c.selected = !c.locked;
				if (GUILayout.Button("None", GUILayout.Width(45)))
					foreach (var c in m_changes)
						c.selected = false;
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.HelpBox(COMMIT_HINT, MessageType.None);
				using (new EditorGUI.DisabledScope(picked.Count == 0))
				{
					if (EditorHelper.ButtonColor($"Apply {picked.Count} assets", Color.green))
					{
						m_summary = pApply(picked);
						m_changes = null;
						Debug.Log($"[Replace] {m_summary}");
						GUIUtility.ExitGUI();
					}
				}
			}
		}

		private static bool IsOpen(string pPath)
		{
			for (int i = 0; i < SceneManager.sceneCount; i++)
				if (SceneManager.GetSceneAt(i).path == pPath)
					return true;
			var stage = PrefabStageUtility.GetCurrentPrefabStage();
			return stage != null && stage.assetPath == pPath;
		}
	}
}
