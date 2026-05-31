/**
 * Author HNB-RaBear - 2024
 * JObjectDBWindow — partial: Search
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
		// Search
		//==========================================================================

		private void RebuildSearchCache()
		{
			if (string.IsNullOrEmpty(m_appliedSearchQuery))
			{
				m_searchMatchedPaths = null;
				m_searchAncestorPaths = null;
				return;
			}

			m_searchMatchedPaths = new HashSet<string>();
			m_searchAncestorPaths = new HashSet<string>();

			if (string.IsNullOrEmpty(m_selectedKey) || !m_parsedData.TryGetValue(m_selectedKey, out var token))
				return;

			string query = m_appliedSearchQuery.ToLowerInvariant();
			SearchRecursive(token, "", query);
		}

		/// <summary>
		/// Recursively walks the JToken tree, collecting paths whose field names or values match the query.
		/// Also collects all ancestor paths so they can be shown/auto-expanded.
		/// </summary>
		private void SearchRecursive(JToken token, string path, string query)
		{
			if (token.Type == JTokenType.Object)
			{
				foreach (var prop in ((NJObject)token).Properties())
				{
					string fieldPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";

					if (m_searchMode == SearchMode.Key)
					{
						if (prop.Name.ToLowerInvariant().Contains(query))
						{
							m_searchMatchedPaths.Add(fieldPath);
							AddAncestorPaths(fieldPath);
						}
					}
					else if (IsLeafValueMatch(prop.Value, query))
					{
						m_searchMatchedPaths.Add(fieldPath);
						AddAncestorPaths(fieldPath);
					}

					SearchRecursive(prop.Value, fieldPath, query);
				}
			}
			else if (token.Type == JTokenType.Array)
			{
				var arr = (NJArray)token;
				for (int i = 0; i < arr.Count; i++)
				{
					string elementPath = $"{path}[{i}]";

					if (m_searchMode == SearchMode.Value && IsLeafValueMatch(arr[i], query))
					{
						m_searchMatchedPaths.Add(elementPath);
						AddAncestorPaths(elementPath);
					}

					SearchRecursive(arr[i], elementPath, query);
				}
			}
		}

		/// <summary>
		/// Returns true if the token is a leaf (non-container) and its string representation contains the query.
		/// </summary>
		private static bool IsLeafValueMatch(JToken token, string query)
		{
			if (token.Type == JTokenType.Object || token.Type == JTokenType.Array)
				return false;
			string valueStr = token.Type == JTokenType.Null ? "null" : token.ToString();
			return valueStr.ToLowerInvariant().Contains(query);
		}

		private void AddAncestorPaths(string path)
		{
			// Walk up the path adding all ancestors: "a.b.c" → "a.b", "a"
			for (int i = path.Length - 1; i >= 0; i--)
			{
				if (path[i] == '.' || path[i] == '[')
				{
					string ancestor = path.Substring(0, i);
					if (!m_searchAncestorPaths.Add(ancestor))
						break; // Already added this and all its ancestors
				}
			}
		}

		private bool IsVisibleInSearch(string path, JToken token)
		{
			if (m_searchMatchedPaths == null)
				return true; // No search active

			// This path itself is a match
			if (m_searchMatchedPaths.Contains(path))
				return true;

			// This path is an ancestor of a match (parent of a matched field)
			if (m_searchAncestorPaths.Contains(path))
				return true;

			// This path is a descendant of a matched container
			// e.g. search "currencies" matches container → children "currencies.coins" should be visible
			for (int i = 0; i < path.Length; i++)
			{
				if (path[i] == '.' || path[i] == '[')
				{
					if (m_searchMatchedPaths.Contains(path.Substring(0, i)))
						return true;
				}
			}

			return false;
		}

		private bool IsSearchMatch(string path)
		{
			return m_searchMatchedPaths != null && m_searchMatchedPaths.Contains(path);
		}

	}
}
