using System;
using System.Collections;
using UnityEngine;

namespace RCore.UI
{
	public class ScreenSafeArea : MonoBehaviour
	{
		public static float topOffset;
		public Canvas canvas;
		public RectTransform[] safeRects;
		public bool fixedTop;
		public bool fixedBottom;
		private ScreenOrientation m_CurrentOrientation;
		private Rect m_CurrentSafeArea;

		private void Start()
		{
			m_CurrentOrientation = Screen.orientation;
			m_CurrentSafeArea = Screen.safeArea;
			CheckSafeArea();
		}

		[InspectorButton]
		public void Log()
		{
			var safeArea = Screen.safeArea;
			var sWidth = Screen.currentResolution.width;
			var sHeight = Screen.currentResolution.height;
			var oWidthTop = (Screen.currentResolution.width - safeArea.width - safeArea.x) / 2f;
			var oHeightTop = (Screen.currentResolution.height - safeArea.height - safeArea.y) / 2f;
			var oWidthBot = -safeArea.x / 2f;
			var oHeightBot = -safeArea.y / 2f;
			Debug.Log($"Screen size: (width:{sWidth}, height:{sHeight})" +
				$"\nSafe area: {safeArea}" +
				$"\nOffset Top: (width:{oWidthTop}, height:{oHeightTop})" +
				$"\nOffset Bottom: (width:{oWidthBot}, height:{oHeightBot})");
		}

		private void OnValidate()
		{
			if (Application.isPlaying)
				CheckSafeArea();
		}

		[InspectorButton]
		public void CheckSafeArea()
		{
			var safeArea = Screen.safeArea;
			safeArea.height -= topOffset;
			var anchorMin = safeArea.position;
			var anchorMax = safeArea.position + safeArea.size;

			anchorMin.x /= canvas.pixelRect.width;
			anchorMin.y /= canvas.pixelRect.height;
			anchorMax.x /= canvas.pixelRect.width;
			anchorMax.y /= canvas.pixelRect.height;

			foreach (var rect in safeRects)
			{
				if (!fixedBottom)
					rect.anchorMin = anchorMin;
				else
					rect.anchorMin = Vector2.zero;
				if (!fixedTop)
					rect.anchorMax = anchorMax;
				else
					rect.anchorMax = Vector2.one;
			}

			m_CurrentOrientation = Screen.orientation;
			m_CurrentSafeArea = Screen.safeArea;
		}

		public static void SetTopOffsetForBannerAd(float pBannerHeight, bool pPlaceInSafeArea = true) //150
		{
			float offset = 0;
			var safeAreaHeightOffer = Screen.height - Screen.safeArea.height; //80
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

			if (topOffset == offset)
				return;

			topOffset = offset;

			var ScreenSafeAreas = FindObjectsOfType<ScreenSafeArea>();
			foreach (var component in ScreenSafeAreas)
				component.StartCoroutine(component.IECheckSafeArea());
		}

		private IEnumerator IECheckSafeArea()
		{
			float time = 0.5f;
			while (time > 0)
			{
				CheckSafeArea();
				time -= Time.deltaTime;
				yield return null;
			}
		}

		[InspectorButton]
		private void TestTopOffsetForBannerAd()
		{
			SetTopOffsetForBannerAd(143);
		}
	}
}