using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RevCore.Tools.Editor
{
    internal sealed class FindByGuidTool : RevCoreTool
    {
        private string m_guid = string.Empty;
        private Object m_asset;

        public override string Name => "Find By GUID";
        public override string Category => "Search";
        public override bool IsQuickAction => true;

        public override void OnGUI()
        {
            m_guid = EditorGUILayout.TextField("GUID", m_guid);
            EditorGUILayout.ObjectField("Asset", m_asset, typeof(Object), false);
            if (GUILayout.Button("Find"))
            {
                string path = AssetDatabase.GUIDToAssetPath(m_guid);
                m_asset = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Object>(path);
                if (m_asset != null)
                {
                    Selection.activeObject = m_asset;
                    EditorGUIUtility.PingObject(m_asset);
                }
            }
        }
    }
}
