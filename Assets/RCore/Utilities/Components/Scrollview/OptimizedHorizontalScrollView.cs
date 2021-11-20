/**
 * Author NBear - nbhung71711 @gmail.com - 2017
 **/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RCore.Components
{
    public class OptimizedHorizontalScrollView : MonoBehaviour
    {
        public ScrollRect scrollView;
        public RectTransform container;
        public RectTransform prefab;
        public int total = 1;
        public float spacing;

        private int mTotalVisible;
        private int mTotalBuffer = 2;
        private float mHalftSizeContainer;
        private float mPrefabSize;

        private List<RectTransform> mListItemRect = new List<RectTransform>();
        private List<OptimizedScrollItem> mListItem = new List<OptimizedScrollItem>();
        private int mOptimizedTotal = 0;
        private Vector3 mStartPos;
        private Vector3 mOffsetVec;
        private bool mInitialized;

        private void Start()
        {
            Initialize(total);
        }

        public void Initialize(int pTotalItems)
        {
            if (mInitialized && total == pTotalItems)
                return;

            mInitialized = true;
            total = pTotalItems;

            container.anchoredPosition3D = new Vector3(0, 0, 0);

            Vector2 prefabScale = prefab.rect.size;
            mPrefabSize = prefabScale.x + spacing;

            container.sizeDelta = new Vector2(mPrefabSize * total, prefabScale.y);
            mHalftSizeContainer = container.rect.size.x * 0.5f;

            var viewport = scrollView.viewport;
            mTotalVisible = Mathf.CeilToInt(viewport.rect.size.x / mPrefabSize);

            mOffsetVec = Vector3.right;
            mStartPos = container.anchoredPosition3D - (mOffsetVec * mHalftSizeContainer) + (mOffsetVec * (prefabScale.x * 0.5f));
            mOptimizedTotal = Mathf.Min(total, mTotalVisible + mTotalBuffer);
            for (int i = 0; i < mOptimizedTotal; i++)
            {
                GameObject obj = Instantiate(prefab.gameObject, container.transform);
                RectTransform rt = obj.transform as RectTransform;
                rt.anchoredPosition3D = mStartPos + (mOffsetVec * i * mPrefabSize);
                mListItemRect.Add(rt);
                obj.SetActive(true);

#if UNITY_2019_2_OR_NEWER
                obj.TryGetComponent(out OptimizedScrollItem item);
#else
                OptimizedScrollItem item = obj.GetComponent<OptimizedScrollItem>();
#endif
                mListItem.Add(item);
                item.UpdateContent(i);
            }

            prefab.gameObject.SetActive(false);
            container.anchoredPosition3D += mOffsetVec * (mHalftSizeContainer - (viewport.rect.size.x * 0.5f));

            scrollView.horizontalScrollbar.onValueChanged.AddListener(ScrollBarChanged);
        }

        public void ScrollBarChanged(float pNormPos)
        {
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
            int id = item.GetInstanceID();
            item.anchoredPosition3D = mStartPos + (mOffsetVec * index * mPrefabSize);
        }

        public List<OptimizedScrollItem> GetListItem()
        {
            return mListItem;
        }
    }
}