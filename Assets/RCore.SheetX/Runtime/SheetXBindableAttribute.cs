/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;

namespace RCore.SheetX
{
	/// <summary>
	/// Marks a class or struct as eligible to back a sheet exported through
	/// Collections' <c>Existing Data Class</c> output mode. The Data Class picker lists
	/// only types carrying this attribute, and export and bake reject any other type.
	/// </summary>
	/// <remarks>
	/// The type must also be <see cref="SerializableAttribute"/> so Unity serializes the
	/// baked array. This attribute alone is a SheetX marker Unity knows nothing about.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
	public sealed class SheetXBindableAttribute : Attribute
	{
	}
}
