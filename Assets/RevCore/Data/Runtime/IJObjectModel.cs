namespace RevCore
{
    /// <summary>
    /// A <see cref="JObjectModel{T}"/> exposed as the lifecycle-aware <see cref="IJObjectHandler"/>
    /// plus access to the underlying data and key. Used by <see cref="JObjectModelCollection"/>
    /// to enumerate registered models without knowing their concrete data type.
    /// </summary>
    public interface IJObjectModel : IJObjectHandler
    {
        /// <summary>The model's data payload.</summary>
        JObjectData Data { get; }

        /// <summary>The PlayerPrefs key under which the data persists.</summary>
        string Key { get; }
    }
}
