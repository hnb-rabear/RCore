/**
 * Author RadBear - nbhung71711@gmail.com - 2017
 **/

#pragma warning disable 0649
using System;
using System.Collections;
#if UNITY_EDITOR
using RCore.Editor;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using RCore.Common;

namespace RCore.UI
{
	public class PanelController : PanelStack
	{
		//==============================

#region Members

		[Tooltip("Set True if this panel is prefab and rarely use in game")]
		public bool useOnce = false;

		[Tooltip("Enable it and override IE_HideFX and IE_ShowFX")]
		public bool enableFXTransition = false;

		[Tooltip("For optimization")]
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
		private Canvas m_canvas;

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
		/// <summary>
		/// Optional, in case we need to control sorting order
		/// </summary>
		public Canvas Canvas
		{
			get
			{
				if (m_canvas == null)
				{
#if UNITY_2019_2_OR_NEWER
					TryGetComponent(out GraphicRaycaster rayCaster);
#else
                    var rayCaster = GetComponent<GraphicRaycaster>();
#endif
					if (rayCaster == null)
						rayCaster = gameObject.AddComponent<GraphicRaycaster>();

#if UNITY_2019_2_OR_NEWER
					TryGetComponent(out m_canvas);
#else
                    mCanvas = gameObject.GetComponent<Canvas>();
#endif
					if (m_canvas == null)
						m_canvas = gameObject.AddComponent<Canvas>();

					//WaitUtil.Enqueue(() => { mCanvas.overrideSorting = true; }); //Quick-fix
				}
				return m_canvas;
			}
		}

		public bool Displayed => m_showed || m_isShowing;
		public bool Transiting => m_isShowing || m_isHiding;

#endregion

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

		protected virtual IEnumerator IE_HideFX() { yield break; }

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

		protected virtual IEnumerator IE_ShowFX() { yield break; }

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

#region Monobehaviour

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

#endregion

		//======================================================

#region Methods

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
				else
					LogError("There is nothing for Back");
			}
			else
				//parentPanel.PopPanel();
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

		public bool IsSubsidiary() => parentPanel.parentPanel != null;

		public bool Contains<T>(T popupConvertCoin) where T : PanelController
		{
			return panelStack.Contains(popupConvertCoin);
		}
		
#endregion

		//======================================================

#if UNITY_EDITOR
		[CustomEditor(typeof(PanelController), true)]
		public class PanelControllerEditor : UnityEditor.Editor
		{
			private PanelController m_script;

			protected virtual void OnEnable()
			{
				m_script = target as PanelController;
			}

			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				EditorGUILayout.Space();
				EditorGUILayout.BeginVertical("box");
				EditorGUILayout.LabelField("Children Count: " + m_script.StackCount, EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Index: " + m_script.Index, EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Display Order: " + m_script.PanelOrder, EditorStyles.boldLabel);
				if (m_script.GetComponent<Canvas>() != null)
					GUILayout.Label("NOTE: sub-panel should not have Canvas component!\nIt should be inherited from parent panel");

				EditorGUILayout.BeginVertical("box");
				EditorGUILayout.LabelField(m_script.TopPanel == null ? $"TopPanel: Null" : $"TopPanel: {m_script.TopPanel.name}");
				ShowChildrenList(m_script, 0);
				EditorGUILayout.EndVertical();

				EditorGUILayout.EndVertical();
			}

			private void ShowChildrenList(PanelController panel, int pLevelIndent)
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