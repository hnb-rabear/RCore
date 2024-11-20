/***
 * Author HNB-RaBear - 2019
 **/

#pragma warning disable 0649

using UnityEngine;
using UnityEngine.UI;
using RCore.Inspector;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using RCore.Editor;
using UnityEditor;
#endif

namespace RCore.Components
{
	[AddComponentMenu("RCore/UI/ImageWithText")]
	public class ImageWithText : MonoBehaviour
	{
		[SerializeField] protected Image m_Img;
		[SerializeField] protected Text m_Txt;

		[Separator("Custom")]
		[SerializeField] protected bool m_autoResize;
		[SerializeField] protected Vector2 m_fixedSize;

		public Image image
		{
			get
			{
				if (m_Img == null)
					m_Img = GetComponentInChildren<Image>();
				return m_Img;
			}
		}
		public Text label
		{
			get
			{
				if (m_Txt == null)
					m_Txt = GetComponentInChildren<Text>();
				return m_Txt;
			}
		}
		public RectTransform rectTransform => transform as RectTransform;
		public Sprite sprite
		{
			get => image.sprite;
			set
			{
				if (m_Img.sprite != value)
					SetSprite(value);
			}
		}
		public string text
		{
			get => label.text;
			set => label.text = value;
		}

		public void SetSprite(Sprite pSprite)
		{
			m_Img.sprite = pSprite;

			if (m_autoResize)
			{
				if (pSprite == null)
					return;

				if (m_fixedSize.x > 0 && m_fixedSize.y > 0)
				{
					m_Img.SetNativeSize(m_fixedSize);
				}
				else if (m_fixedSize.x > 0)
				{
					m_Img.SketchByWidth(m_fixedSize.x);
				}
				else if (m_fixedSize.y > 0)
				{
					m_Img.SketchByWidth(m_fixedSize.y);
				}
				m_Img.PerfectRatio();
			}
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