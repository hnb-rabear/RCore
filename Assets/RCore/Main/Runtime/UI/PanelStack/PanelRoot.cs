using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace RCore.UI
{
	public class PanelRoot : PanelStack
	{
		private readonly List<PanelController> m_panelsInQueue = new List<PanelController>();
		
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
			
			PushPanelInQueue();
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
			if (m_panelsInQueue.Count <= 0)
				return;
			if (IsBusy())
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
			return StackCount == 0;
		}
		
		//======================================================
		
#if UNITY_EDITOR
		[CustomEditor(typeof(PanelRoot), true)]
		public class PanelRootEditor : PanelStackEditor { }
#endif
	}
}