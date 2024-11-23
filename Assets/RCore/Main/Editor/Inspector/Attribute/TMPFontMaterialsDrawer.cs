/***
 * Author HNB-RaBear - 2024
 **/

using RCore.Inspector;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Inspector
{
	[CustomPropertyDrawer(typeof(TMPFontMaterialsAttribute))]
	public class TMPFontMaterialsDrawer : PropertyDrawer
	{
		private static Material[] m_cachedMaterials;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			// Ensure the property is of type Material
			if (property.propertyType == SerializedPropertyType.ObjectReference)
			{
				// Get the TMP_FontAsset from the serialized object
				var fontAsset = GetFontAsset(property);
				if (fontAsset != null)
				{
					// Get all material references for the font asset
					var materials = FindMaterialReferences(fontAsset);

					// Display a popup to select from the materials
					var options = new List<string>();
					foreach (var mat in materials)
						options.Add(mat.name);

					int currentIndex = System.Array.IndexOf(materials, property.objectReferenceValue);
					currentIndex = EditorGUI.Popup(position, label.text, currentIndex, options.ToArray());
					// EditorGUI.Popup(position, label: label.text, selectedIndex: currentIndex, displayedOptions: options.ToArray());

					if (currentIndex >= 0)
					{
						property.objectReferenceValue = materials[currentIndex];
					}
				}
				else
				{
					EditorGUI.LabelField(position, label.text, "TMP_FontAsset not found.");
				}
			}
			else
			{
				EditorGUI.LabelField(position, label.text, "Use TMPFontMaterials with Material.");
			}

			EditorGUI.EndProperty();
		}

		private TMP_FontAsset GetFontAsset(SerializedProperty property)
		{
			var targetObject = property.serializedObject.targetObject as MonoBehaviour;
			if (targetObject == null)
			{
				return null;
			}

			var fontAsset = targetObject.GetComponentInChildren<TextMeshProUGUI>(true);
			if (fontAsset != null)
				return fontAsset.font;
			return null;
		}

		private static Material[] FindMaterialReferences(TMP_FontAsset fontAsset)
		{
			if (m_cachedMaterials != null && m_cachedMaterials.Length > 0)
				return m_cachedMaterials;

			var refs = new List<Material>();
			var mat = fontAsset.material;
			refs.Add(mat);

			// Get materials matching the search pattern
			string searchPattern = "t:Material " + fontAsset.name.Split(new char[] { ' ' })[0];
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
			}

			m_cachedMaterials = refs.ToArray();
			return m_cachedMaterials;
		}

		[InitializeOnLoadMethod]
		private static void ResetCache()
		{
			m_cachedMaterials = null;
		}
	}
}