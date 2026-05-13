using System;
using UnityEngine;

namespace RevCore
{
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class CommentAttribute : PropertyAttribute
	{
		public readonly GUIContent content;

		public CommentAttribute(string comment, string tooltip = "")
		{
			content = string.IsNullOrEmpty(tooltip)
				? new GUIContent(comment)
				: new GUIContent(comment + " [?]", tooltip);
		}
	}
}
