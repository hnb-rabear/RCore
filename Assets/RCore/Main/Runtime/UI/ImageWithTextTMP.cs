/***
 * Author HNB-RaBear - 2019
 **/

#pragma warning disable 0649

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RCore.Inspector;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using RCore.Editor;
using UnityEditor;
#endif

namespace RCore.UI
{
    [AddComponentMenu("RCore/UI/ImageWithTextTMP")]
    public class ImageWithTextTMP : MonoBehaviour
    {
        [SerializeField] protected Image m_Img;
        [SerializeField] protected TextMeshProUGUI m_Txt;

		[Separator("Custom")]
		[SerializeField] protected bool m_AutoResize;
        [SerializeField] protected Vector2 m_FixedSize;

        public Image image
        {
            get
            {
                if (m_Img == null)
                    m_Img = GetComponentInChildren<Image>();
                return m_Img;
            }
        }
        public TextMeshProUGUI label
        {
            get
            {
                if (m_Txt == null)
                    m_Txt = GetComponentInChildren<TextMeshProUGUI>();
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
            image.sprite = pSprite;

            if (m_AutoResize)
            {
                if (pSprite == null)
                    return;

                if (m_FixedSize.x > 0 && m_FixedSize.y > 0)
                {
                    image.SetNativeSize(m_FixedSize);
                }
                else if (m_FixedSize.x > 0)
                {
                    image.SketchByWidth(m_FixedSize.x);
                }
                else if (m_FixedSize.y > 0)
                {
                    image.SketchByWidth(m_FixedSize.y);
                }
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
                m_Txt = GetComponentInChildren<TextMeshProUGUI>();
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ImageWithTextTMP), true)]
    public class ImageWithTextTMPEditor : UnityEditor.Editor
    {
        private ImageWithTextTMP m_Script;

        private void OnEnable()
        {
            m_Script = (ImageWithTextTMP)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (EditorHelper.Button("Auto Resize"))
                m_Script.SetSprite(m_Script.image.sprite);
        }
    }
#endif
}