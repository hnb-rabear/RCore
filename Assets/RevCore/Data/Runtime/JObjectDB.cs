using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace RevCore
{
    public static class JObjectDB
    {
        private const string CollectionsKey = "JObjectDB";

        public static Dictionary<string, JObjectData> collections = new();

        public static JObjectData GetCollection(string key)
        {
            collections.TryGetValue(key, out var col);
            return col;
        }

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

        public static List<string> GetCollectionKeys()
            => collections.Count == 0 ? GetSavedCollectionKeys() : collections.Keys.ToList();

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

        public static string ToJson() => JsonConvert.SerializeObject(GetAllData());

        public static void Save()
        {
            foreach (var pair in collections)
                pair.Value.Save();
        }

        public static void Reload()
        {
            foreach (var pair in collections)
                pair.Value.Load();
        }

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

        public static void DeleteAll()
        {
            foreach (string key in GetSavedCollectionKeys())
                PlayerPrefs.DeleteKey(key);
            PlayerPrefs.DeleteKey(CollectionsKey);
            collections.Clear();
        }

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

        public static void Restore(string filePath)
        {
            using var sr = new StreamReader(filePath);
            string content = sr.ReadToEnd();
            if (!string.IsNullOrEmpty(content))
                Import(content);
        }

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
    }
}
