namespace RevCore
{
    public interface IJObjectModel : IJObjectHandler
    {
        JObjectData Data { get; }
        string Key { get; }
    }
}
