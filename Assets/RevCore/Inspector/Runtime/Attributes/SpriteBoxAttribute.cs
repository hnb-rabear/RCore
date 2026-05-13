using UnityEngine;

namespace RevCore
{
	public sealed class SpriteBoxAttribute : PropertyAttribute
	{
		public float width;
		public float height;

		public SpriteBoxAttribute(float width = 36f, float height = 36f)
		{
			this.width = width;
			this.height = height;
		}
	}
}
