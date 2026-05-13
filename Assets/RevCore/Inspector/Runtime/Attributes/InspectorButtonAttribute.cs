using System;

namespace RevCore
{
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class InspectorButtonAttribute : Attribute
	{
		public string Label { get; }

		public InspectorButtonAttribute(string label = null)
		{
			Label = label;
		}
	}
}
