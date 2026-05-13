using UnityEngine;

namespace RevCore
{
	public sealed class SeparatorAttribute : PropertyAttribute
	{
		public readonly string title;

		public SeparatorAttribute() : this("") { }

		public SeparatorAttribute(string title)
		{
			this.title = title;
		}
	}
}
