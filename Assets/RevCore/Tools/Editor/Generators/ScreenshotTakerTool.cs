namespace RevCore.Tools.Editor
{
    internal sealed class ScreenshotTakerTool : RevCoreTool
    {
        public override string Name => "Screenshot Taker";
        public override string Category => "Generators";

        public override void OnOpen()
        {
            ScreenshotTakerWindow.Open();
        }
    }
}
