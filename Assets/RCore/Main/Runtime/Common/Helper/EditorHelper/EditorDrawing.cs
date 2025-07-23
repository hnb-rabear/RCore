#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
    public static class EditorDrawing
    {
        private static readonly Dictionary<int, Color> BoxColours = new Dictionary<int, Color>();

        public static void BoxVerticalOpen(int id, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0)
        {
            var defaultColor = GUI.backgroundColor;
            if (!BoxColours.ContainsKey(id))
                BoxColours.Add(id, defaultColor);

            if (color != default)
                GUI.backgroundColor = color;
            
            var style = new GUIStyle(isBox ? "box" : "");
            if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
            if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
            EditorGUILayout.BeginVertical(style);
        }
        
        public static void BoxVerticalClose(int id)
        {
            GUI.backgroundColor = BoxColours[id];
            EditorGUILayout.EndVertical();
        }
        
        public static Rect BoxVertical(Action doSomething, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0)
        {
            var defaultColor = GUI.backgroundColor;
            if (color != default) GUI.backgroundColor = color;

            var style = new GUIStyle(isBox ? "box" : "");
            if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
            if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
            
            var rect = EditorGUILayout.BeginVertical(style);
            doSomething?.Invoke();
            EditorGUILayout.EndVertical();

            if (color != default) GUI.backgroundColor = defaultColor;
            return rect;
        }

        public static Rect BoxVertical(string pTitle, Action doSomething, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0)
        {
            Rect rect;
            EditorGUILayout.BeginVertical();
            if (!string.IsNullOrEmpty(pTitle)) DrawHeaderTitle(pTitle);
            rect = BoxVertical(doSomething, color, isBox, pFixedWidth, pFixedHeight);
            EditorGUILayout.EndVertical();
            return rect;
        }
        
        public static Rect BoxHorizontal(Action doSomething, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0)
        {
            var defaultColor = GUI.backgroundColor;
            if (color != default) GUI.backgroundColor = color;

            var style = new GUIStyle(isBox ? "box" : "");
            if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
            if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
            
            var rect = EditorGUILayout.BeginHorizontal(style);
            doSomething?.Invoke();
            EditorGUILayout.EndHorizontal();

            if (color != default) GUI.backgroundColor = defaultColor;
            return rect;
        }

        public static Rect BoxHorizontal(string pTitle, Action doSomething, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0)
        {
            Rect rect;
            EditorGUILayout.BeginVertical();
            if (!string.IsNullOrEmpty(pTitle)) DrawHeaderTitle(pTitle);
            rect = BoxHorizontal(doSomething, color, isBox, pFixedWidth, pFixedHeight);
            EditorGUILayout.EndVertical();
            return rect;
        }
        
        public static void DrawLine(float padding = 0)
        {
            if (padding > 0) EditorGUILayout.Space(padding);
            
            var lineColor = EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f) : new Color(0.6f, 0.6f, 0.6f);
            var originalColor = GUI.color;
            GUI.color = lineColor;
            var lineRect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(lineRect, lineColor);
            GUI.color = originalColor;

            if (padding > 0) EditorGUILayout.Space(padding);
        }

        public static void Separator(string label = null, Color labelColor = default)
        {
            if (string.IsNullOrEmpty(label))
            {
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            }
            else
            {
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.Label("", GUI.skin.horizontalSlider);

                var boldStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                var defaultContentColor = GUI.contentColor;
                if (labelColor != default) GUI.contentColor = labelColor;
                
                GUILayout.Label(label, boldStyle, GUILayout.ExpandWidth(false));
                GUI.contentColor = defaultContentColor;

                GUILayout.Label("", GUI.skin.horizontalSlider);
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
            }
        }
        
        public static void SeparatorBox()
        {
            GUILayout.Space(10);
            if (Event.current.type == EventType.Repaint)
            {
                var tex = EditorGUIUtility.whiteTexture;
                var rect = GUILayoutUtility.GetLastRect();
                GUI.color = new Color(0f, 0f, 0f, 0.25f);
                GUI.DrawTexture(new Rect(0f, rect.yMin + 6f, Screen.width, 4f), tex);
                GUI.DrawTexture(new Rect(0f, rect.yMin + 6f, Screen.width, 1f), tex);
                GUI.DrawTexture(new Rect(0f, rect.yMin + 9f, Screen.width, 1f), tex);
                GUI.color = Color.white;
            }
        }
        
        public static void DrawTextureIcon(Texture pTexture, Vector2 pSize)
        {
            var rect = EditorGUILayout.GetControlRect(GUILayout.Width(pSize.x), GUILayout.Height(pSize.y));
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            var textureToShow = pTexture != null ? pTexture : EditorGUIUtility.FindTexture("console.warnicon");
            GUI.DrawTexture(rect, textureToShow, ScaleMode.ScaleToFit);
        }
        
        public static void DrawHeaderTitle(string pHeader)
        {
            var prevColor = GUI.color;
            var boxStyle = new GUIStyle(EditorStyles.toolbar) { fixedHeight = 0, padding = new RectOffset(5, 5, 5, 5) };
            var titleStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            GUI.color = new Color(0.5f, 0.5f, 0.5f);
            EditorGUILayout.BeginHorizontal(boxStyle, GUILayout.Height(20));
            GUI.color = prevColor;
            EditorGUILayout.LabelField(pHeader, titleStyle, GUILayout.Height(20));
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif