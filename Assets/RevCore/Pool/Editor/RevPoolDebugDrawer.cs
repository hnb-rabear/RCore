using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
    public static class RevPoolDebugDrawer
    {
        public static void Draw<T>(string label, RevPool<T> pool) where T : Component
        {
            if (pool == null)
            {
                EditorGUILayout.HelpBox($"{label}: null", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Active", pool.ActiveCount.ToString());
            EditorGUILayout.LabelField("Inactive", pool.InactiveCount.ToString());
            EditorGUILayout.ObjectField("Prefab", pool.Prefab, typeof(T), false);
            EditorGUILayout.ObjectField("Parent", pool.Parent, typeof(Transform), true);
        }
    }
}
