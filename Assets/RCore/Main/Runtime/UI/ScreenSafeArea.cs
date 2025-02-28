#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using System;
using System.Collections;
using UnityEngine;

namespace RCore.UI
{
	public class ScreenSafeArea : MonoBehaviour
	{
		public static Action OnOffsetChanged;
		
		public float topOffset;
		public float bottomOffset;
		public Canvas canvas;
		public RectTransform[] safeRects;
		public bool fixedTop;
		public bool fixedBottom;

		private void Start()
		{
			CheckSafeArea();
		}

#if ODIN_INSPECTOR
		[Button]
#else
		[InspectorButton]
#endif
		public void Log()
		{
			var safeArea = Screen.safeArea;
			var sWidth = Screen.currentResolution.width;
			var sHeight = Screen.currentResolution.height;
			var oWidthTop = (Screen.currentResolution.width - safeArea.width - safeArea.x) / 2f;
			var oHeightTop = (Screen.currentResolution.height - safeArea.height - safeArea.y) / 2f;
			var oWidthBot = -safeArea.x / 2f;
			var oHeightBot = -safeArea.y / 2f;
			UnityEngine.Debug.Log($"Screen size: (width:{sWidth}, height:{sHeight})"
				+ $"\nSafe area: {safeArea}"
				+ $"\nOffset Top: (width:{oWidthTop}, height:{oHeightTop})"
				+ $"\nOffset Bottom: (width:{oWidthBot}, height:{oHeightBot})");
		}

#if ODIN_INSPECTOR
		[Button]
#else
		[InspectorButton]
#endif
		private void Validate()
		{
			CheckSafeArea();
		}

		private void CheckSafeArea()
		{
			var safeArea = Screen.safeArea;
			safeArea.height -= topOffset;
			if (fixedTop)
			{
				safeArea.height += safeArea.y;
			}
			if (fixedBottom)
			{
				safeArea.height += safeArea.y;
				safeArea.y = 0;
			}
			var anchorMin = safeArea.position;
			var anchorMax = safeArea.position + safeArea.size;

			var sizeDelta = ((RectTransform)canvas.transform).sizeDelta;
			sizeDelta.y = -bottomOffset;
			((RectTransform)canvas.transform).sizeDelta = sizeDelta;
			var position = ((RectTransform)canvas.transform).anchoredPosition;
			position.y = bottomOffset / 2;
			((RectTransform)canvas.transform).anchoredPosition = position;
			
			var pixelRect = canvas.pixelRect;
			anchorMin.x /= pixelRect.width;
			anchorMin.y /= pixelRect.height;
			anchorMax.x /= pixelRect.width;
			anchorMax.y /= pixelRect.height;

			foreach (var rect in safeRects)
			{
				rect.anchorMin = anchorMin;
				rect.anchorMax = anchorMax;
			}
		}

		public static float TopOffset;
		public static void SetTopOffsetForBannerAd(float pBannerHeight, bool pPlaceInSafeArea = true)
		{
			float offset = 0;
			var safeAreaHeightOffer = Screen.height - Screen.safeArea.height;
			if (!pPlaceInSafeArea)
			{
				if (pBannerHeight <= safeAreaHeightOffer)
					offset = 0;
				else
					offset = pBannerHeight - safeAreaHeightOffer;
			}
			else
			{
				offset = pBannerHeight;
			}
			
			TopOffset = offset;
			var ScreenSafeAreas = FindObjectsOfType<ScreenSafeArea>();
			foreach (var component in ScreenSafeAreas)
			{
				component.topOffset = offset;
				component.StartCoroutine(component.IEValidate());
			}
			OnOffsetChanged?.Invoke();
		}

		public static float BottomOffset;
		public static void SetBottomOffsetForBannerAd(float pBannerHeight, bool pPlaceInSafeArea = true)
		{
			float offset = 0;
			var safeAreaHeightOffer = Screen.height - Screen.safeArea.height;
			if (!pPlaceInSafeArea)
			{
				if (pBannerHeight <= safeAreaHeightOffer)
					offset = 0;
				else
					offset = pBannerHeight - safeAreaHeightOffer;
			}
			else
			{
				offset = pBannerHeight;
			}
			
			BottomOffset = offset;
			var screenSafeAreas = FindObjectsOfType<ScreenSafeArea>();
			foreach (var component in screenSafeAreas)
			{
				component.bottomOffset = offset;
				component.StartCoroutine(component.IEValidate());
			}
			OnOffsetChanged?.Invoke();
		}

		private IEnumerator IEValidate()
		{
			Validate();
			yield return null;
			Validate();
		}

#if ODIN_INSPECTOR
		[Button]
#else
		[InspectorButton]
#endif
		private void TestTopOffsetForBannerAd(int height) => SetTopOffsetForBannerAd(height);

#if ODIN_INSPECTOR
		[Button]
#else
		[InspectorButton]
#endif
		private void TestBottomOffsetForBannerAd(int height) => SetBottomOffsetForBannerAd(height);
	}
}