using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RevCore
{
	/// <summary>Extension methods on <see cref="Component"/>, <see cref="GameObject"/>, <see cref="Image"/>, and <see cref="Sprite"/>.</summary>
	public static class ComponentExtension
	{
		/// <summary>Sets the alpha channel of an <see cref="Image"/>'s color, leaving RGB unchanged.</summary>
		public static void SetAlpha(this Image img, float alpha) { var color = img.color; color.a = alpha; img.color = color; }

		/// <summary>Sets the active state of the component's <see cref="GameObject"/>.</summary>
		public static void SetActive(this Component target, bool value) => target.gameObject.SetActive(value);

		/// <summary>Returns whether the component's <see cref="GameObject"/> is active in its hierarchy (self-active only).</summary>
		public static bool IsActive(this Component target) => target.gameObject.activeSelf;

		/// <summary>Sorts the list by <c>name</c> using ordinal comparison. Null elements are treated as empty name. Returns the list for chaining.</summary>
		public static List<T> SortByName<T>(this List<T> objects) where T : Object { objects.Sort((a, b) => string.CompareOrdinal(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty)); return objects; }

		/// <summary>Reparents the component's transform under <paramref name="parent"/>.</summary>
		public static void SetParent(this Component target, Transform parent) => target.transform.SetParent(parent);

		/// <summary>Sugar over <see cref="GameObject.GetComponentInParent{T}()"/>.</summary>
		public static T FindComponentInParent<T>(this GameObject root) where T : Component => root.GetComponentInParent<T>();

		/// <summary>Sugar over <see cref="GameObject.GetComponentInChildren{T}(bool)"/> with inactive included.</summary>
		public static T FindComponentInChildren<T>(this GameObject root) where T : Component => root.GetComponentInChildren<T>(true);

		/// <summary>Same as <see cref="FindComponentInChildren{T}(GameObject)"/> but returns via <c>out</c> for ergonomics.</summary>
		public static void FindComponentInChildren<T>(this GameObject root, out T output) where T : Component => output = root.FindComponentInChildren<T>();

		/// <summary>Returns every component of type <typeparamref name="T"/> on the root and descendants, including inactive.</summary>
		public static List<T> FindComponentsInChildren<T>(this GameObject root) where T : Component => new(root.GetComponentsInChildren<T>(true));

		/// <summary>Same as <see cref="FindComponentsInChildren{T}(GameObject)"/> but returns via <c>out</c>.</summary>
		public static void FindComponentsInChildren<T>(this GameObject root, out List<T> output) where T : Component => output = root.FindComponentsInChildren<T>();

		/// <summary>Returns the n-th component of type <typeparamref name="T"/> in the children, or <c>null</c> if <paramref name="index"/> is out of range.</summary>
		public static T FindComponentInChildrenWithIndex<T>(this GameObject root, int index) where T : Component
		{
			var components = root.GetComponentsInChildren<T>(true);
			return index >= 0 && index < components.Length ? components[index] : null;
		}

		/// <summary>
		/// Finds the first component of type <typeparamref name="T"/> on a descendant whose <see cref="Object.name"/>
		/// matches <paramref name="childName"/> (case-insensitive). When <paramref name="containChildName"/> is
		/// <c>true</c> a substring match is sufficient; otherwise exact-equal is required.
		/// </summary>
		public static T FindComponentInChildren<T>(this GameObject root, string childName, bool containChildName = false) where T : Component
		{
			var components = root.GetComponentsInChildren<T>(true);
			string lookup = childName.ToLowerInvariant();
			for (int i = 0; i < components.Length; i++)
			{
				string name = components[i].name.ToLowerInvariant();
				if (containChildName ? name.Contains(lookup) : name == lookup) return components[i];
			}
			return null;
		}

		/// <summary>Alias of <see cref="FindComponentsInChildren{T}(GameObject)"/>.</summary>
		public static List<T> FindAllComponentsInChildren<T>(this GameObject root) where T : Component => new(root.GetComponentsInChildren<T>(true));

		/// <summary>Returns every descendant <see cref="GameObject"/> below <paramref name="parent"/> (excluding the parent itself).</summary>
		public static List<GameObject> GetAllChildren(this GameObject parent) { var output = new List<GameObject>(); foreach (Transform child in parent.transform.GetAllChildren()) if (child.gameObject != parent) output.Add(child.gameObject); return output; }

		/// <summary>Finds a descendant <see cref="GameObject"/> whose name matches <paramref name="name"/> (case-insensitive, exact or contains).</summary>
		public static GameObject FindChildObject(this GameObject root, string name, bool contain = false) { var children = root.GetComponentsInChildren<Transform>(true); string lookup = name.ToLowerInvariant(); foreach (var child in children) { string childName = child.name.ToLowerInvariant(); if (contain ? childName.Contains(lookup) : childName == lookup) return child.gameObject; } return null; }

		/// <summary>Appends every descendant matching <paramref name="name"/> to <paramref name="output"/>.</summary>
		public static void FindChildObjects(this GameObject root, string name, List<GameObject> output, bool contain = false) { var children = root.GetComponentsInChildren<Transform>(true); string lookup = name.ToLowerInvariant(); foreach (var child in children) { string childName = child.name.ToLowerInvariant(); if (contain ? childName.Contains(lookup) : childName == lookup) output.Add(child.gameObject); } }

		/// <summary>Returns <c>true</c> if the collider's GameObject has any of the given tags.</summary>
		public static bool CompareTags(this Collider collider, params string[] tags) => collider.gameObject.CompareTags(tags);

		/// <summary>Returns <c>true</c> if the GameObject has any of the given tags.</summary>
		public static bool CompareTags(this GameObject gameObject, params string[] tags) { for (int i = 0; i < tags.Length; i++) if (gameObject.CompareTag(tags[i])) return true; return false; }

		/// <summary>Returns the existing <typeparamref name="T"/> on <paramref name="gameObject"/>, adding one if missing.</summary>
		public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component { var component = gameObject.GetComponent<T>(); return component != null ? component : gameObject.AddComponent<T>(); }

		/// <summary>Returns the sprite's source rectangle size in pixels, or <see cref="Vector2.zero"/> when the sprite is null.</summary>
		public static Vector2 NativeSize(this Sprite sprite) => sprite == null ? Vector2.zero : new Vector2(sprite.rect.width, sprite.rect.height);

		/// <summary>Returns the sprite's pivot in 0..1 coordinates, or <see cref="Vector2.zero"/> when sprite/rect is null/empty.</summary>
		public static Vector2 NormalizedPivot(this Sprite sprite) => sprite == null || sprite.rect.size == Vector2.zero ? Vector2.zero : sprite.pivot / sprite.rect.size;

		/// <summary>Convenience that returns the GameObject's instance ID for any component.</summary>
		public static int GameObjectId<T>(this T target) where T : Component => target.gameObject.GetInstanceID();

		/// <summary>
		/// Computes a size that fits the sprite into <paramref name="preferredSize"/> preserving aspect ratio.
		/// When <paramref name="preferNative"/> is <c>true</c> and the native size already fits, returns the native size verbatim.
		/// </summary>
		public static Vector2 Sketch(this Image image, Vector2 preferredSize, bool preferNative = false) { if (image.sprite == null) return preferredSize; var native = image.sprite.NativeSize(); if (preferNative && native.x <= preferredSize.x && native.y <= preferredSize.y) return native; float ratio = Mathf.Min(preferredSize.x / native.x, preferredSize.y / native.y); return native * ratio; }

		/// <summary>Calls <see cref="Sketch"/> and assigns the result to the image's <c>rectTransform.sizeDelta</c>. Returns the chosen size.</summary>
		public static Vector2 SetNativeSize(this Image image, Vector2 maxSize) { var size = image.Sketch(maxSize); image.rectTransform.sizeDelta = size; return size; }

		/// <summary>
		/// For a <see cref="Image.Type.Sliced"/> image, adjusts <see cref="Image.pixelsPerUnitMultiplier"/> so the
		/// sliced borders render at their native pixel size relative to the rect. No-op when the image is null,
		/// has no sprite, or is not sliced.
		/// </summary>
		public static void PerfectRatio(this Image image) { if (image == null || image.sprite == null || image.type != Image.Type.Sliced) return; var native = image.sprite.NativeSize(); var size = image.rectTransform.sizeDelta; image.pixelsPerUnitMultiplier = size.x > 0 && size.x < native.x ? native.x / size.x : 1; }

		/// <summary>
		/// Returns <c>true</c> if the GameObject is part of a prefab asset (not currently in a scene).
		/// Uses the Unity invariant that asset GameObjects have <c>scene.name == null</c>.
		/// </summary>
		public static bool IsPrefab(this GameObject target) => target.scene.name == null;

		/// <summary>
		/// Resizes the image to <paramref name="preferredWidth"/>, computing height from the sprite's aspect ratio.
		/// When <paramref name="preferNative"/> is <c>true</c> and the preferred width exceeds the native width,
		/// falls back to the native size.
		/// </summary>
		public static Vector2 SketchByWidth(this Image image, float preferredWidth, bool preferNative = false)
		{
			if (image.sprite == null) return new Vector2(preferredWidth, preferredWidth);
			var native = image.sprite.NativeSize();
			float coeff = preferredWidth / native.x;
			float sizeY = native.y * coeff;
			if (preferNative && preferredWidth > native.x) { preferredWidth = native.x; sizeY = native.y; }
			image.rectTransform.sizeDelta = new Vector2(preferredWidth, sizeY);
			return image.rectTransform.sizeDelta;
		}

		/// <summary>
		/// Resizes the image to <paramref name="preferredHeight"/>, computing width from the sprite's aspect ratio.
		/// When <paramref name="preferNative"/> is <c>true</c> and the preferred height exceeds the native height,
		/// falls back to the native size.
		/// </summary>
		public static Vector2 SketchByHeight(this Image image, float preferredHeight, bool preferNative = false)
		{
			if (image.sprite == null) return new Vector2(preferredHeight, preferredHeight);
			var native = image.sprite.NativeSize();
			float coeff = preferredHeight / native.y;
			float sizeX = native.x * coeff;
			if (preferNative && preferredHeight > native.y) { sizeX = native.x; preferredHeight = native.y; }
			image.rectTransform.sizeDelta = new Vector2(sizeX, preferredHeight);
			return image.rectTransform.sizeDelta;
		}
	}
}
