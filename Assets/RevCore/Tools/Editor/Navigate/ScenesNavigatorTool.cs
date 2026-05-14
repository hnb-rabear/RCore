namespace RevCore.Tools.Editor
{
    internal sealed class ScenesNavigatorTool : RevCoreTool
    {
        public override string Name => "Scenes Navigator";
        public override string Category => "Navigate";

        public override void OnOpen()
        {
            ScenesNavigatorWindow.Open();
        }
    }
}
