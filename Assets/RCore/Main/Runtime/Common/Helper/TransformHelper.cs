/**
 * Author RaBear - HNB - 2017
 **/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RCore
{
	public enum AnchorType
	{
		TopLeft,
		Top,
		TopRight,
		Right,
		BotRight,
		Bot,
		BotLeft,
		Left,
		Center,
		Stretch
	}

	public static class TransformExtension
	{
		public static Vector3 Add(this Vector3 pVector, Vector3 pValue)
		{
			pVector.x += pValue.x;
			pVector.y += pValue.y;
			pVector.z += pValue.z;
			return pVector;
		}

		public static Vector3 AddX(this Vector3 pVector, float pValue)
		{
			pVector.x += pValue;
			return pVector;
		}

		public static Vector3 AddY(this Vector3 pVector, float pValue)
		{
			pVector.y += pValue;
			return pVector;
		}

		public static Vector3 AddZ(this Vector3 pVector, float pValue)
		{
			pVector.z += pValue;
			return pVector;
		}

		public static Vector3 SetX(this Vector3 pVector, float pValue)
		{
			pVector.x = pValue;
			return pVector;
		}

		public static Vector3 SetY(this Vector3 pVector, float pValue)
		{
			pVector.y = pValue;
			return pVector;
		}

		public static Vector3 SetZ(this Vector3 pVector, float pValue)
		{
			pVector.z = pValue;
			return pVector;
		}

		public static Vector2 ToVector2(this Vector3 pVector)
		{
			return new Vector2(pVector.x, pVector.y);
		}

		public static Vector3 AddX(this Vector2 pVector, float pValue)
		{
			pVector.x += pValue;
			return pVector;
		}

		public static Vector3 AddY(this Vector2 pVector, float pValue)
		{
			pVector.y += pValue;
			return pVector;
		}

		// Since transforms return their position as a property,
		// you can't set the x/y/z values directly, so you have to
		// store a temporary Vector3
		// Or you can use these methods instead
		public static Transform SetPosX(this Transform transform, float x)
		{
			var pos = transform.position;
			pos.x = x;
			transform.position = pos;
			return transform;
		}

		public static Transform SetAnchoredPosX(this RectTransform transform, float x)
		{
			var pos = transform.anchoredPosition;
			pos.x = x;
			transform.anchoredPosition = pos;
			return transform;
		}

		public static Transform SetLocalPosX(this Transform transform, float x)
		{
			var pos = transform.localPosition;
			pos.x = x;
			transform.localPosition = pos;
			return transform;
		}

		public static Transform AddPosX(this Transform transform, float x)
		{
			var pos = transform.position;
			pos.x += x;
			transform.position = pos;
			return transform;
		}

		public static Transform SetPosY(this Transform transform, float y)
		{
			var pos = transform.position;
			pos.y = y;
			transform.position = pos;
			return transform;
		}

		public static Transform SetLocalPosY(this Transform transform, float y)
		{
			var pos = transform.localPosition;
			pos.y = y;
			transform.localPosition = pos;
			return transform;
		}

		public static Transform AddPosY(this Transform transform, float y)
		{
			var pos = transform.position;
			pos.y += y;
			transform.position = pos;
			return transform;
		}

		public static Transform SetPosZ(this Transform transform, float z)
		{
			var pos = transform.position;
			pos.z = z;
			transform.position = pos;
			return transform;
		}

		public static Transform SetLocalPosZ(this Transform transform, float z)
		{
			var pos = transform.localPosition;
			pos.z = z;
			transform.localPosition = pos;
			return transform;
		}

		public static Transform AddPosZ(this Transform transform, float z)
		{
			var pos = transform.position;
			pos.z += z;
			transform.position = pos;
			return transform;
		}

		public static Transform SetScaleX(this Transform transform, float x)
		{
			var scale = transform.localScale;
			scale.x = x;
			transform.localScale = scale;
			return transform;
		}

		public static Transform SetScaleY(this Transform transform, float y)
		{
			var scale = transform.localScale;
			scale.y = y;
			transform.localScale = scale;
			return transform;
		}

		public static Transform SetScaleZ(this Transform transform, float z)
		{
			var scale = transform.localScale;
			scale.z = z;
			transform.localScale = scale;
			return transform;
		}

		public static Transform FlipX(this Transform transform)
		{
			var scale = transform.localScale;
			scale.x *= -1;
			transform.localScale = scale;
			return transform;
		}

		public static Transform FlipX(this Transform transform, int direction)
		{
			if (direction > 0)
				direction = 1;
			else
				direction = -1;

			var scale = transform.localScale;
			scale.x = Mathf.Abs(scale.x) * direction;
			transform.localScale = scale;
			return transform;
		}

		public static Transform FlipY(this Transform transform)
		{
			var scale = transform.localScale;
			scale.y *= -1;
			transform.localScale = scale;
			return transform;
		}

		public static Transform FlipY(this Transform transform, int direction)
		{
			if (direction > 0)
				direction = 1;
			else
				direction = -1;

			var scale = transform.localScale;
			scale.y = Mathf.Abs(scale.x) * direction;
			transform.localScale = scale;
			return transform;
		}

		public static Transform Reset(this Transform transform)
		{
			transform.localPosition = Vector3.zero;
			transform.localScale = Vector3.one;
			transform.localRotation = Quaternion.identity;
			return transform;
		}

		public static Transform LookAtDirection(this Transform transform, Vector3 pDirection)
		{
			pDirection.y = 0;
			transform.LookAt(transform.position + pDirection);
			return transform;
		}

		public static List<Transform> GetChildren(this Transform transform)
		{
			var children = new List<Transform>();

			for (var i = 0; i < transform.childCount; i++)
			{
				var child = transform.GetChild(i);
				children.Add(child);
			}

			return children;
		}

		/// <summary>
		/// Sort children by conditions
		/// E.g: transform.Sort(t => t.name);
		/// </summary>
		public static void Sort(this Transform transform, Func<Transform, IComparable> sortFunction)
		{
			var children = transform.GetChildren();
			var sortedChildren = children.OrderBy(sortFunction).ToList();

			for (int i = 0; i < sortedChildren.Count(); i++)
				sortedChildren[i].SetSiblingIndex(i);
		}

		public static IEnumerable<Transform> GetAllChildren(this Transform transform)
		{
			var openList = new Queue<Transform>();

			openList.Enqueue(transform);

			while (openList.Any())
			{
				var currentChild = openList.Dequeue();

				yield return currentChild;

				var children = transform.GetChildren();

				foreach (var child in children)
				{
					openList.Enqueue(child);
				}
			}
		}

		public static int HierarchyDeep(this Transform pTransform)
		{
			int deep = 0;
			if (pTransform.parent != null)
			{
				deep += 1 + pTransform.parent.HierarchyDeep();
			}
			return deep;
		}

#region RectTransfrom

		public static void SetX(this RectTransform transform, float x)
		{
			var pos = transform.anchoredPosition;
			pos.x = x;
			transform.anchoredPosition = pos;
		}

		public static void SetY(this RectTransform transform, float y)
		{
			var pos = transform.anchoredPosition;
			pos.y = y;
			transform.anchoredPosition = pos;
		}

		public static Vector2 TopLeftWithPivot(this RectTransform rect, Vector2 pivot)
		{
			var x = rect.localPosition.x - rect.rect.width * pivot.x;
			var y = rect.localPosition.y + rect.rect.height * (1 - pivot.y);
			return new Vector2(x, y);
		}

		public static Vector2 TopRightWithPivot(this RectTransform rect, Vector2 pivot)
		{
			var x = rect.localPosition.x + rect.rect.width * (1 - pivot.x);
			var y = rect.localPosition.y + rect.rect.height * (1 - pivot.y);
			return new Vector2(x, y);
		}

		public static Vector2 BotLeftWithPivot(this RectTransform rect, Vector2 pivot)
		{
			var x = rect.localPosition.x - rect.rect.width * pivot.x;
			var y = rect.localPosition.y - rect.rect.height * pivot.y;
			return new Vector2(x, y);
		}

		public static Vector2 BotRightWithPivot(this RectTransform rect, Vector2 pivot)
		{
			var x = rect.localPosition.x + rect.rect.width * (1 - pivot.x);
			var y = rect.localPosition.y - rect.rect.height * pivot.y;
			return new Vector2(x, y);
		}

		public static Vector2 TopLeft(this RectTransform rect)
		{
			return TopLeftWithPivot(rect, rect.pivot);
		}

		public static Vector2 TopRight(this RectTransform rect)
		{
			return TopRightWithPivot(rect, rect.pivot);
		}

		public static Vector2 BotLeft(this RectTransform rect)
		{
			return BotLeftWithPivot(rect, rect.pivot);
		}

		public static Vector2 BotRight(this RectTransform rect)
		{
			return BotRightWithPivot(rect, rect.pivot);
		}

		public static Vector2 BotLeft(this RectTransform rect, Vector2 pAnchor)
		{
			var pivot = rect.pivot;
			var x = rect.anchoredPosition.x - rect.rect.width * pivot.x;
			var y = rect.anchoredPosition.y - rect.rect.height * pivot.y;
			var size = rect.rect.size;
			float offsetX = (rect.anchorMin.x - pAnchor.x) * size.x;
			float offsetY = (rect.anchorMin.y - pAnchor.y) * size.y;
			x += offsetX;
			y += offsetY;
			return new Vector2(x, y);
		}

		public static Vector2 BotRight(this RectTransform rect, Vector2 pAnchor)
		{
			var pivot = rect.pivot;
			var x = rect.anchoredPosition.x + rect.rect.width * (1 - pivot.x);
			var y = rect.anchoredPosition.y - rect.rect.height * pivot.y;
			var size = rect.rect.size;
			float offsetX = (rect.anchorMin.x - pAnchor.x) * size.x;
			float offsetY = (rect.anchorMin.y - pAnchor.y) * size.y;
			x += offsetX;
			y += offsetY;
			return new Vector2(x, y);
		}

		public static Vector2 Center(this RectTransform rect)
		{
			var pivot = rect.pivot;
			var x = rect.anchoredPosition.x - rect.rect.width * pivot.x + rect.rect.width / 2f;
			var y = rect.anchoredPosition.y - rect.rect.height * pivot.y + rect.rect.height / 2f;
			return new Vector2(x, y);
		}

		/// <summary>
		/// Get corresponding anchored position of parent UI from anchored position of children
		/// Most used in scrollview when we need content move to where we can see child item
		/// </summary>
		public static Vector2 CovertAnchoredPosFromChildToParent(this RectTransform pChildRect, RectTransform pParentRect)
		{
			float contentWidth = pParentRect.rect.width;
			float contentHeight = pParentRect.rect.height;
			var itemAnchoredPos = pChildRect.anchoredPosition;
			var targetAnchored = pParentRect.anchoredPosition;
			targetAnchored.y = -itemAnchoredPos.y + (pParentRect.pivot.y - 0.5f) * contentHeight;
			targetAnchored.x = -itemAnchoredPos.x + (pParentRect.pivot.x - 0.5f) * contentWidth;
			targetAnchored.x -= contentWidth * (pChildRect.anchorMax.x - 0.5f);
			targetAnchored.y -= contentHeight * (pChildRect.anchorMax.y - 0.5f);
			return targetAnchored;
		}

		public static Vector2 CovertAnchoredPosFromChildToParent(Vector2 pChildAnchoredPos, Vector2 pChildAnchorMax, RectTransform pParentRect)
		{
			float contentWidth = pParentRect.rect.width;
			float contentHeight = pParentRect.rect.height;
			var targetAnchored = pParentRect.anchoredPosition;
			targetAnchored.y = -pChildAnchoredPos.y + (pParentRect.pivot.y - 0.5f) * contentHeight;
			targetAnchored.x = -pChildAnchoredPos.x + (pParentRect.pivot.x - 0.5f) * contentWidth;
			targetAnchored.x -= contentWidth * (pChildAnchorMax.x - 0.5f);
			targetAnchored.y -= contentHeight * (pChildAnchorMax.y - 0.5f);
			return targetAnchored;
		}

		/// <summary>
		/// Calculate bounds of content which contains many UI objects
		/// </summary>
		public static Bounds GetBounds(List<RectTransform> pItems)
		{
			var contentTopRight = Vector2.zero;
			var contentBotLeft = Vector2.zero;
			for (int i = 0; i < pItems.Count; i++)
			{
				var topRight = pItems[i].TopRight();
				if (topRight.x > contentTopRight.x)
					contentTopRight.x = topRight.x;
				if (topRight.y > contentTopRight.y)
					contentTopRight.y = topRight.y;

				var botLeft = pItems[i].BotLeft();
				if (botLeft.x < contentBotLeft.x)
					contentBotLeft.x = botLeft.x;
				if (botLeft.y < contentBotLeft.y)
					contentBotLeft.y = botLeft.y;
			}

			float height = contentTopRight.y - contentBotLeft.y;
			float width = contentTopRight.x - contentBotLeft.x;
			var bounds = new Bounds();
			bounds.size = new Vector2(width, height);
			bounds.min = contentBotLeft;
			bounds.max = contentTopRight;
			bounds.center = (contentBotLeft + contentTopRight) / 2f;
			return bounds;
		}

		public static Bounds Bounds(this RectTransform rect)
		{
			var size = new Vector2(rect.rect.width, rect.rect.height);
			return new Bounds(rect.Center(), size);
		}

		public static Vector3 WorldToCanvasPoint(this RectTransform mainCanvas, Vector3 worldPos)
		{
			Vector2 viewportPosition = Camera.main.WorldToViewportPoint(worldPos);
			var worldPosition = new Vector2(
				viewportPosition.x * mainCanvas.sizeDelta.x - mainCanvas.sizeDelta.x * 0.5f,
				viewportPosition.y * mainCanvas.sizeDelta.y - mainCanvas.sizeDelta.y * 0.5f);

			return worldPosition;
		}

		public static List<Vector2> GetScreenCoordinateOfUIRect(this RectTransform rt)
		{
			var corners = new Vector3[4];
			rt.GetWorldCorners(corners);

			var output = new List<Vector2>();
			foreach (var item in corners)
			{
				var pos = RectTransformUtility.WorldToScreenPoint(null, item);
				output.Add(pos);
			}
			return output;
		}

		public static AnchorType GetAnchorType(this RectTransform rect)
		{
			var anchorMin = rect.anchorMin;
			var anchorMax = rect.anchorMax;
			bool left, right, bot, top, center;
			left = right = bot = top = center = false;
			if (anchorMin.x == anchorMax.x && anchorMin.x == 0)
				left = true;
			if (anchorMin.x == anchorMax.x && anchorMin.x == 1)
				right = true;
			if (anchorMin.y == anchorMax.y && anchorMin.y == 0)
				bot = true;
			if (anchorMin.y == anchorMax.y && anchorMin.y == 1)
				top = true;
			if (anchorMin.y == anchorMax.y
			    && anchorMin.y == 0.5f
			    && anchorMin.x == anchorMax.x
			    && anchorMin.x == 0.5f)
				center = true;

			var type = AnchorType.Stretch;
			if (top) type = AnchorType.Top;
			if (right) type = AnchorType.Right;
			if (left) type = AnchorType.Left;
			if (bot) type = AnchorType.Bot;
			if (center) type = AnchorType.Center;
			if (top == left) type = AnchorType.TopLeft;
			if (top == right) type = AnchorType.TopRight;
			if (bot == right) type = AnchorType.BotRight;
			if (bot == left) type = AnchorType.BotLeft;
			return type;
		}

		public static void RefreshPivot(this RectTransform pRect, UIPivot pivot)
		{
			switch (pivot)
			{
				case UIPivot.Bot:
					SetPivot(pRect, new Vector2(0.5f, 0));
					break;
				case UIPivot.BotLeft:
					SetPivot(pRect, new Vector2(0, 0));
					break;
				case UIPivot.BotRight:
					SetPivot(pRect, new Vector2(1, 0));
					break;
				case UIPivot.Top:
					SetPivot(pRect, new Vector2(0.5f, 1));
					break;
				case UIPivot.TopLeft:
					SetPivot(pRect, new Vector2(0, 1f));
					break;
				case UIPivot.TopRight:
					SetPivot(pRect, new Vector2(1, 1f));
					break;
				case UIPivot.Center:
					SetPivot(pRect, new Vector2(0.5f, 0.5f));
					break;
			}
			return;

			void SetPivot(RectTransform pRectTransform, Vector2 pivot)
			{
				if (pRectTransform == null) return;

				var size = pRectTransform.rect.size;
				var deltaPivot = pRectTransform.pivot - pivot;
				var deltaPosition = new Vector3(deltaPivot.x * size.x, deltaPivot.y * size.y);
				pRectTransform.pivot = pivot;
				pRectTransform.localPosition -= deltaPosition;
			}
		}

#endregion
	}
}