using UnityEngine;

namespace RevCore
{
	/// <summary>Extension methods for <see cref="Camera"/>.</summary>
	public static class CameraExtension
	{
		/// <summary>
		/// Converts a world-space position to a canvas-local position on the given canvas
		/// <see cref="RectTransform"/>. The canvas is assumed to be in Screen Space - Overlay
		/// or Screen Space - Camera mode; for World Space canvases use the standard
		/// <see cref="Camera.WorldToScreenPoint"/> instead.
		/// </summary>
		/// <param name="camera">Camera the world point is observed from.</param>
		/// <param name="worldPos">World-space position to project.</param>
		/// <param name="canvasRect">Canvas root <see cref="RectTransform"/> whose local space the result is in.</param>
		/// <returns>The position in canvas local coordinates, centered on the canvas origin.</returns>
		public static Vector3 WorldPointToCanvasPoint(this Camera camera, Vector3 worldPos, RectTransform canvasRect)
		{
			Vector2 viewportPosition = camera.WorldToViewportPoint(worldPos);
			return new Vector2(
				viewportPosition.x * canvasRect.rect.size.x - canvasRect.rect.size.x * 0.5f,
				viewportPosition.y * canvasRect.rect.size.y - canvasRect.rect.size.y * 0.5f);
		}
	}
}
