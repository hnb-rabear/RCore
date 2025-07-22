/**
 * Author HNB-RaBear - 2024
 **/

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug; // Explicitly use UnityEngine.Debug

namespace RCore.Data.JObject
{
	/// <summary>
	/// A static database manager that handles the storage and retrieval of data collections.
	/// It uses PlayerPrefs for persistence and provides an in-memory cache for active data objects.
	/// This class facilitates creating, saving, loading, and managing multiple distinct data sets (collections).
	/// </summary>
	public static class JObjectDB
	{
		/// <summary>
		/// The PlayerPrefs key for the master index, which stores a list of all other collection keys.
		/// </summary>
		private static readonly string COLLECTIONS = "JObjectDB";
		
		/// <summary>
		/// An in-memory cache of all currently loaded data collections, keyed by their unique name.
		/// </summary>
		public static Dictionary<string, JObjectData> collections = new Dictionary<string, JObjectData>();

		/// <summary>
		/// Retrieves a loaded data collection from the in-memory cache.
		/// </summary>
		/// <param name="key">The unique key of the collection to retrieve.</param>
		/// <returns>The JObjectData instance if found in memory, otherwise null.</returns>
		public static JObjectData GetCollection(string key)
		{
			collections.TryGetValue(key, out var collection);
			return collection;
		}
		
		/// <summary>
		/// Creates, loads, and returns a data collection of a specific type.
		/// If the collection does not exist in memory, a new one is created.
		/// It then attempts to load data from PlayerPrefs. If loading fails and a default value is provided,
		/// the collection is initialized with the default data.
		/// </summary>
		/// <typeparam name="T">The type of the collection, which must inherit from JObjectData.</typeparam>
		/// <param name="key">The unique key for the collection.</param>
		/// <param name="defaultVal">An optional default object to populate the collection with if no saved data is found.</param>
		/// <returns>The created or loaded collection instance.</returns>
		public static T CreateCollection<T>(string key, T defaultVal = null) where T : JObjectData, new()
		{
			if (!collections.TryGetValue(key, out var collection))
			{
				collection = new T();
				collections[key] = collection;
			}
			else
			{
				Debug.LogWarning($"A collection with key '{key}' already exists in memory. Overwriting is not recommended.");
			}

			collection.key = key;

			// If loading from PlayerPrefs fails and a default value is available, use it.
			if (!collection.Load() && defaultVal != null)
			{
				string json = defaultVal.ToJson();
				collection = JsonUtility.FromJson<T>(json);
				collection.key = key;
				collections[key] = collection;
			}

			SaveCollectionKey(key);
			return collection as T;
		}
		
		/// <summary>
		/// Adds a collection key to the master index in PlayerPrefs, ensuring no duplicates.
		/// </summary>
		private static void SaveCollectionKey(string pKey)
		{
			var keys = GetSavedCollectionKeys();
			if (!keys.Contains(pKey))
			{
				keys.Add(pKey);
				PlayerPrefs.SetString(COLLECTIONS, JsonHelper.ToJson(keys));
			}
		}

		/// <summary>
		/// Adds multiple collection keys to the master index in PlayerPrefs.
		/// </summary>
		private static void SaveCollectionKeys(string[] pKeys)
		{
			var keys = GetSavedCollectionKeys();
			foreach (string pKey in pKeys)
			{
				if (!keys.Contains(pKey))
					keys.Add(pKey);
			}
			PlayerPrefs.SetString(COLLECTIONS, JsonHelper.ToJson(keys));
		}

		/// <summary>
		/// Retrieves the list of all saved collection keys from the master index in PlayerPrefs.
		/// </summary>
		private static List<string> GetSavedCollectionKeys()
		{
			string keysStr = PlayerPrefs.GetString(COLLECTIONS, "[]");
			return JsonHelper.ToList<string>(keysStr) ?? new List<string>();
		}
		
		/// <summary>
		/// Gets a list of all known collection keys. It prioritizes the in-memory cache but falls back to PlayerPrefs if the cache is empty.
		/// </summary>
		/// <returns>A list of collection keys.</returns>
		public static List<string> GetCollectionKeys()
		{
			return collections.Count == 0 ? GetSavedCollectionKeys() : collections.Keys.ToList();
		}
		
		/// <summary>
		/// Retrieves all data from all known collections as a dictionary of raw JSON strings.
		/// </summary>
		/// <returns>A dictionary where the key is the collection name and the value is the collection's data as a JSON string.</returns>
		public static Dictionary<string, string> GetAllData()
		{
			var dict = new Dictionary<string, string>();
			var keys = GetCollectionKeys();
			foreach (string key in keys)
			{
				// Prioritize in-memory data, then fall back to PlayerPrefs.
				if (collections.TryGetValue(key, out JObjectData collection))
				{
					dict.Add(key, collection.ToJson());
				}
				else
				{
					var data = PlayerPrefs.GetString(key);
					if (!string.IsNullOrEmpty(data))
						dict.Add(key, data);
				}
			}
			return dict;
		}
		
		/// <summary>
		/// Retrieves all data from all known collections as a dictionary of deserialized objects.
		/// </summary>
		/// <returns>A dictionary where the key is the collection name and the value is the deserialized JObjectData instance.</returns>
		public static Dictionary<string, object> GetAllDataObjects()
		{
			var dict = new Dictionary<string, object>();
			var keys = GetCollectionKeys();
			foreach (string key in keys)
			{
				// Prioritize in-memory objects, then fall back to deserializing from PlayerPrefs.
				if (collections.TryGetValue(key, out JObjectData collection))
				{
					dict.Add(key, collection);
				}
				else
				{
					string json = PlayerPrefs.GetString(key);
					if (!string.IsNullOrEmpty(json))
						dict.Add(key, JsonConvert.DeserializeObject<object>(json));
				}
			}
			return dict;
		}

		/// <summary>
		/// Serializes the entire database (all collections) into a single JSON string.
		/// </summary>
		/// <returns>A JSON string representing all data.</returns>
		public static string ToJson()
		{
			var dict = GetAllData();
			return JsonConvert.SerializeObject(dict, Formatting.Indented);
		}

		/// <summary>
		/// Deletes all data associated with this database from PlayerPrefs and clears the in-memory cache.
		/// </summary>
		public static void DeleteAll()
		{
			var saverKeys = GetSavedCollectionKeys();
			for (int i = 0; i < saverKeys.Count; i++)
				PlayerPrefs.DeleteKey(saverKeys[i]);
			PlayerPrefs.DeleteKey(COLLECTIONS);
			collections.Clear();
			Debug.Log("JObjectDB: All data deleted.");
		}

		/// <summary>
		/// Deletes a single collection from PlayerPrefs, the master index, and the in-memory cache.
		/// </summary>
		/// <param name="key">The key of the collection to delete.</param>
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

		/// <summary>
		/// Imports data from a JSON string, overwriting any existing data in PlayerPrefs and reloading in-memory collections.
		/// </summary>
		/// <param name="jsonData">A JSON string representing the entire database, typically created by `ToJson()`.</param>
		public static void Import(string jsonData)
		{
			var collectionsJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
			if (collectionsJson == null) return;

			foreach (var keyValue in collectionsJson)
			{
				PlayerPrefs.SetString(keyValue.Key, keyValue.Value);
				// If the collection is already in memory, reload it with the imported data.
				if (GetCollection(keyValue.Key) is { } collection)
				{
					collection.Load(keyValue.Value);
				}
#if UNITY_EDITOR
				Debug.Log($"Imported collection '{keyValue.Key}'");
#endif
			}
			SaveCollectionKeys(collectionsJson.Keys.ToArray());
			PlayerPrefs.Save();
		}

		/// <summary>
		/// A debug utility that copies the entire database as a JSON string to the system clipboard.
		/// </summary>
		public static void CopyAllData()
		{
			var json = ToJson();
			UnityEngine.GUIUtility.systemCopyBuffer = json;
			Debug.Log("JObjectDB: All data copied to clipboard.");
		}

		/// <summary>
		/// Creates a JSON backup file of the entire database in a "Saves" directory.
		/// </summary>
		/// <param name="customFileName">An optional custom name for the backup file (without extension).</param>
		/// <param name="openDirectory">If true, opens the backup directory in the file explorer after saving (Editor only).</param>
		public static void Backup(string customFileName = null, bool openDirectory = false)
		{
			var time = DateTime.Now;
			string identifier = Application.identifier;
			var idParts = identifier.Split('.');
			string path;
			if (string.IsNullOrEmpty(customFileName))
			{
				string fileName = string.IsNullOrEmpty(identifier) ? "backup" : $"{idParts.Last()}";
				fileName += $"_{time:yyMMdd_HHmm}";
				path = GetFilePath(fileName);
			}
			else
				path = GetFilePath(customFileName);

			string jsonData = ToJson();
			File.WriteAllText(path, jsonData);
			Debug.Log($"JObjectDB: Data backed up to {path}");
#if UNITY_EDITOR
			if (openDirectory)
				Process.Start(Path.GetDirectoryName(path));
#endif
		}

		/// <summary>
		/// Restores the database from a specified backup file.
		/// </summary>
		/// <param name="filePath">The full path to the .json backup file.</param>
		public static void Restore(string filePath)
		{
			if (!File.Exists(filePath))
			{
				Debug.LogError($"JObjectDB: Restore failed. File not found at {filePath}");
				return;
			}
			using var sr = new StreamReader(filePath);
			var content = sr.ReadToEnd();
			if (!string.IsNullOrEmpty(content))
				Import(content);
		}

		/// <summary>
		/// Discards all in-memory changes by reloading data for all active collections from PlayerPrefs.
		/// </summary>
		public static void Reload()
		{
			foreach (var pair in collections)
				pair.Value.Load();
		}

		/// <summary>
		/// Saves the current state of all active in-memory collections to PlayerPrefs.
		/// </summary>
		public static void Save()
		{
			foreach (var pair in collections)
				pair.Value.Save();
			PlayerPrefs.Save();
		}
		
		/// <summary>
		/// Gets the full, platform-appropriate file path for a backup file.
		/// </summary>
		/// <param name="fileName">The name of the file without extension.</param>
		/// <returns>The full path for the backup file.</returns>
		private static string GetFilePath(string fileName)
		{
			string directoryPath;
#if UNITY_EDITOR
			// In the editor, save outside the Assets folder for cleanliness.
			directoryPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Saves");
#else
            // On a device, use the persistent data path.
            directoryPath = Application.persistentDataPath;
#endif
			if (!Directory.Exists(directoryPath))
				Directory.CreateDirectory(directoryPath);
			return Path.Combine(directoryPath, fileName + ".json");
		}
	}
}