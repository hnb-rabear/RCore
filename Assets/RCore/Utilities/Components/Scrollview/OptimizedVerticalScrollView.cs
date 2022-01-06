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

        private int mTotalVisible;
        private int mTotalBuffer = 2;
        private float mHalftSizeContainer;
        private float mPrefabSizeY;
        private float mPrefabSizeX;

        private List<RectTransform> mListItemRect = new List<RectTransform>();
        private List<OptimizedScrollItem> mListItem = new List<OptimizedScrollItem>();
        private int mOptimizedTotal = 0;
        private Vector3 mStartPos;
        private Vector3 mOffsetVec;
        private Vector2 mPivot;

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

            Init(pTotalItems, pForce);
        }

        public void Init(int pTotalItems, bool pForce)
        {
            if (pTotalItems == total && !pForce)
                return;

            mListItemRect = new List<RectTransform>();

            if (mListItem == null || mListItem.Count == 0)
            {
                mListItem = new List<OptimizedScrollItem>();
                mListItem.Prepare(prefab, container.parent, 5);
            }
            else
                mListItem.Free(container);

            total = pTotalItems;

            container.anchoredPosition3D = new Vector3(0, 0, 0);

            var rectZero = mListItem[0].GetComponent<RectTransform>();
            Vector2 prefabScale = rectZero.rect.size;
            mPrefabSizeY = prefabScale.y + spacing;
            if (totalCellOnRow > 1)
                mPrefabSizeX = prefabScale.x + spacing;
            else
                mPrefabSizeX = prefabScale.x;
            mPivot = rectZero.pivot;

            container.sizeDelta = new Vector2(prefabScale.x * totalCellOnRow, mPrefabSizeY * Mathf.CeilToInt(total * 1f / totalCellOnRow));
            mHalftSizeContainer = container.rect.size.y * 0.5f;

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
            mTotalVisible = Mathf.CeilToInt(viewport.rect.size.y / mPrefabSizeY) * totalCellOnRow;
            mTotalBuffer *= totalCellOnRow;

            mOffsetVec = Vector3.down;
            mStartPos = container.anchoredPosition3D - (mOffsetVec * mHalftSizeContainer) + (mOffsetVec * (prefabScale.y * 0.5f));
            mOptimizedTotal = Mathf.Min(total, mTotalVisible + mTotalBuffer);

            for (int i = 0; i < mOptimizedTotal; i++)
            {
                int cellIndex = i % totalCellOnRow;
                int rowIndex = Mathf.FloorToInt(i * 1f / totalCellOnRow);

                OptimizedScrollItem item = mListItem.Obtain(container);
                RectTransform rt = item.transform as RectTransform;
                rt.anchoredPosition3D = mStartPos + (mOffsetVec * rowIndex * mPrefabSizeY);
                rt.anchoredPosition3D = new Vector3(-container.rect.size.x / 2 + cellIndex * mPrefabSizeX + mPrefabSizeX * 0.5f,
                    rt.anchoredPosition3D.y,
                    rt.anchoredPosition3D.z);
                mListItemRect.Add(rt);

                item.SetActive(true);
                item.UpdateContent(i);
            }

            prefab.gameObject.SetActive(false);
            container.anchoredPosition3D += mOffsetVec * (mHalftSizeContainer - (viewport.rect.size.y * 0.5f));
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
            if (mOptimizedTotal <= 0)
                return;

            if (totalCellOnRow > 1)
                pNormPos = 1f - pNormPos + 0.06f;
            else
                pNormPos = 1f - pNormPos;
            if (pNormPos > 1)
                pNormPos = 1;

            int numOutOfView = Mathf.CeilToInt(pNormPos * (total - mTotalVisible));   //number of elements beyond the left boundary (or top)
            int firstIndex = Mathf.Max(0, numOutOfView - mTotalBuffer);   //index of first element beyond the left boundary (or top)
            int originalIndex = firstIndex % mOptimizedTotal;


            int newIndex = firstIndex;
            for (int i = originalIndex; i < mOptimizedTotal; i++)
            {
                MoveItemByIndex(mListItemRect[i], newIndex);
                mListItem[i].UpdateContent(newIndex);
                newIndex++;
            }
            for (int i = 0; i < originalIndex; i++)
            {
                MoveItemByIndex(mListItemRect[i], newIndex);
                mListItem[i].UpdateContent(newIndex);
                newIndex++;
            }
        }

        private void MoveItemByIndex(RectTransform item, int index)
        {
            int cellIndex = index % totalCellOnRow;
            int rowIndex = Mathf.FloorToInt(index * 1f / totalCellOnRow);
            item.anchoredPosition3D = mStartPos + (mOffsetVec * rowIndex * mPrefabSizeY);
            item.anchoredPosition3D = new Vector3(-container.rect.size.x / 2 + cellIndex * mPrefabSizeX + mPrefabSizeX * 0.5f,
                   item.anchoredPosition3D.y,
                   item.anchoredPosition3D.z);
            //Debug.Log(item.anchoredPosition3D);
        }

        public List<OptimizedScrollItem> GetListItem()
        {
            return mListItem;
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
            float targetPosition = rowIndex * mPrefabSizeY;

            float offsetY = mPrefabSizeY * (0.5f - mPivot.y);
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

                EditorHelper.SeperatorBox();
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