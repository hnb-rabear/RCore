/**
 * Author HNB-RaBear - 2021
 **/

using RCore.Inspector;
using UnityEngine;

namespace RCore.UI
{
	public class IgnoreScreenSafe : MonoBehaviour
	{
		private Vector2 m_originalOffsetMin;
		private Vector2 m_originalOffsetMax;
		private RectTransform m_rectTransform;
		private Canvas m_canvas;

		private void Start()
		{
			m_rectTransform = transform as RectTransform;
			m_originalOffsetMin = m_rectTransform.offsetMin;
			m_originalOffsetMax = m_rectTransform.offsetMax;
			m_canvas = GetComponentInParent<Canvas>();

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
			if (GetComponentInParent<ScreenSafeArea>() == null)
			{
				Debug.LogError($"{gameObject.name}: IgnoreScreenSafe requires a ScreenSafeArea component in a parent GameObject.");
				return;
			}
			if (m_canvas == null) m_canvas = GetComponentInParent<Canvas>();
			if (m_canvas == null) return;

			var safeArea = Screen.safeArea;

			// Calculate safe area offsets in pixels
			float topUnsafePixels = Screen.height - safeArea.yMax;
			float bottomUnsafePixels = safeArea.y;

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