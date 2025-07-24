using UnityEngine;

namespace RCore.Editor
{
	/// <summary>
	/// Defines an interface for custom editor GUI elements that can be drawn.
	/// </summary>
	public interface IDraw
	{
		/// <summary>
		/// Draws the editor element.
		/// </summary>
		/// <param name="style">Optional GUIStyle to apply to the element.</param>
		void Draw(GUIStyle style = null);
	}
}