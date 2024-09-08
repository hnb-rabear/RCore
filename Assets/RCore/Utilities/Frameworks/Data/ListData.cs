/***
 * Author RadBear - nbhung71711@gmail.com - 2018
 **/

using System;
using System.Collections.Generic;
using RCore.Common;
using Debug = UnityEngine.Debug;

namespace RCore.Framework.Data
{
    public class ListData<T> : FunData
    {
        private List<T> mValues;
        private List<T> mDefaultValues;
        private bool mChanged;

        /// <summary>
        /// NOTE: This type of data should not has Get method
        /// If you are looking for a way to get all values, please use for GetValues method and read it's notice carefully
        /// </summary>
        public List<T> Values
        {
            set
            {
                if (value != mValues)
                {
                    mValues = value;
                    mChanged = true;
                }
            }
        }

        public int Count => mValues == null ? 0 : mValues.Count;

        /// <summary>
        /// NOTE: use this carefully because we can not detect the change made in an element
        /// </summary>
        public T this[int index]
        {
            get => mValues[index];
            set
            {
                mValues[index] = value;
                mChanged = true;
            }
        }

        public ListData(int pId, List<T> pDefaultValues = null, string pAlias = null) : base(pId, pAlias)
        {
            mDefaultValues = pDefaultValues;
        }

        public ListData(int pId, T[] pDefaultValues, string pAlias = null) : base(pId, pAlias)
        {
            mDefaultValues = new List<T>();
            mDefaultValues.AddRange(pDefaultValues);
        }

        public override void Load(string pBaseKey, string pSaverIdString)
        {
            base.Load(pBaseKey, pSaverIdString);
            mValues = GetSavedValues();
        }

        public void Add(T value)
        {
            mValues ??= new List<T>();
            mValues.Add(value);
            mChanged = true;
        }

        public void AddRange(params T[] values)
        {
            mValues ??= new List<T>();
            mValues.AddRange(values);
            mChanged = true;
        }

        public void Remove(T value)
        {
            mValues.Remove(value);
            mChanged = true;
        }

        public void Replace(T newValue, T oldValue)
        {
            int index = mValues.IndexOf(oldValue);
            mValues[index] = newValue;
            mChanged = true;
        }

        public void Insert(int pIndex, T value)
        {
            mValues.Insert(pIndex, value);
            mChanged = true;
        }

        public void RemoveAt(int pIndex)
        {
            mValues.RemoveAt(pIndex);
            mChanged = true;
        }

        public bool Contain(T value)
        {
            return mValues != null && mValues.Contains(value);
        }

        public override bool Stage()
        {
            if (mChanged)
            {
                SetStringValue(JsonHelper.ToJson(mValues));
                mChanged = false;
                return true;
            }
            return false;
        }

        private List<T> GetSavedValues()
        {
            string val = GetStringValue();
            if (string.IsNullOrEmpty(val))
                return mDefaultValues;

            try
            {
                mValues = JsonHelper.ToList<T>(val);
                return mValues;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());

                mValues = mDefaultValues != null ? new List<T>(mDefaultValues) : null;
                return mValues;
            }
        }

        public override void Reload()
        {
            base.Reload();
            mValues = GetSavedValues();
            mChanged = false;
        }

        public override void Reset()
		{
			mValues = mDefaultValues != null ? new List<T>(mDefaultValues) : null;
			mChanged = true;
		}

        public void Clear()
        {
            mValues = new List<T>();
            mChanged = true;
        }

        public override bool Cleanable()
        {
            return false;
        }

        public void Sort()
        {
            mValues.Sort();
            mChanged = true;
        }

        public void Reverse()
        {
            mValues.Reverse();
            mChanged = true;
        }

        public void MarkChange()
        {
            mChanged = true;
        }

        [Obsolete("NOTE: Because It is hard to track the change if we directly add/insert/update internal data of list. Therefore" +
        "If you are planing to change directly data inside list. You must set mChanged manually by MarkChange method")]
        public List<T> GetValues()
        {
            return mValues;
        }
    }
}