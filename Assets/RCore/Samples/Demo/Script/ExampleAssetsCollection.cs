using System.Collections.Generic;
using UnityEngine;
using RCore.Common;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Demo
{
    [System.Serializable]
    public class PrefabsList : AssetsList<GameObject>
    {
        public override bool showBox => false;
        public override bool @readonly => false;
    }

    [CreateAssetMenu(fileName = "ExampleAssetsCollection", menuName = "RCore/Assets Collection Example")]
    public class ExampleAssetsCollection : ScriptableObject
    {
        public SpritesList icons;
        [FormerlySerializedAs("gameobjects")] public List<GameObject> gameObjects;
        public PrefabsList prefabs;

#if UNITY_EDITOR
        [CustomEditor(typeof(ExampleAssetsCollection))]
        public class ExampleAssetsCollectionEditor : UnityEditor.Editor
        {
            private ExampleAssetsCollection mScript;

            private void OnEnable()
            {
                mScript = target as ExampleAssetsCollection;
            }

            public override void OnInspectorGUI()
            {
                var currentTab = EditorHelper.Tabs(mScript.name, "Default", "Custom");
                switch (currentTab)
                {
                    case "Default":
                        base.OnInspectorGUI();
                        break;
                    case "Custom":
                        mScript.icons.DrawInEditor("Icons");
                        EditorHelper.ListObjects("Prefabs 1", ref mScript.gameObjects, null, true);
                        mScript.prefabs.DrawInEditor("Prefabs 2");
                        break;
                }
            }
        }
#endif
    }
}