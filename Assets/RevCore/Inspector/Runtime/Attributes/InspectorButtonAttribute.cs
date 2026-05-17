using System;

namespace RevCore
{
	/// <summary>
	/// On a method, renders a button in the inspector that invokes the method when clicked. The
	/// method must be public and parameterless on the inspected component. Implemented by a custom
	/// editor that scans the inspected type.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class InspectorButtonAttribute : Attribute
	{
		/// <summary>Override label for the button. <c>null</c> uses the method name.</summary>
		public string Label { get; }

		/// <summary>Creates the attribute with an optional <paramref name="label"/>.</summary>
		public InspectorButtonAttribute(string label = null)
		{
			Label = label;
		}
	}
}
