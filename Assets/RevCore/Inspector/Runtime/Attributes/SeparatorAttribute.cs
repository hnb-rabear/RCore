using UnityEngine;

namespace RevCore
{
	/// <summary>
	/// Draws a horizontal separator line (and optional title) above a field, grouping fields into
	/// visual sections in the inspector. Implemented by an editor-side property drawer.
	/// </summary>
	public sealed class SeparatorAttribute : PropertyAttribute
	{
		/// <summary>Optional title displayed in the separator. Empty for a plain line.</summary>
		public readonly string title;

		/// <summary>Creates a plain (untitled) separator.</summary>
		public SeparatorAttribute() : this("") { }

		/// <summary>Creates a separator with the given <paramref name="title"/>.</summary>
		public SeparatorAttribute(string title)
		{
			this.title = title;
		}
	}
}
