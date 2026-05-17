using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RevCore.UI
{
    /// <summary>
    /// Root coordinator for a screen's panel stack. Subscribes to <see cref="PushPanelEvent"/> /
    /// <see cref="RequestPanelEvent"/> so any subsystem can push a panel by event without coupling
    /// to the root. Maintains a queued-push buffer so concurrent push requests serialize behind
    /// any in-progress show/hide animation.
    /// </summary>
    /// <remarks>
    /// Subclass to define how panel types are resolved at runtime — implement
    /// <see cref="OnResolvePanelByType"/> for type-based pushes and <see cref="OnReceivedPanelRequest"/>
    /// for value-carrying requests. The root inherits Unity Canvas + GraphicRaycaster automatically
    /// (added in <see cref="OnValidate"/>).
    /// </remarks>
    public abstract class PanelRoot : PanelStack
    {
        /// <summary>Full-screen button rendered behind the topmost panel; clicking it triggers <see cref="PanelController.Back"/>. Auto-created on first use.</summary>
        [Tooltip("A button that acts as a background dimmer. When clicked, it typically triggers the back action on the top panel.")]
        [SerializeField] protected Button m_dimmerOverlay;

        /// <summary>Invoked after any descendant panel shows.</summary>
        public Action<PanelController> onAnyPanelShow;
        /// <summary>Invoked after any descendant panel hides.</summary>
        public Action<PanelController> onAnyPanelHide;

        protected readonly List<PanelController> m_panelsInQueue = new();
        protected bool m_blockQueue;

        protected virtual void OnEnable()
        {
            Events.Subscribe<PushPanelEvent>(PushPanelHandler);
            Events.Subscribe<RequestPanelEvent>(RequestPanelHandler);
        }

        protected virtual void OnDisable()
        {
            Events.Unsubscribe<PushPanelEvent>(PushPanelHandler);
            Events.Unsubscribe<RequestPanelEvent>(RequestPanelHandler);
        }

        protected virtual void OnValidate()
        {
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
                gameObject.AddComponent<Canvas>();
            var graphicRaycaster = gameObject.GetComponent<GraphicRaycaster>();
            if (graphicRaycaster == null)
                gameObject.AddComponent<GraphicRaycaster>();
        }

        protected override void OnAnyChildHide(PanelController pPanel)
        {
            base.OnAnyChildHide(pPanel);

            if (m_panelsInQueue.Count > 0 && !IsBusy() && !m_blockQueue)
                PushPanelInQueue();
            else
                ToggleDimmerOverlay();

            onAnyPanelHide?.Invoke(pPanel);
        }

        protected override void OnAnyChildShow(PanelController pPanel)
        {
            base.OnAnyChildShow(pPanel);

            ToggleDimmerOverlay();

            onAnyPanelShow?.Invoke(pPanel);
        }

        protected void ToggleDimmerOverlay()
        {
            if (m_dimmerOverlay == null)
                m_dimmerOverlay = CreateDimmerOverlay();
            var highestPanel = GetHighestPanel();
            if (highestPanel != this)
            {
                m_dimmerOverlay.gameObject.SetActive(true);
                m_dimmerOverlay.transform.SetParent(highestPanel.transform.parent);
                m_dimmerOverlay.transform.SetAsLastSibling();
                highestPanel.transform.SetAsLastSibling();
            }
            else
            {
                m_dimmerOverlay.gameObject.SetActive(false);
                m_dimmerOverlay.transform.SetParent(transform);
            }
        }

        /// <summary>
        /// Enqueues <paramref name="pPanel"/> for showing after the stack drains. When the stack
        /// is empty, pushes immediately. Returns the resolved panel instance.
        /// </summary>
        public virtual T AddPanelToQueue<T>(ref T pPanel) where T : PanelController
        {
            if (pPanel == null)
                return null;
            if (TopPanel == pPanel)
                return pPanel;
            if (StackCount == 0)
            {
                var popup = PushPanelToTop(ref pPanel);
                return popup;
            }
            var popupInQueue = CreatePanel(ref pPanel);
            if (!m_panelsInQueue.Contains(popupInQueue))
                m_panelsInQueue.Add(popupInQueue);
            return popupInQueue;
        }

        protected void AddPanelToQueue<T>(T pPanel) where T : PanelController
        {
            if (StackCount == 0)
            {
                PushPanelToTop(ref pPanel);
                return;
            }
            if (!m_panelsInQueue.Contains(pPanel))
                m_panelsInQueue.Add(pPanel);
        }

        /// <summary>Dequeues and pushes the next queued panel. No-op when queue is empty, stack is busy, or the queue is blocked.</summary>
        public virtual void PushPanelInQueue()
        {
            if (m_panelsInQueue.Count <= 0 || IsBusy() || m_blockQueue)
                return;
            var panel = m_panelsInQueue[0];
            m_panelsInQueue.RemoveAt(0);
            PushPanelToTop(ref panel);
        }

        /// <summary>Removes <paramref name="pPanel"/> from the queue if present. No-op for null or unknown panels.</summary>
        public void RemovePanelInQueue(PanelController pPanel)
        {
            if (m_panelsInQueue != null && pPanel != null)
                m_panelsInQueue.Remove(pPanel);
        }

        /// <summary>True when there are panels on the stack, or (with <paramref name="queueInvolved"/>) panels waiting in the queue.</summary>
        public virtual bool IsBusy(bool queueInvolved = false)
        {
            if (queueInvolved && m_panelsInQueue.Count > 0)
                return true;
            return StackCount > 0;
        }

        private Button CreateDimmerOverlay()
        {
            var fullScreenImageObj = new GameObject("BtnBackBackground", typeof(Image));
            fullScreenImageObj.transform.SetParent(transform, false);

            var fullScreenImage = fullScreenImageObj.GetComponent<Image>();
            fullScreenImage.color = new Color(0, 0, 0, 0.96f);

            var rectTransform = fullScreenImage.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(0, 400);

            fullScreenImageObj.SetActive(false);

            var button = fullScreenImageObj.AddComponent<Button>();
            button.onClick.AddListener(() =>
            {
                if (TopPanel != null)
                    TopPanel.Back();
            });
            button.transition = Selectable.Transition.None;
            return button;
        }

        private void PushPanelHandler(PushPanelEvent e)
        {
            if (e.rootType != GetType())
                return;
            if (e.panel != null)
            {
                switch (e.pushMode)
                {
                    case PushMode.OnTop:
                        PushPanelToTop(ref e.panel);
                        break;
                    case PushMode.Replacement:
                        PushPanel(ref e.panel, e.keepCurrentAndReplace);
                        break;
                    case PushMode.Queued:
                        AddPanelToQueue(ref e.panel);
                        break;
                }
            }
            else if (e.panelType != null)
            {
                var panelController = OnResolvePanelByType(e.panelType);
                if (panelController != null)
                {
                    switch (e.pushMode)
                    {
                        case PushMode.OnTop:
                            e.panel = PushPanelToTop(ref panelController);
                            break;
                        case PushMode.Replacement:
                            e.panel = PushPanel(ref panelController, e.keepCurrentAndReplace);
                            break;
                        case PushMode.Queued:
                            e.panel = AddPanelToQueue(ref panelController);
                            break;
                    }
                }
                else
                    Debug.LogError($"Panel of type {e.panelType.Name} not resolved by {GetType().Name}.");
            }
        }

        private void RequestPanelHandler(RequestPanelEvent e)
        {
            if (e.rootType != GetType())
                return;
            e.panel = OnReceivedPanelRequest(e.panelType, e.value);
        }

        protected virtual PanelController OnResolvePanelByType(Type panelType)
        {
            return null;
        }

        protected abstract PanelController OnReceivedPanelRequest(Type panelType, object value);

        /// <summary>
        /// Publishes a <see cref="PushPanelEvent"/> to push <paramref name="pPanel"/> on the root of
        /// type <paramref name="root"/>. Returns the pushed panel (or null on failure).
        /// </summary>
        public static T PushOuterPanel<T>(Type root, T pPanel, PushMode pPushMode = PushMode.OnTop, bool pKeepCurrentAndReplace = true) where T : PanelController
        {
            var @event = new PushPanelEvent(root, pPanel, pPushMode, pKeepCurrentAndReplace);
            Events.Publish(@event);
            return @event.panel as T;
        }

        /// <summary>
        /// Publishes a <see cref="PushPanelEvent"/> that names the panel by <see cref="Type"/>.
        /// The root resolves it via <see cref="OnResolvePanelByType"/>.
        /// </summary>
        public static T PushInternalPanel<T>(Type root, Type pPanel, PushMode pPushMode = PushMode.OnTop, bool pKeepCurrentAndReplace = true) where T : PanelController
        {
            var @event = new PushPanelEvent(root, pPanel, pPushMode, pKeepCurrentAndReplace);
            Events.Publish(@event);
            return @event.panel as T;
        }

        /// <summary>
        /// Publishes a <see cref="RequestPanelEvent"/> to ask <paramref name="root"/> for an instance of
        /// <paramref name="panel"/> initialized with <paramref name="value"/>. Returns the resolved panel,
        /// or null if the root rejected the request.
        /// </summary>
        public static T RequestPanel<T>(Type root, Type panel, object value) where T : PanelController
        {
            var @event = new RequestPanelEvent(root, panel, value);
            Events.Publish(@event);
            return @event.panel as T;
        }
    }

    internal class PushPanelEvent : IEvent
    {
        public Type rootType;
        public Type panelType;
        public PanelController panel;
        public PanelStack.PushMode pushMode;
        public bool keepCurrentAndReplace;

        public PushPanelEvent(Type root, PanelController pPanel, PanelStack.PushMode pPushMode = PanelStack.PushMode.OnTop, bool pKeepCurrentAndReplace = true)
        {
            rootType = root;
            panel = pPanel;
            pushMode = pPushMode;
            keepCurrentAndReplace = pKeepCurrentAndReplace;
        }

        public PushPanelEvent(Type root, Type pPanel, PanelStack.PushMode pPushMode = PanelStack.PushMode.OnTop, bool pKeepCurrentAndReplace = true)
        {
            rootType = root;
            panelType = pPanel;
            pushMode = pPushMode;
            keepCurrentAndReplace = pKeepCurrentAndReplace;
        }
    }

    internal class RequestPanelEvent : IEvent
    {
        public Type rootType;
        public Type panelType;
        public object value;
        public PanelController panel;

        public RequestPanelEvent(Type root, Type pPanel, object pValue = null)
        {
            rootType = root;
            panelType = pPanel;
            value = pValue;
        }
    }
}
