namespace RevCore.Tools.Editor
{
    public abstract class RevCoreTool
    {
        public abstract string Name { get; }
        public abstract string Category { get; }
        public virtual bool IsQuickAction => false;

        public virtual void OnGUI()
        {
        }

        public virtual void OnOpen()
        {
        }
    }
}
