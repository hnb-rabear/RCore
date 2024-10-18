using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace RCore.Data
{
	public static class BinaryDataSaver
	{
		private const string EXTENSION = ".bin";
		private static readonly BinaryFormatter m_BinaryFormatter = new BinaryFormatter();
		
		[Serializable]
		private class DataWrap
		{
			public string document;
			public DataWrap() { }
			public DataWrap(string pDocument) => document = pDocument;
		}

		public static void Save(string data, string pFileName)
		{
			string path = GetFilePath(pFileName);
			var dataWrap = new DataWrap(data);
			using (FileStream file = File.Create(path))
			{
				m_BinaryFormatter.Serialize(file, dataWrap);
			}
		}

		public static string Load(string pFileName)
		{
			string path = GetFilePath(pFileName);
			if (!File.Exists(path))
				return "";
			using (FileStream file = File.OpenRead(path))
			{
				var output = (DataWrap)m_BinaryFormatter.Deserialize(file);
				return output.document;
			}
		}
		
		public static void Delete(string fileName)
		{
			string path = GetFilePath(fileName);
			if (File.Exists(path))
				File.Delete(path);
		}
		
		private static string GetFilePath(string fileName)
		{
#if UNITY_EDITOR
			return Path.Combine(Application.dataPath.Replace("Assets", "Saves"), fileName + EXTENSION);
#endif
			return Path.Combine(Application.persistentDataPath, fileName + EXTENSION);
		}
	}
}