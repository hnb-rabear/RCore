#if ADDRESSABLES
namespace RevCore.Tools.Editor
{
    internal sealed class AddressableGroupsColorizerTool : RevCoreTool
    {
        public override string Name => "Addressable Groups Colorizer";
        public override string Category => "Addressables";
        public override bool IsQuickAction => true;

        public override void OnGUI()
        {
            UnityEngine.GUILayout.Label("Background tool — colors Addressable groups in Project window.");
            if (UnityEngine.GUILayout.Button("Open Settings"))
                AddressableGroupsColorizer.OpenSettings();
        }

        private static void OpenSettings()
        {
            AddressableGroupsColorizer.OpenSettings();
        }
    }
}
#endif
