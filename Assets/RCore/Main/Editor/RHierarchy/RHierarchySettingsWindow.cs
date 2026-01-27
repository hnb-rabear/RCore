using UnityEditor;
using UnityEngine;

namespace RCore.Editor.RHierarchy
{
    public class RHierarchySettingsWindow : EditorWindow
    {
        [MenuItem("RCore/Tools/RHierarchy Settings")]
        public static void ShowWindow()
        {
            GetWindow<RHierarchySettingsWindow>("RHierarchy Settings");
        }

        private UnityEditorInternal.ReorderableList m_ReorderableList;

        private void OnGUI()
        {
            GUILayout.Label("RHierarchy Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            RHierarchySettings.IsEnable = EditorGUILayout.Toggle("Enable RHierarchy", RHierarchySettings.IsEnable);
            
            if (!RHierarchySettings.IsEnable) return;

            RHierarchySettings.IsMonoBehaviourIconEnabled = EditorGUILayout.Toggle("MonoBehaviour Icon", RHierarchySettings.IsMonoBehaviourIconEnabled);
            RHierarchySettings.IsSeparatorEnabled = EditorGUILayout.Toggle("Separator", RHierarchySettings.IsSeparatorEnabled);
            RHierarchySettings.IsVisibilityEnabled = EditorGUILayout.Toggle("Visibility", RHierarchySettings.IsVisibilityEnabled);
            RHierarchySettings.IsTagEnabled = EditorGUILayout.Toggle("Tag", RHierarchySettings.IsTagEnabled);
            RHierarchySettings.IsLayerEnabled = EditorGUILayout.Toggle("Layer", RHierarchySettings.IsLayerEnabled);
            RHierarchySettings.IsStaticEnabled = EditorGUILayout.Toggle("Static", RHierarchySettings.IsStaticEnabled);
            RHierarchySettings.IsChildrenCountEnabled = EditorGUILayout.Toggle("Children Count", RHierarchySettings.IsChildrenCountEnabled);
            RHierarchySettings.IsComponentsEnabled = EditorGUILayout.Toggle("Components", RHierarchySettings.IsComponentsEnabled);
            RHierarchySettings.IsVerticesEnabled = EditorGUILayout.Toggle("Vertices Count", RHierarchySettings.IsVerticesEnabled);

            EditorGUILayout.Space();
            GUILayout.Label("Component Order", EditorStyles.boldLabel);
            var order = RHierarchySettings.ComponentOrder;
            if (m_ReorderableList == null)
            {
                m_ReorderableList = new UnityEditorInternal.ReorderableList(order, typeof(RHierarchySettings.RComponentType), true, false, false, false);
                m_ReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    var element = order[index];
                    EditorGUI.LabelField(rect, ObjectNames.NicifyVariableName(element.ToString()));
                };
                m_ReorderableList.onReorderCallback = (UnityEditorInternal.ReorderableList list) =>
                {
                    RHierarchySettings.ComponentOrder = order;
                };
            }
            m_ReorderableList.list = order;
            m_ReorderableList.DoLayoutList();

            EditorGUILayout.Space();
            GUILayout.Label("Appearance", EditorStyles.boldLabel);
            RHierarchySettings.ActiveColor = EditorGUILayout.ColorField("Active Color", RHierarchySettings.ActiveColor);
            RHierarchySettings.InactiveColor = EditorGUILayout.ColorField("Inactive Color", RHierarchySettings.InactiveColor);
            
            if (GUI.changed)
            {
                EditorApplication.RepaintHierarchyWindow();
            }
        }
    }
}
