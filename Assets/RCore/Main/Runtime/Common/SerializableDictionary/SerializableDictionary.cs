// Author: https://github.com/EduardMalkhasyan/Serializable-Dictionary-Unity

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCore
{
	[System.Serializable]
	public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
	{
		[SerializeField] private List<SerializedDictionaryKVPProps<TKey, TValue>> dictionaryList = new();

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			foreach (var kVP in this)
			{
				if (dictionaryList.FirstOrDefault(value => this.Comparer.Equals(value.k, kVP.Key))
				    is SerializedDictionaryKVPProps<TKey, TValue> serializedKVP)
				{
					serializedKVP.v = kVP.Value;
				}
				else
				{
					dictionaryList.Add(kVP);
				}
			}

			dictionaryList.RemoveAll(value => ContainsKey(value.k) == false);

			for (int i = 0; i < dictionaryList.Count; i++)
			{
				dictionaryList[i].index = i;
			}
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			Clear();

			dictionaryList.RemoveAll(r => r.k == null);

			foreach (var serializedKVP in dictionaryList)
			{
				if (!(serializedKVP.isKeyDuplicated = ContainsKey(serializedKVP.k)))
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
					var kvp = dictionaryList.FirstOrDefault(kvp => this.Comparer.Equals(kvp.k, key));
					if (kvp != null)
					{
						kvp.v = value;
					}
				}
				else
				{
					Add(key, value);
					dictionaryList.Add(new SerializedDictionaryKVPProps<TKey, TValue>(key, value));
				}
			}
			get
			{
#if UNITY_EDITOR
				if (ContainsKey(key))
				{
					var duplicateKeysWithCount = dictionaryList.GroupBy(item => item.k)
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
		public class SerializedDictionaryKVPProps<TypeKey, TypeValue>
		{
			public TypeKey k;
			public TypeValue v;

			public int index;
			public bool isKeyDuplicated;

			public SerializedDictionaryKVPProps(TypeKey key, TypeValue value)
			{
				k = key;
				v = value;
			}

			public static implicit operator SerializedDictionaryKVPProps<TypeKey, TypeValue>(KeyValuePair<TypeKey, TypeValue> kvp)
				=> new SerializedDictionaryKVPProps<TypeKey, TypeValue>(kvp.Key, kvp.Value);
			public static implicit operator KeyValuePair<TypeKey, TypeValue>(SerializedDictionaryKVPProps<TypeKey, TypeValue> kvp)
				=> new KeyValuePair<TypeKey, TypeValue>(kvp.k, kvp.v);
		}
	}
}