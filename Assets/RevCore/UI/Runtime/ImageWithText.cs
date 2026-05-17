using UnityEngine;
using UnityEngine.UI;

namespace RevCore.UI
{
	/// <summary>
	/// Component pairing an <see cref="Image"/> with a legacy <see cref="Text"/> label. Sprite assignment
	/// can optionally resize the image to fit <c>m_fixedSize</c>. See <see cref="ImageWithTextTMP"/>
	/// for the TextMeshPro variant.
	/// </summary>
	[AddComponentMenu("RevCore/UI/ImageWithText")]
	public class ImageWithText : MonoBehaviour
	{
		[SerializeField] protected Image m_Img;
		[SerializeField] protected Text m_Txt;

		[Separator("Custom")]
		[SerializeField] protected bool m_autoResize;
		[SerializeField] protected Vector2 m_fixedSize;

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
		public Text label
		{
			get
			{
				if (m_Txt == null)
					m_Txt = GetComponentInChildren<Text>();
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

		/// <summary>The label text.</summary>
		public string text
		{
			get => label.text;
			set => label.text = value;
		}

		/// <summary>Assigns a sprite and (when <c>m_autoResize</c>) resizes the image per <c>m_fixedSize</c>.</summary>
		public void SetSprite(Sprite sprite)
		{
			m_Img.sprite = sprite;

			if (!m_autoResize || sprite == null)
				return;

			if (m_fixedSize.x > 0 && m_fixedSize.y > 0)
				m_Img.SetNativeSize(m_fixedSize);
			else if (m_fixedSize.x > 0)
				m_Img.SketchByWidth(m_fixedSize.x);
			else if (m_fixedSize.y > 0)
				m_Img.SketchByHeight(m_fixedSize.y);

			m_Img.PerfectRatio();
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
				m_Txt = GetComponentInChildren<Text>();
			if (m_Img != null)
				m_Img.PerfectRatio();
		}
#endif
	}
}
