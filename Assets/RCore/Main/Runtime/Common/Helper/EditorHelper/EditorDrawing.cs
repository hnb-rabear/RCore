#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor
{
    /// <summary>
    /// Provides a collection of static helper methods for drawing custom UI elements in Unity Editor windows and inspectors.
    /// This class simplifies the process of creating common and complex GUI layouts.
    /// </summary>
    public static class EditorDrawing
    {
        /// <summary>Stores the original background color for BoxVerticalOpen/Close pairs to ensure proper restoration.</summary>
        private static readonly Dictionary<int, Color> BoxColours = new Dictionary<int, Color>();
        
        /// <summary>
        /// Draws a texture within a bordered box, commonly used for displaying icons or sprite previews.
        /// If the texture is null, a warning icon is displayed instead.
        /// </summary>
        /// <param name="pTexture">The texture to draw.</param>
        /// <param name="pSize">The size of the box and texture area.</param>
        public static void DrawTextureIcon(Texture pTexture, Vector2 pSize)
        {
	        var rect = EditorGUILayout.GetControlRect(GUILayout.Width(pSize.x), GUILayout.Height(pSize.y));
	        GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
	        if (pTexture != null)
		        GUI.DrawTexture(rect, pTexture, ScaleMode.ScaleToFit);
	        else
		        // Fallback to a warning icon if the provided texture is null.
		        GUI.DrawTexture(rect, EditorGUIUtility.FindTexture("console.warnicon"), ScaleMode.ScaleToFit);
        }
        
        /// <summary>
        /// Draws a prominent, centered header title with a toolbar-style background.
        /// </summary>
        /// <param name="pHeader">The text to display in the header.</param>
        public static void DrawHeaderTitle(string pHeader)
        {
	        var prevColor = GUI.color;

	        var boxStyle = new GUIStyle(EditorStyles.toolbar)
	        {
		        fixedHeight = 0,
		        padding = new RectOffset(5, 5, 5, 5)
	        };

	        var titleStyle = new GUIStyle(EditorStyles.largeLabel)
	        {
		        fontStyle = FontStyle.Bold,
		        normal =
		        {
			        textColor = Color.white // Ensures readability on dark toolbar background
		        },
		        alignment = TextAnchor.MiddleCenter
	        };

	        GUI.color = new Color(0.5f, 0.5f, 0.5f); // Darken the toolbar background
	        EditorGUILayout.BeginHorizontal(boxStyle, GUILayout.Height(20));
	        {
		        GUI.color = prevColor;
		        EditorGUILayout.LabelField(pHeader, titleStyle, GUILayout.Height(20));
	        }
	        EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Begins a vertical group with an optional background color and box style.
        /// This method must be paired with `BoxVerticalClose`.
        /// </summary>
        /// <param name="id">A unique identifier for this box group to manage its color state.</param>
        /// <param name="color">The background color tint for the group.</param>
        /// <param name="isBox">If true, a GUI.Box style is used for the background; otherwise, it's a plain group.</param>
        /// <param name="pFixedWidth">An optional fixed width for the vertical group.</param>
        /// <param name="pFixedHeight">An optional fixed height for the vertical group.</param>
        public static void BoxVerticalOpen(int id, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0)
        {
	        var defaultColor = GUI.backgroundColor;
	        if (!BoxColours.ContainsKey(id))
		        BoxColours.Add(id, defaultColor);

	        if (color != default)
		        GUI.backgroundColor = color;

	        if (!isBox)
	        {
		        var style = new GUIStyle();
		        if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
		        if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
		        EditorGUILayout.BeginVertical(style);
	        }
	        else
	        {
		        var style = new GUIStyle("box");
		        if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
		        if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
		        EditorGUILayout.BeginVertical(style);
	        }
        }

        /// <summary>
        /// Ends a vertical group started with `BoxVerticalOpen` and restores the original background color.
        /// </summary>
        /// <param name="id">The unique identifier that was passed to `BoxVerticalOpen`.</param>
        public static void BoxVerticalClose(int id)
        {
	        GUI.backgroundColor = BoxColours[id];
	        EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draws a vertical group, executing a provided action to draw its content.
        /// This is a convenient alternative to the Begin/End pattern.
        /// </summary>
        /// <param name="doSomething">An action containing the GUI code to be drawn inside the group.</param>
        /// <param name="color">The background color tint for the group.</param>
        /// <param name="isBox">If true, a GUI.Box style is used for the background.</param>
        /// <returns>The Rect used by the vertical group.</returns>
		public static Rect BoxVertical(Action doSomething, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0)
		{
			Rect rect;
			var defaultColor = GUI.backgroundColor;
			if (color != default)
				GUI.backgroundColor = color;

			if (!isBox)
			{
				var style = new GUIStyle();
				if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
				if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
				rect = EditorGUILayout.BeginVertical(style);
			}
			else
			{
				var style = new GUIStyle("box");
				if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
				if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
				rect = EditorGUILayout.BeginVertical(style);
			}

			doSomething();

			EditorGUILayout.EndVertical();
			if (color != default)
				GUI.backgroundColor = defaultColor;

			return rect;
		}

		/// <summary>
		/// Draws a vertical group with a title header, executing a provided action to draw its content.
		/// </summary>
		/// <param name="pTitle">The title to display at the top of the group.</param>
		/// <param name="doSomething">An action containing the GUI code to be drawn inside the group.</param>
		/// <returns>The Rect used by the vertical group.</returns>
		public static Rect BoxVertical(string pTitle, Action doSomething, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0)
		{
			Rect rect;
			var defaultColor = GUI.backgroundColor;
			if (color != default)
				GUI.backgroundColor = color;

			if (!isBox)
			{
				var style = new GUIStyle();
				if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
				if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
				rect = EditorGUILayout.BeginVertical(style);
			}
			else
			{
				var style = new GUIStyle("box");
				if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
				if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
				rect = EditorGUILayout.BeginVertical(style);
			}

			if (!string.IsNullOrEmpty(pTitle))
				DrawHeaderTitle(pTitle);

			doSomething();

			EditorGUILayout.EndVertical();
			if (color != default)
				GUI.backgroundColor = defaultColor;

			return rect;
		}

		/// <summary>
		/// Draws a horizontal group, executing a provided action to draw its content.
		/// </summary>
		/// <param name="doSomething">An action containing the GUI code to be drawn inside the group.</param>
		/// <param name="color">The background color tint for the group.</param>
		/// <param name="isBox">If true, a GUI.Box style is used for the background.</param>
		/// <returns>The Rect used by the horizontal group.</returns>
		public static Rect BoxHorizontal(Action doSomething, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0)
		{
			Rect rect;
			var defaultColor = GUI.backgroundColor;
			if (color != default)
				GUI.backgroundColor = color;

			if (!isBox)
			{
				var style = new GUIStyle();
				if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
				if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
				rect = EditorGUILayout.BeginHorizontal(style);
			}
			else
			{
				var style = new GUIStyle("box");
				if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
				if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
				rect = EditorGUILayout.BeginHorizontal(style);
			}

			doSomething();

			EditorGUILayout.EndHorizontal();

			if (color != default)
				GUI.backgroundColor = defaultColor;
			return rect;
		}

		/// <summary>
		/// Draws a horizontal group within a vertical group that has a title header.
		/// </summary>
		/// <param name="pTitle">The title to display above the horizontal group.</param>
		/// <param name="doSomething">An action containing the GUI code to be drawn inside the horizontal group.</param>
		/// <returns>The Rect used by the horizontal group.</returns>
		public static Rect BoxHorizontal(string pTitle, Action doSomething, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0)
		{
			Rect rect;
			var defaultColor = GUI.backgroundColor;
			if (color != default)
				GUI.backgroundColor = color;

			if (!string.IsNullOrEmpty(pTitle))
			{
				EditorGUILayout.BeginVertical();
				DrawHeaderTitle(pTitle);
			}

			if (!isBox)
			{
				var style = new GUIStyle();
				if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
				if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
				rect = EditorGUILayout.BeginHorizontal(style);
			}
			else
			{
				var style = new GUIStyle("box");
				if (pFixedWidth > 0) style.fixedWidth = pFixedWidth;
				if (pFixedHeight > 0) style.fixedHeight = pFixedHeight;
				rect = EditorGUILayout.BeginHorizontal(style);
			}

			doSomething();

			EditorGUILayout.EndHorizontal();

			if (!string.IsNullOrEmpty(pTitle))
				EditorGUILayout.EndVertical();

			if (color != default)
				GUI.backgroundColor = defaultColor;

			return rect;
		}
		
		/// <summary>
		/// Draws a simple horizontal line separator.
		/// </summary>
		/// <param name="padding">The vertical space in pixels to add above and below the line.</param>
		public static void DrawLine(float padding = 0)
		{
			if (padding > 0)
				EditorGUILayout.Space(padding);
			var lineColor = EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f) : new Color(0.6f, 0.6f, 0.6f);
			var originalColor = GUI.color;
			GUI.color = lineColor;
			float lineThickness = 1;
			var lineRect = EditorGUILayout.GetControlRect(false, lineThickness);
			EditorGUI.DrawRect(lineRect, lineColor);
			GUI.color = originalColor;
			if (padding > 0)
				EditorGUILayout.Space(padding);
		}

		/// <summary>
		/// Draws a separator line, optionally with a centered label.
		/// </summary>
		/// <param name="label">The text to display in the middle of the separator.</param>
		/// <param name="labelColor">The color of the label text.</param>
		public static void Separator(string label = null, Color labelColor = default)
		{
			if (string.IsNullOrEmpty(label))
			{
				// Draw a simple horizontal line if no label is provided.
				EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			}
			else
			{
				GUILayout.Space(10);
				GUILayout.BeginHorizontal();

				// Left separator line
				GUILayout.Label("", GUI.skin.horizontalSlider);

				// Set bold and colored style for the label
				var boldStyle = new GUIStyle(GUI.skin.label);
				boldStyle.fontStyle = FontStyle.Bold;
				boldStyle.alignment = TextAnchor.MiddleCenter;
				if (labelColor != default)
					GUI.contentColor = labelColor;

				// Label with "Editor" in bold and color
				GUILayout.Label(label, boldStyle, GUILayout.Width(50), GUILayout.ExpandWidth(false));

				// Reset content color to default
				GUI.contentColor = Color.white;

				// Right separator line
				GUILayout.Label("", GUI.skin.horizontalSlider);

				GUILayout.EndHorizontal();
				GUILayout.Space(10);
			}
		}
		
		/// <summary>
		/// Draws a stylized, thick separator box line.
		/// </summary>
		public static void SeparatorBox()
		{
			GUILayout.Space(10);

			if (Event.current.type == EventType.Repaint)
			{
				var tex = EditorGUIUtility.whiteTexture;
				var rect = GUILayoutUtility.GetLastRect();
				GUI.color = new Color(0f, 0f, 0f, 0.25f);
				GUI.DrawTexture(new Rect(0f, rect.yMin + 6f, Screen.width, 4f), tex);
				GUI.DrawTexture(new Rect(0f, rect.yMin + 6f, Screen.width, 1f), tex);
				GUI.DrawTexture(new Rect(0f, rect.yMin + 9f, Screen.width, 1f), tex);
				GUI.color = Color.white;
			}
		}
    }
}
#endif