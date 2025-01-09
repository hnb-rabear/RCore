using System;
using UnityEngine;

namespace RCore.Inspector
{
	[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	public class DisplayEnumAttribute : PropertyAttribute
	{
		public Type EnumType { get; private set; }
		public string MethodName { get; private set; }

		public DisplayEnumAttribute(Type enumType)
		{
			if (!enumType.IsEnum)
			{
				throw new ArgumentException("EnumType must be an enum type.");
			}
			EnumType = enumType;
		}

		public DisplayEnumAttribute(string methodName)
		{
			MethodName = methodName;
		}
	}
}