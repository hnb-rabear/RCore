using System;
using UnityEngine;

namespace RevCore
{
	/// <summary>
	/// Renders a help label above a serialized field. When <paramref name="tooltip"/> (the constructor
	/// argument) is non-empty, an inline <c>[?]</c> hover marker shows the tooltip.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class CommentAttribute : PropertyAttribute
	{
		/// <summary>The composed comment GUI content (label + tooltip).</summary>
		public readonly GUIContent content;

		/// <summary>Creates the attribute with a visible <paramref name="comment"/> and optional hover <paramref name="tooltip"/>.</summary>
		public CommentAttribute(string comment, string tooltip = "")
		{
			content = string.IsNullOrEmpty(tooltip)
				? new GUIContent(comment)
				: new GUIContent(comment + " [?]", tooltip);
		}
	}
}
