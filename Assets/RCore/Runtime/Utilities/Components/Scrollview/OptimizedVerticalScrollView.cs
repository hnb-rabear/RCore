/***
 * Author RadBear - nbhung71711@gmail.com - 2017
 **/

#if USE_DOTWEEN
using DG.Tweening;
#endif
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RCore.Common;
using RCore.Inspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Components
{
    public class OptimizedVerticalScrollView : MonoBehaviour
    {
        public ScrollRect scrollView;
        public RectTransform container;
        public RectTransform viewRect;
        public OptimizedScrollItem prefab;
        public int total = 1;
        public float spacing;
        public int totalCellOnRow = 1;
        public RectTransform content => scrollView.content;

        private int m_TotalVisible;
        private int m_TotalBuffer = 2;
        private float m_HalfSizeContainer;
        private float m_CellSizeY;
        private float m_PrefabSizeX;

        private List<RectTransform> m_ItemsRect = new List<RectTransform>();
        private List<OptimizedScrollItem> m_ItemsScrolled = new List<OptimizedScrollItem>();
        private int m_OptimizedTotal = 0;
        private Vector3 m_StartPos;
        private Vector3 m_OffsetVec;
        private Vector2 m_Pivot;

        //Advance settings, in case the height of View is flexible
        [Separator("Advance Settings")]
        public bool autoMatchHeight;
        public float minViewHeight;
        public float maxViewHeight;
        public float scrollHeight => ((RectTransform)scrollView.transform).sizeDelta.y;

        private void Start()
        {
            scrollView.onValueChanged.AddListener(ScrollBarChanged);
        }

        public void Init(OptimizedScrollItem pPrefab, int pTotalItems, bool pForce)
        {
            prefab = pPrefab;
			m_ItemsScrolled.Free();
            for (int i = 0; i < m_ItemsScrolled.Count; i++)
				m_ItemsScrolled[i].Refresh();

            Init(pTotalItems, pForce);
        }

        private void LateUpdate()
        {
            for (int i = 0; i < m_ItemsScrolled.Count; i++)
                m_ItemsScrolled[i].ManualUpdate();
        }

        public void Init(int pTotalItems, bool pForced)
        {
            if (pTotalItems == total && !pForced)
                return;

            m_TotalBuffer = 2;
            m_ItemsRect = new List<RectTransform>();

            if (m_ItemsScrolled == null || m_ItemsScrolled.Count == 0)
            {
                m_ItemsScrolled = new List<OptimizedScrollItem>();
                m_ItemsScrolled.Prepare(prefab, container.parent, 5);
            }
            else
                m_ItemsScrolled.Free(container);

            total = pTotalItems;

            container.anchoredPosition3D = new Vector3(0, 0, 0);

            var rectZero = m_ItemsScrolled[0].GetComponent<RectTransform>();
            var prefabScale = rectZero.rect.size;
            m_CellSizeY = prefabScale.y + spacing;
            if (totalCellOnRow > 1)
                m_PrefabSizeX = prefabScale.x + spacing;
            else m_PrefabSizeX = prefabScale.x;
            m_Pivot = rectZero.pivot;

            container.sizeDelta = new Vector2(m_PrefabSizeX * totalCellOnRow, m_CellSizeY * Mathf.CeilToInt(total * 1f / totalCellOnRow));
            m_HalfSizeContainer = container.rect.size.y * 0.5f;

            var scrollRect = scrollView.transform as RectTransform;

            //Re Update min-max view size
            if (autoMatchHeight)
            {
                float preferHeight = container.rect.size.y + spacing * 2;
                if (maxViewHeight > 0 && preferHeight > maxViewHeight)
                    preferHeight = maxViewHeight;
                else if (minViewHeight > 0 && preferHeight < minViewHeight)
                    preferHeight = minViewHeight;

                var size = scrollRect.rect.size;
                size.y = preferHeight;
                scrollRect.sizeDelta = size;
            }

			viewRect ??= scrollView.viewport;
            m_TotalVisible = Mathf.CeilToInt(viewRect.rect.size.y / m_CellSizeY) * totalCellOnRow;
            m_TotalBuffer *= totalCellOnRow;

            m_OffsetVec = Vector3.down;
            m_StartPos = container.anchoredPosition3D - m_OffsetVec * m_HalfSizeContainer + m_OffsetVec * (prefabScale.y * 0.5f);
            m_OptimizedTotal = Mathf.Min(total, m_TotalVisible + m_TotalBuffer);

            for (int i = 0; i < m_OptimizedTotal; i++)
            {
                int cellIndex = i % totalCellOnRow;
                int rowIndex = Mathf.FloorToInt(i * 1f / totalCellOnRow);

                var item = m_ItemsScrolled.Obtain(container);
                var rt = item.transform as RectTransform;
                rt.anchoredPosition3D = m_StartPos + m_OffsetVec * rowIndex * m_CellSizeY;
                rt.anchoredPosition3D = new Vector3(-container.rect.size.x / 2 + cellIndex * m_PrefabSizeX + m_PrefabSizeX * 0.5f,
                    rt.anchoredPosition3D.y,
                    rt.anchoredPosition3D.z);
                m_ItemsRect.Add(rt);

                item.SetActive(true);
                item.UpdateContent(i, true);
            }

            prefab.gameObject.SetActive(false);
            container.anchoredPosition3D += m_OffsetVec * (m_HalfSizeContainer - viewRect.rect.size.y * 0.5f);
        }

        public void MoveToTop()
        {
            scrollView.StopMovement();
            scrollView.verticalScrollbar.value = 1;
        }

        public void MoveToBot()
        {
            scrollView.StopMovement();
            scrollView.verticalScrollbar.value = 0;
        }

        public void ScrollBarChanged(Vector2 pNormPos)
        {
            if (m_OptimizedTotal <= 0)
                return;

            if (totalCellOnRow > 1)
                pNormPos.y = 1f - pNormPos.y + 0.06f;
            else
                pNormPos.y = 1f - pNormPos.y;
            if (pNormPos.y > 1)
                pNormPos.y = 1;

            int numOutOfView = Mathf.CeilToInt(pNormPos.y * (total - m_TotalVisible));   //number of elements beyond the left boundary (or top)
            int firstIndex = Mathf.Max(0, numOutOfView - m_TotalBuffer);   //index of first element beyond the left boundary (or top)
            int originalIndex = firstIndex % m_OptimizedTotal;


            int newIndex = firstIndex;
            for (int i = originalIndex; i < m_OptimizedTotal; i++)
            {
                MoveItemByIndex(m_ItemsRect[i], newIndex);
                m_ItemsScrolled[i].UpdateContent(newIndex, false);
                newIndex++;
            }
            for (int i = 0; i < originalIndex; i++)
            {
                MoveItemByIndex(m_ItemsRect[i], newIndex);
                m_ItemsScrolled[i].UpdateContent(newIndex, false);
                newIndex++;
            }
        }

        private void MoveItemByIndex(RectTransform item, int index)
        {
            int cellIndex = index % totalCellOnRow;
            int rowIndex = Mathf.FloorToInt(index * 1f / totalCellOnRow);
            item.anchoredPosition3D = m_StartPos + m_OffsetVec * rowIndex * m_CellSizeY;
            item.anchoredPosition3D = new Vector3(-container.rect.size.x / 2 + cellIndex * m_PrefabSizeX + m_PrefabSizeX * 0.5f,
                   item.anchoredPosition3D.y,
                   item.anchoredPosition3D.z);
        }

        public List<OptimizedScrollItem> GetListItem()
        {
            return m_ItemsScrolled;
        }

        public void MoveToTargetIndex(int pIndex, bool pTween = false)
        {
            int rowIndex = Mathf.FloorToInt(pIndex * 1f / totalCellOnRow);

            float contentHeight = content.rect.height;
            float viewHeight = scrollView.viewport.rect.height;
            float scrollLength = contentHeight - viewHeight;
            float targetPosition = rowIndex * m_CellSizeY;

            float offsetY = m_CellSizeY * (0.5f - m_Pivot.y);
            targetPosition -= offsetY;

            if (targetPosition > scrollLength)
                targetPosition = scrollLength;

            scrollView.StopMovement();

            float fromY = content.anchoredPosition.y;
            float toY = -(scrollLength / 2 - targetPosition);
            toY += contentHeight * (content.pivot.y - 0.5f);
            if (!pTween)
            {
                content.anchoredPosition = new Vector2(0, toY);

                for (int i = 0; i < m_ItemsScrolled.Count; i++)
					m_ItemsScrolled[i].Refresh();
            }
            else
            {
                float time = Mathf.Abs(fromY - toY) / 5000f;
                if (time == 0)
                    content.anchoredPosition = new Vector2(0, toY);
                else if (time < 0.04f)
                    time = 0.04f;
                else if (time > 0.4f)
                    time = 0.4f;

                content.anchoredPosition = new Vector2(0, fromY);
#if USE_DOTWEEN
                float val = fromY;
                DOTween.To(() => val, x => val = x, toY, time)
                    .OnUpdate(() =>
                    {
                        content.anchoredPosition = new Vector2(0, val);
                    })
                    .OnComplete(() =>
                    {
                        content.anchoredPosition = new Vector2(0, toY);
                    });
                //NOTE: below method is not work correctly
                //content.DOLocalMoveY(toY, time, true);
#endif
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(OptimizedVerticalScrollView))]
        public class OptimizedVerticalScrollViewEditor : UnityEditor.Editor
        {
            private OptimizedVerticalScrollView mScript;

            private void OnEnable()
            {
                mScript = (OptimizedVerticalScrollView)target;
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                if (EditorHelper.Button("Move To Top"))
                    mScript.MoveToTop();
                if (EditorHelper.Button("Move To Top 2"))
                    mScript.MoveToTargetIndex(0, false);
                if (EditorHelper.Button("Move To Top 3"))
                    mScript.MoveToTargetIndex(0, true);
            }
        }
#endif
    }
}