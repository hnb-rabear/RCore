/***
 * Author RaBear - HNB - 2019
 **/

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RCore.Inspector;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.UI
{
    public class ProgressBar : MonoBehaviour
    {
        public enum FillDirection
        {
            Left,
            Right,
        }

        public Image imgBackground;
        public Image imgProgressValue;
        [FormerlySerializedAs("txtProgessValue")]
        public TextMeshProUGUI txtValue;
        public TextMeshProUGUI txtRank;
        /// <summary>
        /// False: image can fill, 
        /// Tue: image cannot fill, we have to use delta size
        /// </summary>
        public bool fillByBarWidth;
        [Tooltip("If fillByBarWidth = true")] public FillDirection fillByBarWidthDirection;
        /// <summary>
        /// Sometimg we don't fill empty bar as 0%
        /// </summary>
        [Range(0, 1)] public float minFillRatio = 0f;
        /// <summary>
        /// Sometime we don't fill full bar as 100%
        /// </summary>
        [Range(0, 1)] public float maxFillRatio = 1f;
        [SerializeField, Range(0f, 1f)] private float mFill;
        /// <summary>
        /// True: value text will display as countdown timer
        /// </summary>
        public bool isTimeCountdown;
        public bool isPercent;
        [SerializeField] private float mWidthOffset;
        [SerializeField, ReadOnly] private float mValue = -1;
        [SerializeField, ReadOnly] private float mMax = -1;

        private int mRank = -1;
        private Vector2 mBackgroundSize;
        private RectTransform mRectProgressValue;


        public virtual float FillAmount
        {
            get => mFill;
            set
            {
                mFill = maxFillRatio * value;
                if (mFill < minFillRatio && mFill > 0)
                    mFill = minFillRatio;

                FillBar(mFill);
            }
        }
        public virtual float Value
        {
            get => mValue;
            set
            {
                if (mValue != value)
                {
                    mValue = value;
                    if (mMax > 0)
                        FillAmount = mValue / mMax;
                    SetProgressDisplay();
                }
            }
        }
        public virtual float Max
        {
            get => mMax;
            set
            {
                if (mMax != value)
                {
                    mMax = value;
                    if (mMax > 0)
                        FillAmount = mValue / mMax;
                    else
                        FillAmount = 0;
                    SetProgressDisplay();
                }
            }
        }
        public virtual int Rank
        {
            get => mRank;
            set
            {
                if (mRank != value)
                {
                    mRank = value;
                    SetRankDisplay();
                }
                if (txtRank != null)
                    txtRank.enabled = mRank >= 0;
            }
        }

        public Vector2 BarSize()
        {
            return new Vector2(imgBackground.rectTransform.rect.width - mWidthOffset, imgBackground.rectTransform.rect.height);
        }

        protected virtual void FillBar(float pValue)
        {
            if (fillByBarWidth)
            {
                mBackgroundSize = new Vector2(imgBackground.rectTransform.rect.width, imgBackground.rectTransform.rect.height);
                mBackgroundSize.x -= mWidthOffset;
            }

            if (fillByBarWidth)
            {
                if (mRectProgressValue == null)
                    mRectProgressValue = imgProgressValue.rectTransform;
                mRectProgressValue.sizeDelta = new Vector2(mBackgroundSize.x * pValue, mRectProgressValue.sizeDelta.y);
            }
            else
                imgProgressValue.fillAmount = pValue;
        }

        public virtual void Active(bool pValue)
        {
            imgBackground.SetActive(pValue);
            imgProgressValue.SetActive(pValue);
            if (txtValue != null)
                txtValue.SetActive(pValue);
            if (txtRank != null)
                txtRank.gameObject.SetActive(pValue);
        }

        protected virtual void SetProgressDisplay()
        {
            if (txtValue != null)
                txtValue.enabled = mMax >= 0;

            if (isPercent)
            {
                if (mMax > 0 && txtValue != null)
                    txtValue.text = $"{Mathf.RoundToInt(mValue / mMax * 100)}%";
            }
            else if (isTimeCountdown)
            {
                if (mMax > 0 && txtValue != null)
                    txtValue.text = TimeHelper.FormatHHMMss(mMax - mValue, false);
            }
            else
            {
                if (mMax >= 0 && txtValue != null)
                    txtValue.text = $"{mValue}/{mMax}";
            }
        }

        protected virtual void SetRankDisplay()
        {
            if (txtRank != null)
                txtRank.text = mRank.ToString();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            Validate();

            float fill = maxFillRatio * mFill;
            if (fill < minFillRatio)
                fill = minFillRatio;

            FillBar(fill);
        }
#endif

        [ContextMenu("Validate")]
        private void Validate()
        {
            if (imgBackground == null || imgProgressValue == null)
            {
                var imgs = gameObject.GetComponentsInChildren<Image>();
                if (imgs.Length >= 2)
                {
                    imgBackground = imgs[0];
                    imgProgressValue = imgs[1];
                }
            }

            if (txtValue == null)
                txtValue = imgProgressValue.GetComponentInChildren<TextMeshProUGUI>();

            //Max = mMax;
            //= mValue;
            //FillAmount = Value / Max;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ProgressBar))]
        public class ProgressBarEditor : UnityEditor.Editor
        {
            ProgressBar mBar;

            private void OnEnable()
            {
                mBar = target as ProgressBar;
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                if (mBar.txtValue != null)
                    mBar.txtValue.text = EditorGUILayout.TextField("Progress", mBar.txtValue.text);
                if (mBar.txtRank != null)
                    mBar.txtRank.text = EditorGUILayout.TextField("Rank", mBar.txtRank.text);

                if (mBar.fillByBarWidth && mBar.imgBackground != null && mBar.imgProgressValue != null)
                {
                    var barTransform = mBar.imgProgressValue.transform as RectTransform;
                    var pivot = new Vector2(0, 0.5f);
                    switch (mBar.fillByBarWidthDirection)
                    {
                        case FillDirection.Left:
                            pivot = new Vector2(0, 0.5f);
                            break;
                        case FillDirection.Right:
                            pivot = new Vector2(1, 0.5f);
                            break;
                    }
                    var size = barTransform.rect.size;
                    size.x -= mBar.mWidthOffset;
                    var deltaPivot = barTransform.pivot - pivot;
                    var deltaPosition = new Vector3(deltaPivot.x * size.x, deltaPivot.y * size.y);
                    barTransform.pivot = pivot;
                    barTransform.localPosition -= deltaPosition;
                }
            }
        }
#endif
    }
}