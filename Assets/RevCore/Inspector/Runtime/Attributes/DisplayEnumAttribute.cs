using System;
using UnityEngine;

namespace RevCore
{
	/// <summary>
	/// On an <see cref="int"/> field, displays the value as if it were an enum: either by direct
	/// reference to an enum <see cref="Type"/>, or by reflecting the enum to use from a method on
	/// the owning object via <see cref="MethodName"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	public sealed class DisplayEnumAttribute : PropertyAttribute
	{
		/// <summary>The enum type to display, when bound at compile time.</summary>
		public Type EnumType { get; private set; }

		/// <summary>Name of the method on the owning object whose return value supplies the enum type at runtime.</summary>
		public string MethodName { get; private set; }

		/// <summary>Fixed-type variant.</summary>
		/// <exception cref="ArgumentException"><paramref name="enumType"/> is not an enum.</exception>
		public DisplayEnumAttribute(Type enumType)
		{
			if (!enumType.IsEnum)
				throw new ArgumentException("Type must be an enum.", nameof(enumType));

			EnumType = enumType;
		}

		/// <summary>Dynamic-type variant: <paramref name="methodName"/> is invoked on the owning object to discover the enum at runtime.</summary>
		public DisplayEnumAttribute(string methodName)
		{
			MethodName = methodName;
		}
	}
}
