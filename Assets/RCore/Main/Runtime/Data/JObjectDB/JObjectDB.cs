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

namespace RCore.Data.JObject
{
	/// <summary>
	/// A static database manager for handling data collections that inherit from JObjectData.
	/// It uses PlayerPrefs as the storage backend and provides a centralized system for creating,
	/// loading, saving, and managing all data collections as a single unit.
	/// </summary>
	public static class JObjectDB
	{
		/// <summary>
		/// The PlayerPrefs key used to store the master list of all collection keys.
		/// This serves as an index to find all other data saved by this system.
		/// </summary>
		private static readonly string COLLECTIONS = "JObjectDB";
		
		/// <summary>
		/// An in-memory cache of all currently loaded JObjectData collections, keyed by their unique string identifier.
		/// </summary>
		public static Dictionary<string, JObjectData> collections = new Dictionary<string, JObjectData>();

		/// <summary>
		/// Retrieves a loaded data collection from the in-memory cache.
		/// </summary>
		/// <param name="key">The unique key of the collection to retrieve.</param>
		/// <returns>The JObjectData instance if it is currently loaded in memory, otherwise null.</returns>
		public static JObjectData GetCollection(string key)
		{
			if (collections.TryGetValue(key, out var collection))
				return collection;
			return null;
		}
		
		/// <summary>
		/// Creates, loads, and registers a new data collection of a specific type.
		/// If a collection with the same key already exists, it will be overwritten.
		/// The method attempts to load existing data from PlayerPrefs; if that fails and a default value is provided,
		/// it will initialize the collection with the default data.
		/// </summary>
		/// <typeparam name="T">The type of JObjectData to create, which must have a parameterless constructor.</typeparam>
		/// <param name="key">The unique key for this data collection.</param>
		/// <param name="defaultVal">An optional default instance to populate the collection if no saved data is found.</param>
		/// <returns>The newly created or loaded collection instance.</returns>
		public static T CreateCollection<T>(string key, T defaultVal = null) where T : JObjectData, new()
		{
			if (!collections.TryGetValue(key, out var collection))
			{
				collection = new T();
				collections[key] = collection;
			}
			else
			{
				Debug.LogError($"Overwrite the existed Collection: {key}");
			}

			collection.key = key;

			if (!collection.Load() && defaultVal != null)
			{
				string json = defaultVal.ToJson();
				collection = JsonUtility.FromJson<T>(json);
				collection.key = key;
			}

			SaveCollectionKey(key);
			return collection as T;
		}
		
		/// <summary>
		/// A helper method to add a single collection key to the master index in PlayerPrefs.
		/// </summary>
		/// <param name="pKey">The key to add.</param>
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
		/// A helper method to add multiple collection keys to the master index in PlayerPrefs.
		/// </summary>
		/// <param name="pKeys">The keys to add.</param>
		private static void SaveCollectionKeys(string[] pKeys)
		{
			var keys = GetSavedCollectionKeys();
			bool updated = false;
			foreach (string pKey in pKeys)
			{
				if (!keys.Contains(pKey))
				{
					keys.Add(pKey);
					updated = true;
				}
			}
			if (updated)
				PlayerPrefs.SetString(COLLECTIONS, JsonHelper.ToJson(keys));
		}

		/// <summary>
		/// A helper method to retrieve the master list of all collection keys from PlayerPrefs.
		/// </summary>
		/// <returns>A list of all saved collection keys.</returns>
		private static List<string> GetSavedCollectionKeys()
		{
			string keysStr = PlayerPrefs.GetString(COLLECTIONS);
			if (string.IsNullOrEmpty(keysStr))
				return new List<string>();
			var keys = JsonHelper.ToList<string>(keysStr);
			keys ??= new List<string>();
			return keys;
		}
		
		/// <summary>
		/// Gets a list of all known collection keys. It returns the keys from the in-memory cache if available,
		/// otherwise it reads the master list from PlayerPrefs.
		/// </summary>
		/// <returns>A list of all collection keys.</returns>
		public static List<string> GetCollectionKeys()
		{
			return collections.Count == 0 ? GetSavedCollectionKeys() : collections.Keys.ToList();
		}
		
		/// <summary>
		/// Retrieves all saved data from PlayerPrefs as a dictionary of raw JSON strings.
		/// </summary>
		/// <returns>A dictionary where the key is the collection key and the value is its JSON data.</returns>
		public static Dictionary<string, string> GetAllData()
		{
			var dict = new Dictionary<string, string>();
			if (collections.Count == 0)
			{
				// If no collections are loaded in memory, read directly from PlayerPrefs.
				var keys = GetCollectionKeys();
				foreach (string key in keys)
				{
					var data = PlayerPrefs.GetString(key);
					if (!string.IsNullOrEmpty(data))
						dict.Add(key, data);
				}
				return dict;
			}
			// If collections are loaded, serialize their current in-memory state.
			foreach (var pair in collections)
				dict.Add(pair.Key, pair.Value.ToJson());
			return dict;
		}
		
		/// <summary>
		/// Retrieves all saved data and deserializes it into a dictionary of objects using Newtonsoft.Json.
		/// </summary>
		/// <returns>A dictionary where the key is the collection key and the value is the deserialized data object.</returns>
		public static Dictionary<string, object> GetAllDataObjects()
		{
			var dict = new Dictionary<string, object>();
			if (collections.Count == 0)
			{
				var keys = GetCollectionKeys();
				foreach (string key in keys)
				{
					string json = PlayerPrefs.GetString(key);
					if (!string.IsNullOrEmpty(json))
						dict.Add(key, JsonConvert.DeserializeObject<object>(json));
				}
				return dict;
			}
			foreach (var pair in collections)
				dict.Add(pair.Key, pair.Value);
			return dict;
		}

		/// <summary>
		/// Serializes the entire database (all collections) into a single JSON string.
		/// </summary>
		/// <returns>A JSON string representing all data.</returns>
		public static string ToJson()
		{
			var dict = GetAllData();
			return JsonConvert.SerializeObject(dict);
		}

		/// <summary>
		/// Deletes all data for all registered collections from PlayerPrefs and clears the in-memory cache.
		/// </summary>
		public static void DeleteAll()
		{
			var saverKeys = GetSavedCollectionKeys();
			for (int i = 0; i < saverKeys.Count; i++)
				PlayerPrefs.DeleteKey(saverKeys[i]);
			PlayerPrefs.DeleteKey(COLLECTIONS); // Also delete the master index
			collections.Clear();
		}

		/// <summary>
		/// Deletes the data for a single collection by its key.
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
		/// Imports data from a JSON string. The string should represent a dictionary of collection keys and their JSON data.
		/// This will overwrite any existing data in PlayerPrefs.
		/// </summary>
		/// <param name="jsonData">The JSON string containing the data to import.</param>
		public static void Import(string jsonData)
		{
			var collectionsJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
			foreach (var keyValue in collectionsJson)
			{
				PlayerPrefs.SetString(keyValue.Key, keyValue.Value);
				// If the collection is already in memory, reload its data from the imported JSON.
				var collection = GetCollection(keyValue.Key);
				collection?.Load(keyValue.Value);
#if UNITY_EDITOR
				Debug.Log($"Import {keyValue.Key}\n{keyValue.Value}");
#endif
			}
			// Update the master index with all imported keys.
			SaveCollectionKeys(collectionsJson.Keys.ToArray());
			PlayerPrefs.Save();
		}

		/// <summary>
		/// A debug utility method that copies the entire database as a JSON string to the system clipboard.
		/// </summary>
		public static void CopyAllData()
		{
			var json = JsonConvert.SerializeObject(GetAllData());
			Debug.Log(json);
			UniClipboard.SetText(json);
		}

		/// <summary>
		/// Creates a backup of the entire database as a JSON file on disk.
		/// </summary>
		/// <param name="customFileName">An optional custom name for the backup file (without extension).</param>
		/// <param name="openDirectory">If true, opens the backup directory in the file explorer after saving (Editor-only).</param>
		public static void Backup(string customFileName = null, bool openDirectory = false)
		{
			var time = DateTime.Now;
			string identifier = Application.identifier;
			var idParts = identifier.Split('.');
			string path;
			if (string.IsNullOrEmpty(customFileName))
			{
				// Generate a default file name based on the app identifier and timestamp.
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

		/// <summary>
		/// Restores the database from a specified backup file.
		/// </summary>
		/// <param name="filePath">The full path to the backup JSON file.</param>
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
		/// Discards all current in-memory changes for all loaded collections and reloads their data from PlayerPrefs.
		/// </summary>
		public static void Reload()
		{
			foreach (var pair in collections)
				pair.Value.Load();
		}

		/// <summary>
		/// Saves the current in-memory state of all loaded collections to PlayerPrefs.
		/// </summary>
		public static void Save()
		{
			foreach (var pair in collections)
				pair.Value.Save();
		}
		
		/// <summary>
		/// A helper method to get the full file path for a backup file.
		/// In the Unity Editor, it saves to a "Saves" folder next to "Assets".
		/// In a build, it saves to the application's persistent data path.
		/// </summary>
		/// <param name="fileName">The name of the file without extension.</param>
		/// <returns>The full path for the backup file.</returns>
		private static string GetFilePath(string fileName)
		{
#if UNITY_EDITOR
			string directoryPath = Application.dataPath.Replace("Assets", "Saves");
			if (!Directory.Exists(directoryPath))
				Directory.CreateDirectory(directoryPath);
			return Path.Combine(directoryPath, fileName + ".json");
#else
			return Path.Combine(Application.persistentDataPath, fileName + ".json");
#endif
		}
	}
}