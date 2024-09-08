/***
 * Author RadBear - Nguyen Ba Hung - nbhung71711@gmail.com - 2018
 **/

using RCore.Common;
using System;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace RCore.Framework.Data
{
    [Serializable]
    public class KeyValue : IComparable<KeyValue>
    {
        [SerializeField] private string k;
        [SerializeField] private string v;
        [SerializeField] private string a;

        public string Key { get => k; set => k = value; }
        public string Value { get => v; set => v = value; }
        public string Alias { get => a; set => a = value; }

        public KeyValue(string pKey, string pValue, string pAlias = null)
        {
            k = pKey;
            v = pValue;
            a = pAlias;
        }

        public int CompareTo(KeyValue other)
        {
            var ks = k.Split('.');
            var otherKs = other.k.Split('.');
            int lastIndex = 0;
            for (int i = 0; i < ks.Length; i++)
            {
                if (i < otherKs.Length)
                {
                    int a = Convert.ToInt32(ks[i]);
                    int b = Convert.ToInt32(otherKs[i]);
                    if (a != b)
                        return a.CompareTo(b);
                }
                else
                {
                    lastIndex = i - 1;
                    break;
                }
            }
            return Convert.ToInt32(ks[lastIndex]).CompareTo(Convert.ToInt32(otherKs[lastIndex]));
        }
    }

    public class DataSaver
    {
        #region Constants

        private static readonly bool ENCRYPT_KEY = false;

        #endregion

        //============================

        #region Members

        public IEncryption encryption;
        public string idString;
        public List<KeyValue> dataList;
        private bool mIsChanged;
        private bool mIsEncrypt;

        #endregion

        //=============================

        #region Constructor

        public DataSaver(string pSaverIdString, IEncryption pEncryption)
        {
            idString = pSaverIdString;
            encryption = pEncryption;
            mIsEncrypt = encryption != null;

            string dataStr = DataSaverContainer.GetData(idString);
            dataList = JsonHelper.ToList<KeyValue>(dataStr);

            if (dataList == null)
                dataList = new List<KeyValue>();
#if UNITY_EDITOR
            //NOTE: If data list is too long, let it be is better than sort it
            else if (!ENCRYPT_KEY)
                dataList.Sort();
#endif
        }

        #endregion

        //==============================

        #region Save

        public int Set(string pKey, string pValue, string pAlias)
        {
            if (ENCRYPT_KEY)
                pKey = Encrypt(pKey);

            string pValueStr = Encrypt(pValue);
            for (int i = 0; i < dataList.Count; i++)
            {
                if (dataList[i].Key == pKey)
                {
                    dataList[i].Value = pValueStr;
                    dataList[i].Alias = pAlias;
                    mIsChanged = true;
                    return i;
                }
            }

            dataList.Add(new KeyValue(pKey, pValueStr, pAlias));
            mIsChanged = true;
            return dataList.Count - 1;
        }

        public void Set(int pIndex, string pValue, string pAlias)
        {
            string pValueStr = Encrypt(pValue);
            dataList[pIndex].Value = pValueStr;
            dataList[pIndex].Alias = pAlias;
            mIsChanged = true;
        }

        public void Save(bool pForce)
        {
            if (mIsChanged || pForce)
            {
                string dataStr = JsonHelper.ToJson(dataList);
                DataSaverContainer.SetData(idString, dataStr);
                mIsChanged = false;
            }
        }

        #endregion

        //============================================================

        #region Load

        public string Get(string pKey, out int pIndex)
        {
            if (ENCRYPT_KEY)
                pKey = Encrypt(pKey);

            for (int i = 0; i < dataList.Count; i++)
            {
                if (dataList[i].Key == pKey)
                {
                    pIndex = i;
                    return Decrypt(dataList[i].Value);
                }
            }
            pIndex = -1;
            return null;
        }

        public string Get(int pIndex)
        {
            if (dataList.Count > pIndex && pIndex >= 0)
                return Decrypt(dataList[pIndex].Value);
            return null;
        }

        //public string GetString(string pKey, out int pIndex, string pDefaultValue = "")
        //{
        //    string value = Get(pKey, out pIndex);
        //    if (string.IsNullOrEmpty(value))
        //        return pDefaultValue;
        //    else
        //        return value;
        //}

        //public string GetString(int pIndex, string pDefaultValue = "")
        //{
        //    string value = Get(pIndex);
        //    if (string.IsNullOrEmpty(value))
        //        return pDefaultValue;
        //    else
        //        return value;
        //}

        //public float GetNumber(string pKey, out int pIndex, float pDefaultValue = 0)
        //{
        //    string valueStr = Get(pKey, out pIndex);
        //    float output = pDefaultValue;
        //    if (float.TryParse(valueStr, out output))
        //        return output;
        //    else
        //        return pDefaultValue;
        //}

        public void RemoveKeys(List<int> pCleanableKeys)
        {
            //Soft descending 
            pCleanableKeys.Sort((a, b) => -1 * a.CompareTo(b));
            for (int i = 0; i < pCleanableKeys.Count; i++)
            {
                int index = pCleanableKeys[i];
                dataList.RemoveAt(index);
            }
            mIsChanged = true;
#if UNITY_EDITOR
            Debug.Log("Removed Indexes: " + JsonHelper.ToJson(pCleanableKeys));
#endif
        }

        public bool GetBoolean(string pKey, out int pIndex, bool pDefaultValue = false)
        {
            string valueStr = Get(pKey, out pIndex);
            bool output = pDefaultValue;
            if (valueStr != null && bool.TryParse(valueStr, out output))
                return output;
            else
                return pDefaultValue;
        }

        public void Remove(string pKey)
        {
            if (ENCRYPT_KEY)
                pKey = Encrypt(pKey);

            for (int i = 0; i < dataList.Count; i++)
            {
                if (dataList[i].Key == pKey)
                {
                    dataList.RemoveAt(i);
                    mIsChanged = true;
                    break;
                }
            }
        }

        public void RemoveAll()
        {
            dataList = new List<KeyValue>();
            Save(true);
            //We need to call reload after deleting in realtime
        }

        /// <summary>
        /// Get current data, which are stored but did not saved yet
        /// </summary>
        public string GetCurrentData()
        {
            dataList.Sort();
            return JsonHelper.ToJson(dataList);
        }

        /// <summary>
        /// Get saved data
        /// </summary>
        public string GetSavedData()
        {
            return DataSaverContainer.GetData(idString);
        }

        public void SetSavedData(string pContent)
        {
            DataSaverContainer.SetData(idString, pContent);
        }

        #endregion

        private string Encrypt(string value)
        {
            if (mIsEncrypt)
                return encryption.Encrypt(value);

            return value;
        }

        private string Decrypt(string encryptStr)
        {
            if (mIsEncrypt)
                return encryption.Decrypt(encryptStr);

            return encryptStr;
        }
    }
}