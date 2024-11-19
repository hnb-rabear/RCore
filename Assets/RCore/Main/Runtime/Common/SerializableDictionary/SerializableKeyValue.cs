/***
 * Author HNB-RaBear - 2024
 */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace RCore
{
	[Serializable]
	public class SerializableKeyValue<TKey, TValue>
	{
		public TKey k;
		public TValue v;
		[JsonIgnore] public TKey Key => k;
		[JsonIgnore] public TValue Value { get => v; set => v = value; }
		public SerializableKeyValue() { }
		public SerializableKeyValue(TKey pKey, TValue pValue)
		{
			k = pKey;
			v = pValue;
		}
		public static implicit operator SerializableKeyValue<TKey, TValue>(KeyValuePair<TKey, TValue> kvp)
			=> new(kvp.Key, kvp.Value);
		public static implicit operator KeyValuePair<TKey, TValue>(SerializableKeyValue<TKey, TValue> kvp)
			=> new(kvp.k, kvp.v);
	}
}