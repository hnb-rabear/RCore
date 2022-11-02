using UnityEngine;
using UnityEditor;
using RCore.Common;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using System;
using Debug = UnityEngine.Debug;
using System.IO;

namespace RCore.Editor
{
    public class ToolsCollectionWindow : EditorWindow
    {
        private Vector2 mScrollPosition;
        private void OnGUI()
        {
            mScrollPosition = GUILayout.BeginScrollView(mScrollPosition, false, false);

            GUILayout.Space(10);
            DrawGameObjectUtilities();

            GUILayout.Space(10);
            DrawRendererUtilities();

            GUILayout.Space(10);
            DrawUIUtilties();

            GUILayout.Space(10);
            DrawMathUtitlies();

            GUILayout.Space(10);
            DrawGenerators();

            GUILayout.EndScrollView();
        }

        #region GameObject Utilities
        private List<GameObject> m_ReplacableGameObjects = new List<GameObject>();
        private List<GameObject> m_Prefabs = new List<GameObject>();
        private void DrawGameObjectUtilities()
        {
            EditorHelper.HeaderFoldout("GameObject Utilties", "", () =>
            {
                ReplaceGameobjectsInScene();
                FindGameObjectsMissingScript();
            });
        }
        private void ReplaceGameobjectsInScene()
        {
            if (EditorHelper.HeaderFoldout("Replace gameobjects in scene"))
                EditorHelper.BoxVertical(() =>
                {
                    if (m_ReplacableGameObjects == null || m_ReplacableGameObjects.Count == 0)
                        EditorGUILayout.HelpBox("Select at least one Object to see how it work", MessageType.Info);

                    EditorHelper.ListObjects("Replaceable Objects", ref m_ReplacableGameObjects, null, false);
                    EditorHelper.ListObjects("Prefabs", ref m_Prefabs, null, false);

                    if (GUILayout.Button("Replace"))
                        EditorHelper.ReplaceGameobjectsInScene(ref m_ReplacableGameObjects, m_Prefabs);
                }, Color.white, true);
        }
        private bool m_AlsoChildren;
        private void FindGameObjectsMissingScript()
        {
            if (EditorHelper.HeaderFoldout("Find Gameobjects missing script"))
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
        private int m_SubmeshCount;
        private int m_TriangleCount;
        private void DrawRendererUtilities()
        {
            EditorHelper.HeaderFoldout("Renderer Utilties", "", () =>
            {
                DisplayMeshInfos();
                CombineMeshs();
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
                        EditorGUILayout.LabelField("Total SubMeshes: ", m_SubmeshCount.ToString());
                        EditorGUILayout.LabelField("Avr Vertices: ", (m_VertexCount / m_MeshCount).ToString());
                        EditorGUILayout.LabelField("Avr Triangles: ", (m_TriangleCount / m_MeshCount).ToString());
                    }

                    m_VertexCount = 0;
                    m_TriangleCount = 0;
                    m_SubmeshCount = 0;
                    m_MeshCount = 0;

                    foreach (GameObject g in Selection.gameObjects)
                    {
                        var filter = g.GetComponent<MeshFilter>();

                        if (filter != null && filter.sharedMesh != null)
                        {
                            var a = filter.sharedMesh.vertexCount;
                            var b = filter.sharedMesh.triangles.Length / 3;
                            var c = filter.sharedMesh.subMeshCount;
                            m_VertexCount += a;
                            m_TriangleCount += b;
                            m_SubmeshCount += c;
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

                                a += obj.sharedMesh.vertexCount;
                                b += obj.sharedMesh.triangles.Length / 3;
                                c += obj.sharedMesh.subMeshCount;
                            }
                            m_VertexCount += a;
                            m_TriangleCount += b;
                            m_SubmeshCount += c;
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
        private void CombineMeshs()
        {
            if (EditorHelper.HeaderFoldout("Combine Meshs"))
            {
                if (!SelectedObject())
                    return;

                bool available = false;
                var meshFilters = new List<MeshFilter>();
                foreach (GameObject g in Selection.gameObjects)
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
                if (EditorHelper.Button("Combine Meshs"))
                {
                    var combinedMeshs = new GameObject();
                    combinedMeshs.name = "Meshs_Combined";
                    combinedMeshs.AddComponent<MeshRenderer>();
                    combinedMeshs.AddComponent<MeshFilter>();

                    meshFilters = new List<MeshFilter>();
                    foreach (GameObject g in Selection.gameObjects)
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
                    combinedMeshs.GetComponent<MeshFilter>().sharedMesh = new Mesh();
                    combinedMeshs.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combine);
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
        private void DrawUIUtilties()
        {
            EditorHelper.HeaderFoldout("UI Utilties", "", () =>
            {
                FormatTexts();
                SketchImages();
                ToggleRaycastAll();
                ChangeButtonsTransitionColor();
                ChangeTextsFont();
                ChangeTMPTextsFont();
                RepalceTextByTextTMP();
                PerfectRatioButtons();
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
                    foreach (GameObject g in Selection.gameObjects)
                    {
                        var texts = g.FindComponentsInChildren<Text>();
                        var textPros = g.FindComponentsInChildren<TextMeshProUGUI>();
                        if (texts.Count > 0)
                        {
                            m_TextCount += texts.Count;
                            allTexts.AddRange(texts);
                            foreach (var t in allTexts)
                                EditorGUILayout.LabelField("Text: ", t.name.ToString());
                        }
                        if (textPros.Count > 0)
                        {
                            m_TextCount += textPros.Count;
                            allTextPros.AddRange(textPros);
                            foreach (var t in allTextPros)
                                EditorGUILayout.LabelField("Text Mesh Pro: ", t.name.ToString());
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
                    foreach (GameObject g in Selection.gameObjects)
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
                        foreach (GameObject g in Selection.gameObjects)
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
        private bool[] m_ColorBlocksForChange = new bool[4] { true, true, true, true };
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

                    EditorHelper.Seperator();
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
                    m_Buttons = FindComponents<Button>((button) =>
                    {
                        return button.image != null && button.image.sprite != null && button.image.color != Color.clear;
                    });
                }
                if (m_Buttons != null && m_Buttons.Count > 0)
                {
                    m_ButtonAnimation = (RuntimeAnimatorController)EditorHelper.ObjectField<RuntimeAnimatorController>(m_ButtonAnimation, "Animation controlelr", 120);
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

                    EditorHelper.Seperator();
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

                    EditorHelper.Seperator();
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

                    EditorHelper.Seperator();
                    foreach (var texts in m_TMPTexts)
                        EditorGUILayout.LabelField($"{texts.Key.name} has {texts.Value.Count} texts.");
                }

                GUILayout.EndVertical();
            }
        }
        private void RepalceTextByTextTMP()
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
                                var alignMent = item.Value[i].alignment;
                                var bestFit = item.Value[i].resizeTextForBestFit;
                                var color = item.Value[i].color;
                                GameObject.DestroyImmediate(item.Value[i]);
                                var textTMP = gameObj.AddComponent<TextMeshProUGUI>();
                                textTMP.text = content;
                                textTMP.fontSize = fontSize;
                                textTMP.enableAutoSizing = bestFit;
                                textTMP.color = color;
                                switch (alignMent)
                                {
                                    case TextAnchor.MiddleLeft: textTMP.alignment = TextAlignmentOptions.Left; break;
                                    case TextAnchor.MiddleCenter: textTMP.alignment = TextAlignmentOptions.Center; break;
                                    case TextAnchor.MiddleRight: textTMP.alignment = TextAlignmentOptions.Right; break;

                                    case TextAnchor.LowerLeft: textTMP.alignment = TextAlignmentOptions.BottomLeft; break;
                                    case TextAnchor.LowerCenter: textTMP.alignment = TextAlignmentOptions.Bottom; break;
                                    case TextAnchor.LowerRight: textTMP.alignment = TextAlignmentOptions.BottomRight; break;

                                    case TextAnchor.UpperLeft: textTMP.alignment = TextAlignmentOptions.TopLeft; break;
                                    case TextAnchor.UpperCenter: textTMP.alignment = TextAlignmentOptions.Top; break;
                                    case TextAnchor.UpperRight: textTMP.alignment = TextAlignmentOptions.TopRight; break;
                                }
                                Debug.Log($"Replace Text in gameobject {gameObj.name}");
                            }
                        }
                }
            }
        }
        private void PerfectRatioButtons()
        {
            if (EditorHelper.HeaderFoldout("Perfect Ratio Buttons"))
            {
                if (!SelectedObject())
                    return;

                if (EditorHelper.Button("Set Perfect Width"))
                {
                    foreach (GameObject g in Selection.gameObjects)
                    {
                        var buttons = g.FindComponentsInChildren<Button>();
                        foreach (var button in buttons)
                        {
                            var image = button.GetComponent<Image>();
                            if (image != null && image.sprite != null && image.type == Image.Type.Sliced)
                            {
                                var nativeSize = image.sprite.NativeSize();
                                var rectSize = image.rectTransform.sizeDelta;
                                if (rectSize.x > 0 && rectSize.x < nativeSize.x)
                                {
                                    var ratio = nativeSize.x * 1f / rectSize.x;
                                    image.pixelsPerUnitMultiplier = ratio;
                                }
                                else
                                    image.pixelsPerUnitMultiplier = 1;
                            }

                            Debug.Log($"Perfect ratio {image.name}");
                        }
                    }
                }
                if (EditorHelper.Button("Set Perfect Height"))
                {
                    foreach (GameObject g in Selection.gameObjects)
                    {
                        var buttons = g.FindComponentsInChildren<Button>();
                        foreach (var button in buttons)
                        {
                            var image = button.GetComponent<Image>();
                            if (image != null && image.sprite != null && image.type == Image.Type.Sliced)
                            {
                                var nativeSize = image.sprite.NativeSize();
                                var rectSize = image.rectTransform.sizeDelta;
                                if (rectSize.y > 0 && rectSize.y < nativeSize.y)
                                {
                                    var ratio = nativeSize.y * 1f / rectSize.y;
                                    image.pixelsPerUnitMultiplier = ratio;
                                }
                                else
                                    image.pixelsPerUnitMultiplier = 1;

                                Debug.Log($"Perfect ratio {image.name}");
                            }
                        }
                    }
                }
            }
        }
        #endregion
        //===================================================================================================
        #region Math Utilities
        private DayOfWeek m_NextDayOfWeeok;
        private void DrawMathUtitlies()
        {
            EditorHelper.HeaderFoldout("Math Utitlies", "", () =>
            {
                GetSecondsTillEndDayOfWeek();
            });
        }
        private void GetSecondsTillEndDayOfWeek()
        {
            EditorHelper.BoxVertical("Seconds till day of week", () =>
            {
                m_NextDayOfWeeok = EditorHelper.DropdownListEnum<DayOfWeek>(m_NextDayOfWeeok, "Day of week");
                var seconds = TimeHelper.GetSecondsTillDayOfWeek(m_NextDayOfWeeok, DateTime.Now);
                EditorHelper.TextField(seconds.ToString(), "Seconds till day of week", 200);
                seconds = TimeHelper.GetSecondsTillEndDayOfWeek(m_NextDayOfWeeok, DateTime.Now);
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
        private bool SelectedObject()
        {
            if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
            {
                EditorGUILayout.HelpBox("Select at least one GameObject to see how it work", MessageType.Info);
                return false;
            }
            return true;
        }
        private Dictionary<GameObject, List<T>> FindComponents<T>(ConditionalDelegate<T> pValidCondition) where T : Component
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