using System;
using System.Collections.Generic;
using UnityEngine;

namespace RevCore
{
	/// <summary>
	/// Extension methods on <see cref="Vector2"/>, <see cref="Vector3"/>, <see cref="Transform"/>,
	/// and <see cref="RectTransform"/>. Most methods return the target for fluent chaining.
	/// </summary>
	public static class TransformExtension
	{
		// --- Vector arithmetic (component-wise, returns copy) ---

		/// <summary>Returns <paramref name="vector"/> + <paramref name="value"/> (component-wise).</summary>
		public static Vector3 Add(this Vector3 vector, Vector3 value) { vector.x += value.x; vector.y += value.y; vector.z += value.z; return vector; }
		/// <summary>Returns a copy of <paramref name="vector"/> with <paramref name="value"/> added to <c>x</c>.</summary>
		public static Vector3 AddX(this Vector3 vector, float value) { vector.x += value; return vector; }
		/// <summary>Returns a copy of <paramref name="vector"/> with <paramref name="value"/> added to <c>y</c>.</summary>
		public static Vector3 AddY(this Vector3 vector, float value) { vector.y += value; return vector; }
		/// <summary>Returns a copy of <paramref name="vector"/> with <paramref name="value"/> added to <c>z</c>.</summary>
		public static Vector3 AddZ(this Vector3 vector, float value) { vector.z += value; return vector; }
		/// <summary>Returns a copy of <paramref name="vector"/> with <c>x</c> replaced by <paramref name="value"/>.</summary>
		public static Vector3 SetX(this Vector3 vector, float value) { vector.x = value; return vector; }
		/// <summary>Returns a copy of <paramref name="vector"/> with <c>y</c> replaced by <paramref name="value"/>.</summary>
		public static Vector3 SetY(this Vector3 vector, float value) { vector.y = value; return vector; }
		/// <summary>Returns a copy of <paramref name="vector"/> with <c>z</c> replaced by <paramref name="value"/>.</summary>
		public static Vector3 SetZ(this Vector3 vector, float value) { vector.z = value; return vector; }
		/// <summary>Drops the <c>z</c> component.</summary>
		public static Vector2 ToVector2(this Vector3 vector) => new(vector.x, vector.y);
		/// <summary>Returns a copy of <paramref name="vector"/> with <paramref name="value"/> added to <c>x</c> (promotes to Vector3 with z=0).</summary>
		public static Vector3 AddX(this Vector2 vector, float value) { vector.x += value; return vector; }
		/// <summary>Returns a copy of <paramref name="vector"/> with <paramref name="value"/> added to <c>y</c> (promotes to Vector3 with z=0).</summary>
		public static Vector3 AddY(this Vector2 vector, float value) { vector.y += value; return vector; }

		// --- Transform position / scale (mutating, return self for chaining) ---

		/// <summary>Sets the world-space <c>x</c> of <paramref name="transform"/>'s position.</summary>
		public static Transform SetPosX(this Transform transform, float x) { var pos = transform.position; pos.x = x; transform.position = pos; return transform; }
		/// <summary>Sets <c>anchoredPosition.x</c> on the rect transform.</summary>
		public static Transform SetAnchoredPosX(this RectTransform transform, float x) { var pos = transform.anchoredPosition; pos.x = x; transform.anchoredPosition = pos; return transform; }
		/// <summary>Sets <c>localPosition.x</c>.</summary>
		public static Transform SetLocalPosX(this Transform transform, float x) { var pos = transform.localPosition; pos.x = x; transform.localPosition = pos; return transform; }
		/// <summary>Adds to world-space <c>x</c>.</summary>
		public static Transform AddPosX(this Transform transform, float x) { var pos = transform.position; pos.x += x; transform.position = pos; return transform; }
		/// <summary>Sets world-space <c>y</c>.</summary>
		public static Transform SetPosY(this Transform transform, float y) { var pos = transform.position; pos.y = y; transform.position = pos; return transform; }
		/// <summary>Sets <c>localPosition.y</c>.</summary>
		public static Transform SetLocalPosY(this Transform transform, float y) { var pos = transform.localPosition; pos.y = y; transform.localPosition = pos; return transform; }
		/// <summary>Adds to world-space <c>y</c>.</summary>
		public static Transform AddPosY(this Transform transform, float y) { var pos = transform.position; pos.y += y; transform.position = pos; return transform; }
		/// <summary>Sets world-space <c>z</c>.</summary>
		public static Transform SetPosZ(this Transform transform, float z) { var pos = transform.position; pos.z = z; transform.position = pos; return transform; }
		/// <summary>Sets <c>localPosition.z</c>.</summary>
		public static Transform SetLocalPosZ(this Transform transform, float z) { var pos = transform.localPosition; pos.z = z; transform.localPosition = pos; return transform; }
		/// <summary>Adds to world-space <c>z</c>.</summary>
		public static Transform AddPosZ(this Transform transform, float z) { var pos = transform.position; pos.z += z; transform.position = pos; return transform; }
		/// <summary>Sets <c>localScale.x</c>.</summary>
		public static Transform SetScaleX(this Transform transform, float x) { var s = transform.localScale; s.x = x; transform.localScale = s; return transform; }
		/// <summary>Sets <c>localScale.y</c>.</summary>
		public static Transform SetScaleY(this Transform transform, float y) { var s = transform.localScale; s.y = y; transform.localScale = s; return transform; }
		/// <summary>Sets <c>localScale.z</c>.</summary>
		public static Transform SetScaleZ(this Transform transform, float z) { var s = transform.localScale; s.z = z; transform.localScale = s; return transform; }

		// --- Flip / reset ---

		/// <summary>Negates <c>localScale.x</c>, flipping the transform horizontally.</summary>
		public static Transform FlipX(this Transform transform) { var s = transform.localScale; s.x *= -1; transform.localScale = s; return transform; }

		/// <summary>Sets <c>|localScale.x|</c> with the sign of <paramref name="direction"/> (positive → right-facing, negative → left-facing).</summary>
		public static Transform FlipX(this Transform transform, int direction)
		{
			direction = direction > 0 ? 1 : -1;
			var s = transform.localScale; s.x = Mathf.Abs(s.x) * direction; transform.localScale = s; return transform;
		}

		/// <summary>Negates <c>localScale.y</c>, flipping the transform vertically.</summary>
		public static Transform FlipY(this Transform transform) { var s = transform.localScale; s.y *= -1; transform.localScale = s; return transform; }

		/// <summary>Sets <c>|localScale.y|</c> with the sign of <paramref name="direction"/>.</summary>
		public static Transform FlipY(this Transform transform, int direction)
		{
			direction = direction > 0 ? 1 : -1;
			var s = transform.localScale; s.y = Mathf.Abs(s.y) * direction; transform.localScale = s; return transform;
		}

		/// <summary>Resets local position to zero, local scale to one, local rotation to identity.</summary>
		public static Transform Reset(this Transform transform) { transform.localPosition = Vector3.zero; transform.localScale = Vector3.one; transform.localRotation = Quaternion.identity; return transform; }

		/// <summary>
		/// Rotates the transform to look along <paramref name="direction"/> on the XZ plane only (y component zeroed).
		/// Useful for top-down/grounded characters whose up axis must stay world-up.
		/// </summary>
		public static Transform LookAtDirection(this Transform transform, Vector3 direction) { direction.y = 0; transform.LookAt(transform.position + direction); return transform; }

		// --- Hierarchy ---

		/// <summary>Returns the immediate children of <paramref name="transform"/> as a new list.</summary>
		public static List<Transform> GetChildren(this Transform transform)
		{
			var children = new List<Transform>();
			for (int i = 0; i < transform.childCount; i++)
				children.Add(transform.GetChild(i));
			return children;
		}

		/// <summary>
		/// Sorts the immediate children of <paramref name="transform"/> by <paramref name="sortFunction"/>
		/// and applies the order via <see cref="Transform.SetSiblingIndex"/>.
		/// </summary>
		public static void Sort(this Transform transform, Func<Transform, IComparable> sortFunction)
		{
			int count = transform.childCount;
			var children = new List<Transform>(count);
			for (int i = 0; i < count; i++) children.Add(transform.GetChild(i));
			children.Sort((a, b) => sortFunction(a).CompareTo(sortFunction(b)));
			for (int i = 0; i < count; i++) children[i].SetSiblingIndex(i);
		}

		/// <summary>
		/// Breadth-first walk of <paramref name="transform"/> and all descendants. The first
		/// yielded element is the root itself.
		/// </summary>
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

		/// <summary>Returns the depth of <paramref name="transform"/> in its hierarchy (0 for a root).</summary>
		public static int HierarchyDeep(this Transform transform)
		{
			int deep = 0;
			if (transform.parent != null) deep += 1 + transform.parent.HierarchyDeep();
			return deep;
		}

		// --- RectTransform helpers ---

		/// <summary>Sets <c>anchoredPosition.x</c> on the rect transform.</summary>
		public static void SetX(this RectTransform transform, float x) { var pos = transform.anchoredPosition; pos.x = x; transform.anchoredPosition = pos; }

		/// <summary>Sets <c>anchoredPosition.y</c> on the rect transform.</summary>
		public static void SetY(this RectTransform transform, float y) { var pos = transform.anchoredPosition; pos.y = y; transform.anchoredPosition = pos; }

		/// <summary>Returns the top-left corner in the rect's local space, accounting for pivot.</summary>
		public static Vector2 TopLeft(this RectTransform rect) { var p = rect.pivot; return new Vector2(rect.localPosition.x - rect.rect.width * p.x, rect.localPosition.y + rect.rect.height * (1 - p.y)); }

		/// <summary>Returns the top-right corner in the rect's local space, accounting for pivot.</summary>
		public static Vector2 TopRight(this RectTransform rect) { var p = rect.pivot; return new Vector2(rect.localPosition.x + rect.rect.width * (1 - p.x), rect.localPosition.y + rect.rect.height * (1 - p.y)); }

		/// <summary>Returns the bottom-left corner in the rect's local space, accounting for pivot.</summary>
		public static Vector2 BotLeft(this RectTransform rect) { var p = rect.pivot; return new Vector2(rect.localPosition.x - rect.rect.width * p.x, rect.localPosition.y - rect.rect.height * p.y); }

		/// <summary>Returns the bottom-right corner in the rect's local space, accounting for pivot.</summary>
		public static Vector2 BotRight(this RectTransform rect) { var p = rect.pivot; return new Vector2(rect.localPosition.x + rect.rect.width * (1 - p.x), rect.localPosition.y - rect.rect.height * p.y); }

		/// <summary>Returns the geometric center of the rect in anchored-position space (pivot-corrected).</summary>
		public static Vector2 Center(this RectTransform rect) { var p = rect.pivot; return new Vector2(rect.anchoredPosition.x - rect.rect.width * p.x + rect.rect.width / 2f, rect.anchoredPosition.y - rect.rect.height * p.y + rect.rect.height / 2f); }

		/// <summary>Returns a 2D <see cref="UnityEngine.Bounds"/> from the rect's center and size.</summary>
		public static Bounds Bounds(this RectTransform rect) => new(rect.Center(), new Vector2(rect.rect.width, rect.rect.height));

		/// <summary>
		/// Converts <paramref name="childRect"/>'s anchored position into <paramref name="parentRect"/>'s
		/// local anchored space — useful when teleporting an item between two scroll containers without
		/// visible jump.
		/// </summary>
		public static Vector2 ConvertAnchoredPosFromChildToParent(this RectTransform childRect, RectTransform parentRect)
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

		/// <summary>
		/// Overload that takes raw values instead of reading them from a child <see cref="RectTransform"/>.
		/// Useful when the child hasn't been instantiated yet (placement preview).
		/// </summary>
		public static Vector2 ConvertAnchoredPosFromChildToParent(Vector2 childAnchoredPos, Vector2 childAnchorMax, RectTransform parentRect)
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

		/// <summary>Returns <c>true</c> when <paramref name="item"/> is anywhere in the transitive child chain of <paramref name="parent"/>.</summary>
		public static bool IsChildOfParent(this Transform item, Transform parent)
		{
			var current = item;
			while (current != null) { if (current.parent == parent) return true; current = current.parent; }
			return false;
		}
	}
}
