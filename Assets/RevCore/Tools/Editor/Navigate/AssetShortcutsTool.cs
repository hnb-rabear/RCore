namespace RevCore.Tools.Editor
{
    internal sealed class AssetShortcutsTool : RevCoreTool
    {
        public override string Name => "Asset Shortcuts";
        public override string Category => "Navigate";

        public override void OnOpen()
        {
            AssetShortcutsWindow.Open();
        }
    }
}
