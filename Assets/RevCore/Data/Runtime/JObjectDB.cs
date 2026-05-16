using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace RevCore
{
    /// <summary>
    /// Static registry of all <see cref="JObjectData"/> collections active in the process. Provides
    /// import / export / backup / delete operations that span every registered collection. Used
    /// internally by <see cref="JObjectModelCollection"/>; consumers typically interact through it
    /// rather than calling these methods directly.
    /// </summary>
    public static class JObjectDB
    {
        private const string CollectionsKey = "JObjectDB";

        /// <summary>
        /// Direct access to the in-memory collection map. Public for backward compatibility only;
        /// will become private in v1.0. Use <see cref="GetCollection"/>, <see cref="CreateCollection{T}"/>,
        /// <see cref="GetCollectionKeys"/>, or <see cref="GetAllData"/> instead. Direct mutation of this
        /// dictionary bypasses key persistence (<c>SaveCollectionKey</c>) and will silently lose data
        /// on the next <see cref="Reload"/>.
        /// </summary>
        [Obsolete("Direct field access will be made private in v1.0. Use GetCollection, CreateCollection, GetCollectionKeys, or GetAllData instead.", error: false)]
        public static Dictionary<string, JObjectData> collections = new();

#pragma warning disable CS0618 // Self-references to the obsolete field below are intentional during the deprecation window.

        /// <summary>Returns the collection registered under <paramref name="key"/>, or <c>null</c> when absent.</summary>
        public static JObjectData GetCollection(string key)
        {
            collections.TryGetValue(key, out var col);
            return col;
        }

        /// <summary>
        /// Creates a new collection of type <typeparamref name="T"/> under <paramref name="key"/>,
        /// loads it from PlayerPrefs (if present), or falls back to <paramref name="defaultVal"/>.
        /// Logs an error and overwrites if a collection with the same key already exists.
        /// </summary>
        public static T CreateCollection<T>(string key, T defaultVal = null) where T : JObjectData, new()
        {
            if (collections.ContainsKey(key))
                Debug.LogError($"JObjectDB: Overwriting existing collection '{key}'.");

            var col = new T { key = key };
            collections[key] = col;

            if (!col.Load() && defaultVal != null)
            {
                string json = defaultVal.ToJson();
                JsonUtility.FromJsonOverwrite(json, col);
                col.key = key;
            }

            SaveCollectionKey(key);
            return col;
        }

        /// <summary>Returns every registered key. Falls back to keys persisted in PlayerPrefs when nothing is registered (e.g. before <see cref="JObjectDBManager{T}.Init"/>).</summary>
        public static List<string> GetCollectionKeys()
            => collections.Count == 0 ? GetSavedCollectionKeys() : collections.Keys.ToList();

        /// <summary>Returns <c>key → json</c> for every registered collection. Allocates a fresh dictionary on each call.</summary>
        public static Dictionary<string, string> GetAllData()
        {
            var dict = new Dictionary<string, string>();
            if (collections.Count == 0)
            {
                foreach (string key in GetCollectionKeys())
                {
                    string data = PlayerPrefs.GetString(key);
                    if (!string.IsNullOrEmpty(data))
                        dict[key] = data;
                }
                return dict;
            }
            foreach (var pair in collections)
                dict[pair.Key] = pair.Value.ToJson();
            return dict;
        }

        /// <summary>Like <see cref="GetAllData"/> but values are the deserialized object form (useful for direct field access).</summary>
        public static Dictionary<string, object> GetAllDataObjects()
        {
            var dict = new Dictionary<string, object>();
            if (collections.Count == 0)
            {
                foreach (string key in GetCollectionKeys())
                {
                    string json = PlayerPrefs.GetString(key);
                    if (!string.IsNullOrEmpty(json))
                        dict[key] = JsonConvert.DeserializeObject<object>(json);
                }
                return dict;
            }
            foreach (var pair in collections)
                dict[pair.Key] = pair.Value;
            return dict;
        }

        /// <summary>JSON-serializes the result of <see cref="GetAllData"/>. The format consumed by <see cref="Import"/>.</summary>
        public static string ToJson() => JsonConvert.SerializeObject(GetAllData());

        /// <summary>Calls <see cref="JObjectData.Save"/> on every registered collection.</summary>
        public static void Save()
        {
            foreach (var pair in collections)
                pair.Value.Save();
        }

        /// <summary>Calls <see cref="JObjectData.Load()"/> on every registered collection — overwrites in-memory state with the latest persisted version.</summary>
        public static void Reload()
        {
            foreach (var pair in collections)
                pair.Value.Load();
        }

        /// <summary>Removes a collection both from the in-memory registry and from PlayerPrefs (data + key list).</summary>
        public static void Delete(string key)
        {
            var keys = GetSavedCollectionKeys();
            if (keys.Remove(key))
            {
                PlayerPrefs.SetString(CollectionsKey, JsonConvert.SerializeObject(keys));
                PlayerPrefs.DeleteKey(key);
            }
            collections.Remove(key);
        }

        /// <summary>Deletes every persisted key (data + key list) and clears the in-memory registry. Factory-reset.</summary>
        public static void DeleteAll()
        {
            foreach (string key in GetSavedCollectionKeys())
                PlayerPrefs.DeleteKey(key);
            PlayerPrefs.DeleteKey(CollectionsKey);
            collections.Clear();
        }

        /// <summary>
        /// Replaces persisted collection data from a JSON map (the format produced by <see cref="ToJson"/>).
        /// Updates both PlayerPrefs and the in-memory collections, and appends any new keys to the registry list.
        /// </summary>
        public static void Import(string jsonData)
        {
            var pairs = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
            if (pairs == null) return;
            foreach (var kv in pairs)
            {
                PlayerPrefs.SetString(kv.Key, kv.Value);
                GetCollection(kv.Key)?.Load(kv.Value);
            }
            var keys = GetSavedCollectionKeys();
            bool changed = false;
            foreach (string k in pairs.Keys)
                if (!keys.Contains(k)) { keys.Add(k); changed = true; }
            if (changed)
                PlayerPrefs.SetString(CollectionsKey, JsonConvert.SerializeObject(keys));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Writes the current state of every registered collection to a JSON file. Without
        /// <paramref name="filePath"/>, picks a timestamped path under the project's Saves folder
        /// (editor) or persistent data path (player). <paramref name="openDirectory"/> reveals the
        /// containing folder in the OS file browser (editor only).
        /// </summary>
        public static void Backup(string filePath = null, bool openDirectory = false)
        {
            string path = filePath ?? GetDefaultBackupPath();
            File.WriteAllText(path, ToJson());
#if UNITY_EDITOR
            Debug.Log($"Backup saved: {path}");
            if (openDirectory)
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path.GetDirectoryName(path)));
#endif
        }

        /// <summary>Reads a backup file written by <see cref="Backup"/> and feeds it through <see cref="Import"/>.</summary>
        public static void Restore(string filePath)
        {
            using var sr = new StreamReader(filePath);
            string content = sr.ReadToEnd();
            if (!string.IsNullOrEmpty(content))
                Import(content);
        }

        /// <summary>Logs the result of <see cref="ToJson"/> and copies it to the system clipboard. Editor diagnostic.</summary>
        public static void CopyAllData()
        {
            string json = ToJson();
            Debug.Log(json);
            GUIUtility.systemCopyBuffer = json;
        }

        private static void SaveCollectionKey(string key)
        {
            var keys = GetSavedCollectionKeys();
            if (!keys.Contains(key))
            {
                keys.Add(key);
                PlayerPrefs.SetString(CollectionsKey, JsonConvert.SerializeObject(keys));
            }
        }

        private static List<string> GetSavedCollectionKeys()
        {
            string raw = PlayerPrefs.GetString(CollectionsKey);
            if (string.IsNullOrEmpty(raw))
                return new List<string>();
            return JsonConvert.DeserializeObject<List<string>>(raw) ?? new List<string>();
        }

        private static string GetDefaultBackupPath()
        {
            var t = DateTime.Now;
            string id = Application.identifier;
            var parts = id.Split('.');
            string name = parts.Length > 0 ? $"{parts[^1]}_" : "";
            name += $"{t.Year % 100}{t.Month:00}{t.Day:00}_{t.Hour:00}h{t.Minute:00}";
#if UNITY_EDITOR
            string dir = Application.dataPath.Replace("Assets", "Saves");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, name + ".json");
#else
            return Path.Combine(Application.persistentDataPath, name + ".json");
#endif
        }
#pragma warning restore CS0618
    }
}
