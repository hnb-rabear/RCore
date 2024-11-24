/**
 * Author HNB-RaBear - 2021
 **/

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
	[InitializeOnLoad]
	public static class REditorPrefContainer
	{
		private static readonly List<REditorPref> m_REditorPrefs = new List<REditorPref>();
		static REditorPrefContainer()
		{
			EditorApplication.update += SaveChanges;
			EditorApplication.playModeStateChanged += _ =>
			{
				SaveChanges();
			};
		}
		public static void DeleteAll()
		{
			for (int i = 0; i < m_REditorPrefs.Count; i++)
				m_REditorPrefs[i].Delete();
		}
		public static void Register(REditorPref pChange)
        {
            for (int i = 0; i < m_REditorPrefs.Count; i++)
            {
                if (m_REditorPrefs[i].key == pChange.key)
                {
                    m_REditorPrefs[i] = pChange;
                    return;
                }
            }
            m_REditorPrefs.Add(pChange);
        }
		public static void SaveChanges()
		{
			for (int i = 0; i < m_REditorPrefs.Count; i++)
				m_REditorPrefs[i].SaveChange();
		}
	}

	public abstract class REditorPref
	{
		public string key;
		protected bool changed;
		protected REditorPref(string pKey)
		{
			key = pKey;
			changed = false;
			REditorPrefContainer.Register(this);
		}
		public void Delete()
		{
			EditorPrefs.DeleteKey(key);
		}
		public abstract void SaveChange();
	}

	public class REditorPrefBool : REditorPref
	{
		private bool m_value;
		public bool Value
		{
			get => m_value;
			set
			{
				if (m_value == value)
					return;
				m_value = value;
				changed = true;
			}
		}
		public REditorPrefBool(string pKey, bool pDefault = false) : base(pKey)
		{
			m_value = EditorPrefs.GetInt(pKey, pDefault ? 1 : 0) == 1;
		}
		public override string ToString()
		{
			return m_value.ToString();
		}
		public override void SaveChange()
		{
			if (!changed)
				return;
			EditorPrefs.SetInt(key, m_value ? 1 : 0);
			changed = false;
		}
	}

	public class REditorPrefInt : REditorPref
	{
		private int m_value;
		public int Value
		{
			get => m_value;
			set
			{
				if (m_value == value)
					return;
				m_value = value;
				changed = true;
			}
		}
		public REditorPrefInt(string pKey, int pDefault = 0) : base(pKey)
		{
			m_value = EditorPrefs.GetInt(pKey, pDefault);
		}
		public override string ToString()
		{
			return m_value.ToString();
		}
		public override void SaveChange()
		{
			if (!changed)
				return;
			EditorPrefs.SetInt(key, m_value);
			changed = false;
		}
	}

	public class REditorPrefFloat : REditorPref
	{
		private float m_value;
		public float Value
		{
			get => m_value;
			set
			{
				if (m_value == value)
					return;
				m_value = value;
				changed = true;
			}
		}
		public REditorPrefFloat(string pKey, float pDefault = 0) : base(pKey)
		{
			m_value = EditorPrefs.GetFloat(pKey, pDefault);
		}
		public override string ToString()
		{
			return m_value.ToString();
		}
		public override void SaveChange()
		{
			if (!changed)
				return;
			EditorPrefs.SetFloat(key, m_value);
			changed = false;
		}
	}

    [Obsolete]
	public class REditorPrefDateTime : REditorPref
	{
		private DateTime m_value;
		public DateTime Value { get => m_value; set => m_value = value; }
		public REditorPrefDateTime(string pKey, DateTime pDefault) : base(pKey)
		{
			m_value = pDefault;

			string dateStr = EditorPrefs.GetString(key);
			if (DateTime.TryParse(dateStr, out DateTime date))
				m_value = date;
		}
		public override string ToString()
		{
			return m_value.ToString();
		}
		public override void SaveChange()
		{
			EditorPrefs.SetString(key, m_value.ToString());
		}
	}

	public class REditorPrefString : REditorPref
	{
		private string m_value;
		public string Value
		{
			get => m_value;
			set
			{
				if (m_value == value)
					return;
				m_value = value;
				changed = true;
			}
		}

		public REditorPrefString(string pKey, string pDefault = "") : base(pKey)
		{
			m_value = EditorPrefs.GetString(pKey, pDefault);
		}
		public override string ToString()
		{
			return m_value;
		}
		public override void SaveChange()
		{
			if (!changed)
				return;
			EditorPrefs.SetString(key, m_value);
			changed = false;
		}
	}

	public class REditorPrefList<T> : REditorPref
	{
		private List<T> m_values;
		public REditorPrefList(string pKey, List<T> pDefaultValues = null) : base(pKey)
		{
			m_values = pDefaultValues;

			if (EditorPrefs.HasKey(key))
				m_values = JsonHelper.ToList<T>(EditorPrefs.GetString(key));
		}
		public List<T> Values
		{
			get => m_values;
			set
			{
				m_values = value;
				changed = true;
			}
		}
		public T this[int index]
		{
			get => m_values[index];
			set
			{
				m_values[index] = value;
				changed = true;
			}
		}
		public void Add(T pValue)
		{
			m_values.Add(pValue);
			changed = true;
		}
		public void TryAdd(T pValue)
		{
			if (Contain(pValue))
				return;
			Add(pValue);
			changed = true;
		}
		public void AddRange(params T[] pValues)
		{
			m_values.AddRange(pValues);
			changed = true;
		}
		public void AddRange(List<T> pValues)
		{
			m_values.AddRange(pValues);
			changed = true;
		}
		public void Remove(T pValue)
		{
			if (m_values.Remove(pValue))
				changed = true;
		}
		public bool Contain(T value)
		{
			return m_values.Contains(value);
		}
		public void RemoveAt(int pIndex)
		{
			m_values.RemoveAt(pIndex);
			changed = true;
		}
		public override void SaveChange()
		{
			if (m_values == null || m_values.Count == 0)
			{
				EditorPrefs.DeleteKey(key);
				return;
			}
			if (!changed)
				return;
			EditorPrefs.SetString(key, JsonHelper.ToJson(m_values));
			changed = false;
		}
		public void Clear()
		{
			m_values.Clear();
			changed = true;
		}
	}

	public class REditorPrefDict<TKey, TVal> : REditorPref
	{
		private Dictionary<TKey, TVal> m_values;

		public REditorPrefDict(string pKey, Dictionary<TKey, TVal> pDefaultValues = null) : base(pKey)
		{
			m_values = pDefaultValues;

			if (EditorPrefs.HasKey(key))
				m_values = JsonConvert.DeserializeObject<Dictionary<TKey, TVal>>(EditorPrefs.GetString(key));

			m_values ??= new Dictionary<TKey, TVal>();
		}

		public Dictionary<TKey, TVal> Values { get => m_values; set => m_values = value; }

		public TVal this[TKey index] { get => m_values[index]; set => m_values[index] = value; }

		public void Add(TKey pKey, TVal pVal)
		{
			m_values[pKey] = pVal;
			changed = true;
		}

		public void Remove(TKey pValue)
		{
			if (m_values.Remove(pValue))
				changed = true;
		}

		public bool Contain(TKey value)
		{
			return m_values.ContainsKey(value);
		}

		public override void SaveChange()
		{
			if (m_values.Count == 0)
			{
				EditorPrefs.DeleteKey(key);
				return;
			}
			if (!changed)
				return;
			EditorPrefs.SetString(key, JsonConvert.SerializeObject(m_values));
			changed = false;
		}

		public void Clear()
		{
			m_values.Clear();
			changed = true;
		}
	}
	
	public class REditorPrefObject<T> : REditorPref
	{
		public T value;
		public REditorPrefObject(string pKey, T pDefault) : base(pKey)
		{
			value = pDefault;
			key = pKey;

			if (EditorPrefs.HasKey(key))
			{
				var val = EditorPrefs.GetString(key);
				try
				{
					value = JsonConvert.DeserializeObject<T>(val);
				}
				catch
				{
					value = pDefault;
				}
			}
		}
		public override void SaveChange()
		{
			var json = JsonConvert.SerializeObject(value, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore,
				DefaultValueHandling = DefaultValueHandling.Ignore,
			});
			EditorPrefs.SetString(key, json);
		}
	}

	public class REditorPrefSerializableObject<T> : REditorPref
	{
		public T value;
        private bool m_encrypted;
		public REditorPrefSerializableObject(string pKey, bool pEncrypted, T pDefault) : base(pKey)
		{
            m_encrypted = pEncrypted;
			value = pDefault;
			key = pEncrypted ? Encryption.Singleton.Encrypt(pKey) : pKey;

			if (EditorPrefs.HasKey(key))
			{
				var val = EditorPrefs.GetString(key);
				try
				{
					string content = pEncrypted ? Encryption.Singleton.Decrypt(val) : val;
					value = JsonUtility.FromJson<T>(content);
				}
				catch
				{
					value = pDefault;
				}
			}
		}
		public override void SaveChange()
		{
            var json = JsonUtility.ToJson(value);
            if (m_encrypted)
                json = Encryption.Singleton.Encrypt(json);
            EditorPrefs.SetString(key, json);
		}
	}
}