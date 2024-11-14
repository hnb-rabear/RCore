/***
 * Author HNB-RaBear - 2018
 **/

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace RCore.Data.KeyValue
{
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="T">T is serializable object</typeparam>
	public class ObjectData<T> : FunData where T : IComparable<T>
	{
		private T m_Value;
		private T m_DefaultValue;
		private T m_CompareValue; //If the object T is changed inside it
		private bool m_Changed;

		public T Value
		{
			get => m_Value != null ? m_Value : m_DefaultValue;
			set
			{
				if (m_Value != null && value == null || m_Value == null && value != null || m_Value.CompareTo(value) != 0)
				{
					m_Value = value;
					m_CompareValue = Clone(value);
					m_Changed = true;
				}
			}
		}

		public ObjectData(int pId, T pDefaultValue) : base(pId)
		{
			m_DefaultValue = pDefaultValue;
		}

		public override void Load(string pBaseKey, string pSaverIdString)
		{
			base.Load(pBaseKey, pSaverIdString);

			m_Value = GetSavedValue();
		}

		public override bool Stage()
		{
			if (m_Value != null && m_CompareValue == null || m_Value == null && m_CompareValue != null || m_Changed || m_Value.CompareTo(m_CompareValue) != 0)
			{
				var saveStr = JsonUtility.ToJson(m_Value);
				SetStringValue(saveStr);
				m_CompareValue = Clone(Value);
				m_Changed = false;
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
					m_DefaultValue;
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.ToString());

				Value = m_DefaultValue;
				return m_DefaultValue;
			}
		}

		private T Clone(T source)
		{
			if (!typeof(T).IsSerializable)
				throw new ArgumentException("The type must be serializable.", "source");

			// Don't serialize a null object, simply return the default for that object
			if (ReferenceEquals(source, null))
				return default;

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
			m_Value = GetSavedValue();
			m_Changed = false;
		}

		public override void Reset()
		{
			Value = m_DefaultValue;
		}

		public override bool Cleanable()
		{
			return false;
		}
	}
}