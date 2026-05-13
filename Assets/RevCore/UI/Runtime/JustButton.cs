#if DOTWEEN
using DG.Tweening;
#endif
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RevCore.UI
{
    [AddComponentMenu("RevCore/UI/JustButton")]
    public class JustButton : Button
    {
        private static Material s_greyMat;

        [SerializeField] protected Image m_img;

        public bool scaleBounceEffect = true;
        public PerfectRatio perfectRatio = PerfectRatio.Height;
        public bool greyscaleEffect;
        public bool imgOnOffSwap;
        public Sprite imgOn;
        public Sprite imgOff;
        public string clickSfx = "button";

        private Vector3 m_initialScale;
        private bool m_active = true;
        private bool m_findImg;
        private bool m_initialed;
        private int m_perfectSpriteId;
        private Action m_inactionStateAction;

        public Image Img
        {
            get
            {
                if (m_img == null && !m_findImg)
                {
                    m_img = GetComponent<Image>();
                    m_findImg = true;
                }
                return m_img;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            Init();
        }

#if UNITY_EDITOR
        [ContextMenu("Validate")]
        protected override void OnValidate()
        {
            if (Application.isPlaying)
                return;

            base.OnValidate();

            if (targetGraphic == null)
            {
                var images = gameObject.GetComponentsInChildren<Image>();
                if (images.Length > 0)
                {
                    targetGraphic = images[0];
                    m_img = (Image)targetGraphic;
                }
            }
            if (targetGraphic != null && m_img == null)
                m_img = targetGraphic as Image;

            if (scaleBounceEffect)
            {
                if (transition == Transition.Animation)
                    transition = Transition.None;

                if (gameObject.TryGetComponent(out Animator component))
                    component.enabled = false;
            }
            else if (transition == Transition.Animation)
            {
                var animator = gameObject.GetOrAddComponent<Animator>();
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                animator.enabled = true;
            }

            m_perfectSpriteId = 0;
            CheckPerfectRatio();
        }
#endif

        protected override void Start()
        {
            base.Start();
            if (!m_initialed)
                Init();
        }

        public void Init()
        {
            if (m_initialed)
                return;

            if (s_greyMat == null)
                s_greyMat = Resources.Load<Material>("Greyscale");
            m_initialScale = transform.localScale;
            SetEnable(interactable);
            m_initialed = true;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (scaleBounceEffect)
                transform.localScale = m_initialScale;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (scaleBounceEffect)
                transform.localScale = m_initialScale;

            if (m_img != null && m_img.sprite != null && m_perfectSpriteId != m_img.sprite.GetInstanceID())
                CheckPerfectRatio();
        }

        public virtual void SetInactiveStateAction(Action action)
        {
            m_inactionStateAction = action;
            enabled = m_active || m_inactionStateAction != null;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (m_active)
            {
                base.OnPointerDown(eventData);

                if (!string.IsNullOrEmpty(clickSfx))
                    Events.Publish(new UISfxTriggeredEvent(clickSfx));
            }
            else if (m_inactionStateAction != null)
            {
                m_inactionStateAction();
                if (TryGetComponent(out Animator component) && component.enabled)
                    component.SetTrigger("Pressed");
            }

            if (scaleBounceEffect)
            {
#if DOTWEEN
                DOTween.Kill(GetInstanceID());
                transform
                    .DOScale(m_initialScale * 0.9f, 0.125f)
                    .SetUpdate(true)
                    .SetEase(Ease.InOutBack)
                    .SetId(GetInstanceID());
#else
                transform.localScale = m_initialScale * 0.95f;
#endif
            }
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            if (m_active)
                base.OnPointerUp(eventData);

            if (scaleBounceEffect)
            {
#if DOTWEEN
                DOTween.Kill(GetInstanceID());
                transform
                    .DOScale(m_initialScale, 0.1f)
                    .SetUpdate(true)
                    .SetId(GetInstanceID());
#else
                transform.localScale = m_initialScale;
#endif
            }
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (m_active)
                base.OnPointerClick(eventData);
        }

        public override void OnSelect(BaseEventData eventData)
        {
            if (m_active)
                base.OnSelect(eventData);
        }

        public Material GetGreyMat()
        {
            if (s_greyMat == null)
                s_greyMat = Resources.Load<Material>("Greyscale");
            return s_greyMat;
        }

        public void EnableGrey(bool value)
        {
            greyscaleEffect = value;
        }

        public bool Enabled()
        {
            return enabled && m_active;
        }

        public void SetActiveSprite(Sprite sprite)
        {
            imgOn = sprite;
        }

        public void SetActive(bool value)
        {
            m_active = value;
        }

        public virtual void SetEnable(bool value)
        {
            if (Img != null)
            {
                if (s_greyMat != null)
                    Img.material = value || !greyscaleEffect ? null : s_greyMat;

                if (imgOnOffSwap)
                    Img.sprite = value ? imgOn : imgOff;
            }

            interactable = value;
        }

        public void CheckPerfectRatio()
        {
            if (m_img == null || m_img.sprite == null || m_img.type != Image.Type.Sliced
                || m_perfectSpriteId == m_img.sprite.GetInstanceID())
                return;

            if (perfectRatio == PerfectRatio.Width)
            {
                var nativeSize = m_img.sprite.NativeSize();
                var rectSize = rectTransform.sizeDelta;
                m_img.pixelsPerUnitMultiplier = rectSize.x > 0 && rectSize.x < nativeSize.x
                    ? nativeSize.x / rectSize.x
                    : 1;
                m_perfectSpriteId = m_img.sprite.GetInstanceID();
            }
            else if (perfectRatio == PerfectRatio.Height)
            {
                var nativeSize = m_img.sprite.NativeSize();
                var rectSize = rectTransform.sizeDelta;
                m_img.pixelsPerUnitMultiplier = rectSize.y > 0 && rectSize.y < nativeSize.y
                    ? nativeSize.y / rectSize.y
                    : 1;
                m_perfectSpriteId = m_img.sprite.GetInstanceID();
            }
        }

        public void PlayBubbleEffect(float duration)
        {
#if DOTWEEN
            float scaleDuration = 0.6f;
            int loopCount = Mathf.Max(2, Mathf.RoundToInt(duration / scaleDuration));
            if (loopCount % 2 != 0)
                loopCount++;

            DOTween.Kill(GetInstanceID());
            var seq = DOTween.Sequence();
            seq.Append(transform.DOScale(0.9f, scaleDuration * 0.4f));
            seq.Append(transform.DOScale(1.1f, scaleDuration * 0.6f)).SetEase(Ease.OutSine);
            seq.SetLoops(loopCount, LoopType.Yoyo);
            seq.SetId(GetInstanceID());
            seq.OnComplete(() => transform.localScale = m_initialScale);
#else
            Debug.LogError("PlayBubbleEffect requires DOTween");
#endif
        }
    }
}
