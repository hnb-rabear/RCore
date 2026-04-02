using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

namespace RCore.Editor.Tool
{
	public class UIFormatTextsTool : RCoreHubTool
	{
		public override string Name => "Format Texts";
		public override string Category => "UI Tools";
		public override string Description => "Convert text case (UPPERCASE, lowercase, Capitalize Each Word...) for selected Text/TMP components.";
		public override bool IsQuickAction => true;

		private enum FormatType { UpperCase, LowerCase, CapitalizeEachWord, SentenceCase }
		private FormatType m_FormatType;
		private int m_TextCount;

		public override void DrawCard()
		{
			if (m_TextCount > 0)
				EditorGUILayout.LabelField("Text Count: ", m_TextCount.ToString());

			m_FormatType = EditorHelper.DropdownListEnum(m_FormatType, "Format Type");

			m_TextCount = 0;
			var allTexts = new List<Text>();
			var allTextPros = new List<TextMeshProUGUI>();

			if (Selection.gameObjects != null)
			{
				foreach (var g in Selection.gameObjects)
				{
					allTexts.AddRange(g.GetComponentsInChildren<Text>(true));
					allTextPros.AddRange(g.GetComponentsInChildren<TextMeshProUGUI>(true));
				}
			}
			
			m_TextCount = allTexts.Count + allTextPros.Count;

			if (EditorHelper.ButtonColor("Format Selected Texts", Color.cyan))
			{
				foreach (var t in allTexts)
				{
					switch (m_FormatType)
					{
						case FormatType.UpperCase: t.text = t.text.ToUpper(); break;
						case FormatType.SentenceCase: t.text = t.text.ToSentenceCase(); break;
						case FormatType.LowerCase: t.text = t.text.ToLower(); break;
						case FormatType.CapitalizeEachWord: t.text = t.text.ToCapitalizeEachWord(); break;
					}
					EditorUtility.SetDirty(t);
				}

				foreach (var t in allTextPros)
				{
					switch (m_FormatType)
					{
						case FormatType.UpperCase: t.text = t.text.ToUpper(); break;
						case FormatType.SentenceCase: t.text = t.text.ToSentenceCase(); break;
						case FormatType.LowerCase: t.text = t.text.ToLower(); break;
						case FormatType.CapitalizeEachWord: t.text = t.text.ToCapitalizeEachWord(); break;
					}
					EditorUtility.SetDirty(t);
				}
			}
		}
	}

	public class UIToggleRaycastAllTool : RCoreHubTool
	{
		public override string Name => "Toggle Raycast All";
		public override string Category => "UI Tools";
		public override string Description => "Scan selected GameObjects in the hierarchy to bulk enable/disable Raycast Target.";
		public override bool IsQuickAction => false;

		private int m_RaycastOnCount;
		private int m_RaycastOffCount;
		private List<Graphic> m_Graphics;

		public override void DrawFocusMode()
		{
			if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
			{
				EditorGUILayout.HelpBox("Select at least one Graphic GameObject in Hierarchy", MessageType.Warning);
				return;
			}

			if (EditorHelper.Button("Scan Selection"))
			{
				m_Graphics = new List<Graphic>();
				foreach (var g in Selection.gameObjects)
				{
					var graphics = g.GetComponentsInChildren<Graphic>(true);
					foreach (var graphic in graphics)
					{
						if (!m_Graphics.Contains(graphic))
							m_Graphics.Add(graphic);
					}
				}
			}

			if (m_Graphics == null || m_Graphics.Count == 0) return;

			m_RaycastOnCount = 0;
			m_RaycastOffCount = 0;

			int rootDeep = 0;
			for (int i = 0; i < m_Graphics.Count; i++)
			{
				var graphic = m_Graphics[i];
				GUILayout.BeginHorizontal();
				if (graphic.raycastTarget) m_RaycastOnCount++;
				else m_RaycastOffCount++;

				int deep = graphic.transform.HierarchyDeep();
				if (i == 0) rootDeep = deep;

				string deepStr = "";
				for (int d = rootDeep; d < deep; d++) deepStr += "__";

				EditorHelper.LabelField($"{i + 1}", 30);
				if (GUILayout.Button($"{deepStr}" + graphic.name, new GUIStyle("button") { fixedWidth = 250, alignment = TextAnchor.MiddleLeft }))
				{
					Selection.activeObject = graphic.gameObject;
				}

				if (EditorHelper.ButtonColor($"Raycast " + (graphic.raycastTarget ? "On" : "Off"), (graphic.raycastTarget ? Color.cyan : ColorHelper.DarkCyan), 100))
				{
					graphic.raycastTarget = !graphic.raycastTarget;
					EditorUtility.SetDirty(graphic);
				}

				GUILayout.EndHorizontal();
			}

			EditorGUILayout.Space();
			GUILayout.BeginHorizontal();
			if (m_RaycastOffCount > 0 && EditorHelper.ButtonColor("Raycast On All", Color.cyan))
			{
				foreach (var graphic in m_Graphics) { graphic.raycastTarget = true; EditorUtility.SetDirty(graphic); }
			}
			if (m_RaycastOnCount > 0 && EditorHelper.ButtonColor("Raycast Off All", ColorHelper.DarkCyan))
			{
				foreach (var graphic in m_Graphics) { graphic.raycastTarget = false; EditorUtility.SetDirty(graphic); }
			}
			GUILayout.EndHorizontal();
		}
	}

	public class UIChangeTMPTextsFontTool : RCoreHubTool
	{
		public override string Name => "Change TMP Texts Font";
		public override string Category => "UI Tools";
		public override string Description => "Batch-change the Font Asset for TextMeshPro UGUI components.";
		public override bool IsQuickAction => true;

		private TMP_FontAsset m_TMPFont;

		public override void DrawCard()
		{
			var objs = Selection.gameObjects;
			if (objs == null || objs.Length == 0)
			{
				GUILayout.Label("Select GameObjects first.");
				return;
			}

			m_TMPFont = (TMP_FontAsset)EditorHelper.ObjectField<TMP_FontAsset>(m_TMPFont, "TMP Font", 70);
			if (m_TMPFont != null && EditorHelper.ButtonColor("Apply Font", Color.cyan))
			{
				var m_TMPTexts = EditorHelper.FindComponents<TextMeshProUGUI>(objs, null);
				if (m_TMPTexts != null)
				{
					foreach (var texts in m_TMPTexts)
					{
						foreach (var text in texts.Value)
						{
							text.font = m_TMPFont;
							EditorUtility.SetDirty(text);
						}
					}
				}
			}
		}
	}

	public class UIConvertSpriteRendererTool : RCoreHubTool
	{
		public override string Name => "Convert Sprite to Image";
		public override string Category => "UI Tools";
		public override string Description => "Remove SpriteRenderer and attach RectTransform and Image component to selected GameObjects.";
		public override bool IsQuickAction => true;

		public override void DrawCard()
		{
			if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
			{
				GUILayout.Label("Select Non-UI GameObjects");
				return;
			}

			if (EditorHelper.ButtonColor("Convert Currently Selected", Color.cyan))
			{
				foreach (var g in Selection.gameObjects)
				{
					var allChildren = new List<Transform>();
					GetAllChildren(g.transform, allChildren);
					for (int i = allChildren.Count - 1; i >= 0; i--)
					{
						var child = allChildren[i];
						if (child.TryGetComponent(out SpriteRenderer spr))
						{
							var img = spr.gameObject.GetOrAddComponent<Image>();
							if (spr.sprite != null)
							{
								img.sprite = spr.sprite;
								img.color = spr.color;
								img.rectTransform.sizeDelta = spr.sprite.NativeSize() / 100f;
							}
							Object.DestroyImmediate(spr, true);
						}
						else if (!child.TryGetComponent(out RectTransform _))
							child.gameObject.AddComponent<RectTransform>();

						if (child != null && child.TryGetComponent(out UnityEngine.Rendering.SortingGroup sg))
							Object.DestroyImmediate(sg, true);
					}
				}
			}
		}

		private void GetAllChildren(Transform parent, List<Transform> childrenList)
		{
			foreach (Transform child in parent)
			{
				childrenList.Add(child);
				GetAllChildren(child, childrenList);
			}
		}
	}
}
