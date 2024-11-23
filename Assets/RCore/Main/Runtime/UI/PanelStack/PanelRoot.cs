using System;
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
		[SerializeField] private Button m_dimmerOverlay;

		private readonly List<PanelController> m_panelsInQueue = new List<PanelController>();

		protected virtual void OnEnable()
		{
			EventDispatcher.AddListener<PushOuterPanelEvent>(OnPushOuterPanel);
			EventDispatcher.AddListener<PushInterPanelEvent>(OnPushInterPanel);
			EventDispatcher.AddListener<RequestPanelPushEvent>(OnRequestPanelPush);
		}

		protected virtual void OnDisable()
		{
			EventDispatcher.RemoveListener<PushOuterPanelEvent>(OnPushOuterPanel);
			EventDispatcher.RemoveListener<PushInterPanelEvent>(OnPushInterPanel);
			EventDispatcher.RemoveListener<RequestPanelPushEvent>(OnRequestPanelPush);
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

			if (!PushPanelInQueue())
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
				m_dimmerOverlay.SetActive(true);
				m_dimmerOverlay.transform.SetParent(highestPanel.transform.parent);
				m_dimmerOverlay.transform.SetAsLastSibling();
				highestPanel.transform.SetAsLastSibling();
			}
			else
			{
				m_dimmerOverlay.SetActive(false);
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

		public virtual bool PushPanelInQueue()
		{
			if (m_panelsInQueue.Count <= 0 || IsBusy())
				return false;
			var panel = m_panelsInQueue[0];
			m_panelsInQueue.RemoveAt(0);
			PushPanelToTop(ref panel);
			return true;
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
			return StackCount == 0;
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

		private void OnPushOuterPanel(PushOuterPanelEvent e)
		{
			if (e.rootType != GetType().FullName)
				return;
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

		private void OnPushInterPanel(PushInterPanelEvent e)
		{
			if (e.rootType != GetType().FullName)
				return;
			
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
		
		private void OnRequestPanelPush(RequestPanelPushEvent e)
		{
			if (e.rootType != GetType().FullName)
				return;
			OnRequestPanelPush(e.panelType, e.value);
		}

		protected abstract void OnRequestPanelPush(string panelTypeFullName, object eValue);
		
		//======================================================

#if UNITY_EDITOR
		[CustomEditor(typeof(PanelRoot), true)]
		public class PanelRootEditor : PanelStackEditor { }
#endif
	}
	
	//======================================================
	
	/// <summary>
	/// Example of dispatching an event:
	/// EventDispatcher.Raise(new PushOuterPanelEvent(typeof(PanelHome), m_panelSettings));
	/// </summary>
	public class PushOuterPanelEvent : BaseEvent
	{
		public string rootType;
		public PanelController panel;
		public PanelStack.PushMode pushMode;
		public bool keepCurrentAndReplace;
		public PushOuterPanelEvent(Type root, PanelController pPanel, PanelStack.PushMode pPushMode = PanelStack.PushMode.OnTop, bool pKeepCurrentAndReplace = true)
		{
			rootType = root.FullName;
			panel = pPanel;
			pushMode = pPushMode;
			keepCurrentAndReplace = pKeepCurrentAndReplace;
		}
	}

	/// <summary>
	/// Example of dispatching an event:
	/// EventDispatcher.Raise(new PushInterPanelEvent(typeof(PanelHome), typeof(PanelSettings)));
	/// </summary>
	public class PushInterPanelEvent : BaseEvent
	{
		public string rootType;
		public string panelType;
		public PanelStack.PushMode pushMode;
		public bool keepCurrentAndReplace;
		public PanelController panel;
		public PushInterPanelEvent(Type root, Type pPanel, PanelStack.PushMode pPushMode = PanelStack.PushMode.OnTop, bool pKeepCurrentAndReplace = true)
		{
			rootType = root.FullName;
			panelType = pPanel.FullName;
			pushMode = pPushMode;
			keepCurrentAndReplace = pKeepCurrentAndReplace;
		}
	}

	/// <summary>
	/// Example of dispatching an event:
	/// EventDispatcher.Raise(new RequestPanelPushEvent(typeof(PanelHome), typeof(PopupRewardChest), rewards));
	/// </summary>
	public class RequestPanelPushEvent : BaseEvent
	{
		public string rootType;
		public string panelType;
		public object value;
		public RequestPanelPushEvent(Type root, Type pPanel, object pValue = null)
		{
			rootType = root.FullName;
			panelType = pPanel.FullName;
			value = pValue;
		}
	}
}