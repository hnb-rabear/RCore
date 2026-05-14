using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RevCore.Tools.Editor
{
    internal sealed class ToggleRaycastAllTool : RevCoreTool
    {
        public override string Name => "Toggle Raycast All";
        public override string Category => "UI Tools";
        public override bool IsQuickAction => true;

        public override void OnGUI()
        {
            EditorGUILayout.HelpBox("Select GameObjects in Hierarchy, then toggle Raycast Target on all Graphic children.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Enable Raycast Target"))
                SetRaycastTarget(true);
            if (GUILayout.Button("Disable Raycast Target"))
                SetRaycastTarget(false);
            EditorGUILayout.EndHorizontal();
        }

        private static void SetRaycastTarget(bool value)
        {
            int count = 0;
            foreach (GameObject go in Selection.gameObjects)
            {
                foreach (Graphic graphic in go.GetComponentsInChildren<Graphic>(true))
                {
                    Undo.RecordObject(graphic, "Toggle Raycast Target");
                    graphic.raycastTarget = value;
                    EditorUtility.SetDirty(graphic);
                    count++;
                }
            }
            Debug.Log($"Set raycastTarget={value} on {count} Graphic components.");
        }
    }
}
