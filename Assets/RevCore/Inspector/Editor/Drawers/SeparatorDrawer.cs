using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
	[CustomPropertyDrawer(typeof(SeparatorAttribute))]
	public sealed class SeparatorDrawer : DecoratorDrawer
	{
		private SeparatorAttribute Separator => (SeparatorAttribute)attribute;

		public override float GetHeight()
			=> EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + 4f;

		public override void OnGUI(Rect position)
		{
			position.y += 2f;
			if (string.IsNullOrEmpty(Separator.title))
			{
				position.y += EditorGUIUtility.singleLineHeight * 0.5f;
				DrawLine(position, Color.gray);
				return;
			}

			float labelWidth = EditorStyles.boldLabel.CalcSize(new GUIContent(Separator.title)).x + 8f;
			var labelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
			GUI.Label(labelRect, Separator.title, EditorStyles.boldLabel);
			var lineRect = new Rect(position.x + labelWidth, position.y + EditorGUIUtility.singleLineHeight * 0.5f, position.width - labelWidth, 1f);
			DrawLine(lineRect, Color.gray);
		}

		private static void DrawLine(Rect position, Color color)
		{
			position.height = 1f;
			EditorGUI.DrawRect(position, color);
		}
	}
}
