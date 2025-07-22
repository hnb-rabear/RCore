/**
 * Author HNB-RaBear - 2024
 **/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace RCore.UI
{
	/// <summary>
	/// Represents the root of a UI panel system, managing a stack of panels and a queue for pending panels.
	/// It handles panel lifecycle events and provides a global access point for pushing and requesting panels.
	/// </summary>
	public abstract class PanelRoot : PanelStack
	{
		[Tooltip("A button that acts as a background dimmer. When clicked, it typically triggers the back action on the top panel.")]
		[SerializeField] protected Button m_dimmerOverlay;

		/// <summary>
		/// A queue for panels waiting to be displayed.
		/// </summary>
		protected readonly List<PanelController> m_panelsInQueue = new List<PanelController>();

		/// <summary>
		/// A flag to block the processing of the panel queue.
		/// </summary>
		protected bool m_blockQueue;

		protected virtual void OnEnable()
		{
			EventDispatcher.AddListener<PushPanelEvent>(PushPanelHandler);
			EventDispatcher.AddListener<RequestPanelEvent>(RequestPanelHandler);
		}

		protected virtual void OnDisable()
		{
			EventDispatcher.RemoveListener<PushPanelEvent>(PushPanelHandler);
			EventDispatcher.RemoveListener<RequestPanelEvent>(RequestPanelHandler);
		}

		/// <summary>
		/// Ensures that the root GameObject has a Canvas and a GraphicRaycaster component.
		/// </summary>
		protected virtual void OnValidate()
		{
			var canvas = gameObject.GetComponent<Canvas>();
			if (canvas == null)
				gameObject.AddComponent<Canvas>();
			var graphicRaycaster = gameObject.GetComponent<GraphicRaycaster>();
			if (graphicRaycaster == null)
				gameObject.AddComponent<GraphicRaycaster>();
		}

		/// <summary>
		/// Overrides the base method to handle events when a child panel is hidden.
		/// It attempts to push the next panel from the queue and updates the dimmer overlay.
		/// </summary>
		/// <param name="pPanel">The panel that was hidden.</param>
		protected override void OnAnyChildHide(PanelController pPanel)
		{
			base.OnAnyChildHide(pPanel);

			if (m_panelsInQueue.Count > 0 && !IsBusy() && !m_blockQueue)
				PushPanelInQueue();
			else
				ToggleDimmerOverlay();
		}


		/// <summary>
		/// Overrides the base method to handle events when a child panel is shown.
		/// It updates the visibility and position of the dimmer overlay.
		/// </summary>
		/// <param name="pPanel">The panel that was shown.</param>
		protected override void OnAnyChildShow(PanelController pPanel)
		{
			base.OnAnyChildShow(pPanel);

			ToggleDimmerOverlay();
		}

		/// <summary>
		/// Manages the state of the dimmer overlay. It shows the dimmer behind the topmost panel
		/// and hides it when no panels are active.
		/// </summary>
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

		/// <summary>
		/// Adds a panel to the queue. If the stack is empty, the panel is pushed immediately.
		/// </summary>
		/// <typeparam name="T">The type of the panel.</typeparam>
		/// <param name="pPanel">The panel prefab to be queued.</param>
		/// <returns>The instantiated panel that was added to the queue.</returns>
		public virtual T AddPanelToQueue<T>(ref T pPanel) where T : PanelController
		{
			if (pPanel == null) return null;
			if (TopPanel == pPanel) return pPanel;
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

		/// <summary>
		/// Adds an already instantiated panel to the queue.
		/// </summary>
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

		/// <summary>
		/// Pushes the next panel from the queue onto the stack if conditions are met.
		/// </summary>
		public virtual void PushPanelInQueue()
		{
			if (m_panelsInQueue.Count <= 0 || IsBusy() || m_blockQueue)
				return;
			var panel = m_panelsInQueue[0];
			m_panelsInQueue.RemoveAt(0);
			PushPanelToTop(ref panel);
		}

		/// <summary>
		/// Removes a specific panel from the queue.
		/// </summary>
		/// <param name="pPanel">The panel to remove.</param>
		public void RemovePanelInQueue(PanelController pPanel)
		{
			if (m_panelsInQueue != null && pPanel != null)
				m_panelsInQueue.Remove(pPanel);
		}

		/// <summary>
		/// Checks if the panel system is currently busy (i.e., has active panels).
		/// </summary>
		/// <param name="queueInvolved">If true, the check will also consider panels in the queue.</param>
		/// <returns>True if the system is busy, false otherwise.</returns>
		public virtual bool IsBusy(bool queueInvolved = false)
		{
			if (queueInvolved && m_panelsInQueue.Count > 0)
				return true;
			return StackCount > 0;
		}

		/// <summary>
		/// Programmatically creates the dimmer overlay GameObject with an Image and a Button.
		/// </summary>
		/// <returns>The created Button component of the dimmer overlay.</returns>
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
		
		/// <summary>
		/// Handles the PushPanelEvent to show panels based on various modes.
		/// </summary>
		private void PushPanelHandler(PushPanelEvent e)
		{
			if (e.rootType != GetType().FullName)
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
			else if (!string.IsNullOrEmpty(e.panelType))
			{
				// Fallback to find the panel by its type name using reflection
				var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				object panelInstance = null;
				foreach (var field in fields)
				{
					if (field.FieldType.FullName == e.panelType)
					{
						panelInstance = field.GetValue(this);
						break;
					}
				}

				if (panelInstance is PanelController panelController)
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
					Debug.LogError($"Property or field of type {e.panelType} not found in {GetType().Name}.");
			}
		}
		
		/// <summary>
		/// Handles the RequestPanelEvent by delegating to the abstract OnReceivedPanelRequest method.
		/// </summary>
		private void RequestPanelHandler(RequestPanelEvent e)
		{
			if (e.rootType != GetType().FullName)
				return;
			e.panel = OnReceivedPanelRequest(e.panelType, e.value);
		}

		/// <summary>
		/// Abstract method that must be implemented by subclasses to handle panel requests.
		/// This allows for custom logic to retrieve or create a panel.
		/// </summary>
		/// <param name="panelTypeFullName">The full name of the requested panel's type.</param>
		/// <param name="value">An optional value passed with the request.</param>
		/// <returns>The requested PanelController instance.</returns>
		protected abstract PanelController OnReceivedPanelRequest(string panelTypeFullName, object value);

		//======================================================

		/// <summary>
		/// Static helper method to push a panel that is defined outside the root (e.g., a prefab).
		/// </summary>
		public static T PushOuterPanel<T>(Type root, T pPanel, PushMode pPushMode = PushMode.OnTop, bool pKeepCurrentAndReplace = true) where T : PanelController
		{
			var @event = new PushPanelEvent(root, pPanel, pPushMode, pKeepCurrentAndReplace);
			EventDispatcher.Raise(@event);
			return @event.panel as T;
		}
		
		/// <summary>
		/// Static helper method to push a panel that is defined as a field within the root.
		/// </summary>
		public static T PushInternalPanel<T>(Type root, Type pPanel, PushMode pPushMode = PushMode.OnTop, bool pKeepCurrentAndReplace = true) where T : PanelController
		{
			var @event = new PushPanelEvent(root, pPanel, pPushMode, pKeepCurrentAndReplace);
			EventDispatcher.Raise(@event);
			return @event.panel as T;
		}

		/// <summary>
		/// Static helper method to request a panel from a specific root.
		/// </summary>
		public static T RequestPanel<T>(Type root, Type panel, object value) where T : PanelController
		{
			var @event = new RequestPanelEvent(root, panel, value);
			EventDispatcher.Raise(@event);
			return @event.panel as T;
		}

		//======================================================

#if UNITY_EDITOR
		[CustomEditor(typeof(PanelRoot), true)]
		public class PanelRootEditor : PanelStackEditor { }
#endif
	}

	//======================================================
	
	/// <summary>
	/// Event used to request that a panel be pushed onto a PanelRoot's stack.
	/// </summary>
	internal class PushPanelEvent : BaseEvent
	{
		public string rootType;
		public string panelType;
		public PanelController panel;
		public PanelStack.PushMode pushMode;
		public bool keepCurrentAndReplace;
		
		public PushPanelEvent(Type root, PanelController pPanel, PanelStack.PushMode pPushMode = PanelStack.PushMode.OnTop, bool pKeepCurrentAndReplace = true)
		{
			rootType = root.FullName;
			panel = pPanel;
			pushMode = pPushMode;
			keepCurrentAndReplace = pKeepCurrentAndReplace;
		}
		
		public PushPanelEvent(Type root, Type pPanel, PanelStack.PushMode pPushMode = PanelStack.PushMode.OnTop, bool pKeepCurrentAndReplace = true)
		{
			rootType = root.FullName;
			panelType = pPanel.FullName;
			pushMode = pPushMode;
			keepCurrentAndReplace = pKeepCurrentAndReplace;
		}
	}

	/// <summary>
	/// Event used to request a panel instance from a PanelRoot, potentially with some data.
	/// </summary>
	internal class RequestPanelEvent : BaseEvent
	{
		public string rootType;
		public string panelType;
		public object value;
		public PanelController panel;
		
		public RequestPanelEvent(Type root, Type pPanel, object pValue = null)
		{
			rootType = root.FullName;
			panelType = pPanel.FullName;
			value = pValue;
		}
	}
}