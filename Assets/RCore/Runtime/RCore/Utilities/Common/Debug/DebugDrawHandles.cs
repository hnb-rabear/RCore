﻿/***
 * Author RadBear - nbhung71711 @gmail.com - 2017
 **/
#pragma warning disable 0649
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections.Generic;

namespace RCore.Common
{
    public static class DebugDrawHandles
    {
        public static Rect DrawHandlesRectangleXY(Vector3 pRoot, Rect pRect, Color pWireColor)
        {
#if UNITY_EDITOR
            var topLeft = new Vector3(pRect.xMin, pRect.yMax) + pRoot;
            var topRight = new Vector3(pRect.xMax, pRect.yMax) + pRoot;
            var botLeft = new Vector3(pRect.xMin, pRect.yMin) + pRoot;
            var botRight = new Vector3(pRect.xMax, pRect.yMin) + pRoot;

            topLeft = RoundVector(topLeft, 2);
            topRight = RoundVector(topRight, 2);
            botLeft = RoundVector(botLeft, 2);
            botRight = RoundVector(botRight, 2);

            Handles.color = pWireColor;
            Handles.DrawPolyLine(topLeft, topRight, botRight, botLeft, topLeft);
            var newTopRight = Handles.FreeMoveHandle(topRight, Quaternion.identity, 0.05f, Vector3.zero, Handles.RectangleHandleCap);
            Handles.color = pWireColor.Invert();
            var newBotLeft = Handles.FreeMoveHandle(botLeft, Quaternion.identity, 0.05f, Vector3.zero, Handles.RectangleHandleCap);

            newTopRight = RoundVector(newTopRight, 2);
            newBotLeft = RoundVector(newBotLeft, 2);

            if (topRight != newTopRight || botLeft != newBotLeft)
            {
                topRight = newTopRight;
                botLeft = newBotLeft;
                var newXY = botLeft - pRoot;
                var newSize = topRight - pRoot - newXY;
                pRect.Set(newXY.x, newXY.y, newSize.x, newSize.y);
            }
#endif
            return pRect;
        }

        public static void DrawHandlesRectangleXZ(ref Vector3 pBotLeft, ref Vector3 pTopRight, Color pWireColor = default(Color))
        {
#if UNITY_EDITOR
            var topLeft = new Vector3(pBotLeft.x, 0, pTopRight.z);
            var botRight = new Vector3(pTopRight.x, 0, pBotLeft.z);

            Handles.color = pWireColor;
            Handles.DrawPolyLine(topLeft, pTopRight, botRight, pBotLeft, topLeft);
            var newTopRight = Handles.FreeMoveHandle(pTopRight, Quaternion.identity, 1, Vector3.zero, Handles.SphereHandleCap);
            var newBotLeft = Handles.FreeMoveHandle(pBotLeft, Quaternion.identity, 1, Vector3.zero, Handles.SphereHandleCap);

            pTopRight = RoundVector(newTopRight, 0);
            pBotLeft = RoundVector(newBotLeft, 0);
#endif
        }

        public static Bounds DrawHandlesRectangleXY(Vector3 pRoot, Bounds pBounds, Color pWireColor)
        {
#if UNITY_EDITOR
            var topLeft = new Vector3(pBounds.min.x, pBounds.max.y) + pRoot;
            var topRight = new Vector3(pBounds.max.x, pBounds.max.y) + pRoot;
            var botLeft = new Vector3(pBounds.min.x, pBounds.min.y) + pRoot;
            var botRight = new Vector3(pBounds.max.x, pBounds.min.y) + pRoot;

            topLeft = RoundVector(topLeft, 2);
            topRight = RoundVector(topRight, 2);
            botLeft = RoundVector(botLeft, 2);
            botRight = RoundVector(botRight, 2);

            Handles.color = pWireColor;
            Handles.DrawPolyLine(topLeft, topRight, botRight, botLeft, topLeft);
            var newTopRight = Handles.FreeMoveHandle(topRight, Quaternion.identity, 0.05f, Vector3.zero, Handles.RectangleHandleCap);
            Handles.color = pWireColor.Invert();
            var newBotLeft = Handles.FreeMoveHandle(botLeft, Quaternion.identity, 0.05f, Vector3.zero, Handles.RectangleHandleCap);

            newTopRight = RoundVector(newTopRight, 2);
            newBotLeft = RoundVector(newBotLeft, 2);

            if (topRight != newTopRight || botLeft != newBotLeft)
            {
                topRight = newTopRight;
                botLeft = newBotLeft;
                pBounds.SetMinMax(botLeft - pRoot, topRight - pRoot);
            }
#endif
            return pBounds;
        }

        public static Vector3 DrawHandlesCube(Transform pRoot, Vector3 pSize, Color pCubeColor)
        {
#if UNITY_EDITOR
            Handles.color = pCubeColor;
            Handles.DrawWireCube(pRoot.position + pSize / 2f, pSize);

            var right = pRoot.position + new Vector3(pSize.x, 0f, 0f);
            var up = pRoot.position + new Vector3(0f, pSize.y, 0f);
            var foward = pRoot.position + new Vector3(0f, 0f, pSize.z);
            Handles.color = Handles.xAxisColor;
            Handles.ArrowHandleCap(
                0,
                right,
                pRoot.rotation * Quaternion.LookRotation(Vector3.right),
                6,
                EventType.Repaint
            );
            var newRight = Handles.FreeMoveHandle(right, Quaternion.identity, 1, Vector3.zero, Handles.CubeHandleCap);
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
            var newUp = Handles.FreeMoveHandle(up, Quaternion.identity, 1, Vector3.zero, Handles.CubeHandleCap);
            if (newUp != up)
                pSize.y = newUp.y - pRoot.position.y;

            Handles.color = Handles.zAxisColor;
            Handles.ArrowHandleCap(
                0,
                foward,
                pRoot.rotation * Quaternion.LookRotation(Vector3.forward),
                6,
                EventType.Repaint
            );
            var newFoward = Handles.FreeMoveHandle(foward, Quaternion.identity, 1, Vector3.zero, Handles.CubeHandleCap);
            if (newFoward != foward)
                pSize.z = newFoward.z - pRoot.position.z;
#endif
            return pSize;
        }

        private static Vector3 RoundVector(Vector3 pVector, int pDecimal)
        {
            pVector.Set((float)System.Math.Round(pVector.x, pDecimal),
                (float)System.Math.Round(pVector.y, pDecimal),
                (float)System.Math.Round(pVector.z, pDecimal));
            return pVector;
        }

        public static void DrawText(string text, Vector3 worldPos, Color? colour = null)
        {
#if UNITY_EDITOR
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
            GUI.Label(new Rect(screenPos.x - size.x / 2, -screenPos.y + view.position.height + 4, size.x, size.y), text);
            GUI.color = restoreColor;
            Handles.EndGUI();
#endif
        }

        public static void DrawGirdLines(Vector3 rootPos, int width, int length, float pTileSize, bool pRootIsCenter = false)
        {
#if UNITY_EDITOR
            for (int i = 0; i < width; i++)
            {
                //Draw verticle line
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
#endif
        }

        public static void DrawGridNodes(Vector3 rootPos, int width, int length, float tileSize, float pNodeSize, EventType pEventType)
        {
#if UNITY_EDITOR
            var nodes = BuildGridNodes(rootPos, width, length, tileSize);
            for (int i = 0; i < nodes.Count; i++)
                Handles.CubeHandleCap(0, nodes[i], Quaternion.identity, pNodeSize, pEventType);
#endif
        }

        public static Vector3 MouseWolrdPosition(Event e)
        {
#if UNITY_EDITOR
            Vector3 mousePosition = e.mousePosition;
            var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            return ray.origin;
#else
            return Vector3.zero;
#endif
        }

        public static List<Vector3> BuildGridNodes(Vector3 rootPos, int width, int length, float tileSize, bool pRootIsCenter = false)
        {
            var list = new List<Vector3>();
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    var pos = rootPos;
                    pos.x += tileSize * i + tileSize / 2f;
                    pos.z += tileSize * j + tileSize / 2f;
                    if (pRootIsCenter)
                    {
                        pos.x -= width * tileSize / 2f;
                        pos.z -= length * tileSize / 2f;
                    }
                    list.Add(pos);
                }
            }
            return list;
        }
    }
}