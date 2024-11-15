/**
 * Author HNB-RaBear - 2018
 **/

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RCore.Data.KeyValue
{
	public static class KeyValueDB
	{
		/// <summary>
		/// Used to save Key or Data Saver, which lately used for indexing data list
		/// </summary>
		private static readonly string COLLECTIONS = "6597123654782136";
		public static readonly bool USE_PLAYERPREFS = true;
		public static readonly bool USE_BINARY = false;

		public static Dictionary<string, KeyValueCollection> collections = new Dictionary<string, KeyValueCollection>();

#region Runtime only

		public static KeyValueCollection GetCollection(string key)
		{
			if (collections.TryGetValue(key, out var collection))
				return collection;
			return null;
		}

		public static KeyValueCollection CreateCollection(string key, IEncryption pEncryption)
		{
			if (collections.ContainsKey(key))
			{
				Debug.LogError($"Collection {key} Existed");
				return null;
			}

			var collection = new KeyValueCollection(key, pEncryption);

			SaveCollectionKey(key);
			collections.Add(key, collection);

			return collection;
		}

		private static void SaveCollectionKey(string key)
		{
			string keysStr = PlayerPrefs.GetString(COLLECTIONS);
			string[] keys = keysStr.Split(':');
			for (int i = 0; i < keys.Length; i++)
				if (keys[i] == key)
					return;

			if (keys.Length == 0)
				keysStr += key;
			else
				keysStr += ":" + key;

			PlayerPrefs.SetString(COLLECTIONS, keysStr);
			PlayerPrefs.Save();
		}

#endregion

		//================================================

#region Editor Friendly but also work on runtime

		public static string[] GetCollectionKeys()
		{
			string keysStr = PlayerPrefs.GetString(COLLECTIONS);
			if (string.IsNullOrEmpty(keysStr))
				return Array.Empty<string>();

			string[] keys = keysStr.Split(':');
			return keys;
		}

		public static void SetData(string pSaverKey, string pData)
		{
			if (USE_PLAYERPREFS)
			{
				PlayerPrefs.SetString(pSaverKey, pData);
				PlayerPrefs.Save();
			}
			if (USE_BINARY)
			{
				BinaryDataSaver.Save(pData, pSaverKey);
			}
#if UNITY_EDITOR
			Debug.Log($"Saved Key: {pSaverKey}\nData: {pData}");
#endif
		}

		public static string GetData(string pSaverKey)
		{
			string dataStr = "";
			if (USE_PLAYERPREFS)
				dataStr = PlayerPrefs.GetString(pSaverKey, "");
			if (USE_BINARY)
				dataStr = BinaryDataSaver.Load(pSaverKey);
			return dataStr;
		}

		/// <summary>
		/// Get data from all collections
		/// </summary>
		public static string GetAllData()
		{
			var saverKeys = GetCollectionKeys();
			var saverBrands = new List<KeyValueSS>();
			foreach (var saverKey in saverKeys)
			{
				if (string.IsNullOrEmpty(saverKey))
					continue;

				string data = GetData(saverKey);
				saverBrands.Add(new KeyValueSS(saverKey, data));
			}
			string jsonData = JsonHelper.ToJson(saverBrands);
			return jsonData;
		}

		public static void DeleteAll()
		{
			if (Application.isPlaying)
			{
				foreach (var saver in collections)
					saver.Value.RemoveAll();
				//NOTE: we need some kind of reload function after deleting in realtime
			}
			else
			{
				var saverKeys = GetCollectionKeys();
				for (int i = 0; i < saverKeys.Length; i++)
				{
					if (USE_PLAYERPREFS)
						PlayerPrefs.DeleteKey(saverKeys[i]);
					if (USE_BINARY)
						BinaryDataSaver.Delete(saverKeys[i]);
				}
			}
			if (USE_PLAYERPREFS)
				PlayerPrefs.Save();
		}

		public static void ImportData(string pJsonData)
		{
			var collectionsJson = JsonHelper.ToList<KeyValueSS>(pJsonData);
			if (collectionsJson != null)
			{
				foreach (var keyValue in collectionsJson)
				{
					if (USE_PLAYERPREFS)
						PlayerPrefs.SetString(keyValue.Key, keyValue.Value);
					if (USE_BINARY)
						BinaryDataSaver.Save(keyValue.Value, keyValue.Key);
#if UNITY_EDITOR
					Debug.Log($"Import {keyValue.Key}\n{keyValue.Value}");
#endif
					var collection = GetCollection(keyValue.Key);
					if (collection != null)
					{
						collection.dataList = JsonHelper.ToList<KeyValueSS>(keyValue.Value);
						if (collection.dataList == null)
							collection.dataList = new List<KeyValueSS>();
					}
				}
				if (USE_PLAYERPREFS)
					PlayerPrefs.Save();
			}
		}

		public static void LogData()
		{
			bool hasData = false;
			var saverKeys = GetCollectionKeys();
			foreach (var k in saverKeys)
			{
				if (string.IsNullOrEmpty(k))
					continue;

				hasData = true;
				Debug.Log($"Key {k}: {GetData(k)}");
			}
			if (!hasData)
				Debug.Log("No Data");
		}

		/// <summary>
		/// Get keys and values from data saver from saver
		/// </summary>
		public static List<KeyValueSS> GetCollectionKeys(string pSaverKey)
		{
			string data = GetData(pSaverKey);
			return JsonHelper.ToList<KeyValueSS>(data);
		}

		/// <summary>
		/// Get keys and values from all data saver from savers
		/// </summary>
		public static Dictionary<string, List<KeyValueSS>> GetAllDataKeyValues()
		{
			var saverKeys = GetCollectionKeys();
			var dictKeyValues = new Dictionary<string, List<KeyValueSS>>();
			foreach (var saverKey in saverKeys)
			{
				if (string.IsNullOrEmpty(saverKey))
					continue;

				var keyValues = GetCollectionKeys(saverKey);
				dictKeyValues.Add(saverKey, keyValues);
			}
			return dictKeyValues;
		}

#endregion

		//================================================

#region Editor Only

		public static void BackupData(string filePath)
		{
			string jsonData = GetAllData();
#if UNITY_ANDROID
			File.WriteAllText(filePath, jsonData);
#else
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                sw.WriteLine(jsonData);
                sw.Close();
                Debug.Log("Backup Success full \n" + filePath);
            }
#endif
		}

		public static void RestoreData(string filePath)
		{
			using (var sw = new StreamReader(filePath))
			{
				var content = sw.ReadToEnd();
				if (!string.IsNullOrEmpty(content))
					ImportData(content);
			}
		}

#endregion
	}
}