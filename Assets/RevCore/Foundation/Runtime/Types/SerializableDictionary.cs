using System;
using System.Collections.Generic;
using UnityEngine;

namespace RevCore
{
	/// <summary>
	/// <see cref="Dictionary{TKey,TValue}"/> that survives Unity's serialization roundtrip. Backed by a
	/// <see cref="List{T}"/> of <see cref="SerializedDictionary{TKey,TValue}"/> entries that Unity can
	/// serialize. Use this for inspector-editable map data.
	/// </summary>
	/// <remarks>
	/// On deserialize, null keys are dropped with a warning; duplicate keys keep the first occurrence and
	/// mark subsequent ones via <see cref="SerializedDictionary{TKey,TValue}.keyDuplicated"/> so a custom
	/// property drawer can surface the conflict to the user.
	/// </remarks>
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

		/// <summary>
		/// Indexer that returns <c>default(TValue)</c> for missing keys instead of throwing
		/// <see cref="KeyNotFoundException"/>. Use base <see cref="Dictionary{TKey,TValue}.TryGetValue"/>
		/// when you need to distinguish "key absent" from "key present with default value".
		/// </summary>
		public new TValue this[TKey key]
		{
			get => TryGetValue(key, out var value) ? value : default;
			set => base[key] = value;
		}
	}

	/// <summary>
	/// Serializable key-value entry used internally by <see cref="SerializableDictionary{TKey,TValue}"/>.
	/// Field names are kept short (<c>k</c>, <c>v</c>) to match the conventional inspector layout.
	/// </summary>
	[Serializable]
	public class SerializedDictionary<TKey, TValue>
	{
		/// <summary>The key.</summary>
		public TKey k;

		/// <summary>The value.</summary>
		public TValue v;

		/// <summary>Index of this entry in the backing list — used by drawers to render stable row numbers.</summary>
		public int index;

		/// <summary>Set by deserialization when this entry's key clashes with an earlier one. UI can flag it.</summary>
		public bool keyDuplicated;

		/// <summary>Empty constructor required by Unity serialization.</summary>
		public SerializedDictionary() { }

		/// <summary>Creates an entry with the given key, value, and ordinal index.</summary>
		public SerializedDictionary(TKey key, TValue value, int index = 0)
		{
			k = key;
			v = value;
			this.index = index;
		}

		/// <summary>Implicit conversion from a runtime <see cref="KeyValuePair{TKey,TValue}"/>.</summary>
		public static implicit operator SerializedDictionary<TKey, TValue>(KeyValuePair<TKey, TValue> kvp)
			=> new(kvp.Key, kvp.Value);

		/// <summary>Implicit conversion back to a runtime <see cref="KeyValuePair{TKey,TValue}"/>.</summary>
		public static implicit operator KeyValuePair<TKey, TValue>(SerializedDictionary<TKey, TValue> kvp)
			=> new(kvp.k, kvp.v);
	}
}
