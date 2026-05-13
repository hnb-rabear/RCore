using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
	[CustomPropertyDrawer(typeof(CommentAttribute))]
	public sealed class CommentDrawer : DecoratorDrawer
	{
		private CommentAttribute Comment => (CommentAttribute)attribute;

		public override float GetHeight()
			=> EditorStyles.whiteLabel.CalcHeight(Comment.content, Screen.width - 19f);

		public override void OnGUI(Rect position)
		{
			EditorGUI.BeginDisabledGroup(true);
			EditorGUI.LabelField(position, Comment.content, EditorStyles.whiteLabel);
			EditorGUI.EndDisabledGroup();
		}
	}
}
