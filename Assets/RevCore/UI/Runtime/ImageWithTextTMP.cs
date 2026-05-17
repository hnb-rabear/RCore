using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RevCore.UI
{
	/// <summary>TextMeshPro variant of <see cref="ImageWithText"/> — image plus a <see cref="TextMeshProUGUI"/> label with optional auto-resize on sprite change.</summary>
	[AddComponentMenu("RevCore/UI/ImageWithTextTMP")]
	public class ImageWithTextTMP : MonoBehaviour
	{
		[SerializeField] protected Image m_Img;
		[SerializeField] protected TextMeshProUGUI m_Txt;

		[Separator("Custom")]
		[SerializeField] protected bool m_AutoResize;
		[SerializeField] protected Vector2 m_FixedSize;

		/// <summary>The image component (resolved lazily from children if not assigned).</summary>
		public Image image
		{
			get
			{
				if (m_Img == null)
					m_Img = GetComponentInChildren<Image>();
				return m_Img;
			}
		}

		/// <summary>The label (resolved lazily from children if not assigned).</summary>
		public TextMeshProUGUI label
		{
			get
			{
				if (m_Txt == null)
					m_Txt = GetComponentInChildren<TextMeshProUGUI>();
				return m_Txt;
			}
		}

		/// <summary>This component's rect transform.</summary>
		public RectTransform rectTransform => transform as RectTransform;

		/// <summary>The active sprite. Setting it applies <see cref="SetSprite"/>.</summary>
		public Sprite sprite
		{
			get => image.sprite;
			set
			{
				if (m_Img.sprite != value)
					SetSprite(value);
			}
		}

		/// <summary>Assigns a sprite and (when <c>m_AutoResize</c>) resizes the image per <c>m_FixedSize</c>.</summary>
		public void SetSprite(Sprite sprite)
		{
			image.sprite = sprite;

			if (!m_AutoResize || sprite == null)
				return;

			if (m_FixedSize.x > 0 && m_FixedSize.y > 0)
				image.SetNativeSize(m_FixedSize);
			else if (m_FixedSize.x > 0)
				image.SketchByWidth(m_FixedSize.x);
			else if (m_FixedSize.y > 0)
				image.SketchByHeight(m_FixedSize.y);
		}

#if UNITY_EDITOR
		[ContextMenu("Validate")]
		private void OnValidate()
		{
			Validate();
		}

		protected virtual void Validate()
		{
			if (m_Img == null)
				m_Img = GetComponentInChildren<Image>();
			if (m_Txt == null)
				m_Txt = GetComponentInChildren<TextMeshProUGUI>();
		}
#endif
	}
}
