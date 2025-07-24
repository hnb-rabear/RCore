/**
 * Author HNB-RaBear - 2019
 **/

using UnityEngine;

namespace RCore
{
    /// <summary>
    /// A static utility class that provides convenient extension methods for the Unity Camera class.
    /// </summary>
    public static class CameraHelper
    {
        /// <summary>
        /// Converts the current mouse position on the screen to a world point.
        /// The z-coordinate of the resulting vector is set to 0.
        /// </summary>
        /// <param name="pCamera">The camera to perform the conversion with.</param>
        /// <returns>The mouse position in world space with z = 0.</returns>
        public static Vector3 MousePointToWorldPoint(this Camera pCamera)
        {
            var pos = pCamera.ScreenToWorldPoint(Input.mousePosition);
            pos.z = 0;
            return pos;
        }

        /// <summary>
        /// Calculates the world space size (width and height) of an orthographic camera's viewport.
        /// </summary>
        /// <param name="camera">The orthographic camera.</param>
        /// <returns>A Vector2 representing the width and height of the camera's view.</returns>
        public static Vector2 Size(this Camera camera)
        {
            float height = 2f * camera.orthographicSize;
            float width = height * camera.aspect;
            return new Vector2(width, height);
        }

        /// <summary>
        /// Calculates the world space bounding box of an orthographic camera's view.
        /// </summary>
        /// <param name="camera">The orthographic camera.</param>
        /// <returns>A Bounds struct representing the camera's viewing area in world space.</returns>
        public static Bounds OrthographicBounds(this Camera camera)
        {
            // Note: This calculation uses Screen.width/height which might differ from the camera's aspect ratio
            // if the camera doesn't render to the full screen. Using camera.aspect is generally safer.
            float screenAspect = (float)Screen.width / Screen.height;
            float cameraHeight = camera.orthographicSize * 2;
            var bounds = new Bounds(camera.transform.position, new Vector3(cameraHeight * screenAspect, cameraHeight, 0));
            return bounds;
        }

        /// <summary>
        /// Converts a screen point to a world point relative to a specific camera.
        /// This method is intended for use with a UI Canvas that has its Render Mode set to "Screen Space - Camera".
        /// </summary>
        /// <param name="camera">The camera associated with the canvas.</param>
        /// <param name="screenPoint">The position on the screen to convert.</param>
        /// <returns>The corresponding position in the camera's world space.</returns>
        public static Vector3 ScreenPointToWorldPoint(this Camera camera, Vector3 screenPoint)
        {
            float screenRatioX = screenPoint.x / Screen.width;
            float screenRatioY = screenPoint.y / Screen.height;

            var camSize = camera.Size();
            Vector2 position = camera.transform.position;
            position.x -= camSize.x / 2;
            position.y -= camSize.y / 2;

            float dx = camSize.x * screenRatioX;
            float dy = camSize.y * screenRatioY;

            position.x += dx;
            position.y += dy;

            return position;
        }

        /// <summary>
        /// Checks if a given world position is within the visible bounds of an orthographic camera.
        /// </summary>
        /// <param name="pCamera">The orthographic camera.</param>
        /// <param name="pWorldPosition">The world position to check.</param>
        /// <returns>True if the position is inside the camera's view, false otherwise.</returns>
        public static bool InsideOrthographicCamera(this Camera pCamera, Vector3 pWorldPosition)
        {
            float screenAspect = Screen.width * 1f / Screen.height;
            float cameraHeight = pCamera.orthographicSize * 2;
            var bounds = new Bounds(pCamera.transform.position, new Vector3(cameraHeight * screenAspect, cameraHeight, 0));

            if (pWorldPosition.x < bounds.min.x)
                return false;
            if (pWorldPosition.x > bounds.max.x)
                return false;
            if (pWorldPosition.y < bounds.min.y)
                return false;
            if (pWorldPosition.y > bounds.max.y)
                return false;
            return true;
        }

        /// <summary>
        /// Checks if a given world position is within the camera's viewport.
        /// This works for both orthographic and perspective cameras.
        /// </summary>
        /// <param name="pCamera">The camera.</param>
        /// <param name="pPosition">The world position to check.</param>
        /// <param name="pExtXRatio">An optional ratio to extend the horizontal bounds check (e.g., 0.1 for a 10% extension).</param>
        /// <param name="pExtYRatio">An optional ratio to extend the vertical bounds check (e.g., 0.1 for a 10% extension).</param>
        /// <returns>True if the position is within the viewport, false otherwise.</returns>
        public static bool InsideCamera(this Camera pCamera, Vector3 pPosition, float pExtXRatio = 0, float pExtYRatio = 0)
        {
            var screenPoint = pCamera.WorldToViewportPoint(pPosition);
            // Check if the point is in front of the camera and within the 0-1 viewport range (with extensions).
            bool onScreen = screenPoint.z > 0
                && screenPoint.x >= 0 - pExtXRatio
                && screenPoint.x <= 1 + pExtXRatio
                && screenPoint.y >= 0 - pExtYRatio
                && screenPoint.y <= 1 + pExtYRatio;
            return onScreen;
        }

        /// <summary>
        /// Converts a world position to a UI position (anchoredPosition) within a given canvas.
        /// This is useful for placing UI elements on top of 3D objects.
        /// </summary>
        /// <param name="camera">The world camera.</param>
        /// <param name="worldPos">The world position to convert.</param>
        /// <param name="canvasRect">The RectTransform of the parent canvas.</param>
        /// <returns>The calculated anchoredPosition for a UI element on the canvas.</returns>
        public static Vector3 WorldPointToCanvasPoint(this Camera camera, Vector3 worldPos, RectTransform canvasRect)
        {
            Vector2 viewportPosition = camera.WorldToViewportPoint(worldPos);
            // Convert viewport coordinates (0-1) to anchored coordinates within the canvas.
            var anchoredPosition = new Vector2(
            viewportPosition.x * canvasRect.rect.size.x - canvasRect.rect.size.x * 0.5f,
            viewportPosition.y * canvasRect.rect.size.y - canvasRect.rect.size.y * 0.5f);

            return anchoredPosition;
        }

        /// <summary>
        /// Checks if the current mouse pointer is over any of the specified UI RectTransforms.
        /// </summary>
        /// <param name="camera">The camera associated with the canvas.</param>
        /// <param name="rects">An array of RectTransforms to check against.</param>
        /// <returns>True if the pointer is over any of the rects, false otherwise.</returns>
        public static bool PointerOnRects(this Camera camera, params RectTransform[] rects)
        {
            foreach (var b in rects)
            {
                // Note: The conversion here might be incorrect depending on the Canvas render mode.
                // For "Screen Space - Overlay", Input.mousePosition should be used directly with RectangleContainsScreenPoint.
                // For "Screen Space - Camera", the camera needs to be provided in the last argument of RectangleContainsScreenPoint.
                var screenPoint = camera.ScreenToWorldPoint(Input.mousePosition);
                var inRect = RectTransformUtility.RectangleContainsScreenPoint(b, screenPoint);
                if (inRect)
                    return true;
            }
            return false;
        }
    }
}