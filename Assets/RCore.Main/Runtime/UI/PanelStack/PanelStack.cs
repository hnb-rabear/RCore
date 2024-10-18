/**
 * Author RadBear - nbhung71711@gmail.com - 2017
 **/

using System.Collections.Generic;
using UnityEngine;
using RCore.Common;
using Debug = UnityEngine.Debug;

namespace RCore.UI
{
	public class PanelStack : MonoBehaviour
	{
		protected Stack<PanelController> panelStack = new Stack<PanelController>();
		private Dictionary<int, PanelController> m_cachedOnceUsePanels = new Dictionary<int, PanelController>();

		internal PanelStack parentPanel;
		/// <summary>
		/// Top child
		/// </summary>
		public PanelController TopPanel => panelStack != null && panelStack.Count > 0 ? panelStack.Peek() : null;
		/// <summary>
		/// Index in stack
		/// </summary>
		public int Index
		{
			get
			{
				if (parentPanel == null)
					return 0;
				int i = 0;
				foreach (var p in parentPanel.panelStack)
				{
					if (p == this)
						return i;
					i++;
				}
				return parentPanel.panelStack.Count;
			}
		}
		/// <summary>
		/// Order base-on active sibling
		/// </summary>
		public int PanelOrder
		{
			get
			{
				if (parentPanel == null)
					return 1;
				return parentPanel.panelStack.Count - Index;
			}
		}
		/// <summary>
		/// Total children panels
		/// </summary>
		public int StackCount => panelStack?.Count ?? 0;

		protected virtual void Awake()
		{
			if (parentPanel == null)
				parentPanel = GetComponentInParent<PanelController>();
			if (parentPanel == this)
				parentPanel = null;
		}

		//=============================================================

#region Create

		/// <summary>
		/// Create and init panel
		/// </summary>
		/// <typeparam name="T">Panels inherit PanelController</typeparam>
		/// <param name="pPanel">Can be prefab or buildin prefab</param>
		/// <returns></returns>
		public T CreatePanel<T>(ref T pPanel) where T : PanelController
		{
			if (pPanel == null)
				return null;

			if (!pPanel.useOnce)
			{
				if (pPanel.gameObject.IsPrefab())
				{
					string name = pPanel.name;
					pPanel = Instantiate(pPanel, transform);
					pPanel.SetActive(false);
					pPanel.Init();
					pPanel.name = name;
				}
				return pPanel;
			}
			if (!pPanel.gameObject.IsPrefab())
				Debug.LogWarning("Once used panel must be prefab!");

			string name2 = pPanel.name;
			var panel = Instantiate(pPanel, transform);
			panel.useOnce = true;
			panel.SetActive(false);
			panel.Init();
			panel.name = name2;

			if (!m_cachedOnceUsePanels.ContainsKey(pPanel.GetInstanceID()))
				m_cachedOnceUsePanels.Add(pPanel.GetInstanceID(), panel);
			else
				m_cachedOnceUsePanels[pPanel.GetInstanceID()] = panel;

			return panel;
		}

		/// <summary>
		/// Find child panel of this Panel
		/// </summary>
		/// <typeparam name="T">Panels inherit PanelController</typeparam>
		/// <param name="pOriginal">Can be prefab or buildin prefab</param>
		/// <returns></returns>
		protected T GetCachedPanel<T>(T pOriginal) where T : PanelController
		{
			if (pOriginal == null) return null;
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
			return parentPanel != null ? parentPanel.GetRootPanel() : this;
		}

		public PanelStack GetHighestPanel()
		{
			return TopPanel != null ? TopPanel.GetHighestPanel() : this;
		}

#endregion

		//=============================================================

#region Single

		/// <summary>
		/// Check if panel is prefab or build-in prefab then create and init
		/// </summary>
		public T PushPanel<T>(ref T pPanel, bool keepCurrentInStack, bool onlyInactivePanel = true, bool instantPopAndPush = true) where T : PanelController
		{
			var panel = CreatePanel(ref pPanel);
			PushPanel(panel, keepCurrentInStack, onlyInactivePanel, instantPopAndPush);
			return panel;
		}

		/// <summary>
		/// Push new panel will hide the current top panel
		/// </summary>
		/// <param name="panel">New Top Panel</param>
		/// <param name="onlyDisablePanel">Do nothing if panel is currently active</param>
		/// <param name="instantPopAndPush">Allow pop current panel and push new </param>
		public void PushPanel(PanelController panel, bool keepCurrentInStack, bool onlyInactivePanel = true, bool instantPopAndPush = true)
		{
			if (panel == null)
			{
				Log($"Panel is null");
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
				//If top panel is locked we must keep it
				Log($"{blockingPanel.name} can't hide now!");
				PushPanelToTop(panel);
				return;
			}

			panel.parentPanel = this;
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

					// Hide all current panels
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

		/// <summary>
		/// Pop the top panel off the stack and show the one beneath it
		/// </summary>
		public void PopPanel(bool instant = true)
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
				var newPanel = this.TopPanel;
				if (newPanel != null && !newPanel.Displayed)
				{
					newPanel.Show();
					newPanel.OnReshow();
					OnAnyChildShow(newPanel);
				}
			}
		}

		/// <summary>
		/// Check if panel is prefab or build-in prefab then create and init
		/// </summary>
		public virtual T PushPanelToTop<T>(ref T pPanel) where T : PanelController
		{
			var panel = CreatePanel(ref pPanel);
			PushPanelToTop(panel);
			return panel;
		}

		/// <summary>
		/// Push panel without hiding panel is under it
		/// </summary>
		public virtual void PushPanelToTop(PanelController panel)
		{
			if (TopPanel == panel && TopPanel.Displayed)
				return;

			panelStack.Push(panel);
			panel.parentPanel = this;
			panel.Show();
			OnAnyChildShow(panel);
		}

#endregion

		//=============================================================

#region Multi

		/// <summary>
		/// Keep only one panel in stack
		/// </summary>
		public void PopAllThenPush(PanelController panel)
		{
			PopAllPanels();
			PushPanel(panel, false);
		}

		/// <summary>
		/// Pop all panels till there is only one panel left in the stack
		/// </summary>
		public void PopTillOneLeft()
		{
			var lockedPanels = new List<PanelController>();
			PanelController lastTopPanel = null;
			while (panelStack.Count > 1)
			{
				lastTopPanel = panelStack.Pop();
				if (lastTopPanel.IsLocked())
					lockedPanels.Add(lastTopPanel); //Locked panel should not be hide
				else
					lastTopPanel.Hide();
			}

			//Resign every locked panels, because we removed them temporarily above
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

		/// <summary>
		/// Pop till we remove specific panel
		/// </summary>
		public void PopTillNoPanel(PanelController panel)
		{
			if (!panelStack.Contains(panel))
			{
				Log($"Panel {name} doesn't contain panel {panel.name}");
				return;
			}

			var lockedPanels = new List<PanelController>();
			PanelController lastTopPanel;

			//Pop panels until we find the right one we're trying to pop
			do
			{
				lastTopPanel = panelStack.Pop();
				if (lastTopPanel.IsLocked())
					lockedPanels.Add(lastTopPanel); //Locked panel should not be hide
				else
					lastTopPanel.Hide();

			} while (lastTopPanel.GetInstanceID() != panel.GetInstanceID() && panelStack.Count > 0);

			//Resign every locked panels, because we removed them temporarily above
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
					lockedPanels.Add(curTopPanel); //Locked panel should not be hide
				else
					curTopPanel.Hide();
			}

			//Resign every locked panels, because we removed them temporarily above
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

		/// <summary>
		/// Pop and hide all panels in stack, at the same time
		/// </summary>
		public void PopAllPanels()
		{
			var lockedPanels = new List<PanelController>();
			PanelController lastTopPanel = null;
			while (panelStack.Count > 0)
			{
				lastTopPanel = panelStack.Pop();
				if (lastTopPanel.IsLocked())
					lockedPanels.Add(lastTopPanel); //Locked panel should not be hide
				else
					lastTopPanel.Hide();
			}

			//Resign every locked panel, because we removed them temporarily above
			if (lockedPanels.Count > 0)
			{
				for (int i = lockedPanels.Count - 1; i >= 0; i--)
					panelStack.Push(lockedPanels[i]);
			}

			if (lastTopPanel != null)
				OnAnyChildHide(lastTopPanel);
		}

		/// <summary>
		/// Pop one by one, children then parent
		/// </summary>
		public void PopChildrenThenParent()
		{
			if (TopPanel == null)
				return;

			if (TopPanel.TopPanel != null)
				TopPanel.PopChildrenThenParent();
			else
				PopPanel();
		}

#endregion

		//==============================================================

		protected virtual void OnAnyChildHide(PanelController pPanel)
		{
			//Parent notifies to grandparent of hidden panel
			if (parentPanel != null)
				parentPanel.OnAnyChildHide(pPanel);
		}
		protected virtual void OnAnyChildShow(PanelController pPanel)
		{
			if (parentPanel != null)
				parentPanel.OnAnyChildShow(pPanel);
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