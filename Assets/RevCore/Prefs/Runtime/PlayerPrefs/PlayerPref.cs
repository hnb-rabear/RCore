using UnityEngine;

namespace RevCore
{
    /// <summary>
    /// Base class for typed PlayerPrefs entries. Auto-registers each instance with
    /// <see cref="PlayerPrefContainer"/> so they can be bulk-saved or deleted. Subclasses
    /// implement the typed cache and <see cref="SaveChange"/> serialization.
    /// </summary>
    public abstract class PlayerPref : IPref
    {
        /// <summary>Subclass-managed dirty flag. Set when the cached value diverges from the saved value.</summary>
        protected bool changed;

        /// <inheritdoc />
        public string Key { get; }

        /// <inheritdoc />
        public bool IsChanged => changed;

        /// <summary>Stores the key and registers <c>this</c> with the global container.</summary>
        protected PlayerPref(string key)
        {
            Key = key;
            PlayerPrefContainer.Register(this);
        }

        /// <inheritdoc />
        public void Delete()
        {
            PlayerPrefs.DeleteKey(Key);
            changed = false;
        }

        /// <inheritdoc />
        public abstract void SaveChange();
    }
}
