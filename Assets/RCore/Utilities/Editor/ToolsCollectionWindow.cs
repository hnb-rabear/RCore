using UnityEngine;
using UnityEditor;
using RCore.Common;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using System;
using Debug = RCore.Common.Debug;

namespace RCore.Editor
{
    public class ToolsCollectionWindow : EditorWindow
    {
        private Vector2 mScrollPosition;
        private void OnGUI()
        {
            mScrollPosition = GUILayout.BeginScrollView(mScrollPosition, false, false);

            GUILayout.Space(15);
            DrawGameObjectUtilities();

            GUILayout.Space(15);
            DrawRendererUtilities();

            GUILayout.Space(15);
            DrawUIUtilties();

            GUILayout.Space(15);
            DrawMathUtitlies();

            GUILayout.EndScrollView();
        }

        #region GameObject Utilities
        public List<GameObject> sources = new List<GameObject>();
        public List<GameObject> prefabs = new List<GameObject>();
        private void DrawGameObjectUtilities()
        {
            EditorHelper.HeaderFoldout("GameObject Utilties", "", () =>
            {
                ReplaceGameobjects();
                FindGameObjectsMissingScript();
            });
        }
        private void ReplaceGameobjects()
        {
            if (EditorHelper.HeaderFoldout("Replace gameobjects"))
                EditorHelper.BoxVertical(() =>
                {
                    if (sources == null || sources.Count == 0)
                        EditorGUILayout.HelpBox("Select at least one Object to see how it work", MessageType.Info);

                    EditorHelper.ListObjects("Replaceable Objects", ref sources, null, false);
                    EditorHelper.ListObjects("Prefabs", ref prefabs, null, false);

                    if (GUILayout.Button("Replace"))
                        EditorHelper.ReplaceGameobjectsInScene(ref sources, prefabs);
                }, Color.white, true);
        }
        private bool mAlsoChildren;
        private void FindGameObjectsMissingScript()
        {
            if (EditorHelper.HeaderFoldout("Find Gameobjects missing script"))
            {
                mAlsoChildren = EditorHelper.Toggle(mAlsoChildren, "Also Children of children");
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

                        if (mAlsoChildren)
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
        private int mMeshCount = 1;
        private int mVertexCount;
        private int mSubmeshCount;
        private int mTriangleCount;
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
                    if (mMeshCount == 0)
                        EditorGUILayout.HelpBox("Select at least one Mesh Object to see how it work", MessageType.Info);

                    if (mMeshCount > 1)
                    {
                        EditorGUILayout.LabelField("Total Vertices: ", mVertexCount.ToString());
                        EditorGUILayout.LabelField("Total Triangles: ", mTriangleCount.ToString());
                        EditorGUILayout.LabelField("Total SubMeshes: ", mSubmeshCount.ToString());
                        EditorGUILayout.LabelField("Avr Vertices: ", (mVertexCount / mMeshCount).ToString());
                        EditorGUILayout.LabelField("Avr Triangles: ", (mTriangleCount / mMeshCount).ToString());
                    }

                    mVertexCount = 0;
                    mTriangleCount = 0;
                    mSubmeshCount = 0;
                    mMeshCount = 0;

                    foreach (GameObject g in Selection.gameObjects)
                    {
                        var filter = g.GetComponent<MeshFilter>();

                        if (filter != null && filter.sharedMesh != null)
                        {
                            var a = filter.sharedMesh.vertexCount;
                            var b = filter.sharedMesh.triangles.Length / 3;
                            var c = filter.sharedMesh.subMeshCount;
                            mVertexCount += a;
                            mTriangleCount += b;
                            mSubmeshCount += c;
                            mMeshCount += 1;

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
                            mVertexCount += a;
                            mTriangleCount += b;
                            mSubmeshCount += c;
                            mMeshCount += 1;
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
        private FormatType mFormatType;
        private int mTextCount;
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
            });
        }
        private void FormatTexts()
        {
            if (EditorHelper.HeaderFoldout("Format Texts"))
            {
                GUILayout.BeginVertical("box");
                {
                    if (mTextCount == 0)
                        EditorGUILayout.HelpBox("Select at least one Text Object to see how it work", MessageType.Info);
                    else
                        EditorGUILayout.LabelField("Text Count: ", mTextCount.ToString());

                    mFormatType = EditorHelper.DropdownListEnum(mFormatType, "Format Type");

                    mTextCount = 0;
                    var allTexts = new List<Text>();
                    var allTextPros = new List<TextMeshProUGUI>();
                    foreach (GameObject g in Selection.gameObjects)
                    {
                        var texts = g.FindComponentsInChildren<Text>();
                        var textPros = g.FindComponentsInChildren<TextMeshProUGUI>();
                        if (texts.Count > 0)
                        {
                            mTextCount += texts.Count;
                            allTexts.AddRange(texts);
                            foreach (var t in allTexts)
                                EditorGUILayout.LabelField("Text: ", t.name.ToString());
                        }
                        if (textPros.Count > 0)
                        {
                            mTextCount += textPros.Count;
                            allTextPros.AddRange(textPros);
                            foreach (var t in allTextPros)
                                EditorGUILayout.LabelField("Text Mesh Pro: ", t.name.ToString());
                        }
                    }

                    if (EditorHelper.Button("Format"))
                    {
                        foreach (var t in allTexts)
                        {
                            switch (mFormatType)
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
                            switch (mFormatType)
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
        private float mImgWidth;
        private float mImgHeight;
        private int mCountImgs;
        private void SketchImages()
        {
            if (EditorHelper.HeaderFoldout("Sketch Images"))
            {
                GUILayout.BeginVertical("box");
                {
                    if (mCountImgs == 0)
                        EditorGUILayout.HelpBox("Select at least one Image Object to see how it work", MessageType.Info);
                    else
                        EditorGUILayout.LabelField("Image Count: ", mCountImgs.ToString());

                    mImgWidth = EditorHelper.FloatField(mImgWidth, "Width");
                    mImgHeight = EditorHelper.FloatField(mImgHeight, "Height");

                    mCountImgs = 0;
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
                                img.SketchByHeight(mImgHeight);
                        }
                    });
                    buttons.Add(new EditorButton()
                    {
                        label = "Sketch By Width",
                        onPressed = () =>
                        {
                            foreach (var img in allImages)
                                img.SketchByWidth(mImgWidth);
                        }
                    });
                    buttons.Add(new EditorButton()
                    {
                        label = "Sketch",
                        onPressed = () =>
                        {
                            foreach (var img in allImages)
                                img.Sketch(new Vector2(mImgWidth, mImgHeight));
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
                                Debug.Log($"{button.name} updated!", EditorGUIUtility.isProSkin ? Color.green : ColorHelper.DarkGreenX11);
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
                                Debug.Log($"{button.name} updated!", EditorGUIUtility.isProSkin ? Color.green : ColorHelper.DarkGreenX11);
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

                    EditorHelper.Seperator();
                    foreach (var texts in m_TMPTexts)
                        EditorGUILayout.LabelField($"{texts.Key.name} has {texts.Value.Count} texts.");
                }

                GUILayout.EndVertical();
            }
        }
        #endregion
        //===================================================================================================
        #region Math Utilities
        private DayOfWeek mNextDayOfWeeok;
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
                mNextDayOfWeeok = EditorHelper.DropdownListEnum<DayOfWeek>(mNextDayOfWeeok, "Day of week");
                var seconds = TimeHelper.GetSecondsTillDayOfWeek(mNextDayOfWeeok, DateTime.Now);
                EditorHelper.TextField(seconds.ToString(), "Seconds till day of week", 200);
                seconds = TimeHelper.GetSecondsTillEndDayOfWeek(mNextDayOfWeeok, DateTime.Now);
                EditorHelper.TextField(seconds.ToString(), "Seconds till end day of week", 200);
            }, Color.white, true);
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