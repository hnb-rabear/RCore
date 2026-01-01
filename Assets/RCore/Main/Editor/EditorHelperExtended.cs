using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	/// <summary>
	/// Provides extended helper methods for the Unity Editor, including TMP material handling and missing script removal.
	/// </summary>
	public static class EditorHelperExtended
	{
		/// <summary>
		/// Gets a list of material preset names associated with the given TMP Font Asset.
		/// </summary>
		public static string[] GetTMPMaterialPresets(TMPro.TMP_FontAsset fontAsset)
		{
			if (fontAsset == null) return null;

			var materialReferences = FindMaterialReferences(fontAsset);
			var materialPresetNames = new string[materialReferences.Length];

			for (int i = 0; i < materialPresetNames.Length; i++)
				materialPresetNames[i] = materialReferences[i].name;

			return materialPresetNames;
		}

		/// <summary>
		/// Finds all materials that reference the texture of the given TMP Font Asset.
		/// </summary>
		public static Material[] FindMaterialReferences(TMP_FontAsset fontAsset)
		{
			var refs = new List<Material>();
			var mat = fontAsset.material;
			refs.Add(mat);

			// Get materials matching the search pattern.
			string searchPattern = "t:Material" + " " + fontAsset.name.Split(new char[] { ' ' })[0];
			string[] materialAssetGUIDs = AssetDatabase.FindAssets(searchPattern);

			for (int i = 0; i < materialAssetGUIDs.Length; i++)
			{
				string materialPath = AssetDatabase.GUIDToAssetPath(materialAssetGUIDs[i]);
				var targetMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

				if (targetMaterial.HasProperty(ShaderUtilities.ID_MainTex)
				    && targetMaterial.GetTexture(ShaderUtilities.ID_MainTex) != null
				    && mat.GetTexture(ShaderUtilities.ID_MainTex) != null
				    && targetMaterial.GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID() == mat.GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID())
				{
					if (!refs.Contains(targetMaterial))
						refs.Add(targetMaterial);
				}
				else
				{
					// TODO: Find a more efficient method to unload resources.
					//Resources.UnloadAsset(targetMaterial.GetTexture(ShaderUtilities.ID_MainTex));
				}
			}

			return refs.ToArray();
		}

		/// <summary>
		/// Scans selected GameObjects (and their children/prefabs) for missing MonoBehaviours and removes them.
		/// </summary>
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

		/// <summary>
		/// Draws a date time picker field in the inspector.
		/// </summary>
		public static void DateTimePicker(DateTime? value, string label, int labelWidth, Action<DateTime?> onSelect)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
			if (value != null)
				EditorGUILayout.SelectableLabel(value.Value.ToString(CultureInfo.CurrentCulture), EditorStyles.textField,
					GUILayout.Height(EditorGUIUtility.singleLineHeight));
			else
				EditorGUILayout.SelectableLabel("No expiry", EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));

			if (EditorHelper.Button("Set Date"))
			{
				var window = DateTimePickerWindow.ShowWindow(value ?? default, true);
				window.onQuit = datetime => onSelect?.Invoke(datetime);
			}
			if (EditorHelper.Button("Remove Date"))
			{
				onSelect?.Invoke(null);
			}
			EditorGUILayout.EndHorizontal();
		}
	}
}