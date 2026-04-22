/**
 * Author HNB-RaBear - 2021
 **/

using RCore.Inspector;
using UnityEngine;
#if UNITY_EDITOR
using Screen = UnityEngine.Device.Screen;
#endif

namespace RCore.UI
{
	public class IgnoreScreenSafe : MonoBehaviour
	{
		private Vector2 m_originalOffsetMin;
		private Vector2 m_originalOffsetMax;
		private RectTransform m_rectTransform;
		private Canvas m_canvas;
		private ScreenSafeArea m_screenSafeArea;

		private void Start()
		{
			m_rectTransform = transform as RectTransform;
			m_originalOffsetMin = m_rectTransform.offsetMin;
			m_originalOffsetMax = m_rectTransform.offsetMax;
			m_canvas = GetComponentInParent<Canvas>();
			m_screenSafeArea = GetComponentInParent<ScreenSafeArea>();

			ScreenSafeArea.OnOffsetChanged += OnOffsetChanged;
			Validate();
		}

		private void OnDestroy()
		{
			ScreenSafeArea.OnOffsetChanged -= OnOffsetChanged;
		}

		private void OnOffsetChanged()
		{
			Validate();
		}

		[ContextMenu("Validate")]
		[InspectorButton]
		private void Validate()
		{
			if (m_rectTransform == null) m_rectTransform = transform as RectTransform;
			if (m_screenSafeArea == null) m_screenSafeArea = GetComponentInParent<ScreenSafeArea>();
			if (m_screenSafeArea == null)
			{
				Debug.LogError($"{gameObject.name}: IgnoreScreenSafe requires a ScreenSafeArea component in a parent GameObject.");
				return;
			}
			if (m_canvas == null) m_canvas = GetComponentInParent<Canvas>();
			if (m_canvas == null) return;

			var safeArea = Screen.safeArea;

			// In Unity Editor with Device Simulator, Screen.safeArea uses simulated device
			// resolution while Screen.height uses Game view resolution. Skip adjustment.
			if (safeArea.x + safeArea.width > Screen.width * 1.01f
				|| safeArea.y + safeArea.height > Screen.height * 1.01f)
			{
				m_rectTransform.offsetMin = m_originalOffsetMin;
				m_rectTransform.offsetMax = m_originalOffsetMax;
				return;
			}

			// Calculate safe area offsets in pixels
			// If the parent ScreenSafeArea ignores an edge (fullTop/fullBottom),
			// it didn't apply that inset, so we must not counteract it.
			float topUnsafePixels = m_screenSafeArea.fullTop ? 0f : Screen.height - safeArea.yMax;
			float bottomUnsafePixels = m_screenSafeArea.fullBottom ? 0f : safeArea.y;

			// Convert pixels to local space units (Canvas Scaler units)
			// Assuming Screen Space - Overlay or Camera where scaleFactor applies uniformly
			float scaleFactor = m_canvas.scaleFactor;

			// Avoid division by zero
			if (scaleFactor == 0) scaleFactor = 1f;

			float topUnsafeLocal = topUnsafePixels / scaleFactor;
			float bottomUnsafeLocal = bottomUnsafePixels / scaleFactor;

			// Apply offsets to Top and Bottom
			// offsetMax.y corresponds to -Top in Inspector (so +TopUnsafe expands Up)
			// offsetMin.y corresponds to Bottom in Inspector (so -BottomUnsafe expands Down)
			m_rectTransform.offsetMax = new Vector2(m_originalOffsetMax.x, m_originalOffsetMax.y + topUnsafeLocal);
			m_rectTransform.offsetMin = new Vector2(m_originalOffsetMin.x, m_originalOffsetMin.y - bottomUnsafeLocal);
		}
	}
}