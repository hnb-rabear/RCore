using System;
using UnityEngine;

namespace RevCore
{
	[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	public sealed class DisplayEnumAttribute : PropertyAttribute
	{
		public Type EnumType { get; private set; }
		public string MethodName { get; private set; }

		public DisplayEnumAttribute(Type enumType)
		{
			if (!enumType.IsEnum)
				throw new ArgumentException("Type must be an enum.", nameof(enumType));

			EnumType = enumType;
		}

		public DisplayEnumAttribute(string methodName)
		{
			MethodName = methodName;
		}
	}
}
