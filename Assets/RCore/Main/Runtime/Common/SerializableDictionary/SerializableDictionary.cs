using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace RCore
{
	[System.Serializable]
	public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
	{
		[SerializeField] private List<SerializedDictionary<TKey, TValue>> keyValues = new();

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			foreach (var kVP in this)
			{
				if (keyValues.FirstOrDefault(value => this.Comparer.Equals(value.k, kVP.Key))
				    is SerializedDictionary<TKey, TValue> serializedKVP)
				{
					serializedKVP.v = kVP.Value;
				}
				else
				{
					keyValues.Add(kVP);
				}
			}

			keyValues.RemoveAll(value => ContainsKey(value.k) == false);

			for (int i = 0; i < keyValues.Count; i++)
			{
				keyValues[i].index = i;
			}
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			Clear();

			keyValues.RemoveAll(r => r.k == null);

			foreach (var serializedKVP in keyValues)
			{
				if (!(serializedKVP.keyDuplicated = ContainsKey(serializedKVP.k)))
				{
					Add(serializedKVP.k, serializedKVP.v);
				}
			}
		}

		public new TValue this[TKey key]
		{
			set
			{
				if (ContainsKey(key))
				{
					base[key] = value;
					var kvp = keyValues.FirstOrDefault(kvp => this.Comparer.Equals(kvp.k, key));
					if (kvp != null)
					{
						kvp.v = value;
					}
				}
				else
				{
					Add(key, value);
					keyValues.Add(new SerializedDictionary<TKey, TValue>(key, value));
				}
			}
			get
			{
#if UNITY_EDITOR
				if (ContainsKey(key))
				{
					var duplicateKeysWithCount = keyValues.GroupBy(item => item.k)
						.Where(group => group.Count() > 1)
						.Select(group => new { Key = group.Key, Count = group.Count() });

					foreach (var duplicatedKey in duplicateKeysWithCount)
					{
						Debug.LogError($"Key '{duplicatedKey.Key}' is duplicated {duplicatedKey.Count} times in the dictionary.");
					}

					return base[key];
				}
				else
				{
					Debug.LogError($"Key '{key}' not found in dictionary.");
					return default(TValue);
				}
#else
                return base[key];
#endif
			}
		}

		[System.Serializable]
		public class SerializedDictionary<TypeKey, TypeValue> : SerializableKeyValue<TypeKey, TypeValue>
		{
			public int index;
			public bool keyDuplicated;

			public SerializedDictionary(TypeKey key, TypeValue value) : base(key, value) { }

			public static implicit operator SerializedDictionary<TypeKey, TypeValue>(KeyValuePair<TypeKey, TypeValue> kvp)
				=> new(kvp.Key, kvp.Value);
			public static implicit operator KeyValuePair<TypeKey, TypeValue>(SerializedDictionary<TypeKey, TypeValue> kvp)
				=> new(kvp.k, kvp.v);
		}
	}
}