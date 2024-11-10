using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Linq;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace RCore.Editor.Tool
{
	public class ToolsCollectionWindow : EditorWindow
	{
		private Vector2 m_ScrollPosition;

		private void OnGUI()
		{
			m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition, false, false);
			
			DrawUtilities();
			DrawRendererUtilities();
			DrawUIUtilities();
			DrawGenerators();

			GUILayout.EndScrollView();
		}

#region GameObject Utilities

		private List<GameObject> m_ReplaceableGameObjects = new List<GameObject>();
		private List<GameObject> m_Prefabs = new List<GameObject>();

		private void DrawUtilities()
		{
			EditorHelper.HeaderFoldout("Utilities", "", () =>
			{
				ReplaceGameObjectsInScene();
				FindObjectsByGuid();
			});
		}

		private void ReplaceGameObjectsInScene()
		{
			if (EditorHelper.HeaderFoldout("Replace GameObjects in scene"))
				EditorHelper.BoxVertical(() =>
				{
					if (m_ReplaceableGameObjects == null || m_ReplaceableGameObjects.Count == 0)
						EditorGUILayout.HelpBox("Select at least one GameObject in Scene", MessageType.Info);

					EditorHelper.ListObjects("Replaceable Objects", ref m_ReplaceableGameObjects, null, false);
					EditorHelper.ListObjects("Prefabs", ref m_Prefabs, null, false);

					if (GUILayout.Button("Replace"))
						EditorHelper.ReplaceGameObjectsInScene(ref m_ReplaceableGameObjects, m_Prefabs);
				}, Color.white, true);
		}

		private string m_Guid;
		private Object m_FoundObject;
		private void FindObjectsByGuid()
		{
			if (EditorHelper.HeaderFoldout("Find Object by guid"))
			{
				m_Guid = EditorHelper.TextField(m_Guid, "Guid");
				EditorHelper.ObjectField<Object>(m_FoundObject, "Object");
				if (!string.IsNullOrEmpty(m_Guid) && EditorHelper.Button("Find"))
				{
					string path = AssetDatabase.GUIDToAssetPath(m_Guid);
					if (!string.IsNullOrEmpty(path))
						m_FoundObject = AssetDatabase.LoadAssetAtPath<Object>(path);
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
						EditorGUILayout.HelpBox("Select GameObjects that have MeshFilter component", MessageType.Info);

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

						var objs = g.GetComponentsInChildren<SkinnedMeshRenderer>(true);
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
				if (!HasSelectedGameObject("Select GameObjects that have a MeshFilter and MeshRenderer component"))
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
					EditorGUILayout.HelpBox("Select GameObjects that have a MeshFilter and MeshRenderer component", MessageType.Info);
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
						var filters = g.GetComponentsInChildren<MeshFilter>(true);
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
			if (EditorHelper.HeaderFoldout("Align Center MeshRenderer obj"))
			{
				if (!HasSelectedGameObject("Select GameObjects that have a MeshRenderer component"))
					return;

				foreach (var g in Selection.gameObjects)
				{
					var renderer = g.transform.GetComponent<Renderer>();
					var center = renderer.bounds.extents;
					g.transform.localPosition = new Vector3(-center.x, g.transform.localPosition.y, -center.z);
				}
			}
		}

#endregion

		//===================================================================================================

#region UI Utilities

		private enum FormatType
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
				ToggleRaycastAll();
				ChangeButtonsTransitionColor();
				ChangeTextsFont();
				ChangeTMPTextsFont();
				ConvertSpriteRendererToImage();
			});
		}

		private void FormatTexts()
		{
			if (EditorHelper.HeaderFoldout("Format Texts"))
			{
				GUILayout.BeginVertical("box");
				{
					EditorGUILayout.HelpBox("Select GameObjects that have a Text or TextMeshProUGUI component either on them or within their children", MessageType.Info);
					
					if (m_TextCount > 0)
						EditorGUILayout.LabelField("Text Count: ", m_TextCount.ToString());

					m_FormatType = EditorHelper.DropdownListEnum(m_FormatType, "Format Type");

					m_TextCount = 0;
					var allTexts = new List<Text>();
					var allTextPros = new List<TextMeshProUGUI>();
					foreach (var g in Selection.gameObjects)
					{
						var texts = g.GetComponentsInChildren<Text>(true);
						var textPros = g.GetComponentsInChildren<TextMeshProUGUI>(true);
						if (texts.Length > 0)
						{
							m_TextCount += texts.Length;
							allTexts.AddRange(texts);
							foreach (var t in allTexts)
								EditorGUILayout.LabelField("Text: ", t.name);
						}

						if (textPros.Length > 0)
						{
							m_TextCount += textPros.Length;
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

		private int m_RaycastOnCount;
		private int m_RaycastOffCount;
		private List<Graphic> m_Graphics;

		private void ToggleRaycastAll()
		{
			if (EditorHelper.HeaderFoldout("Toggle Raycast All"))
			{
				GUILayout.BeginVertical("box");
				{
					if (!HasSelectedGameObject("Select at least one Graphic GameObject"))
					{
						GUILayout.EndVertical();
						return;
					}

					if (EditorHelper.Button("Scan"))
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

                if (!HasSelectedGameObject("Select GameObjects that have a Button component either on them or within their children"))
				{
					GUILayout.EndVertical();
					return;
				}

				if (EditorHelper.Button("Scan"))
				{
					m_Buttons = EditorHelper.FindComponents<Button>(Selection.gameObjects, button => button.image != null && button.image.color != Color.clear);
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
								Debug.Log($"{button.name} updated!", EditorGUIUtility.isProSkin ? Color.green : ColorHelper.DarkGreenX11);
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

                if (!HasSelectedGameObject("Select GameObjects that have a Button component either on them or within their children"))
				{
					GUILayout.EndVertical();
					return;
				}

				if (EditorHelper.Button("Scan"))
				{
					m_Buttons = EditorHelper.FindComponents<Button>(Selection.gameObjects, button => button.image != null && button.image.sprite != null && button.image.color != Color.clear);
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
								Debug.Log($"{button.name} updated!", EditorGUIUtility.isProSkin ? Color.green : ColorHelper.DarkGreenX11);
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

                if (!HasSelectedGameObject("Select GameObjects that have a Text component either on them or within their children"))
				{
					GUILayout.EndVertical();
					return;
				}

				if (EditorHelper.Button("Scan Texts"))
					m_Texts = EditorHelper.FindComponents<Text>(Selection.gameObjects, null);

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
									Debug.Log($"{text.name} unchanged!", EditorGUIUtility.isProSkin ? Color.yellow : ColorHelper.DarkOrange);
								else
								{
									text.font = m_Font;
									Debug.Log($"{text.name} updated!", EditorGUIUtility.isProSkin ? Color.green : ColorHelper.DarkGreenX11);
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

                if (!HasSelectedGameObject("Select GameObjects that have a TextMeshProUGUI component either on them or within their children"))
				{
					GUILayout.EndVertical();
					return;
				}

				if (EditorHelper.Button("Scan Texts"))
					m_TMPTexts = EditorHelper.FindComponents<TextMeshProUGUI>(Selection.gameObjects, null);

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
									Debug.Log($"{text.name} unchanged!", EditorGUIUtility.isProSkin ? Color.yellow : ColorHelper.DarkOrange);
								else
								{
									text.font = m_TMPFont;
									Debug.Log($"{text.name} updated!", EditorGUIUtility.isProSkin ? Color.green : ColorHelper.DarkGreenX11);
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

		private static void ConvertSpriteRendererToImage()
		{
			if (EditorHelper.HeaderFoldout("Convert Transform To RectTransform"))
			{
				if (!HasSelectedGameObject("Select Non-UI GameObjects on Scene or Hierarchy"))
					return;

				if (EditorHelper.Button("Convert"))
				{
					foreach (var g in Selection.gameObjects)
					{
						var children = g.GetComponentsInChildren<Component>(true);
						for (int i = children.Length - 1; i >= 0; i--)
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

#endregion

		//===================================================================================================

#region Generator

		private List<AnimationClip> m_AnimationClips;
		private List<string> m_AnimationPaths;
		private EditorPrefsString m_AnimationClipsPackScript;
		private EditorPrefsString m_AnimationClipsPackPath;

		private void DrawGenerators()
		{
			EditorHelper.HeaderFoldout("Script Generators", "", () =>
			{
				GenerateAnimationsPackScript();
				GenerateCharactersMap();
				GenerateJsonListOfFiles();
			});
		}

		private void GenerateAnimationsPackScript()
		{
			if (EditorHelper.HeaderFoldout("Generate Animations Pack Script"))
			{
				if (!HasSelectedGameObject("Select FBX files that have Animations"))
					return;
				
				GUILayout.BeginVertical("box");

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
							validateFields += $"\t\t\tif ({fieldName} == null) {fieldName} = RCore.Editor.EditorHelper.GetAnimationFromModel({fieldName}_PATH, {fieldName}_NAME);\n";
							validateFields += $"\t\t\tif ({fieldName} == null) Debug.LogError(nameof({fieldName}) + \" is Null\");\n";
							i++;
						}

						enum_ += "\t}\n";
						var generatedContent = Resources.Load<TextAsset>("AnimationsPackTemplate.txt").text;
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
				}
				GUILayout.EndVertical();
			}
		}

		private void GenerateJsonListOfFiles()
		{
			if (EditorHelper.HeaderFoldout("Generate a Json contain list of files"))
			{
				if (!HasSelectedObject("Select Text Files"))
					return;
				
				GUILayout.BeginVertical("box");

				if (EditorHelper.Button("Generate"))
				{
					var names = new List<string>();
					string folderPath = "";
					foreach (var t in Selection.objects)
					{
						var path = AssetDatabase.GetAssetPath(t);
						var fileName = Path.GetFileName(path);
						names.Add(fileName);
						if (folderPath == "")
							folderPath = Path.GetDirectoryName(path);
					}
					var json = JsonHelper.ToJson(names);
					EditorHelper.SaveFilePanel(folderPath, "all_files", json, "json");
				}
				GUILayout.EndVertical();
			}
		}

#endregion

		//===================================================================================================

#region Remove Duplicate Characters

		private static string m_CombinedTextsResult;
		private static readonly List<TextAsset> m_TextFiles = new List<TextAsset>();
		private static void GenerateCharactersMap()
		{
			if (EditorHelper.HeaderFoldout("Generate Characters Set"))
			{
				if (EditorHelper.ButtonColor("Add Txt File", Color.green))
				{
					m_TextFiles.Add(null);
					RemoveDuplicateCharacters();
				}
				EditorHelper.DragDropBox<TextAsset>("TextAsset", objs =>
				{
					foreach (var obj in objs)
						m_TextFiles.Add(obj);
					RemoveDuplicateCharacters();
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
				void RemoveDuplicateCharacters()
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
				if (EditorHelper.Button("Save Characters Set"))
				{
					EditorHelper.SaveFilePanel(null, "combined_text", m_CombinedTextsResult, "txt");
				}
			}
		}

#endregion

		public static void ShowWindow()
		{
			var window = GetWindow<ToolsCollectionWindow>("Tools Collection", true);
			window.Show();
		}
		
		public static bool HasSelectedGameObject(string message = null)
		{
			if (string.IsNullOrEmpty(message))
				message = $"Select at least one GameObject";
			if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
			{
				EditorGUILayout.HelpBox(message, MessageType.Warning);
				return false;
			}
			EditorGUILayout.HelpBox(message, MessageType.Info);
			return true;
		}

		public static bool HasSelectedObject(string message = null)
		{
			if (string.IsNullOrEmpty(message))
				message = "Select at least one Object";
			if (Selection.objects == null || Selection.objects.Length == 0)
			{
				EditorGUILayout.HelpBox(message, MessageType.Warning);
				return false;
			}
			EditorGUILayout.HelpBox(message, MessageType.Info);
			return true;
		}
	}
}