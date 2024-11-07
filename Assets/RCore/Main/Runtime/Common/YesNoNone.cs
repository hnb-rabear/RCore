using System;

namespace RCore
{
	public enum YesNoNone
	{
		None = 0,
		No = 1,
		Yes = 2,
	}
	
	public enum PerfectRatio
	{
		None,
		Width,
		Height,
	}

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
	}
}