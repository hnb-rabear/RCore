/**
 * Author HNB-RaBear - 2017
 **/

using System;
using System.Collections;
#if UNITY_EDITOR
using RCore.Editor;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RCore.UI
{
	public class PanelController : PanelStack
	{
		[Tooltip("Set True if this panel is prefab and rarely use in game")]
		public bool useOnce;
		[Tooltip("Enable it and override IE_HideFX and IE_ShowFX")]
		public bool enableFXTransition = false;
		public bool nested = true;
		public Button btnBack;

		public Action onWillShow;
		public Action onWillHide;
		public Action onDidShow;
		public Action onDidHide;

		protected bool m_showed;
		protected bool m_isShowing;
		protected bool m_isHiding;

		/// <summary>
		/// When panel is lock, any action pop from itself or its parent will be restricted
		/// Note: in one moment, there should be no-more one locked child
		/// </summary>
		private bool m_locked;

		private CanvasGroup m_canvasGroup;
		private static readonly int m_AnimClose = Animator.StringToHash("close");
		private static readonly int m_AnimOpen = Animator.StringToHash("open");

		public CanvasGroup CanvasGroup
		{
			get
			{
				if (m_canvasGroup == null)
				{
#if UNITY_2019_2_OR_NEWER
					TryGetComponent(out m_canvasGroup);
#else
                    mCanvasGroup = GetComponent<CanvasGroup>();
#endif
					if (m_canvasGroup == null)
						m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
				}
				return m_canvasGroup;
			}
		}

		public bool Displayed => m_showed || m_isShowing;
		public bool Transiting => m_isShowing || m_isHiding;

		protected override void Awake()
		{
			base.Awake();

			if (btnBack != null)
				btnBack.onClick.AddListener(BtnBack_Pressed);
		}

		protected virtual void OnDisable()
		{
			LockWhileTransiting(false);
		}

		protected virtual void OnValidate()
		{
			if (Application.isPlaying)
				return;
			if (nested)
			{
				var canvas = gameObject.GetComponent<Canvas>();
				if (canvas == null)
					gameObject.AddComponent<Canvas>();
				var graphicRaycaster = gameObject.GetComponent<GraphicRaycaster>();
				if (graphicRaycaster == null)
					gameObject.AddComponent<GraphicRaycaster>();
			}
		}
		
		//=================================

#region Hide

		public void Hide(UnityAction pOnDidHide = null)
		{
			if (!m_showed || m_isHiding)
				return;
			
			TimerEventsInScene.Instance.StartCoroutine(IE_Hide(pOnDidHide));
		}

		protected IEnumerator IE_Hide(UnityAction pOnDidHide)
		{
			m_isHiding = true;
			
			onWillHide?.Invoke();

			BeforeHiding();
			LockWhileTransiting(true);

			//Wait till there is no sub active panel
			while (panelStack.Count > 0)
			{
				var subPanel = panelStack.Pop();
				yield return subPanel.IE_Hide(null);
			}

			PopAllPanels();

			if (enableFXTransition)
				yield return IE_HideFX();
			else
				yield return null;

			m_isHiding = false;
			m_showed = false;
			SetActivePanel(false);

			LockWhileTransiting(false);
			AfterHiding();
			if (useOnce)
				Destroy(gameObject, 0.1f);

			pOnDidHide?.Invoke();
			onDidHide?.Invoke();
		}

		protected virtual IEnumerator IE_HideFX()
		{
			if (TryGetComponent(out Animator animator) && animator.parameters.Exists(x => x.name == "close"))
			{
				animator.SetTrigger(m_AnimClose);
				yield return null;
				var info = animator.GetCurrentAnimatorStateInfo(0);
				yield return new WaitForSecondsRealtime(info.length - Time.deltaTime);
			}
		}

		protected virtual void BeforeHiding() { }

		protected virtual void AfterHiding() { }

#endregion

		//==================================

#region Show

		public void Show(UnityAction pOnDidShow = null)
		{
			if (m_showed || m_isShowing)
				return;

			if (transform.parent != parentPanel.transform)
				transform.SetParent(parentPanel.transform);

			TimerEventsInScene.Instance.StartCoroutine(IE_Show(pOnDidShow));
		}

		protected IEnumerator IE_Show(UnityAction pOnDidShow)
		{
			m_isShowing = true;

			onWillShow?.Invoke();

			BeforeShowing();
			LockWhileTransiting(true);

			//Make the shown panel on the top of all other siblings
			transform.SetAsLastSibling();

			SetActivePanel(true);
			if (enableFXTransition)
				yield return IE_ShowFX();
			else
				yield return null;

			m_isShowing = false;
			m_showed = true;

			LockWhileTransiting(false);
			AfterShowing();

			pOnDidShow?.Invoke();
			onDidShow?.Invoke();
		}

		protected virtual IEnumerator IE_ShowFX()
		{
			if (TryGetComponent(out Animator animator) && animator.parameters.Exists(x => x.name == "open"))
			{
				animator.SetTrigger(m_AnimOpen);
				yield return null;
				var info = animator.GetCurrentAnimatorStateInfo(0);
				yield return new WaitForSecondsRealtime(info.length - Time.deltaTime);
			}
		}

		protected virtual void BeforeShowing() { }

		protected virtual void AfterShowing() { }

		protected virtual void SetActivePanel(bool pValue)
		{
			gameObject.SetActive(pValue);
		}

		/// <summary>
		/// When the panel above it is hidden, the panel that is hidden in the stack is called to reappear
		/// </summary>
		public virtual void OnReshow() { }
		
#endregion

		//===================================

		private void LockWhileTransiting(bool value)
		{
			if (enableFXTransition)
			{
				if (CanvasGroup != null)
					CanvasGroup.interactable = !Transiting;
			}
		}

		public virtual void Back()
		{
			if (parentPanel == null)
			{
				if (TopPanel != null)
					TopPanel.Back();
			}
			else
				parentPanel.PopChildrenThenParent();
		}

		protected virtual void BtnBack_Pressed()
		{
			Back();
		}

		public bool CanPop(out PanelController blockingPanel)
		{
			blockingPanel = null;
			foreach (var p in panelStack)
			{
				if (p.m_locked || p.Transiting)
				{
					blockingPanel = p;
					return false;
				}
			}
			if (m_locked || Transiting)
			{
				blockingPanel = this;
				return false;
			}
			return true;
		}

		public virtual void Init() { }

		public void Lock(bool pLock)
		{
			m_locked = pLock;
		}

		public bool IsActiveAndEnabled()
		{
			return !gameObject.IsPrefab() && isActiveAndEnabled;
		}

		public bool IsLocked()
		{
			if (TopPanel == null)
				return m_locked;
			foreach (var panelController in panelStack)
				if (panelController.IsLocked())
					return true;
			return false;
		}

		public bool IsChildPanel() => parentPanel.parentPanel != null;

		public bool Contains<T>(T popupConvertCoin) where T : PanelController
		{
			return panelStack.Contains(popupConvertCoin);
		}

		//======================================================

#if UNITY_EDITOR
		[CustomEditor(typeof(PanelController), true)]
		public class PanelControllerEditor : PanelStackEditor
		{
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				// Create a layer to block user taps on DimmerOverlay, preventing the panel from closing
				if (EditorHelper.Button("Add a layer that blocks DimmerOverlay"))
				{
					var img = m_script.gameObject.GetOrAddComponent<Image>();
					if (img.color != Color.clear)
						img.color = Color.clear;
				}
				if (EditorHelper.Button("Add Transition Animation"))
				{
					var animator = m_script.gameObject.GetOrAddComponent<Animator>();
					if (animator.runtimeAnimatorController == null)
					{
						string animatorCtrlPath = AssetDatabase.GUIDToAssetPath("7d5f83b914c1a9b4ca7b5203f47e50c0");
						if (!string.IsNullOrEmpty(animatorCtrlPath))
						{
							var animatorCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(animatorCtrlPath);
							if (animatorCtrl != null)
							{
								animator.runtimeAnimatorController = animatorCtrl;
								animator.gameObject.GetOrAddComponent<CanvasGroup>();
							}
						}
					}
					((PanelController)target).enableFXTransition = true;
					EditorUtility.SetDirty(m_script);
				}
			}
		}
#endif
	}
}