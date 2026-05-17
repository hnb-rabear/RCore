using UnityEngine;

namespace RevCore
{
    /// <summary>
    /// ScriptableObject that pairs a <see cref="JObjectData"/> payload with a string <see cref="key"/>
    /// and lifecycle callbacks. Subclass with a concrete data type to give designers an asset-based
    /// entry point for game state (player profile, settings, run state).
    /// </summary>
    /// <typeparam name="T">The concrete <see cref="JObjectData"/> subclass this model manages.</typeparam>
    public abstract class JObjectModel<T> : ScriptableObject, IJObjectModel where T : JObjectData
    {
        /// <summary>Unique PlayerPrefs key used by <see cref="Save"/> and <see cref="JObjectData.Load()"/>.</summary>
        [Tooltip("Unique key used to save and load the associated data.")]
        public string key;

        /// <summary>The data payload. Authored in the inspector for defaults; overwritten by Load on startup.</summary>
        [Tooltip("The serializable data object managed by this model.")]
        public T data;

        /// <inheritdoc />
        public JObjectData Data => data;
        /// <inheritdoc />
        public string Key => key;

        /// <summary>One-time initialization called from <see cref="JObjectModelCollection.PostLoad"/>. Subclass to wire up dependencies.</summary>
        public abstract void Init();
        /// <inheritdoc />
        public abstract void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds);
        /// <inheritdoc />
        public abstract void OnPostLoad(int utcNowTimestamp, int offlineSeconds);
        /// <inheritdoc />
        public abstract void OnUpdate(float deltaTime);
        /// <inheritdoc />
        public abstract void OnPreSave(int utcNowTimestamp);
        /// <inheritdoc />
        public abstract void OnRemoteConfigFetched();

        /// <summary>Persists <see cref="data"/>. Auto-assigns <see cref="JObjectData.key"/> from <see cref="key"/> when needed.</summary>
        public void Save()
        {
            if (data != null && string.IsNullOrEmpty(data.key) && !string.IsNullOrEmpty(key))
                data.key = key;
            data?.Save();
        }

        /// <summary>Replaces <see cref="data"/> with <paramref name="newData"/>, preserving the existing key.</summary>
        public void Import(T newData)
        {
            if (data != null)
                newData.key = data.key;
            data = newData;
        }

        /// <summary>Sugar for subclasses to publish an event, optionally debounced.</summary>
        protected void DispatchEvent<TEvent>(TEvent e, float debounceSeconds = 0) where TEvent : IEvent
        {
            if (debounceSeconds > 0)
                Timers.Debounce(e, debounceSeconds);
            else
                Events.Publish(e);
        }
    }
}
