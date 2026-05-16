using System.Collections.Generic;
using UnityEngine;

namespace RevCore.UI
{
    /// <summary>
    /// Stack-based panel host. <see cref="PanelController"/> instances push and pop on this stack;
    /// the top of the stack is the visible panel, the rest are hidden but in memory. Supports nested
    /// stacks: a <see cref="PanelStack"/> can itself host child panels via <see cref="ParentPanel"/>.
    /// </summary>
    /// <remarks>
    /// Caches instantiated panels two ways: regular panels are cached by prefab instance id (so the
    /// same prefab maps to one runtime instance), while <see cref="PanelController.useOnce"/> panels
    /// get a single dedicated cache slot, replaced on each <see cref="CreatePanel{T}"/> call.
    /// </remarks>
    public abstract class PanelStack : MonoBehaviour
    {
        /// <summary>How a new panel relates to the current top.</summary>
        public enum PushMode
        {
            /// <summary>Layer the new panel above the current one (current stays in stack).</summary>
            OnTop,
            /// <summary>Replace the current top — pops then pushes.</summary>
            Replacement,
            /// <summary>Queue the panel for after the stack drains.</summary>
            Queued,
        }

        protected Stack<PanelController> panelStack = new();
        private Dictionary<int, PanelController> m_cachedOnceUsePanels = new();
        private Dictionary<int, PanelController> m_createdPanels = new();

        /// <summary>The enclosing stack, when this stack is nested inside a <see cref="PanelController"/>. <c>null</c> for the root.</summary>
        public PanelStack ParentPanel { get; private set; }

        /// <summary>The panel currently at the top of the stack (the visible one), or <c>null</c> if the stack is empty.</summary>
        public virtual PanelController TopPanel => panelStack != null && panelStack.Count > 0 ? panelStack.Peek() : null;

        /// <summary>Index of this stack within its parent's stack — 0 for the bottom-most, parent's count for an unparented stack.</summary>
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

        /// <summary>Z-order in the parent stack — top of stack returns 1, deeper panels return higher numbers.</summary>
        public int DisplayOrder
        {
            get
            {
                if (ParentPanel == null)
                    return 1;
                return ParentPanel.panelStack.Count - Index;
            }
        }

        /// <summary>Number of panels currently in this stack.</summary>
        public int StackCount => panelStack?.Count ?? 0;

        protected virtual void Awake()
        {
            if (ParentPanel == null)
                ParentPanel = GetComponentInParent<PanelStack>();
            if (ParentPanel == this)
                ParentPanel = null;
        }

        /// <summary>
        /// Returns a runtime instance for <paramref name="pPanel"/>, instantiating from the prefab on
        /// first request and caching for reuse. Reassigns <paramref name="pPanel"/> by reference to
        /// the cached instance so callers don't have to maintain prefab vs instance distinction.
        /// <see cref="PanelController.useOnce"/> panels are instantiated fresh on every call.
        /// </summary>
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

        /// <summary>Walks up the nested stacks to return the outermost <see cref="PanelStack"/>.</summary>
        public PanelStack GetRootPanel()
        {
            return ParentPanel != null ? ParentPanel.GetRootPanel() : this;
        }

        /// <summary>Walks down through top panels to return the deepest visible <see cref="PanelStack"/>.</summary>
        public PanelStack GetHighestPanel()
        {
            return TopPanel != null ? TopPanel.GetHighestPanel() : this;
        }

        /// <summary>
        /// Pushes <paramref name="pPanel"/>, optionally keeping the current top in the stack
        /// (<paramref name="keepCurrentInStack"/>). Returns the resolved instance.
        /// </summary>
        public virtual T PushPanel<T>(ref T pPanel, bool keepCurrentInStack, bool onlyInactivePanel = true, bool instantPopAndPush = true) where T : PanelController
        {
            var panel = CreatePanel(ref pPanel);
            PushPanel(panel, keepCurrentInStack, onlyInactivePanel, instantPopAndPush);
            return panel;
        }

        /// <summary>
        /// Concrete-instance push (no prefab resolution). The current top is hidden first; when its
        /// hide animation completes, the new panel is shown. <paramref name="instantPopAndPush"/>
        /// controls whether the show happens immediately or after the hide.
        /// </summary>
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

        /// <summary>
        /// Pushes <paramref name="pPanel"/> above the current top without hiding the current top
        /// (overlay mode). When <paramref name="hidePusher"/> is true, the current top is hidden by
        /// the parent stack instead.
        /// </summary>
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

        /// <summary>Clears the entire stack, then pushes <paramref name="panel"/>. Locked panels are preserved.</summary>
        public void PopAllThenPush(PanelController panel)
        {
            PopAllPanels();
            PushPanel(panel, false);
        }

        /// <summary>Pops every panel except the bottom one. Locked panels are preserved in their original order.</summary>
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

        /// <summary>Pops panels off the top until — and including — <paramref name="panel"/> has been popped.</summary>
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

        /// <summary>Pops panels off the top until <paramref name="panel"/> is at the top (does not pop <paramref name="panel"/> itself).</summary>
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

        /// <summary>Pops every panel off the stack. Locked panels remain (pushed back in their original order).</summary>
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

        /// <summary>Recursive pop — pops the deepest nested top first, working back up to this stack.</summary>
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
