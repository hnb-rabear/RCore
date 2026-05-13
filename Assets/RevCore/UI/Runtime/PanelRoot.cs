using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RevCore.UI
{
    public abstract class PanelRoot : PanelStack
    {
        [Tooltip("A button that acts as a background dimmer. When clicked, it typically triggers the back action on the top panel.")]
        [SerializeField] protected Button m_dimmerOverlay;

        public Action<PanelController> onAnyPanelShow;
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
                m_dimmerOverlay = CreatDimmerOverlay();
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

        public virtual void PushPanelInQueue()
        {
            if (m_panelsInQueue.Count <= 0 || IsBusy() || m_blockQueue)
                return;
            var panel = m_panelsInQueue[0];
            m_panelsInQueue.RemoveAt(0);
            PushPanelToTop(ref panel);
        }

        public void RemovePanelInQueue(PanelController pPanel)
        {
            if (m_panelsInQueue != null && pPanel != null)
                m_panelsInQueue.Remove(pPanel);
        }

        public virtual bool IsBusy(bool queueInvolved = false)
        {
            if (queueInvolved && m_panelsInQueue.Count > 0)
                return true;
            return StackCount > 0;
        }

        private Button CreatDimmerOverlay()
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

        public static T PushOuterPanel<T>(Type root, T pPanel, PushMode pPushMode = PushMode.OnTop, bool pKeepCurrentAndReplace = true) where T : PanelController
        {
            var @event = new PushPanelEvent(root, pPanel, pPushMode, pKeepCurrentAndReplace);
            Events.Publish(@event);
            return @event.panel as T;
        }

        public static T PushInternalPanel<T>(Type root, Type pPanel, PushMode pPushMode = PushMode.OnTop, bool pKeepCurrentAndReplace = true) where T : PanelController
        {
            var @event = new PushPanelEvent(root, pPanel, pPushMode, pKeepCurrentAndReplace);
            Events.Publish(@event);
            return @event.panel as T;
        }

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
