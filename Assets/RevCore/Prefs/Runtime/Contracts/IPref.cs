namespace RevCore
{
    public interface IPref
    {
        string Key { get; }
        bool IsChanged { get; }
        void SaveChange();
        void Delete();
    }
}
