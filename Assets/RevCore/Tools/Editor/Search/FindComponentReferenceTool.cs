namespace RevCore.Tools.Editor
{
    internal sealed class FindComponentReferenceTool : RevCoreTool
    {
        public override string Name => "Find Component Reference";
        public override string Category => "Search";

        public override void OnOpen()
        {
            FindComponentReferenceWindow.Open();
        }
    }
}
