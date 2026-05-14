using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RevCore.Tools.Editor
{
    internal static class EditorGuiHelper
    {
        public static bool ButtonColor(string label, Color color, float width = 0)
        {
            Color oldColor = GUI.color;
            GUI.color = color;
            bool clicked = width > 0 ? GUILayout.Button(label, GUILayout.Width(width)) : GUILayout.Button(label);
            GUI.color = oldColor;
            return clicked;
        }

        public static bool HeaderFoldout(string label, string key = null)
        {
            string prefKey = string.IsNullOrEmpty(key) ? "RevCore.Tools.Foldout." + label : "RevCore.Tools.Foldout." + key;
            bool value = EditorPrefs.GetBool(prefKey, true);
            value = EditorGUILayout.Foldout(value, label, true, EditorStyles.foldoutHeader);
            EditorPrefs.SetBool(prefKey, value);
            return value;
        }

        public static void Separator()
        {
            EditorGUILayout.Space(4);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.25f, 0.25f, 0.25f));
            EditorGUILayout.Space(4);
        }

        public static void DragDropBox<T>(string label, Action<T[]> onDrop) where T : Object
        {
            Rect dropArea = GUILayoutUtility.GetRect(0, 48, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, label);

            Event evt = Event.current;
            if (!dropArea.Contains(evt.mousePosition))
                return;

            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                var results = new System.Collections.Generic.List<T>();
                foreach (Object obj in DragAndDrop.objectReferences)
                    if (obj is T typed)
                        results.Add(typed);
                onDrop(results.ToArray());
                evt.Use();
            }
        }
    }
}
