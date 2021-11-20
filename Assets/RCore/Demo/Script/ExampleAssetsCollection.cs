using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RCore.Common;
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

    [CreateAssetMenu(fileName = "ExampleAssetsCollection", menuName = "RUtilities/Assets Collection Example")]
    public class ExampleAssetsCollection : ScriptableObject
    {
        public SpritesList icons;
        public List<GameObject> gameobjects;
        public PrefabsList prefabs;

#if UNITY_EDITOR
        [CustomEditor(typeof(ExampleAssetsCollection))]
        public class ExampleAssetsCollectionEditor : Editor
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
                        EditorHelper.ListObjects("Prefabs 1", ref mScript.gameobjects, null, true);
                        mScript.prefabs.DrawInEditor("Prefabs 2");
                        break;
                }
            }
        }
#endif
    }
}