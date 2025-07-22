/**
 * Author HNB-RaBear - 2017
 **/

using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using RCore.Editor;
using UnityEditor;
#endif

namespace RCore.UI
{
	/// <summary>
	/// Provides a performance-optimized solution for a horizontal scroll view.
	/// It recycles a small pool of UI elements (virtualization) to display a large number of items,
	/// preventing the performance cost of instantiating thousands of GameObjects.
	/// This version includes support for fixed border elements on the left and right.
	/// </summary>
	public class OptimizedHorizontalScrollView : MonoBehaviour
	{
		#region Public Fields
		
		/// <summary>The main ScrollRect component that this script controls.</summary>
		[Tooltip("The main ScrollRect component that this script controls.")]
		public ScrollRect scrollView;
		
		/// <summary>The RectTransform that holds the visible, recycled item GameObjects. This should be the content of the ScrollRect.</summary>
		[Tooltip("The RectTransform that holds the visible, recycled item GameObjects. This should be the content of the ScrollRect.")]
		public RectTransform container;
		
		/// <summary>The RectTransform of the viewport (the visible area of the scroll view).</summary>
		[Tooltip("The RectTransform of the viewport (the visible area of the scroll view).")]
		public RectTransform viewRect;
		
		/// <summary>The prefab for a single item in the scroll view. It must have an OptimizedScrollItem component and its pivot must be (0.5, 0.5).</summary>
		[Tooltip("The prefab for a single item in the scroll view. It must have an OptimizedScrollItem component and its pivot must be (0.5, 0.5).")]
		public OptimizedScrollItem prefab;
		
		/// <summary>The total number of items in the virtual list.</summary>
		[Tooltip("The total number of items in the virtual list.")]
		public int total = 1;
		
		/// <summary>The horizontal spacing between each item.</summary>
		[Tooltip("The horizontal spacing between each item.")]
		public float spacing;
		
		/// <summary>An optional RectTransform for a fixed element at the far left of the content.</summary>
		[Tooltip("An optional RectTransform for a fixed element at the far left of the content.")]
		public RectTransform borderLeft;
		
		/// <summary>An optional RectTransform for a fixed element at the far right of the content.</summary>
		[Tooltip("An optional RectTransform for a fixed element at the far right of the content.")]
		public RectTransform borderRight;
		
		/// <summary>Read-only access to the ScrollRect's content RectTransform.</summary>
		public RectTransform content => scrollView.content;
		
		#endregion

		#region Private Fields
		
		private int m_totalBuffer = 2; // Extra items on each side to prevent visual pop-ins
		private int m_totalVisible;
		private float m_halfSizeContainer;
		private float m_cellSizeX; // The width of one item plus spacing
		private float m_rightBarOffset;
		private float m_leftBarOffset;

		private List<RectTransform> m_itemsRect = new List<RectTransform>();
		private List<OptimizedScrollItem> m_itemsScrolled = new List<OptimizedScrollItem>();
		private int m_optimizedTotal; // The actual number of GameObjects instantiated
		private Vector3 m_startPos;
		private Vector3 m_offsetVec;
		
		#endregion

		private void Start()
		{
			if (scrollView != null && scrollView.horizontalScrollbar != null)
				scrollView.horizontalScrollbar.onValueChanged.AddListener(ScrollBarChanged);
		}

		private void LateUpdate()
		{
			// Provide a manual update tick to all active items.
			for (int i = 0; i < m_itemsScrolled.Count; i++)
				m_itemsScrolled[i].ManualUpdate();
		}

		/// <summary>
		/// Initializes or re-initializes the scroll view with a new prefab and total item count.
		/// </summary>
		/// <param name="pPrefab">The new item prefab.</param>
		/// <param name="pTotalItems">The new total number of items.</param>
		/// <param name="pForce">If true, forces re-initialization even if the total item count hasn't changed.</param>
		public void Init(OptimizedScrollItem pPrefab, int pTotalItems, bool pForce)
		{
			prefab = pPrefab;
			Init(pTotalItems, pForce);
		}

		/// <summary>
		/// Initializes or re-initializes the scroll view. This sets up the content size,
		/// determines the number of items to pool, and instantiates them.
		/// </summary>
		/// <param name="pTotalItems">The total number of items the scroll view should represent.</param>
		/// <param name="pForce">If true, forces re-initialization even if the total item count hasn't changed.</param>
		public void Init(int pTotalItems, bool pForce)
		{
			if (total == pTotalItems && !pForce)
				return;
			
			if (prefab == null)
			{
				Debug.LogError("OptimizedHorizontalScrollView: Prefab is not assigned.");
				return;
			}

			m_itemsRect = new List<RectTransform>();

			// Prepare the object pool for the items
			if (m_itemsScrolled == null || m_itemsScrolled.Count == 0)
			{
				m_itemsScrolled = new List<OptimizedScrollItem>();
				m_itemsScrolled.Prepare(prefab, container.parent, 5);
			}
			else
				m_itemsScrolled.Free(container);

			total = pTotalItems;

			container.anchoredPosition3D = Vector3.zero;

			var rectZero = m_itemsScrolled[0].GetComponent<RectTransform>();
			var prefabSize = rectZero.rect.size;
			m_cellSizeX = prefabSize.x + spacing;

			// Set the total size of the content container, including any borders
			container.sizeDelta = new Vector2(m_cellSizeX * total, prefabSize.y);
			if (borderLeft != null && borderLeft.gameObject.activeSelf)
				container.sizeDelta = container.sizeDelta.AddX(borderLeft.rect.size.x);
			if (borderRight != null && borderRight.gameObject.activeSelf)
				container.sizeDelta = container.sizeDelta.AddX(borderRight.rect.size.x);

			// Calculate border offsets for scrollbar correction
			m_leftBarOffset = (borderLeft != null && borderLeft.gameObject.activeSelf) ? borderLeft.rect.size.x / container.sizeDelta.x : 0;
			m_rightBarOffset = (borderRight != null && borderRight.gameObject.activeSelf) ? borderRight.rect.size.x / container.sizeDelta.x : 0;
			
			m_halfSizeContainer = container.rect.size.x * 0.5f;

			// Calculate how many items are visible and the total number to pool
			m_totalVisible = Mathf.CeilToInt(viewRect.rect.size.x / m_cellSizeX);
			m_optimizedTotal = Mathf.Min(total, m_totalVisible + m_totalBuffer);
			
			// Calculate the starting position for the first item
			m_offsetVec = Vector3.right;
			m_startPos = container.anchoredPosition3D - m_offsetVec * m_halfSizeContainer + m_offsetVec * (prefabSize.x * 0.5f);

			// Adjust starting position for the left border
			if (borderLeft != null && borderLeft.gameObject.activeSelf)
				m_startPos.x += borderLeft.rect.size.x;
				
			// Instantiate and position the initial set of items
			for (int i = 0; i < m_optimizedTotal; i++)
			{
				var item = m_itemsScrolled.Obtain(container);
				var rt = item.transform as RectTransform;
				rt.anchoredPosition3D = m_startPos + m_offsetVec * (i * m_cellSizeX);
				m_itemsRect.Add(rt);

				item.gameObject.SetActive(true);
				item.UpdateContent(i, true);
			}

			prefab.gameObject.SetActive(false);
			
			// Adjust initial position to start at the beginning
			container.anchoredPosition3D += m_offsetVec * (m_halfSizeContainer - viewRect.rect.size.x * 0.5f);
		}
		
		#region Scroll Control
		
		/// <summary>
		/// Scrolls the view to the far left (beginning).
		/// </summary>
		/// <param name="tween">If true, animates the scroll using DOTween. If false, jumps instantly.</param>
		public void ScrollToTop(bool tween = false)
		{
			ScrollToNormalizedPosition(0f, tween);
		}
		
		/// <summary>
		/// Scrolls the view to the far right (end).
		/// </summary>
		/// <param name="tween">If true, animates the scroll using DOTween. If false, jumps instantly.</param>
		public void ScrollToBot(bool tween = false)
		{
			ScrollToNormalizedPosition(1f, tween);
		}

		/// <summary>
		/// Scrolls to a specific item index, ensuring it is visible within the viewport.
		/// </summary>
		/// <param name="pIndex">The index of the item to scroll to.</param>
		public void ScrollToTarget(int pIndex)
		{
			pIndex = Mathf.Clamp(pIndex, 0, total - 1);
			scrollView.StopMovement();

			float contentWidth = container.rect.width;
			// NOTE: This calculation assumes the container's anchor is centered (0.5, 0.5)
			float contentAnchoredXMin = contentWidth * (1 - container.pivot.x) * -1 + viewRect.rect.width * 0.5f;
			float contentAnchoredXMax = contentWidth * container.pivot.x - viewRect.rect.width * 0.5f;

			var prefabRect = prefab.transform as RectTransform;
			float targetX = contentAnchoredXMax - m_cellSizeX * pIndex + (prefabRect.pivot.x - 0.5f) * prefabRect.rect.width;
			float clampedX = Mathf.Clamp(targetX, contentAnchoredXMin, contentAnchoredXMax);

			container.anchoredPosition = new Vector2(clampedX, container.anchoredPosition.y);
		}

		/// <summary>
		/// Scrolls the view to center a specific item in the viewport, if possible.
		/// </summary>
		/// <param name="pIndex">The index of the item to center.</param>
		public void CenterChild(int pIndex)
		{
			pIndex = Mathf.Clamp(pIndex, 0, total - 1);
			scrollView.StopMovement();

			float contentWidth = container.rect.width;
			// NOTE: This calculation assumes the container's anchor is centered (0.5, 0.5)
			float contentAnchoredXMin = contentWidth * (1 - container.pivot.x) * -1 + viewRect.rect.width * 0.5f;
			float contentAnchoredXMax = contentWidth * container.pivot.x - viewRect.rect.width * 0.5f;
			
			// Calculate the container position that would center the item
			float targetX = -(m_startPos + m_offsetVec * (pIndex * m_cellSizeX)).x;
			float clampedX = Mathf.Clamp(targetX, contentAnchoredXMin, contentAnchoredXMax);
			
			container.anchoredPosition = new Vector2(clampedX, container.anchoredPosition.y);
		}

		private void ScrollToNormalizedPosition(float pNormX, bool pTween)
		{
			scrollView.StopMovement();
			if (pTween)
			{
#if DOTWEEN
				float fromValue = scrollView.horizontalNormalizedPosition;
				if (!Mathf.Approximately(fromValue, pNormX))
				{
					float time = Mathf.Abs(pNormX - fromValue) * 2;
					time = Mathf.Max(0.1f, time);
					DOTween.To(() => scrollView.horizontalNormalizedPosition, x => scrollView.horizontalNormalizedPosition = x, pNormX, time)
						.SetEase(Ease.OutCubic);
				}
#else
				scrollView.horizontalNormalizedPosition = pNormX;
#endif
			}
			else
			{
				scrollView.horizontalNormalizedPosition = pNormX;
			}
		}

		#endregion
		
		/// <summary>
		/// Manually triggers the ScrollBarChanged logic to refresh the view based on the current scroll position.
		/// </summary>
		public void RefreshScrollBar()
		{
			if (scrollView != null && scrollView.horizontalScrollbar != null)
				ScrollBarChanged(scrollView.horizontalScrollbar.value);
		}
		
		/// <summary>
		/// The core recycling logic, called when the scrollbar's value changes.
		/// </summary>
		/// <param name="pNormPos">The normalized position of the scrollbar (0 to 1).</param>
		public void ScrollBarChanged(float pNormPos)
		{
			if (m_optimizedTotal == 0)
			{
				Debug.LogError("m_OptimizedTotal should not be Zero. The scroll view may not be initialized.");
				return;
			}
			
			// Correct the normalized position to account for fixed borders
			float normPos = pNormPos;
			normPos += m_rightBarOffset * pNormPos;
			normPos -= m_leftBarOffset * (1 - pNormPos);
			normPos = Mathf.Clamp01(normPos);
			
			// Calculate the new virtual position and repositions/updates the pooled items.
			int numOutOfView = Mathf.CeilToInt(normPos * (total - m_totalVisible));
			int firstIndex = Mathf.Max(0, numOutOfView - m_totalBuffer);
			int originalIndex = firstIndex % m_optimizedTotal;

			int newIndex = firstIndex;
			// Reposition and update the content of all pooled items
			for (int i = originalIndex; i < m_optimizedTotal; i++)
			{
				MoveItemByIndex(m_itemsRect[i], newIndex);
				m_itemsScrolled[i].UpdateContent(newIndex);
				newIndex++;
			}
			for (int i = 0; i < originalIndex; i++)
			{
				MoveItemByIndex(m_itemsRect[i], newIndex);
				m_itemsScrolled[i].UpdateContent(newIndex);
				newIndex++;
			}
		}

		/// <summary>
		/// Dynamically adds more items to the end of the list.
		/// </summary>
		/// <param name="pTotalSlot">The number of new item slots to add.</param>
		public void Expand(int pTotalSlot)
		{
			total += pTotalSlot;
			// Recalculate container size, offsets, and other properties
			container.sizeDelta = container.sizeDelta.AddX(pTotalSlot * m_cellSizeX);
			m_halfSizeContainer = container.sizeDelta.x * 0.5f;

			var rectZero = m_itemsScrolled[0].GetComponent<RectTransform>();
			var prefabSize = rectZero.rect.size;

			m_offsetVec = Vector3.right;
			m_startPos = Vector3.zero - m_offsetVec * m_halfSizeContainer + m_offsetVec * (prefabSize.x * 0.5f);
			m_optimizedTotal = Mathf.Min(total, m_totalVisible + m_totalBuffer);

			if (borderLeft != null && borderLeft.gameObject.activeSelf)
			{
				m_startPos.x += borderLeft.rect.size.x;
				m_leftBarOffset = borderLeft.rect.size.x / container.sizeDelta.x;
			}
			if (borderRight != null && borderRight.gameObject.activeSelf)
				m_rightBarOffset = borderRight.rect.size.x / container.sizeDelta.x;

			// Refresh the view to reflect the changes
			RefreshScrollBar();
		}

		/// <summary>
		/// A helper method to calculate and set the anchored position of a pooled item.
		/// </summary>
		private void MoveItemByIndex(RectTransform item, int index)
		{
			item.anchoredPosition3D = m_startPos + m_offsetVec * (index * m_cellSizeX);
		}

		/// <summary>
		/// Returns the list of currently active (pooled) OptimizedScrollItem instances.
		/// </summary>
		public List<OptimizedScrollItem> GetListItem()
		{
			return m_itemsScrolled;
		}

		/// <summary>
		/// Calculates how many full items can be visible in the viewport at once.
		/// </summary>
		/// <returns>The number of fully visible cells.</returns>
		public int TotalFullCellVisible()
		{
			var rectZero = prefab.GetComponent<RectTransform>();
			var cellSizeX = rectZero.rect.size.x + spacing;
			return Mathf.FloorToInt(viewRect.rect.size.x / cellSizeX);
		}

#if UNITY_EDITOR
		[CustomEditor(typeof(OptimizedHorizontalScrollView))]
#if ODIN_INSPECTOR
		public class OptimizedHorizontalScrollViewEditor : Sirenix.OdinInspector.Editor.OdinEditor
		{
			private OptimizedHorizontalScrollView m_script;

			protected override void OnEnable()
			{
				base.OnEnable();
				m_script = (OptimizedHorizontalScrollView)target;
			}
#else
		public class OptimizedHorizontalScrollViewEditor : UnityEditor.Editor
		{
			private OptimizedHorizontalScrollView m_script;

			private void OnEnable()
			{
				m_script = (OptimizedHorizontalScrollView)target;
			}
#endif
			/// <summary>
			/// Draws the custom inspector GUI with test buttons.
			/// </summary>
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();
				
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Editor Controls", EditorStyles.boldLabel);

				if (EditorHelper.Button("Scroll To Start"))
					m_script.ScrollToTop();
				if (EditorHelper.Button("Scroll To End"))
					m_script.ScrollToBot();
				if (EditorHelper.Button("Center on Index 0"))
					m_script.CenterChild(0);
			}
		}
#endif
	}
}