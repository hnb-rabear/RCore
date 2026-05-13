using UnityEngine;

namespace RevCore
{
    public abstract class JObjectModel<T> : ScriptableObject, IJObjectModel where T : JObjectData
    {
        [Tooltip("Unique key used to save and load the associated data.")]
        public string key;
        [Tooltip("The serializable data object managed by this model.")]
        public T data;

        public JObjectData Data => data;
        public string Key => key;

        public abstract void Init();
        public abstract void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds);
        public abstract void OnPostLoad(int utcNowTimestamp, int offlineSeconds);
        public abstract void OnUpdate(float deltaTime);
        public abstract void OnPreSave(int utcNowTimestamp);
        public abstract void OnRemoteConfigFetched();

        public void Save()
        {
            if (data != null && string.IsNullOrEmpty(data.key) && !string.IsNullOrEmpty(key))
                data.key = key;
            data?.Save();
        }

        public void Import(T newData)
        {
            if (data != null)
                newData.key = data.key;
            data = newData;
        }

        protected void DispatchEvent<TEvent>(TEvent e, float debounceSeconds = 0) where TEvent : IEvent
        {
            if (debounceSeconds > 0)
                Timers.Debounce(e, debounceSeconds);
            else
                Events.Publish(e);
        }
    }
}
