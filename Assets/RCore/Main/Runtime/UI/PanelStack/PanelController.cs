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
	/// <summary>
	/// Represents an individual UI panel that can be managed within a PanelStack.
	/// It handles its own lifecycle, including showing, hiding, and animations.
	/// </summary>
	public class PanelController : PanelStack
	{
		[Tooltip("Set True if this panel is a prefab and rarely used, causing it to be destroyed after hiding.")]
		public bool useOnce;
		[Tooltip("Enable this to use custom transition effects via IE_HideFX and IE_ShowFX coroutines.")]
		public bool enableFXTransition = false;
		[Tooltip("If true, ensures the panel has its own Canvas and GraphicRaycaster.")]
		public bool nested = true;
		[Tooltip("Optional button to trigger the Back() action.")]
		public Button btnBack;

		/// <summary>
		/// Action invoked just before the panel starts its showing transition.
		/// </summary>
		public Action onWillShow;
		/// <summary>
		/// Action invoked just before the panel starts its hiding transition.
		/// </summary>
		public Action onWillHide;
		/// <summary>
		/// Action invoked after the panel has completed its showing transition.
		/// </summary>
		public Action onDidShow;
		/// <summary>
		/// Action invoked after the panel has completed its hiding transition.
		/// </summary>
		public Action onDidHide;

		protected bool m_showed;
		protected bool m_isShowing;
		protected bool m_isHiding;

		/// <summary>
		/// When a panel is locked, any pop action from itself or its parent is restricted.
		/// Note: There should be at most one locked child panel at any given time.
		/// </summary>
		private bool m_locked;

		private CanvasGroup m_canvasGroup;
		private static readonly int m_AnimClose = Animator.StringToHash("close");
		private static readonly int m_AnimOpen = Animator.StringToHash("open");

		/// <summary>
		/// Gets the CanvasGroup component attached to this panel.
		/// A CanvasGroup is added automatically if one doesn't exist.
		/// </summary>
		public CanvasGroup CanvasGroup
		{
			get
			{
				if (m_canvasGroup == null)
				{
#if UNITY_2019_2_OR_NEWER
					TryGetComponent(out m_canvasGroup);
#else
                    m_canvasGroup = GetComponent<CanvasGroup>();
#endif
					if (m_canvasGroup == null)
						m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
				}
				return m_canvasGroup;
			}
		}

		/// <summary>
		/// Returns true if the panel is currently displayed or in the process of being shown.
		/// </summary>
		public bool Displayed => m_showed || m_isShowing;
		/// <summary>
		/// Returns true if the panel is currently in a showing or hiding transition.
		/// </summary>
		public bool Transiting => m_isShowing || m_isHiding;

		protected override void Awake()
		{
			base.Awake();

			if (btnBack != null)
				btnBack.onClick.AddListener(BtnBack_Pressed);
		}

		protected virtual void OnDisable()
		{
			// Ensure the panel is not locked when disabled unexpectedly.
			LockWhileTransiting(false);
		}

		/// <summary>
		/// Editor-only method to validate component setup.
		/// Ensures nested panels have a Canvas and GraphicRaycaster, and sets Animator to unscaled time.
		/// </summary>
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
			if (TryGetComponent(out Animator animator))
				animator.updateMode = AnimatorUpdateMode.UnscaledTime;
		}

		//=================================

#region Hide

		/// <summary>
		/// Hides the panel.
		/// </summary>
		/// <param name="pOnDidHide">An action to be invoked after the panel is hidden.</param>
		public void Hide(UnityAction pOnDidHide = null)
		{
			if (!m_showed || m_isHiding)
				return;

			TimerEventsInScene.Instance.StartCoroutine(IE_Hide(pOnDidHide));
		}

		/// <summary>
		/// Coroutine that handles the hiding process of the panel.
		/// </summary>
		/// <param name="pOnDidHide">Callback action after hiding is complete.</param>
		protected IEnumerator IE_Hide(UnityAction pOnDidHide)
		{
			m_isHiding = true;

			onWillHide?.Invoke();

			BeforeHiding();
			LockWhileTransiting(true);

			// Wait until all sub-panels are hidden
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

		/// <summary>
		/// Coroutine for playing the hiding animation. Can be overridden for custom effects.
		/// </summary>
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

		/// <summary>
		/// Virtual method called before the hiding process begins.
		/// </summary>
		protected virtual void BeforeHiding() { }

		/// <summary>
		/// Virtual method called after the hiding process is complete.
		/// </summary>
		protected virtual void AfterHiding() { }

#endregion

		//==================================

#region Show

		/// <summary>
		/// Shows the panel.
		/// </summary>
		/// <param name="pOnDidShow">An action to be invoked after the panel is shown.</param>
		public void Show(UnityAction pOnDidShow = null)
		{
			if (m_showed || m_isShowing)
				return;

			if (transform.parent != ParentPanel.transform)
				transform.SetParent(ParentPanel.transform);

			TimerEventsInScene.Instance.StartCoroutine(IE_Show(pOnDidShow));
		}

		/// <summary>
		/// Coroutine that handles the showing process of the panel.
		/// </summary>
		/// <param name="pOnDidShow">Callback action after showing is complete.</param>
		protected IEnumerator IE_Show(UnityAction pOnDidShow)
		{
			m_isShowing = true;

			onWillShow?.Invoke();

			BeforeShowing();
			LockWhileTransiting(true);

			// Ensure the panel is rendered on top of its siblings.
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

		/// <summary>
		/// Coroutine for playing the showing animation. Can be overridden for custom effects.
		/// </summary>
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

		/// <summary>
		/// Virtual method called before the showing process begins.
		/// </summary>
		protected virtual void BeforeShowing() { }

		/// <summary>
		/// Virtual method called after the showing process is complete.
		/// </summary>
		protected virtual void AfterShowing() { }

		/// <summary>
		/// Activates or deactivates the panel's GameObject.
		/// </summary>
		/// <param name="pValue">True to activate, false to deactivate.</param>
		protected virtual void SetActivePanel(bool pValue)
		{
			gameObject.SetActive(pValue);
		}

		/// <summary>
		/// Called when a panel above this one is hidden, making this panel visible again.
		/// </summary>
		public virtual void OnReshow() { }

#endregion

		//===================================

		/// <summary>
		/// Locks the panel's interactability during transitions if FX are enabled.
		/// </summary>
		private void LockWhileTransiting(bool value)
		{
			if (enableFXTransition)
			{
				if (CanvasGroup != null)
					CanvasGroup.interactable = !value;
			}
		}

		/// <summary>
		/// Triggers the back action, typically popping the current panel or its children.
		/// </summary>
		public virtual void Back()
		{
			if (ParentPanel == null)
			{
				if (TopPanel != null)
					TopPanel.Back();
			}
			else
				ParentPanel.PopChildrenThenParent();
		}

		/// <summary>
		/// Handles the back button press event.
		/// </summary>
		protected virtual void BtnBack_Pressed()
		{
			Back();
		}

		/// <summary>
		/// Checks if this panel or any of its sub-panels can be popped.
		/// </summary>
		/// <param name="blockingPanel">The panel that is preventing the pop action (if any).</param>
		/// <returns>True if the panel can be popped, false otherwise.</returns>
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

		/// <summary>
		/// Virtual method for custom initialization logic. Called during panel creation.
		/// </summary>
		public virtual void Init() { }

		/// <summary>
		/// Locks or unlocks the panel, preventing it from being popped.
		/// </summary>
		/// <param name="pLock">True to lock, false to unlock.</param>
		public void Lock(bool pLock)
		{
			m_locked = pLock;
		}

		/// <summary>
		/// Checks if the panel's GameObject is active in the hierarchy and not a prefab.
		/// </summary>
		public bool IsActiveAndEnabled()
		{
			return !gameObject.IsPrefab() && isActiveAndEnabled;
		}

		/// <summary>
		/// Checks if this panel or any of its children are locked.
		/// </summary>
		/// <returns>True if any panel in the hierarchy is locked.</returns>
		public bool IsLocked()
		{
			if (TopPanel == null)
				return m_locked;
			foreach (var panelController in panelStack)
				if (panelController.IsLocked())
					return true;
			return false;
		}

		/// <summary>
		/// Determines if this panel is a child of another panel (not a root panel).
		/// </summary>
		public bool IsChildPanel() => ParentPanel.ParentPanel != null;

		/// <summary>
		/// Checks if a specific panel is a direct child in this panel's stack.
		/// </summary>
		public bool Contains<T>(T panel) where T : PanelController
		{
			return panelStack.Contains(panel);
		}

		//======================================================

#if UNITY_EDITOR
		[CustomEditor(typeof(PanelController), true)]
		public class PanelControllerEditor : PanelStackEditor
		{
			/// <summary>
			/// Draws the custom inspector GUI for PanelController.
			/// </summary>
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				if (EditorHelper.Button("Add Blocker Image"))
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
						// GUID for a default animator controller.
						//7d5f83b914c1a9b4ca7b5203f47e50c0 Open-ZoomOut, Close-ZoomIn
						//aacc2936d5462ba48a06864252b2704e Open-ZoomIn, Close-ZoomOut
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