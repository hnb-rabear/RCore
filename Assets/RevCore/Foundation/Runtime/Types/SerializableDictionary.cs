using System;
using System.Collections.Generic;
using UnityEngine;

namespace RevCore
{
	[Serializable]
	public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
	{
		[SerializeField] private List<SerializedDictionary<TKey, TValue>> m_keyValues = new();

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			m_keyValues.Clear();
			int index = 0;
			foreach (var kvp in this)
			{
				m_keyValues.Add(new SerializedDictionary<TKey, TValue>(kvp.Key, kvp.Value, index));
				index++;
			}
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			Clear();

			for (int i = 0; i < m_keyValues.Count; i++)
			{
				var item = m_keyValues[i];
				if (item.k == null)
				{
					Debug.LogWarning("SerializableDictionary dropped null key during deserialize.");
					continue;
				}

				item.keyDuplicated = ContainsKey(item.k);
				if (!item.keyDuplicated)
					Add(item.k, item.v);
			}
		}

		public new TValue this[TKey key]
		{
			get => TryGetValue(key, out var value) ? value : default;
			set => base[key] = value;
		}
	}

	[Serializable]
	public class SerializedDictionary<TKey, TValue>
	{
		public TKey k;
		public TValue v;
		public int index;
		public bool keyDuplicated;

		public SerializedDictionary() { }

		public SerializedDictionary(TKey key, TValue value, int index = 0)
		{
			k = key;
			v = value;
			this.index = index;
		}

		public static implicit operator SerializedDictionary<TKey, TValue>(KeyValuePair<TKey, TValue> kvp)
			=> new(kvp.Key, kvp.Value);

		public static implicit operator KeyValuePair<TKey, TValue>(SerializedDictionary<TKey, TValue> kvp)
			=> new(kvp.k, kvp.v);
	}
}
