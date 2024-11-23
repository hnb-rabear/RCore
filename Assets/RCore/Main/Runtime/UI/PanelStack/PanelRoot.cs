using System.Collections.Generic;
#if UNITY_EDITOR
using RCore.Editor;
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
			EventDispatcher.AddListener<PushPanelToTopEvent>(OnPushPanelToTop);
			EventDispatcher.AddListener<PushPanelEvent>(OnPushPanel);
			EventDispatcher.AddListener<PushPanelToQueueEvent>(OnPushPanelToQueue);
			EventDispatcher.AddListener<PushInterPanelEvent>(OnPushInterPanel);
		}

		protected virtual void OnDisable()
		{
			EventDispatcher.RemoveListener<PushPanelToTopEvent>(OnPushPanelToTop);
			EventDispatcher.RemoveListener<PushPanelEvent>(OnPushPanel);
			EventDispatcher.RemoveListener<PushPanelToQueueEvent>(OnPushPanelToQueue);
			EventDispatcher.RemoveListener<PushInterPanelEvent>(OnPushInterPanel);
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

		protected void OnPushPanelToTop(PushPanelToTopEvent e)
		{
			if (e.rootType != GetType().Name)
				return;
			PushPanelToTop(ref e.panel);
		}
		
		
		private void OnPushPanel(PushPanelEvent e)
		{
			if (e.rootType != GetType().Name)
				return;
			PushPanel(ref e.panel, e.keepCurrentInStack);
		}
		
		private void OnPushPanelToQueue(PushPanelToQueueEvent e)
		{
			if (e.rootType != GetType().Name)
				return;
			AddPanelToQueue(ref e.panel);
		}
		
		protected abstract void OnPushInterPanel()
		
		//======================================================

#if UNITY_EDITOR
		[CustomEditor(typeof(PanelRoot), true)]
		public class PanelRootEditor : PanelStackEditor { }
#endif
	}
	
	//======================================================
	
	public class PushPanelToTopEvent : BaseEvent
	{
		public string rootType;
		public PanelController panel;
		public PushPanelToTopEvent(System.Type root, PanelController pPanel, object pValue = null)
		{
			rootType = root.Name;
			panel = pPanel;
		}
	}
	
	public class PushPanelEvent : PushPanelToTopEvent
	{
		public bool keepCurrentInStack;
		public PushPanelEvent(System.Type root, PanelController pPanel, object pValue = null) : base(root, pPanel, pValue) { }
	}
	
	public class PushPanelToQueueEvent : PushPanelToTopEvent
	{
		public PushPanelToQueueEvent(System.Type root, PanelController pPanel, object pValue = null) : base(root, pPanel, pValue) { }
	}

	public class PushInterPanelEvent : BaseEvent
	{
		public string rootType;
		public string panelType;
		public PanelStack.PushType pushType;
		public bool keepCurrentAndReplace;
		public PushInterPanelEvent(System.Type root, System.Type pPanel)
		{
			rootType = root.Name;
			panelType = pPanel.Name;
		}
	}
}