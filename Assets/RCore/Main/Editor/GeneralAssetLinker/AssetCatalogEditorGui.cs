using System;
using RCore.Config;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	public static class AssetCatalogEditorGui
	{
		public static string StripAssetsPrefix(string pPath)
		{
			if (pPath != null && pPath.StartsWith("Assets/", StringComparison.Ordinal))
				return pPath.Substring("Assets/".Length);
			return pPath ?? string.Empty;
		}

		public static void DrawAssetPath(UnityEngine.Object pAsset)
		{
			var assetPath = AssetDatabase.GetAssetPath(pAsset);
			if (string.IsNullOrEmpty(assetPath))
				return;

			EditorGUILayout.BeginHorizontal();
			var displayPath = StripAssetsPrefix(assetPath);
			EditorGUILayout.LabelField("Path", displayPath, EditorStyles.miniLabel);
			if (GUILayout.Button("Ping", GUILayout.Width(40)))
				EditorGUIUtility.PingObject(pAsset);
			EditorGUILayout.EndHorizontal();
		}

		public static bool DrawQuickAdd(AssetCatalog pCatalog, CatalogAssetType pType, ref bool pShowQuickAdd, ref string pCategory, string pAssetName)
		{
			EditorGUILayout.Space();
			if (!pShowQuickAdd)
			{
				if (GUILayout.Button($"Add '{pAssetName}' to AssetCatalog", GUILayout.Height(30)))
					pShowQuickAdd = true;
				return false;
			}

			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("Quick Add to Catalog", EditorStyles.boldLabel);
			pCategory = EditorGUILayout.TextField("Category", pCategory);

			var existingCategories = pCatalog.EditorGetDistinctCategories(pType);
			if (existingCategories.Length > 0)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Existing:", GUILayout.Width(EditorGUIUtility.labelWidth - 4));
				foreach (var category in existingCategories)
				{
					if (GUILayout.Button(category, EditorStyles.miniButton))
						pCategory = category;
				}
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();
			var confirmed = GUILayout.Button("Confirm Add");
			if (GUILayout.Button("Cancel"))
				pShowQuickAdd = false;
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
			return confirmed;
		}
	}
}
