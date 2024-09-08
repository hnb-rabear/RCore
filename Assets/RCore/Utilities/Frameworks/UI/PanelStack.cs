/***
* Author RadBear - nbhung71711@gmail.com - 2017
**/
#if USE_DOTWEEN
using DG.Tweening;
#endif
using System.Collections.Generic;
using UnityEngine;
using RCore.Common;
using Debug = UnityEngine.Debug;

namespace RCore.Framework.UI
{
	public class TransitionFX
	{
		public PanelController panel;
		public Vector2 mDefaultAnchoredPosition;

		public TransitionFX(PanelController pPanel, Vector2 defaultPosition)
		{
			panel = pPanel;
			mDefaultAnchoredPosition = defaultPosition;
		}

		public void TransitLeftToMid(float pTime)
		{
			var rootPanel = panel.GetRootPanel();
			var screenWidth = ((RectTransform)rootPanel.transform).rect.width;
			var panelRect = panel.transform as RectTransform;
			var moveFrom = -(screenWidth / 2f + panelRect.rect.width * (1 - panelRect.pivot.x));
			var moveTo = mDefaultAnchoredPosition.x;
			panelRect.SetX(moveFrom);
#if USE_DOTWEEN
            panelRect.DOLocalMoveX(moveTo, pTime);
#else
			panelRect.SetX(moveTo);
#endif
		}

		public void TransitMidToRight(float pTime)
		{
			var rootPanel = panel.GetRootPanel();
			var screenWidth = ((RectTransform)rootPanel.transform).rect.width;
			var panelRect = panel.transform as RectTransform;
			var moveFrom = mDefaultAnchoredPosition.x;
			var moveTo = screenWidth / 2f + panelRect.rect.width * (1 - panelRect.pivot.x);
			panelRect.SetX(moveFrom);
#if USE_DOTWEEN
            panelRect.DOLocalMoveX(moveTo, pTime);
#else
			panelRect.SetX(moveTo);
#endif
		}

		public void Fade(float pFrom, float pTo, float pTime)
		{
#if USE_DOTWEEN
            panel.CanvasGroup.alpha = pFrom;
            panel.CanvasGroup.DOFade(pTo, pTime);
#else
			panel.CanvasGroup.alpha = 1;
#endif
		}
	}

	public class PanelStack : MonoBehaviour
	{
		protected Stack<PanelController> panelStack = new Stack<PanelController>();
		protected Dictionary<int, PanelController> m_cachedOnceUsePanels = new Dictionary<int, PanelController>();

		protected PanelStack mParentPanel;
		internal PanelStack ParentPanel => mParentPanel;

		/// <summary>
		/// Top child
		/// </summary>
		public PanelController TopPanel
		{
			get
			{
				if (panelStack.Count > 0)
					return panelStack.Peek();
				return null;
			}
		}
		/// <summary>
		/// Index in stack
		/// </summary>
		public int Index
		{
			get
			{
				if (mParentPanel == null)
					return 0;
				int i = 0;
				foreach (var p in mParentPanel.panelStack)
				{
					if (p == this)
						return i;
					i++;
				}
				return mParentPanel.panelStack.Count;
			}
		}
		/// <summary>
		/// Order base-on active sibling
		/// </summary>
		public int PanelOrder
		{
			get
			{
				if (mParentPanel == null)
					return 1;
				return mParentPanel.panelStack.Count - Index;
			}
		}
		/// <summary>
		/// Total children panels
		/// </summary>
		public int StackCount => panelStack.Count;

		protected virtual void Awake()
		{
			if (mParentPanel == null)
				mParentPanel = GetComponentInParent<PanelController>();
			if (mParentPanel == this)
				mParentPanel = null;
		}

		//=============================================================

#region Create

		/// <summary>
		/// Create and init panel
		/// </summary>
		/// <typeparam name="T">Panels inherit PanelController</typeparam>
		/// <param name="pPanel">Can be prefab or built-in prefab</param>
		/// <returns></returns>
		protected T CreatePanel<T>(ref T pPanel) where T : PanelController
		{
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
				return pPanel as T;
			}
			{
				if (!pPanel.gameObject.IsPrefab())
					Debug.LogWarning("Once used panel must be prefab!");

				string name = pPanel.name;
				var panel = Instantiate(pPanel, transform);
				panel.useOnce = true;
				panel.SetActive(false);
				panel.Init();
				panel.name = name;

				if (!m_cachedOnceUsePanels.ContainsKey(pPanel.GetInstanceID()))
					m_cachedOnceUsePanels.Add(pPanel.GetInstanceID(), panel);
				else
					m_cachedOnceUsePanels[pPanel.GetInstanceID()] = panel;

				return panel;
			}
		}

		/// <summary>
		/// Find child panel of this Panel
		/// </summary>
		/// <typeparam name="T">Panels inherit PanelController</typeparam>
		/// <param name="pOriginal">Can be prefab or built-in prefab</param>
		/// <returns></returns>
		protected T GetCachedPanel<T>(T pOriginal) where T : PanelController
		{
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
			return mParentPanel != null ? mParentPanel.GetRootPanel() : this;
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
		internal virtual T PushPanel<T>(ref T pPanel, bool keepCurrentInStack, bool onlyInactivePanel = true, bool sameTimePopAndPush = true) where T : PanelController
		{
			var panel = CreatePanel(ref pPanel);
			PushPanel(panel, keepCurrentInStack, onlyInactivePanel, sameTimePopAndPush);
			return panel;
		}

		/// <summary>
		/// Push new panel will hide the current top panel
		/// </summary>
		/// <param name="panel">New Top Panel</param>
		/// <param name="onlyInactivePanel">Do nothing if panel is currently active</param>
		/// <param name="sameTimePopAndPush">Allow pop current panel and push new </param>
		internal virtual void PushPanel(PanelController panel, bool keepCurrentInStack, bool onlyInactivePanel = true, bool sameTimePopAndPush = true)
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

			if (TopPanel != null && !TopPanel.CanPop())
			{
				//If top panel is locked we must keep it
				PushPanelToTop(panel);
				return;
			}

			panel.mParentPanel = this;
			if (TopPanel != null)
			{
				var currentTopPanel = TopPanel;
				if (currentTopPanel.Displayed)
				{
					currentTopPanel.Hide(() =>
					{
						if (!sameTimePopAndPush)
						{
							if (!keepCurrentInStack)
								panelStack.Pop();
							panelStack.Push(panel);
							panel.Show();
							OnAnyChildShow(panel);
						}

						OnAnyChildHide(currentTopPanel);
					});

					if (sameTimePopAndPush)
					{
						if (!keepCurrentInStack)
							panelStack.Pop();
						panelStack.Push(panel);
						panel.Show();
						OnAnyChildShow(panel);
					}
				}
				else
				{
					if (!keepCurrentInStack)
						panelStack.Pop();
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
		internal virtual void PopPanel(bool actionSameTime = true)
		{
			if (TopPanel == null)
			{
				Log("Parent Panel is null");
				return;
			}

			if (TopPanel != null && !TopPanel.CanPop())
			{
				Log($"Current Parent panel {TopPanel.name} is locked");
				return;
			}

			var topStack = panelStack.Pop();
			if (topStack.Displayed)
			{
				topStack.Hide(() =>
				{
					if (!actionSameTime)
					{
						var newPanel = TopPanel;
						if (newPanel != null && !newPanel.Displayed)
						{
							newPanel.Show();
							OnAnyChildShow(newPanel);
						}
					}

					OnAnyChildHide(topStack);
				});

				if (actionSameTime)
				{
					var newPanel = TopPanel;
					if (newPanel != null && !newPanel.Displayed)
					{
						newPanel.Show();
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
					OnAnyChildShow(newPanel);
				}
			}
		}

		/// <summary>
		/// Check if panel is prefab or build-in prefab then create and init
		/// </summary>
		internal virtual T PushPanelToTop<T>(ref T pPanel) where T : PanelController
		{
			var panel = CreatePanel(ref pPanel);
			PushPanelToTop(panel);
			return panel;
		}

		/// <summary>
		/// Push panel without hiding panel is under it
		/// </summary>
		internal virtual void PushPanelToTop(PanelController panel)
		{
			if (TopPanel == panel)
				return;

			panelStack.Push(panel);
			panel.mParentPanel = this;
			panel.Show();
			OnAnyChildShow(panel);
		}

#endregion

		//=============================================================

#region Multi

		/// <summary>
		/// Keep only one panel in stack
		/// </summary>
		internal virtual void PopAllThenPush(PanelController panel)
		{
			PopAllPanels();
			PushPanel(panel, false);
		}

		/// <summary>
		/// Pop all panels till there is only one panel left in the stack
		/// </summary>
		internal virtual void PopTillOneLeft()
		{
			var lockedPanels = new List<PanelController>();
			PanelController oldTopPanel = null;
			while (panelStack.Count > 1)
			{
				oldTopPanel = panelStack.Pop();
				if (!oldTopPanel.CanPop())
					//Locked panel should not be hide
					lockedPanels.Add(oldTopPanel);
				else
					oldTopPanel.Hide();
			}

			//Resign every locked panels, because we removed them temporary above
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

			if (oldTopPanel != null)
				OnAnyChildHide(oldTopPanel);
		}

		/// <summary>
		/// Pop till we remove specific panel
		/// </summary>
		internal virtual void PopTillNoPanel(PanelController panel)
		{
			if (!panelStack.Contains(panel))
			{
				Log($"Panel {name} doesn't contain panel {panel.name}");
				return;
			}

			var lockedPanels = new List<PanelController>();
			PanelController oldTopPanel;

			//Pop panels until we find the right one we're trying to pop
			do
			{
				oldTopPanel = panelStack.Pop();
				if (!oldTopPanel.CanPop())
					//Locked panel should not be hide
					lockedPanels.Add(oldTopPanel);
				else
					oldTopPanel.Hide();
			} while (oldTopPanel.GetInstanceID() != panel.GetInstanceID() && panelStack.Count > 0);

			//Resign every locked panels, because we removed them temporary above
			if (lockedPanels.Count > 0)
			{
				for (int i = lockedPanels.Count - 1; i >= 0; i--)
					panelStack.Push(lockedPanels[i]);
			}

			var newPanel = TopPanel;
			if (newPanel != null && !newPanel.Displayed)
			{
				newPanel.Show();
				OnAnyChildShow(newPanel);
			}

			if (oldTopPanel != null)
				OnAnyChildHide(oldTopPanel);
		}

		internal virtual void PopTillPanel(PanelController panel)
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
				if (!curTopPanel.CanPop())
					//Locked panel should not be hide
					lockedPanels.Add(curTopPanel);
				else
					curTopPanel.Hide();
			}

			//Resign every locked panels, because we removed them temporary above
			if (lockedPanels.Count > 0)
			{
				for (int i = lockedPanels.Count - 1; i >= 0; i--)
					panelStack.Push(lockedPanels[i]);
			}

			var newPanel = TopPanel;
			if (newPanel != null && !newPanel.Displayed)
			{
				newPanel.Show();
				OnAnyChildShow(newPanel);
			}

			if (curTopPanel != null)
				OnAnyChildHide(curTopPanel);
		}

		/// <summary>
		/// Pop and hide all panels in stack, at the same time
		/// </summary>
		internal virtual void PopAllPanels()
		{
			var lockedPanels = new List<PanelController>();
			PanelController oldTopPanel = null;
			while (panelStack.Count > 0)
			{
				oldTopPanel = panelStack.Pop();
				if (!oldTopPanel.CanPop())
					//Locked panel should not be hide
					lockedPanels.Add(oldTopPanel);
				else
					oldTopPanel.Hide();
			}

			//Resign every locked panel, because we removed them temporary above
			if (lockedPanels.Count > 0)
			{
				for (int i = lockedPanels.Count - 1; i >= 0; i--)
					panelStack.Push(lockedPanels[i]);
			}

			if (oldTopPanel != null)
				OnAnyChildHide(oldTopPanel);
		}

		/// <summary>
		/// Pop one by one, chilren then parent
		/// </summary>
		internal virtual void PopChildrenThenParent()
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
			if (mParentPanel != null)
				mParentPanel.OnAnyChildHide(pPanel);
		}
		protected virtual void OnAnyChildShow(PanelController pPanel)
		{
			if (mParentPanel != null)
				mParentPanel.OnAnyChildShow(pPanel);
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		protected void Log(string pMessage)
		{
			Debug.Log(string.Format("<color=yellow><b>[{1}]:</b></color>{1}", gameObject.name, pMessage));
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		protected void LogError(string pMessage)
		{
			Debug.LogError(string.Format("<color=red><b>[{1}]:</b></color>{1}", gameObject.name, pMessage));
		}
	}
}