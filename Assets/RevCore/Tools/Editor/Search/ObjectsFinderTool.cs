namespace RevCore.Tools.Editor
{
    internal sealed class ObjectsFinderTool : RevCoreTool
    {
        public override string Name => "Objects Finder";
        public override string Category => "Search";

        public override void OnOpen()
        {
            ObjectsFinderWindow.Open();
        }
    }
}
