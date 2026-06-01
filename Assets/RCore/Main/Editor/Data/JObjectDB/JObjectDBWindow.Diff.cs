/**
 * Author HNB-RaBear - 2024
 * JObjectDBWindow — partial: Diff
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
		// Diff/Compare (Feature 5)
		//==========================================================================

		private void StartDiff(string filePath)
		{
			try
			{
				string content = File.ReadAllText(filePath);
				var parsed = NJObject.Parse(content);

				// Support wrapped format
				JToken dataToken = parsed;
				if (parsed.TryGetValue("data", out var wrappedData) && wrappedData.Type == JTokenType.Object)
					dataToken = wrappedData;

				// Extract the selected collection's data from the diff file
				if (string.IsNullOrEmpty(m_selectedKey))
					return;

				var dataDict = dataToken as NJObject;
				if (dataDict == null || !dataDict.TryGetValue(m_selectedKey, out var collectionJson))
				{
					EditorUtility.DisplayDialog("Diff Error",
						$"Collection '{m_selectedKey}' not found in the comparison file.", "OK");
					return;
				}

				// Parse the collection JSON (it's stored as a string value in the dict)
				JToken baseToken;
				if (collectionJson.Type == JTokenType.String)
					baseToken = JToken.Parse(collectionJson.Value<string>());
				else
					baseToken = collectionJson;

				m_diffBaseToken = baseToken;
				m_diffEnabled = true;

				// Build diff paths
				RebuildDiffCache();
				SetStatus($"✓ Comparing with {Path.GetFileName(filePath)}");
			}
			catch (Exception ex)
			{
				EditorUtility.DisplayDialog("Diff Error", $"Failed to load comparison file:\n{ex.Message}", "OK");
			}
		}

		private void ClearDiff()
		{
			m_diffEnabled = false;
			m_diffBaseToken = null;
			m_diffChangedPaths.Clear();
			m_diffAddedPaths.Clear();
			m_diffRemovedPaths.Clear();
			m_diffAncestorPaths.Clear();
			SetStatus("Diff cleared");
		}

		private void RebuildDiffCache()
		{
			m_diffChangedPaths.Clear();
			m_diffAddedPaths.Clear();
			m_diffRemovedPaths.Clear();
			m_diffAncestorPaths.Clear();

			if (!m_diffEnabled || m_diffBaseToken == null || string.IsNullOrEmpty(m_selectedKey))
				return;

			var currentToken = GetParsed(m_selectedKey);
			if (currentToken == null)
				return;

			CompareDiffTokens(currentToken, m_diffBaseToken, "");

			// Precompute ancestor paths for all diffs (reuse search's AddAncestorPaths logic)
			foreach (string p in m_diffChangedPaths) AddDiffAncestorPaths(p);
			foreach (string p in m_diffAddedPaths) AddDiffAncestorPaths(p);
			foreach (string p in m_diffRemovedPaths) AddDiffAncestorPaths(p);
		}

		private void AddDiffAncestorPaths(string path)
		{
			for (int i = path.Length - 1; i >= 0; i--)
			{
				if (path[i] == '.' || path[i] == '[')
				{
					if (!m_diffAncestorPaths.Add(path.Substring(0, i)))
						break;
				}
			}
		}

		private void CompareDiffTokens(JToken current, JToken baseline, string path)
		{
			if (current.Type == JTokenType.Object && baseline.Type == JTokenType.Object)
			{
				var currentObj = (NJObject)current;
				var baseObj = (NJObject)baseline;

				// Check current fields
				foreach (var prop in currentObj.Properties())
				{
					string fieldPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";
					if (baseObj.TryGetValue(prop.Name, out var baseProp))
						CompareDiffTokens(prop.Value, baseProp, fieldPath);
					else
						m_diffAddedPaths.Add(fieldPath); // New field
				}

				// Check removed fields
				foreach (var prop in baseObj.Properties())
				{
					string fieldPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";
					if (!currentObj.ContainsKey(prop.Name))
						m_diffRemovedPaths.Add(fieldPath);
				}
			}
			else if (current.Type == JTokenType.Array && baseline.Type == JTokenType.Array)
			{
				var currentArr = (NJArray)current;
				var baseArr = (NJArray)baseline;
				int maxLen = Math.Max(currentArr.Count, baseArr.Count);

				for (int i = 0; i < maxLen; i++)
				{
					string elemPath = $"{path}[{i}]";
					if (i >= currentArr.Count)
						m_diffRemovedPaths.Add(elemPath);
					else if (i >= baseArr.Count)
						m_diffAddedPaths.Add(elemPath);
					else
						CompareDiffTokens(currentArr[i], baseArr[i], elemPath);
				}
			}
			else
			{
				// Leaf comparison
				if (!JToken.DeepEquals(current, baseline))
					m_diffChangedPaths.Add(path);
			}
		}

		private bool HasDiffChildren(string path)
		{
			return m_diffAncestorPaths.Contains(path);
		}

		private static void DrawColorLegend(Color color, string label)
		{
			var rect = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(12));
			EditorGUI.DrawRect(rect, color);
			GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.Width(55));
		}

	}
}
