/***
 * Author HNB-RaBear - 2017
 */

#if DOTWEEN
using DG.Tweening;
#endif
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;

namespace RCore
{
	public static class ComponentHelper
	{
		public static void ReorderSortingOfSpriteRenderers(SpriteRenderer[] pItems)
		{
			var dict = new Dictionary<SpriteRenderer, int>();
			foreach (var item in pItems)
				dict.Add(item, item.sortingOrder);

			var sortedDict = dict.OrderBy(x => x.Value);

			int order = -1;
			int lastSortingOrder = -1;
			foreach (var item in sortedDict)
			{
				if (lastSortingOrder < item.Key.sortingOrder)
				{
					order++;
					lastSortingOrder = item.Key.sortingOrder;
				}
				item.Key.sortingOrder = order;
			}
		}
	}
	
	public static class ComponentExtension
	{
		public static void SetAlpha(this UnityEngine.UI.Image img, float alpha)
		{
			var color = img.color;
			color.a = alpha;
			img.color = color;
		}

		public static void SetActive(this Component target, bool value)
		{
			try
			{
				target.gameObject.SetActive(value);
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.ToString());
			}
		}

		public static bool IsActive(this Component target)
		{
			try
			{
				return target.gameObject.activeSelf;
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.ToString());
				return false;
			}
		}

		public static List<T> SortByName<T>(this List<T> objects) where T : UnityEngine.Object
		{
			return objects.OrderBy(m => m.name).ToList();
		}

		public static void SetParent(this Component target, Transform parent)
		{
			target.transform.SetParent(parent);
		}

		public static T FindComponentInParent<T>(this GameObject objRoot) where T : Component
		{
			objRoot.TryGetComponent(out T component);

			if (component == null && objRoot.transform.parent != null)
				component = objRoot.transform.parent.gameObject.FindComponentInParent<T>();

			return component;
		}

		public static T FindComponentInChildren<T>(this GameObject objRoot) where T : Component
		{
			// if we don't find the component in this object 
			// recursively iterate children until we do
#if UNITY_2019_2_OR_NEWER
			objRoot.TryGetComponent(out T component);
#else
            T component = objRoot.GetComponentInChildren<T>(true);
#endif
			if (null == component)
			{
				// transform is what makes the hierarchy of GameObjects, so 
				// need to access it to iterate children
				var trnsRoot = objRoot.transform;
				int iNumChildren = trnsRoot.childCount;

				// could have used foreach(), but it causes GC churn
				for (int iChild = 0; iChild < iNumChildren; ++iChild)
				{
					// recursive call to this function for each child
					// break out of the loop and return as soon as we find 
					// a component of the specified type
					component = FindComponentInChildren<T>(trnsRoot.GetChild(iChild).gameObject);
					if (null != component)
					{
						break;
					}
				}
			}

			return component;
		}

		public static void FindComponentInChildren<T>(this GameObject objRoot, out T pOutput) where T : Component
		{
			pOutput = objRoot.FindComponentInChildren<T>();
		}

		public static T FindComponentInChildrenWithIndex<T>(this GameObject objRoot, int pIndex) where T : Component
		{
			int index = -1;
#if UNITY_2019_2_OR_NEWER
			objRoot.TryGetComponent(out T component);
#else
            T component = objRoot.GetComponent<T>();
#endif
			if (component != null)
			{
				index++;
				if (index == pIndex)
					return component;
			}

			foreach (Transform t in objRoot.transform)
			{
				var components = FindComponentsInChildren<T>(t.gameObject);
				if (components != null)
				{
					for (int i = 0; i < components.Count; i++)
					{
						index++;
						if (index == pIndex)
							return component;
					}
				}
			}
			return null;
		}

		public static List<T> FindComponentsInChildren<T>(this GameObject objRoot) where T : Component
		{
			var list = new List<T>();
#if UNITY_2019_2_OR_NEWER
			objRoot.TryGetComponent(out T component);
#else
            T component = objRoot.GetComponent<T>();
#endif

			if (component != null)
				list.Add(component);

			foreach (Transform t in objRoot.transform)
			{
				var components = FindComponentsInChildren<T>(t.gameObject);
				if (components != null)
					list.AddRange(components);
			}

			return list;
		}

		public static void FindComponentsInChildren<T>(this GameObject objRoot, out List<T> pOutput) where T : Component
		{
			pOutput = objRoot.FindComponentsInChildren<T>();
		}

		public static T FindComponentInChildren<T>(this GameObject objRoot, string pChildName, bool pContainChildName = false) where T : Component
		{
			var components = objRoot.FindComponentsInChildren<T>();
			foreach (var component in components)
			{
				if (component.name.ToLower() == pChildName.ToLower() || pContainChildName && component.name.ToLower().Contains(pChildName))
					return component;
			}
			return null;
		}

		public static List<T> FindAllComponentsInChildren<T>(this GameObject objRoot) where T : Component
		{
			var list = new List<T>();
			var component = objRoot.GetComponents<T>();

			if (component != null)
				list.AddRange(component);

			foreach (Transform t in objRoot.transform)
			{
				var components = FindAllComponentsInChildren<T>(t.gameObject);
				if (components != null)
					list.AddRange(components);
			}

			return list;
		}

		public static List<GameObject> GetAllChildren(this GameObject pParent)
		{
			var list = new List<GameObject>();
			foreach (Transform t in pParent.transform)
			{
				list.Add(t.gameObject);
				if (t.childCount > 0)
				{
					var children = GetAllChildren(t.gameObject);
					list.AddRange(children);
				}
			}
			return list;
		}

		public static List<string> ToList(this string[] pArray)
		{
			var list = new List<string>();
			for (int i = 0; i < pArray.Length; i++)
				list.Add(pArray[i]);
			return list;
		}

		public static GameObject FindChildObject(this GameObject objRoot, string pName, bool pContain = false)
		{
			GameObject obj;
			bool found = !pContain ? objRoot.name.ToLower() == pName.ToLower() : objRoot.name.ToLower().Contains(pName.ToLower());
			if (found)
			{
				obj = objRoot;
				return obj;
			}
			var trnsRoot = objRoot.transform;
			int iNumChildren = trnsRoot.childCount;
			for (int i = 0; i < iNumChildren; ++i)
			{
				obj = trnsRoot.GetChild(i).gameObject.FindChildObject(pName, pContain);
				if (obj != null)
				{
					return obj;
				}
			}

			return null;
		}

		public static void FindChildObjects(this GameObject objRoot, string pName, List<GameObject> pOutput, bool pContain = false)
		{
			bool found = !pContain ? objRoot.name == pName : objRoot.name.Contains(pName);
			if (found)
				pOutput.Add(objRoot);
			var trnsRoot = objRoot.transform;
			int iNumChildren = trnsRoot.childCount;
			for (int i = 0; i < iNumChildren; ++i)
				trnsRoot.GetChild(i).gameObject.FindChildObjects(pName, pOutput, pContain);
		}

		public static void StopMove(this UnityEngine.AI.NavMeshAgent pAgent, bool pStop)
		{
			if (pAgent.gameObject.activeSelf || !pAgent.enabled || !pAgent.isOnNavMesh)
				return;
			pAgent.isStopped = pStop;
		}

		public static bool CompareTags(this Collider collider, params string[] tags)
		{
			for (int i = 0; i < tags.Length; i++)
			{
				if (collider.CompareTag(tags[i]))
					return true;
			}
			return false;
		}

		public static bool CompareTags(this GameObject gameObject, params string[] tags)
		{
			for (int i = 0; i < tags.Length; i++)
			{
				if (gameObject.CompareTag(tags[i]))
					return true;
			}
			return false;
		}

		public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
		{
			var component = gameObject.GetComponent<T>();
			return component != null ? component : gameObject.AddComponent<T>();
		}
		
#region Simple Pool

		public static T Obtain<T>(this List<T> pool, GameObject prefab, Transform parent, string name = null) where T : Component
		{
			for (int i = 0; i < pool.Count; i++)
			{
				if (!pool[i].gameObject.activeSelf)
				{
					pool[i].SetParent(parent);
					pool[i].transform.localPosition = Vector3.zero;
					return pool[i];
				}
			}

			var temp = UnityEngine.Object.Instantiate(prefab, parent);
			temp.name = name ?? prefab.name;
			temp.transform.localPosition = Vector3.zero;
#if UNITY_2019_2_OR_NEWER
			temp.TryGetComponent(out T t);
#else
            var t = temp.GetComponent<T>();
#endif
			pool.Add(t);

			return t;
		}

		public static T Obtain<T>(this List<T> pool, Transform parent, string name = null) where T : Component
		{
			for (int i = 0; i < pool.Count; i++)
			{
				if (!pool[i].gameObject.activeSelf)
				{
					pool[i].SetParent(parent);
					pool[i].transform.localPosition = Vector3.zero;
					return pool[i];
				}
			}

			var temp = UnityEngine.Object.Instantiate(pool[0].gameObject, parent);
			temp.name = name ?? $"{pool[0].name}_{pool.Count() + 1}";
			temp.transform.localPosition = Vector3.zero;
#if UNITY_2019_2_OR_NEWER
			temp.TryGetComponent(out T t);
#else
            var t = temp.GetComponent<T>();
#endif
			pool.Add(t);

			return t;
		}

		public static T Obtain<T>(this List<T> pool, Transform pParent, int max, string pName = null) where T : Component
		{
			for (int i = pool.Count - 1; i >= 0; i--)
			{
				if (!pool[i].gameObject.activeSelf)
				{
					var obj = pool[i];
					pool.RemoveAt(i); //Temporary remove to push this item to bottom of list latter
					Transform transform;
					(transform = obj.transform).SetParent(pParent);
					transform.localPosition = Vector3.zero;
					transform.localScale = Vector3.one;
					pool.Add(obj);
					return obj;
				}
			}

			if (max > 1 && max > pool.Count)
			{
				var temp = UnityEngine.Object.Instantiate(pool[0], pParent);
				pool.Add(temp);
				var transform = temp.transform;
				transform.localPosition = Vector3.zero;
				transform.localScale = Vector3.one;
				if (!string.IsNullOrEmpty(pName))
					temp.name = pName;
				return temp;
			}
			else
			{
				var obj = pool[pool.Count - 1];
				pool.RemoveAt(pool.Count - 1);
				pool.Add(obj);
				return obj;
			}
		}

		public static void Free<T>(this List<T> pool) where T : Component
		{
			foreach (var t in pool)
				t.SetActive(false);
		}

		public static void Free<T>(this List<T> pool, Transform pParent) where T : Component
		{
			for (int i = 0; i < pool.Count; i++)
			{
				pool[i].SetParent(pParent);
				pool[i].SetActive(false);
			}
		}

		public static void Prepare<T>(this List<T> pool, GameObject prefab, Transform parent, int count) where T : Component
		{
			for (int i = 0; i < count; i++)
			{
				var temp = UnityEngine.Object.Instantiate(prefab, parent);
				temp.SetActive(false);
#if UNITY_2019_2_OR_NEWER
				temp.TryGetComponent(out T t);
#else
                var t = temp.GetComponent<T>();
#endif
				pool.Add(t);
			}
		}

		public static T Obtain<T>(this List<T> pool, T prefab, Transform parent) where T : Component
		{
			for (int i = 0; i < pool.Count; i++)
			{
				if (!pool[i].gameObject.activeSelf)
				{
					pool[i].transform.SetParent(parent);
					return pool[i];
				}
			}

			var temp = UnityEngine.Object.Instantiate(prefab, parent);
			temp.name = prefab.name;
			pool.Add(temp);

			return temp;
		}

		public static void Prepare<T>(this List<T> pool, T prefab, Transform parent, int count, string name = "") where T : Component
		{
			for (int i = 0; i < count; i++)
			{
				var temp = UnityEngine.Object.Instantiate(prefab, parent);
				temp.SetActive(false);
				if (!string.IsNullOrEmpty(name))
					temp.name = name;
				pool.Add(temp);
			}
		}

		public static GameObject Obtain(this List<GameObject> pool, GameObject prefab, Transform parent, string name = null)
		{
			for (int i = 0; i < pool.Count; i++)
			{
				if (!pool[i].gameObject.activeSelf)
				{
					pool[i].transform.SetParent(parent);
					pool[i].transform.localPosition = Vector3.zero;
					return pool[i];
				}
			}

			var temp = UnityEngine.Object.Instantiate(prefab, parent);
			temp.name = name ?? prefab.name;
			temp.transform.localPosition = Vector3.zero;
			pool.Add(temp);
			return temp;
		}

		public static GameObject Obtain(this List<GameObject> pool, Transform parent, string name = null)
		{
			for (int i = 0; i < pool.Count; i++)
			{
				if (!pool[i].gameObject.activeSelf)
				{
					pool[i].transform.SetParent(parent);
					pool[i].transform.localPosition = Vector3.zero;
					return pool[i];
				}
			}

			var temp = UnityEngine.Object.Instantiate(pool[0].gameObject, parent);
			temp.name = name ?? $"{pool[0].name}_{pool.Count() + 1}";
			temp.transform.localPosition = Vector3.zero;
			pool.Add(temp);
			return temp;
		}

		public static GameObject Obtain(this List<GameObject> pool, Transform pParent, int max, string pName = null)
		{
			for (int i = pool.Count - 1; i >= 0; i--)
			{
				if (!pool[i].gameObject.activeSelf)
				{
					var obj = pool[i];
					pool.RemoveAt(i); //Temporary remove to push this item to bottom of list latter
					Transform transform;
					(transform = obj.transform).SetParent(pParent);
					transform.localPosition = Vector3.zero;
					transform.localScale = Vector3.one;
					pool.Add(obj);
					return obj;
				}
			}

			if (max > 1 && max > pool.Count)
			{
				var temp = UnityEngine.Object.Instantiate(pool[0], pParent);
				pool.Add(temp);
				var transform = temp.transform;
				transform.localPosition = Vector3.zero;
				transform.localScale = Vector3.one;
				if (!string.IsNullOrEmpty(pName))
					temp.name = pName;
				return temp;
			}
			else
			{
				var obj = pool[pool.Count - 1];
				pool.RemoveAt(pool.Count - 1);
				pool.Add(obj);
				return obj;
			}
		}

		public static void Free(this List<GameObject> pool)
		{
			foreach (var t in pool)
				t.SetActive(false);
		}

		public static void Free(this List<GameObject> pool, Transform pParent)
		{
			for (int i = 0; i < pool.Count; i++)
			{
				pool[i].transform.SetParent(pParent);
				pool[i].SetActive(false);
			}
		}

		public static void Prepare(this List<GameObject> pool, GameObject prefab, Transform parent, int count)
		{
			for (int i = 0; i < count; i++)
			{
				var temp = UnityEngine.Object.Instantiate(prefab, parent);
				temp.SetActive(false);
				pool.Add(temp);
			}
		}

		public static GameObject Obtain(this List<GameObject> pool, GameObject prefab, Transform parent)
		{
			for (int i = 0; i < pool.Count; i++)
			{
				if (!pool[i].gameObject.activeSelf)
				{
					pool[i].transform.SetParent(parent);
					return pool[i];
				}
			}

			var temp = UnityEngine.Object.Instantiate(prefab, parent);
			temp.name = prefab.name;
			pool.Add(temp);

			return temp;
		}

		public static void Prepare(this List<GameObject> pool, GameObject prefab, Transform parent, int count, string name = "")
		{
			for (int i = 0; i < count; i++)
			{
				var temp = UnityEngine.Object.Instantiate(prefab, parent);
				temp.SetActive(false);
				if (!string.IsNullOrEmpty(name))
					temp.name = name;
				pool.Add(temp);
			}
		}
#endregion

		public static T Find<T>(this List<T> pList, string pName) where T : Component
		{
			for (int i = 0; i < pList.Count; i++)
			{
				if (pList[i].name == pName)
					return pList[i];
			}
			return null;
		}

		public static T Find<T>(this T[] pList, string pName) where T : Component
		{
			for (int i = 0; i < pList.Length; i++)
			{
				if (pList[i].name == pName)
					return pList[i];
			}
			return null;
		}

		public static bool IsPrefab(this GameObject target)
		{
			return target.scene.name == null;
		}

		public static Vector2 NativeSize(this Sprite pSprite)
		{
			var sizeX = pSprite.bounds.size.x * pSprite.pixelsPerUnit;
			var sizeY = pSprite.bounds.size.y * pSprite.pixelsPerUnit;
			return new Vector2(sizeX, sizeY);
		}

		public static Vector2 NormalizedPivot(this Sprite pSprite)
		{
			float x = pSprite.pivot.x / pSprite.bounds.size.x / pSprite.pixelsPerUnit;
			float y = pSprite.pivot.y / pSprite.bounds.size.y / pSprite.pixelsPerUnit;
			var normalizedPivot = new Vector2(MathHelper.Round(x, 4), MathHelper.Round(y, 4));
			return normalizedPivot;
		}

		public static int GameObjectId<T>(this T target) where T : Component
		{
			return target.gameObject.GetInstanceID();
		}

		//===================================================

#region Image

		/// <summary>
		/// Sketch image following preferred with
		/// </summary>
		public static Vector2 SketchByHeight(this UnityEngine.UI.Image pImage, float pPreferredHeight, bool pPreferNative = false)
		{
			if (pImage.sprite == null)
				return new Vector2(pPreferredHeight, pPreferredHeight);

			var nativeSizeX = pImage.sprite.bounds.size.x * pImage.sprite.pixelsPerUnit;
			var nativeSizeY = pImage.sprite.bounds.size.y * pImage.sprite.pixelsPerUnit;
			float coeff = pPreferredHeight / nativeSizeY;
			float sizeX = nativeSizeX * coeff;
			if (pPreferNative && pPreferredHeight > nativeSizeY)
			{
				sizeX = nativeSizeX;
				pPreferredHeight = nativeSizeY;
			}
			pImage.rectTransform.sizeDelta = new Vector2(sizeX, pPreferredHeight);
			return pImage.rectTransform.sizeDelta;
		}

		/// <summary>
		/// Sketch image following preferred with
		/// </summary>
		public static Vector2 SketchByWidth(this UnityEngine.UI.Image pImage, float pPreferredWith, bool pPreferNative = false)
		{
			if (pImage.sprite == null)
				return new Vector2(pPreferredWith, pPreferredWith);

			var nativeSizeX = pImage.sprite.bounds.size.x * pImage.sprite.pixelsPerUnit;
			var nativeSizeY = pImage.sprite.bounds.size.y * pImage.sprite.pixelsPerUnit;
			float coeff = pPreferredWith / nativeSizeX;
			float sizeY = nativeSizeY * coeff;
			if (pPreferNative && pPreferredWith > nativeSizeX)
			{
				pPreferredWith = nativeSizeX;
				sizeY = nativeSizeY;
			}
			pImage.rectTransform.sizeDelta = new Vector2(pPreferredWith, sizeY);
			return pImage.rectTransform.sizeDelta;
		}

		public static Vector2 Sketch(this UnityEngine.UI.Image pImage, Vector2 pPreferredSize, bool pPreferNative = false)
		{
			if (pImage.sprite == null)
				return pPreferredSize;

			var nativeSizeX = pImage.sprite.bounds.size.x * pImage.sprite.pixelsPerUnit;
			var nativeSizeY = pImage.sprite.bounds.size.y * pImage.sprite.pixelsPerUnit;
			float coeffX = pPreferredSize.x / nativeSizeX;
			float coeffY = pPreferredSize.y / nativeSizeY;
			float sizeX;
			float sizeY;
			if (coeffX > coeffY)
			{
				sizeX = nativeSizeX * coeffY;
				sizeY = nativeSizeY * coeffY;
			}
			else
			{
				sizeX = nativeSizeX * coeffX;
				sizeY = nativeSizeY * coeffX;
			}
			if (pPreferNative && (sizeX > nativeSizeX || sizeY > nativeSizeY))
			{
				sizeX = nativeSizeX;
				sizeY = nativeSizeY;
			}
			pImage.rectTransform.sizeDelta = new Vector2(sizeX, sizeY);
			return pImage.rectTransform.sizeDelta;
		}

		public static Vector2 SetNativeSize(this UnityEngine.UI.Image pImage, Vector2 pMaxSize)
		{
			if (pImage.sprite == null)
			{
				pImage.rectTransform.sizeDelta = pMaxSize;
				return pImage.rectTransform.sizeDelta;
			}
			var nativeSizeX = pImage.sprite.bounds.size.x * pImage.sprite.pixelsPerUnit;
			var nativeSizeY = pImage.sprite.bounds.size.y * pImage.sprite.pixelsPerUnit;
			if (nativeSizeX > pMaxSize.x)
				nativeSizeX = pMaxSize.x;
			if (nativeSizeY > pMaxSize.y)
				nativeSizeY = pMaxSize.y;
			pImage.rectTransform.sizeDelta = new Vector2(nativeSizeX, nativeSizeY);
			return pImage.rectTransform.sizeDelta;
		}

		public static void PerfectRatio(this UnityEngine.UI.Image image)
		{
			if (image != null && image.sprite != null)
			{
				if (image.type == UnityEngine.UI.Image.Type.Sliced)
				{
					var nativeSize = image.sprite.NativeSize();
					var rectSize = image.rectTransform.sizeDelta;
					if (rectSize.y > 0 && rectSize.y < nativeSize.y)
					{
						var ratio = nativeSize.y * 1f / rectSize.y;
						image.pixelsPerUnitMultiplier = ratio;
					}
					else if (rectSize.x > 0 && rectSize.x < nativeSize.x)
					{
						var ratio = nativeSize.x * 1f / rectSize.x;
						image.pixelsPerUnitMultiplier = ratio;
					}
					else
						image.pixelsPerUnitMultiplier = 1;
				}
				else
				{
					var nativeSize = image.sprite.NativeSize();
					var nativeRatio = nativeSize.x / nativeSize.y;
					
					var size = image.rectTransform.sizeDelta;
					var ratio = size.x / size.y;

					if (nativeRatio > ratio) //
						size.x = nativeSize.x * size.y / nativeSize.y;
					else if (nativeRatio < ratio)
						size.y = size.x * nativeSize.y / nativeSize.x;
						
					image.rectTransform.sizeDelta = size;
				}
			}
		}

		public static void FillSmooth(this UnityEngine.UI.Image pImage, float pFill, float durationFull, float durationMin)
		{
#if DOTWEEN
			if (Application.isPlaying && durationFull > 0 && pImage.fillAmount != pFill)
			{
				float previousFill = pImage.fillAmount;
				float duration = Mathf.Abs(previousFill - pFill * 1f) * durationFull;
				if (durationMin > 0 && duration < durationMin)
					duration = durationMin;
				float lerp = previousFill;
				DOTween.Kill(pImage.GetInstanceID());
				DOTween.To(() => lerp, x => lerp = x, pFill, duration)
					.OnUpdate(() =>
					{
						pImage.fillAmount = lerp;
					})
					.OnComplete(() =>
					{
						pImage.fillAmount = pFill;
					})
					.SetUpdate(true)
					.SetId(pImage.GetInstanceID());
			}
#endif
		}

#endregion

		//===================================================
	}
}