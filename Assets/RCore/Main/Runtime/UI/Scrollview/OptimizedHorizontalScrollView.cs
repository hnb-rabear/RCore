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
	/// Provides an optimized horizontal scroll view that recycles its items.
	/// This is useful for lists with a large number of items, as it only instantiates
	/// and displays the items that are currently visible, plus a small buffer.
	/// </summary>
	public class OptimizedHorizontalScrollView : MonoBehaviour
	{
		/// <summary>
		/// The main ScrollRect component.
		/// </summary>
		public ScrollRect scrollView;
		/// <summary>
		/// The RectTransform that holds the scrollable content and whose size is adjusted.
		/// </summary>
		public RectTransform container;
		/// <summary>
		/// The RectTransform of the viewport, which defines the visible area.
		/// </summary>
		public RectTransform viewRect;
		/// <summary>
		/// The prefab for a single item in the scroll view. The pivot must be (0.5, 0.5).
		/// </summary>
		public OptimizedScrollItem prefab;
		/// <summary>
		/// The total number of items in the list.
		/// </summary>
		public int total = 1;
		/// <summary>
		/// The spacing between items.
		/// </summary>
		public float spacing;
		/// <summary>
		/// An optional RectTransform for a border or padding on the left side.
		/// </summary>
		public RectTransform borderLeft;
		/// <summary>
		/// An optional RectTransform for a border or padding on the right side.
		/// </summary>
		public RectTransform borderRight;
		/// <summary>
		/// A public getter for the ScrollRect's content.
		/// </summary>
		public RectTransform content => scrollView.content;

		/// <summary>
		/// The number of items to instantiate on either side of the visible area as a buffer.
		/// </summary>
		private int m_totalBuffer = 2;
		/// <summary>
		/// The number of items that can be visible in the viewport at one time.
		/// </summary>
		private int m_totalVisible;
		/// <summary>
		/// Half of the container's width, used for positioning calculations.
		/// </summary>
		private float m_halfSizeContainer;
		/// <summary>
		/// The width of a single cell (prefab width + spacing).
		/// </summary>
		private float m_cellSizeX;
		/// <summary>
		/// The normalized offset caused by the right border.
		/// </summary>
		private float m_rightBarOffset;
		/// <summary>
		/// The normalized offset caused by the left border.
		/// </summary>
		private float m_leftBarOffset;

		/// <summary>
		/// A list of the RectTransforms of the recycled item instances.
		/// </summary>
		private List<RectTransform> m_itemsRect = new List<RectTransform>();
		/// <summary>
		/// A list of the script components of the recycled item instances.
		/// </summary>
		private List<OptimizedScrollItem> m_itemsScrolled = new List<OptimizedScrollItem>();
		/// <summary>
		/// The number of items that are actually instantiated (visible items + buffer).
		/// </summary>
		private int m_optimizedTotal;
		/// <summary>
		/// The starting position for the first item.
		/// </summary>
		private Vector3 m_startPos;
		/// <summary>
		/// A vector representing the direction of scrolling (Vector3.right).
		/// </summary>
		private Vector3 m_offsetVec;

		private void Start()
		{
			// Subscribe to the scrollbar's value changed event to update the items.
			scrollView.horizontalScrollbar.onValueChanged.AddListener(ScrollBarChanged);
		}

		/// <summary>
		/// Initializes the scroll view with a specific prefab and total item count.
		/// </summary>
		/// <param name="pPrefab">The item prefab.</param>
		/// <param name="pTotalItems">The total number of items in the list.</param>
		/// <param name="pForce">If true, forces re-initialization even if the total item count hasn't changed.</param>
		public void Init(OptimizedScrollItem pPrefab, int pTotalItems, bool pForce)
		{
			prefab = pPrefab;
			Init(pTotalItems, pForce);
		}

		/// <summary>
		/// Called every frame after all Update functions have been called.
		/// Used to manually update the visible scroll items.
		/// </summary>
		private void LateUpdate()
		{
			for (int i = 0; i < m_itemsScrolled.Count; i++)
				m_itemsScrolled[i].ManualUpdate();
		}

		/// <summary>
		/// Initializes or re-initializes the scroll view. Sets up item pooling, calculates container size, and positions the initial items.
		/// </summary>
		/// <param name="pTotalItems">The total number of items in the list.</param>
		/// <param name="pForce">If true, forces re-initialization even if the total item count hasn't changed.</param>
		public void Init(int pTotalItems, bool pForce)
		{
			if (total == pTotalItems && !pForce)
				return;

			m_itemsRect = new List<RectTransform>();

			// Initialize or reset the item pool.
			if (m_itemsScrolled == null || m_itemsScrolled.Count == 0)
			{
				m_itemsScrolled = new List<OptimizedScrollItem>();
				m_itemsScrolled.Prepare(prefab, container.parent, 5);
			}
			else
				m_itemsScrolled.Free(container);

			total = pTotalItems;
			container.anchoredPosition3D = Vector3.zero;

			// Calculate cell and container sizes.
			var rectZero = m_itemsScrolled[0].GetComponent<RectTransform>();
			var prefabSize = rectZero.rect.size;
			m_cellSizeX = prefabSize.x + spacing;
			container.sizeDelta = new Vector2(m_cellSizeX * total, prefabSize.y);

			// Adjust container size for borders.
			if (borderLeft != null && borderLeft.gameObject.activeSelf)
				container.sizeDelta = container.sizeDelta.AddX(borderLeft.rect.size.x);
			if (borderRight != null && borderRight.gameObject.activeSelf)
				container.sizeDelta = container.sizeDelta.AddX(borderRight.rect.size.x);

			// Calculate normalized border offsets.
			if (borderLeft != null && borderLeft.gameObject.activeSelf)
				m_leftBarOffset = borderLeft.rect.size.x / container.sizeDelta.x;
			if (borderRight != null && borderRight.gameObject.activeSelf)
				m_rightBarOffset = borderRight.rect.size.x / container.sizeDelta.x;

			m_halfSizeContainer = container.rect.size.x * 0.5f;
			m_totalVisible = Mathf.CeilToInt(viewRect.rect.size.x / m_cellSizeX);

			// Determine starting position and the number of items to instantiate.
			m_offsetVec = Vector3.right;
			m_startPos = container.anchoredPosition3D - m_offsetVec * m_halfSizeContainer + m_offsetVec * (prefabSize.x * 0.5f);
			m_optimizedTotal = Mathf.Min(total, m_totalVisible + m_totalBuffer);

			if (borderLeft != null && borderLeft.gameObject.activeSelf)
				m_startPos.x += borderLeft.rect.size.x;

			// Instantiate and position the initial set of items.
			for (int i = 0; i < m_optimizedTotal; i++)
			{
				var item = m_itemsScrolled.Obtain(container);
				var rt = item.transform as RectTransform;
				rt.anchoredPosition3D = m_startPos + m_offsetVec * (i * m_cellSizeX);
				m_itemsRect.Add(rt);
				item.gameObject.SetActive(true);
				item.UpdateContent(i, true);
			}

			// Deactivate the original prefab and set the initial scroll position.
			prefab.gameObject.SetActive(false);
			container.anchoredPosition3D += m_offsetVec * (m_halfSizeContainer - viewRect.rect.size.x * 0.5f);
		}

		/// <summary>
		/// Scrolls the view to the beginning (left).
		/// </summary>
		/// <param name="tween">If true, animates the scroll using DOTween.</param>
		public void ScrollToTop(bool tween = false)
		{
			scrollView.StopMovement();
			if (tween)
			{
#if DOTWEEN
				float fromValue = scrollView.horizontalScrollbar.value;
				float toValue = 0f;
				if (fromValue != toValue)
				{
					float time = Mathf.Abs(toValue - fromValue) * 2;
					if (time < 0.1f && time > 0)
						time = 0.1f;
					DOTween.To(() => scrollView.horizontalScrollbar.value, x => scrollView.horizontalScrollbar.value = x, toValue, time);
				}
#else
				scrollView.horizontalScrollbar.value = 0;
#endif
			}
			else
				scrollView.horizontalScrollbar.value = 0;
		}

		/// <summary>
		/// Scrolls the view to the end (right).
		/// </summary>
		/// <param name="tween">If true, animates the scroll using DOTween.</param>
		public void ScrollToBot(bool tween = false)
		{
			scrollView.StopMovement();
#if DOTWEEN
			if (tween)
			{
				float fromValue = scrollView.horizontalScrollbar.value;
				float toValue = 1f;
				if (fromValue != toValue)
				{
					float time = Mathf.Abs(toValue - fromValue) * 2;
					if (time < 0.1f && time > 0)
						time = 0.1f;
					DOTween.To(() => scrollView.horizontalScrollbar.value, x => scrollView.horizontalScrollbar.value = x, toValue, time);
				}
			}
			else
#endif
			{
				scrollView.horizontalScrollbar.value = 1;
			}
		}

		/// <summary>
		/// Manually triggers the scroll bar changed logic to refresh item positions.
		/// </summary>
		public void RefreshScrollBar()
		{
			ScrollBarChanged(scrollView.horizontalScrollbar.value);
		}

		/// <summary>
		/// This is the core logic for the optimized scroll view.
		/// It is called whenever the scrollbar's value changes. It calculates which items should be visible
		/// and repositions/recycles the instantiated items to represent the correct data.
		/// </summary>
		/// <param name="pNormPos">The current normalized position of the scrollbar (0 to 1).</param>
		public void ScrollBarChanged(float pNormPos)
		{
			if (m_optimizedTotal == 0)
			{
				Debug.LogError("m_OptimizedTotal should not be Zero");
				return;
			}

			// Adjust normalized position for borders.
			float normPos = pNormPos;
			normPos += m_rightBarOffset * pNormPos;
			normPos -= m_leftBarOffset * (1 - pNormPos);
			normPos = Mathf.Clamp(normPos, 0, 1);

			// Calculate the index of the first item that should be in the buffer zone off-screen to the left.
			int numOutOfView = Mathf.CeilToInt(normPos * (total - m_totalVisible));
			int firstIndex = Mathf.Max(0, numOutOfView - m_totalBuffer);
			// Determine which of our pooled items corresponds to this new first index.
			int originalIndex = firstIndex % m_optimizedTotal;

			// Reposition and update the content of the pooled items based on the new scroll position.
			int newIndex = firstIndex;
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
		/// Expands the scroll view by adding more item slots.
		/// </summary>
		/// <param name="pTotalSlot">The number of new slots to add.</param>
		public void Expand(int pTotalSlot)
		{
			total += pTotalSlot;
			container.sizeDelta = container.sizeDelta.AddX(pTotalSlot * m_cellSizeX);
			m_halfSizeContainer = container.sizeDelta.x * 0.5f;

			var rectZero = m_itemsScrolled[0].GetComponent<RectTransform>();
			var prefabSize = rectZero.rect.size;

			// Recalculate starting position and other variables.
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

			ScrollBarChanged(scrollView.horizontalScrollbar.value);
		}

		/// <summary>
		/// Moves a specific item's RectTransform to the position corresponding to a given data index.
		/// </summary>
		/// <param name="item">The RectTransform of the item to move.</param>
		/// <param name="index">The data index this item should represent.</param>
		private void MoveItemByIndex(RectTransform item, int index)
		{
			item.anchoredPosition3D = m_startPos + m_offsetVec * (index * m_cellSizeX);
		}

		/// <summary>
		/// Gets the list of currently active (pooled) item scripts.
		/// </summary>
		/// <returns>A list of OptimizedScrollItem components.</returns>
		public List<OptimizedScrollItem> GetListItem()
		{
			return m_itemsScrolled;
		}

		/// <summary>
		/// Instantly scrolls the view so that a specific item is visible at the start of the view.
		/// </summary>
		/// <param name="pIndex">The index of the item to scroll to.</param>
		public void ScrollToTarget(int pIndex)
		{
			pIndex = Mathf.Clamp(pIndex, 0, total - 1);
			scrollView.StopMovement();

			// Calculate the min and max anchored positions for the content.
			float contentWidth = container.rect.width;
			float contentPivotX = container.pivot.x;
			float contentAnchoredXMin = contentWidth * (1 - contentPivotX) * -1 + viewRect.rect.width * 0.5f;
			float contentAnchoredXMax = contentWidth * contentPivotX - viewRect.rect.width * 0.5f;

			// Calculate the target position.
			var prefabRect = prefab.transform as RectTransform;
			float x = contentAnchoredXMax - m_cellSizeX * pIndex + (prefabRect.pivot.x - 0.5f) * prefabRect.rect.width;
			x = Mathf.Clamp(x, contentAnchoredXMin, contentAnchoredXMax);

			container.anchoredPosition = new Vector2(x, container.anchoredPosition.y);
		}

		/// <summary>
		/// Instantly scrolls the view to center a specific item in the viewport.
		/// </summary>
		/// <param name="pIndex">The index of the item to center.</param>
		public void CenterChild(int pIndex)
		{
			pIndex = Mathf.Clamp(pIndex, 0, total - 1);
			scrollView.StopMovement();

			// Calculate the min and max anchored positions for the content.
			float contentWidth = container.rect.width;
			float contentPivotX = container.pivot.x;
			float contentAnchoredXMin = contentWidth * (1 - contentPivotX) * -1 + viewRect.rect.width * 0.5f;
			float contentAnchoredXMax = contentWidth * contentPivotX - viewRect.rect.width * 0.5f;
			
			// Calculate the target position to center the item.
			var x = -(m_startPos + m_offsetVec * (pIndex * m_cellSizeX)).x;
			x = Mathf.Clamp(x, contentAnchoredXMin, contentAnchoredXMax);
			
			container.anchoredPosition = new Vector2(x, container.anchoredPosition.y);
		}

		/// <summary>
		/// Calculates how many full items can be visible in the viewport.
		/// </summary>
		/// <returns>The number of fully visible cells.</returns>
		public int TotalFullCellVisible()
		{
			var rectZero = prefab.GetComponent<RectTransform>();
			var cellSizeX = rectZero.rect.size.x + spacing;
			return Mathf.FloorToInt(viewRect.rect.size.x / cellSizeX);
		}

#if UNITY_EDITOR
		/// <summary>
		/// Custom editor for the OptimizedHorizontalScrollView to add helper buttons in the inspector.
		/// </summary>
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
			/// Draws the custom inspector GUI.
			/// </summary>
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();
				if (EditorHelper.Button("Move To Top"))
					m_script.ScrollToTop();
				if (EditorHelper.Button("Move To Top 2"))
					m_script.ScrollToTarget(0);
				if (EditorHelper.Button("Move To Top 3"))
					m_script.ScrollToTarget(0);
			}
		}
#endif
	}
}