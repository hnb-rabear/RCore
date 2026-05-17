using UnityEngine;

namespace RevCore
{
	/// <summary>
	/// On a <see cref="Sprite"/>-typed field, renders a preview thumbnail of the assigned sprite at
	/// the requested size. Implemented by an editor-side property drawer.
	/// </summary>
	public sealed class SpriteBoxAttribute : PropertyAttribute
	{
		/// <summary>Preview thumbnail width in pixels.</summary>
		public float width;

		/// <summary>Preview thumbnail height in pixels.</summary>
		public float height;

		/// <summary>Creates the attribute with optional <paramref name="width"/> / <paramref name="height"/> overrides.</summary>
		public SpriteBoxAttribute(float width = 36f, float height = 36f)
		{
			this.width = width;
			this.height = height;
		}
	}
}
