#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RCore.Editor
{
	/// <summary>
	/// Provides a collection of static utility methods for finding, replacing, and manipulating
	/// components on GameObjects within the Unity Editor. These methods are intended for editor scripting purposes.
	/// </summary>
	public static class EditorComponentUtil
	{
		/// <summary>
		/// Finds all components of a specified type in all loaded scenes, including inactive GameObjects.
		/// It filters out components that are part of prefab assets in the Project window.
		/// </summary>
		/// <typeparam name="T">The type of Component to find.</typeparam>
		/// <returns>A List of all found components of type T in the current scene(s).</returns>
		public static List<T> FindAllInScene<T>() where T : Component
		{
			// FindObjectsOfTypeAll finds all objects, including assets, so we filter them out.
			return Resources.FindObjectsOfTypeAll<T>()
				.Where(c => c.gameObject.hideFlags == HideFlags.None && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(c.gameObject)))
				.ToList();
		}

		/// <summary>
		/// Replaces a list of selected GameObjects in the scene with new instances of a provided prefab.
		/// The new instances inherit the transform properties (parent, position, rotation, scale, sibling index) of the objects they replace.
		/// This operation is Undo-able.
		/// </summary>
		/// <param name="selections">The list of GameObjects to be replaced.</param>
		/// <param name="prefab">The prefab to instantiate as the replacement.</param>
		public static void ReplaceWithPrefab(List<GameObject> selections, GameObject prefab)
		{
			if (prefab == null) return;

			for (var i = selections.Count - 1; i >= 0; --i)
			{
				var selected = selections[i];
				var newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, selected.transform.parent);

				// Register the creation for Undo functionality.
				Undo.RegisterCreatedObjectUndo(newObject, $"Replace {selected.name} With Prefab");
				
				// Copy transform properties.
				newObject.transform.SetSiblingIndex(selected.transform.GetSiblingIndex());
				newObject.transform.localPosition = selected.transform.localPosition;
				newObject.transform.localRotation = selected.transform.localRotation;
				newObject.transform.localScale = selected.transform.localScale;

				// Destroy the original object and register it for Undo.
				Undo.DestroyObjectImmediate(selected);
				selections[i] = newObject;
			}
		}

		/// <summary>
		/// Replaces all legacy UI Text components with TextMeshProUGUI components on the specified GameObjects and their children.
		/// It attempts to preserve basic properties like text content, color, and font size. This is a simplified version of `ReplaceTextsByTextTMP`.
		/// </summary>
		/// <param name="rootObjects">The root GameObjects from which to start the search for Text components.</param>
		public static void ReplaceTextWithTextMeshPro(params GameObject[] rootObjects)
		{
			foreach (var go in rootObjects)
			{
				var texts = go.GetComponentsInChildren<Text>(true);
				foreach (var text in texts)
				{
					var textGo = text.gameObject;
					// Capture properties from the old Text component.
					string content = text.text;
					Color color = text.color;
					int fontSize = text.fontSize;
					// ... capture other properties as needed

					// Replace the component.
					Undo.DestroyObjectImmediate(text);
					var textTMP = Undo.AddComponent<TextMeshProUGUI>(textGo);

					// Apply the captured properties to the new TextMeshProUGUI component.
					textTMP.text = content;
					textTMP.color = color;
					textTMP.fontSize = fontSize;
					// ... apply other properties

					Debug.Log($"Replaced Text with TextMeshProUGUI on {textGo.name}", textGo);
				}
			}
		}
		
		/// <summary>
		/// Finds all components of a specific type on an array of GameObjects and their children, with an optional validation condition.
		/// </summary>
		/// <typeparam name="T">The type of component to find.</typeparam>
		/// <param name="objs">The array of root GameObjects to search within.</param>
		/// <param name="pValidCondition">An optional delegate that can filter the results. Only components for which this returns true will be included.</param>
		/// <returns>A dictionary where the key is a root GameObject and the value is a list of valid components found within it.</returns>
		public static Dictionary<GameObject, List<T>> FindComponents<T>(GameObject[] objs, ConditionalDelegate<T> pValidCondition) where T : Component
		{
			var allComponents = new Dictionary<GameObject, List<T>>();
			for (int i = 0; i < objs.Length; i++)
			{
				var components = objs[i].gameObject.GetComponentsInChildren<T>(true);
				if (components.Length > 0)
				{
					allComponents.Add(objs[i], new List<T>());
					foreach (var component in components)
					{
						// Apply the filter condition if provided.
						if (pValidCondition != null && !pValidCondition(component))
							continue;

						if (!allComponents[objs[i]].Contains(component))
							allComponents[objs[i]].Add(component);
					}
				}
			}

			return allComponents;
		}

		/// <summary>
		/// Replaces all legacy UI Text components with TextMeshProUGUI components on the given GameObjects and their children.
		/// This is a more comprehensive version that preserves more properties like alignment, overflow, and best fit.
		/// </summary>
		/// <param name="gos">The root GameObjects from which to start the replacement process.</param>
		public static void ReplaceTextsByTextTMP(params GameObject[] gos)
		{
			var textsDict = FindComponents<Text>(gos, null);
			if (textsDict != null)
				foreach (var item in textsDict)
				{
					for (int i = item.Value.Count - 1; i >= 0; i--)
					{
						var go = item.Value[i].gameObject;
						
						// --- Capture properties from the original Text component ---
						var content = item.Value[i].text;
						var fontSize = item.Value[i].fontSize;
						var alignment = item.Value[i].alignment;
						var bestFit = item.Value[i].resizeTextForBestFit;
						var horizontalOverflow = item.Value[i].horizontalOverflow;
						var verticalOverflow = item.Value[i].verticalOverflow;
						var raycastTarget = item.Value[i].raycastTarget;
						var color = item.Value[i].color;
						
						// --- Remove old components ---
						if (item.Value[i].gameObject.TryGetComponent(out Outline outline))
							Object.DestroyImmediate(outline);
						if (item.Value[i].gameObject.TryGetComponent(out Shadow shadow))
							Object.DestroyImmediate(shadow);
						Object.DestroyImmediate(item.Value[i]);
						
						// --- Add and configure the new TextMeshProUGUI component ---
						var textTMP = go.AddComponent<TextMeshProUGUI>();
						textTMP.text = content;
						textTMP.fontSize = fontSize;
						textTMP.enableAutoSizing = bestFit;
						textTMP.color = color;
						textTMP.raycastTarget = raycastTarget;
						
						// --- Convert properties ---
						switch (alignment)
						{
							case TextAnchor.MiddleLeft:
								textTMP.alignment = TextAlignmentOptions.Left;
								break;
							case TextAnchor.MiddleCenter:
								textTMP.alignment = TextAlignmentOptions.Center;
								break;
							case TextAnchor.MiddleRight:
								textTMP.alignment = TextAlignmentOptions.Right;
								break;

							case TextAnchor.LowerLeft:
								textTMP.alignment = TextAlignmentOptions.BottomLeft;
								break;
							case TextAnchor.LowerCenter:
								textTMP.alignment = TextAlignmentOptions.Bottom;
								break;
							case TextAnchor.LowerRight:
								textTMP.alignment = TextAlignmentOptions.BottomRight;
								break;

							case TextAnchor.UpperLeft:
								textTMP.alignment = TextAlignmentOptions.TopLeft;
								break;
							case TextAnchor.UpperCenter:
								textTMP.alignment = TextAlignmentOptions.Top;
								break;
							case TextAnchor.UpperRight:
								textTMP.alignment = TextAlignmentOptions.TopRight;
								break;
						}
						textTMP.enableWordWrapping = horizontalOverflow == HorizontalWrapMode.Wrap;
						if (verticalOverflow == VerticalWrapMode.Truncate)
							textTMP.overflowMode = TextOverflowModes.Truncate;
							
						UnityEngine.Debug.Log($"Replace Text in GameObject {go.name}");
						EditorUtility.SetDirty(go);
					}
				}
		}
		
		/// <summary>
		/// Finds all components of a specific type in the current scene(s). An alias for `FindAllInScene`.
		/// </summary>
		/// <typeparam name="T">The type of Component to find.</typeparam>
		/// <returns>A list of all found components.</returns>
		public static List<T> FindAll<T>() where T : Component
		{
			var comps = Resources.FindObjectsOfTypeAll(typeof(T)) as T[];

			var list = new List<T>();

			if (comps != null)
				foreach (var comp in comps)
				{
					// Ensure the component is part of a scene object (not a prefab asset) and is not hidden.
					if (comp.gameObject.hideFlags == 0)
					{
						string path = AssetDatabase.GetAssetPath(comp.gameObject);
						if (string.IsNullOrEmpty(path)) list.Add(comp);
					}
				}

			return list;
		}
	}
}
#endif