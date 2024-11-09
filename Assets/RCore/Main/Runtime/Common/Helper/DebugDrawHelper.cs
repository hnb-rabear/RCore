/**
 * Author RaBear - HNB - 2017
 **/

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RCore
{
    public static class DebugDrawHelper
    {
        public static Rect DrawHandlesRectangleXY(Vector3 pRoot, Rect pRect, Color pWireColor)
        {
            var topLeft = new Vector3(pRect.xMin, pRect.yMax) + pRoot;
            var topRight = new Vector3(pRect.xMax, pRect.yMax) + pRoot;
            var botLeft = new Vector3(pRect.xMin, pRect.yMin) + pRoot;
            var botRight = new Vector3(pRect.xMax, pRect.yMin) + pRoot;

            topLeft = topLeft.Round(2);
            topRight = topRight.Round(2);
            botLeft = botLeft.Round(2);
            botRight = botRight.Round(2);

            Handles.color = pWireColor;
            Handles.DrawPolyLine(topLeft, topRight, botRight, botLeft, topLeft);
            var newTopRight = Handles.FreeMoveHandle(topRight, 0.05f, Vector3.zero, Handles.RectangleHandleCap);
            Handles.color = pWireColor.Invert();
            var newBotLeft = Handles.FreeMoveHandle(botLeft, 0.05f, Vector3.zero, Handles.RectangleHandleCap);

            newTopRight = newTopRight.Round(2);
            newBotLeft = newBotLeft.Round(2);

            if (topRight != newTopRight || botLeft != newBotLeft)
            {
                topRight = newTopRight;
                botLeft = newBotLeft;
                var newXY = botLeft - pRoot;
                var newSize = topRight - pRoot - newXY;
                pRect.Set(newXY.x, newXY.y, newSize.x, newSize.y);
            }
            return pRect;
        }
        public static void DrawHandlesRectangleXZ(ref Vector3 pBotLeft, ref Vector3 pTopRight, Color pWireColor = default)
        {
            var topLeft = new Vector3(pBotLeft.x, 0, pTopRight.z);
            var botRight = new Vector3(pTopRight.x, 0, pBotLeft.z);

            Handles.color = pWireColor;
            Handles.DrawPolyLine(topLeft, pTopRight, botRight, pBotLeft, topLeft);
            var newTopRight = Handles.FreeMoveHandle(pTopRight, 1, Vector3.zero, Handles.SphereHandleCap);
            var newBotLeft = Handles.FreeMoveHandle(pBotLeft, 1, Vector3.zero, Handles.SphereHandleCap);

            pTopRight = newTopRight.Round(0);
            pBotLeft = newBotLeft.Round(0);
        }
        public static Bounds DrawHandlesRectangleXY(Vector3 pRoot, Bounds pBounds, Color pWireColor)
        {
            var topLeft = new Vector3(pBounds.min.x, pBounds.max.y) + pRoot;
            var topRight = new Vector3(pBounds.max.x, pBounds.max.y) + pRoot;
            var botLeft = new Vector3(pBounds.min.x, pBounds.min.y) + pRoot;
            var botRight = new Vector3(pBounds.max.x, pBounds.min.y) + pRoot;

            topLeft = topLeft.Round(2);
            topRight = topRight.Round(2);
            botLeft = botLeft.Round(2);
            botRight = botRight.Round(2);

            Handles.color = pWireColor;
            Handles.DrawPolyLine(topLeft, topRight, botRight, botLeft, topLeft);
            var newTopRight = Handles.FreeMoveHandle(topRight, 0.05f, Vector3.zero, Handles.RectangleHandleCap);
            Handles.color = pWireColor.Invert();
            var newBotLeft = Handles.FreeMoveHandle(botLeft, 0.05f, Vector3.zero, Handles.RectangleHandleCap);

            newTopRight = newTopRight.Round(2);
            newBotLeft = newBotLeft.Round(2);

            if (topRight != newTopRight || botLeft != newBotLeft)
            {
                topRight = newTopRight;
                botLeft = newBotLeft;
                pBounds.SetMinMax(botLeft - pRoot, topRight - pRoot);
            }
            return pBounds;
        }
        public static Vector3 DrawHandlesCube(Transform pRoot, Vector3 pSize, Color pCubeColor)
        {
            Handles.color = pCubeColor;
            var position = pRoot.position;
            Handles.DrawWireCube(position + pSize / 2f, pSize);

            var right = position + new Vector3(pSize.x, 0f, 0f);
            var up = position + new Vector3(0f, pSize.y, 0f);
            var forward = position + new Vector3(0f, 0f, pSize.z);
            Handles.color = Handles.xAxisColor;
            Handles.ArrowHandleCap(
                0,
                right,
                pRoot.rotation * Quaternion.LookRotation(Vector3.right),
                6,
                EventType.Repaint
            );
            var newRight = Handles.FreeMoveHandle(right, 1, Vector3.zero, Handles.CubeHandleCap);
            if (newRight != right)
                pSize.x = newRight.x - pRoot.position.x;

            Handles.color = Handles.yAxisColor;
            Handles.ArrowHandleCap(
                0,
                up,
                pRoot.rotation * Quaternion.LookRotation(Vector3.up),
                6,
                EventType.Repaint
            );
            var newUp = Handles.FreeMoveHandle(up, 1, Vector3.zero, Handles.CubeHandleCap);
            if (newUp != up)
                pSize.y = newUp.y - pRoot.position.y;

            Handles.color = Handles.zAxisColor;
            Handles.ArrowHandleCap(
                0,
                forward,
                pRoot.rotation * Quaternion.LookRotation(Vector3.forward),
                6,
                EventType.Repaint
            );
            var newForward = Handles.FreeMoveHandle(forward, 1, Vector3.zero, Handles.CubeHandleCap);
            if (newForward != forward)
                pSize.z = newForward.z - pRoot.position.z;
            return pSize;
        }
        public static void DrawText(string text, Vector3 worldPos, Color? colour = null)
        {
            Handles.BeginGUI();
            var restoreColor = GUI.color;
            if (colour.HasValue) GUI.color = colour.Value;
            var view = SceneView.currentDrawingSceneView;
            var screenPos = view.camera.WorldToScreenPoint(worldPos);

            if (screenPos.y < 0 || screenPos.y > Screen.height || screenPos.x < 0 || screenPos.x > Screen.width || screenPos.z < 0)
            {
                GUI.color = restoreColor;
                Handles.EndGUI();
                return;
            }

            var size = GUI.skin.label.CalcSize(new GUIContent(text));
            GUI.Label(new Rect(screenPos.x - (size.x / 2), -screenPos.y + view.position.height + 4, size.x, size.y), text);
            GUI.color = restoreColor;
            Handles.EndGUI();
        }
        public static void DrawGirdLines(Vector3 rootPos, int width, int length, float pTileSize, bool pRootIsCenter = false)
        {
            for (int i = 0; i < width; i++)
            {
                //Draw vertical line
                var from = rootPos;
                from.x += i * pTileSize;
                var to = rootPos;
                to.z += length * pTileSize;
                to.x += i * pTileSize;
                if (pRootIsCenter)
                {
                    from.x -= width * pTileSize / 2f;
                    from.z -= length * pTileSize / 2f;
                    to.x -= width * pTileSize / 2f;
                    to.z -= length * pTileSize / 2f;
                }
                Handles.DrawLine(from, to);
            }
            for (int i = 0; i < length; i++)
            {
                //Draw horizontal line
                var from = rootPos;
                from.z += i * pTileSize;
                var to = rootPos;
                to.x += width * pTileSize;
                to.z += i * pTileSize;
                if (pRootIsCenter)
                {
                    from.x -= width * pTileSize / 2f;
                    from.z -= length * pTileSize / 2f;
                    to.x -= width * pTileSize / 2f;
                    to.z -= length * pTileSize / 2f;
                }
                Handles.DrawLine(from, to);
            }
        }
        public static void DrawGridNodes(Vector3 rootPos, int width, int length, float tileSize, float pNodeSize, EventType pEventType)
        {
            var nodes = MathHelper.CalcGridNodes(rootPos, width, length, tileSize);
            for (int i = 0; i < nodes.Count; i++)
                Handles.CubeHandleCap(0, nodes[i], Quaternion.identity, pNodeSize, pEventType);
        }
        public static Vector3 MouseWorldPosition(Event e)
        {
            Vector3 mousePosition = e.mousePosition;
            var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            return ray.origin;
        }
        public static Vector3[] DrawMovePath(Vector3[] points, Vector3 rootPos)
        {
            for (int i = 0; i < points.Length; i++)
            {
                if (i < points.Length - 2)
                    Handles.DrawLine(points[i] + rootPos, points[i + 1] + rootPos);

                if (i == 0)
                    Handles.color = Color.yellow;
                else if (i == points.Length - 1)
                    Handles.color = Color.red;
                else
                    Handles.color = Color.white;
                var newPos = Handles.FreeMoveHandle(points[i] + rootPos, 0.1f, Vector3.zero, Handles.CubeHandleCap);
                newPos = MathHelper.Round(newPos, 1);
                if (newPos != rootPos + points[i])
                    points[i] = newPos - rootPos;
            }
            return points;
        }
        public static Vector3 DrawMovePoint(Vector3 point, Vector3 rootPos)
        {
            var newPos = Handles.FreeMoveHandle(point + rootPos, 0.1f, Vector3.zero, Handles.CubeHandleCap);
            newPos = MathHelper.Round(newPos, 1);
            if (newPos != rootPos + point)
                point = newPos - rootPos;
            return point;
        }
    }
}
#endif