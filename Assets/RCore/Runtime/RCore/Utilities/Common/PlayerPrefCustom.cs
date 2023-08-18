using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RCore.Common
{
	public class PlayerPrefCustom
	{
		public Action onUpdated;
		protected string m_Key;
		public PlayerPrefCustom(string pKey)
		{
			m_Key = pKey;
		}
	}

	public class PlayerPrefBool : PlayerPrefCustom
	{
		private bool m_Value;
		public bool Value
		{
			get => m_Value;
			set
			{
				if (m_Value == value)
					return;
				m_Value = value;
				PlayerPrefs.SetInt(m_Key, value ? 1 : 0);
				onUpdated?.Invoke();
			}
		}
		public PlayerPrefBool(string pKey, bool pDefault = false) : base(pKey)
		{
			m_Value = PlayerPrefs.GetInt(pKey, pDefault ? 1 : 0) == 1;
		}
		public override string ToString()
		{
			return m_Value.ToString();
		}
	}

	public class PlayerPrefInt : PlayerPrefCustom
	{
		private int m_Value;
		public int Value
		{
			get => m_Value;
			set
			{
				if (m_Value == value)
					return;
				m_Value = value;
				PlayerPrefs.SetInt(m_Key, value);
				onUpdated?.Invoke();
			}
		}
		public PlayerPrefInt(string pKey, int pDefault = 0) : base(pKey)
		{
			m_Value = PlayerPrefs.GetInt(pKey, pDefault);
		}
		public override string ToString()
		{
			return m_Value.ToString();
		}
	}

	public class PlayerPrefFloat : PlayerPrefCustom
	{
		private float m_Value;
		public float Value
		{
			get => m_Value;
			set
			{
				if (m_Value == value)
					return;
				m_Value = value;
				PlayerPrefs.SetFloat(m_Key, value);
				onUpdated?.Invoke();
			}
		}
		public PlayerPrefFloat(string pKey, float pDefault = 0) : base(pKey)
		{
			m_Value = PlayerPrefs.GetFloat(pKey, pDefault);
		}
		public override string ToString()
		{
			return m_Value.ToString();
		}
	}

	public class PlayerPrefDateTime : PlayerPrefCustom
	{
		private DateTime m_Value;
		public DateTime Value
		{
			get => m_Value;
			set
			{
				if (m_Value == value)
					return;
				m_Value = value;
				PlayerPrefs.SetString(m_Key, value.ToString());
				onUpdated?.Invoke();
			}
		}
		public PlayerPrefDateTime(string pKey, DateTime pDefault) : base(pKey)
		{
			string dateStr = PlayerPrefs.GetString(m_Key);
			if (DateTime.TryParse(dateStr, out var date))
				m_Value = date;
			else
				m_Value = pDefault;
		}
		public override string ToString()
		{
			return m_Value.ToString();
		}
	}

	public class PlayerPrefString : PlayerPrefCustom
	{
		private string m_Value;
		public string Value
		{
			get => m_Value;
			set
			{
				if (m_Value == value)
					return;
				m_Value = value;
				PlayerPrefs.SetString(m_Key, value);
				onUpdated?.Invoke();
			}
		}
		public PlayerPrefString(string pKey, string pDefault = "") : base(pKey)
		{
			m_Value = PlayerPrefs.GetString(pKey, pDefault);
		}
		public override string ToString()
		{
			return m_Value;
		}
	}

	public class PlayerPrefList<T> : PlayerPrefCustom
	{
		private List<T> m_Values = new List<T>();

		public PlayerPrefList(string pKey, List<T> pDefaultValues = null) : base(pKey)
		{
			if (!PlayerPrefs.HasKey(m_Key))
				m_Values = pDefaultValues;
			else
				m_Values = JsonHelper.ToList<T>(PlayerPrefs.GetString(m_Key));
		}

		public List<T> Values
		{
			get => m_Values;
			set => m_Values = value;
		}

		public T this[int index]
		{
			get => m_Values[index];
			set => m_Values[index] = value;
		}

		public void Add(T pValue)
		{
			m_Values.Add(pValue);
			onUpdated?.Invoke();
		}

		public void AddRange(params T[] pValues)
		{
			m_Values.AddRange(pValues);
			onUpdated?.Invoke();
		}

		public void AddRange(List<T> pValues)
		{
			m_Values.AddRange(pValues);
			onUpdated?.Invoke();
		}

		public void Remove(T pValue)
		{
			m_Values.Remove(pValue);
			onUpdated?.Invoke();
		}

		public bool Contain(T value)
		{
			return m_Values.Contains(value);
		}

		public void RemoveAt(int pIndex)
		{
			m_Values.RemoveAt(pIndex);
			onUpdated?.Invoke();
		}

		public void SaveChange()
		{
			PlayerPrefs.SetString(m_Key, JsonHelper.ToJson(m_Values));
		}
	}

	public class PlayerPrefObject<T> : PlayerPrefCustom
	{
		private T m_Value;
		public T Value
		{
			get => m_Value;
			set
			{
				m_Value = value;
				if (value != null)
					PlayerPrefs.SetString(m_Key, JsonConvert.SerializeObject(value, new JsonSerializerSettings()
					{
						NullValueHandling = NullValueHandling.Ignore,
						DefaultValueHandling = DefaultValueHandling.Ignore,
					}));
				else
					PlayerPrefs.SetString(m_Key, "");
				onUpdated?.Invoke();
			}
		}
		public PlayerPrefObject(string pKey, T pDefault) : base(pKey)
		{
			string dateStr = PlayerPrefs.GetString(m_Key);
			if (!string.IsNullOrEmpty(dateStr))
				m_Value = JsonConvert.DeserializeObject<T>(dateStr);
			else
				m_Value = pDefault;
		}
		public override string ToString()
		{
			if (m_Value != null)
				return JsonConvert.SerializeObject(m_Value, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore,
					DefaultValueHandling = DefaultValueHandling.Ignore,
				});
			return null;
		}
	}
}