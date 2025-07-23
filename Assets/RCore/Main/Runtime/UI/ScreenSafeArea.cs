#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using RCore.Inspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCore.UI
{
	public class ScreenSafeArea : MonoBehaviour
	{
		public static Action OnOffsetChanged;
		public Canvas canvas;
		public RectTransform[] safeRects;
		[FormerlySerializedAs("fixedTop")]
		public bool fullTop;
		[FormerlySerializedAs("fixedBottom")]
		public bool fullBottom;

		private void Start()
		{
			screenSafeAreas.Add(this);
		}

		private void OnEnable()
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
			safeArea.height -= topBannerOffset;
			if (fullTop)
			{
				safeArea.height = Screen.currentResolution.height - Screen.safeArea.y;
			}
			if (fullBottom)
			{
				safeArea.height += Screen.safeArea.y;
				safeArea.y = 0;
			}
			var anchorMin = safeArea.position;
			var anchorMax = safeArea.position + safeArea.size;

			var sizeDelta = ((RectTransform)canvas.transform).sizeDelta;
			sizeDelta.y = -bottomBannerOffset;
			((RectTransform)canvas.transform).sizeDelta = sizeDelta;
			var position = ((RectTransform)canvas.transform).anchoredPosition;
			position.y = bottomBannerOffset / 2;
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

		//========================================================================================

		public static float topBannerOffset;
		public static float bottomBannerOffset;
		public static List<ScreenSafeArea> screenSafeAreas = new();

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
				offset = pBannerHeight;

			topBannerOffset = offset;
			foreach (var component in screenSafeAreas)
				if (component != null && component.gameObject.activeSelf)
					component.StartCoroutine(component.IEValidate());
			OnOffsetChanged?.Invoke();
		}
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
				offset = pBannerHeight;

			bottomBannerOffset = offset;
			foreach (var component in screenSafeAreas)
				if (component != null && component.gameObject.activeSelf)
					component.StartCoroutine(component.IEValidate());
			OnOffsetChanged?.Invoke();
		}
	}
}