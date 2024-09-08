/***
 * Author RadBear - nbhung71711@gmail.com - 2017
 **/

#pragma warning disable 0649
using System;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using RCore.Common;

namespace RCore.Framework.UI
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

		internal Action onWillShow;
		internal Action onWillHide;
		internal Action onDidShow;
		internal Action onDidHide;

		protected bool mShowed;
		private bool mIsShowing;
		private bool mIsHiding;

		/// <summary>
		/// When panel is lock, any action pop from itself or its parent will be restricted
		/// Note: in one moment, there should be no-more one locked child
		/// </summary>
		private bool mIsLock;

		private CanvasGroup mCanvasGroup;
		private Canvas mCanvas;

		internal CanvasGroup CanvasGroup
		{
			get
			{
				if (mCanvasGroup == null)
				{
#if UNITY_2019_2_OR_NEWER
					TryGetComponent(out mCanvasGroup);
#else
                    mCanvasGroup = GetComponent<CanvasGroup>();
#endif
					if (mCanvasGroup == null)
						mCanvasGroup = gameObject.AddComponent<CanvasGroup>();
				}
				return mCanvasGroup;
			}
		}
		/// <summary>
		/// Optional, in case we need to control sorting order
		/// </summary>
		internal Canvas Canvas
		{
			get
			{
				if (mCanvas == null)
				{
#if UNITY_2019_2_OR_NEWER
					TryGetComponent(out GraphicRaycaster rayCaster);
#else
                    var rayCaster = GetComponent<GraphicRaycaster>();
#endif
					if (rayCaster == null)
						rayCaster = gameObject.AddComponent<GraphicRaycaster>();

#if UNITY_2019_2_OR_NEWER
					TryGetComponent(out mCanvas);
#else
                    mCanvas = gameObject.GetComponent<Canvas>();
#endif
					if (mCanvas == null)
						mCanvas = gameObject.AddComponent<Canvas>();

					//WaitUtil.Enqueue(() => { mCanvas.overrideSorting = true; }); //Quick-fix
				}
				return mCanvas;
			}
		}

		internal bool Displayed => mShowed || mIsShowing;
		internal bool Transiting => mIsShowing || mIsHiding;
		internal bool IsLock => mIsLock;

#endregion

		//=================================

#region Hide

		internal virtual void Hide(UnityAction OnDidHide = null)
		{
			if (!mShowed || mIsHiding)
			{
				Log(name + " Panel is hidden");
				return;
			}

			CoroutineUtil.StartCoroutine(IE_Hide(OnDidHide));
		}

		protected IEnumerator IE_Hide(UnityAction pOnDidHide)
		{
			mIsHiding = true;

			onWillHide?.Invoke();

			BeforeHiding();
			LockWhileTransiting(true);

			//Wait till there is no sub active panel
			while (panelStack.Count > 0)
			{
				var subPanel = panelStack.Pop();
				subPanel.Hide();

				if (panelStack.Count == 0)
					yield return new WaitUntil(() => !subPanel.mShowed);
				else if (!subPanel.enableFXTransition)
					yield return null;
			}

			PopAllPanels();

			if (enableFXTransition)
				yield return CoroutineUtil.StartCoroutine(IE_HideFX());
			else
				yield return null;

			mIsHiding = false;
			mShowed = false;
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

		internal virtual void Show(UnityAction pOnDidShow = null)
		{
			if (mShowed || mIsShowing)
			{
				Log(name + " Panel showed");
				return;
			}

			if (transform.parent != mParentPanel.transform)
				transform.SetParent(mParentPanel.transform);

			CoroutineUtil.StartCoroutine(IE_Show(pOnDidShow));
		}

		protected IEnumerator IE_Show(UnityAction pOnDidShow)
		{
			mIsShowing = true;

			onWillShow?.Invoke();

			BeforeShowing();
			LockWhileTransiting(true);

			//Make the shown panel on the top of all other siblings
			transform.SetAsLastSibling();

			SetActivePanel(true);
			if (enableFXTransition)
				yield return CoroutineUtil.StartCoroutine(IE_ShowFX());
			else
				yield return null;

			mIsShowing = false;
			mShowed = true;

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
				CanvasGroup.interactable = !Transiting;
		}

		public virtual void Back()
		{
			if (mParentPanel == null)
			{
				if (TopPanel != null)
					TopPanel.Back();
				else
					LogError("There is nothing for Back");
			}
			else
				//parentPanel.PopPanel();
				mParentPanel.PopChildrenThenParent();
		}

		protected virtual void BtnBack_Pressed()
		{
			Back();
		}

		internal bool CanPop()
		{
			foreach (var p in panelStack)
			{
				if (p.mIsLock || p.Transiting)
					return false;
			}
			if (mIsLock || Transiting)
				return false;
			return true;
		}

		internal virtual void Init() { }

		internal void Lock(bool pLock)
		{
			mIsLock = pLock;
		}

		public bool IsActiveAndEnabled()
		{
			return !gameObject.IsPrefab() && isActiveAndEnabled;
		}

#endregion

		//======================================================

#if UNITY_EDITOR
		[CustomEditor(typeof(PanelController), true)]
		public class PanelControllerEditor : UnityEditor.Editor
		{
			private PanelController mScript;

			protected virtual void OnEnable()
			{
				mScript = target as PanelController;
			}

			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				EditorGUILayout.Space();
				EditorGUILayout.BeginVertical("box");
				EditorGUILayout.LabelField("Children Count: " + mScript.StackCount, EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Index: " + mScript.Index, EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Display Order: " + mScript.PanelOrder, EditorStyles.boldLabel);
				if (mScript.GetComponent<Canvas>() != null)
					GUILayout.Label("NOTE: sub-panel should not have Canvas component!\nIt should be inherited from parent panel");

				EditorGUILayout.BeginVertical("box");
				EditorGUILayout.LabelField(mScript.TopPanel == null ? $"TopPanel: Null" : $"TopPanel: {mScript.TopPanel.name}");
				ShowChildrenList(mScript, 0);
				EditorGUILayout.EndVertical();

				EditorGUILayout.EndVertical();
			}

			private void ShowChildrenList(PanelController panel, int p_LevelIndent)
			{
				int levelIndent = EditorGUI.indentLevel;
				EditorGUI.indentLevel = p_LevelIndent;
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