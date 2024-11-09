/***
* Author RaBear - HNB - 2019
**/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
#if DOTWEEN
using DG.Tweening;
#endif

namespace RCore.UI
{
    public class TableAlignmentUI : MonoBehaviour, IAligned
    {
        public enum TableLayoutType
        {
            Horizontal,
            Vertical
        }

        public enum Alignment
        {
            Top,
            Bottom,
            Left,
            Right,
            Center,
        }

        [FormerlySerializedAs("tableLayoutType")]
        [SerializeField, Tooltip("Horizontal:L,R,C, Vertical:T,B,C")] private TableLayoutType m_tableLayoutType;
        [FormerlySerializedAs("alignmentType")]
        [SerializeField] private Alignment m_alignmentType;
        [FormerlySerializedAs("reverseY")]
        [SerializeField] private bool m_reverseY = true;
        [FormerlySerializedAs("tweenTime")]
        [SerializeField] private float m_tweenTime = 0.25f;
        [FormerlySerializedAs("animCurve")]
        [SerializeField] private AnimationCurve m_animCurve;

        [Space(10)]
        public int maxRow;

        [Space(10)]
        public int maxColumn;

        [Space(10)]
        public float columnDistance;
        public float rowDistance;

        [Space(10)]
        [SerializeField] private bool m_AutoResizeContentX;
        [SerializeField] private bool m_AutoResizeContentY;
        [SerializeField] private Vector2 m_ContentSizeBonus;
        [SerializeField] private List<Transform> m_IgnoredObjects;

        private float m_width;
        private float m_height;
        private int m_firstGroupIndex;
        private int m_lastGroupIndex;
        private Coroutine m_coroutine;
        private Vector2 m_childTopRight = Vector2.zero;
        private Vector2 m_childBotLeft = Vector2.zero;
        private AnimationCurve m_animCurveTemp;
        private Dictionary<int, List<RectTransform>> childrenGroup;

        private void OnEnable()
        {
            m_childTopRight = Vector2.zero;
            m_childBotLeft = Vector2.zero;
            if (m_AutoResizeContentX || m_AutoResizeContentY)
                AutoResizeContent();
        }
        
        public void Init()
        {
            int totalRow;
            int totalCol;

            var allChildren = new List<RectTransform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.gameObject.activeSelf)
                {
                    if (m_IgnoredObjects.Contains(child))
                        continue;
                    allChildren.Add(transform.GetChild(i) as RectTransform);
                }
            }

            childrenGroup = new Dictionary<int, List<RectTransform>>();
            if (m_tableLayoutType == TableLayoutType.Horizontal)
            {
                if (maxColumn == 0)
                    maxColumn = 1;

                totalRow = Mathf.CeilToInt(allChildren.Count * 1f / maxColumn);
                totalCol = Mathf.CeilToInt(allChildren.Count * 1f / totalRow);
                int row = 0;
                while (allChildren.Count > 0)
                {
                    if (row == 0) m_firstGroupIndex = row;
                    if (row > 0) m_lastGroupIndex = row;
                    for (int i = 0; i < maxColumn; i++)
                    {
                        if (allChildren.Count == 0)
                            break;

                        if (!childrenGroup.ContainsKey(row))
                            childrenGroup.Add(row, new List<RectTransform>());
                        childrenGroup[row].Add(allChildren[0]);
                        allChildren.RemoveAt(0);
                    }
                    row++;
                }
            }
            else
            {
                if (maxRow == 0)
                    maxRow = 1;

                totalCol = Mathf.CeilToInt(allChildren.Count * 1f / maxRow);
                totalRow = Mathf.CeilToInt(allChildren.Count * 1f / totalCol);
                int col = 0;
                while (allChildren.Count > 0)
                {
                    if (col == 0) m_firstGroupIndex = col;
                    if (col > 0) m_lastGroupIndex = col;
                    for (int i = 0; i < maxRow; i++)
                    {
                        if (allChildren.Count == 0)
                            break;

                        if (!childrenGroup.ContainsKey(col))
                            childrenGroup.Add(col, new List<RectTransform>());
                        childrenGroup[col].Add(allChildren[0]);
                        allChildren.RemoveAt(0);
                    }
                    col++;
                }
            }

            m_width = (totalCol - 1) * columnDistance;
            m_height = (totalRow - 1) * rowDistance;
        }

        [InspectorButton]
        public void Align()
        {
            Init();

            if (m_tableLayoutType == TableLayoutType.Horizontal)
            {
                foreach (var a in childrenGroup)
                {
                    var children = a.Value;
                    float y = a.Key * rowDistance;
                    if (m_reverseY) y = -y + m_height;

                    switch (m_alignmentType)
                    {
                        case Alignment.Left:
                            for (int i = 0; i < children.Count; i++)
                            {
                                var pos = i * new Vector3(columnDistance, 0, 0);
                                pos.y = y - m_height / 2f;
                                children[i].anchoredPosition = pos;
                            }
                            break;

                        case Alignment.Right:
                            for (int i = 0; i < children.Count; i++)
                            {
                                var pos = (children.Count - 1 - i) * new Vector3(columnDistance, 0, 0) * -1;
                                pos.y = y - m_height / 2f;
                                children[i].anchoredPosition = pos;
                            }
                            break;

                        case Alignment.Center:
                            for (int i = 0; i < children.Count; i++)
                            {
                                var pos = i * new Vector3(columnDistance, 0, 0);
                                pos.y = y - m_height / 2f;
                                children[i].anchoredPosition = pos;
                            }
                            if (a.Key == m_lastGroupIndex && m_lastGroupIndex != m_firstGroupIndex)
                            {
                                for (int i = 0; i < children.Count; i++)
                                {
                                    var pos = children[i].anchoredPosition;
                                    pos.x -= columnDistance * (maxColumn - 1) / 2f;
                                    children[i].anchoredPosition = pos;
                                }
                            }
                            else
                                for (int i = 0; i < children.Count; i++)
                                {
                                    children[i].anchoredPosition = new Vector3(
                                        children[i].anchoredPosition.x - children[children.Count - 1].anchoredPosition.x / 2,
                                        children[i].anchoredPosition.y);
                                }
                            break;
                    }

                    for (int i = 0; i < children.Count; i++)
                    {
                        var topRight = children[i].TopRight();
                        if (topRight.x > m_childTopRight.x)
                            m_childTopRight.x = topRight.x;
                        if (topRight.y > m_childTopRight.y)
                            m_childTopRight.y = topRight.y;

                        var botLeft = children[i].BotLeft();
                        if (botLeft.x < m_childBotLeft.x)
                            m_childBotLeft.x = botLeft.x;
                        if (botLeft.y < m_childBotLeft.y)
                            m_childBotLeft.y = botLeft.y;
                    }
                }
            }
            else
            {
                foreach (var a in childrenGroup)
                {
                    var children = a.Value;
                    float x = a.Key * columnDistance;

                    switch (m_alignmentType)
                    {
                        case Alignment.Top:
                            for (int i = 0; i < children.Count; i++)
                            {
                                var pos = (children.Count - 1 - i) * new Vector3(0, rowDistance, 0) * -1;
                                pos.x = x - m_width / 2f;
                                children[i].anchoredPosition = pos;
                            }
                            break;

                        case Alignment.Bottom:
                            for (int i = 0; i < children.Count; i++)
                            {
                                var pos = i * new Vector3(0, rowDistance, 0);
                                pos.x = x - m_width / 2f;
                                children[i].anchoredPosition = pos;
                            }
                            break;

                        case Alignment.Center:
                            for (int i = 0; i < children.Count; i++)
                            {
                                var pos = i * new Vector3(0, rowDistance, 0);
                                pos.x = x - m_width / 2f;
                                children[i].anchoredPosition = pos;
                            }
                            if (a.Key == m_lastGroupIndex && m_lastGroupIndex != m_firstGroupIndex)
                            {
                                for (int i = 0; i < children.Count; i++)
                                {
                                    var pos = children[i].anchoredPosition;
                                    pos.y -= rowDistance * (maxRow - 1) / 2f;
                                    children[i].anchoredPosition = pos;
                                }
                            }
                            else
                                for (int i = 0; i < children.Count; i++)
                                {
                                    children[i].anchoredPosition = new Vector3(
                                        children[i].anchoredPosition.x,
                                        children[i].anchoredPosition.y - children[children.Count - 1].anchoredPosition.y / 2);
                                }
                            break;
                    }

                    for (int i = 0; i < children.Count; i++)
                    {
                        var topRight = children[i].TopRight();
                        if (topRight.x > m_childTopRight.x)
                            m_childTopRight.x = topRight.x;
                        if (topRight.y > m_childTopRight.y)
                            m_childTopRight.y = topRight.y;

                        var botLeft = children[i].BotLeft();
                        if (botLeft.x < m_childBotLeft.x)
                            m_childBotLeft.x = botLeft.x;
                        if (botLeft.y < m_childBotLeft.y)
                            m_childBotLeft.y = botLeft.y;
                    }
                }
            }

            if (m_AutoResizeContentX || m_AutoResizeContentY)
                AutoResizeContent();
        }

        public void AutoResizeContent()
        {
            float height = m_childTopRight.y - m_childBotLeft.y + m_ContentSizeBonus.y;
            float width = m_childTopRight.x - m_childBotLeft.x + m_ContentSizeBonus.x;

            var size = (transform as RectTransform).sizeDelta;
            if (m_AutoResizeContentX)
                size.x = width;
            if (m_AutoResizeContentY)
                size.y = height;
            (transform as RectTransform).sizeDelta = size;
        }

        [InspectorButton]
        private void AlignByTweener()
        {
	        AlignByTweener(null);
        }
        
        public void AlignByTweener(Action onFinish, AnimationCurve pCurve = null)
        {
            Init();

            m_animCurveTemp = pCurve ?? this.m_animCurve;
            var initialPositions = new Dictionary<int, Vector3[]>();
            var finalPositions = new Dictionary<int, Vector3[]>();

            if (m_tableLayoutType == TableLayoutType.Horizontal)
            {
                foreach (var a in childrenGroup)
                {
                    var children = a.Value;
                    float y = a.Key * rowDistance;
                    if (m_reverseY) y = -y + m_height;

                    var childrenNewPosition = new Vector3[children.Count];
                    var childrenPrePosition = new Vector3[children.Count];
                    switch (m_alignmentType)
                    {
                        case Alignment.Left:
                            for (int i = 0; i < children.Count; i++)
                            {
                                childrenPrePosition[i] = children[i].anchoredPosition;
                                var pos = i * new Vector3(columnDistance, 0, 0);
                                pos.y = y - m_height / 2f;
                                childrenNewPosition[i] = pos;
                            }
                            break;

                        case Alignment.Right:
                            for (int i = 0; i < children.Count; i++)
                            {
                                childrenPrePosition[i] = children[i].anchoredPosition;
                                var pos = (children.Count - 1 - i) * new Vector3(columnDistance, 0, 0) * -1;
                                pos.y = y - m_height / 2f;
                                childrenNewPosition[i] = pos;
                            }
                            break;

                        case Alignment.Center:
                            for (int i = 0; i < children.Count; i++)
                            {
                                childrenPrePosition[i] = children[i].anchoredPosition;
                                var pos = i * new Vector3(columnDistance, 0, 0);
                                pos.y = y - m_height / 2f;
                                childrenNewPosition[i] = pos;
                            }
                            if (a.Key == m_lastGroupIndex && m_lastGroupIndex != m_firstGroupIndex)
                                for (int i = 0; i < children.Count; i++)
                                {
                                    var pos = childrenNewPosition[i];
                                    pos.x -= columnDistance * (maxColumn - 1) / 2f;
                                    childrenNewPosition[i] = pos;
                                }
                            else
                                for (int i = 0; i < childrenNewPosition.Length; i++)
                                {
                                    childrenNewPosition[i] = new Vector3(
                                        childrenNewPosition[i].x - childrenNewPosition[children.Count - 1].x / 2,
                                        childrenNewPosition[i].y,
                                        childrenNewPosition[i].z);
                                }
                            break;
                    }

                    initialPositions.Add(a.Key, childrenPrePosition);
                    finalPositions.Add(a.Key, childrenNewPosition);
                }
            }
            else
            {
                foreach (var a in childrenGroup)
                {
                    var children = a.Value;
                    float x = a.Key * columnDistance;

                    var childrenPrePosition = new Vector3[children.Count];
                    var childrenNewPosition = new Vector3[children.Count];
                    switch (m_alignmentType)
                    {
                        case Alignment.Top:
                            for (int i = 0; i < children.Count; i++)
                            {
                                childrenPrePosition[i] = children[i].anchoredPosition;
                                var pos = i * new Vector3(0, rowDistance, 0);
                                pos.x = x - m_width / 2f;
                                childrenNewPosition[i] = pos;
                            }
                            break;

                        case Alignment.Bottom:
                            for (int i = 0; i < children.Count; i++)
                            {
                                childrenPrePosition[i] = children[i].anchoredPosition;
                                var pos = (childrenNewPosition.Length - 1 - i) * new Vector3(0, rowDistance, 0) * -1;
                                pos.x = x - m_width / 2f;
                                childrenNewPosition[i] = pos;
                            }
                            break;

                        case Alignment.Center:
                            for (int i = 0; i < children.Count; i++)
                            {
                                childrenPrePosition[i] = children[i].anchoredPosition;
                                var pos = i * new Vector3(0, rowDistance, 0);
                                pos.x = x - m_width / 2f;
                                childrenNewPosition[i] = pos;
                            }
                            if (a.Key == m_lastGroupIndex && m_lastGroupIndex != m_firstGroupIndex)
                            {
                                for (int i = 0; i < children.Count; i++)
                                {
                                    var pos = childrenNewPosition[i];
                                    pos.y -= rowDistance * (maxRow - 1) / 2f;
                                    childrenNewPosition[i] = pos;
                                }
                            }
                            else
                                for (int i = 0; i < childrenNewPosition.Length; i++)
                                {
                                    childrenNewPosition[i] = new Vector3(
                                        childrenNewPosition[i].x,
                                        childrenNewPosition[i].y - childrenNewPosition[childrenNewPosition.Length - 1].y / 2,
                                        childrenNewPosition[i].z);
                                }
                            break;
                    }

                    initialPositions.Add(a.Key, childrenPrePosition);
                    finalPositions.Add(a.Key, childrenNewPosition);
                }
            }

#if DOTWEEN
            float lerp = 0;
            DOTween.Kill(GetInstanceID());
            DOTween.To(val => lerp = val, 0f, 1f, m_tweenTime)
                .OnUpdate(() =>
                {
                    float t = lerp;
                    if (m_animCurveTemp.length > 1)
                        t = m_animCurveTemp.Evaluate(lerp);
                    foreach (var a in childrenGroup)
                    {
                        var children = a.Value;
                        for (int j = 0; j < children.Count; j++)
                        {
                            var pos = Vector2.Lerp(initialPositions[a.Key][j], finalPositions[a.Key][j], t);
                            children[j].anchoredPosition = pos;

                            var topRight = children[j].TopRight();
                            if (topRight.x > m_childTopRight.x)
                                m_childTopRight.x = topRight.x;
                            if (topRight.y > m_childTopRight.y)
                                m_childTopRight.y = topRight.y;

                            var botLeft = children[j].BotLeft();
                            if (botLeft.x < m_childBotLeft.x)
                                m_childBotLeft.x = botLeft.x;
                            if (botLeft.y < m_childBotLeft.y)
                                m_childBotLeft.y = botLeft.y;

                            if (m_AutoResizeContentX || m_AutoResizeContentY)
                                AutoResizeContent();
                        }
                    }
                })
                .OnComplete(() =>
				{
					onFinish?.Invoke();
				});

#else
            StartCoroutine(IEArrangeChildren(childrenGroup, initialPositions, finalPositions, m_tweenTime, onFinish));
#endif
        }

        private IEnumerator IEArrangeChildren(Dictionary<int, List<RectTransform>> p_childrenGroup, Dictionary<int, Vector3[]> p_initialPositions, Dictionary<int, Vector3[]> p_finalPositions, float p_pDuration, Action p_pOnCompleted)
        {
            float time = 0;
            while (true)
            {
                if (time >= p_pDuration)
                    time = p_pDuration;
                float lerp = time / p_pDuration;
                float t = lerp;

                if (m_animCurveTemp.length > 1)
                    t = m_animCurveTemp.Evaluate(lerp);

                foreach (var a in p_childrenGroup)
                {
                    var children = a.Value;
                    for (int j = 0; j < children.Count; j++)
                    {
                        var pos = Vector2.Lerp(p_initialPositions[a.Key][j], p_finalPositions[a.Key][j], t);
                        children[j].anchoredPosition = pos;

                        var topRight = children[j].TopRight();
                        if (topRight.x > m_childTopRight.x)
                            m_childTopRight.x = topRight.x;
                        if (topRight.y > m_childTopRight.y)
                            m_childTopRight.y = topRight.y;

                        var botLeft = children[j].BotLeft();
                        if (botLeft.x < m_childBotLeft.x)
                            m_childBotLeft.x = botLeft.x;
                        if (botLeft.y < m_childBotLeft.y)
                            m_childBotLeft.y = botLeft.y;

                        if (m_AutoResizeContentX || m_AutoResizeContentY)
                            AutoResizeContent();
                    }
                }

                if (lerp >= 1)
                    break;
                yield return null;
                time += Time.deltaTime;
            }

            p_pOnCompleted?.Invoke();
        }
    }
}