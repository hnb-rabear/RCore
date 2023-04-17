/**
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
using Debug = RCore.Common.Debug;

namespace RCore.Components
{
    public class OptimizedVerticalScrollView : MonoBehaviour
    {
        public ScrollRect scrollView;
        public RectTransform container;
        public OptimizedScrollItem prefab;
        public int total = 1;
        public float spacing;
        public int totalCellOnRow = 1;
        public RectTransform content => scrollView.content;

        private int m_TotalVisible;
        private int m_TotalBuffer = 2;
        private float m_HalftSizeContainer;
        private float m_PrefabSizeY;
        private float m_PrefabSizeX;

        private List<RectTransform> m_ListItemRect = new List<RectTransform>();
        private List<OptimizedScrollItem> m_ListItem = new List<OptimizedScrollItem>();
        private int m_OptimizedTotal = 0;
        private Vector3 m_StartPos;
        private Vector3 m_OffsetVec;
        private Vector2 m_Pivot;

        //Advance settings, in case the height of View is flexible
        [Separator("Advance Settings")]
        public bool autoMatchHeight;
        public float minViewHeight;
        public float maxViewHeight;
        public float scrollHeight => (scrollView.transform as RectTransform).sizeDelta.y;

        private void Start()
        {
            scrollView.verticalScrollbar.onValueChanged.AddListener(ScrollBarChanged);

            //Initialize(total, true);
        }

        public void Init(OptimizedScrollItem pPrefab, int pTotalItems, bool pForce)
        {
            prefab = pPrefab;
            m_ListItem.Free();
            for (int i = 0; i < m_ListItem.Count; i++)
                m_ListItem[i].ResetIndex();

            Init(pTotalItems, pForce);
        }

        public void Init(int pTotalItems, bool pForce)
        {
            if (pTotalItems == total && !pForce)
                return;

            m_TotalBuffer = 2;
            m_ListItemRect = new List<RectTransform>();

            if (m_ListItem == null || m_ListItem.Count == 0)
            {
                m_ListItem = new List<OptimizedScrollItem>();
                m_ListItem.Prepare(prefab, container.parent, 5);
            }
            else
                m_ListItem.Free(container);

            total = pTotalItems;

            container.anchoredPosition3D = new Vector3(0, 0, 0);

            var rectZero = m_ListItem[0].GetComponent<RectTransform>();
            Vector2 prefabScale = rectZero.rect.size;
            m_PrefabSizeY = prefabScale.y + spacing;
            if (totalCellOnRow > 1)
                m_PrefabSizeX = prefabScale.x + spacing;
            else
                m_PrefabSizeX = prefabScale.x;
            m_Pivot = rectZero.pivot;

            container.sizeDelta = new Vector2(m_PrefabSizeX * totalCellOnRow, m_PrefabSizeY * Mathf.CeilToInt(total * 1f / totalCellOnRow));
            m_HalftSizeContainer = container.rect.size.y * 0.5f;

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

            var viewport = scrollView.viewport;
            m_TotalVisible = Mathf.CeilToInt(viewport.rect.size.y / m_PrefabSizeY) * totalCellOnRow;
            m_TotalBuffer *= totalCellOnRow;

            m_OffsetVec = Vector3.down;
            m_StartPos = container.anchoredPosition3D - (m_OffsetVec * m_HalftSizeContainer) + (m_OffsetVec * (prefabScale.y * 0.5f));
            m_OptimizedTotal = Mathf.Min(total, m_TotalVisible + m_TotalBuffer);

            for (int i = 0; i < m_OptimizedTotal; i++)
            {
                int cellIndex = i % totalCellOnRow;
                int rowIndex = Mathf.FloorToInt(i * 1f / totalCellOnRow);

                OptimizedScrollItem item = m_ListItem.Obtain(container);
                RectTransform rt = item.transform as RectTransform;
                rt.anchoredPosition3D = m_StartPos + (m_OffsetVec * rowIndex * m_PrefabSizeY);
                rt.anchoredPosition3D = new Vector3(-container.rect.size.x / 2 + cellIndex * m_PrefabSizeX + m_PrefabSizeX * 0.5f,
                    rt.anchoredPosition3D.y,
                    rt.anchoredPosition3D.z);
                m_ListItemRect.Add(rt);

                item.SetActive(true);
                item.UpdateContent(i);
            }

            prefab.gameObject.SetActive(false);
            container.anchoredPosition3D += m_OffsetVec * (m_HalftSizeContainer - (viewport.rect.size.y * 0.5f));
        }

        public void MoveToTop()
        {
            scrollView.StopMovement();
            scrollView.verticalScrollbar.value = 1;
            ScrollBarChanged(1f);
        }

        public void MoveToBot()
        {
            scrollView.StopMovement();
            scrollView.verticalScrollbar.value = 0;
            ScrollBarChanged(0);
        }

        public void ScrollBarChanged(float pNormPos)
        {
            if (m_OptimizedTotal <= 0)
                return;

            if (totalCellOnRow > 1)
                pNormPos = 1f - pNormPos + 0.06f;
            else
                pNormPos = 1f - pNormPos;
            if (pNormPos > 1)
                pNormPos = 1;

            int numOutOfView = Mathf.CeilToInt(pNormPos * (total - m_TotalVisible));   //number of elements beyond the left boundary (or top)
            int firstIndex = Mathf.Max(0, numOutOfView - m_TotalBuffer);   //index of first element beyond the left boundary (or top)
            int originalIndex = firstIndex % m_OptimizedTotal;


            int newIndex = firstIndex;
            for (int i = originalIndex; i < m_OptimizedTotal; i++)
            {
                MoveItemByIndex(m_ListItemRect[i], newIndex);
                m_ListItem[i].UpdateContent(newIndex);
                newIndex++;
            }
            for (int i = 0; i < originalIndex; i++)
            {
                MoveItemByIndex(m_ListItemRect[i], newIndex);
                m_ListItem[i].UpdateContent(newIndex);
                newIndex++;
            }
        }

        private void MoveItemByIndex(RectTransform item, int index)
        {
            int cellIndex = index % totalCellOnRow;
            int rowIndex = Mathf.FloorToInt(index * 1f / totalCellOnRow);
            item.anchoredPosition3D = m_StartPos + (m_OffsetVec * rowIndex * m_PrefabSizeY);
            item.anchoredPosition3D = new Vector3(-container.rect.size.x / 2 + cellIndex * m_PrefabSizeX + m_PrefabSizeX * 0.5f,
                   item.anchoredPosition3D.y,
                   item.anchoredPosition3D.z);
            //Debug.Log(item.anchoredPosition3D);
        }

        public List<OptimizedScrollItem> GetListItem()
        {
            return m_ListItem;
        }

        public void MoveToTargetIndex(int pIndex, bool pTween = false)
        {
            if (pIndex < 0)
                pIndex = 0;
            if (pIndex >= total)
                pIndex = total - 1;

            int rowIndex = Mathf.FloorToInt(pIndex * 1f / totalCellOnRow);

            float contentHeight = content.rect.height;
            float viewHeight = scrollView.viewport.rect.height;
            float scrollLength = contentHeight - viewHeight;
            float targetPosition = rowIndex * m_PrefabSizeY;

            float offsetY = m_PrefabSizeY * (0.5f - m_Pivot.y);
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

                for (int i = 0; i < m_ListItem.Count; i++)
                    m_ListItem[i].ResetIndex();

                ScrollBarChanged(1 - (pIndex + 1f) / total);
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
#if USE_LEANTWEEN
                LeanTween.value(gameObject, fromY, toY, time)
                    .setOnUpdate((float val) =>
                    {
                        content.anchoredPosition = new Vector2(0, val);
                    });
#endif
#if USE_DOTWEEN
                float val = fromY;
                DOTween.To(() => val, x => val = x, toY, time)
                    .OnStart(() =>
                    {
                        scrollView.vertical = false;
                    })
                    .OnUpdate(() =>
                    {
                        content.anchoredPosition = new Vector2(0, val);
                    })
                    .OnComplete(() =>
                    {
                        scrollView.vertical = true;
                        content.anchoredPosition = new Vector2(0, toY);
                    });
                //NOTE: below method is not work correctly
                //content.DOLocalMoveY(toY, time, true);
#endif
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(OptimizedVerticalScrollView))]
        public class OptimizedVerticalScrollViewEditor : Editor
        {
            private OptimizedVerticalScrollView mScript;
            private int mTestIndex;

            private void OnEnable()
            {
                mScript = (OptimizedVerticalScrollView)target;
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                EditorHelper.SeparatorBox();
                mTestIndex = EditorHelper.IntField(mTestIndex, "Test Index");

                if (EditorHelper.Button("Move To Top"))
                    mScript.MoveToTop();
                if (EditorHelper.Button("Move To Bot"))
                    mScript.MoveToBot();
                if (EditorHelper.Button("Move To Index"))
                    mScript.MoveToTargetIndex(mTestIndex, Application.isPlaying);
            }
        }
#endif
    }
}