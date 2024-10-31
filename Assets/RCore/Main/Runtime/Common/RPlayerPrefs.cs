using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RCore
{
	public static class RPlayerPrefContainer
	{
		private static readonly List<RPlayerPref> m_RPlayerPrefs = new List<RPlayerPref>();
		public static void DeleteAll()
		{
			for (int i = 0; i < m_RPlayerPrefs.Count; i++)
				m_RPlayerPrefs[i].Delete();
		}
		public static void Register(RPlayerPref pChange)
        {
            for (int i = 0; i < m_RPlayerPrefs.Count; i++)
            {
                if (m_RPlayerPrefs[i].key == pChange.key)
                {
                    m_RPlayerPrefs[i] = pChange;
                    return;
                }
            }
            m_RPlayerPrefs.Add(pChange);
        }
		public static void SaveChanges()
		{
			for (int i = 0; i < m_RPlayerPrefs.Count; i++)
				m_RPlayerPrefs[i].SaveChange();
		}
	}

	public abstract class RPlayerPref
	{
		public string key;
		protected bool changed;
		protected RPlayerPref(string pKey)
		{
			key = pKey;
			changed = false;
			RPlayerPrefContainer.Register(this);
		}
		public void Delete()
		{
			PlayerPrefs.DeleteKey(key);
		}
		public abstract void SaveChange();
	}

	public class RPlayerPrefBool : RPlayerPref
	{
		public Action onUpdated;
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
				onUpdated?.Invoke();
			}
		}
		public RPlayerPrefBool(string pKey, bool pDefault = false) : base(pKey)
		{
			m_value = PlayerPrefs.GetInt(pKey, pDefault ? 1 : 0) == 1;
		}
		public override string ToString()
		{
			return m_value.ToString();
		}
		public override void SaveChange()
		{
			if (!changed)
				return;
			PlayerPrefs.SetInt(key, m_value ? 1 : 0);
			changed = false;
		}
	}

	public class RPlayerPrefInt : RPlayerPref
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
		public RPlayerPrefInt(string pKey, int pDefault = 0) : base(pKey)
		{
			m_value = PlayerPrefs.GetInt(pKey, pDefault);
		}
		public override string ToString()
		{
			return m_value.ToString();
		}
		public override void SaveChange()
		{
			if (!changed)
				return;
			PlayerPrefs.SetInt(key, m_value);
			changed = false;
		}
	}

	public class RPlayerPrefFloat : RPlayerPref
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
		public RPlayerPrefFloat(string pKey, float pDefault = 0) : base(pKey)
		{
			m_value = PlayerPrefs.GetFloat(pKey, pDefault);
		}
		public override string ToString()
		{
			return m_value.ToString();
		}
		public override void SaveChange()
		{
			if (!changed)
				return;
			PlayerPrefs.SetFloat(key, m_value);
			changed = false;
		}
	}

    [Obsolete]
	public class RPlayerPrefDateTime : RPlayerPref
	{
		private DateTime m_value;
		public DateTime Value { get => m_value; set => m_value = value; }
		public RPlayerPrefDateTime(string pKey, DateTime pDefault) : base(pKey)
		{
			m_value = pDefault;

			string dateStr = PlayerPrefs.GetString(key);
			if (DateTime.TryParse(dateStr, out DateTime date))
				m_value = date;
		}
		public override string ToString()
		{
			return m_value.ToString();
		}
		public override void SaveChange()
		{
			PlayerPrefs.SetString(key, m_value.ToString());
		}
	}

	public class RPlayerPrefString : RPlayerPref
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

		public RPlayerPrefString(string pKey, string pDefault = "") : base(pKey)
		{
			m_value = PlayerPrefs.GetString(pKey, pDefault);
		}
		public override string ToString()
		{
			return m_value;
		}
		public override void SaveChange()
		{
			if (!changed)
				return;
			PlayerPrefs.SetString(key, m_value);
			changed = false;
		}
	}

	public class RPlayerPrefList<T> : RPlayerPref
	{
		private List<T> m_values;
		public RPlayerPrefList(string pKey, List<T> pDefaultValues = null) : base(pKey)
		{
			m_values = pDefaultValues;

			if (PlayerPrefs.HasKey(key))
				m_values = JsonHelper.ToList<T>(PlayerPrefs.GetString(key));
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
				PlayerPrefs.DeleteKey(key);
				return;
			}
			if (!changed)
				return;
			PlayerPrefs.SetString(key, JsonHelper.ToJson(m_values));
			changed = false;
		}
		public void Clear()
		{
			m_values.Clear();
			changed = true;
		}
	}

	public class RPlayerPrefDict<TKey, TVal> : RPlayerPref
	{
		private Dictionary<TKey, TVal> m_values;

		public RPlayerPrefDict(string pKey, Dictionary<TKey, TVal> pDefaultValues = null) : base(pKey)
		{
			m_values = pDefaultValues;

			if (PlayerPrefs.HasKey(key))
				m_values = JsonConvert.DeserializeObject<Dictionary<TKey, TVal>>(PlayerPrefs.GetString(key));

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
				PlayerPrefs.DeleteKey(key);
				return;
			}
			if (!changed)
				return;
			PlayerPrefs.SetString(key, JsonConvert.SerializeObject(m_values));
			changed = false;
		}

		public void Clear()
		{
			m_values.Clear();
			changed = true;
		}
	}
	
	public class RPlayerPrefObject<T> : RPlayerPref
	{
		public T value;
		private bool m_encrypted;
		public RPlayerPrefObject(string pKey, bool pEncrypted, T pDefault) : base(pKey)
		{
			m_encrypted = pEncrypted;
			value = pDefault;
			key = pEncrypted ? Encryption.Singleton.Encrypt(pKey) : pKey;

			if (PlayerPrefs.HasKey(key))
			{
				var val = PlayerPrefs.GetString(key);
				try
				{
					string content = pEncrypted ? Encryption.Singleton.Decrypt(val) : val;
					value = JsonConvert.DeserializeObject<T>(content);
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
			if (m_encrypted)
				json = Encryption.Singleton.Encrypt(json);
			PlayerPrefs.SetString(key, json);
		}
	}

	public class RPlayerPrefSerializableObject<T> : RPlayerPref
	{
		public T value;
        private bool m_encrypted;
		public RPlayerPrefSerializableObject(string pKey, bool pEncrypted, T pDefault) : base(pKey)
		{
            m_encrypted = pEncrypted;
			value = pDefault;
			key = pEncrypted ? Encryption.Singleton.Encrypt(pKey) : pKey;

			if (PlayerPrefs.HasKey(key))
			{
				var val = PlayerPrefs.GetString(key);
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
            PlayerPrefs.SetString(key, json);
		}
	}
}