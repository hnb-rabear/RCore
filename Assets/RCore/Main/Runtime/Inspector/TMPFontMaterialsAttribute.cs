using System.Collections.Generic;
using System.Linq;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace RCore.Inspector
{
	/// <summary>
	/// Attribute for a Material field that displays a dropdown of all materials
	/// associated with a TMP_FontAsset found on the same GameObject.
	/// This is useful for easily switching between font presets (e.g., outline, glow).
	/// </summary>
	public class TMPFontMaterialsAttribute : PropertyAttribute { }

#if UNITY_EDITOR
	/// <summary>
	/// Custom property drawer for TMPFontMaterialsAttribute.
	/// </summary>
	[CustomPropertyDrawer(typeof(TMPFontMaterialsAttribute))]
	public class TMPFontMaterialsDrawer : PropertyDrawer
	{
		private static Dictionary<int, Material[]> s_materialCache = new Dictionary<int, Material[]>();

		/// <summary>
		/// Renders the property as a dropdown of available font materials.
		// </summary>
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			// This attribute only works on Material fields.
			if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue != null && !(property.objectReferenceValue is Material))
			{
				EditorGUI.LabelField(position, label.text, "Use TMPFontMaterials with Material fields.");
				EditorGUI.EndProperty();
				return;
			}
			
			var fontAsset = GetFontAsset(property);
			if (fontAsset != null)
			{
				var materials = FindMaterialReferences(fontAsset);
				var options = materials.Select(mat => mat.name).ToArray();
				int currentIndex = System.Array.IndexOf(materials, property.objectReferenceValue as Material);

				// Draw the popup and update the property with the selected material.
				int newIndex = EditorGUI.Popup(position, label.text, currentIndex, options);
				if (newIndex >= 0 && newIndex < materials.Length)
				{
					property.objectReferenceValue = materials[newIndex];
				}
			}
			else
			{
				EditorGUI.LabelField(position, label.text, "TMP_FontAsset not found on this GameObject.");
			}

			EditorGUI.EndProperty();
		}

		/// <summary>
		/// Finds the TMP_FontAsset by searching for a TextMeshProUGUI component
		/// on the same GameObject as the inspected component.
		/// </summary>
		private TMP_FontAsset GetFontAsset(SerializedProperty property)
		{
			if (property.serializedObject.targetObject is Component component)
			{
				// Find a TextMeshPro or TextMeshProUGUI component on the same GameObject or its children.
				var textComponent = component.GetComponentInChildren<TMP_Text>(true);
				if (textComponent != null)
				{
					return textComponent.font;
				}
			}
			return null;
		}

		/// <summary>
		/// Finds all materials in the project that use the same texture atlas as the font's default material.
		/// Uses a cache for better performance.
		/// </summary>
		private static Material[] FindMaterialReferences(TMP_FontAsset fontAsset)
		{
			int fontAssetId = fontAsset.GetInstanceID();
			if (s_materialCache.TryGetValue(fontAssetId, out var cachedMaterials))
			{
				return cachedMaterials;
			}

			var refs = new HashSet<Material>();
			var baseMaterial = fontAsset.material;
			refs.Add(baseMaterial);

			var baseTexture = baseMaterial.GetTexture(ShaderUtilities.ID_MainTex);
			if (baseTexture == null)
			{
				s_materialCache[fontAssetId] = refs.ToArray();
				return s_materialCache[fontAssetId];
			}

			// Find all material assets in the project.
			string[] materialAssetGUIDs = AssetDatabase.FindAssets("t:Material");
			foreach (var guid in materialAssetGUIDs)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var material = AssetDatabase.LoadAssetAtPath<Material>(path);

				// Check if the material uses the same font texture atlas.
				if (material != null && material.HasProperty(ShaderUtilities.ID_MainTex))
				{
					var texture = material.GetTexture(ShaderUtilities.ID_MainTex);
					if (texture != null && texture.GetInstanceID() == baseTexture.GetInstanceID())
					{
						refs.Add(material);
					}
				}
			}
			
			s_materialCache[fontAssetId] = refs.ToArray();
			return s_materialCache[fontAssetId];
		}

		/// <summary>
		/// Clears the material cache when the editor recompiles or enters play mode.
		/// </summary>
		[InitializeOnLoadMethod]
		private static void ClearCacheOnReload()
		{
			s_materialCache.Clear();
			EditorApplication.playModeStateChanged += (state) => {
				if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode)
				{
					s_materialCache.Clear();
				}
			};
		}
	}
#endif
}