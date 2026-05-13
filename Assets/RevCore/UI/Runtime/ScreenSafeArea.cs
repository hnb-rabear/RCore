using RevCore.Inspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using Screen = UnityEngine.Device.Screen;
#endif

namespace RevCore.UI
{
	public class ScreenSafeArea : MonoBehaviour
	{
		public static Action OnOffsetChanged;

		public RectTransform[] safeRects;
		public bool fullTop;
		public bool fullBottom;

		private Canvas m_canvas;

		private void Start()
		{
			screenSafeAreas.Add(this);
		}

		private void OnEnable()
		{
			CheckSafeArea();
		}

		[InspectorButton]
		public void Log()
		{
			var safeArea = Screen.safeArea;
			var sWidth = Screen.width;
			var sHeight = Screen.height;
			var oWidthTop = (Screen.width - safeArea.width - safeArea.x) / 2f;
			var oHeightTop = (Screen.height - safeArea.height - safeArea.y) / 2f;
			var oWidthBot = -safeArea.x / 2f;
			var oHeightBot = -safeArea.y / 2f;
			Debug.Log($"Screen size: (width:{sWidth}, height:{sHeight})"
				+ $"\nSafe area: {safeArea}"
				+ $"\nOffset Top: (width:{oWidthTop}, height:{oHeightTop})"
				+ $"\nOffset Bottom: (width:{oWidthBot}, height:{oHeightBot})");
		}

		[InspectorButton]
		private void Validate()
		{
			CheckSafeArea();
		}

		private void CheckSafeArea()
		{
			if (m_canvas == null)
				m_canvas = GetComponentInParent<Canvas>();

			var safeArea = Screen.safeArea;

			bool coordMismatch = safeArea.x + safeArea.width > Screen.width * 1.01f
				|| safeArea.y + safeArea.height > Screen.height * 1.01f;
			if (coordMismatch)
				safeArea = new Rect(0, 0, Screen.width, Screen.height);

			safeArea.height -= topBannerOffset;
			if (fullTop)
				safeArea.height = Screen.height - safeArea.y;
			if (fullBottom)
			{
				safeArea.height += safeArea.y;
				safeArea.y = 0;
			}

			var anchorMin = safeArea.position;
			var anchorMax = safeArea.position + safeArea.size;

			if (m_canvas == null)
				return;

			var sizeDelta = ((RectTransform)m_canvas.transform).sizeDelta;
			sizeDelta.y = -bottomBannerOffset;
			((RectTransform)m_canvas.transform).sizeDelta = sizeDelta;
			var position = ((RectTransform)m_canvas.transform).anchoredPosition;
			position.y = bottomBannerOffset / 2;
			((RectTransform)m_canvas.transform).anchoredPosition = position;

			anchorMin.x /= Screen.width;
			anchorMin.y /= Screen.height;
			anchorMax.x /= Screen.width;
			anchorMax.y /= Screen.height;

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

		[InspectorButton]
		private void TestTopOffsetForBannerAd(int height) => SetTopOffsetForBannerAd(height);

		[InspectorButton]
		private void TestBottomOffsetForBannerAd(int height) => SetBottomOffsetForBannerAd(height);

		public static float topBannerOffset;
		public static float bottomBannerOffset;
		public static List<ScreenSafeArea> screenSafeAreas = new();

		public static void SetTopOffsetForBannerAd(float bannerHeight, bool placeInSafeArea = true)
		{
			float offset = 0;
			var safeAreaHeightOffset = Screen.height - Screen.safeArea.height;
			if (!placeInSafeArea)
				offset = bannerHeight <= safeAreaHeightOffset ? 0 : bannerHeight - safeAreaHeightOffset;
			else
				offset = bannerHeight;

			topBannerOffset = offset;
			foreach (var component in screenSafeAreas)
				if (component != null && component.gameObject.activeSelf)
					component.StartCoroutine(component.IEValidate());
			OnOffsetChanged?.Invoke();
		}

		public static void SetBottomOffsetForBannerAd(float bannerHeight, bool placeInSafeArea = true)
		{
			float offset = 0;
			var safeAreaHeightOffset = Screen.height - Screen.safeArea.height;
			if (!placeInSafeArea)
				offset = bannerHeight <= safeAreaHeightOffset ? 0 : bannerHeight - safeAreaHeightOffset;
			else
				offset = bannerHeight;

			bottomBannerOffset = offset;
			foreach (var component in screenSafeAreas)
				if (component != null && component.gameObject.activeSelf)
					component.StartCoroutine(component.IEValidate());
			OnOffsetChanged?.Invoke();
		}
	}
}
