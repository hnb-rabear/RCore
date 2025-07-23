using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace RCore.Inspector
{
	/// <summary>
	/// An attribute used to display a descriptive comment or note above a field in the Unity Inspector.
	/// This is useful for providing context or instructions for a serialized field.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class CommentAttribute : PropertyAttribute
	{
		/// <summary>
		/// The GUIContent object that holds the comment text and optional tooltip.
		/// </summary>
		public readonly GUIContent content;

		/// <summary>
		/// Initializes a new instance of the CommentAttribute.
		/// </summary>
		/// <param name="comment">The text to be displayed as a comment above the field.</param>
		/// <param name="tooltip">An optional tooltip that appears when the user hovers over the comment.</param>
		public CommentAttribute(string comment, string tooltip = "")
		{
			content = string.IsNullOrEmpty(tooltip) 
				? new GUIContent(comment) 
				: new GUIContent(comment + " [?]", tooltip);
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// A custom drawer that renders the UI for the [Comment] attribute in the Inspector.
	/// It acts as a decorator, drawing the comment text before the actual property field is drawn.
	/// </summary>
	[CustomPropertyDrawer(typeof(CommentAttribute), useForChildren: true)]
	public class CommentDecoratorDrawer : DecoratorDrawer
	{
		/// <summary>
		/// Gets the CommentAttribute instance associated with this drawer.
		/// </summary>
		private CommentAttribute CommentAttribute => (CommentAttribute)attribute;

		/// <summary>
		/// Calculates the vertical height required to draw the comment text.
		/// This allows for multi-line comments that automatically adjust the inspector layout.
		/// </summary>
		public override float GetHeight()
		{
			// Use the "whiteLabel" style for proper word wrapping and height calculation.
			return EditorStyles.whiteLabel.CalcHeight(CommentAttribute.content, Screen.width - 19);
		}

		/// <summary>
		/// Renders the comment text in the Inspector.
		/// </summary>
		/// <param name="position">The rectangle on the screen to draw the comment in.</param>
		public override void OnGUI(Rect position)
		{
			// The comment is drawn as a disabled label to give it a distinct, non-interactive look.
			EditorGUI.BeginDisabledGroup(true);
			EditorGUI.LabelField(position, CommentAttribute.content, EditorStyles.whiteLabel);
			EditorGUI.EndDisabledGroup();
		}
	}
#endif
}