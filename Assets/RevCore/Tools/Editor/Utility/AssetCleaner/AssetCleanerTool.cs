namespace RevCore.Tools.Editor
{
    internal sealed class AssetCleanerTool : RevCoreTool
    {
        public override string Name => "Asset Cleaner";
        public override string Category => "Utility";

        public override void OnOpen()
        {
            AssetCleanerWindow.Open();
        }
    }
}
