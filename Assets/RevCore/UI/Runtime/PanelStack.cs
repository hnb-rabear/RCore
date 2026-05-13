using System.Collections.Generic;
using UnityEngine;

namespace RevCore.UI
{
    public abstract class PanelStack : MonoBehaviour
    {
        public enum PushMode
        {
            OnTop,
            Replacement,
            Queued,
        }

        protected Stack<PanelController> panelStack = new();
        private Dictionary<int, PanelController> m_cachedOnceUsePanels = new();
        private Dictionary<int, PanelController> m_createdPanels = new();

        public PanelStack ParentPanel { get; private set; }
        public virtual PanelController TopPanel => panelStack != null && panelStack.Count > 0 ? panelStack.Peek() : null;

        public int Index
        {
            get
            {
                if (ParentPanel == null)
                    return 0;
                int i = 0;
                foreach (var p in ParentPanel.panelStack)
                {
                    if (p == this)
                        return i;
                    i++;
                }
                return ParentPanel.panelStack.Count;
            }
        }

        public int DisplayOrder
        {
            get
            {
                if (ParentPanel == null)
                    return 1;
                return ParentPanel.panelStack.Count - Index;
            }
        }

        public int StackCount => panelStack?.Count ?? 0;

        protected virtual void Awake()
        {
            if (ParentPanel == null)
                ParentPanel = GetComponentInParent<PanelStack>();
            if (ParentPanel == this)
                ParentPanel = null;
        }

        public T CreatePanel<T>(ref T pPanel) where T : PanelController
        {
            if (pPanel == null)
                return null;

            if (!pPanel.useOnce)
            {
                if (pPanel.gameObject.IsPrefab())
                {
                    int prefabId = pPanel.gameObject.GetInstanceID();
                    if (!m_createdPanels.ContainsKey(prefabId) || m_createdPanels[prefabId] == null)
                    {
                        string panelName = pPanel.name;
                        pPanel = Instantiate(pPanel, transform);
                        pPanel.gameObject.SetActive(false);
                        pPanel.Init();
                        pPanel.name = panelName;
                        m_createdPanels.TryAdd(prefabId, pPanel);
                    }
                    else
                        pPanel = m_createdPanels[prefabId] as T;
                }
                return pPanel;
            }
            if (!pPanel.gameObject.IsPrefab())
                Debug.LogWarning("Once used panel must be prefab!");

            string name2 = pPanel.name;
            var panel = Instantiate(pPanel, transform);
            panel.useOnce = true;
            panel.gameObject.SetActive(false);
            panel.Init();
            panel.name = name2;

            if (!m_cachedOnceUsePanels.ContainsKey(pPanel.GetInstanceID()))
                m_cachedOnceUsePanels.Add(pPanel.GetInstanceID(), panel);
            else
                m_cachedOnceUsePanels[pPanel.GetInstanceID()] = panel;

            return panel;
        }

        protected T GetCachedPanel<T>(T pOriginal) where T : PanelController
        {
            if (pOriginal == null)
                return null;
            if (pOriginal.useOnce)
            {
                if (m_cachedOnceUsePanels.ContainsKey(pOriginal.GetInstanceID()))
                    return m_cachedOnceUsePanels[pOriginal.GetInstanceID()] as T;
                return null;
            }
            return pOriginal;
        }

        public PanelStack GetRootPanel()
        {
            return ParentPanel != null ? ParentPanel.GetRootPanel() : this;
        }

        public PanelStack GetHighestPanel()
        {
            return TopPanel != null ? TopPanel.GetHighestPanel() : this;
        }

        public virtual T PushPanel<T>(ref T pPanel, bool keepCurrentInStack, bool onlyInactivePanel = true, bool instantPopAndPush = true) where T : PanelController
        {
            var panel = CreatePanel(ref pPanel);
            PushPanel(panel, keepCurrentInStack, onlyInactivePanel, instantPopAndPush);
            return panel;
        }

        public void PushPanel(PanelController panel, bool keepCurrentInStack, bool onlyInactivePanel = true, bool instantPopAndPush = true)
        {
            if (panel == null)
            {
                Log("Panel is null");
                return;
            }

            if (panel.Transiting)
            {
                Log($"Couldn't push, because panel {panel.name} is transiting");
                return;
            }

            if (onlyInactivePanel && panel.Displayed && panelStack.Contains(panel))
            {
                Log($"Panel {panel.name} already displayed");
                return;
            }

            if (TopPanel == panel)
            {
                Log($"Couldn't push, because panel {panel.name} doesn't have parent!");
                return;
            }

            if (TopPanel != null && !TopPanel.CanPop(out PanelController blockingPanel))
            {
                Log($"{blockingPanel.name} can't hide now!");
                PushPanelToTop(panel);
                return;
            }

            panel.ParentPanel = this;
            if (TopPanel != null)
            {
                var currentTopPanel = TopPanel;
                if (currentTopPanel.Displayed)
                {
                    currentTopPanel.Hide(() =>
                    {
                        if (!instantPopAndPush)
                            HideThenShow();

                        OnAnyChildHide(currentTopPanel);
                    });

                    if (instantPopAndPush)
                        HideThenShow();
                }
                else
                    HideThenShow();

                void HideThenShow()
                {
                    if (!keepCurrentInStack)
                        panelStack.Pop();

                    foreach (var panelController in panelStack)
                        panelController.Hide();

                    panelStack.Push(panel);
                    panel.Show();
                    OnAnyChildShow(panel);
                }
            }
            else
            {
                panelStack.Push(panel);
                panel.Show();
                OnAnyChildShow(panel);
            }
        }

        protected void PopPanel(bool instant = true)
        {
            if (TopPanel == null)
            {
                Log("Parent Panel is null");
                return;
            }

            if (TopPanel != null && !TopPanel.CanPop(out PanelController blockingPanel))
            {
                Log($"{blockingPanel.name} can't hide now!");
                return;
            }

            var topStack = panelStack.Pop();
            if (topStack.Displayed)
            {
                topStack.Hide(() =>
                {
                    if (!instant)
                    {
                        var newPanel = TopPanel;
                        if (newPanel != null && !newPanel.Displayed)
                        {
                            newPanel.Show();
                            newPanel.OnReshow();
                            OnAnyChildShow(newPanel);
                        }
                    }

                    OnAnyChildHide(topStack);
                });

                if (instant)
                {
                    var newPanel = TopPanel;
                    if (newPanel != null && !newPanel.Displayed)
                    {
                        newPanel.Show();
                        newPanel.OnReshow();
                        OnAnyChildShow(newPanel);
                    }
                }
            }
            else
            {
                var newPanel = TopPanel;
                if (newPanel != null && !newPanel.Displayed)
                {
                    newPanel.Show();
                    newPanel.OnReshow();
                    OnAnyChildShow(newPanel);
                }
            }
        }

        public virtual T PushPanelToTop<T>(ref T pPanel, bool hidePusher = false) where T : PanelController
        {
            if (!hidePusher || ParentPanel == null)
            {
                var panel = CreatePanel(ref pPanel);
                PushPanelToTop(panel);
                return panel;
            }
            return ParentPanel.PushPanel(ref pPanel, true);
        }

        protected virtual void PushPanelToTop(PanelController panel)
        {
            if (panel == null)
            {
                Log("Panel is null");
                return;
            }

            if (TopPanel == panel && TopPanel.Displayed)
                return;

            panelStack.Push(panel);
            panel.ParentPanel = this;
            panel.Show();
            OnAnyChildShow(panel);
        }

        public void PopAllThenPush(PanelController panel)
        {
            PopAllPanels();
            PushPanel(panel, false);
        }

        public void PopTillOneLeft()
        {
            var lockedPanels = new List<PanelController>();
            PanelController lastTopPanel = null;
            while (panelStack.Count > 1)
            {
                lastTopPanel = panelStack.Pop();
                if (lastTopPanel.IsLocked())
                    lockedPanels.Add(lastTopPanel);
                else
                    lastTopPanel.Hide();
            }

            if (lockedPanels.Count > 0)
            {
                for (int i = lockedPanels.Count - 1; i >= 0; i--)
                    panelStack.Push(lockedPanels[i]);
            }

            if (!TopPanel.Displayed)
            {
                TopPanel.Show();
                OnAnyChildShow(TopPanel);
            }

            if (lastTopPanel != null)
                OnAnyChildHide(lastTopPanel);
        }

        public void PopTillNoPanel(PanelController panel)
        {
            if (!panelStack.Contains(panel))
            {
                Log($"Panel {name} doesn't contain panel {panel.name}");
                return;
            }

            var lockedPanels = new List<PanelController>();
            PanelController lastTopPanel;

            do
            {
                lastTopPanel = panelStack.Pop();
                if (lastTopPanel.IsLocked())
                    lockedPanels.Add(lastTopPanel);
                else
                    lastTopPanel.Hide();

            } while (lastTopPanel.GetInstanceID() != panel.GetInstanceID() && panelStack.Count > 0);

            if (lockedPanels.Count > 0)
            {
                for (int i = lockedPanels.Count - 1; i >= 0; i--)
                    panelStack.Push(lockedPanels[i]);
            }

            var newPanel = TopPanel;
            if (newPanel != null && !newPanel.Displayed)
            {
                newPanel.Show();
                newPanel.OnReshow();
                OnAnyChildShow(newPanel);
            }

            if (lastTopPanel != null)
                OnAnyChildHide(lastTopPanel);
        }

        public void PopTillPanel(PanelController panel)
        {
            if (!panelStack.Contains(panel))
            {
                Log($"Panel {name} doesn't contain panel {panel.name}");
                return;
            }

            var lockedPanels = new List<PanelController>();
            PanelController curTopPanel = null;

            while (panelStack.Count > 0)
            {
                curTopPanel = panelStack.Peek();
                if (curTopPanel.GetInstanceID() == panel.GetInstanceID())
                    break;

                panelStack.Pop();
                if (curTopPanel.IsLocked())
                    lockedPanels.Add(curTopPanel);
                else
                    curTopPanel.Hide();
            }

            if (lockedPanels.Count > 0)
            {
                for (int i = lockedPanels.Count - 1; i >= 0; i--)
                    panelStack.Push(lockedPanels[i]);
            }

            var newPanel = TopPanel;
            if (newPanel != null && !newPanel.Displayed)
            {
                newPanel.Show();
                newPanel.OnReshow();
                OnAnyChildShow(newPanel);
            }

            if (curTopPanel != null)
                OnAnyChildHide(curTopPanel);
        }

        public virtual void PopAllPanels()
        {
            var lockedPanels = new List<PanelController>();
            PanelController lastTopPanel = null;
            while (panelStack.Count > 0)
            {
                lastTopPanel = panelStack.Pop();
                if (lastTopPanel.IsLocked())
                    lockedPanels.Add(lastTopPanel);
                else
                    lastTopPanel.Hide();
            }

            if (lockedPanels.Count > 0)
            {
                for (int i = lockedPanels.Count - 1; i >= 0; i--)
                    panelStack.Push(lockedPanels[i]);
            }

            if (lastTopPanel != null)
                OnAnyChildHide(lastTopPanel);
        }

        public void PopChildrenThenParent()
        {
            if (TopPanel == null)
                return;

            if (TopPanel.TopPanel != null)
                TopPanel.PopChildrenThenParent();
            else
                PopPanel();
        }

        protected virtual void OnAnyChildHide(PanelController pPanel)
        {
            if (ParentPanel != null)
                ParentPanel.OnAnyChildHide(pPanel);
        }

        protected virtual void OnAnyChildShow(PanelController pPanel)
        {
            if (ParentPanel != null)
                ParentPanel.OnAnyChildShow(pPanel);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        protected void Log(string pMessage)
        {
            Debug.Log($"<color=yellow><b>[{gameObject.name}]:</b></color>{pMessage}");
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        protected void LogError(string pMessage)
        {
            Debug.LogError($"<color=red><b>[{gameObject.name}]:</b></color>{pMessage}");
        }
    }
}
