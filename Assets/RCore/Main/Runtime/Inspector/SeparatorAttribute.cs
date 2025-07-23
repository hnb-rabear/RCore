using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Inspector
{
    /// <summary>
    /// Displays a separator line in the Unity Inspector, optionally with a title.
    /// This attribute can be used to organize and group fields visually.
    /// </summary>
    public class SeparatorAttribute : PropertyAttribute
    {
        public readonly string title;

        /// <summary>
        /// Creates a separator without a title.
        /// </summary>
        public SeparatorAttribute()
        {
            title = "";
        }

        /// <summary>
        /// Creates a separator with a title.
        /// </summary>
        /// <param name="title">The title to display in the middle of the separator.</param>
        public SeparatorAttribute(string title)
        {
            this.title = title;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Custom drawer for the SeparatorAttribute. This class handles the actual drawing
    /// of the separator line and title in the inspector.
    /// </summary>
    [CustomPropertyDrawer(typeof(SeparatorAttribute))]
    public class SeparatorDecoratorDrawer : DecoratorDrawer
    {
        /// <summary>
        /// Gets the SeparatorAttribute instance for this drawer.
        /// </summary>
        private SeparatorAttribute SeparatorAttribute => (SeparatorAttribute)attribute;

        /// <summary>
        /// Renders the separator GUI.
        /// </summary>
        /// <param name="position">The rectangle on the screen to draw the separator within.</param>
        public override void OnGUI(Rect position)
        {
            // Choose line color based on the current editor skin (light/dark mode)
            var lineColor = EditorGUIUtility.isProSkin ? Color.gray : Color.black;

            // If the title is empty, draw a simple horizontal line
            if (string.IsNullOrEmpty(SeparatorAttribute.title))
            {
                // Vertically center the line
                position.y += EditorGUIUtility.singleLineHeight / 2f - 1;
                DrawLine(position, lineColor);
            }
            else
            {
                // Calculate the size of the title text
                var textSize = GUI.skin.label.CalcSize(new GUIContent(SeparatorAttribute.title));
                // Calculate the width of the lines on either side of the title
                float separatorWidth = (position.width - textSize.x) / 2.0f - 5.0f; // 5.0f for padding

                // Center the entire element vertically
                position.y += EditorGUIUtility.singleLineHeight / 2f;

                // Draw the left line
                var leftLineRect = new Rect(position.xMin, position.yMin, separatorWidth, 1);
                DrawLine(leftLineRect, lineColor);

                // Draw the title label
                var labelRect = new Rect(position.xMin + separatorWidth + 5.0f, position.yMin - 8.0f, textSize.x, 20);
                GUI.Label(labelRect, SeparatorAttribute.title);

                // Draw the right line
                var rightLineRect = new Rect(position.xMin + separatorWidth + 10.0f + textSize.x, position.yMin, separatorWidth, 1);
                DrawLine(rightLineRect, lineColor);
            }
        }

        /// <summary>
        /// Draws a 1-pixel height rectangle to serve as a line.
        /// </summary>
        private void DrawLine(Rect position, Color color)
        {
            position.height = 1;
            EditorGUI.DrawRect(position, color);
        }

        /// <summary>
        /// Gets the total height required by the separator.
        /// </summary>
        public override float GetHeight()
        {
            // A bit more than a standard line height to provide some visual spacing.
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + 4f;
        }
    }
#endif
}