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
	/// Provides utility methods for finding, replacing, and manipulating components on GameObjects in the scene.
	/// </summary>
	public static class EditorComponentUtil
	{
		/// <summary>
		/// Finds all components of a specified type in the loaded scenes, including inactive ones.
		/// </summary>
		public static List<T> FindAllInScene<T>() where T : Component
		{
			return Resources.FindObjectsOfTypeAll<T>()
				.Where(c => c.gameObject.hideFlags == HideFlags.None && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(c.gameObject)))
				.ToList();
		}

		/// <summary>
		/// Replaces selected GameObjects in the scene with instances of a provided prefab.
		/// </summary>
		public static void ReplaceWithPrefab(List<GameObject> selections, GameObject prefab)
		{
			if (prefab == null) return;

			for (var i = selections.Count - 1; i >= 0; --i)
			{
				var selected = selections[i];
				var newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, selected.transform.parent);

				Undo.RegisterCreatedObjectUndo(newObject, $"Replace {selected.name} With Prefab");
				newObject.transform.SetSiblingIndex(selected.transform.GetSiblingIndex());
				newObject.transform.localPosition = selected.transform.localPosition;
				newObject.transform.localRotation = selected.transform.localRotation;
				newObject.transform.localScale = selected.transform.localScale;

				Undo.DestroyObjectImmediate(selected);
				selections[i] = newObject;
			}
		}

		/// <summary>
		/// Replaces all legacy UI Text components with TextMeshProUGUI components on the given GameObjects.
		/// </summary>
		public static void ReplaceTextWithTextMeshPro(params GameObject[] rootObjects)
		{
			foreach (var go in rootObjects)
			{
				var texts = go.GetComponentsInChildren<Text>(true);
				foreach (var text in texts)
				{
					var textGo = text.gameObject;
					string content = text.text;
					Color color = text.color;
					int fontSize = text.fontSize;
					// ... capture other properties as needed

					Undo.DestroyObjectImmediate(text);
					var textTMP = Undo.AddComponent<TextMeshProUGUI>(textGo);

					textTMP.text = content;
					textTMP.color = color;
					textTMP.fontSize = fontSize;
					// ... apply other properties

					Debug.Log($"Replaced Text with TextMeshProUGUI on {textGo.name}", textGo);
				}
			}
		}

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
						if (pValidCondition != null && !pValidCondition(component))
							continue;

						if (!allComponents[objs[i]].Contains(component))
							allComponents[objs[i]].Add(component);
					}
				}
			}

			return allComponents;
		}

		public static void ReplaceTextsByTextTMP(params GameObject[] gos)
		{
			var textsDict = FindComponents<Text>(gos, null);
			if (textsDict != null)
				foreach (var item in textsDict)
				{
					for (int i = item.Value.Count - 1; i >= 0; i--)
					{
						var go = item.Value[i].gameObject;
						var content = item.Value[i].text;
						var fontSize = item.Value[i].fontSize;
						var alignment = item.Value[i].alignment;
						var bestFit = item.Value[i].resizeTextForBestFit;
						var horizontalOverflow = item.Value[i].horizontalOverflow;
						var verticalOverflow = item.Value[i].verticalOverflow;
						var raycastTarget = item.Value[i].raycastTarget;
						var color = item.Value[i].color;
						if (item.Value[i].gameObject.TryGetComponent(out Outline outline))
							Object.DestroyImmediate(outline);
						if (item.Value[i].gameObject.TryGetComponent(out Shadow shadow))
							Object.DestroyImmediate(shadow);
						Object.DestroyImmediate(item.Value[i]);
						var textTMP = go.AddComponent<TextMeshProUGUI>();
						textTMP.text = content;
						textTMP.fontSize = fontSize;
						textTMP.enableAutoSizing = bestFit;
						textTMP.color = color;
						textTMP.raycastTarget = raycastTarget;
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
		
		public static List<T> FindAll<T>() where T : Component
		{
			var comps = Resources.FindObjectsOfTypeAll(typeof(T)) as T[];

			var list = new List<T>();

			if (comps != null)
				foreach (var comp in comps)
				{
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