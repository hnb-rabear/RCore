using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RCore.Editor.Tool
{
	/// <summary>
	/// Unified font replacer that handles both TMP_FontAsset and legacy Font replacement.
	/// Supports folder-scoped scanning based on Unity Selection, with fallback to all Assets.
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
		[SerializeField] private List<string> m_scopeFolders = new List<string>();

		public void Draw()
		{
			// --- Scope display ---
			DrawScopeInfo();

			EditorGUILayout.Space(4);

			// --- TMP Font Section ---
			DrawTMPSection();

			EditorGUILayout.Space(8);

			// --- Legacy Font Section ---
			DrawLegacySection();

			EditorGUILayout.Space(8);

			// --- Action Buttons ---
			DrawActionButtons();
		}

		#region Scope

		private string[] GetSearchFolders()
		{
			return m_scopeFolders.Count > 0 ? m_scopeFolders.ToArray() : new[] { "Assets" };
		}

		private void DrawScopeInfo()
		{
			EditorGUILayout.BeginVertical("box");
			{
				// Header
				EditorGUILayout.BeginHorizontal();
				{
					EditorGUILayout.LabelField("📁 Scan Scope", EditorStyles.boldLabel);
					if (m_scopeFolders.Count > 0)
					{
						if (EditorHelper.ButtonColor("Clear All", Color.red, 70))
						{
							m_scopeFolders.Clear();
						}
					}
				}
				EditorGUILayout.EndHorizontal();

				// Drag-and-drop area
				var dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
				var dropStyle = new GUIStyle("box")
				{
					alignment = TextAnchor.MiddleCenter,
					fontStyle = FontStyle.Italic,
					normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
				};

				if (m_scopeFolders.Count == 0)
				{
					GUI.Box(dropArea, "Drag & drop folders here\n(empty = scan all Assets)", dropStyle);
				}
				else
				{
					GUI.Box(dropArea, $"Drag & drop to add more folders\n({m_scopeFolders.Count} folder(s) selected)", dropStyle);
				}

				// Handle drag-and-drop
				HandleDragAndDrop(dropArea);

				// Show selected folders
				if (m_scopeFolders.Count > 0)
				{
					EditorGUILayout.Space(2);
					for (int i = 0; i < m_scopeFolders.Count; i++)
					{
						EditorGUILayout.BeginHorizontal();
						{
							EditorGUILayout.LabelField($"  📂 {m_scopeFolders[i]}", EditorStyles.miniLabel);
							if (EditorHelper.ButtonColor("x", Color.red, 20))
							{
								m_scopeFolders.RemoveAt(i);
								i--;
							}
						}
						EditorGUILayout.EndHorizontal();
					}
				}
				else
				{
					EditorGUILayout.LabelField("  ⚡ Will scan: All Assets/", EditorStyles.miniLabel);
				}
			}
			EditorGUILayout.EndVertical();
		}

		private void HandleDragAndDrop(Rect dropArea)
		{
			var evt = Event.current;
			if (!dropArea.Contains(evt.mousePosition))
				return;

			switch (evt.type)
			{
				case EventType.DragUpdated:
					bool hasFolder = false;
					foreach (var obj in DragAndDrop.objectReferences)
					{
						string path = AssetDatabase.GetAssetPath(obj);
						if (AssetDatabase.IsValidFolder(path))
						{
							hasFolder = true;
							break;
						}
					}
					DragAndDrop.visualMode = hasFolder ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
					evt.Use();
					break;

				case EventType.DragPerform:
					DragAndDrop.AcceptDrag();
					foreach (var obj in DragAndDrop.objectReferences)
					{
						string path = AssetDatabase.GetAssetPath(obj);
						if (AssetDatabase.IsValidFolder(path) && !m_scopeFolders.Contains(path))
						{
							m_scopeFolders.Add(path);
						}
					}
					evt.Use();
					break;
			}
		}

		#endregion

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

		private void DrawActionButtons()
		{
			bool hasAnyInput = m_tmpInputs.Count > 0 || m_legacyInputs.Count > 0;

			GUI.enabled = hasAnyInput;

			if (EditorHelper.ButtonColor("Scan & Replace", new Color(0.4f, 0.8f, 0.4f)))
			{
				ReplaceInProject();
				ReplaceInScene();
			}

			GUI.enabled = true;
		}

		private void ReplaceInProject()
		{
			var folders = GetSearchFolders();
			var assetGUIDs = AssetDatabase.FindAssets("t:GameObject t:ScriptableObject t:Scene", folders);

			if (assetGUIDs.Length == 0)
			{
				Debug.LogWarning("[FontReplacer] No assets found in selected folders.");
				return;
			}

			AssetDatabase.StartAssetEditing();

			try
			{
				int totalReplaced = 0;

				// TMP replacements
				foreach (var input in m_tmpInputs)
				{
					if (input.replace == null)
						continue;
					var validTargets = input.targets.Where(t => t != null).ToList();
					if (validTargets.Count == 0)
						continue;

					var result = EditorHelper.SearchAndReplaceGuid(validTargets, input.replace, assetGUIDs);
					foreach (var item in result)
					{
						Debug.Log($"[FontReplacer] TMP: {input.replace.name} replaced in {item.Value} asset(s) — {item.Key}");
						totalReplaced += item.Value;
					}
				}

				// Legacy replacements
				foreach (var input in m_legacyInputs)
				{
					if (input.replace == null)
						continue;
					var validTargets = input.targets.Where(t => t != null).ToList();
					if (validTargets.Count == 0)
						continue;

					var result = EditorHelper.SearchAndReplaceGuid(validTargets, input.replace, assetGUIDs);
					foreach (var item in result)
					{
						Debug.Log($"[FontReplacer] Legacy: {input.replace.name} replaced in {item.Value} asset(s) — {item.Key}");
						totalReplaced += item.Value;
					}
				}

				Debug.Log($"[FontReplacer] Done. Total: {totalReplaced} replacement(s) in {string.Join(", ", folders)}");
			}
			finally
			{
				EditorUtility.ClearProgressBar();
				AssetDatabase.StopAssetEditing();
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
		}

		private void ReplaceInScene()
		{
			// Build lookup sets from inputs to avoid O(n) Contains calls per component
			var tmpMap = new Dictionary<TMP_FontAsset, TMP_FontAsset>();
			foreach (var input in m_tmpInputs)
			{
				if (input.replace == null)
					continue;
				foreach (var target in input.targets)
				{
					if (target != null && target != input.replace)
						tmpMap[target] = input.replace;
				}
			}

			var legacyMap = new Dictionary<Font, Font>();
			foreach (var input in m_legacyInputs)
			{
				if (input.replace == null)
					continue;
				foreach (var target in input.targets)
				{
					if (target != null && target != input.replace)
						legacyMap[target] = input.replace;
				}
			}

			if (tmpMap.Count == 0 && legacyMap.Count == 0)
			{
				Debug.LogWarning("[FontReplacer] No valid font mappings to replace in scene.");
				return;
			}

			// Use root objects to avoid double-counting from GetComponentsInChildren
			var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
			var rootObjects = scene.GetRootGameObjects();
			int count = 0;
			var modifiedObjects = new HashSet<GameObject>();

			foreach (var root in rootObjects)
			{
				// TMP replacements — TextMeshProUGUI (Canvas) + TextMeshPro (3D)
				var tmps = root.GetComponentsInChildren<TMP_Text>(true);
				foreach (var com in tmps)
				{
					if (com.font != null && tmpMap.TryGetValue(com.font, out var newFont))
					{
						com.font = newFont;
						count++;
						var go = com.gameObject;
						EditorUtility.SetDirty(go);
						modifiedObjects.Add(go);
					}
				}

				// Legacy replacements
				var texts = root.GetComponentsInChildren<Text>(true);
				foreach (var com in texts)
				{
					if (com.font != null && legacyMap.TryGetValue(com.font, out var newFont))
					{
						com.font = newFont;
						count++;
						var go = com.gameObject;
						EditorUtility.SetDirty(go);
						modifiedObjects.Add(go);
					}
				}
			}

			Selection.objects = modifiedObjects.ToArray();
			AssetDatabase.SaveAssets();

			Debug.Log($"[FontReplacer] Scene: Replaced {count} font reference(s) in {modifiedObjects.Count} object(s)");
		}

		#endregion
	}
}
