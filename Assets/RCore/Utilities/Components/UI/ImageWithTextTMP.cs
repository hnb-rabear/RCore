/**
 * Author RadBear - nbhung71711 @gmail.com - 2019
 **/

#pragma warning disable 0649

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RCore.Common;
using RCore.Inspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Components
{
    [AddComponentMenu("Utitlies/UI/ImageWithTextTMP")]
    public class ImageWithTextTMP : MonoBehaviour
    {
        [SerializeField] protected Image mImg;
        [SerializeField] protected TextMeshProUGUI mTxt;

        [Separator("Custom")]
        [SerializeField] protected bool mAutoReize;
        [SerializeField] protected Vector2 mFixedSize;

        public Image image
        {
            get
            {
                if (mImg == null)
                    mImg = GetComponentInChildren<Image>();
                return mImg;
            }
        }
        public TextMeshProUGUI label
        {
            get
            {
                if (mTxt == null)
                    mTxt = GetComponentInChildren<TextMeshProUGUI>();
                return mTxt;
            }
        }
        public RectTransform rectTransform => transform as RectTransform;
        public Sprite sprite
        {
            get { return image.sprite; }
            set
            {
                if (mImg.sprite != value)
                    SetSprite(value);
            }
        }
        public string text
        {
            get { return label.text; }
            set { label.text = value; }
        }

        public void SetSprite(Sprite pSprite)
        {
            image.sprite = pSprite;

            if (mAutoReize)
            {
                if (pSprite == null)
                    return;

                if (mFixedSize.x > 0 && mFixedSize.y > 0)
                {
                    image.SetNativeSize(mFixedSize);
                }
                else if (mFixedSize.x > 0)
                {
                    image.SketchByWidth(mFixedSize.x);
                }
                else if (mFixedSize.y > 0)
                {
                    image.SketchByWidth(mFixedSize.y);
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
            if (mImg == null)
                mImg = GetComponentInChildren<Image>();
            if (mTxt == null)
                mTxt = GetComponentInChildren<TextMeshProUGUI>();
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ImageWithTextTMP), true)]
    public class ImageWithTextTMPEditor : Editor
    {
        private ImageWithTextTMP m_Script;

        private void OnEnable()
        {
            m_Script = (ImageWithTextTMP)target;
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
