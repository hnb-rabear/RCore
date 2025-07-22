// Author - RaBear - 2020

using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using RCore.Inspector;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
#if DOTWEEN
using DG.Tweening;
#endif
#if UNITY_EDITOR
using RCore.Editor;
using UnityEditor;
#endif

namespace RCore.UI
{
	/// <summary>
	/// Manages a horizontal scroll view that snaps to child items.
	/// It handles user input (dragging), automatically finds the nearest item to the center,
	/// and animates the scroll view to focus on that item. It works in conjunction with
	/// SnapScrollItem components on its children.
	/// </summary>
	public class HorizontalSnapScrollView : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
	{
		#region Events

		/// <summary>Invoked when the focused item index changes. Passes the new index as a parameter.</summary>
		public Action<int> onIndexChanged;
		/// <summary>Invoked when a scrolling or snapping animation has finished.</summary>
		public Action onScrollEnd;
		/// <summary>Invoked when the user begins dragging or a snapping animation starts.</summary>
		public Action onScrollStart;

		#endregion

		#region Serialized Fields

		[Tooltip("The item index that the scroll view will focus on at startup.")]
		[SerializeField] private int m_StartIndex;
		[Tooltip("The minimum duration for the snapping animation.")]
		[SerializeField] private float m_MinSpringTime = 0.5f;
		[Tooltip("The maximum duration for the snapping animation.")]
		[SerializeField] private float m_MaxSpringTime = 1f;
		[Tooltip("The scroll velocity (speed) below which the view will automatically start a snap animation.")]
		[SerializeField] private float m_SpringThreshold = 15;
		[Tooltip("If true, the MinScrollReaction will be set automatically based on the item's width.")]
		[SerializeField] private bool m_AutoSetMinScrollReaction = true;
		[Tooltip("The minimum distance the user must drag to trigger a scroll to the next/previous item.")]
		[SerializeField] private float m_MinScrollReaction = 10;
		[Tooltip("A positional offset applied to the final snapped position of an item. Useful for fine-tuning the alignment.")]
		[SerializeField] private Vector2 m_TargetPosOffset;
		[Tooltip("The main ScrollRect component for this view.")]
		[SerializeField] private ScrollRect m_ScrollView;
		[Tooltip("The array of child items to be snapped to. This is populated automatically.")]
		[SerializeField] private SnapScrollItem[] m_Items;
		[Tooltip("Set to TRUE if the items in the hierarchy are ordered from right to left.")]
		[SerializeField] private bool m_ReverseList;
		[Tooltip("A reference RectTransform (usually the center of the viewport) used as the point to measure distance from when finding the nearest item.")]
		[SerializeField] private RectTransform m_PointToCheckDistanceToCenter;

		#endregion

		#region Debug Fields

		[SerializeField, ReadOnly] private float m_ContentAnchoredXMin;
		[SerializeField, ReadOnly] private float m_ContentAnchoredXMax;
		[SerializeField, ReadOnly] private int m_FocusedItemIndex = -1;
		[SerializeField, ReadOnly] private int m_PreviousItemIndex = -1;
		[SerializeField, ReadOnly] private bool m_IsSnapping;
		[SerializeField, ReadOnly] private bool m_IsDragging;
		[SerializeField, ReadOnly] private bool m_Validated;

		#endregion

		#region Private Fields

		private Vector2 m_previousPosition;
		private float m_dragDistance;
		private Vector2 m_velocity;
		private float m_distance;
		private Vector2 m_beginDragPosition;
		private bool m_dragFromLeft;
		private bool m_checkBoundary;
		private bool m_checkStop;

		#endregion

		#region Public Properties

		/// <summary>Gets the RectTransform of the scroll view's content.</summary>
		public RectTransform Content => m_ScrollView.content;
		/// <summary>Gets the index of the item that is currently focused or being snapped to.</summary>
		public int FocusedItemIndex => m_FocusedItemIndex;
		/// <summary>Gets the total number of snap items.</summary>
		public int TotalItems => m_Items.Length;
		/// <summary>Gets a value indicating whether the view is currently executing a snap animation.</summary>
		public bool IsSnapping => m_IsSnapping;
		/// <summary>Gets a value indicating whether the user is currently dragging the scroll view.</summary>
		public bool IsDragging => m_IsDragging;
		/// <summary>Gets the array of all SnapScrollItem components managed by this view.</summary>
		public SnapScrollItem[] Items => m_Items;
		/// <summary>Gets the SnapScrollItem instance that is currently focused.</summary>
		public SnapScrollItem FocusedItem => m_Items[m_FocusedItemIndex];
		
		#endregion

		#region MonoBehaviour

		private void Start()
		{
			// Auto-resize items to fit the viewport width.
			float parentWidth = m_ScrollView.viewport.rect.width;
			foreach (var item in m_Items)
			{
				var itemRectTransform = item.GetComponent<RectTransform>();
				if (itemRectTransform != null)
					itemRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, parentWidth);
			}
			
			Validate();

			m_PreviousItemIndex = m_StartIndex;
			MoveToItem(m_StartIndex, true);
		}

		private void OnEnable()
		{
			m_checkBoundary = m_ScrollView.movementType == ScrollRect.MovementType.Unrestricted;
		}

		private void Update()
		{
			if (!m_Validated || !m_ScrollView.horizontal)
				return;

			// Calculate velocity for snapping logic
			m_velocity = Content.anchoredPosition - m_previousPosition;
			m_previousPosition = Content.anchoredPosition;

			if (m_IsDragging || m_IsSnapping)
				return;

			float speedX = Mathf.Abs(m_velocity.x);
			if (speedX == 0)
			{
				if (m_checkStop)
				{
					// When scrolling stops completely, snap to the nearest item.
					FindNearestItem();
					m_checkStop = false;
				}
				return;
			}
			m_checkStop = true;
			
			// If scrolling slows down enough, initiate a snap.
			if (speedX > 0 && speedX <= m_SpringThreshold)
			{
				FindNearestItem();
				// This logic is complex and seems to predict the next item based on current velocity and position.
				// A simpler approach might just be to snap to the nearest item found.
				// However, retaining original logic:
				int index = m_FocusedItemIndex;
				var targetPos = m_Items[index].RectTransform.CovertAnchoredPosFromChildToParent(m_ScrollView.content);
				if (m_dragFromLeft)
				{
					if (Content.anchoredPosition.x > targetPos.x + m_MinScrollReaction)
						index = m_ReverseList ? index + 1 : index - 1;
				}
				else
				{
					if (Content.anchoredPosition.x < targetPos.x - m_MinScrollReaction)
						index = m_ReverseList ? index - 1 : index + 1;
				}
				index = Mathf.Clamp(index, 0, m_Items.Length - 1);
				SetFocusedIndex(index);
				MoveToFocusedItem(false, speedX);
			}
		}

		private void OnValidate()
		{
			if (Application.isPlaying)
				return;

			Validate();
			if (m_Items != null && m_Items.Length > 0)
			{
				m_FocusedItemIndex = m_Items.Length / 2;
				m_PreviousItemIndex = m_Items.Length / 2;
				MoveToFocusedItem();
			}
		}

		#endregion

		#region Event Handlers

		public void OnBeginDrag(PointerEventData eventData)
		{
			if (!m_ScrollView.horizontal || m_IsSnapping)
				return;

#if DOTWEEN
			DOTween.Kill(GetInstanceID()); // Stop any ongoing snap tweens
#endif
			m_IsDragging = true;
			m_IsSnapping = false;
			m_beginDragPosition = Content.anchoredPosition;
			onScrollStart?.Invoke();
			foreach (var item in m_Items)
				if (item.gameObject.activeSelf)
					item.OnBeginDrag();
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (!m_ScrollView.horizontal) return;
			if (!m_IsDragging)
			{
				OnBeginDrag(eventData); // Ensure drag state is correctly set
			}
			m_IsDragging = true;
			FindNearestItem();

			// Preemptively show adjacent items for a smoother visual experience
			if (m_beginDragPosition.x < Content.anchoredPosition.x)
				m_Items[Mathf.Clamp(m_PreviousItemIndex - 1, 0, m_Items.Length - 1)].Show();
			else if (m_beginDragPosition.x > Content.anchoredPosition.x)
				m_Items[Mathf.Clamp(m_PreviousItemIndex + 1, 0, m_Items.Length - 1)].Show();
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			if (!m_ScrollView.horizontal)
				return;
				
			m_IsDragging = false;
			var endDragPosition = Content.anchoredPosition;
			m_dragDistance = Mathf.Abs(m_beginDragPosition.x - endDragPosition.x);
			m_dragFromLeft = m_beginDragPosition.x < endDragPosition.x;

			// The Update loop will handle the snapping logic from here.
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Moves the scroll view to focus on a specific item instance.
		/// </summary>
		/// <param name="item">The SnapScrollItem to move to.</param>
		/// <param name="pImmediately">If true, the view jumps instantly. If false, it animates.</param>
		public void MoveToItem(SnapScrollItem item, bool pImmediately = false)
		{
			int index = m_Items.IndexOf(item);
			if (index >= 0)
				MoveToItem(index, pImmediately);
		}
		
		/// <summary>
		/// Moves the scroll view to focus on a specific item by its index.
		/// </summary>
		/// <param name="pIndex">The index of the item to move to.</param>
		/// <param name="pImmediately">If true, the view jumps instantly. If false, it animates.</param>
		public void MoveToItem(int pIndex, bool pImmediately = false)
		{
			if (pIndex < 0 || pIndex >= m_Items.Length || !m_Validated)
				return;

#if UNITY_EDITOR
			// Handle editor-time previews
			if (!Application.isPlaying)
			{
				SetFocusedIndex(pIndex);
				MoveToFocusedItem();
				return;
			}
#endif
			SetFocusedIndex(pIndex);
			MoveToFocusedItem(pImmediately, m_SpringThreshold);
		}

		/// <summary>
		/// Calculates the boundaries and populates the item list.
		/// Call this if you dynamically add or remove items at runtime.
		/// </summary>
		public void Validate()
		{
			m_Items = gameObject.GetComponentsInChildren<SnapScrollItem>();
			if (m_ScrollView == null || m_ScrollView.content == null) return;
			
			// Calculate content width and boundaries for snapping
			// This logic assumes a ContentSizeFitter or a fixed-size content panel.
			Content.TryGetComponent(out ContentSizeFitter contentFilter);
			float contentWidth = 0;
			if (contentFilter != null)
			{
				// Complex calculation for layout groups
			}
			else
				contentWidth = Content.rect.width;

			float contentPivotX = Content.pivot.x;
			float viewPortOffsetX = m_ScrollView.viewport.rect.width / 2f;
			// Calculate the min and max anchored X positions the content can have while keeping an item centered.
			m_ContentAnchoredXMin = (contentWidth - contentWidth * contentPivotX - viewPortOffsetX) * -1;
			m_ContentAnchoredXMax = (0 - contentWidth * contentPivotX + viewPortOffsetX) * -1;
			
			m_Validated = true;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Performs the actual movement or animation to center the currently focused item.
		/// </summary>
		private void MoveToFocusedItem(bool pImmediately, float pSpeed)
		{
			m_ScrollView.StopMovement();

			// Calculate the target anchored position for the content panel
			var targetAnchored = m_Items[m_FocusedItemIndex].RectTransform.CovertAnchoredPosFromChildToParent(m_ScrollView.content);
			targetAnchored -= m_TargetPosOffset;
			targetAnchored.x = Mathf.Clamp(targetAnchored.x, m_ContentAnchoredXMin, m_ContentAnchoredXMax);
			
			var contentAnchored = Content.anchoredPosition;
			if (pImmediately)
			{
				Content.anchoredPosition = new Vector2(targetAnchored.x, contentAnchored.y);
				onScrollEnd?.Invoke();
				// Show only the focused item
				for (int i = 0; i < m_Items.Length; i++)
					m_Items[i].gameObject.SetActive(i == m_FocusedItemIndex);
				m_PreviousItemIndex = m_FocusedItemIndex;
			}
			else
			{
				m_IsSnapping = false;

#if DOTWEEN
				m_distance = Vector2.Distance(m_PointToCheckDistanceToCenter.position, m_Items[m_FocusedItemIndex].RectTransform.position);
				float time = (pSpeed > 0) ? (m_distance / (pSpeed / Time.deltaTime)) : m_MinSpringTime;
				time = Mathf.Clamp(time, m_MinSpringTime, m_MaxSpringTime);

				// Hide items that are not relevant to the current snap animation
				bool moveToLeft = Content.anchoredPosition.x < targetAnchored.x;
				for (int i = 0; i < m_Items.Length; i++)
				{
					if ((moveToLeft && i > m_PreviousItemIndex) || (!moveToLeft && i < m_PreviousItemIndex) || (moveToLeft && i < m_FocusedItemIndex) || (!moveToLeft && i > m_FocusedItemIndex))
						m_Items[i].Hide();
					else
						m_Items[i].Show();
				}
				
				// Animate the content position using DOTween
				var fromPos = Content.anchoredPosition;
				DOTween.Kill(GetInstanceID());
				DOTween.To(() => 0f, x => {
						contentAnchored.x = Mathf.Lerp(fromPos.x, targetAnchored.x, x);
						Content.anchoredPosition = contentAnchored;
					}, 1f, time)
					.OnStart(() => {
						m_IsSnapping = true;
						onScrollStart?.Invoke();
						foreach (var item in m_Items)
							if(item.gameObject.activeSelf) item.OnBeginDrag();
					})
					.OnComplete(() => {
						for (int i = 0; i < m_Items.Length; i++)
						{
							if (i == m_FocusedItemIndex)
								m_Items[i].Show();
							else
								m_Items[i].Hide();
						}
						m_PreviousItemIndex = m_FocusedItemIndex;
						m_IsSnapping = false;
						Content.anchoredPosition = new Vector2(targetAnchored.x, contentAnchored.y);
						onScrollEnd?.Invoke();
					})
					.SetId(GetInstanceID());
#else
				// Fallback if DOTween is not available
				Content.anchoredPosition = new Vector2(targetAnchored.x, contentAnchored.y);
				m_PreviousItemIndex = m_FocusedItemIndex;
				onScrollEnd?.Invoke();
#endif
			}
		}

		/// <summary>
		/// (Editor-only) Moves to the focused item instantly for preview purposes.
		/// </summary>
		private void MoveToFocusedItem()
		{
			if (m_Items.Length == 0) return;

			for (int i = 0; i < m_Items.Length; i++) m_Items[i].Show();
			
			var targetAnchored = m_Items[m_FocusedItemIndex].RectTransform.CovertAnchoredPosFromChildToParent(m_ScrollView.content);
			targetAnchored -= m_TargetPosOffset;
			targetAnchored.x = Mathf.Clamp(targetAnchored.x, m_ContentAnchoredXMin, m_ContentAnchoredXMax);
			
			Content.anchoredPosition = new Vector2(targetAnchored.x, Content.anchoredPosition.y);
			m_PreviousItemIndex = m_FocusedItemIndex;
		}

		/// <summary>
		/// Updates the focused item index and invokes the onIndexChanged event.
		/// </summary>
		private void SetFocusedIndex(int pIndex)
		{
			if (m_FocusedItemIndex == pIndex)
				return;

			m_FocusedItemIndex = pIndex;
			onIndexChanged?.Invoke(pIndex);

			if (m_AutoSetMinScrollReaction)
				m_MinScrollReaction = m_Items[m_FocusedItemIndex].RectTransform.rect.width / 20f;
		}

		/// <summary>
		/// Finds the item whose center is physically closest to the center reference point.
		/// </summary>
		private void FindNearestItem()
		{
			m_distance = float.MaxValue;
			int nearestItemIndex = 0;
			for (int i = 0; i < m_Items.Length; i++)
			{
				float distance = Vector2.Distance(m_PointToCheckDistanceToCenter.position, m_Items[i].RectTransform.position);
				if (m_distance > distance)
				{
					m_distance = distance;
					nearestItemIndex = i;
				}
			}
			SetFocusedIndex(nearestItemIndex);
		}

		#endregion

		#if UNITY_EDITOR
		[CustomEditor(typeof(HorizontalSnapScrollView))]
#if ODIN_INSPECTOR
		private class HorizontalSnapScrollViewEditor : Sirenix.OdinInspector.Editor.OdinEditor
#else
		private class HorizontalSnapScrollViewEditor : UnityEditor.Editor
#endif
		{
			private HorizontalSnapScrollView m_target;
			private int m_ItemIndex;

#if ODIN_INSPECTOR
			protected override void OnEnable()
			{
				base.OnEnable();
#else
			private void OnEnable()
			{
#endif
				m_target = target as HorizontalSnapScrollView;
				m_target.Validate();
			}

			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				if (EditorHelper.Button("Validate"))
					m_target.Validate();

				GUILayout.BeginHorizontal();
				m_ItemIndex = EditorHelper.IntField(m_ItemIndex, "Item Index");
				if (EditorHelper.Button("Move To Item"))
					m_target.MoveToItem(m_ItemIndex, true); // Use immediate move in editor
				GUILayout.EndHorizontal();
			}
		}
#endif
	}
}