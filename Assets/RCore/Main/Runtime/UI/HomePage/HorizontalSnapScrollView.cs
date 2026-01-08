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
	/// Creates a scroll view that snaps to its child items horizontally.
	/// It handles user input (dragging) and programmatic scrolling.
	/// </summary>
	public class HorizontalSnapScrollView : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
	{
		/// <summary>
		/// Invoked when the focused item index changes. The new index is passed as an argument.
		/// </summary>
		public Action<int> onIndexChanged;
		/// <summary>
		/// Invoked when the scroll view finishes its snapping animation and comes to a stop.
		/// </summary>
		public Action onScrollEnd;
		/// <summary>
		/// Invoked when the scroll view starts moving, either by user drag or by a snapping animation.
		/// </summary>
		public Action onScrollStart;

		[Tooltip("The index of the item to be displayed first when the scene loads.")]
		[SerializeField] private int m_StartIndex;
		[Tooltip("The minimum duration of the snapping animation.")]
		[SerializeField] private float m_MinSpringTime = 0.5f;
		[Tooltip("The maximum duration of the snapping animation.")]
		[SerializeField] private float m_MaxSpringTime = 1f;
		[Tooltip("The velocity threshold below which the scroll view will start snapping to the nearest item.")]
		[SerializeField] private float m_SpringThreshold = 15;
		[Tooltip("If true, m_MinScrollReaction will be automatically calculated based on the focused item's width.")]
		[SerializeField] private bool m_AutoSetMinScrollReaction = true;
		[Tooltip("The minimum distance the user must drag to trigger a snap to the next/previous item. Only used if m_AutoSetMinScrollReaction is false.")]
		[SerializeField] private float m_MinScrollReaction = 10;
		[Tooltip("An offset applied to the final snapped position of the content.")]
		[SerializeField] private Vector2 m_TargetPosOffset;
		[Tooltip("Reference to the ScrollRect component.")]
		[SerializeField] private ScrollRect m_ScrollView;
		[Tooltip("The array of items within the scroll view. This is populated automatically.")]
		[SerializeField] private SnapScrollItem[] m_Items;
		[Tooltip("Set to TRUE if the items are ordered from right to left in the hierarchy.")]
		[SerializeField] private bool m_ReverseList;
		[Tooltip("A RectTransform used as a reference point (usually the center of the viewport) to determine the nearest item to snap to.")]
		[SerializeField] private RectTransform m_PointToCheckDistanceToCenter;

		[SerializeField, ReadOnly] private float m_ContentAnchoredXMin;
		[SerializeField, ReadOnly] private float m_ContentAnchoredXMax;
		[SerializeField, ReadOnly] private int m_FocusedItemIndex = -1;
		[SerializeField, ReadOnly] private int m_PreviousItemIndex = -1;
		[SerializeField, ReadOnly] private bool m_IsSnapping;
		[SerializeField, ReadOnly] private bool m_IsDragging;
		[SerializeField, ReadOnly] private bool m_Validated;
		private bool m_validateNextFrame;

		// The content's anchored position in the previous frame, used to calculate velocity.
		private Vector2 m_previousPosition;
		// The distance of a single drag gesture.
		private float m_dragDistance;
		// The velocity of the content's movement.
		private Vector2 m_velocity;
		// The distance between the center point and the nearest item, used for animation timing.
		private float m_distance;
		// The content's anchored position when a drag begins.
		private Vector2 m_beginDragPosition;
		// Flag indicating if the current drag is from left to right.
		private bool m_dragFromLeft;
		// Flag to check boundaries if the ScrollRect movement type is Unrestricted.
		private bool m_checkBoundary;
		// Flag to trigger finding the nearest item once the scroll view stops moving.
		private bool m_checkStop;

		/// <summary>
		/// Gets the RectTransform of the scroll view's content.
		/// </summary>
		public RectTransform Content => m_ScrollView.content;
		/// <summary>
		/// Gets the index of the currently focused or centered item.
		/// </summary>
		public int FocusedItemIndex => m_FocusedItemIndex;
		/// <summary>
		/// Gets the total number of items in the scroll view.
		/// </summary>
		public int TotalItems => m_Items.Length;
		/// <summary>
		/// Returns true if the scroll view is currently in a snapping animation.
		/// </summary>
		public bool IsSnapping => m_IsSnapping;
		/// <summary>
		/// Returns true if the user is currently dragging the scroll view.
		/// </summary>
		public bool IsDragging => m_IsDragging;
		/// <summary>
		/// Gets the array of all snap scroll items.
		/// </summary>
		public SnapScrollItem[] Items => m_Items;
		/// <summary>
		/// Gets the currently focused SnapScrollItem.
		/// </summary>
		public SnapScrollItem FocusedItem => m_Items[m_FocusedItemIndex];

		//=============================================

#region MonoBehaviour

		/// <summary>
		/// Initializes the component on the first frame.
		/// </summary>
		private void Start()
		{
			// Ensure all items have the same width as the viewport for a consistent layout.
			float parentWidth = m_ScrollView.viewport.rect.width;
			foreach (var item in m_Items)
			{
				var itemRectTransform = item.GetComponent<RectTransform>();
				if (itemRectTransform != null)
					itemRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, parentWidth);
			}
			
			// Perform validation to set up required variables.
			Validate();

			// Initialize the previous index and move to the designated start item.
			m_PreviousItemIndex = m_StartIndex;
			MoveToItem(m_StartIndex, true);
		}

		/// <summary>
		/// Called when the object becomes enabled and active.
		/// </summary>
		private void OnEnable()
		{
			// Determine if boundary checks are needed based on the ScrollRect's movement type.
			m_checkBoundary = m_ScrollView.movementType == ScrollRect.MovementType.Unrestricted;
		}

		/// <summary>
		/// Called every frame. Handles the snapping logic when not being dragged.
		/// </summary>
		private void Update()
		{
			// Do nothing if the component is not validated or is set to vertical scrolling.
			if (!m_Validated || !m_ScrollView.horizontal)
				return;

			if (m_validateNextFrame)
			{
				m_validateNextFrame = false;
				Validate();
			}

			if (m_Items == null || m_Items.Length == 0)
				return;

			// Calculate the velocity of the content.
			m_velocity = Content.anchoredPosition - m_previousPosition;
			m_previousPosition = Content.anchoredPosition;

			// If the user is dragging or a snap animation is in progress, do nothing.
			if (m_IsDragging || m_IsSnapping)
				return;

			float speedX = Mathf.Abs(m_velocity.x);
			if (speedX == 0)
			{
				// If movement just stopped, find the nearest item to snap to.
				if (m_checkStop)
				{
					FindNearestItem();
					m_checkStop = false;
				}
				return;
			}
			// If moving, set a flag to check for stop on the next frame.
			m_checkStop = true;

			// Check for out-of-bounds, otherwise handle snapping logic.
			if (m_checkBoundary && OutOfBoundary()) { }
			else
			{
				// If the scroll speed drops below the threshold, initiate a snap.
				if (speedX > 0 && speedX <= m_SpringThreshold)
				{
					FindNearestItem();
					int index = m_FocusedItemIndex;
					var targetPos = m_Items[index].RectTransform.CovertAnchoredPosFromChildToParent(m_ScrollView.content);
					
					// Based on the drag direction, determine if we should snap to the current or an adjacent item.
					if (m_dragFromLeft)
					{
						if (Content.anchoredPosition.x > targetPos.x + m_MinScrollReaction)
						{
							index += m_ReverseList ? 1 : -1;
						}
					}
					else
					{
						if (Content.anchoredPosition.x < targetPos.x - m_MinScrollReaction)
						{
							index += m_ReverseList ? -1 : 1;
						}
					}

					// Clamp the index to be within the bounds of the items array.
					if (index < 0)
						index = 0;
					else if (index >= m_Items.Length - 1)
						index = m_Items.Length - 1;
					
					// Set the new focused index and start the move animation.
					SetFocusedIndex(index);
					MoveToFocusedItem(false, speedX);
				}
			}
		}

		/// <summary>
		/// Called in the editor when a value is changed.
		/// </summary>
		private void OnValidate()
		{
			if (Application.isPlaying)
				return;

			// Validate and set up the initial position in the editor for preview.
			Validate();
			if (m_Items.Length > 0)
			{
				m_FocusedItemIndex = m_Items.Length / 2;
				m_PreviousItemIndex = m_Items.Length / 2;
				MoveToFocusedItem();
			}
		}

		/// <summary>
		/// Called when a drag gesture begins.
		/// </summary>
		public void OnBeginDrag(PointerEventData eventData)
		{
			if (!m_ScrollView.horizontal || m_IsSnapping)
				return;
				
			// Kill any ongoing snapping animation.
#if DOTWEEN
			DOTween.Kill(GetInstanceID());
#endif
			// Set state flags.
			m_IsDragging = true;
			m_IsSnapping = false;
			m_beginDragPosition = Content.anchoredPosition;
			onScrollStart?.Invoke();
			
			// Notify items that a drag has started.
			foreach (var item in m_Items)
				if (item.gameObject.activeSelf)
					item.OnBeginDrag();
		}

		/// <summary>
		/// Called every frame during a drag gesture.
		/// </summary>
		public void OnDrag(PointerEventData eventData)
		{
			if (!m_ScrollView.horizontal)
				return;
				
			// Ensure dragging state is active.
			if (!m_IsDragging)
			{
				onScrollStart?.Invoke();
				foreach (var item in m_Items)
					if (item.gameObject.activeSelf)
						item.OnBeginDrag();
			}
			m_IsDragging = true;
			
			// Continuously find the nearest item while dragging.
			FindNearestItem();

			// Show adjacent items to prepare for them to scroll into view.
			if (m_beginDragPosition.x < Content.anchoredPosition.x)
				m_Items[Mathf.Clamp(m_PreviousItemIndex - 1, 0, m_Items.Length - 1)].Show();
			else if (m_beginDragPosition.x > Content.anchoredPosition.x)
				m_Items[Mathf.Clamp(m_PreviousItemIndex + 1, 0, m_Items.Length - 1)].Show();
		}

		/// <summary>
		/// Called when a drag gesture ends.
		/// </summary>
		public void OnEndDrag(PointerEventData eventData)
		{
			if (!m_ScrollView.horizontal)
				return;
				
			m_IsDragging = false;
			
			// If out of bounds, let the ScrollRect's elasticity handle it.
			if (m_checkBoundary && OutOfBoundary())
				return;

			// Calculate the total drag distance and direction. This will be used by Update() to decide snapping.
			var endDragPosition = Content.anchoredPosition;
			m_dragDistance = Mathf.Abs(m_beginDragPosition.x - endDragPosition.x);
			m_dragFromLeft = m_beginDragPosition.x < endDragPosition.x;
		}

#endregion

		/// <summary>
		/// Moves the scroll view to focus on a specific item.
		/// </summary>
		/// <param name="item">The item to move to.</param>
		/// <param name="pImmediately">If true, the move is instant; otherwise, it's animated.</param>
		public void MoveToItem(SnapScrollItem item, bool pImmediately = false)
		{
			int index = m_Items.IndexOf(item);
			if (index >= 0)
				MoveToItem(index, pImmediately);
		}
		
		/// <summary>
		/// Moves the scroll view to focus on the item at the specified index.
		/// </summary>
		/// <param name="pIndex">The index of the item to move to.</param>
		/// <param name="pImmediately">If true, the move is instant; otherwise, it's animated.</param>
		public void MoveToItem(int pIndex, bool pImmediately = false)
		{
			if (pIndex < 0 || pIndex >= m_Items.Length || !m_Validated)
				return;

#if UNITY_EDITOR
			// In the editor, allow moving to items for testing purposes.
			if (!Application.isPlaying)
			{
				SetFocusedIndex(pIndex);
				MoveToFocusedItem(false, 0);
				return;
			}
#endif
			// Set the target index and initiate the move.
			SetFocusedIndex(pIndex);
			MoveToFocusedItem(pImmediately, m_SpringThreshold);
		}

		public void ValidateNextFrame()
		{
			if (m_validateNextFrame) return;
			m_validateNextFrame = true;
		}

		/// <summary>
		/// Validates the component's setup. Finds all child items and calculates content boundaries.
		/// This should be called if items are added or removed at runtime.
		/// </summary>
		public void Validate()
		{
			SnapScrollItem focusedItem = null;
			if (m_Items != null && m_FocusedItemIndex >= 0 && m_FocusedItemIndex < m_Items.Length)
				focusedItem = m_Items[m_FocusedItemIndex];

			// Automatically find all SnapScrollItem components in children.
			m_Items = gameObject.GetComponentsInChildren<SnapScrollItem>();
			if (focusedItem != null)
			{
				int index = Array.IndexOf(m_Items, focusedItem);
				if (index != -1)
					m_FocusedItemIndex = index;
				else
					m_FocusedItemIndex = Mathf.Clamp(m_FocusedItemIndex, 0, Mathf.Max(0, m_Items.Length - 1));
			}
#if UNITY_EDITOR
			// string str = "Cotent Top Right: "
			// 	+ Content.TopRight()
			// 	+ "\nContent Bot Left: "
			// 	+ Content.BotLeft()
			// 	+ "\nContent Center: "
			// 	+ Content.Center()
			// 	+ "\nContent Size"
			// 	+ Content.sizeDelta
			// 	+ "\nContent Pivot"
			// 	+ Content.pivot
			// 	+ "\nViewPort Size"
			// 	+ m_ScrollView.viewport.rect.size;
			//Debug.Log(str);
#endif

			// Calculate the content width based on items, layout groups, and padding.
			Content.TryGetComponent(out ContentSizeFitter contentFilter);
			float contentWidth = 0;
			float contentHeight = 0;
			if (contentFilter != null)
			{
				// Consider layout properties if a HorizontalLayoutGroup is present.
				Content.TryGetComponent(out HorizontalLayoutGroup horizontalLayout);
				float paddingLeft = 0;
				float paddingRight = 0;
				float spacing = 0;
				if (horizontalLayout != null)
				{
					paddingLeft = horizontalLayout.padding.left;
					paddingRight = horizontalLayout.padding.right;
					spacing = horizontalLayout.spacing;
				}
				for (int i = 0; i < m_Items.Length; i++)
				{
					if (m_Items[i].gameObject.activeSelf)
					{
						var itemSize = m_Items[i].RectTransform.rect.size;
						contentWidth += itemSize.x;
						if (contentHeight < itemSize.y)
							contentHeight = itemSize.y;
					}
				}
				contentWidth += paddingLeft + paddingRight;
				contentWidth += spacing * (m_Items.Length - 1);
			}
			else
				contentWidth = Content.rect.width;

			// Calculate the min and max anchored positions for the content.
			float contentPivotX = Content.pivot.x;
			float viewPortOffsetX = m_ScrollView.viewport.rect.width / 2f;
			m_ContentAnchoredXMin = (contentWidth - contentWidth * contentPivotX - viewPortOffsetX) * -1;
			m_ContentAnchoredXMax = (0 - contentWidth * contentPivotX + viewPortOffsetX) * -1;

			if (m_MinScrollReaction < 10)
				m_MinScrollReaction = 10;
			m_Validated = true;

			// If StartIndex has changed, move to the new item.
			if (!Application.isPlaying && m_StartIndex != m_PreviousItemIndex)
				MoveToItem(m_StartIndex, true);
		}

		/// <summary>
		/// Handles the actual movement of the content to the focused item's position.
		/// </summary>
		/// <param name="pImmediately">If true, move instantly. If false, animate the movement.</param>
		/// <param name="pSpeed">The speed of the movement, used to calculate animation duration.</param>
		private void MoveToFocusedItem(bool pImmediately, float pSpeed)
		{
			if (m_Items == null || m_Items.Length == 0)
				return;

			// Stop any residual movement from the ScrollRect.
			m_ScrollView.StopMovement();

			// Calculate the target anchored position for the content.
			var targetAnchored = m_Items[m_FocusedItemIndex].RectTransform.CovertAnchoredPosFromChildToParent(m_ScrollView.content);
			targetAnchored.x -= m_TargetPosOffset.x;
			targetAnchored.y -= m_TargetPosOffset.y;
			
			// Clamp the target position within the calculated boundaries.
			if (targetAnchored.x > m_ContentAnchoredXMax)
				targetAnchored.x = m_ContentAnchoredXMax;
			if (targetAnchored.x < m_ContentAnchoredXMin)
				targetAnchored.x = m_ContentAnchoredXMin;

			var contentAnchored = Content.anchoredPosition;
			if (pImmediately)
			{
				// Move instantly to the target position.
				contentAnchored.x = targetAnchored.x;
				Content.anchoredPosition = contentAnchored;
				onScrollEnd?.Invoke();
				
				// Show the focused item and hide others.
				for (int i = 0; i < m_Items.Length; i++)
				{
					if (i == m_FocusedItemIndex)
						m_Items[i].Show();
					else
						m_Items[i].Hide();
				}
				m_PreviousItemIndex = m_FocusedItemIndex;
			}
			else
			{
				m_IsSnapping = false;

#if DOTWEEN
				// Calculate animation duration based on distance and speed.
				if (m_distance == 0)
					m_distance = Vector2.Distance(m_PointToCheckDistanceToCenter.position, m_Items[m_FocusedItemIndex].RectTransform.position);
				float time = m_distance / (pSpeed / Time.deltaTime);
				if (time == 0)
					return;
				time = Mathf.Clamp(time, m_MinSpringTime, m_MaxSpringTime);

				// Determine which items to hide during the transition.
				bool moveToLeft = Content.anchoredPosition.x < targetAnchored.x;
				for (int i = 0; i < m_Items.Length; i++)
				{
					if (moveToLeft && i > m_PreviousItemIndex || !moveToLeft && i < m_PreviousItemIndex || moveToLeft && i < m_FocusedItemIndex || !moveToLeft && i > m_FocusedItemIndex)
						m_Items[i].Hide();
					else
						m_Items[i].Show();
				}
				
				// Create the DOTween animation.
				var fromPos = Content.anchoredPosition;
				float lerp = 0;
				DOTween.Kill(GetInstanceID());
				DOTween.To(() => lerp, x => lerp = x, 1f, time)
					.OnStart(() =>
					{
						m_IsSnapping = true;
						onScrollStart?.Invoke();
						foreach (var item in m_Items)
							if (item.gameObject.activeSelf)
								item.OnBeginDrag();
					})
					.OnUpdate(() =>
					{
						contentAnchored.x = Mathf.Lerp(fromPos.x, targetAnchored.x, lerp);
						Content.anchoredPosition = contentAnchored;
					})
					.OnComplete(() =>
					{
						// Finalize positions and states on completion.
						for (int i = 0; i < m_Items.Length; i++)
						{
							if (i == m_FocusedItemIndex)
								m_Items[i].Show();
							else
								m_Items[i].Hide();
						}

						m_PreviousItemIndex = m_FocusedItemIndex;
						m_IsSnapping = false;
						contentAnchored.x = targetAnchored.x;
						Content.anchoredPosition = contentAnchored;
						onScrollEnd?.Invoke();
					})
					.SetId(GetInstanceID());
#else
				// Fallback if DOTween is not available: move instantly.
				for (int i = 0; i < m_Items.Length; i++)
				{
					if (i == m_FocusedItemIndex)
						m_Items[i].Show();
					else
						m_Items[i].Hide();
				}
				m_PreviousItemIndex = m_FocusedItemIndex;
				contentAnchored.x = targetAnchored.x;
				Content.anchoredPosition = contentAnchored;
				onScrollEnd?.Invoke();
#endif
			}
		}

		/// <summary>
		/// Moves to the focused item instantly. Used only in the Unity Editor for previewing.
		/// </summary>
		private void MoveToFocusedItem()
		{
			if (m_Items.Length == 0)
				return;

			// Ensure all items are visible for editor manipulation.
			for (int i = 0; i < m_Items.Length; i++)
				m_Items[i].Show();
			
			// Calculate the target position.
			var targetAnchored = m_Items[m_FocusedItemIndex].RectTransform.CovertAnchoredPosFromChildToParent(m_ScrollView.content);
			targetAnchored.x -= m_TargetPosOffset.x;
			targetAnchored.y -= m_TargetPosOffset.y;
			if (targetAnchored.x > m_ContentAnchoredXMax)
				targetAnchored.x = m_ContentAnchoredXMax;
			if (targetAnchored.x < m_ContentAnchoredXMin)
				targetAnchored.x = m_ContentAnchoredXMin;
			
			// Set the content position instantly.
			var contentAnchored = Content.anchoredPosition;
			contentAnchored.x = targetAnchored.x;
			Content.anchoredPosition = contentAnchored;
			m_PreviousItemIndex = m_FocusedItemIndex;
		}

		/// <summary>
		/// Sets the focused item index and invokes the onIndexChanged event.
		/// </summary>
		/// <param name="pIndex">The new index to be focused.</param>
		private void SetFocusedIndex(int pIndex)
		{
			if (m_FocusedItemIndex == pIndex)
				return;

			m_FocusedItemIndex = pIndex;
			onIndexChanged?.Invoke(pIndex);

			// Automatically adjust the scroll reaction sensitivity if enabled.
			if (m_AutoSetMinScrollReaction && m_Items != null && m_Items.Length > 0 && m_FocusedItemIndex < m_Items.Length)
				m_MinScrollReaction = m_Items[m_FocusedItemIndex].RectTransform.rect.width / 20f;
		}

		/// <summary>
		/// Checks if the drag distance is sufficient to move to the next or previous item.
		/// Note: This method is not called in the current implementation but is kept for potential future use.
		/// </summary>
		private void CheckScrollReaction()
		{
			if (m_dragDistance > m_MinScrollReaction)
			{
				int index = m_FocusedItemIndex;
				
				// Determine the next index based on drag direction and list order.
				if (m_dragFromLeft)
				{
					index += m_ReverseList ? 1 : -1;
				}
				else
				{
					index += m_ReverseList ? -1 : 1;
				}
				
				// Clamp the index to valid bounds.
				if (index < 0)
					index = 0;
				else if (index >= m_Items.Length - 1)
					index = m_Items.Length - 1;
				SetFocusedIndex(index);
			}
		}

		/// <summary>
		/// Finds the item whose center is closest to the reference point (m_PointToCheckDistanceToCenter).
		/// </summary>
		private void FindNearestItem()
		{
			m_distance = 1000000;
			int nearestItemIndex = 0;
			for (int i = 0; i < m_Items.Length; i++)
			{
				// Calculate the distance from the reference point to the item's center.
				float distance = Vector2.Distance(m_PointToCheckDistanceToCenter.position, m_Items[i].RectTransform.position);
				distance = Mathf.Abs(distance);
				
				// Keep track of the item with the minimum distance.
				if (m_distance > distance)
				{
					m_distance = distance;
					nearestItemIndex = i;
				}
			}
			// Set the focused index to the nearest item found.
			SetFocusedIndex(nearestItemIndex);
		}

		/// <summary>
		/// Used to check if the content has moved beyond its calculated boundaries.
		/// Relevant when the ScrollRect's movement type is Unrestricted.
		/// </summary>
		/// <returns>True if the content is out of bounds, otherwise false.</returns>
		private bool OutOfBoundary()
		{
			var contentAnchored = Content.anchoredPosition;
			if (contentAnchored.x < m_ContentAnchoredXMin || contentAnchored.x > m_ContentAnchoredXMax)
			{
				Debug.Log("Out of boundary");
				return true;
			}
			return false;
		}

#if UNITY_EDITOR
		/// <summary>
		/// Custom editor for the HorizontalSnapScrollView component to provide extra functionality in the Inspector.
		/// </summary>
		[CustomEditor(typeof(HorizontalSnapScrollView))]
#if ODIN_INSPECTOR
		private class HorizontalSnapScrollViewEditor : Sirenix.OdinInspector.Editor.OdinEditor
		{
			private HorizontalSnapScrollView m_target;
			private int m_ItemIndex;

			protected override void OnEnable()
			{
				base.OnEnable();
				m_target = target as HorizontalSnapScrollView;
				m_target.Validate();
			}
#else
		private class HorizontalSnapScrollViewEditor : UnityEditor.Editor
		{
			private HorizontalSnapScrollView m_target;
			private int m_ItemIndex;

			private void OnEnable()
			{
				m_target = target as HorizontalSnapScrollView;
				m_target.Validate();
			}
#endif

			/// <summary>
			/// Draws the custom inspector GUI.
			/// </summary>
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				if (EditorHelper.Button("Validate"))
					m_target.Validate();

				// Adds a section for testing the MoveToItem functionality directly from the editor.
				GUILayout.BeginHorizontal();
				m_ItemIndex = EditorHelper.IntField(m_ItemIndex, "Item Index");
				if (EditorHelper.Button("MoveToItem"))
					m_target.MoveToItem(m_ItemIndex, false);
				GUILayout.EndHorizontal();
			}
		}
#endif
	}
}