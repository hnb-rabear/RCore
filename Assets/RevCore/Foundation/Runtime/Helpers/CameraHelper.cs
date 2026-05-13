using UnityEngine;

namespace RevCore
{
	public static class CameraExtension
	{
		public static Vector3 WorldPointToCanvasPoint(this Camera camera, Vector3 worldPos, RectTransform canvasRect)
		{
			Vector2 viewportPosition = camera.WorldToViewportPoint(worldPos);
			return new Vector2(
				viewportPosition.x * canvasRect.rect.size.x - canvasRect.rect.size.x * 0.5f,
				viewportPosition.y * canvasRect.rect.size.y - canvasRect.rect.size.y * 0.5f);
		}
	}
}
