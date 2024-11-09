/**
* Author RaBear - HNB - 2024
**/

using System;
using System.Collections.Generic;

namespace RCore
{
	[Serializable]
	public class SerializableKeyValue<TypeKey, TypeValue>
	{
		public TypeKey k;
		public TypeValue v;
		public SerializableKeyValue() { }
		public SerializableKeyValue(TypeKey pKey, TypeValue pValue)
		{
			k = pKey;
			v = pValue;
		}
		public static implicit operator SerializableKeyValue<TypeKey, TypeValue>(KeyValuePair<TypeKey, TypeValue> kvp)
			=> new(kvp.Key, kvp.Value);
		public static implicit operator KeyValuePair<TypeKey, TypeValue>(SerializableKeyValue<TypeKey, TypeValue> kvp)
			=> new(kvp.k, kvp.v);
	}
}