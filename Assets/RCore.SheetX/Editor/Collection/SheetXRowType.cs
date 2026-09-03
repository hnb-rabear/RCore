/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;

namespace RCore.SheetX.Editor
{
	/// <summary>
	/// The single definition of a valid <c>Existing Data Class</c> row type. The Data Class
	/// picker, the export session, and the collection baker all validate through here, so a
	/// type offered in the picker is exactly a type that exports and bakes.
	/// </summary>
	internal static class SheetXRowType
	{
		/// <summary>
		/// Checks whether <paramref name="type"/> may back a sheet, and explains the fix when it may not.
		/// </summary>
		/// <param name="type">The candidate row type. <c>null</c> is rejected, not thrown on.</param>
		/// <param name="error">Null when valid; otherwise a message naming the type and the required edit.</param>
		/// <returns><c>true</c> when the type is a publicly visible, concrete, non-generic, [Serializable] and [SheetXBindable] class or struct.</returns>
		internal static bool Validate(Type type, out string error)
		{
			error = null;
			if (type == null)
			{
				error = "row type was not found in any loaded assembly.";
				return false;
			}

			// The two attribute rules come first: a developer migrating an existing binding
			// should be told to add the marker, not lectured about a shape they never chose.
			if (!type.IsDefined(typeof(SheetXBindableAttribute), inherit: false))
			{
				error = $"row type '{type.FullName}' is missing [SheetXBindable]. "
					+ $"Fix: add [RCore.SheetX.SheetXBindable] to {type.Name}.";
				return false;
			}
			// [Serializable] is a pseudo-custom attribute the compiler emits as the
			// TypeAttributes.Serializable metadata flag, so IsDefined never sees it.
			// Type.IsSerializable reads the flag.
			if (!type.IsSerializable)
			{
				error = $"row type '{type.FullName}' is missing [Serializable]. "
					+ $"Fix: add [System.Serializable] to {type.Name} so Unity serializes the baked array.";
				return false;
			}
			if (type.IsEnum)
			{
				error = $"row type '{type.FullName}' must be a class or struct, not an enum.";
				return false;
			}
			if (!type.IsClass && !type.IsValueType)
			{
				error = $"row type '{type.FullName}' must be a concrete class or struct.";
				return false;
			}
			// Structs report IsAbstract false, so this covers abstract and static classes only.
			if (type.IsAbstract)
			{
				error = $"row type '{type.FullName}' must be concrete, not abstract.";
				return false;
			}
			if (type.IsGenericType || type.IsGenericTypeDefinition)
			{
				error = $"row type '{type.FullName}' must not be generic.";
				return false;
			}
			// IsVisible is true only when the type AND every enclosing type are public, which is
			// exactly what the generated `public <RowType>[] field;` needs. An internal type fails
			// even in the same assembly: a public field of an internal type is CS0052.
			if (!type.IsVisible)
			{
				error = $"row type '{type.FullName}' is not publicly visible. "
					+ $"Fix: make {type.Name} public, along with every type it is nested in, so the generated collection field can reference it.";
				return false;
			}
			return true;
		}
	}
}
