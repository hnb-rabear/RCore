/***
 * Author HNB-RaBear - 2017
 **/

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Inspector
{
	public class SeparatorAttribute : PropertyAttribute
	{
		public readonly string title;

		public SeparatorAttribute()
		{
			title = "";
		}

		public SeparatorAttribute(string _title)
		{
			title = _title;
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(SeparatorAttribute))]
	public class SeparatorDecoratorDrawer : DecoratorDrawer
	{
		SeparatorAttribute separatorAttribute => (SeparatorAttribute)attribute;

		public override void OnGUI(Rect _position)
		{
			var lineColor = EditorGUIUtility.isProSkin ? Color.gray : Color.black;

			if (separatorAttribute.title == "")
			{
				DrawLine(_position, lineColor);
			}
			else
			{
				var textSize = GUI.skin.label.CalcSize(new GUIContent(separatorAttribute.title));
				float separatorWidth = (_position.width - textSize.x) / 2.0f - 5.0f;
				_position.y += 9;

				DrawLine(new Rect(_position.xMin, _position.yMin, separatorWidth, 1), lineColor);
				GUI.Label(new Rect(_position.xMin + separatorWidth + 5.0f, _position.yMin - 8.0f, textSize.x, 20), separatorAttribute.title);
				DrawLine(new Rect(_position.xMin + separatorWidth + 10.0f + textSize.x, _position.yMin, separatorWidth, 1), lineColor);
			}
		}

		private void DrawLine(Rect _position, Color color)
		{
			_position.height = 1;
			_position.y += 9;
			EditorGUI.DrawRect(_position, color);
		}

		public override float GetHeight()
		{
			return 20;
		}
	}
#endif
}