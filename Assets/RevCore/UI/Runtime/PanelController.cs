using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RevCore.UI
{
    public class PanelController : PanelStack
    {
        [Tooltip("Set True if this panel is a prefab and rarely used, causing it to be destroyed after hiding.")]
        public bool useOnce;
        [Tooltip("Enable this to use custom transition effects via IE_HideFX and IE_ShowFX coroutines.")]
        public bool enableFXTransition;
        [Tooltip("If true, ensures the panel has its own Canvas and GraphicRaycaster.")]
        public bool nested = true;
        [Tooltip("Optional button to trigger the Back() action.")]
        public Button btnBack;

        public Action onWillShow;
        public Action onWillHide;
        public Action onDidShow;
        public Action onDidHide;

        protected bool m_showed;
        protected bool m_isShowing;
        protected bool m_isHiding;
        private bool m_locked;

        private static readonly Dictionary<string, int> s_sessionShowCounts = new();
        private CanvasGroup m_canvasGroup;
        private static readonly int m_AnimClose = Animator.StringToHash("close");
        private static readonly int m_AnimOpen = Animator.StringToHash("open");
        private Coroutine m_transitionCoroutine;

        public CanvasGroup CanvasGroup
        {
            get
            {
                if (m_canvasGroup == null)
                {
                    TryGetComponent(out m_canvasGroup);
                    if (m_canvasGroup == null)
                        m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
                return m_canvasGroup;
            }
        }

        public bool Displayed => m_showed || m_isShowing;
        public bool Transiting => m_isShowing || m_isHiding;

        protected override void Awake()
        {
            base.Awake();

            if (btnBack != null)
                btnBack.onClick.AddListener(BtnBack_Pressed);
        }

        protected virtual void OnDisable()
        {
            LockWhileTransiting();
        }

        protected virtual void OnValidate()
        {
            if (Application.isPlaying)
                return;
            if (nested)
            {
                var canvas = gameObject.GetComponent<Canvas>();
                if (canvas == null)
                    gameObject.AddComponent<Canvas>();
                var graphicRaycaster = gameObject.GetComponent<GraphicRaycaster>();
                if (graphicRaycaster == null)
                    gameObject.AddComponent<GraphicRaycaster>();
            }
            if (TryGetComponent(out Animator animator))
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        public void Hide(UnityAction pOnDidHide = null)
        {
            if (!m_showed || m_isHiding)
                return;

            m_transitionCoroutine = StartCoroutine(IE_Hide(pOnDidHide));
        }

        protected IEnumerator IE_Hide(UnityAction pOnDidHide)
        {
            m_isHiding = true;

            onWillHide?.Invoke();

            BeforeHiding();
            LockWhileTransiting();

            while (panelStack.Count > 0)
            {
                var subPanel = panelStack.Pop();
                yield return subPanel.IE_Hide(null);
            }

            PopAllPanels();

            if (enableFXTransition)
                yield return IE_HideFX();
            else
                yield return null;

            m_isHiding = false;
            m_showed = false;
            SetActivePanel(false);

            LockWhileTransiting();
            AfterHiding();
            if (useOnce)
                Destroy(gameObject, 0.1f);

            pOnDidHide?.Invoke();
            onDidHide?.Invoke();
            m_transitionCoroutine = null;
        }

        protected virtual IEnumerator IE_HideFX()
        {
            if (TryGetComponent(out Animator animator) && animator.parameters.Exists(x => x.name == "close"))
            {
                animator.SetTrigger(m_AnimClose);
                yield return null;
                var info = animator.GetCurrentAnimatorStateInfo(0);
                yield return new WaitForSecondsRealtime(info.length - Time.deltaTime);
            }
        }

        protected virtual void BeforeHiding() { }
        protected virtual void AfterHiding() { }

        public void Show(UnityAction pOnDidShow = null)
        {
            if (m_showed || m_isShowing)
                return;

            if (transform.parent != ParentPanel.transform)
                transform.SetParent(ParentPanel.transform);

            m_transitionCoroutine = StartCoroutine(IE_Show(pOnDidShow));
        }

        protected IEnumerator IE_Show(UnityAction pOnDidShow)
        {
            m_isShowing = true;

            string key = SessionShowCountKey;
            s_sessionShowCounts[key] = s_sessionShowCounts.GetValueOrDefault(key, 0) + 1;

            onWillShow?.Invoke();

            BeforeShowing();
            LockWhileTransiting();

            transform.SetAsLastSibling();

            SetActivePanel(true);
            if (enableFXTransition)
                yield return IE_ShowFX();
            else
                yield return null;

            m_isShowing = false;
            m_showed = true;

            LockWhileTransiting();
            AfterShowing();

            pOnDidShow?.Invoke();
            onDidShow?.Invoke();
            m_transitionCoroutine = null;
        }

        protected virtual IEnumerator IE_ShowFX()
        {
            if (TryGetComponent(out Animator animator) && animator.parameters.Exists(x => x.name == "open"))
            {
                animator.SetTrigger(m_AnimOpen);
                yield return null;
                var info = animator.GetCurrentAnimatorStateInfo(0);
                yield return new WaitForSecondsRealtime(info.length - Time.deltaTime);
            }
        }

        protected virtual void BeforeShowing() { }
        protected virtual void AfterShowing() { }

        protected virtual void SetActivePanel(bool pValue)
        {
            gameObject.SetActive(pValue);
        }

        public virtual void OnReshow() { }

        private void LockWhileTransiting()
        {
            if (enableFXTransition)
            {
                if (CanvasGroup != null)
                    CanvasGroup.interactable = !Transiting;
            }
        }

        public virtual void Back()
        {
            if (ParentPanel == null)
            {
                if (TopPanel != null)
                    TopPanel.Back();
            }
            else
                ParentPanel.PopChildrenThenParent();
        }

        protected virtual void BtnBack_Pressed()
        {
            Back();
        }

        public bool CanPop(out PanelController blockingPanel)
        {
            blockingPanel = null;
            foreach (var p in panelStack)
            {
                if (p.m_locked || p.Transiting)
                {
                    blockingPanel = p;
                    return false;
                }
            }
            if (m_locked || Transiting)
            {
                blockingPanel = this;
                return false;
            }
            return true;
        }

        public virtual void Init() { }

        public virtual void Lock(bool pLock)
        {
            m_locked = pLock;
        }

        public bool IsActiveAndEnabled()
        {
            return !gameObject.IsPrefab() && isActiveAndEnabled;
        }

        public bool IsLocked()
        {
            if (TopPanel == null)
                return m_locked;
            foreach (var panelController in panelStack)
                if (panelController.IsLocked())
                    return true;
            return false;
        }

        public bool IsChildPanel() => ParentPanel.ParentPanel != null;

        public bool Contains<T>(T panel) where T : PanelController
        {
            return panelStack.Contains(panel);
        }

        private string SessionShowCountKey => $"{GetType().FullName}:{gameObject.name}";

        public int SessionShowCount => s_sessionShowCounts.GetValueOrDefault(SessionShowCountKey, 0);

        public static int GetSessionShowCount<T>(string name = null) where T : PanelController
        {
            if (name != null)
                return s_sessionShowCounts.GetValueOrDefault($"{typeof(T).FullName}:{name}", 0);
            string prefix = typeof(T).FullName;
            int total = 0;
            foreach (var kvp in s_sessionShowCounts)
                if (kvp.Key == prefix || kvp.Key.StartsWith(prefix + ":"))
                    total += kvp.Value;
            return total;
        }
    }
}
