#if UNITY_EDITOR
using System;
using UnityEngine;

namespace RCore.Editor
{
	/// <summary>
	/// Represents a drawable button for the Unity Editor.
	/// </summary>
	public class GuiButton : IDraw
	{
		/// <summary>The text label displayed on the button.</summary>
		public string label;
		/// <summary>The background color of the button.</summary>
		public Color color;
		/// <summary>The fixed width of the button. If 0, it auto-sizes.</summary>
		public int width;
		/// <summary>The fixed height of the button. If 0, it auto-sizes.</summary>
		public int height;
		/// <summary>An optional icon to display on the button.</summary>
		public Texture2D icon;
		/// <summary>An action to be invoked when the button is pressed.</summary>
		public Action onPressed;
		
		/// <summary>Gets a value indicating whether the button was pressed in the last GUI frame.</summary>
		public bool IsPressed { get; private set; }

		/// <summary>
		/// Draws the button using GUILayout.
		/// </summary>
		/// <param name="style">Optional custom GUIStyle. If null, uses the default "Button" style.</param>
		public void Draw(GUIStyle style = null)
		{
			var minHeight = GUILayout.MinHeight(21);
			var defaultColor = GUI.backgroundColor;
			style ??= new GUIStyle("Button");
			if (width > 0)
			{
				style.fixedWidth = width;
			}
			if (height > 0)
			{
				style.fixedHeight = height;
				minHeight = GUILayout.MinHeight(height);
			}
			if (color != default)
				GUI.backgroundColor = color;
			var content = new GUIContent(label);
			if (icon != null)
				content = new GUIContent(label, icon);
			IsPressed = GUILayout.Button(content, style, minHeight);
			if (IsPressed && onPressed != null)
				onPressed();
			GUI.backgroundColor = defaultColor;
		}
	}
}
#endif