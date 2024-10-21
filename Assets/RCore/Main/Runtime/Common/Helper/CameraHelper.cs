/**
 * Author RadBear - Nguyen Ba Hung - nbhung71711@gmail.com 
 **/

using UnityEngine;

namespace RCore
{
    public static class CameraHelper
    {
        public static Vector3 MousePointToWorldPoint(this Camera pCamera)
        {
            var pos = pCamera.ScreenToWorldPoint(Input.mousePosition);
            pos.z = 0;
            return pos;
        }

        public static Vector2 Size(this Camera camera)
        {
            float height = 2f * camera.orthographicSize;
            float width = height * camera.aspect;
            return new Vector2(width, height);
        }

        public static Bounds OrthographicBounds(this Camera camera)
        {
            float screenAspect = Screen.width / Screen.height;
            float cameraHeight = camera.orthographicSize * 2;
            var bounds = new Bounds(camera.transform.position, new Vector3(cameraHeight * screenAspect, cameraHeight, 0));
            return bounds;
        }

        /// <summary>
        /// Convert a position of screen point to a coresponding position with a camera
        /// Canvas Render Mode should be "Screen Space - Camera"
        /// </summary>
        /// <param name="camera">Camera related to coresponding position</param>
        /// <param name="screenPoint">Position in screen</param>
        /// <returns></returns>
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

        public static bool InsideCamera(this Camera pCamera, Vector3 pPosition, float pExtXRatio = 0, float pExtYRatio = 0)
        {
            var screenPoint = pCamera.WorldToViewportPoint(pPosition);
            bool onScreen = screenPoint.z > 0
                && screenPoint.x >= 0 - pExtXRatio
                && screenPoint.x <= 1 + pExtXRatio
                && screenPoint.y >= 0 - pExtYRatio
                && screenPoint.y <= 1 + pExtYRatio;
            return onScreen;
        }

        public static Vector3 WorldPointToCanvasPoint(this Camera camera, Vector3 worldPos, RectTransform canvasRect)
        {
            Vector2 viewportPosition = camera.WorldToViewportPoint(worldPos);
            var anchoredPosition = new Vector2(
            viewportPosition.x * canvasRect.rect.size.x - canvasRect.rect.size.x * 0.5f,
            viewportPosition.y * canvasRect.rect.size.y - canvasRect.rect.size.y * 0.5f);

            return anchoredPosition;
        }

        public static bool PointerOnRects(this Camera camera, params RectTransform[] rects)
        {
            foreach (var b in rects)
            {
                var screenPoint = camera.ScreenToWorldPoint(Input.mousePosition);
                var inRect = RectTransformUtility.RectangleContainsScreenPoint(b, screenPoint);
                if (inRect)
                    return true;
            }
            return false;
        }
    }
}