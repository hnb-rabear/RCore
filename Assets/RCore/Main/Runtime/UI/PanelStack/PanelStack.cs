/**
 * Author HNB-RaBear - 2017
 **/

#if UNITY_EDITOR
using RCore.Editor;
#endif
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RCore.UI
{
	/// <summary>
	/// Manages a stack of UI panels, handling their creation, display, and transitions.
	/// </summary>
	public abstract class PanelStack : MonoBehaviour
	{
		/// <summary>
		/// Defines how a new panel is added to the stack.
		/// </summary>
		public enum PushMode
		{
			/// <summary>
			/// Adds the new panel on top of the current one, without hiding the one below.
			/// </summary>
			OnTop,
			/// <summary>
			/// Replaces the current top panel with the new one.
			/// </summary>
			Replacement,
			/// <summary>
			/// The new panel is queued and will be displayed after the current one is closed.
			/// </summary>
			Queued,
		}

		/// <summary>
		/// The stack that holds the panels.
		/// </summary>
		protected Stack<PanelController> panelStack = new();
		/// <summary>
		/// The parent PanelStack of this instance.
		/// </summary>
		public PanelStack ParentPanel { get; private set; }

		/// <summary>
		/// Caches panels that are marked for single use.
		/// </summary>
		private Dictionary<int, PanelController> m_cachedOnceUsePanels = new Dictionary<int, PanelController>();

		/// <summary>
		/// Gets the panel at the top of the stack.
		/// </summary>
		public virtual PanelController TopPanel => panelStack != null && panelStack.Count > 0 ? panelStack.Peek() : null;
		
		/// <summary>
		/// The index of this PanelStack within its parent's stack.
		/// </summary>
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

		/// <summary>
		/// The display order based on its sibling position.
		/// </summary>
		public int DisplayOrder
		{
			get
			{
				if (ParentPanel == null)
					return 1;
				return ParentPanel.panelStack.Count - Index;
			}
		}

		/// <summary>
		/// The total number of child panels in the stack.
		/// </summary>
		public int StackCount => panelStack?.Count ?? 0;

		/// <summary>
		/// Initializes the PanelStack, finding its parent if one exists.
		/// </summary>
		protected virtual void Awake()
		{
			if (ParentPanel == null)
				ParentPanel = GetComponentInParent<PanelStack>();
			if (ParentPanel == this)
				ParentPanel = null;
		}

		//=============================================================

#region Create

		/// <summary>
		/// Caches created panels.
		/// </summary>
		private Dictionary<int, PanelController> m_createdPanels = new();

		/// <summary>
		/// Creates and initializes a panel.
		/// </summary>
		/// <typeparam name="T">The type of the panel, inheriting from PanelController.</typeparam>
		/// <param name="pPanel">The panel prefab to instantiate.</param>
		/// <returns>The created panel.</returns>
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

		/// <summary>
		/// Retrieves a cached panel.
		/// </summary>
		/// <typeparam name="T">The type of the panel.</typeparam>
		/// <param name="pOriginal">The original panel prefab.</param>
		/// <returns>The cached panel instance.</returns>
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

		/// <summary>
		/// Gets the root PanelStack in the hierarchy.
		/// </summary>
		/// <returns>The root PanelStack.</returns>
		public PanelStack GetRootPanel()
		{
			return ParentPanel != null ? ParentPanel.GetRootPanel() : this;
		}

		/// <summary>
		/// Gets the highest (topmost) PanelStack in the hierarchy.
		/// </summary>
		/// <returns>The highest PanelStack.</returns>
		public PanelStack GetHighestPanel()
		{
			return TopPanel != null ? TopPanel.GetHighestPanel() : this;
		}

#endregion

		//=============================================================

#region Single

		/// <summary>
		/// Creates and pushes a panel onto the stack.
		/// </summary>
		/// <typeparam name="T">The type of the panel.</typeparam>
		/// <param name="pPanel">The panel prefab.</param>
		/// <param name="keepCurrentInStack">If true, the current top panel is kept in the stack but hidden.</param>
		/// <param name="onlyInactivePanel">If true, the panel is only pushed if it's not already active.</param>
		/// <param name="instantPopAndPush">If true, the transition happens instantly.</param>
		/// <returns>The pushed panel.</returns>
		public virtual T PushPanel<T>(ref T pPanel, bool keepCurrentInStack, bool onlyInactivePanel = true, bool instantPopAndPush = true) where T : PanelController
		{
			var panel = CreatePanel(ref pPanel);
			PushPanel(panel, keepCurrentInStack, onlyInactivePanel, instantPopAndPush);
			return panel;
		}

		/// <summary>
		/// Pushes a new panel onto the stack, hiding the current top panel.
		/// </summary>
		/// <param name="panel">The new panel to become the top panel.</param>
		/// <param name="keepCurrentInStack">If true, the current panel remains in the stack but is hidden.</param>
		/// <param name="onlyInactivePanel">If true, does nothing if the panel is already active.</param>
		/// <param name="instantPopAndPush">If true, pops the current panel and pushes the new one instantly.</param>
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
		/// Pops the top panel from the stack and shows the one beneath it.
		/// </summary>
		/// <param name="instant">If true, the transition is instant.</param>
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
		/// Creates and pushes a panel to the top without hiding the panel below it.
		/// </summary>
		/// <typeparam name="T">The type of the panel.</typeparam>
		/// <param name="pPanel">The panel prefab.</param>
		/// <param name="hidePusher">If true, the panel that pushed this one will be hidden.</param>
		/// <returns>The pushed panel.</returns>
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

		/// <summary>
		/// Pushes a panel to the top without hiding the panel below it.
		/// </summary>
		/// <param name="panel">The panel to push.</param>
		protected virtual void PushPanelToTop(PanelController panel)
		{
			if (TopPanel == panel && TopPanel.Displayed)
				return;

			panelStack.Push(panel);
			panel.ParentPanel = this;
			panel.Show();
			OnAnyChildShow(panel);
		}

#endregion

		//=============================================================

#region Multi

		/// <summary>
		/// Pops all panels and then pushes a new one, leaving only one panel in the stack.
		/// </summary>
		/// <param name="panel">The panel to push.</param>
		public void PopAllThenPush(PanelController panel)
		{
			PopAllPanels();
			PushPanel(panel, false);
		}

		/// <summary>
		/// Pops panels until only one is left in the stack.
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
		/// Pops panels until a specific panel is removed from the stack.
		/// </summary>
		/// <param name="panel">The panel to pop to.</param>
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

		/// <summary>
		/// Pops panels until a specific panel is at the top of the stack.
		/// </summary>
		/// <param name="panel">The panel to pop to.</param>
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
		/// Pops and hides all panels in the stack simultaneously.
		/// </summary>
		public virtual void PopAllPanels()
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
		/// Pops panels one by one, starting from the children and moving to the parent.
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

		/// <summary>
		/// Called when any child panel is hidden.
		/// </summary>
		/// <param name="pPanel">The panel that was hidden.</param>
		protected virtual void OnAnyChildHide(PanelController pPanel)
		{
			//Parent notifies to grandparent of hidden panel
			if (ParentPanel != null)
				ParentPanel.OnAnyChildHide(pPanel);
		}
		
		/// <summary>
		/// Called when any child panel is shown.
		/// </summary>
		/// <param name="pPanel">The panel that was shown.</param>
		protected virtual void OnAnyChildShow(PanelController pPanel)
		{
			if (ParentPanel != null)
				ParentPanel.OnAnyChildShow(pPanel);
		}

		/// <summary>
		/// Logs a message to the console (Editor only).
		/// </summary>
		/// <param name="pMessage">The message to log.</param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		protected void Log(string pMessage)
		{
			Debug.Log($"<color=yellow><b>[{gameObject.name}]:</b></color>{pMessage}");
		}

		/// <summary>
		/// Logs an error message to the console (Editor only).
		/// </summary>
		/// <param name="pMessage">The error message to log.</param>
		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		protected void LogError(string pMessage)
		{
			Debug.LogError($"<color=red><b>[{gameObject.name}]:</b></color>{pMessage}");
		}

		//==============================================================

#if UNITY_EDITOR
		[CustomEditor(typeof(PanelStack), true)]
#if ODIN_INSPECTOR
		public class PanelStackEditor : Sirenix.OdinInspector.Editor.OdinEditor
		{
			protected PanelStack m_script;

			protected override void OnEnable()
			{
				base.OnEnable();

				m_script = target as PanelStack;
			}
#else
		/// <summary>
		/// Custom editor for the PanelStack component.
		/// </summary>
		public class PanelStackEditor : UnityEditor.Editor
		{
			protected PanelStack m_script;

			protected virtual void OnEnable()
			{
				m_script = target as PanelStack;
			}
#endif
			/// <summary>
			/// Draws the custom inspector GUI.
			/// </summary>
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				EditorGUILayout.Space();
				EditorGUILayout.BeginVertical("box");
				EditorGUILayout.LabelField("Children Count: " + m_script.StackCount, EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Index: " + m_script.Index, EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Display Order: " + m_script.DisplayOrder, EditorStyles.boldLabel);
				if (m_script.GetComponent<Canvas>() != null)
					GUILayout.Label("NOTE: sub-panel should not have Canvas component!\nIt should be inherited from parent panel");

				EditorGUILayout.BeginVertical("box");
				EditorGUILayout.LabelField(m_script.TopPanel == null ? $"TopPanel: Null" : $"TopPanel: {m_script.TopPanel.name}");
				ShowChildrenList(m_script, 0);
				EditorGUILayout.EndVertical();

				EditorGUILayout.EndVertical();
			}

			/// <summary>
			/// Recursively displays the list of child panels.
			/// </summary>
			/// <param name="panel">The parent panel.</param>
			/// <param name="pLevelIndent">The indentation level.</param>
			private void ShowChildrenList(PanelStack panel, int pLevelIndent)
			{
				if (panel.panelStack == null)
					return;
				int levelIndent = EditorGUI.indentLevel;
				EditorGUI.indentLevel = pLevelIndent;
				foreach (var p in panel.panelStack)
				{
					if (EditorHelper.ButtonColor($"{p.Index}: {p.name}", ColorHelper.LightAzure))
						Selection.activeObject = p.gameObject;
					if (p.StackCount > 0)
					{
						EditorGUI.indentLevel++;
						levelIndent = EditorGUI.indentLevel;
						ShowChildrenList(p, levelIndent);
					}
				}
			}
		}
#endif
	}
}