using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RevCore
{
	public static class ComponentExtension
	{
		public static void SetAlpha(this Image img, float alpha) { var color = img.color; color.a = alpha; img.color = color; }
		public static void SetActive(this Component target, bool value) => target.gameObject.SetActive(value);
		public static bool IsActive(this Component target) => target.gameObject.activeSelf;
		public static List<T> SortByName<T>(this List<T> objects) where T : Object { objects.Sort((a, b) => string.CompareOrdinal(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty)); return objects; }
		public static void SetParent(this Component target, Transform parent) => target.transform.SetParent(parent);
		public static T FindComponentInParent<T>(this GameObject root) where T : Component => root.GetComponentInParent<T>();
		public static T FindComponentInChildren<T>(this GameObject root) where T : Component => root.GetComponentInChildren<T>(true);
		public static void FindComponentInChildren<T>(this GameObject root, out T output) where T : Component => output = root.FindComponentInChildren<T>();
		public static List<T> FindComponentsInChildren<T>(this GameObject root) where T : Component => new(root.GetComponentsInChildren<T>(true));
		public static void FindComponentsInChildren<T>(this GameObject root, out List<T> output) where T : Component => output = root.FindComponentsInChildren<T>();

		public static T FindComponentInChildrenWithIndex<T>(this GameObject root, int index) where T : Component
		{
			var components = root.GetComponentsInChildren<T>(true);
			return index >= 0 && index < components.Length ? components[index] : null;
		}

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

		public static List<T> FindAllComponentsInChildren<T>(this GameObject root) where T : Component => new(root.GetComponentsInChildren<T>(true));
		public static List<GameObject> GetAllChildren(this GameObject parent) { var output = new List<GameObject>(); foreach (Transform child in parent.transform.GetAllChildren()) if (child.gameObject != parent) output.Add(child.gameObject); return output; }
		public static GameObject FindChildObject(this GameObject root, string name, bool contain = false) { var children = root.GetComponentsInChildren<Transform>(true); string lookup = name.ToLowerInvariant(); foreach (var child in children) { string childName = child.name.ToLowerInvariant(); if (contain ? childName.Contains(lookup) : childName == lookup) return child.gameObject; } return null; }
		public static void FindChildObjects(this GameObject root, string name, List<GameObject> output, bool contain = false) { var children = root.GetComponentsInChildren<Transform>(true); string lookup = name.ToLowerInvariant(); foreach (var child in children) { string childName = child.name.ToLowerInvariant(); if (contain ? childName.Contains(lookup) : childName == lookup) output.Add(child.gameObject); } }
		public static bool CompareTags(this Collider collider, params string[] tags) => collider.gameObject.CompareTags(tags);
		public static bool CompareTags(this GameObject gameObject, params string[] tags) { for (int i = 0; i < tags.Length; i++) if (gameObject.CompareTag(tags[i])) return true; return false; }
		public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component { var component = gameObject.GetComponent<T>(); return component != null ? component : gameObject.AddComponent<T>(); }
		public static Vector2 NativeSize(this Sprite sprite) => sprite == null ? Vector2.zero : new Vector2(sprite.rect.width, sprite.rect.height);
		public static Vector2 NormalizedPivot(this Sprite sprite) => sprite == null || sprite.rect.size == Vector2.zero ? Vector2.zero : sprite.pivot / sprite.rect.size;
		public static int GameObjectId<T>(this T target) where T : Component => target.gameObject.GetInstanceID();
		public static Vector2 Sketch(this Image image, Vector2 preferredSize, bool preferNative = false) { if (image.sprite == null) return preferredSize; var native = image.sprite.NativeSize(); if (preferNative && native.x <= preferredSize.x && native.y <= preferredSize.y) return native; float ratio = Mathf.Min(preferredSize.x / native.x, preferredSize.y / native.y); return native * ratio; }
		public static Vector2 SetNativeSize(this Image image, Vector2 maxSize) { var size = image.Sketch(maxSize); image.rectTransform.sizeDelta = size; return size; }
		public static void PerfectRatio(this Image image) { if (image == null || image.sprite == null || image.type != Image.Type.Sliced) return; var native = image.sprite.NativeSize(); var size = image.rectTransform.sizeDelta; image.pixelsPerUnitMultiplier = size.x > 0 && size.x < native.x ? native.x / size.x : 1; }
	}
}
