#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using RCore.Inspector;
using System;
using UnityEngine;

namespace RCore.UI
{
	public class IgnoreScreenSafe : MonoBehaviour
	{
		private Vector2 m_original;
		private Vector2 m_sizeDelta;

		private void Start()
		{
			ScreenSafeArea.OnOffsetChanged += OnOffsetChanged;

			var rectTransform = transform as RectTransform;
			m_original = rectTransform.anchoredPosition;
			m_sizeDelta = rectTransform.sizeDelta;
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

#if ODIN_INSPECTOR
		[Button]
#else
		[InspectorButton]
#endif
		private void Validate()
		{
			var offsetHeight = Screen.currentResolution.height - Screen.safeArea.height;
			if (offsetHeight > 0)
			{
				var rectTransform = (transform as RectTransform);
				rectTransform.anchoredPosition = new Vector2(m_original.x, m_original.y + offsetHeight / 2f);
				rectTransform.sizeDelta = new Vector2(m_sizeDelta.x, m_sizeDelta.y + offsetHeight);
			}
		}
	}
}