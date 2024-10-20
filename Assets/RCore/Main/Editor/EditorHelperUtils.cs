using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RCore.Common;
using Debug = UnityEngine.Debug;

namespace RCore.Editor
{
	public static class EditorHelperUtils
	{
		public static string[] GetTMPMaterialPresets(TMPro.TMP_FontAsset fontAsset)
		{
			if (fontAsset == null) return null;

			var materialReferences = TMPro.EditorUtilities.TMP_EditorUtility.FindMaterialReferences(fontAsset);
			var materialPresetNames = new string[materialReferences.Length];

			for (int i = 0; i < materialPresetNames.Length; i++)
				materialPresetNames[i] = materialReferences[i].name;

			return materialPresetNames;
		}

		[MenuItem("GameObject/RCore/Remove Missing Script In GameObject")]
		[MenuItem("Assets/RCore/Find Missing Script In GameObject")]
		public static void RemoveMissingScriptInGameObject()
		{
			var gos = Selection.gameObjects;
			var invalidGOs = new List<GameObject>();
			for (int i = 0; i < gos.Length; i++)
			{
				bool found = false;
				bool isPrefab = gos[i].IsPrefab();
				var components = gos[i].GetComponents<Component>();
				for (int j = components.Length - 1; j >= 0; j--)
				{
					if (components[j] == null)
					{
						Debug.Log(gos[i].gameObject.name + " is missing component! Let clear it!");
						GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gos[i].gameObject);
						found = true;
						if (isPrefab)
							invalidGOs.Add(gos[i]);
					}
				}

				var children = gos[i].GetAllChildren();
				for (int k = children.Count - 1; k >= 0; k--)
				{
					var childComponents = children[k].GetComponents<Component>();
					for (int j = childComponents.Length - 1; j >= 0; j--)
					{
						if (childComponents[j] == null)
						{
							Debug.Log(children[k].gameObject.name + " is missing component! Let clear it!");
							GameObjectUtility.RemoveMonoBehavioursWithMissingScript(children[k].gameObject);
							found = true;
							if (isPrefab)
								invalidGOs.Add(gos[i]);
						}
					}
				}
				if (found)
					EditorUtility.SetDirty(gos[i]);
				if (invalidGOs.Count > 0)
					Selection.objects = invalidGOs.ToArray();
			}
		}
	}
}