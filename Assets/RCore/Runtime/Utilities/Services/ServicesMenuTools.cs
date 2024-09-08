#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RCore.Service
{
    public class ServicesMenuTools : UnityEditor.Editor
    {
        [MenuItem("RCore/Services/Add Firebase Manager")]
        private static void AddFirebaseManager()
        {
            var manager = FindObjectOfType<RFirebaseManager>();
            if (manager == null)
            {
                var obj = new GameObject("RFirebaseManager");
                obj.AddComponent<RFirebaseManager>();
            }
        }
    }
}
#endif