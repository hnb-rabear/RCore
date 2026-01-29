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
	/// <summary>
	/// Adjusts the size and position of a container to fit within the screen's safe area.
	/// Useful for phones with notches or rounded corners.
	/// </summary>
	public class ScreenSafeArea : MonoBehaviour
	{
		public static Action OnOffsetChanged;
		private Canvas m_canvas;
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
		/// <summary>
		/// Logs debug information about screen resolution and safe area offsets.
		/// </summary>
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

		/// <summary>
		/// Validates and applies the safe area adjustments.
		/// </summary>
		private void CheckSafeArea()
		{
			if (m_canvas == null)
				m_canvas = GetComponentInParent<Canvas>();

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

			if (m_canvas == null)
				return;

			var sizeDelta = ((RectTransform)m_canvas.transform).sizeDelta;
			sizeDelta.y = -bottomBannerOffset;
			((RectTransform)m_canvas.transform).sizeDelta = sizeDelta;
			var position = ((RectTransform)m_canvas.transform).anchoredPosition;
			position.y = bottomBannerOffset / 2;
			((RectTransform)m_canvas.transform).anchoredPosition = position;

			var pixelRect = m_canvas.pixelRect;
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

		/// <summary>
		/// Sets the top offset for a banner advertisement and triggers a safe area update.
		/// </summary>
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
		/// <summary>
		/// Sets the bottom offset for a banner advertisement and triggers a safe area update.
		/// </summary>
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