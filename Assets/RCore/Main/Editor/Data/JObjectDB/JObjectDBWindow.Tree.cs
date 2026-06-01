/**
 * Author HNB-RaBear - 2024
 * JObjectDBWindow — partial: Tree
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
		// Tree Node Rendering (Recursive)
		//==========================================================================

		private void DrawTreeNode(string collectionKey, string path, JToken token, int depth)
		{
			if (token == null) return;

			switch (token.Type)
			{
				case JTokenType.Object:
					DrawObjectNode(collectionKey, path, (NJObject)token, depth);
					break;

				case JTokenType.Array:
					DrawArrayNode(collectionKey, path, (NJArray)token, depth);
					break;

				default:
					// Leaf values are drawn by their parent
					break;
			}
		}

		private void DrawObjectNode(string collectionKey, string path, NJObject obj, int depth)
		{
			foreach (var property in obj.Properties())
			{
				string fieldPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";

				// Search filtering
				if (!IsVisibleInSearch(fieldPath, property.Value))
					continue;

				if (property.Value.Type == JTokenType.Object || property.Value.Type == JTokenType.Array)
				{
					DrawFoldoutContainer(collectionKey, fieldPath, property.Name, property.Value, depth);
				}
				else
				{
					DrawValueField(collectionKey, fieldPath, property.Name, property, depth);
				}
			}
		}

		private void DrawArrayNode(string collectionKey, string path, NJArray array, int depth)
		{
			bool isSearchActive = m_searchMatchedPaths != null;
			int totalItems = array.Count;
			
			// When search is active, skip pagination — show all matching items
			int startIndex, endIndex;
			if (isSearchActive)
			{
				startIndex = 0;
				endIndex = totalItems;
			}
			else
			{
				string pageKey = path;
				if (!m_arrayPages.TryGetValue(pageKey, out int currentPage))
					currentPage = 0;

				startIndex = currentPage * ARRAY_PAGE_SIZE;
				endIndex = Mathf.Min(startIndex + ARRAY_PAGE_SIZE, totalItems);
			}

			for (int i = startIndex; i < endIndex; i++)
			{
				string elementPath = $"{path}[{i}]";
				var element = array[i];

				if (!IsVisibleInSearch(elementPath, element))
					continue;

				if (element.Type == JTokenType.Object || element.Type == JTokenType.Array)
				{
					DrawFoldoutContainer(collectionKey, elementPath, $"[{i}]", element, depth);
				}
				else
				{
					DrawValueField(collectionKey, elementPath, $"[{i}]", null, depth, element, i, array);
				}
			}

			// Pagination controls — only when NOT searching
			if (!isSearchActive && totalItems > ARRAY_PAGE_SIZE)
			{
				string pageKey = path;
				if (!m_arrayPages.TryGetValue(pageKey, out int currentPage))
					currentPage = 0;
					
				int prevIndent = EditorGUI.indentLevel;
				EditorGUI.indentLevel = depth + 1;
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(depth * INDENT_WIDTH);

				int totalPages = Mathf.CeilToInt((float)totalItems / ARRAY_PAGE_SIZE);
				GUI.enabled = currentPage > 0;
				if (GUILayout.Button("◀", GUILayout.Width(30)))
				{
					m_arrayPages[pageKey] = currentPage - 1;
					Repaint();
				}
				GUI.enabled = true;

				GUILayout.Label($"Page {currentPage + 1}/{totalPages} ({totalItems} items)", EditorStyles.centeredGreyMiniLabel);

				GUI.enabled = currentPage < totalPages - 1;
				if (GUILayout.Button("▶", GUILayout.Width(30)))
				{
					m_arrayPages[pageKey] = currentPage + 1;
					Repaint();
				}
				GUI.enabled = true;

				EditorGUILayout.EndHorizontal();
				EditorGUI.indentLevel = prevIndent;
			}
		}

		private void DrawFoldoutContainer(string collectionKey, string path, string label, JToken token, int depth)
		{
			bool wasExpanded = m_expandedPaths.Contains(path);
			// Auto-expand when search matches this node or a descendant
			if (m_searchMatchedPaths != null && (m_searchMatchedPaths.Contains(path)
			    || (m_searchAncestorPaths != null && m_searchAncestorPaths.Contains(path))))
				wasExpanded = true;

			var foldoutRect = EditorGUILayout.BeginHorizontal();
			if (IsSearchMatch(path))
				EditorGUI.DrawRect(foldoutRect, COLOR_SEARCH_MATCH);
			else if (m_diffEnabled && HasDiffChildren(path))
				EditorGUI.DrawRect(foldoutRect, new Color(1f, 0.7f, 0.2f, 0.1f));
			GUILayout.Space(depth * INDENT_WIDTH);

			// Count info
			string countLabel;
			bool isArray = token.Type == JTokenType.Array;
			if (isArray)
				countLabel = $"[{((NJArray)token).Count} items]";
			else
				countLabel = $"{{{((NJObject)token).Count} fields}}";

			bool isExpanded = EditorGUILayout.Foldout(wasExpanded, $"{label}  {countLabel}", true);

			// Array: Add item button
			if (isArray)
			{
				if (GUILayout.Button("+", GUILayout.Width(20), GUILayout.Height(16)))
				{
					var arr = (NJArray)token;
					// Infer type from last element, default to int 0
					JToken newItem = arr.Count > 0 ? arr.Last.DeepClone() : new JValue(0);
					arr.Add(newItem);
					MarkDirty(collectionKey);
				}
			}

			// Copy button
			DrawCopyButton(path, token.ToString(Formatting.None));

			EditorGUILayout.EndHorizontal();

			if (isExpanded != wasExpanded)
			{
				if (isExpanded)
					m_expandedPaths.Add(path);
				else
					m_expandedPaths.Remove(path);
			}

			if (isExpanded)
				DrawTreeNode(collectionKey, path, token, depth + 1);
		}

		//==========================================================================
		// Value Field Rendering + Inline Editing
		//==========================================================================

		private void DrawValueField(string collectionKey, string path, string label, JProperty property, int depth,
			JToken directToken = null, int arrayIndex = -1, NJArray parentArray = null)
		{
			var token = property?.Value ?? directToken;
			if (token == null) return;

			// Get full-width rect for this row
			float indent = depth * INDENT_WIDTH;
			float labelWidth = Mathf.Max(120, 200 - indent);
			bool isArrayElement = parentArray != null && arrayIndex >= 0;
			float buttonsWidth = COPY_BUTTON_WIDTH + (isArrayElement ? COPY_BUTTON_WIDTH + 2 : 0);
			
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(indent);
			var rowRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
			EditorGUILayout.EndHorizontal();
			
			// Split rect into: [Label] [Value] [DeleteBtn?] [CopyButton]
			var labelRect = new Rect(rowRect.x, rowRect.y, labelWidth, rowRect.height);
			var copyRect = new Rect(rowRect.xMax - COPY_BUTTON_WIDTH, rowRect.y, COPY_BUTTON_WIDTH, rowRect.height);
			Rect deleteRect = default;
			if (isArrayElement)
				deleteRect = new Rect(copyRect.x - COPY_BUTTON_WIDTH - 2, rowRect.y, COPY_BUTTON_WIDTH, rowRect.height);
			var valueRect = new Rect(labelRect.xMax + 2, rowRect.y, rowRect.xMax - buttonsWidth - labelRect.xMax - 4, rowRect.height);

			// Search highlight + Diff highlight
			if (IsSearchMatch(path))
				EditorGUI.DrawRect(rowRect, COLOR_SEARCH_MATCH);
			else if (m_diffEnabled && m_diffChangedPaths.Contains(path))
				EditorGUI.DrawRect(rowRect, COLOR_DIFF_CHANGED);
			else if (m_diffEnabled && m_diffAddedPaths.Contains(path))
				EditorGUI.DrawRect(rowRect, COLOR_DIFF_ADDED);

			// Label
			EditorGUI.LabelField(labelRect, label);

			// Value with type-specific color
			var prevColor = GUI.contentColor;

			EditorGUI.BeginChangeCheck();
			JToken newValue = null;

			switch (token.Type)
			{
				case JTokenType.Integer:
					GUI.contentColor = COLOR_INT;
					long intVal = token.Value<long>();
					long newIntVal = EditorGUI.LongField(valueRect, intVal);
					newValue = new JValue(newIntVal);
					break;

				case JTokenType.Float:
					GUI.contentColor = COLOR_FLOAT;
					double floatVal = token.Value<double>();
					double newFloatVal = EditorGUI.DoubleField(valueRect, floatVal);
					newValue = new JValue(newFloatVal);
					break;

				case JTokenType.String:
					GUI.contentColor = COLOR_STRING;
					string strVal = token.Value<string>() ?? "";
					string newStrVal = EditorGUI.TextField(valueRect, strVal);
					newValue = new JValue(newStrVal);
					break;

				case JTokenType.Boolean:
					GUI.contentColor = COLOR_BOOL;
					bool boolVal = token.Value<bool>();
					bool newBoolVal = EditorGUI.Toggle(valueRect, boolVal);
					newValue = new JValue(newBoolVal);
					break;

				case JTokenType.Null:
					GUI.contentColor = COLOR_NULL;
					EditorGUI.LabelField(valueRect, "(null)");
					break;

				default:
					EditorGUI.LabelField(valueRect, token.ToString());
					break;
			}

			bool changed = EditorGUI.EndChangeCheck();
			if (!changed)
				newValue = null;

			GUI.contentColor = prevColor;

			// Apply change
			if (newValue != null)
			{
				if (property != null)
					property.Value = newValue;
				else if (parentArray != null && arrayIndex >= 0)
					parentArray[arrayIndex] = newValue;

				MarkDirty(collectionKey);
			}

			// Copy button — absolute positioned, always visible
			DrawCopyButton(copyRect, path, token.ToString());

			// Delete button for array elements
			if (isArrayElement)
			{
				var prevBgColor = GUI.backgroundColor;
				GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
				if (GUI.Button(deleteRect, "✕"))
				{
					parentArray.RemoveAt(arrayIndex);
					MarkDirty(collectionKey);
					GUIUtility.ExitGUI(); // Prevent layout errors after modifying collection
				}
				GUI.backgroundColor = prevBgColor;
			}

			// Timestamp display
			if (token.Type == JTokenType.Integer && IsTimestampField(label))
			{
				long ts = token.Value<long>();
				if (ts > 1000000000 && ts < 9999999999) // reasonable Unix timestamp range
				{
					var prevContentColor = GUI.contentColor;
					GUI.contentColor = COLOR_TIMESTAMP;
					EditorGUILayout.BeginHorizontal();
					GUILayout.Space(depth * INDENT_WIDTH + 20);
					try
					{
						var dt = DateTimeOffset.FromUnixTimeSeconds(ts).LocalDateTime;
						EditorGUILayout.LabelField($"📅 {dt:yyyy-MM-dd HH:mm:ss}", EditorStyles.miniLabel);
					}
					catch
					{
						EditorGUILayout.LabelField("📅 (invalid timestamp)", EditorStyles.miniLabel);
					}
					EditorGUILayout.EndHorizontal();
					GUI.contentColor = prevContentColor;
				}
			}
		}

		//==========================================================================
		// Copy Button
		//==========================================================================

		private void DrawCopyButton(string path, string value)
		{
			bool justCopied = m_copyFeedbackPath == path
			                  && EditorApplication.timeSinceStartup - m_copyFeedbackTime < 1.0;

			if (justCopied)
				GUI.backgroundColor = Color.green;

			var icon = justCopied ? m_iconCopyDone : m_iconCopy;
			var content = icon != null ? icon : new GUIContent(justCopied ? "✓" : "C");
			if (GUILayout.Button(content, GUILayout.Width(COPY_BUTTON_WIDTH), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
			{
				EditorGUIUtility.systemCopyBuffer = value;
				m_copyFeedbackPath = path;
				m_copyFeedbackTime = EditorApplication.timeSinceStartup;
			}

			if (justCopied)
				GUI.backgroundColor = Color.white;
		}
		
		private void DrawCopyButton(Rect rect, string path, string value)
		{
			bool justCopied = m_copyFeedbackPath == path
			                  && EditorApplication.timeSinceStartup - m_copyFeedbackTime < 1.0;

			if (justCopied)
				GUI.backgroundColor = Color.green;

			var icon = justCopied ? m_iconCopyDone : m_iconCopy;
			var content = icon != null ? icon : new GUIContent(justCopied ? "✓" : "C");
			if (GUI.Button(rect, content))
			{
				EditorGUIUtility.systemCopyBuffer = value;
				m_copyFeedbackPath = path;
				m_copyFeedbackTime = EditorApplication.timeSinceStartup;
			}

			if (justCopied)
				GUI.backgroundColor = Color.white;
		}

		//==========================================================================
		// Utility — Timestamp
		//==========================================================================

		private static bool IsTimestampField(string fieldName)
		{
			if (string.IsNullOrEmpty(fieldName)) return false;
			string name = fieldName.TrimStart('[').TrimEnd(']');

			// Suffix match on a word boundary (camelCase "createdAt" / snake_case "created_at"),
			// so unrelated names like "format", "seat" or "stat" are not mistaken for timestamps.
			foreach (string suffix in TIMESTAMP_SUFFIXES)
			{
				if (EndsWithWord(name, suffix))
					return true;
			}

			// Keyword match anywhere in the name.
			string lower = name.ToLowerInvariant();
			if (lower.Contains("timestamp") || lower.Contains("expired")
			    || lower.Contains("lastactive") || lower.Contains("firstactive"))
				return true;

			return false;
		}

		/// <summary>
		/// Returns true if <paramref name="name"/> ends with <paramref name="suffix"/> (case-insensitive)
		/// at a word boundary — the match is the whole name, starts right after an underscore, or starts
		/// at a camelCase hump (uppercase letter). Prevents "format" from matching the "at" suffix.
		/// </summary>
		private static bool EndsWithWord(string name, string suffix)
		{
			if (name.Length < suffix.Length) return false;
			int idx = name.Length - suffix.Length;
			if (string.Compare(name, idx, suffix, 0, suffix.Length, StringComparison.OrdinalIgnoreCase) != 0)
				return false;
			if (idx == 0) return true; // whole word
			char start = name[idx];
			char before = name[idx - 1];
			return char.IsUpper(start) || before == '_' || !char.IsLetter(before);
		}
	}
}
