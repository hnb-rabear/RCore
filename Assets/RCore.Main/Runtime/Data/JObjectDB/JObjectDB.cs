using Newtonsoft.Json;
using RCore.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace RCore.Data.JObject
{
	public static class JObjectDB
	{
		/// <summary>
		/// Used to save Key or Data Saver, which lately used for indexing data list
		/// </summary>
		private static readonly string COLLECTIONS = "JObjectDB";
		
		public static Dictionary<string, JObjectCollection> collections = new Dictionary<string, JObjectCollection>();

		public static JObjectCollection GetCollection(string key)
		{
			if (collections.TryGetValue(key, out var collection))
				return collection;
			return null;
		}
		
		public static T CreateCollection<T>(string key, T defaultVal = null) where T : JObjectCollection, new()
		{
			if (collections.ContainsKey(key))
			{
				Debug.LogError($"Collection {key} Existed");
				return null;
			}

			var collection = new T();
			collection.key = key;
			if (!collection.Load() && defaultVal != null)
			{
				string json = defaultVal.ToJson();
				collection = JsonUtility.FromJson<T>(json);
				collection.key = key;
			}
			SaveCollectionKey(key);
			collections.Add(key, collection);

			return collection;
		}
		
		private static void SaveCollectionKey(string pKey)
		{
			var keys = GetSavedCollectionKeys();
			foreach (string key in keys)
				if (key == pKey)
					return;
			keys.Add(pKey);
			PlayerPrefs.SetString(COLLECTIONS, JsonHelper.ToJson(keys));
		}

		private static void SaveCollectionKeys(string[] pKeys)
		{
			var keys = GetSavedCollectionKeys();
			foreach (string pKey in pKeys)
			{
				foreach (string key in keys)
					if (key == pKey)
						break;
				keys.Add(pKey);
			}
			PlayerPrefs.SetString(COLLECTIONS, JsonHelper.ToJson(keys));
		}

		private static List<string> GetSavedCollectionKeys()
		{
			string keysStr = PlayerPrefs.GetString(COLLECTIONS);
			if (string.IsNullOrEmpty(keysStr))
				return new List<string>();
			var keys = JsonHelper.ToList<string>(keysStr);
			keys ??= new List<string>();
			return keys;
		}
		
		public static List<string> GetCollectionKeys()
		{
			return collections.Count == 0 ? GetSavedCollectionKeys() : collections.Keys.ToList();
		}
		
		/// <summary>
		/// Get data from all collections
		/// </summary>
		public static Dictionary<string, string> GetAllData()
		{
			var dict = new Dictionary<string, string>();
			if (collections.Count == 0)
			{
				var keys = GetCollectionKeys();
				foreach (string key in keys)
				{
					var data = PlayerPrefs.GetString(key);
					if (!string.IsNullOrEmpty(data))
						dict.Add(key, data);
				}
				return dict;
			}
			foreach (var pair in collections)
				dict.Add(pair.Key, pair.Value.ToJson());
			return dict;
		}

		public static void DeleteAll()
		{
			var saverKeys = GetSavedCollectionKeys();
			for (int i = 0; i < saverKeys.Count; i++)
				PlayerPrefs.DeleteKey(saverKeys[i]);
			collections.Clear();
		}

		public static void Delete(string key)
		{
			var keys = GetSavedCollectionKeys();
			if (keys.Remove(key))
			{
				PlayerPrefs.SetString(COLLECTIONS, JsonHelper.ToJson(keys));
				PlayerPrefs.DeleteKey(key);
			}
			collections.Remove(key);
		}

		public static void Import(string jsonData)
		{
			var collectionsJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
			foreach (var keyValue in collectionsJson)
			{
				PlayerPrefs.SetString(keyValue.Key, keyValue.Value);
				var collection = GetCollection(keyValue.Key);
				collection?.Load(keyValue.Value);
#if UNITY_EDITOR
				Debug.Log($"Import {keyValue.Key}\n{keyValue.Value}");
#endif
			}
			SaveCollectionKeys(collectionsJson.Keys.ToArray());
			PlayerPrefs.Save();
		}

		public static void CopyAllData()
		{
			var json = JsonConvert.SerializeObject(GetAllData());
			Debug.Log(json);
			UniClipboard.SetText(json);
		}

		public static void Backup(string customFileName = null, bool openDirectory = false)
		{
			var time = DateTime.Now;
			string identifier = Application.identifier;
			var idParts = identifier.Split('.');
			string path;
			if (string.IsNullOrEmpty(customFileName))
			{
				string fileName = string.IsNullOrEmpty(identifier) ? "" : $"{idParts[idParts.Length - 1]}_";
				fileName += $"{time.Year % 100}{time.Month:00}{time.Day:00}_{time.Hour:00}h{time.Minute:00}";
				path = GetFilePath(fileName);
			}
			else
				path = GetFilePath(customFileName);
			string jsonData = JsonConvert.SerializeObject(GetAllData());
			File.WriteAllText(path, jsonData);
#if UNITY_EDITOR
			Debug.Log($"Backup data at path {path}");
			if (openDirectory)
				Process.Start(new ProcessStartInfo(Path.GetDirectoryName(path)));
#endif
		}

		public static void Restore(string filePath)
		{
			using (var sw = new StreamReader(filePath))
			{
				var content = sw.ReadToEnd();
				if (!string.IsNullOrEmpty(content))
					Import(content);
			}
		}

		/// <summary>
		/// Discard all changes, back to last data save
		/// </summary>
		public static void Reload()
		{
			foreach (var pair in collections)
				pair.Value.Load();
		}

		public static void Save()
		{
			foreach (var pair in collections)
				pair.Value.Save();
		}
		
		private static string GetFilePath(string fileName)
		{
#if UNITY_EDITOR
			string directoryPath = Application.dataPath.Replace("Assets", "Saves");
			if (!Directory.Exists(directoryPath))
				Directory.CreateDirectory(directoryPath);
			return Path.Combine(directoryPath, fileName + ".json");
#endif
			return Path.Combine(Application.persistentDataPath, fileName + ".json");
		}
	}

	public static class JObjectDBHelper
	{
		public static Dictionary<string, string> ToDictionary(this List<JObjectCollection> collections)
		{
			var list = new Dictionary<string, string>();
			foreach (var pair in collections)
				list.Add(pair.key, pair.ToJson());
			return list;
		}
		public static string ToJson(this List<JObjectCollection> collections)
		{
			var list = collections.ToDictionary();
			return JsonConvert.SerializeObject(list);
		}
		public static void Import(this List<JObjectCollection> collections, string jsonData)
		{
			var keyValuePairs = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
			if (keyValuePairs != null)
				foreach (var pair in keyValuePairs)
					PlayerPrefs.SetString(pair.Key, pair.Value);
		}
	}
}