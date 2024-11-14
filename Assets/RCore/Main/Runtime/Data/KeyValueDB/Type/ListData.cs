/***
 * Author HNB-RaBear - 2018
 **/

using System;
using System.Collections.Generic;

namespace RCore.Data.KeyValue
{
    public class ListData<T> : FunData
    {
        private List<T> m_values;
        private List<T> m_defaultValues;
        private bool m_changed;

        /// <summary>
        /// NOTE: This type of data should not has Get method
        /// If you are looking for a way to get all values, please use for GetValues method and read it's notice carefully
        /// </summary>
        public List<T> Values
        {
            set
            {
                if (value != m_values)
                {
                    m_values = value;
                    m_changed = true;
                }
            }
        }

        public int Count => m_values?.Count ?? 0;

        /// <summary>
        /// NOTE: use this carefully because we can not detect the change made in an element
        /// </summary>
        public T this[int index]
        {
            get => m_values[index];
            set
            {
                m_values[index] = value;
                m_changed = true;
            }
        }

        public ListData(int pId, List<T> pDefaultValues = null) : base(pId)
        {
            m_defaultValues = pDefaultValues;
        }

        public ListData(int pId, T[] pDefaultValues) : base(pId)
        {
            m_defaultValues = new List<T>();
            m_defaultValues.AddRange(pDefaultValues);
        }

        public override void Load(string pBaseKey, string pSaverIdString)
        {
            base.Load(pBaseKey, pSaverIdString);
            m_values = GetSavedValues();
        }

        public void Add(T value)
        {
            m_values ??= new List<T>();
            m_values.Add(value);
            m_changed = true;
        }

        public void AddRange(params T[] values)
        {
            m_values ??= new List<T>();
            m_values.AddRange(values);
            m_changed = true;
        }

        public void Remove(T value)
        {
            m_values.Remove(value);
            m_changed = true;
        }

        public void Replace(T newValue, T oldValue)
        {
            int index = m_values.IndexOf(oldValue);
            m_values[index] = newValue;
            m_changed = true;
        }

        public void Insert(int pIndex, T value)
        {
            m_values.Insert(pIndex, value);
            m_changed = true;
        }

        public void RemoveAt(int pIndex)
        {
            m_values.RemoveAt(pIndex);
            m_changed = true;
        }

        public bool Contain(T value)
        {
            return m_values != null && m_values.Contains(value);
        }

        public override bool Stage()
        {
            if (m_changed)
            {
                SetStringValue(JsonHelper.ToJson(m_values));
                m_changed = false;
                return true;
            }
            return false;
        }

        private List<T> GetSavedValues()
        {
            string val = GetStringValue();
            if (string.IsNullOrEmpty(val))
                return m_defaultValues;

            try
            {
                m_values = JsonHelper.ToList<T>(val);
                return m_values;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());

                m_values = m_defaultValues != null ? new List<T>(m_defaultValues) : null;
                return m_values;
            }
        }

        public override void Reload()
        {
            base.Reload();
            m_values = GetSavedValues();
            m_changed = false;
        }

        public override void Reset()
		{
			m_values = m_defaultValues != null ? new List<T>(m_defaultValues) : null;
			m_changed = true;
		}

        public void Clear()
        {
            m_values = new List<T>();
            m_changed = true;
        }

        public override bool Cleanable()
        {
            return false;
        }

        public void Sort()
        {
            m_values.Sort();
            m_changed = true;
        }

        public void Reverse()
        {
            m_values.Reverse();
            m_changed = true;
        }

        public void MarkChange()
        {
            m_changed = true;
        }

        [Obsolete("NOTE: Because It is hard to track the change if we directly add/insert/update internal data of list. Therefore" +
        "If you are planing to change directly data inside list. You must set mChanged manually by MarkChange method")]
        public List<T> GetValues()
        {
            return m_values;
        }
    }
}