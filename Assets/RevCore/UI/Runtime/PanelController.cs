using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RevCore.UI
{
    /// <summary>
    /// A single panel hosted by a <see cref="PanelStack"/>. Can itself host child panels (it
    /// inherits <see cref="PanelStack"/>), so nested screen flows compose naturally.
    /// Handles show/hide animations via either Animator triggers or coroutine effects
    /// (<c>IE_ShowFX</c>/<c>IE_HideFX</c>).
    /// </summary>
    public class PanelController : PanelStack
    {
        /// <summary>When true, the panel prefab is treated as a one-shot — a fresh instance is created on every push and destroyed after hide.</summary>
        [Tooltip("Set True if this panel is a prefab and rarely used, causing it to be destroyed after hiding.")]
        public bool useOnce;
        /// <summary>When true, the panel uses the <c>IE_ShowFX</c>/<c>IE_HideFX</c> coroutines for transitions instead of Animator triggers.</summary>
        [Tooltip("Enable this to use custom transition effects via IE_HideFX and IE_ShowFX coroutines.")]
        public bool enableFXTransition;
        /// <summary>When true, ensures the panel has its own <see cref="Canvas"/> + <see cref="GraphicRaycaster"/> so it can be parented anywhere.</summary>
        [Tooltip("If true, ensures the panel has its own Canvas and GraphicRaycaster.")]
        public bool nested = true;
        /// <summary>Optional back button. When assigned, its click triggers <see cref="Back"/>.</summary>
        [Tooltip("Optional button to trigger the Back() action.")]
        public Button btnBack;

        /// <summary>Invoked before the show transition begins.</summary>
        public Action onWillShow;
        /// <summary>Invoked before the hide transition begins.</summary>
        public Action onWillHide;
        /// <summary>Invoked when the show transition completes.</summary>
        public Action onDidShow;
        /// <summary>Invoked when the hide transition completes.</summary>
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

        /// <summary>Lazily-resolved <see cref="UnityEngine.CanvasGroup"/> on this panel; added automatically if missing.</summary>
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

        /// <summary>True when the panel is currently shown, or in the middle of its show transition.</summary>
        public bool Displayed => m_showed || m_isShowing;

        /// <summary>True during either the show or hide transition. While transitioning, pushes/pops are rejected.</summary>
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

        /// <summary>Starts the hide transition. <paramref name="pOnDidHide"/> runs when complete.</summary>
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

        /// <summary>Starts the show transition. <paramref name="pOnDidShow"/> runs when complete. Increments the per-type session show counter.</summary>
        public void Show(UnityAction pOnDidShow = null)
        {
            if (m_showed || m_isShowing)
                return;

            if (transform.parent != ParentPanel.transform)
                transform.SetParent(ParentPanel.transform);

            // Activate the GameObject before starting the coroutine. Unity refuses
            // StartCoroutine on inactive objects and logs an error otherwise; with panels
            // typically beginning hidden, every Show() on a fresh panel hit that path and
            // the transition never ran. SetActivePanel(true) inside IE_Show stays — it is
            // idempotent for active GameObjects, so subclass overrides still see the
            // activation hook at the same point in the transition lifecycle.
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

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

        /// <summary>Called when a panel becomes top of stack again after another panel was popped above it. Override to refresh state.</summary>
        public virtual void OnReshow() { }

        private void LockWhileTransiting()
        {
            if (enableFXTransition)
            {
                if (CanvasGroup != null)
                    CanvasGroup.interactable = !Transiting;
            }
        }

        /// <summary>Default back action — pops this panel from its stack. Override for custom back behavior (confirmation dialog, save prompt, etc.).</summary>
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

        /// <summary>
        /// Returns whether this panel allows itself (and any descendants) to be popped right now.
        /// On <c>false</c>, <paramref name="blockingPanel"/> identifies the locked descendant blocking the pop.
        /// </summary>
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

        /// <summary>One-time init called by <see cref="PanelStack.CreatePanel{T}"/> immediately after instantiation. Override to wire up components.</summary>
        public virtual void Init() { }

        /// <summary>Locked panels cannot be popped by <see cref="CanPop"/>. Used for tutorial / forced flows.</summary>
        public virtual void Lock(bool pLock)
        {
            m_locked = pLock;
        }

        /// <summary>True when the panel's GameObject is active and the component is enabled.</summary>
        public bool IsActiveAndEnabled()
        {
            return !gameObject.IsPrefab() && isActiveAndEnabled;
        }

        /// <summary>True when <see cref="Lock"/> has been called with <c>true</c> on this panel.</summary>
        public bool IsLocked()
        {
            if (TopPanel == null)
                return m_locked;
            foreach (var panelController in panelStack)
                if (panelController.IsLocked())
                    return true;
            return false;
        }

        /// <summary>True when this panel is nested inside another panel (rather than directly under a root).</summary>
        public bool IsChildPanel() => ParentPanel.ParentPanel != null;

        /// <summary>True if <paramref name="panel"/> is anywhere in this panel's stack.</summary>
        public bool Contains<T>(T panel) where T : PanelController
        {
            return panelStack.Contains(panel);
        }

        private string SessionShowCountKey => $"{GetType().FullName}:{gameObject.name}";

        /// <summary>Times this panel type has been shown during the current session (process lifetime).</summary>
        public int SessionShowCount => s_sessionShowCounts.GetValueOrDefault(SessionShowCountKey, 0);

        /// <summary>Same as <see cref="SessionShowCount"/> but for type <typeparamref name="T"/> (optionally name-disambiguated). Useful when there's no live instance to query.</summary>
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
