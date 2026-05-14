using UnityEditor;
using UnityEngine;

namespace RevCore.Tools.Editor
{
    internal sealed class ProjectExplorerTool : RevCoreTool
    {
        public override string Name => "Project Explorer";
        public override string Category => "Navigate";
        public override bool IsQuickAction => true;

        public override void OnGUI()
        {
            if (GUILayout.Button("Open Assets Folder"))
                EditorUtility.RevealInFinder(Application.dataPath);
            if (GUILayout.Button("Open StreamingAssets Folder"))
                EditorUtility.RevealInFinder(Application.streamingAssetsPath);
            if (GUILayout.Button("Open PersistentData Folder"))
                EditorUtility.RevealInFinder(Application.persistentDataPath);
            if (GUILayout.Button("Open Unity Editor Folder"))
                EditorUtility.RevealInFinder(System.IO.Path.GetDirectoryName(EditorApplication.applicationPath));
        }
    }
}
