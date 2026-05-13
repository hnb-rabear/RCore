using System;
using System.Collections.Generic;
using UnityEngine;

namespace RevCore
{
	public static class TransformExtension
	{
		public static Vector3 Add(this Vector3 vector, Vector3 value) { vector.x += value.x; vector.y += value.y; vector.z += value.z; return vector; }
		public static Vector3 AddX(this Vector3 vector, float value) { vector.x += value; return vector; }
		public static Vector3 AddY(this Vector3 vector, float value) { vector.y += value; return vector; }
		public static Vector3 AddZ(this Vector3 vector, float value) { vector.z += value; return vector; }
		public static Vector3 SetX(this Vector3 vector, float value) { vector.x = value; return vector; }
		public static Vector3 SetY(this Vector3 vector, float value) { vector.y = value; return vector; }
		public static Vector3 SetZ(this Vector3 vector, float value) { vector.z = value; return vector; }
		public static Vector2 ToVector2(this Vector3 vector) => new(vector.x, vector.y);
		public static Vector3 AddX(this Vector2 vector, float value) { vector.x += value; return vector; }
		public static Vector3 AddY(this Vector2 vector, float value) { vector.y += value; return vector; }

		public static Transform SetPosX(this Transform transform, float x) { var pos = transform.position; pos.x = x; transform.position = pos; return transform; }
		public static Transform SetAnchoredPosX(this RectTransform transform, float x) { var pos = transform.anchoredPosition; pos.x = x; transform.anchoredPosition = pos; return transform; }
		public static Transform SetLocalPosX(this Transform transform, float x) { var pos = transform.localPosition; pos.x = x; transform.localPosition = pos; return transform; }
		public static Transform AddPosX(this Transform transform, float x) { var pos = transform.position; pos.x += x; transform.position = pos; return transform; }
		public static Transform SetPosY(this Transform transform, float y) { var pos = transform.position; pos.y = y; transform.position = pos; return transform; }
		public static Transform SetLocalPosY(this Transform transform, float y) { var pos = transform.localPosition; pos.y = y; transform.localPosition = pos; return transform; }
		public static Transform AddPosY(this Transform transform, float y) { var pos = transform.position; pos.y += y; transform.position = pos; return transform; }
		public static Transform SetPosZ(this Transform transform, float z) { var pos = transform.position; pos.z = z; transform.position = pos; return transform; }
		public static Transform SetLocalPosZ(this Transform transform, float z) { var pos = transform.localPosition; pos.z = z; transform.localPosition = pos; return transform; }
		public static Transform AddPosZ(this Transform transform, float z) { var pos = transform.position; pos.z += z; transform.position = pos; return transform; }
		public static Transform SetScaleX(this Transform transform, float x) { var s = transform.localScale; s.x = x; transform.localScale = s; return transform; }
		public static Transform SetScaleY(this Transform transform, float y) { var s = transform.localScale; s.y = y; transform.localScale = s; return transform; }
		public static Transform SetScaleZ(this Transform transform, float z) { var s = transform.localScale; s.z = z; transform.localScale = s; return transform; }

		public static Transform FlipX(this Transform transform) { var s = transform.localScale; s.x *= -1; transform.localScale = s; return transform; }
		public static Transform FlipX(this Transform transform, int direction)
		{
			direction = direction > 0 ? 1 : -1;
			var s = transform.localScale; s.x = Mathf.Abs(s.x) * direction; transform.localScale = s; return transform;
		}
		public static Transform FlipY(this Transform transform) { var s = transform.localScale; s.y *= -1; transform.localScale = s; return transform; }
		public static Transform FlipY(this Transform transform, int direction)
		{
			direction = direction > 0 ? 1 : -1;
			var s = transform.localScale; s.y = Mathf.Abs(s.y) * direction; transform.localScale = s; return transform;
		}

		public static Transform Reset(this Transform transform) { transform.localPosition = Vector3.zero; transform.localScale = Vector3.one; transform.localRotation = Quaternion.identity; return transform; }
		public static Transform LookAtDirection(this Transform transform, Vector3 direction) { direction.y = 0; transform.LookAt(transform.position + direction); return transform; }

		public static List<Transform> GetChildren(this Transform transform)
		{
			var children = new List<Transform>();
			for (int i = 0; i < transform.childCount; i++)
				children.Add(transform.GetChild(i));
			return children;
		}

		public static void Sort(this Transform transform, Func<Transform, IComparable> sortFunction)
		{
			int count = transform.childCount;
			var children = new List<Transform>(count);
			for (int i = 0; i < count; i++) children.Add(transform.GetChild(i));
			children.Sort((a, b) => sortFunction(a).CompareTo(sortFunction(b)));
			for (int i = 0; i < count; i++) children[i].SetSiblingIndex(i);
		}

		public static IEnumerable<Transform> GetAllChildren(this Transform transform)
		{
			var queue = new Queue<Transform>();
			queue.Enqueue(transform);
			while (queue.Count > 0)
			{
				var current = queue.Dequeue();
				yield return current;
				foreach (Transform child in current) queue.Enqueue(child);
			}
		}

		public static int HierarchyDeep(this Transform transform)
		{
			int deep = 0;
			if (transform.parent != null) deep += 1 + transform.parent.HierarchyDeep();
			return deep;
		}

		public static void SetX(this RectTransform transform, float x) { var pos = transform.anchoredPosition; pos.x = x; transform.anchoredPosition = pos; }
		public static void SetY(this RectTransform transform, float y) { var pos = transform.anchoredPosition; pos.y = y; transform.anchoredPosition = pos; }

		public static Vector2 TopLeft(this RectTransform rect) { var p = rect.pivot; return new Vector2(rect.localPosition.x - rect.rect.width * p.x, rect.localPosition.y + rect.rect.height * (1 - p.y)); }
		public static Vector2 TopRight(this RectTransform rect) { var p = rect.pivot; return new Vector2(rect.localPosition.x + rect.rect.width * (1 - p.x), rect.localPosition.y + rect.rect.height * (1 - p.y)); }
		public static Vector2 BotLeft(this RectTransform rect) { var p = rect.pivot; return new Vector2(rect.localPosition.x - rect.rect.width * p.x, rect.localPosition.y - rect.rect.height * p.y); }
		public static Vector2 BotRight(this RectTransform rect) { var p = rect.pivot; return new Vector2(rect.localPosition.x + rect.rect.width * (1 - p.x), rect.localPosition.y - rect.rect.height * p.y); }
		public static Vector2 Center(this RectTransform rect) { var p = rect.pivot; return new Vector2(rect.anchoredPosition.x - rect.rect.width * p.x + rect.rect.width / 2f, rect.anchoredPosition.y - rect.rect.height * p.y + rect.rect.height / 2f); }
		public static Bounds Bounds(this RectTransform rect) => new(rect.Center(), new Vector2(rect.rect.width, rect.rect.height));

		public static Vector2 CovertAnchoredPosFromChildToParent(this RectTransform childRect, RectTransform parentRect)
		{
			float contentWidth = parentRect.rect.width;
			float contentHeight = parentRect.rect.height;
			var itemAnchoredPos = childRect.anchoredPosition;
			var targetAnchored = parentRect.anchoredPosition;
			targetAnchored.y = -itemAnchoredPos.y + (parentRect.pivot.y - 0.5f) * contentHeight;
			targetAnchored.x = -itemAnchoredPos.x + (parentRect.pivot.x - 0.5f) * contentWidth;
			targetAnchored.x -= contentWidth * (childRect.anchorMax.x - 0.5f);
			targetAnchored.y -= contentHeight * (childRect.anchorMax.y - 0.5f);
			return targetAnchored;
		}

		public static Vector2 CovertAnchoredPosFromChildToParent(Vector2 childAnchoredPos, Vector2 childAnchorMax, RectTransform parentRect)
		{
			float contentWidth = parentRect.rect.width;
			float contentHeight = parentRect.rect.height;
			var targetAnchored = parentRect.anchoredPosition;
			targetAnchored.y = -childAnchoredPos.y + (parentRect.pivot.y - 0.5f) * contentHeight;
			targetAnchored.x = -childAnchoredPos.x + (parentRect.pivot.x - 0.5f) * contentWidth;
			targetAnchored.x -= contentWidth * (childAnchorMax.x - 0.5f);
			targetAnchored.y -= contentHeight * (childAnchorMax.y - 0.5f);
			return targetAnchored;
		}

		public static bool IsChildOfParent(this Transform item, Transform parent)
		{
			var current = item;
			while (current != null) { if (current.parent == parent) return true; current = current.parent; }
			return false;
		}
	}
}
