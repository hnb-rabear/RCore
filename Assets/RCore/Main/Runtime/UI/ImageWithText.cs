/***
 * Author RadBear - nbhung71711 @gmail.com - 2019
 **/

#pragma warning disable 0649

using UnityEngine;
using UnityEngine.UI;
using RCore.Inspector;
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
		[SerializeField] protected bool mAutoReize;
		[SerializeField] protected Vector2 mFixedSize;

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

			if (mAutoReize)
			{
				if (pSprite == null)
					return;

				if (mFixedSize.x > 0 && mFixedSize.y > 0)
				{
					m_Img.SetNativeSize(mFixedSize);
				}
				else if (mFixedSize.x > 0)
				{
					m_Img.SketchByWidth(mFixedSize.x);
				}
				else if (mFixedSize.y > 0)
				{
					m_Img.SketchByWidth(mFixedSize.y);
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

#if UNITY_EDITOR
	[CustomEditor(typeof(ImageWithText), true)]
	public class ImageWithTextEditor : UnityEditor.Editor
	{
		private ImageWithText m_Script;

		private void OnEnable()
		{
			m_Script = (ImageWithText)target;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (EditorHelper.Button("Auto Reize"))
				m_Script.SetSprite(m_Script.image.sprite);
		}
	}
#endif
}