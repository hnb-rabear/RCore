/**
 * Author RadBear - nbhung71711@gmail.com - 2018
 **/

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace RCore.Pattern.Data
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">T is serializable object</typeparam>
    public class ObjectData<T> : FunData where T : IComparable<T>
    {
        private T mValue;
        private T mDefaultValue;
        private T mCompareValue; //If the object T is changed inside it
        private bool mChanged;

        public T Value
        {
            get { return mValue != null ? mValue : mDefaultValue; }
            set
            {
                if ((mValue != null && value == null) || (mValue == null && value != null) || mValue.CompareTo(value) != 0)
                {
                    mValue = value;
                    mCompareValue = Clone(value);
                    mChanged = true;
                }
            }
        }

        public ObjectData(int pId, T pDefaultValue, string pAlias = null) : base(pId, pAlias)
        {
            mDefaultValue = pDefaultValue;
        }

        public override void Load(string pBaseKey, string pSaverIdString)
        {
            base.Load(pBaseKey, pSaverIdString);

            mValue = GetSavedValue();
        }

        public override bool Stage()
        {
            if ((mValue != null && mCompareValue == null) || (mValue == null && mCompareValue != null) || mChanged || mValue.CompareTo(mCompareValue) != 0)
            {
                var saveStr = JsonUtility.ToJson(mValue);
                SetStringValue(saveStr);
                mCompareValue = Clone(Value);
                mChanged = false;
                return true;
            }
            return false;
        }

        private T GetSavedValue()
        {
            string val = GetStringValue();
            try
            {
                if (!string.IsNullOrEmpty(val))
                    return JsonUtility.FromJson<T>(val);
                return
                    mDefaultValue;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());

                Value = mDefaultValue;
                return mDefaultValue;
            }
        }

        private T Clone(T source)
        {
            if (!typeof(T).IsSerializable)
                throw new ArgumentException("The type must be serializable.", "source");

            // Don't serialize a null object, simply return the default for that object
            if (System.Object.ReferenceEquals(source, null))
                return default(T);

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                formatter.Serialize(stream, source);
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
        }

        public override void Reload()
        {
            base.Reload();
            mValue = GetSavedValue();
            mChanged = false;
        }

        public override void Reset()
        {
            Value = mDefaultValue;
        }

        public override bool Cleanable()
        {
            return false;
        }
    }
}