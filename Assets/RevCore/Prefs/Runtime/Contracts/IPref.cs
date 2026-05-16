namespace RevCore
{
    /// <summary>
    /// A single key in <see cref="UnityEngine.PlayerPrefs"/> (or another configured backend), with
    /// in-memory caching to avoid repeated reads. Implementations expose a typed <c>Value</c> property.
    /// </summary>
    public interface IPref
    {
        /// <summary>The PlayerPrefs key.</summary>
        string Key { get; }

        /// <summary>True when the cached value has been written since the last <see cref="SaveChange"/>.</summary>
        bool IsChanged { get; }

        /// <summary>Writes the cached value to the backend and clears the changed flag.</summary>
        void SaveChange();

        /// <summary>Removes the key from the backend and resets the cache to the default.</summary>
        void Delete();
    }
}
