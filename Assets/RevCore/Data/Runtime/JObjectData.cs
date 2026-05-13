using System;
using Newtonsoft.Json;
using UnityEngine;

namespace RevCore
{
    public interface IJObjectData
    {
        void Save(bool minimizeSize = false);
        bool Load();
        void Delete();
        string ToJson(bool minimizeSize = false);
    }

    public abstract class JObjectData : IJObjectData
    {
        [JsonIgnore] public string key { get; set; }

        public virtual void Save(bool minimizeSize = false)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError($"{GetType().Name}: Cannot save — key is null or empty.");
                return;
            }
            PlayerPrefs.SetString(key, ToJson(minimizeSize));
        }

        public virtual bool Load()
        {
            if (!PlayerPrefs.HasKey(key))
                return false;
            return Load(PlayerPrefs.GetString(key));
        }

        public bool Load(string json)
        {
            if (string.IsNullOrEmpty(json))
                return false;
            try
            {
                JsonUtility.FromJsonOverwrite(json, this);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading JSON for key '{key}': {ex.Message}");
                return false;
            }
        }

        public void Delete() => PlayerPrefs.DeleteKey(key);

        public string ToJson(bool minimizeSize = false)
        {
            if (!minimizeSize)
                return JsonUtility.ToJson(this);

            return JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
        }

        protected void DispatchEvent<T>(T e, float debounceSeconds = 0) where T : IEvent
        {
            if (debounceSeconds > 0)
                Timers.Debounce(e, debounceSeconds);
            else
                Events.Publish(e);
        }
    }
}
