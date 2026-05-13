#if DOTWEEN
using DG.Tweening;
#endif
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RevCore.UI
{
    public class OptimizedHorizontalScrollView : MonoBehaviour
    {
        public ScrollRect scrollView;
        public RectTransform container;
        public RectTransform viewRect;
        public OptimizedScrollItem prefab;
        public int total = 1;
        public float spacing;
        public RectTransform borderLeft;
        public RectTransform borderRight;

        public RectTransform content => scrollView.content;

        private int m_totalBuffer = 2;
        private int m_totalVisible;
        private float m_halfSizeContainer;
        private float m_cellSizeX;
        private float m_rightBarOffset;
        private float m_leftBarOffset;
        private List<RectTransform> m_itemsRect = new();
        private List<OptimizedScrollItem> m_itemsScrolled = new();
        private int m_optimizedTotal;
        private Vector3 m_startPos;
        private Vector3 m_offsetVec;

        private void Start()
        {
            scrollView.horizontalScrollbar.onValueChanged.AddListener(ScrollBarChanged);
        }

        public void Init(OptimizedScrollItem itemPrefab, int totalItems, bool force)
        {
            prefab = itemPrefab;
            Init(totalItems, force);
        }

        private void LateUpdate()
        {
            for (int i = 0; i < m_itemsScrolled.Count; i++)
                m_itemsScrolled[i].ManualUpdate();
        }

        public void Init(int totalItems, bool force)
        {
            if (total == totalItems && !force)
                return;

            m_itemsRect = new List<RectTransform>();

            if (m_itemsScrolled == null || m_itemsScrolled.Count == 0)
            {
                m_itemsScrolled = new List<OptimizedScrollItem>();
                m_itemsScrolled.Prepare(prefab, container.parent, 5);
            }
            else
                m_itemsScrolled.Free(container);

            total = totalItems;
            container.anchoredPosition3D = Vector3.zero;

            var rectZero = m_itemsScrolled[0].GetComponent<RectTransform>();
            var prefabSize = rectZero.rect.size;
            m_cellSizeX = prefabSize.x + spacing;
            container.sizeDelta = new Vector2(m_cellSizeX * total, prefabSize.y);

            if (borderLeft != null && borderLeft.gameObject.activeSelf)
                container.sizeDelta = container.sizeDelta.AddX(borderLeft.rect.size.x);
            if (borderRight != null && borderRight.gameObject.activeSelf)
                container.sizeDelta = container.sizeDelta.AddX(borderRight.rect.size.x);

            if (borderLeft != null && borderLeft.gameObject.activeSelf)
                m_leftBarOffset = borderLeft.rect.size.x / container.sizeDelta.x;
            if (borderRight != null && borderRight.gameObject.activeSelf)
                m_rightBarOffset = borderRight.rect.size.x / container.sizeDelta.x;

            m_halfSizeContainer = container.rect.size.x * 0.5f;
            m_totalVisible = Mathf.CeilToInt(viewRect.rect.size.x / m_cellSizeX);

            m_offsetVec = Vector3.right;
            m_startPos = container.anchoredPosition3D - m_offsetVec * m_halfSizeContainer + m_offsetVec * (prefabSize.x * 0.5f);
            m_optimizedTotal = Mathf.Min(total, m_totalVisible + m_totalBuffer);

            if (borderLeft != null && borderLeft.gameObject.activeSelf)
                m_startPos.x += borderLeft.rect.size.x;

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
            container.anchoredPosition3D += m_offsetVec * (m_halfSizeContainer - viewRect.rect.size.x * 0.5f);
        }

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
                    DOTween.To(() => scrollView.horizontalScrollbar.value, value => scrollView.horizontalScrollbar.value = value, toValue, time);
                }
#else
                scrollView.horizontalScrollbar.value = 0;
#endif
            }
            else
                scrollView.horizontalScrollbar.value = 0;
        }

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
                    DOTween.To(() => scrollView.horizontalScrollbar.value, value => scrollView.horizontalScrollbar.value = value, toValue, time);
                }
            }
            else
#endif
            {
                scrollView.horizontalScrollbar.value = 1;
            }
        }

        public void RefreshScrollBar()
        {
            ScrollBarChanged(scrollView.horizontalScrollbar.value);
        }

        public void ScrollBarChanged(float normPos)
        {
            if (m_optimizedTotal == 0)
            {
                Debug.LogError("m_OptimizedTotal should not be Zero");
                return;
            }

            normPos += m_rightBarOffset * normPos;
            normPos -= m_leftBarOffset * (1 - normPos);
            normPos = Mathf.Clamp(normPos, 0, 1);

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

        public void Expand(int totalSlot)
        {
            total += totalSlot;
            container.sizeDelta = container.sizeDelta.AddX(totalSlot * m_cellSizeX);
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

            ScrollBarChanged(scrollView.horizontalScrollbar.value);
        }

        private void MoveItemByIndex(RectTransform item, int index)
        {
            item.anchoredPosition3D = m_startPos + m_offsetVec * (index * m_cellSizeX);
        }

        public List<OptimizedScrollItem> GetListItem()
        {
            return m_itemsScrolled;
        }

        public void ScrollToTarget(int index)
        {
            index = Mathf.Clamp(index, 0, total - 1);
            scrollView.StopMovement();

            float contentWidth = container.rect.width;
            float contentPivotX = container.pivot.x;
            float contentAnchoredXMin = contentWidth * (1 - contentPivotX) * -1 + viewRect.rect.width * 0.5f;
            float contentAnchoredXMax = contentWidth * contentPivotX - viewRect.rect.width * 0.5f;

            var prefabRect = prefab.transform as RectTransform;
            float x = contentAnchoredXMax - m_cellSizeX * index + (prefabRect.pivot.x - 0.5f) * prefabRect.rect.width;
            x = Mathf.Clamp(x, contentAnchoredXMin, contentAnchoredXMax);

            container.anchoredPosition = new Vector2(x, container.anchoredPosition.y);
        }

        public void CenterChild(int index)
        {
            index = Mathf.Clamp(index, 0, total - 1);
            scrollView.StopMovement();

            float contentWidth = container.rect.width;
            float contentPivotX = container.pivot.x;
            float contentAnchoredXMin = contentWidth * (1 - contentPivotX) * -1 + viewRect.rect.width * 0.5f;
            float contentAnchoredXMax = contentWidth * contentPivotX - viewRect.rect.width * 0.5f;

            var x = -(m_startPos + m_offsetVec * (index * m_cellSizeX)).x;
            x = Mathf.Clamp(x, contentAnchoredXMin, contentAnchoredXMax);

            container.anchoredPosition = new Vector2(x, container.anchoredPosition.y);
        }

        public int TotalFullCellVisible()
        {
            var rectZero = prefab.GetComponent<RectTransform>();
            var cellSizeX = rectZero.rect.size.x + spacing;
            return Mathf.FloorToInt(viewRect.rect.size.x / cellSizeX);
        }
    }
}
