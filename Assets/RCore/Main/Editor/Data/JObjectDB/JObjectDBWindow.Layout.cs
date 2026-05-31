/**
 * Author HNB-RaBear - 2024
 * JObjectDBWindow — partial: Layout
 **/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RCore.Data.JObject;
using UnityEditor;
using UnityEngine;
using NJObject = Newtonsoft.Json.Linq.JObject;
using NJArray = Newtonsoft.Json.Linq.JArray;

namespace RCore.Editor.Data.JObject
{
	public partial class JObjectDBWindow
	{
		//==========================================================================
		// Toolbar
		//==========================================================================

		private void DrawToolbar()
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

			// Search field
			EditorGUI.BeginChangeCheck();
			m_searchQuery = EditorGUILayout.TextField(m_searchQuery, EditorStyles.toolbarSearchField, GUILayout.MinWidth(150));
			if (EditorGUI.EndChangeCheck())
				m_searchLastTypedTime = EditorApplication.timeSinceStartup;

			if (!string.IsNullOrEmpty(m_searchQuery))
			{
				if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(22)))
				{
					m_searchQuery = "";
					m_appliedSearchQuery = "";
					m_searchMatchedPaths = null;
					m_searchAncestorPaths = null;
					GUI.FocusControl(null);
				}
			}

			// Search mode toggle
			EditorGUI.BeginChangeCheck();
			m_searchMode = (SearchMode)EditorGUILayout.EnumPopup(m_searchMode, EditorStyles.toolbarDropDown, GUILayout.Width(55));
			if (EditorGUI.EndChangeCheck() && !string.IsNullOrEmpty(m_appliedSearchQuery))
			{
				RebuildSearchCache();
				Repaint();
			}

			GUILayout.FlexibleSpace();

			// Action buttons with icons
			GUI.enabled = m_dirtyKeys.Count > 0;
			if (GUILayout.Button(new GUIContent($" Save ({m_dirtyKeys.Count})", m_iconSave?.image), EditorStyles.toolbarButton, GUILayout.Width(80)))
				SaveAllDirty();
			GUI.enabled = true;

			if (GUILayout.Button(new GUIContent(" Reload", m_iconRefresh?.image), EditorStyles.toolbarButton, GUILayout.Width(65)))
				RefreshData();

			if (GUILayout.Button(new GUIContent(" Backup", m_iconBackup?.image), EditorStyles.toolbarButton, GUILayout.Width(68)))
				JObjectDB.Backup(openDirectory: true);

			if (GUILayout.Button(new GUIContent(" Import", m_iconImport?.image), EditorStyles.toolbarButton, GUILayout.Width(70)))
			{
				string savesDir = Application.dataPath.Replace("Assets", "Saves");
				string path = EditorUtility.OpenFilePanel("Import Save Data", savesDir, "json,txt");
				if (!string.IsNullOrEmpty(path))
					ImportFromFile(path);
			}

			if (GUILayout.Button(new GUIContent(" Paste", m_iconImport?.image), EditorStyles.toolbarButton, GUILayout.Width(65)))
				ImportFromClipboard();

			var prevBgColor = GUI.backgroundColor;
			GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
			if (GUILayout.Button(new GUIContent(" Delete", m_iconDelete?.image), EditorStyles.toolbarButton, GUILayout.Width(65)))
			{
				if (EditorUtility.DisplayDialog("Confirm", "Delete ALL JObjectDB data from PlayerPrefs?", "Delete", "Cancel"))
				{
					JObjectDB.DeleteAll();
					RefreshData();
				}
			}
			GUI.backgroundColor = prevBgColor;

			EditorGUILayout.EndHorizontal();
		}

		//==========================================================================
		// Left Panel — Collection List
		//==========================================================================

		private void DrawLeftPanel()
		{
			EditorGUILayout.BeginVertical(GUILayout.Width(m_leftPanelWidth));
			EditorGUILayout.LabelField($"Collections ({m_sortedKeys?.Count ?? 0})", EditorStyles.boldLabel);

			m_leftScrollPos = EditorGUILayout.BeginScrollView(m_leftScrollPos);

			if (m_sortedKeys != null)
			{
				foreach (string key in m_sortedKeys)
				{
					bool isSelected = key == m_selectedKey;
					bool isDirty = m_dirtyKeys.Contains(key);
					bool isLoaded = Application.isPlaying && JObjectDB.collections.ContainsKey(key);

					// Background highlight for selected
					var rect = EditorGUILayout.BeginHorizontal();
					if (isSelected)
						EditorGUI.DrawRect(rect, COLOR_SELECTED);

					// Left color bar indicator (no icons)
					var barRect = new Rect(rect.x, rect.y + 2, 3, rect.height - 4);
					if (isDirty)
						EditorGUI.DrawRect(barRect, COLOR_DIRTY);
					else if (isLoaded)
						EditorGUI.DrawRect(barRect, new Color(0.3f, 0.85f, 0.4f));
					
					GUILayout.Space(8);

					// Collection name
					var labelStyle = isSelected ? EditorStyles.boldLabel : EditorStyles.label;
					if (GUILayout.Button(key, labelStyle))
					{
						m_selectedKey = key;
						m_treeScrollPos = Vector2.zero;
						if (m_diffEnabled) ClearDiff();
					}

					// Per-collection reset: clears data back to type defaults but keeps the key in the list.
					if (GUILayout.Button(new GUIContent("⟲", "Reset this collection to default values"), EditorStyles.label, GUILayout.Width(18)))
					{
						if (EditorUtility.DisplayDialog("Reset Collection",
							    $"Reset '{key}' to default values?\nIts saved data will be cleared. The collection stays in the list.",
							    "Reset", "Cancel"))
						{
							JObjectDB.Reset(key);
							RefreshData();
							SetStatus($"✓ Reset '{key}' to defaults");
							GUIUtility.ExitGUI(); // m_sortedKeys was rebuilt; bail out of this OnGUI pass safely.
						}
					}

					// Per-collection delete: removes the key and its data from the database entirely.
					if (GUILayout.Button(new GUIContent("✕", "Delete this collection entirely"), EditorStyles.label, GUILayout.Width(16)))
					{
						if (EditorUtility.DisplayDialog("Delete Collection",
							    $"Delete '{key}' entirely?\nIts key and data are removed from the database.",
							    "Delete", "Cancel"))
						{
							JObjectDB.Delete(key);
							if (m_selectedKey == key) m_selectedKey = null;
							RefreshData();
							SetStatus($"✓ Deleted '{key}'");
							GUIUtility.ExitGUI();
						}
					}

					EditorGUILayout.EndHorizontal();
				}
			}

			EditorGUILayout.EndScrollView();

			// Preset section
			DrawPresetSection();

			EditorGUILayout.EndVertical();
		}

		//==========================================================================
		// Splitter
		//==========================================================================

		private void DrawSplitter()
		{
			var splitterRect = EditorGUILayout.BeginVertical(GUILayout.Width(SPLITTER_WIDTH));
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndVertical();

			EditorGUI.DrawRect(splitterRect, new Color(0.15f, 0.15f, 0.15f));
			EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

			if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
			{
				m_isResizingSplitter = true;
				Event.current.Use();
			}

			if (m_isResizingSplitter)
			{
				if (Event.current.type == EventType.MouseDrag)
				{
					m_leftPanelWidth = Mathf.Clamp(Event.current.mousePosition.x, MIN_LEFT_PANEL_WIDTH, MAX_LEFT_PANEL_WIDTH);
					Event.current.Use();
					Repaint();
				}
				if (Event.current.type == EventType.MouseUp)
				{
					m_isResizingSplitter = false;
					Event.current.Use();
				}
			}
		}

		//==========================================================================
		// Right Panel — Tree View
		//==========================================================================

		private void DrawRightPanel()
		{
			EditorGUILayout.BeginVertical();

			if (string.IsNullOrEmpty(m_selectedKey) || !m_parsedData.ContainsKey(m_selectedKey))
			{
				EditorGUILayout.HelpBox("Select a collection from the left panel.", MessageType.Info);
				EditorGUILayout.EndVertical();
				return;
			}

			// Header
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(m_selectedKey, EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();

			if (m_dirtyKeys.Contains(m_selectedKey))
			{
				var prevBgColor = GUI.backgroundColor;
				GUI.backgroundColor = COLOR_DIRTY;
				if (GUILayout.Button(new GUIContent(" Apply", m_iconApply?.image), GUILayout.Width(75)))
					SaveCollection(m_selectedKey);
				GUI.backgroundColor = prevBgColor;
			}

			if (GUILayout.Button(new GUIContent(" Copy JSON", m_iconCopy?.image), GUILayout.Width(95)))
			{
				string json = m_parsedData[m_selectedKey].ToString(Formatting.Indented);
				EditorGUIUtility.systemCopyBuffer = json;
				SetStatus("✓ JSON copied to clipboard");
			}

			// Diff toggle
			if (m_diffEnabled)
			{
				var prevBgColor = GUI.backgroundColor;
				GUI.backgroundColor = COLOR_DIFF_CHANGED;
				if (GUILayout.Button("Clear Diff", GUILayout.Width(75)))
					ClearDiff();
				GUI.backgroundColor = prevBgColor;
			}
			else
			{
				if (GUILayout.Button("Compare", GUILayout.Width(70)))
				{
					string savesDir = Application.dataPath.Replace("Assets", "Saves");
					string diffPath = EditorUtility.OpenFilePanel("Select file to compare with", savesDir, "json,txt");
					if (!string.IsNullOrEmpty(diffPath))
						StartDiff(diffPath);
				}
			}

			if (GUILayout.Button(new GUIContent(" Edit Raw", m_iconEdit?.image), GUILayout.Width(85)))
			{
				string json = m_parsedData[m_selectedKey].ToString(Formatting.Indented);
				TextEditorWindow.ShowWindow(json, result =>
				{
					try
					{
						var parsed = JToken.Parse(result);
						m_parsedData[m_selectedKey] = parsed;
						MarkDirty(m_selectedKey);
						Repaint();
					}
					catch (Exception ex)
					{
						Debug.LogError($"Invalid JSON: {ex.Message}");
					}
				});
			}

			EditorGUILayout.EndHorizontal();

			// Play-mode warning
			if (Application.isPlaying && m_dirtyKeys.Contains(m_selectedKey))
			{
				EditorGUILayout.HelpBox(
					"⚠ Play Mode: cached values (timers, counters) may not refresh after Apply. Consider pausing the game.",
					MessageType.Warning);
			}

			// Diff legend
			if (m_diffEnabled)
			{
				EditorGUILayout.BeginHorizontal();
				DrawColorLegend(COLOR_DIFF_CHANGED, "Changed");
				DrawColorLegend(COLOR_DIFF_ADDED, "New");
				DrawColorLegend(COLOR_DIFF_REMOVED, "Removed");
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();
			}

			// Tree view
			m_treeScrollPos = EditorGUILayout.BeginScrollView(m_treeScrollPos);
			{
				var token = m_parsedData[m_selectedKey];
				DrawTreeNode(m_selectedKey, "", token, 0);

				// Show removed fields at the end
				if (m_diffEnabled && m_diffRemovedPaths.Count > 0)
				{
					GUILayout.Space(8);
					var prevColor = GUI.contentColor;
					GUI.contentColor = new Color(1f, 0.4f, 0.4f);
					EditorGUILayout.LabelField("── Removed Fields ──", EditorStyles.boldLabel);
					foreach (string removedPath in m_diffRemovedPaths.OrderBy(p => p))
					{
						var rect = EditorGUILayout.GetControlRect();
						EditorGUI.DrawRect(rect, COLOR_DIFF_REMOVED);
						EditorGUI.LabelField(rect, $"  ✕ {removedPath}");
					}
					GUI.contentColor = prevColor;
				}
			}
			EditorGUILayout.EndScrollView();

			EditorGUILayout.EndVertical();
		}

	}
}
