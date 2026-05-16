using System;
using Newtonsoft.Json;
using UnityEngine;

namespace RevCore
{
    /// <summary>
    /// The persistable payload of a <see cref="JObjectModel{T}"/>. Implementations marshal to
    /// JSON and are keyed in PlayerPrefs.
    /// </summary>
    public interface IJObjectData
    {
        /// <summary>Writes the payload to PlayerPrefs under its key. <paramref name="minimizeSize"/> uses Newtonsoft with null/default omission.</summary>
        void Save(bool minimizeSize = false);

        /// <summary>Reads the payload from PlayerPrefs. Returns <c>false</c> when the key is absent.</summary>
        bool Load();

        /// <summary>Removes the payload's key from PlayerPrefs.</summary>
        void Delete();

        /// <summary>Serializes to JSON. <paramref name="minimizeSize"/> omits null/default fields.</summary>
        string ToJson(bool minimizeSize = false);
    }

    /// <summary>
    /// Base data class for any model persisted through <see cref="JObjectModel{T}"/>. Subclass
    /// this with your serializable fields. Override <see cref="Save"/>/<see cref="Load()"/> to
    /// retarget storage (e.g. to write to a server instead of PlayerPrefs).
    /// </summary>
    public abstract class JObjectData : IJObjectData
    {
        /// <summary>PlayerPrefs key. Assigned by the owning <see cref="JObjectModel{T}"/> on registration.</summary>
        [JsonIgnore] public string key { get; set; }

        /// <inheritdoc />
        public virtual void Save(bool minimizeSize = false)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError($"{GetType().Name}: Cannot save — key is null or empty.");
                return;
            }
            PlayerPrefs.SetString(key, ToJson(minimizeSize));
        }

        /// <inheritdoc />
        public virtual bool Load()
        {
            if (!PlayerPrefs.HasKey(key))
                return false;
            return Load(PlayerPrefs.GetString(key));
        }

        /// <summary>Deserializes from an explicit JSON string instead of PlayerPrefs. Used by <see cref="JObjectDB.Import"/>.</summary>
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

        /// <inheritdoc />
        public void Delete() => PlayerPrefs.DeleteKey(key);

        /// <inheritdoc />
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

        /// <summary>
        /// Helper for subclasses to publish an event on the global bus, optionally debounced. Useful
        /// when a setter wants to notify observers but the change may happen in a tight loop.
        /// </summary>
        protected void DispatchEvent<T>(T e, float debounceSeconds = 0) where T : IEvent
        {
            if (debounceSeconds > 0)
                Timers.Debounce(e, debounceSeconds);
            else
                Events.Publish(e);
        }
    }
}
