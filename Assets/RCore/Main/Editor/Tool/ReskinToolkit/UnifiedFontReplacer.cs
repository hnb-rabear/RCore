using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RCore.Editor.Tool
{
	/// <summary>
	/// Unified font replacer that handles both TMP_FontAsset and legacy Font replacement.
	/// Scans assets in the shared scope (empty = all of Assets/) plus the open scene, which supports Undo.
	/// </summary>
	[Serializable]
	public class UnifiedFontReplacer
	{
		[Serializable]
		public class TMPInput
		{
			public List<TMP_FontAsset> targets = new List<TMP_FontAsset>();
			public TMP_FontAsset replace;
		}

		[Serializable]
		public class LegacyInput
		{
			public List<Font> targets = new List<Font>();
			public Font replace;
		}

		[SerializeField] private List<TMPInput> m_tmpInputs = new List<TMPInput>();
		[SerializeField] private List<LegacyInput> m_legacyInputs = new List<LegacyInput>();
		[SerializeField] private ReplaceScope m_scope = new ReplaceScope();
		private ReplaceFlow m_flow;

		public void Draw()
		{
			m_flow ??= new ReplaceFlow();
			m_scope ??= new ReplaceScope();
			DrawTMPSection();
			EditorGUILayout.Space(8);
			DrawLegacySection();
			EditorGUILayout.Space(8);
			var mappings = GetMappings();
			m_flow.Draw(nameof(UnifiedFontReplacer), m_scope, ReplaceFlow.Fingerprint(m_scope, mappings),
				mappings.Count == 0 ? "Add a TMP or Legacy mapping with a New font and at least one Old font." : null,
				() => Scan(mappings),
				changes => Apply(mappings, changes));
		}

		#region TMP Section

		private void DrawTMPSection()
		{
			var addBtn = new GuiButton()
			{
				color = Color.yellow,
				onPressed = () => m_tmpInputs.Add(new TMPInput()),
				label = "Add",
			};

			if (EditorHelper.HeaderFoldout("TMP Font Replacement", null, false, addBtn))
			{
				EditorGUILayout.BeginVertical("box");
				{
					if (m_tmpInputs.Count == 0)
					{
						EditorGUILayout.HelpBox("No TMP font mappings. Click 'Add' to create one.", MessageType.Info);
					}

					for (int i = 0; i < m_tmpInputs.Count; i++)
					{
						DrawTMPInputRow(m_tmpInputs[i], i);
					}
				}
				EditorGUILayout.EndVertical();
			}
		}

		private void DrawTMPInputRow(TMPInput input, int index)
		{
			EditorGUILayout.BeginVertical("box");
			{
				// New font row
				EditorGUILayout.BeginHorizontal();
				{
					EditorGUILayout.LabelField("New:", GUILayout.Width(35));
					input.replace = (TMP_FontAsset)EditorHelper.ObjectField<TMP_FontAsset>(input.replace, "");
					if (EditorHelper.ButtonColor("+", Color.green, 23))
						input.targets.Add(null);
					if (EditorHelper.ButtonColor("x", Color.red, 23))
					{
						m_tmpInputs.RemoveAt(index);
						GUIUtility.ExitGUI();
						return;
					}
				}
				EditorGUILayout.EndHorizontal();

				// Old font targets
				EditorGUI.indentLevel++;
				for (int t = 0; t < input.targets.Count; t++)
				{
					EditorGUILayout.BeginHorizontal();
					{
						EditorGUILayout.LabelField("Old:", GUILayout.Width(35));
						input.targets[t] = (TMP_FontAsset)EditorHelper.ObjectField<TMP_FontAsset>(input.targets[t], "");
						if (EditorHelper.ButtonColor("x", Color.red, 23))
						{
							input.targets.RemoveAt(t);
							t--;
						}
					}
					EditorGUILayout.EndHorizontal();
				}
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.EndVertical();
		}

		#endregion

		#region Legacy Section

		private void DrawLegacySection()
		{
			var addBtn = new GuiButton()
			{
				color = Color.yellow,
				onPressed = () => m_legacyInputs.Add(new LegacyInput()),
				label = "Add",
			};

			if (EditorHelper.HeaderFoldout("Legacy Font Replacement", null, false, addBtn))
			{
				EditorGUILayout.BeginVertical("box");
				{
					if (m_legacyInputs.Count == 0)
					{
						EditorGUILayout.HelpBox("No Legacy font mappings. Click 'Add' to create one.", MessageType.Info);
					}

					for (int i = 0; i < m_legacyInputs.Count; i++)
					{
						DrawLegacyInputRow(m_legacyInputs[i], i);
					}
				}
				EditorGUILayout.EndVertical();
			}
		}

		private void DrawLegacyInputRow(LegacyInput input, int index)
		{
			EditorGUILayout.BeginVertical("box");
			{
				// New font row
				EditorGUILayout.BeginHorizontal();
				{
					EditorGUILayout.LabelField("New:", GUILayout.Width(35));
					input.replace = (Font)EditorHelper.ObjectField<Font>(input.replace, "");
					if (EditorHelper.ButtonColor("+", Color.green, 23))
						input.targets.Add(null);
					if (EditorHelper.ButtonColor("x", Color.red, 23))
					{
						m_legacyInputs.RemoveAt(index);
						GUIUtility.ExitGUI();
						return;
					}
				}
				EditorGUILayout.EndHorizontal();

				// Old font targets
				EditorGUI.indentLevel++;
				for (int t = 0; t < input.targets.Count; t++)
				{
					EditorGUILayout.BeginHorizontal();
					{
						EditorGUILayout.LabelField("Old:", GUILayout.Width(35));
						input.targets[t] = (Font)EditorHelper.ObjectField<Font>(input.targets[t], "");
						if (EditorHelper.ButtonColor("x", Color.red, 23))
						{
							input.targets.RemoveAt(t);
							t--;
						}
					}
					EditorGUILayout.EndHorizontal();
				}
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.EndVertical();
		}

		#endregion

		#region Actions

		private const string SCAN_FILTER = "t:GameObject t:ScriptableObject t:Scene";
		private const string SCENE_ROW_PREFIX = "Open scene: ";

		private List<(Object oldObj, Object newObj)> GetMappings()
		{
			var mappings = new List<(Object oldObj, Object newObj)>();
			foreach (var input in m_tmpInputs)
				if (input.replace != null)
					mappings.AddRange(input.targets.Where(t => t != null && t != input.replace).Select(t => ((Object)t, (Object)input.replace)));
			foreach (var input in m_legacyInputs)
				if (input.replace != null)
					mappings.AddRange(input.targets.Where(t => t != null && t != input.replace).Select(t => ((Object)t, (Object)input.replace)));
			return mappings;
		}

		private List<ReplaceChange> Scan(List<(Object oldObj, Object newObj)> pMappings)
		{
			var changes = ReplaceFlow.ScanGuidRefs(pMappings, m_scope.GetGuids(SCAN_FILTER));
			if (changes == null) // cancelled
				return null;
			int sceneCount = ProcessOpenScene(pMappings, false);
			if (sceneCount > 0)
				changes.Insert(0, new ReplaceChange { path = SCENE_ROW_PREFIX + SceneManager.GetActiveScene().name, count = sceneCount });
			return changes;
		}

		private static string Apply(List<(Object oldObj, Object newObj)> pMappings, List<ReplaceChange> pChanges)
		{
			bool scene = pChanges.RemoveAll(c => c.path.StartsWith(SCENE_ROW_PREFIX)) > 0;
			string summary = pChanges.Count > 0 ? ReplaceFlow.ApplyGuidRefs(pMappings, pChanges) : "No files changed";
			if (scene)
				summary += $" · open scene: {ProcessOpenScene(pMappings, true)} text components (Ctrl+Z works)";
			return summary;
		}

		/// <summary>Counts, or with pApply replaces, fonts on TMP_Text and Text components in the active scene.</summary>
		private static int ProcessOpenScene(List<(Object oldObj, Object newObj)> pMappings, bool pApply)
		{
			var map = new Dictionary<Object, Object>();
			foreach (var (oldObj, newObj) in pMappings)
				map[oldObj] = newObj;
			var scene = SceneManager.GetActiveScene();
			int count = 0;
			foreach (var root in scene.GetRootGameObjects())
			{
				foreach (var com in root.GetComponentsInChildren<TMP_Text>(true))
				{
					if (com.font == null || !map.TryGetValue(com.font, out var newFont))
						continue;
					count++;
					if (pApply)
					{
						Undo.RecordObject(com, "Replace font");
						com.font = (TMP_FontAsset)newFont;
					}
				}
				foreach (var com in root.GetComponentsInChildren<Text>(true))
				{
					if (com.font == null || !map.TryGetValue(com.font, out var newFont))
						continue;
					count++;
					if (pApply)
					{
						Undo.RecordObject(com, "Replace font");
						com.font = (Font)newFont;
					}
				}
			}
			if (pApply && count > 0)
				EditorSceneManager.MarkSceneDirty(scene);
			return count;
		}

		#endregion
	}
}
