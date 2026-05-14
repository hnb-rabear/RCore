#if DOTWEEN
using DG.Tweening;
#endif
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RevCore.UI
{
    [AddComponentMenu("RevCore/UI/JustToggle")]
    public class JustToggle : Toggle
    {
        [Serializable]
        public class SizeTransition
        {
            public RectTransform target;
            public LayoutElement layoutElement;
            public Vector3 on;
            public Vector3 off;
        }

        [Serializable]
        public class PositionTransition
        {
            public RectTransform target;
            public Vector3 on;
            public Vector3 off;
        }

        [Serializable]
        public class ColorTransition
        {
            public MaskableGraphic target;
            public Color on;
            public Color off;
        }

        [Serializable]
        public class SpriteTransition
        {
            public Image target;
            public Sprite on;
            public Sprite off;
        }

        public Image imgBackground;
        public TextMeshProUGUI txtLabel;
        public List<RectTransform> contentsActive;
        public List<RectTransform> contentsInactive;
        public string sfxClip = "button";
        public string sfxClipOff = "button";
        public bool scaleBounceEffect = true;
        public bool enableSizeSwitch;
        public Vector2 sizeActive;
        public Vector2 sizeInactive;
        public float tweenTime = 0.3f;
        public SizeTransition[] sizeTransitions;
        public PositionTransition[] positionTransitions;
        public ColorTransition[] colorTransitions;
        public SpriteTransition[] spriteTransitions;

        [ReadOnly] public bool isLocked;
        public Action onClickOnLock;

        private CustomToggleGroup m_customToggleGroup;
        private bool m_isOn2;
        private Vector2 m_initialScale;
        private bool m_clicked;

        public bool IsOn
        {
            get => isOn;
            set
            {
                isOn = value;
                if (m_isOn2 != value)
                {
                    m_isOn2 = value;
                    onValueChanged?.Invoke(value);
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();
            m_initialScale = transform.localScale;
        }

        protected override void Start()
        {
            base.Start();
            m_customToggleGroup = group as CustomToggleGroup;
            m_isOn2 = isOn;

            if (isOn && m_customToggleGroup != null)
                m_customToggleGroup.SetTarget(transform as RectTransform);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            if (Application.isPlaying)
                return;

            base.OnValidate();

            if (txtLabel == null)
                txtLabel = gameObject.FindComponentInChildren<TextMeshProUGUI>();

            if (imgBackground == null)
            {
                var images = gameObject.GetComponentsInChildren<Image>();
                if (images.Length > 0)
                    imgBackground = images[0];
            }

            if (contentsInactive != null)
                for (var i = contentsInactive.Count - 1; i >= 0; i--)
                    if (contentsInactive[i] == null)
                        contentsInactive.RemoveAt(i);

            if (enableSizeSwitch && sizeActive == Vector2.zero)
                sizeActive = ((RectTransform)transform).sizeDelta;

            if (group == null)
                group = gameObject.GetComponentInParent<ToggleGroup>();

            if (scaleBounceEffect)
            {
                if (transition == Transition.Animation)
                    transition = Transition.None;

                if (gameObject.TryGetComponent(out Animator animator))
                    animator.enabled = false;
            }
            else if (transition == Transition.Animation)
            {
                var animator = gameObject.GetOrAddComponent<Animator>();
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                animator.enabled = true;
            }

            if (sizeTransitions != null)
                foreach (var transition1 in sizeTransitions)
                {
                    if (transition1.target == null) continue;
                    if (transition1.layoutElement == null) transition1.target.TryGetComponent(out transition1.layoutElement);
                    if (transition1.on == default) transition1.on = transition1.target.rect.size;
                    if (transition1.off == default) transition1.off = transition1.target.rect.size;
                }
            if (positionTransitions != null)
                foreach (var transition1 in positionTransitions)
                {
                    if (transition1.target == null) continue;
                    if (transition1.on == default) transition1.on = transition1.target.anchoredPosition;
                    if (transition1.off == default) transition1.off = transition1.target.anchoredPosition;
                }
        }
#endif

        protected override void OnEnable()
        {
            base.OnEnable();

            if (m_initialScale == Vector2.zero)
                m_initialScale = Vector2.one;

            if (scaleBounceEffect)
                transform.localScale = m_initialScale;

            onValueChanged.AddListener(OnValueChanged);
            Refresh();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (m_initialScale == Vector2.zero)
                m_initialScale = Vector2.one;

            if (scaleBounceEffect)
                transform.localScale = m_initialScale;

            onValueChanged.RemoveListener(OnValueChanged);
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (isLocked)
            {
                onClickOnLock?.Invoke();
                return;
            }
            m_clicked = true;
            base.OnPointerClick(eventData);
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            if (!scaleBounceEffect)
                return;
            if (isLocked && onClickOnLock == null)
                return;

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

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);

            if (!scaleBounceEffect)
                return;
            if (isLocked && onClickOnLock == null)
                return;

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

        private void Refresh()
        {
            if (contentsActive != null)
                foreach (var item in contentsActive)
                    item.gameObject.SetActive(isOn);

            if (contentsInactive != null)
                foreach (var item in contentsInactive)
                    item.gameObject.SetActive(!isOn);

            if (enableSizeSwitch)
            {
                var size = isOn ? sizeActive : sizeInactive;
                if (gameObject.TryGetComponent(out LayoutElement layoutElement))
                {
                    layoutElement.minWidth = size.x;
                    layoutElement.minHeight = size.y;
                }
                else
                    ((RectTransform)transform).sizeDelta = size;
            }

            if (sizeTransitions != null)
                foreach (var transition1 in sizeTransitions)
                {
                    var size = isOn ? transition1.on : transition1.off;
                    if (transition1.layoutElement != null)
                    {
                        transition1.layoutElement.minWidth = size.x;
                        transition1.layoutElement.minHeight = size.y;
                    }
                    if (transition1.target != null)
                        transition1.target.sizeDelta = size;
                }

            if (positionTransitions != null)
                foreach (var transition1 in positionTransitions)
                    if (transition1.target != null)
                        transition1.target.anchoredPosition = isOn ? transition1.on : transition1.off;

            if (colorTransitions != null)
                foreach (var transition1 in colorTransitions)
                    if (transition1.target != null)
                        transition1.target.color = isOn ? transition1.on : transition1.off;

            if (spriteTransitions != null)
                foreach (var transition1 in spriteTransitions)
                    if (transition1.target != null)
                        transition1.target.sprite = isOn ? transition1.on : transition1.off;

            if (m_customToggleGroup != null && isOn)
                m_customToggleGroup.SetTarget(transform as RectTransform, tweenTime);
        }

        private void RefreshByTween()
        {
#if DOTWEEN
            if (m_isOn2 == isOn)
                return;

            m_isOn2 = isOn;

            if (contentsActive != null)
                foreach (var item in contentsActive)
                    item.gameObject.SetActive(isOn);

            if (contentsInactive != null)
                foreach (var item in contentsInactive)
                    item.gameObject.SetActive(!isOn);

            if (Application.isPlaying)
            {
                bool hasTransitions = enableSizeSwitch
                    || (sizeTransitions != null && sizeTransitions.Length > 0)
                    || (positionTransitions != null && positionTransitions.Length > 0)
                    || (colorTransitions != null && colorTransitions.Length > 0)
                    || (spriteTransitions != null && spriteTransitions.Length > 0);
                if (hasTransitions)
                {
                    if (m_customToggleGroup != null)
                        m_customToggleGroup.SetToggleInteractable(false);

                    var layoutElement = gameObject.GetComponent<LayoutElement>();
                    var sizeFrom = !isOn ? sizeActive : sizeInactive;
                    var sizeTo = isOn ? sizeActive : sizeInactive;
                    float lerp = 0;
                    var rectTransform = transform as RectTransform;
                    DOTween.Kill(GetInstanceID() + 1);

                    DOTween.To(() => lerp, value => lerp = value, 1, tweenTime)
                        .OnUpdate(() =>
                        {
                            if (enableSizeSwitch)
                            {
                                var size = Vector2.Lerp(sizeFrom, sizeTo, lerp);
                                if (layoutElement == null)
                                    rectTransform.sizeDelta = size;
                                else
                                {
                                    layoutElement.minWidth = size.x;
                                    layoutElement.minHeight = size.y;
                                }
                            }

                            if (sizeTransitions != null)
                                foreach (var transition1 in sizeTransitions)
                                {
                                    if (transition1.target == null) continue;
                                    var size = isOn ? Vector2.Lerp(transition1.off, transition1.on, lerp) : Vector2.Lerp(transition1.on, transition1.off, lerp);
                                    if (transition1.layoutElement != null)
                                    {
                                        transition1.layoutElement.minWidth = size.x;
                                        transition1.layoutElement.minHeight = size.y;
                                    }
                                    transition1.target.sizeDelta = size;
                                }

                            if (positionTransitions != null)
                                foreach (var transition1 in positionTransitions)
                                    if (transition1.target != null)
                                        transition1.target.anchoredPosition = isOn ? Vector2.Lerp(transition1.off, transition1.on, lerp) : Vector2.Lerp(transition1.on, transition1.off, lerp);

                            if (colorTransitions != null)
                                foreach (var transition1 in colorTransitions)
                                    if (transition1.target != null)
                                        transition1.target.color = isOn ? Color.Lerp(transition1.off, transition1.on, lerp) : Color.Lerp(transition1.on, transition1.off, lerp);

                            if (spriteTransitions != null)
                                foreach (var transition1 in spriteTransitions)
                                {
                                    if (transition1.target == null) continue;
                                    if (lerp < 0.5f)
                                    {
                                        var tempColor = transition1.target.color;
                                        tempColor.a = Mathf.Lerp(1, 0.3f, lerp * 2);
                                        transition1.target.color = tempColor;
                                        transition1.target.sprite = isOn ? transition1.off : transition1.on;
                                    }
                                    else
                                    {
                                        var tempColor = transition1.target.color;
                                        tempColor.a = Mathf.Lerp(0.3f, 1, (lerp - 0.5f) * 2);
                                        transition1.target.color = tempColor;
                                        transition1.target.sprite = isOn ? transition1.on : transition1.off;
                                    }
                                }
                        })
                        .OnComplete(() =>
                        {
                            if (m_customToggleGroup != null)
                                m_customToggleGroup.SetToggleInteractable(true);
                            Refresh();
                        })
                        .SetId(GetInstanceID() + 1)
                        .SetEase(Ease.OutCubic)
                        .SetUpdate(true);
                }
                if (m_customToggleGroup != null)
                    m_customToggleGroup.SetTarget(transform as RectTransform, tweenTime);
            }
#endif
        }

        private void OnValueChanged(bool value)
        {
            if (m_clicked)
            {
                if (IsOn && !string.IsNullOrEmpty(sfxClip))
                    Events.Publish(new UISfxTriggeredEvent(sfxClip));
                else if (!isOn && !string.IsNullOrEmpty(sfxClipOff))
                    Events.Publish(new UISfxTriggeredEvent(sfxClipOff));
                m_clicked = false;
            }

#if DOTWEEN
            if (Application.isPlaying && tweenTime > 0 && transition != Transition.Animation)
            {
                RefreshByTween();
                return;
            }
#endif
            Refresh();
        }
    }
}
