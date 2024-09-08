/***
 * Author RadBear - nbhung71711@gmail.com - 2017
 **/

using RCore.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace RCore.Framework.Data
{
    [Obsolete("It's not really helpful")]
    public class PlayerPrefsSaver
    {
        [Serializable]
        public class KeyValue
        {
            public string k;
            public string v;
            public KeyValue(string k, string v)
            {
                this.k = k;
                this.v = v;
            }
        }

        [Serializable]
        public class ListKeyValue
        {
            public List<KeyValue> data;
            public ListKeyValue() { data = new List<KeyValue>(); }
        }

        [Serializable]
        public class Vector3Parse
        {
            public float x;
            public float y;
            public float z;

            public Vector3Parse(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public Vector3Parse(Vector3 v)
            {
                x = v.x;
                y = v.y;
                z = v.z;
            }

            public Vector3Parse(Vector2 v)
            {
                x = v.x;
                y = v.y;
                z = 0;
            }

            public override string ToString()
            {
                return $"{x}:{y}:{z}";
            }
        }

        private static readonly string KEY = "llkauliur";
        private static readonly bool ENCRYPT = true;

        private static PlayerPrefsSaver mInstance;
        private static PlayerPrefsSaver Instance
        {
            get
            {
                if (mInstance == null)
                    mInstance = new PlayerPrefsSaver();
                return mInstance;
            }
        }
        private ListKeyValue listKeyValue;
        private static Encryption m_Encryption = new Encryption();

        /// <summary>
        /// Sometime, when there are too much things we have to save at the save time
        /// In this case we should not call PlayerPrefs.Save() method casually, let call it at the end of saving process when every properties is already set
        /// </summary>
        public bool isChanged;

        public PlayerPrefsSaver()
        {
            string jsonString = PlayerPrefs.GetString(KEY, "");
            listKeyValue = string.IsNullOrEmpty(jsonString) ? new ListKeyValue() : JsonUtility.FromJson<ListKeyValue>(jsonString);
            listKeyValue ??= new ListKeyValue();
        }

        public static int FindIndex(string pKey)
        {
            var instance = Instance;
            for (int i = 0; i < instance.listKeyValue.data.Count; i++)
            {
                if (instance.listKeyValue.data[i].k == pKey)
                    return i;
            }

            return -1;
        }

        #region Save

        public static void Set(string key, float value)
        {
            string valueStr = "";
            if (ENCRYPT)
            {
                key = m_Encryption.Encrypt(key);
                valueStr = m_Encryption.Encrypt(value.ToString());
            }
            else
                valueStr = value.ToString();

            SetFinal(key, valueStr);
        }

        public static void Set(int index, float value)
        {
            string valueStr = "";
            if (ENCRYPT)
                valueStr = m_Encryption.Encrypt(value.ToString());
            else
                valueStr = value.ToString();

            Instance.listKeyValue.data[index].v = valueStr;
        }

        public static void Set(string key, int value)
        {
            string valueStr = "";
            if (ENCRYPT)
            {
                key = m_Encryption.Encrypt(key);
                valueStr = m_Encryption.Encrypt(value.ToString());
            }
            else
                valueStr = value.ToString();

            SetFinal(key, valueStr);
        }

        public static void Set(int index, int value)
        {
            string valueStr = "";
            if (ENCRYPT)
                valueStr = m_Encryption.Encrypt(value.ToString());
            else
                valueStr = value.ToString();
            Instance.listKeyValue.data[index].v = valueStr;
        }

        public static void Set(string key, string value)
        {
            string valueStr = "";
            if (ENCRYPT)
            {
                key = m_Encryption.Encrypt(key);
                valueStr = m_Encryption.Encrypt(value);
            }
            else
                valueStr = value;

            SetFinal(key, valueStr);
        }

        public static void Set(int index, string value)
        {
            string valueStr = "";
            if (ENCRYPT)
                valueStr = m_Encryption.Encrypt(value);
            else
                valueStr = value;
            Instance.listKeyValue.data[index].v = valueStr;
        }

        public static void Set(string key, Vector3 value)
        {
            string valueStr = "";
            if (ENCRYPT)
            {
                key = m_Encryption.Encrypt(key);
                valueStr = m_Encryption.Encrypt(JsonUtility.ToJson(new Vector3Parse(value)));
            }
            else
                valueStr = JsonUtility.ToJson(new Vector3Parse(value));

            SetFinal(key, valueStr);
        }

        public static void Set(int index, Vector3 value)
        {
            string valueStr = "";
            if (ENCRYPT)
                valueStr = m_Encryption.Encrypt(JsonUtility.ToJson(new Vector3Parse(value)));
            else
                valueStr = JsonUtility.ToJson(new Vector3Parse(value));
            Instance.listKeyValue.data[index].v = valueStr;
        }

        public static void Set(string key, Vector2 value)
        {
            string valueStr = "";
            if (ENCRYPT)
            {
                key = m_Encryption.Encrypt(key);
                valueStr = m_Encryption.Encrypt(JsonUtility.ToJson(new Vector3Parse(value)));
            }
            else
                valueStr = JsonUtility.ToJson(new Vector3Parse(value));

            SetFinal(key, valueStr);
        }

        public static void Set(int index, Vector2 value)
        {
            string valueStr = "";
            if (ENCRYPT)
                valueStr = m_Encryption.Encrypt(JsonUtility.ToJson(new Vector3Parse(value)));
            else
                valueStr = JsonUtility.ToJson(new Vector3Parse(value));
            Instance.listKeyValue.data[index].v = valueStr;
        }

        public static void Set(string key, bool value)
        {
            string valueStr = "";
            if (ENCRYPT)
            {
                key = m_Encryption.Encrypt(key);
                valueStr = m_Encryption.Encrypt(value.ToString());
            }
            else
                valueStr = value.ToString();

            SetFinal(key, valueStr);
        }

        public static void Set(int index, bool value)
        {
            string valueStr = "";
            if (ENCRYPT)
                valueStr = m_Encryption.Encrypt(value.ToString());
            else
                valueStr = value.ToString();
            Instance.listKeyValue.data[index].v = value.ToString();
        }

        public static void SaveAll()
        {
            if (Instance.isChanged)
            {
                try
                {
                    string jsonString = JsonUtility.ToJson(Instance.listKeyValue);
                    PlayerPrefs.SetString(KEY, jsonString);
                    PlayerPrefs.Save();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError(ex);
                }
            }
            Instance.isChanged = false;
        }

        private static void SetFinal(string pFinalKey, string pFinalValue)
        {
            int index = FindIndex(pFinalKey);
            if (index == -1)
                Instance.listKeyValue.data.Add(new KeyValue(pFinalKey, pFinalValue));
            else
                Instance.listKeyValue.data[index].v = pFinalValue;

            Instance.isChanged = true;
        }

        #endregion

        //====================================================================

        #region Load

        public static string GetString(string key, string defaultValue = "")
        {
            for (int i = 0; i < Instance.listKeyValue.data.Count; i++)
            {
                if (Instance.listKeyValue.data[i].k == key)
                    return Instance.listKeyValue.data[i].v;
            }

            return defaultValue;
        }

        public static float GetNumber(string key, float defaultValue = 0)
        {
            try
            {
                for (int i = 0; i < Instance.listKeyValue.data.Count; i++)
                {
                    if (Instance.listKeyValue.data[i].k == key)
                        return float.Parse(Instance.listKeyValue.data[i].v);
                }
            }
            catch
            {
                return defaultValue;
            }

            return defaultValue;
        }

        public static bool GetBoolean(string key, bool defaultValue)
        {
            try
            {
                for (int i = 0; i < Instance.listKeyValue.data.Count; i++)
                {
                    if (Instance.listKeyValue.data[i].k == key)
                        return bool.Parse(Instance.listKeyValue.data[i].v);
                }
            }
            catch
            {
                return defaultValue;
            }

            return defaultValue;
        }

        public static Vector3 GetVector3(string key, Vector3 defaultValue)
        {
            try
            {
                for (int i = 0; i < Instance.listKeyValue.data.Count; i++)
                {
                    if (Instance.listKeyValue.data[i].k == key)
                    {
                        var v = JsonUtility.FromJson<Vector3Parse>(Instance.listKeyValue.data[i].v);
                        return new Vector3(v.x, v.y, v.z);
                    }
                }
            }
            catch
            {
                return defaultValue;
            }

            return defaultValue;
        }

        public static Vector2 GetVector2(string key, Vector2 defaultValue)
        {
            try
            {
                for (int i = 0; i < Instance.listKeyValue.data.Count; i++)
                {
                    if (Instance.listKeyValue.data[i].k == key)
                    {
                        var v = JsonUtility.FromJson<Vector3Parse>(Instance.listKeyValue.data[i].v);
                        return new Vector3(v.x, v.y, v.z);
                    }
                }
            }
            catch
            {
                return defaultValue;
            }

            return defaultValue;
        }

        public static void DeleteAll()
        {
            PlayerPrefs.DeleteKey(KEY);
            Instance.isChanged = true;
        }

        public static void DeleteKey(string key)
        {
            int index = FindIndex(key);
            if (index != -1)
            {
                Instance.listKeyValue.data.RemoveAt(index);
                Instance.isChanged = true;
            }
        }

        #endregion
    }

    public class BinarySaver
    {
        private const string EXTENSION = ".sav";

        [Serializable]
        public class DataWrap
        {
            public string text;
            public DataWrap() { }
            public DataWrap(string pText) { text = pText; }
        }

        public static void SaveBinary(string data, string pFileName)
        {
            var temp = new DataWrap(data);
            var bf = new BinaryFormatter();
            var folder = Application.persistentDataPath + Path.DirectorySeparatorChar;
#if UNITY_EDITOR
            folder = Application.dataPath.Replace("Assets", "Saves") + Path.DirectorySeparatorChar;
#endif
            var file = File.Create(folder + pFileName + EXTENSION);
            bf.Serialize(file, temp);
            file.Close();
        }

        public static string LoadBinary(string pFileName)
        {
            var folder = Application.persistentDataPath + Path.DirectorySeparatorChar;
#if UNITY_EDITOR
            folder = Application.dataPath.Replace("Assets", "Saves") + Path.DirectorySeparatorChar;
#endif
            if (File.Exists(folder + pFileName + EXTENSION))
            {
                var bf = new BinaryFormatter();
                var file = File.Open(folder + pFileName + EXTENSION, FileMode.Open);
                var output = (DataWrap)bf.Deserialize(file);
                file.Close();
                return output.text;
            }
            return "";
        }

        public static void DeleteFile(string pFileName)
        {
            var folder = Application.persistentDataPath + Path.DirectorySeparatorChar;
#if UNITY_EDITOR
            folder = Application.dataPath.Replace("Assets", "Saves") + Path.DirectorySeparatorChar;
#endif
            File.Delete(folder + pFileName + EXTENSION);
        }
    }
}