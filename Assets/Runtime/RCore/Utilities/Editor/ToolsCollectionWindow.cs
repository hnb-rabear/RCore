﻿using UnityEngine;
using UnityEditor;
using RCore.Common;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace RCore.Editor
{
	public class ToolsCollectionWindow : EditorWindow
	{
		private Vector2 mScrollPosition;

		private void OnGUI()
		{
			mScrollPosition = GUILayout.BeginScrollView(mScrollPosition, false, false);

			EditorHelper.SeparatorBox();
			DrawGameObjectUtilities();

			EditorHelper.SeparatorBox();
			DrawRendererUtilities();

			EditorHelper.SeparatorBox();
			DrawUIUtilities();

			EditorHelper.SeparatorBox();
			DrawMathUtilities();

			EditorHelper.SeparatorBox();
			DrawGenerators();

			GUILayout.EndScrollView();
		}

#region GameObject Utilities

		private List<GameObject> m_ReplaceableGameObjects = new List<GameObject>();
		private List<GameObject> m_Prefabs = new List<GameObject>();

		private void DrawGameObjectUtilities()
		{
			EditorHelper.HeaderFoldout("GameObject Utilities", "", () =>
			{
				ReplaceGameObjectsInScene();
				FindGameObjectsMissingScript();
			});
		}

		private void ReplaceGameObjectsInScene()
		{
			if (EditorHelper.HeaderFoldout("Replace GameObjects in scene"))
				EditorHelper.BoxVertical(() =>
				{
					if (m_ReplaceableGameObjects == null || m_ReplaceableGameObjects.Count == 0)
						EditorGUILayout.HelpBox("Select at least one Object to see how it work", MessageType.Info);

					EditorHelper.ListObjects("Replaceable Objects", ref m_ReplaceableGameObjects, null, false);
					EditorHelper.ListObjects("Prefabs", ref m_Prefabs, null, false);

					if (GUILayout.Button("Replace"))
						EditorHelper.ReplaceGameObjectsInScene(ref m_ReplaceableGameObjects, m_Prefabs);
				}, Color.white, true);
		}

		private bool m_AlsoChildren;

		private void FindGameObjectsMissingScript()
		{
			if (EditorHelper.HeaderFoldout("Find GameObjects missing script"))
			{
				m_AlsoChildren = EditorHelper.Toggle(m_AlsoChildren, "Also Children of children");
				if (!SelectedObject())
					return;

				if (EditorHelper.Button("Scan"))
				{
					var invalidObjs = new List<GameObject>();
					var objs = Selection.gameObjects;
					for (int i = 0; i < objs.Length; i++)
					{
						var components = objs[i].GetComponents<Component>();
						for (int j = components.Length - 1; j >= 0; j--)
						{
							if (components[j] == null)
							{
								Debug.Log(objs[i].gameObject.name + " is missing component! Let clear it!");
								invalidObjs.Add(objs[i]);
								GameObjectUtility.RemoveMonoBehavioursWithMissingScript(objs[i].gameObject);
							}
						}

						if (m_AlsoChildren)
						{
							var children = objs[i].GetAllChildren();
							for (int k = children.Count - 1; k >= 0; k--)
							{
								var childComponents = children[k].GetComponents<Component>();
								for (int j = childComponents.Length - 1; j >= 0; j--)
								{
									if (childComponents[j] == null)
									{
										Debug.Log(children[k].gameObject.name + " is missing component! Let clear it!");
										invalidObjs.Add(objs[i]);
										GameObjectUtility.RemoveMonoBehavioursWithMissingScript(children[k].gameObject);
									}
								}
							}
						}
					}

					Selection.objects = invalidObjs.ToArray();
				}
			}
		}

#endregion

		//===================================================================================================

#region Renderer Utilities

		private int m_MeshCount = 1;
		private int m_VertexCount;
		private int m_SubMeshCount;
		private int m_TriangleCount;

		private void DrawRendererUtilities()
		{
			EditorHelper.HeaderFoldout("Renderer Utilities", "", () =>
			{
				DisplayMeshInfos();
				CombineMeshes();
				AlignCenterMeshRendererObj();
			});
		}

		private void DisplayMeshInfos()
		{
			if (EditorHelper.HeaderFoldout("Mesh Info"))
				EditorHelper.BoxVertical(() =>
				{
					if (m_MeshCount == 0)
						EditorGUILayout.HelpBox("Select at least one Mesh Object to see how it work", MessageType.Info);

					if (m_MeshCount > 1)
					{
						EditorGUILayout.LabelField("Total Vertices: ", m_VertexCount.ToString());
						EditorGUILayout.LabelField("Total Triangles: ", m_TriangleCount.ToString());
						EditorGUILayout.LabelField("Total SubMeshes: ", m_SubMeshCount.ToString());
						EditorGUILayout.LabelField("Avr Vertices: ", (m_VertexCount / m_MeshCount).ToString());
						EditorGUILayout.LabelField("Avr Triangles: ", (m_TriangleCount / m_MeshCount).ToString());
					}

					m_VertexCount = 0;
					m_TriangleCount = 0;
					m_SubMeshCount = 0;
					m_MeshCount = 0;

					foreach (var g in Selection.gameObjects)
					{
						var filter = g.GetComponent<MeshFilter>();

						if (filter != null && filter.sharedMesh != null)
						{
							var sharedMesh = filter.sharedMesh;
							var a = sharedMesh.vertexCount;
							var b = sharedMesh.triangles.Length / 3;
							var c = sharedMesh.subMeshCount;
							m_VertexCount += a;
							m_TriangleCount += b;
							m_SubMeshCount += c;
							m_MeshCount += 1;

							EditorGUILayout.Space();
							EditorGUILayout.LabelField(g.name);
							EditorGUILayout.LabelField("Vertices: ", a.ToString());
							EditorGUILayout.LabelField("Triangles: ", b.ToString());
							EditorGUILayout.LabelField("SubMeshes: ", c.ToString());
							return;
						}

						var objs = g.FindComponentsInChildren<SkinnedMeshRenderer>();
						if (objs != null)
						{
							int a = 0, b = 0, c = 0;
							foreach (var obj in objs)
							{
								if (obj.sharedMesh == null)
									continue;

								var sharedMesh = obj.sharedMesh;
								a += sharedMesh.vertexCount;
								b += sharedMesh.triangles.Length / 3;
								c += sharedMesh.subMeshCount;
							}

							m_VertexCount += a;
							m_TriangleCount += b;
							m_SubMeshCount += c;
							m_MeshCount += 1;
							EditorGUILayout.Space();
							EditorGUILayout.LabelField(g.name);
							EditorGUILayout.LabelField("Vertices: ", a.ToString());
							EditorGUILayout.LabelField("Triangles: ", b.ToString());
							EditorGUILayout.LabelField("SubMeshes: ", c.ToString());
						}
					}
				}, Color.white, true);
		}

		private static void CombineMeshes()
		{
			if (EditorHelper.HeaderFoldout("Combine Meshes"))
			{
				if (!SelectedObject())
					return;

				bool available = false;
				foreach (var g in Selection.gameObjects)
				{
					var filter = g.FindComponentInChildren<MeshFilter>();
					if (filter != null)
					{
						available = true;
						break;
					}
				}

				if (!available)
				{
					EditorGUILayout.HelpBox("Select at least one Mesh Object to see how it work", MessageType.Info);
					return;
				}

				if (EditorHelper.Button("Combine Meshes"))
				{
					var combinedMeshes = new GameObject();
					combinedMeshes.name = "Meshes_Combined";
					combinedMeshes.AddComponent<MeshRenderer>();
					combinedMeshes.AddComponent<MeshFilter>();

					var meshFilters = new List<MeshFilter>();
					foreach (var g in Selection.gameObjects)
					{
						var filters = g.FindComponentsInChildren<MeshFilter>();
						meshFilters.AddRange(filters);
					}

					var combine = new CombineInstance[meshFilters.Count];
					int i = 0;
					while (i < meshFilters.Count)
					{
						combine[i].mesh = meshFilters[i].sharedMesh;
						combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
						meshFilters[i].gameObject.SetActive(false);
						i++;
					}

					combinedMeshes.GetComponent<MeshFilter>().sharedMesh = new Mesh();
					combinedMeshes.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combine);
				}
			}
		}

		private static void AlignCenterMeshRendererObj()
		{
			if (EditorHelper.HeaderFoldout("Align Center Mesh Renderer obj"))
			{
				if (!SelectedObject())
					return;

				foreach (var g in Selection.gameObjects)
				{
					if (g.IsPrefab())
						continue;

					var renderer = g.transform.GetComponent<Renderer>();
					var center = renderer.bounds.extents;
					g.transform.localPosition = new Vector3(-center.x, g.transform.localPosition.y, -center.z);
				}
			}
		}

#endregion

		//===================================================================================================

#region UI Utilities

		public enum FormatType
		{
			UpperCase,
			Lowercase,
			CapitalizeEachWord,
			SentenceCase
		}

		private FormatType m_FormatType;
		private int m_TextCount;

		private void DrawUIUtilities()
		{
			EditorHelper.HeaderFoldout("UI Utilities", "", () =>
			{
				FormatTexts();
				SketchImages();
				ToggleRaycastAll();
				ChangeButtonsTransitionColor();
				ChangeTextsFont();
				ChangeTMPTextsFont();
				ReplaceTextByTextTMP();
				PerfectRatioImages();
				ConvertSpriteRendererToImage();
				SearchAndReplaceSprite();
				SearchAndReplaceFont();
				SearchAndReplaceTMPFont();
			});
		}

		private void FormatTexts()
		{
			if (EditorHelper.HeaderFoldout("Format Texts"))
			{
				GUILayout.BeginVertical("box");
				{
					if (m_TextCount == 0)
						EditorGUILayout.HelpBox("Select at least one Text Object to see how it work", MessageType.Info);
					else
						EditorGUILayout.LabelField("Text Count: ", m_TextCount.ToString());

					m_FormatType = EditorHelper.DropdownListEnum(m_FormatType, "Format Type");

					m_TextCount = 0;
					var allTexts = new List<Text>();
					var allTextPros = new List<TextMeshProUGUI>();
					foreach (var g in Selection.gameObjects)
					{
						var texts = g.FindComponentsInChildren<Text>();
						var textPros = g.FindComponentsInChildren<TextMeshProUGUI>();
						if (texts.Count > 0)
						{
							m_TextCount += texts.Count;
							allTexts.AddRange(texts);
							foreach (var t in allTexts)
								EditorGUILayout.LabelField("Text: ", t.name);
						}

						if (textPros.Count > 0)
						{
							m_TextCount += textPros.Count;
							allTextPros.AddRange(textPros);
							foreach (var t in allTextPros)
								EditorGUILayout.LabelField("Text Mesh Pro: ", t.name);
						}
					}

					if (EditorHelper.Button("Format"))
					{
						foreach (var t in allTexts)
						{
							switch (m_FormatType)
							{
								case FormatType.UpperCase:
									t.text = t.text.ToUpper();
									break;
								case FormatType.SentenceCase:
									t.text = t.text.ToSentenceCase();
									break;
								case FormatType.Lowercase:
									t.text = t.text.ToLower();
									break;
								case FormatType.CapitalizeEachWord:
									t.text = t.text.ToCapitalizeEachWord();
									break;
							}
						}

						foreach (var t in allTextPros)
						{
							switch (m_FormatType)
							{
								case FormatType.UpperCase:
									t.text = t.text.ToUpper();
									break;
								case FormatType.SentenceCase:
									t.text = t.text.ToSentenceCase();
									break;
								case FormatType.Lowercase:
									t.text = t.text.ToLower();
									break;
								case FormatType.CapitalizeEachWord:
									t.text = t.text.ToCapitalizeEachWord();
									break;
							}
						}
					}
				}

				GUILayout.EndVertical();
			}
		}

		private float m_ImgWidth;
		private float m_ImgHeight;
		private int m_CountImgs;

		private void SketchImages()
		{
			if (EditorHelper.HeaderFoldout("Sketch Images"))
			{
				GUILayout.BeginVertical("box");
				{
					if (m_CountImgs == 0)
						EditorGUILayout.HelpBox("Select at least one Image Object to see how it work", MessageType.Info);
					else
						EditorGUILayout.LabelField("Image Count: ", m_CountImgs.ToString());

					m_ImgWidth = EditorHelper.FloatField(m_ImgWidth, "Width");
					m_ImgHeight = EditorHelper.FloatField(m_ImgHeight, "Height");

					m_CountImgs = 0;
					var allImages = new List<Image>();
					foreach (var g in Selection.gameObjects)
					{
						var img = g.GetComponent<Image>();
						if (img != null)
						{
							allImages.Add(img);
							EditorGUILayout.LabelField("Image: ", img.ToString());
						}
					}

					var buttons = new List<IDraw>();
					buttons.Add(new EditorButton()
					{
						label = "Sketch By Height",
						onPressed = () =>
						{
							foreach (var img in allImages)
								img.SketchByHeight(m_ImgHeight);
						}
					});

					buttons.Add(new EditorButton()
					{
						label = "Sketch By Width",
						onPressed = () =>
						{
							foreach (var img in allImages)
								img.SketchByWidth(m_ImgWidth);
						}
					});

					buttons.Add(new EditorButton()
					{
						label = "Sketch",
						onPressed = () =>
						{
							foreach (var img in allImages)
								img.Sketch(new Vector2(m_ImgWidth, m_ImgHeight));
						}
					});

					EditorHelper.GridDraws(2, buttons);
				}

				GUILayout.EndVertical();
			}
		}

		private int m_RaycastOnCount;
		private int m_RaycastOffCount;
		private List<Graphic> m_Graphics;

		private void ToggleRaycastAll()
		{
			if (EditorHelper.HeaderFoldout("Toggle Raycast All"))
			{
				GUILayout.BeginVertical("box");
				{
					if (!SelectedObject())
					{
						GUILayout.EndVertical();
						return;
					}

					if (EditorHelper.Button("Scan"))
					{
						m_Graphics = new List<Graphic>();
						foreach (var g in Selection.gameObjects)
						{
							var graphics = g.FindComponentsInChildren<Graphic>();
							foreach (var graphic in graphics)
							{
								if (!m_Graphics.Contains(graphic))
									m_Graphics.Add(graphic);
							}
						}
					}

					if (m_Graphics == null || m_Graphics.Count == 0)
					{
						GUILayout.EndVertical();
						return;
					}

					m_RaycastOnCount = 0;
					m_RaycastOffCount = 0;

					int rootDeep = 0;
					for (int i = 0; i < m_Graphics.Count; i++)
					{
						var graphic = m_Graphics[i];
						GUILayout.BeginHorizontal();
						if (graphic.raycastTarget)
							m_RaycastOnCount++;
						else
							m_RaycastOffCount++;

						int deep = graphic.transform.HierarchyDeep();
						if (i == 0)
							rootDeep = deep;

						string deepStr = "";
						for (int d = rootDeep; d < deep; d++)
							deepStr += "__";

						EditorHelper.LabelField($"{i + 1}", 30);
						if (EditorHelper.Button($"{deepStr}" + graphic.name, new GUIStyle("button")
							{
								fixedWidth = 250,
								alignment = TextAnchor.MiddleLeft
							}))
						{
							Selection.activeObject = graphic.gameObject;
						}

						if (EditorHelper.ButtonColor($"Raycast " + (graphic.raycastTarget ? "On" : "Off"), (graphic.raycastTarget ? Color.cyan : ColorHelper.DarkCyan), 100))
						{
							if (!graphic.raycastTarget)
							{
								graphic.raycastTarget = true;
								m_RaycastOffCount--;
							}
							else
							{
								graphic.raycastTarget = false;
								m_RaycastOnCount--;
							}
						}

						GUILayout.EndHorizontal();
					}

					EditorGUILayout.LabelField("Raycast On Count: ", m_RaycastOnCount.ToString());
					EditorGUILayout.LabelField("Raycast On Count: ", m_RaycastOffCount.ToString());

					if (m_RaycastOffCount > 0)
						if (EditorHelper.ButtonColor("Raycast On All", Color.cyan))
						{
							foreach (var graphic in m_Graphics)
								graphic.raycastTarget = true;
						}

					if (m_RaycastOnCount > 0)
						if (EditorHelper.ButtonColor("Raycast Off All", ColorHelper.DarkCyan))
						{
							foreach (var graphic in m_Graphics)
								graphic.raycastTarget = false;
						}
				}

				GUILayout.EndVertical();
			}
		}

		private Dictionary<GameObject, List<Button>> m_Buttons;
		private ColorBlock m_ButtonColors;
		private readonly bool[] m_ColorBlocksForChange = { true, true, true, true };
		private RuntimeAnimatorController m_ButtonAnimation;

		private void ChangeButtonsTransitionColor()
		{
			if (EditorHelper.HeaderFoldout("Change Buttons Transition Color"))
			{
				GUILayout.BeginVertical("box");

				if (!SelectedObject())
				{
					GUILayout.EndVertical();
					return;
				}

				if (EditorHelper.Button("Scan"))
				{
					m_Buttons = FindComponents<Button>((button) => button.image != null && button.image.color != Color.clear);
				}

				if (m_Buttons != null && m_Buttons.Count > 0)
				{
					EditorHelper.BoxHorizontal(() =>
					{
						m_ColorBlocksForChange[0] = EditorHelper.Toggle(m_ColorBlocksForChange[0], "Normal Color", 150, 23);
						if (m_ColorBlocksForChange[0])
							m_ButtonColors.normalColor = EditorHelper.ColorField(m_ButtonColors.normalColor, "", 0, 100);
					});

					EditorHelper.BoxHorizontal(() =>
					{
						m_ColorBlocksForChange[1] = EditorHelper.Toggle(m_ColorBlocksForChange[1], "Pressed Color", 150, 23);
						if (m_ColorBlocksForChange[1])
							m_ButtonColors.pressedColor = EditorHelper.ColorField(m_ButtonColors.pressedColor, "", 0, 100);
					});

					EditorHelper.BoxHorizontal(() =>
					{
						m_ColorBlocksForChange[2] = EditorHelper.Toggle(m_ColorBlocksForChange[2], "Highlighted Color", 150, 23);
						if (m_ColorBlocksForChange[2])
							m_ButtonColors.highlightedColor = EditorHelper.ColorField(m_ButtonColors.highlightedColor, "", 0, 100);
					});

					EditorHelper.BoxHorizontal(() =>
					{
						m_ColorBlocksForChange[3] = EditorHelper.Toggle(m_ColorBlocksForChange[3], "Disabled Color", 150, 23);
						if (m_ColorBlocksForChange[3])
							m_ButtonColors.disabledColor = EditorHelper.ColorField(m_ButtonColors.disabledColor, "", 0, 100);
					});

					if (EditorHelper.ButtonColor("Change Colors", Color.yellow))
					{
						foreach (var buttons in m_Buttons)
						{
							foreach (var button in buttons.Value)
							{
								var colors = button.colors;
								if (m_ColorBlocksForChange[0])
									colors.normalColor = m_ButtonColors.normalColor;

								if (m_ColorBlocksForChange[1])
									colors.pressedColor = m_ButtonColors.pressedColor;

								if (m_ColorBlocksForChange[2])
									colors.highlightedColor = m_ButtonColors.highlightedColor;

								if (m_ColorBlocksForChange[3])
									colors.disabledColor = m_ButtonColors.disabledColor;

								button.colors = colors;
								Common.Debug.Log($"{button.name} updated!", EditorGUIUtility.isProSkin ? Color.green : ColorHelper.DarkGreenX11);
							}

							EditorUtility.SetDirty(buttons.Key);
						}

						AssetDatabase.SaveAssets();
					}

					EditorHelper.Separator();
					foreach (var buttons in m_Buttons)
						EditorGUILayout.LabelField($"{buttons.Key.name} has {buttons.Value.Count} buttons.");
				}

				GUILayout.EndVertical();
			}

			if (EditorHelper.HeaderFoldout("Change Buttons Transition Animator"))
			{
				GUILayout.BeginVertical("box");

				if (!SelectedObject())
				{
					GUILayout.EndVertical();
					return;
				}

				if (EditorHelper.Button("Scan"))
				{
					m_Buttons = FindComponents<Button>((button) => button.image != null && button.image.sprite != null && button.image.color != Color.clear);
				}

				if (m_Buttons != null && m_Buttons.Count > 0)
				{
					m_ButtonAnimation = (RuntimeAnimatorController)EditorHelper.ObjectField<RuntimeAnimatorController>(m_ButtonAnimation, "Animation controller", 120);
					if (m_ButtonAnimation != null && EditorHelper.Button("Add Animation"))
					{
						foreach (var buttons in m_Buttons)
						{
							foreach (var button in buttons.Value)
							{
								var animator = button.GetComponent<Animator>();
								if (animator != null && animator.runtimeAnimatorController != null)
									continue;

								var animation = button.GetComponent<Animation>();
								if (animation != null)
									continue;

								if (animator == null)
									animator = button.gameObject.AddComponent<Animator>();

								button.transition = Selectable.Transition.Animation;
								animator.runtimeAnimatorController = m_ButtonAnimation;
								Common.Debug.Log($"{button.name} updated!", EditorGUIUtility.isProSkin ? Color.green : ColorHelper.DarkGreenX11);
							}

							EditorUtility.SetDirty(buttons.Key);
						}

						AssetDatabase.SaveAssets();
					}

					EditorHelper.Separator();
					foreach (var buttons in m_Buttons)
						EditorGUILayout.LabelField($"{buttons.Key.name} has {buttons.Value.Count} buttons.");
				}

				GUILayout.EndVertical();
			}
		}

		private Font m_Font;
		private Dictionary<GameObject, List<Text>> m_Texts;

		private void ChangeTextsFont()
		{
			if (EditorHelper.HeaderFoldout("Change Texts Font"))
			{
				GUILayout.BeginVertical("box");

				if (!SelectedObject())
				{
					GUILayout.EndVertical();
					return;
				}

				if (EditorHelper.Button("Scan Texts"))
					m_Texts = FindComponents<Text>(null);

				if (m_Texts != null && m_Texts.Count > 0)
				{
					m_Font = (Font)EditorHelper.ObjectField<Font>(m_Font, "Font");
					if (m_Font != null && EditorHelper.Button("Set Font"))
					{
						foreach (var texts in m_Texts)
						{
							foreach (var text in texts.Value)
							{
								if (text.font == m_Font)
									Common.Debug.Log($"{text.name} unchanged!", EditorGUIUtility.isProSkin ? Color.yellow : ColorHelper.DarkOrange);
								else
								{
									text.font = m_Font;
									Common.Debug.Log($"{text.name} updated!", EditorGUIUtility.isProSkin ? Color.green : ColorHelper.DarkGreenX11);
								}
							}

							EditorUtility.SetDirty(texts.Key);
						}

						AssetDatabase.SaveAssets();
					}

					EditorHelper.Separator();
					foreach (var texts in m_Texts)
						EditorGUILayout.LabelField($"{texts.Key.name} has {texts.Value.Count} texts.");
				}

				GUILayout.EndVertical();
			}
		}

		private TMP_FontAsset m_TMPFont;
		private Dictionary<GameObject, List<TextMeshProUGUI>> m_TMPTexts;

		private void ChangeTMPTextsFont()
		{
			if (EditorHelper.HeaderFoldout("Change TMP Font"))
			{
				GUILayout.BeginVertical("box");

				if (!SelectedObject())
				{
					GUILayout.EndVertical();
					return;
				}

				if (EditorHelper.Button("Scan Texts"))
					m_TMPTexts = FindComponents<TextMeshProUGUI>(null);

				if (m_TMPTexts != null && m_TMPTexts.Count > 0)
				{
					m_TMPFont = (TMP_FontAsset)EditorHelper.ObjectField<TMP_FontAsset>(m_TMPFont, "Font Asset");
					if (m_TMPFont != null && EditorHelper.Button("Set Font"))
					{
						foreach (var texts in m_TMPTexts)
						{
							foreach (var text in texts.Value)
							{
								if (text.font == m_TMPFont)
									Common.Debug.Log($"{text.name} unchanged!", EditorGUIUtility.isProSkin ? Color.yellow : ColorHelper.DarkOrange);
								else
								{
									text.font = m_TMPFont;
									Common.Debug.Log($"{text.name} updated!", EditorGUIUtility.isProSkin ? Color.green : ColorHelper.DarkGreenX11);
								}
							}

							EditorUtility.SetDirty(texts.Key);
						}

						AssetDatabase.SaveAssets();
					}

					EditorHelper.Separator();
					foreach (var texts in m_TMPTexts)
						EditorGUILayout.LabelField($"{texts.Key.name} has {texts.Value.Count} texts.");
				}

				GUILayout.EndVertical();
			}
		}

		private static void ReplaceTextByTextTMP()
		{
			if (EditorHelper.HeaderFoldout("Replace Text By TextMeshProUGUI"))
			{
				if (!SelectedObject())
					return;

				if (EditorHelper.Button("Replace Texts"))
				{
					var textsDict = FindComponents<Text>(null);
					if (textsDict != null)
						foreach (var item in textsDict)
						{
							for (int i = item.Value.Count - 1; i >= 0; i--)
							{
								var gameObj = item.Value[i].gameObject;
								var content = item.Value[i].text;
								var fontSize = item.Value[i].fontSize;
								var alignment = item.Value[i].alignment;
								var bestFit = item.Value[i].resizeTextForBestFit;
								var color = item.Value[i].color;
								DestroyImmediate(item.Value[i]);
								var textTMP = gameObj.AddComponent<TextMeshProUGUI>();
								textTMP.text = content;
								textTMP.fontSize = fontSize;
								textTMP.enableAutoSizing = bestFit;
								textTMP.color = color;
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

								Debug.Log($"Replace Text in GameObject {gameObj.name}");
							}
						}
				}
			}
		}

		private static void PerfectRatioImages()
		{
			if (EditorHelper.HeaderFoldout("Perfect Ratio Images"))
			{
				if (!SelectedObject())
					return;

				if (EditorHelper.Button("Set Perfect Width"))
				{
					RUtil.PerfectRatioImagesByWidth(Selection.gameObjects);
				}

				if (EditorHelper.Button("Set Perfect Height"))
				{
					RUtil.PerfectRatioImagesByHeight(Selection.gameObjects);
				}
			}
		}

		private static void ConvertSpriteRendererToImage()
		{
			if (EditorHelper.HeaderFoldout("Convert Transform To RectTransform"))
			{
				if (!SelectedObject())
					return;

				if (EditorHelper.Button("Convert"))
				{
					foreach (var g in Selection.gameObjects)
					{
						var children = g.FindComponentsInChildren<Component>();
						for (int i = children.Count - 1; i >= 0; i--)
						{
							var child = children[i];
							//if (!child.TryGetComponent(out RectTransform rt))
							//	child.AddComponent<RectTransform>();

							//if (child.TryGetComponent(out RectTransform rt2))
							{
								if (child.TryGetComponent(out SpriteRenderer spr))
								{
									var img = child.gameObject.GetOrAddComponent<Image>();
									img.sprite = spr.sprite;
									img.rectTransform.sizeDelta = spr.sprite.NativeSize() / 100f;
									DestroyImmediate(spr, true);
								}

								if (child != null && child.TryGetComponent(out SortingGroup sg))
									DestroyImmediate(sg, true);
							}
						}
					}
				}
			}
		}
		
		//
		public class SpriteReplace
		{
			public class Input
			{
				public readonly List<Sprite> targets = new List<Sprite>();
				public Sprite replace;
			}

			public readonly List<Input> inputs = new List<Input>();
		}

		private SpriteReplace m_SpriteReplace;

		private void SearchAndReplaceSprite()
		{
			var btn = new EditorButton()
			{
				color = Color.yellow,
				onPressed = () => m_SpriteReplace.inputs.Add(new SpriteReplace.Input()),
				label = "Add Targets And Replace",
			};

			if (EditorHelper.HeaderFoldout("Search And Replace Sprite", null, false, btn))
			{
				EditorGUILayout.BeginVertical("box");
				{
					m_SpriteReplace ??= new SpriteReplace();

					foreach (var target in m_SpriteReplace.inputs)
					{
						EditorGUILayout.BeginHorizontal();
						{
							EditorGUILayout.BeginVertical();
							{
								target.replace = (Sprite)EditorHelper.ObjectField<Sprite>(target.replace, "", 0, 60, showAsBox: true);
								if (EditorHelper.ButtonColor("Remove", Color.red))
								{
									m_SpriteReplace.inputs.Remove(target);
									break;
								}

								EditorHelper.TextField(target.replace != null ? target.replace.name : "", "");
								EditorHelper.TextField(target.replace != null ? $"{target.replace.NativeSize().x},{target.replace.NativeSize().y}" : "", "");
							}

							EditorGUILayout.EndVertical();

							if (EditorHelper.ButtonColor("+", Color.green, 23))
								target.targets.Add(null);

							for (int t = 0; t < target.targets.Count; t++)
							{
								EditorGUILayout.BeginVertical();
								{
									target.targets[t] = (Sprite)EditorHelper.ObjectField<Sprite>(target.targets[t], "", 0, 60, showAsBox: true);
									if (EditorHelper.ButtonColor("Remove", Color.red))
									{
										target.targets.RemoveAt(t);
										t--;
									}

									EditorHelper.TextField(target.targets[t] != null ? target.targets[t].name : "", "");
									EditorHelper.TextField(target.targets[t] != null ? $"{target.targets[t].NativeSize().x},{target.targets[t].NativeSize().y}" : "", "");
								}

								EditorGUILayout.EndVertical();
							}
						}

						EditorGUILayout.EndHorizontal();
						EditorGUILayout.Separator();
					}

					if (EditorHelper.Button("Search and replace"))
					{
						int count = 0;
						var objectsFound = new List<GameObject>();
						var assetIds = AssetDatabase.FindAssets("t:prefab", new[] { "Assets" });
						foreach (var guid in assetIds)
						{
							var obj = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
							bool valid = false;

							foreach (var target in m_SpriteReplace.inputs)
							{
								for (int t = target.targets.Count - 1; t >= 0; t--)
									if (target.targets[t] == null)
										target.targets.RemoveAt(t);
							}

							var images = obj.FindComponentsInChildren<Image>();
							foreach (var com in images)
							{
								foreach (var target in m_SpriteReplace.inputs)
									if (target.targets.Contains(com.sprite))
									{
										com.sprite = target.replace;
										valid = true;
										count++;
									}
							}

							var buttons = obj.FindComponentsInChildren<Components.JustButton>();
							foreach (var com in buttons)
							{
								foreach (var target in m_SpriteReplace.inputs)
								{
									if (target.targets.Contains(com.mImgActive))
									{
										com.mImgActive = target.replace;
										valid = true;
									}

									if (target.targets.Contains(com.mImgInactive))
									{
										com.mImgInactive = target.replace;
										valid = true;
										count++;
									}
								}
							}

							var sptRenderers = obj.FindComponentsInChildren<SpriteRenderer>();
							foreach (var com in sptRenderers)
							{
								foreach (var target in m_SpriteReplace.inputs)
									if (target.targets.Contains(com.sprite))
									{
										com.sprite = target.replace;
										valid = true;
										count++;
									}
							}

							if (valid && !objectsFound.Contains(obj))
								objectsFound.Add(obj);
						}

						foreach (var g in objectsFound)
							EditorUtility.SetDirty(g);

						Selection.objects = objectsFound.ToArray();
						AssetDatabase.SaveAssets();

						Debug.Log($"Replace {count} Objects");
					}
				}

				EditorGUILayout.EndVertical();
			}
		}

		//
		public class FontReplace
		{
			public class Input
			{
				public readonly List<Font> targets = new List<Font>();
				public Font replace;
			}

			public readonly List<Input> inputs = new List<Input>();
		}

		private FontReplace m_FontReplace = new FontReplace();

		private void SearchAndReplaceFont()
		{
			var btn = new EditorButton()
			{
				color = Color.yellow,
				onPressed = () => m_FontReplace.inputs.Add(new FontReplace.Input()),
				label = "Add Targets And Replace",
			};

			if (EditorHelper.HeaderFoldout("Search And Replace Font", null, false, btn))
			{
				EditorGUILayout.BeginVertical("box");
				{
					m_FontReplace ??= new FontReplace();

					foreach (var target in m_FontReplace.inputs)
					{
						EditorGUILayout.BeginHorizontal();
						{
							EditorGUILayout.BeginVertical();
							{
								EditorGUILayout.BeginHorizontal();
								{
									target.replace = (Font)EditorHelper.ObjectField<Font>(target.replace, "");
									if (EditorHelper.ButtonColor("x", Color.red, 23))
									{
										m_FontReplace.inputs.Remove(target);
										break;
									}
								}

								EditorGUILayout.EndHorizontal();
							}

							EditorGUILayout.EndVertical();

							if (EditorHelper.ButtonColor("+", Color.green, 23))
								target.targets.Add(null);

							for (int t = 0; t < target.targets.Count; t++)
							{
								EditorGUILayout.BeginVertical();
								{
									EditorGUILayout.BeginHorizontal();
									{
										target.targets[t] = (Font)EditorHelper.ObjectField<Font>(target.targets[t], "");
										if (EditorHelper.ButtonColor("x", Color.red, 23))
										{
											target.targets.RemoveAt(t);
											t--;
										}
									}

									EditorGUILayout.EndHorizontal();
								}

								EditorGUILayout.EndVertical();
							}
						}

						EditorGUILayout.EndHorizontal();
						EditorGUILayout.Separator();
					}

					if (EditorHelper.Button("Search and replace in Projects"))
					{
						int count = 0;
						var objectsFound = new List<GameObject>();
						var assetIds = AssetDatabase.FindAssets("t:prefab", new[] { "Assets" });
						foreach (var guid in assetIds)
						{
							var obj = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
							bool valid = false;

							foreach (var target in m_FontReplace.inputs)
							{
								for (int t = target.targets.Count - 1; t >= 0; t--)
									if (target.targets[t] == null)
										target.targets.RemoveAt(t);
							}

							var images = obj.FindComponentsInChildren<Text>();
							foreach (var com in images)
							{
								foreach (var target in m_FontReplace.inputs)
									if (target.targets.Contains(com.font))
									{
										com.font = target.replace;
										valid = true;
										count++;
									}
							}

							if (valid && !objectsFound.Contains(obj))
								objectsFound.Add(obj);
						}

						foreach (var g in objectsFound)
							EditorUtility.SetDirty(g);

						Selection.objects = objectsFound.ToArray();
						AssetDatabase.SaveAssets();

						Debug.Log($"Replace {count} Objects");
					}

					if (EditorHelper.Button("Search and replace in Scene"))
					{
						var objs = FindObjectsOfType<GameObject>();
						int count = 0;
						//var objs = Selection.gameObjects;
						var objectsFound = new List<GameObject>();
						for (int i = 0; i < objs.Length; i++)
						{
							bool valid = false;
							var obj = objs[i];
							var images = obj.FindComponentsInChildren<Text>();
							foreach (var com in images)
							{
								foreach (var target in m_FontReplace.inputs)
									if (target.targets.Contains(com.font))
									{
										com.font = target.replace;
										valid = true;
										count++;
									}
							}

							if (valid && !objectsFound.Contains(obj))
								objectsFound.Add(obj);
						}

						foreach (var g in objectsFound)
							EditorUtility.SetDirty(g);

						Selection.objects = objectsFound.ToArray();
						AssetDatabase.SaveAssets();

						Debug.Log($"Replace {count} Objects");
					}
				}

				EditorGUILayout.EndVertical();
			}
		}

		//
		public class TextMeshProFontReplace
		{
			public class Input
			{
				public readonly List<TMP_FontAsset> targets = new List<TMP_FontAsset>();
				public TMP_FontAsset replace;
			}

			public readonly List<Input> inputs = new List<Input>();
		}

		public TextMeshProFontReplace m_TMPFontReplace = new TextMeshProFontReplace();

		private void SearchAndReplaceTMPFont()
		{
			var btn = new EditorButton()
			{
				color = Color.yellow,
				onPressed = () => m_TMPFontReplace.inputs.Add(new TextMeshProFontReplace.Input()),
				label = "Add Targets And Replace",
			};

			if (EditorHelper.HeaderFoldout("Search And Replace TMP Font", null, false, btn))
			{
				EditorGUILayout.BeginVertical("box");
				{
					m_TMPFontReplace ??= new TextMeshProFontReplace();

					foreach (var target in m_TMPFontReplace.inputs)
					{
						EditorGUILayout.BeginHorizontal();
						{
							EditorGUILayout.BeginVertical();
							{
								EditorGUILayout.BeginHorizontal();
								{
									target.replace = (TMP_FontAsset)EditorHelper.ObjectField<TMP_FontAsset>(target.replace, "");
									if (EditorHelper.ButtonColor("x", Color.red, 23))
									{
										m_TMPFontReplace.inputs.Remove(target);
										break;
									}
								}

								EditorGUILayout.EndHorizontal();
							}

							EditorGUILayout.EndVertical();

							if (EditorHelper.ButtonColor("+", Color.green, 23))
								target.targets.Add(null);

							for (int t = 0; t < target.targets.Count; t++)
							{
								EditorGUILayout.BeginVertical();
								{
									EditorGUILayout.BeginHorizontal();
									{
										target.targets[t] = (TMP_FontAsset)EditorHelper.ObjectField<TMP_FontAsset>(target.targets[t], "");
										if (EditorHelper.ButtonColor("x", Color.red, 23))
										{
											target.targets.RemoveAt(t);
											t--;
										}
									}

									EditorGUILayout.EndHorizontal();
								}

								EditorGUILayout.EndVertical();
							}
						}

						EditorGUILayout.EndHorizontal();
						EditorGUILayout.Separator();
					}

					if (EditorHelper.Button("Search and replace in Projects"))
					{
						int count = 0;
						var objectsFound = new List<GameObject>();
						var assetIds = AssetDatabase.FindAssets("t:prefab", new[] { "Assets" });
						foreach (var guid in assetIds)
						{
							var obj = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
							bool valid = false;

							foreach (var target in m_TMPFontReplace.inputs)
							{
								for (int t = target.targets.Count - 1; t >= 0; t--)
									if (target.targets[t] == null)
										target.targets.RemoveAt(t);
							}

							var txtsUI = obj.FindComponentsInChildren<TextMeshProUGUI>();
							foreach (var com in txtsUI)
							{
								foreach (var target in m_TMPFontReplace.inputs)
									if (target.targets.Contains(com.font))
									{
										com.font = target.replace;
										valid = true;
										count++;
									}
							}

							var txts = obj.FindComponentsInChildren<TextMeshPro>();
							foreach (var com in txts)
							{
								foreach (var target in m_TMPFontReplace.inputs)
									if (target.targets.Contains(com.font))
									{
										com.font = target.replace;
										valid = true;
										count++;
									}
							}

							if (valid && !objectsFound.Contains(obj))
								objectsFound.Add(obj);
						}

						foreach (var g in objectsFound)
							EditorUtility.SetDirty(g);

						Selection.objects = objectsFound.ToArray();
						AssetDatabase.SaveAssets();

						Debug.Log($"Replace {count} Objects");
					}

					if (EditorHelper.Button("Search and replace in Scene"))
					{
						var objs = FindObjectsOfType<GameObject>();
						int count = 0;
						var objectsFound = new List<GameObject>();
						for (int i = 0; i < objs.Length; i++)
						{
							bool valid = false;
							var obj = objs[i];
							var txtsUI = obj.FindComponentsInChildren<TextMeshProUGUI>();
							foreach (var com in txtsUI)
							{
								foreach (var target in m_TMPFontReplace.inputs)
									if (target.targets.Contains(com.font))
									{
										com.font = target.replace;
										valid = true;
										count++;
									}
							}

							var txts = obj.FindComponentsInChildren<TextMeshPro>();
							foreach (var com in txts)
							{
								foreach (var target in m_TMPFontReplace.inputs)
									if (target.targets.Contains(com.font))
									{
										com.font = target.replace;
										valid = true;
										count++;
									}
							}

							if (valid && !objectsFound.Contains(obj))
								objectsFound.Add(obj);
						}

						foreach (var g in objectsFound)
							EditorUtility.SetDirty(g);

						Selection.objects = objectsFound.ToArray();
						AssetDatabase.SaveAssets();

						Debug.Log($"Replace {count} Objects");
					}
				}

				EditorGUILayout.EndVertical();
			}
		}
		
#endregion

		//===================================================================================================

#region Math Utilities

		private DayOfWeek m_NextDayOfWeek;

		private void DrawMathUtilities()
		{
			EditorHelper.HeaderFoldout("Math Utilities", "", GetSecondsTillEndDayOfWeek);
		}

		private void GetSecondsTillEndDayOfWeek()
		{
			EditorHelper.BoxVertical("Seconds till day of week", () =>
			{
				m_NextDayOfWeek = EditorHelper.DropdownListEnum(m_NextDayOfWeek, "Day of week");
				var seconds = TimeHelper.GetSecondsTillDayOfWeek(m_NextDayOfWeek, DateTime.Now);
				EditorHelper.TextField(seconds.ToString(), "Seconds till day of week", 200);
				seconds = TimeHelper.GetSecondsTillEndDayOfWeek(m_NextDayOfWeek, DateTime.Now);
				EditorHelper.TextField(seconds.ToString(), "Seconds till end day of week", 200);
			}, Color.white, true);
		}

#endregion

		//===================================================================================================

#region Generator

		private List<AnimationClip> m_AnimationClips;
		private List<string> m_AnimationPaths;
		private EditorPrefsString m_AnimationClipsPackScript;
		private EditorPrefsString m_AnimationClipsPackPath;

		private void DrawGenerators()
		{
			GenerateAnimationsPackScript();
		}

		private void GenerateAnimationsPackScript()
		{
			if (EditorHelper.HeaderFoldout("Generate Animations Pack Script"))
			{
				if (!SelectedObject())
					return;

				m_AnimationClipsPackScript = new EditorPrefsString("m_AnimationClipsPackScript");
				m_AnimationClipsPackPath = new EditorPrefsString("m_AnimationClipsPackPath");

				if (EditorHelper.Button("Scan"))
				{
					m_AnimationClips = new List<AnimationClip>();
					m_AnimationPaths = new List<string>();

					var objs = Selection.gameObjects;
					for (int i = 0; i < objs.Length; i++)
					{
						var path = AssetDatabase.GetAssetPath(objs[i]);
						var representations = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
						foreach (var asset in representations)
						{
							var clip = asset as AnimationClip;
							if (clip != null)
							{
								m_AnimationClips.Add(clip);
								m_AnimationPaths.Add(path);
							}
						}
					}
				}

				if (m_AnimationClips != null && m_AnimationClips.Count > 0)
				{
					GUILayout.BeginVertical("box");
					for (int i = 0; i < m_AnimationClips.Count; i++)
					{
						var clip = m_AnimationClips[i];
						EditorGUILayout.LabelField($"{i}: {clip.name} | {clip.length} | {clip.wrapMode} | {clip.isLooping}");
					}

					GUILayout.BeginHorizontal();
					m_AnimationClipsPackScript.Value = EditorHelper.TextField(m_AnimationClipsPackScript.Value, "Script Name");

					if (EditorHelper.Button("Generate Script"))
					{
						if (string.IsNullOrEmpty(m_AnimationClipsPackScript.Value))
							return;

						string templateFilePath = "Assets/RCore/Utilities/Editor/AnimationsPackTemplate.txt";
						string fieldsName = "";
						string enum_ = "\tpublic enum Clip \n\t{\n";
						string indexes = "";
						string arrayElements = "";
						string paths = "";
						string names = "";
						string validateFields = "";
						int i = 0;
						foreach (var clip in m_AnimationClips)
						{
							string fieldName = clip.name.ToCapitalizeEachWord().Replace(" ", "").RemoveSpecialCharacters();
							fieldsName += $"\tpublic AnimationClip {fieldName};\n";
							enum_ += $"\t\t{fieldName} = {i},\n";
							indexes += $"\tpublic const int {fieldName}_ID = {i};\n";
							names += $"\tpublic const string {fieldName}_NAME = \"{clip.name}\";\n";
							arrayElements += $"\t\t\t\t{fieldName},\n";
							paths += $"\tprivate const string {fieldName}_PATH = \"{m_AnimationPaths[i]}\";\n";
							validateFields += $"\t\t\tif ({fieldName} == null) {fieldName} = RCore.Common.EditorHelper.GetAnimationFromModel({fieldName}_PATH, {fieldName}_NAME);\n";
							validateFields += $"\t\t\tif ({fieldName} == null) Debug.LogError(nameof({fieldName}) + \" is Null\");\n";
							i++;
						}

						enum_ += "\t}\n";
						var generatedContent = AssetDatabase.LoadAssetAtPath<TextAsset>(templateFilePath).text;
						generatedContent = generatedContent
							.Replace("<class_name>", m_AnimationClipsPackScript.Value)
							.Replace("<enum_>", enum_)
							.Replace("<const>", indexes)
							.Replace("<fieldsName>", fieldsName)
							.Replace("<names>", names)
							.Replace("<paths>", paths)
							.Replace("<arrayElements>", arrayElements)
							.Replace("<validateFields>", validateFields);

						Debug.Log($"{generatedContent}");

						m_AnimationClipsPackPath.Value = EditorHelper.SaveFilePanel(m_AnimationClipsPackPath.Value, m_AnimationClipsPackScript.Value, generatedContent, "cs");
					}

					GUILayout.EndHorizontal();
					GUILayout.EndVertical();
				}
			}
		}

#endregion

		//===================================================================================================

#region Remove Duplicate Characters

		private static string m_CombinedTextsResult;
		private static readonly List<TextAsset> m_TextFiles = new List<TextAsset>();
		private static void DrawRemoveDuplicateCharacters()
		{
			if (EditorHelper.HeaderFoldout("Remove Duplicate Characters"))
			{
				if (EditorHelper.ButtonColor("Add Txt File", Color.green))
					m_TextFiles.Add(null);
				EditorHelper.DragDropBox<TextAsset>("TextAsset", objs =>
				{
					foreach (var obj in objs)
						m_TextFiles.Add(obj);
				});
				for (int i = 0; i < m_TextFiles.Count; i++)
				{
					EditorGUILayout.BeginHorizontal();
					m_TextFiles[i] = (TextAsset)EditorHelper.ObjectField<TextAsset>(m_TextFiles[i], null);
					if (EditorHelper.ButtonColor("+", Color.green, 23))
						m_TextFiles.Insert(i + 1, null);
					if (EditorHelper.ButtonColor("x", Color.red, 23))
						m_TextFiles.RemoveAt(i);
					EditorGUILayout.EndHorizontal();
				}
				if (EditorHelper.Button("Remove Duplicate Characters"))
				{
					string combineStr = "";
					foreach (var textAsset in m_TextFiles)
					{
						if (textAsset != null)
							combineStr += textAsset.text;
					}
					
					m_CombinedTextsResult = string.Empty;
					var unique = new HashSet<char>(combineStr);
					foreach (char c in unique)
						m_CombinedTextsResult += c;
					m_CombinedTextsResult = string.Concat(m_CombinedTextsResult.OrderBy(c => c));
				}
				m_CombinedTextsResult = EditorHelper.TextArea(m_CombinedTextsResult, null);
				if (EditorHelper.Button("Save To File"))
				{
					EditorHelper.SaveFilePanel(null, "combined_text", m_CombinedTextsResult, "txt");
				}
			}
		}

#endregion
		
		//===================================================================================================
		
		private static bool SelectedObject()
		{
			if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
			{
				EditorGUILayout.HelpBox("Select at least one GameObject to see how it work", MessageType.Info);
				return false;
			}

			return true;
		}

		private static Dictionary<GameObject, List<T>> FindComponents<T>(ConditionalDelegate<T> pValidCondition) where T : Component
		{
			var allComponents = new Dictionary<GameObject, List<T>>();
			var objs = Selection.gameObjects;
			for (int i = 0; i < objs.Length; i++)
			{
				var components = objs[i].gameObject.FindComponentsInChildren<T>();
				if (components.Count > 0)
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

		[MenuItem("RUtilities/Tools/Tools Collection")]
		private static void OpenEditorWindow()
		{
			var window = GetWindow<ToolsCollectionWindow>("Tools Collection", true);
			window.Show();
		}
	}

	public delegate bool ConditionalDelegate<T>(T pComponent) where T : Component;
}