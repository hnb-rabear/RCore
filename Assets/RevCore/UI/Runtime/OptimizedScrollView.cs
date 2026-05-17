using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RevCore.UI
{
    /// <summary>Scroll axis for <see cref="OptimizedScrollView"/>.</summary>
    public enum ScrollDirection
    {
        /// <summary>Items laid out left-to-right.</summary>
        Horizontal = 0,
        /// <summary>Items laid out top-to-bottom.</summary>
        Vertical = 1
    }

    /// <summary>
    /// Virtualized scroll view that instantiates only as many <see cref="OptimizedScrollItem"/>
    /// instances as fit on screen (plus a small buffer), recycling them as the user scrolls.
    /// Cuts allocation cost for long lists. Supports both horizontal and vertical layout via the
    /// <see cref="Direction"/> field; specialized horizontal/vertical subclasses give finer control.
    /// </summary>
    public class OptimizedScrollView : MonoBehaviour
    {
        /// <summary>The underlying <see cref="UnityEngine.UI.ScrollRect"/>.</summary>
        public ScrollRect scrollView;
        /// <summary>The content container whose size grows to the virtual total.</summary>
        public RectTransform container;
        /// <summary>Mask used to clip scrolled items.</summary>
        public Mask mask;
        /// <summary>Prefab cloned to populate items.</summary>
        public OptimizedScrollItem prefab;
        /// <summary>Total virtual item count. Edit via <see cref="Initialize"/>.</summary>
        public int total = 1;
        /// <summary>Spacing between items along the scroll axis.</summary>
        public float spacing;
        /// <summary>Scroll axis.</summary>
        public ScrollDirection Direction = ScrollDirection.Horizontal;

        private RectTransform m_maskRect;
        private int m_totalVisible;
        private int m_totalBuffer = 2;
        private float m_halfSizeContainer;
        private float m_prefabSize;
        private List<RectTransform> m_itemsRect = new();
        private List<OptimizedScrollItem> m_itemsScrolled = new();
        private int m_optimizedTotal;
        private Vector3 m_startPos;
        private Vector3 m_offsetVec;

        private void Start()
        {
            if (Direction == ScrollDirection.Vertical)
                scrollView.verticalScrollbar.onValueChanged.AddListener(ScrollBarChanged);
            else if (Direction == ScrollDirection.Horizontal)
                scrollView.horizontalScrollbar.onValueChanged.AddListener(ScrollBarChanged);

            Initialize(total, true);
        }

        private void LateUpdate()
        {
            for (int i = 0; i < m_itemsScrolled.Count; i++)
                m_itemsScrolled[i].ManualUpdate();
        }

        /// <summary>
        /// (Re)builds the scroll view for <paramref name="totalItems"/> items. Pass
        /// <paramref name="force"/> to force a rebuild even when the count hasn't changed.
        /// Returns early without crashing when <paramref name="totalItems"/> is 0.
        /// </summary>
        public void Initialize(int totalItems, bool force = false)
        {
            if (totalItems == total && !force)
                return;

            if (totalItems <= 0)
            {
                if (m_itemsScrolled != null)
                    m_itemsScrolled.Free(container);
                m_itemsRect = new List<RectTransform>();
                total = 0;
                m_optimizedTotal = 0;
                container.sizeDelta = Direction == ScrollDirection.Horizontal
                    ? new Vector2(0, container.sizeDelta.y)
                    : new Vector2(container.sizeDelta.x, 0);
                return;
            }

            m_itemsRect = new List<RectTransform>();

            if (m_itemsScrolled == null || m_itemsScrolled.Count == 0)
            {
                m_itemsScrolled = new List<OptimizedScrollItem>();
                m_itemsScrolled.Prepare(prefab, container.parent, 5);
            }
            else
            {
                m_itemsScrolled.Free(container);
            }

            total = totalItems;
            container.anchoredPosition3D = Vector3.zero;

            if (m_maskRect == null)
                m_maskRect = mask.GetComponent<RectTransform>();

            var prefabScale = m_itemsScrolled[0].GetComponent<RectTransform>().rect.size;
            m_prefabSize = (Direction == ScrollDirection.Horizontal ? prefabScale.x : prefabScale.y) + spacing;
            container.sizeDelta = Direction == ScrollDirection.Horizontal
                ? new Vector2(m_prefabSize * total, prefabScale.y)
                : new Vector2(prefabScale.x, m_prefabSize * total);
            m_halfSizeContainer = Direction == ScrollDirection.Horizontal
                ? container.rect.size.x * 0.5f
                : container.rect.size.y * 0.5f;

            m_totalVisible = Mathf.CeilToInt((Direction == ScrollDirection.Horizontal ? m_maskRect.rect.size.x : m_maskRect.rect.size.y) / m_prefabSize);
            m_offsetVec = Direction == ScrollDirection.Horizontal ? Vector3.right : Vector3.down;
            m_startPos = container.anchoredPosition3D - m_offsetVec * m_halfSizeContainer + m_offsetVec * ((Direction == ScrollDirection.Horizontal ? prefabScale.x : prefabScale.y) * 0.5f);
            m_optimizedTotal = Mathf.Min(total, m_totalVisible + m_totalBuffer);

            for (int i = 0; i < m_optimizedTotal; i++)
            {
                var item = m_itemsScrolled.Obtain(container);
                var rt = item.GetComponent<RectTransform>();
                rt.anchoredPosition3D = m_startPos + m_offsetVec * i * m_prefabSize;
                m_itemsRect.Add(rt);
                item.gameObject.SetActive(true);
                item.UpdateContent(i, true);
            }

            prefab.gameObject.SetActive(false);
            container.anchoredPosition3D += m_offsetVec * (m_halfSizeContainer - ((Direction == ScrollDirection.Horizontal ? m_maskRect.rect.size.x : m_maskRect.rect.size.y) * 0.5f));
        }

        /// <summary>Recomputes which items are visible at <paramref name="normPos"/> (0..1) and reassigns indices. Wired to the scroll bar's onValueChanged.</summary>
        public void ScrollBarChanged(float normPos)
        {
            if (m_optimizedTotal <= 0)
                return;

            if (Direction == ScrollDirection.Vertical)
                normPos = 1f - normPos;

            if (normPos <= 0)
                return;
            if (normPos > 1)
                normPos = 1f;

            int numOutOfView = Mathf.CeilToInt(normPos * (total - m_totalVisible));
            int firstIndex = Mathf.Max(0, numOutOfView - m_totalBuffer);
            int originalIndex = firstIndex % m_optimizedTotal;

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

        private void MoveItemByIndex(RectTransform item, int index)
        {
            item.anchoredPosition3D = m_startPos + m_offsetVec * index * m_prefabSize;
        }

        /// <summary>Returns the active pool of scroll items. Live view, not a copy.</summary>
        public List<OptimizedScrollItem> GetListItem()
        {
            return m_itemsScrolled;
        }
    }
}
