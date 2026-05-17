#if DOTWEEN
using DG.Tweening;
#endif
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RevCore.UI
{
    /// <summary>
    /// Extended Unity <see cref="Button"/> with three opt-in behaviors common in mobile games: scale-bounce
    /// on press, on/off sprite swap for interactable state, and a greyscale material for the disabled look.
    /// Plays a named SFX on click via the global <see cref="UISfxTriggeredEvent"/>.
    /// </summary>
    [AddComponentMenu("RevCore/UI/JustButton")]
    public class JustButton : Button
    {
        [SerializeField] protected Image m_img;
        [SerializeField] protected Material m_greyscaleMaterial;

        /// <summary>Scale down on press, bounce back on release.</summary>
        public bool scaleBounceEffect = true;
        /// <summary>Aspect-ratio constraint applied on enable. See <see cref="RevCore.PerfectRatio"/>.</summary>
        public PerfectRatio perfectRatio = PerfectRatio.Height;
        /// <summary>Render with a greyscale material when interactable is false.</summary>
        public bool greyscaleEffect;
        /// <summary>Swap between <see cref="imgOn"/> and <see cref="imgOff"/> sprites based on interactable state.</summary>
        public bool imgOnOffSwap;
        /// <summary>Sprite shown when interactable.</summary>
        public Sprite imgOn;
        /// <summary>Sprite shown when not interactable.</summary>
        public Sprite imgOff;
        /// <summary>Name of the SFX clip to play on click. Empty string silences clicks.</summary>
        public string clickSfx = "button";

        private Vector3 m_initialScale;
        private bool m_active = true;
        private bool m_findImg;
        private bool m_initialed;
        private int m_perfectSpriteId;
        private Action m_inactionStateAction;

        /// <summary>RectTransform of the target graphic, falling back to this transform. Used for scale/bounce calculations.</summary>
        public RectTransform RectTransform => targetGraphic != null ? targetGraphic.rectTransform : transform as RectTransform;

        /// <summary>Cached <see cref="UnityEngine.UI.Image"/> on this GameObject. Resolved lazily on first access.</summary>
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

        /// <summary>Caches the initial scale and applies <see cref="CheckPerfectRatio"/> + initial sprite/greyscale state. Safe to call multiple times.</summary>
        public void Init()
        {
            if (m_initialed)
                return;

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

        /// <summary>Sets a callback to invoke when a press lands while the button is not interactable. Useful for "buy more energy" prompts on locked actions.</summary>
        public virtual void SetInactiveStateAction(Action action)
        {
            m_inactionStateAction = action;
            enabled = m_active || m_inactionStateAction != null;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override void OnPointerClick(PointerEventData eventData)
        {
            if (m_active)
                base.OnPointerClick(eventData);
        }

        /// <inheritdoc />
        public override void OnSelect(BaseEventData eventData)
        {
            if (m_active)
                base.OnSelect(eventData);
        }

        /// <summary>Returns the greyscale material (loaded from Resources on first call).</summary>
        public Material GetGreyMat()
        {
            return m_greyscaleMaterial;
        }

        /// <summary>Swaps the image's material between the original and the greyscale variant.</summary>
        public void EnableGrey(bool value)
        {
            greyscaleEffect = value;
        }

        /// <summary>True when the button is currently accepting input (interactable + active in hierarchy + not in the inactive state).</summary>
        public bool Enabled()
        {
            return enabled && m_active;
        }

        /// <summary>Sets the currently displayed sprite, refreshing the perfect-ratio constraint.</summary>
        public void SetActiveSprite(Sprite sprite)
        {
            imgOn = sprite;
        }

        /// <summary>Marks the button "active" (responds to clicks normally) vs inactive (routes to <see cref="SetInactiveStateAction"/>).</summary>
        public void SetActive(bool value)
        {
            m_active = value;
        }

        /// <summary>Sets <c>interactable</c> and applies the on/off sprite swap and greyscale effect as configured.</summary>
        public virtual void SetEnable(bool value)
        {
            if (Img != null)
            {
                if (m_greyscaleMaterial != null)
                    Img.material = value || !greyscaleEffect ? null : m_greyscaleMaterial;

                if (imgOnOffSwap)
                    Img.sprite = value ? imgOn : imgOff;
            }

            interactable = value;
        }

        /// <summary>Applies the <see cref="perfectRatio"/> constraint to the rect size based on the current sprite's native dimensions.</summary>
        public void CheckPerfectRatio()
        {
            if (m_img == null || m_img.sprite == null || m_img.type != Image.Type.Sliced
                || m_perfectSpriteId == m_img.sprite.GetInstanceID())
                return;

            if (perfectRatio == PerfectRatio.Width)
            {
                var nativeSize = m_img.sprite.NativeSize();
                var rectSize = RectTransform.sizeDelta;
                m_img.pixelsPerUnitMultiplier = rectSize.x > 0 && rectSize.x < nativeSize.x
                    ? nativeSize.x / rectSize.x
                    : 1;
                m_perfectSpriteId = m_img.sprite.GetInstanceID();
            }
            else if (perfectRatio == PerfectRatio.Height)
            {
                var nativeSize = m_img.sprite.NativeSize();
                var rectSize = RectTransform.sizeDelta;
                m_img.pixelsPerUnitMultiplier = rectSize.y > 0 && rectSize.y < nativeSize.y
                    ? nativeSize.y / rectSize.y
                    : 1;
                m_perfectSpriteId = m_img.sprite.GetInstanceID();
            }
        }

        /// <summary>Plays a scale-pulse bubble effect over <paramref name="duration"/> seconds. Requires DOTWEEN.</summary>
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
