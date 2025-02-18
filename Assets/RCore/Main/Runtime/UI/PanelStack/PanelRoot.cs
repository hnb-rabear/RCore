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
	public abstract class PanelRoot : PanelStack
	{
		[SerializeField] protected Button m_dimmerOverlay;

		private readonly List<PanelController> m_panelsInQueue = new List<PanelController>();

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

		private void OnValidate()
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
		}

		protected override void OnAnyChildShow(PanelController pPanel)
		{
			base.OnAnyChildShow(pPanel);

			ToggleDimmerOverlay();
		}

		private void ToggleDimmerOverlay()
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
			if (pPanel == null) return null;
			if (TopPanel == pPanel) return pPanel;
			if (StackCount == 0)
			{
				var popup = PushPanelToTop(ref pPanel);
				return popup;
			}
			var popupInQueue = CreatePanel(ref pPanel);
			if (!m_panelsInQueue.Contains(pPanel))
				m_panelsInQueue.Add(pPanel);
			return popupInQueue;
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
			fullScreenImage.color = Color.black.SetAlpha(0.9f);

			var rectTransform = fullScreenImage.GetComponent<RectTransform>();
			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.one;
			rectTransform.offsetMin = Vector2.zero;
			rectTransform.offsetMax = Vector2.zero;

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
				var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

				object panelInstance = null;

				foreach (var field in fields)
					if (field.FieldType.FullName == e.panelType)
					{
						panelInstance = field.GetValue(this);
						break;
					}

				if (panelInstance != null && panelInstance is PanelController panelController)
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

		private void RequestPanelHandler(RequestPanelEvent e)
		{
			if (e.rootType != GetType().FullName)
				return;
			e.panel = OnReceivedPanelRequest(e.panelType, e.value);
		}

		protected abstract PanelController OnReceivedPanelRequest(string panelTypeFullName, object value);

		//======================================================

		public static T PushOuterPanel<T>(Type root, T pPanel, PushMode pPushMode = PushMode.OnTop, bool pKeepCurrentAndReplace = true) where T : PanelController
		{
			var @event = new PushPanelEvent(root, pPanel, pPushMode, pKeepCurrentAndReplace);
			EventDispatcher.Raise(@event);
			return @event.panel as T;
		}

		public static T PushInternalPanel<T>(Type root, Type pPanel, PushMode pPushMode = PushMode.OnTop, bool pKeepCurrentAndReplace = true) where T : PanelController
		{
			var @event = new PushPanelEvent(root, pPanel, pPushMode, pKeepCurrentAndReplace);
			EventDispatcher.Raise(@event);
			return @event.panel as T;
		}

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