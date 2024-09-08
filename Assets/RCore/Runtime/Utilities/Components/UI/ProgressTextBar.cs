using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RCore.Common;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Components
{
    public class ProgressTextBar : MonoBehaviour
    {
        public Image imgBackground;
        public Image imgProgressValue;
        /// <summary>
        /// This component has two layers because there is a case that we want two texts with different colors, 
        /// one above and have same color with progress bar 
        /// one below and have different color with progess bar
        /// Two Layer must be same size and same pivot
        /// NOTE: progress bar must has mask and fill by width not image filling
        /// </summary>
        public TextMeshProUGUI txtProgessValueLayer1; //This Layer is beloow progress bar and will be hidden when progress bar filled
        public TextMeshProUGUI txtProgessValueLayer2; //This layer is child of progress bar will be affected by mask of progress bar
        public TextMeshProUGUI txtRank;
        /// <summary>
        /// False: image can fill, 
        /// Tue: image cannot fill, we have to use delta size
        /// </summary>
        public bool fillByBarWidth;
        /// <summary>
        /// Sometimg we don't fill empty bar as 0%
        /// </summary>
        [Range(0, 1)]
        public float minFillRatio = 0f;
        /// <summary>
        /// Sometime we don't fill full bar as 100%
        /// </summary>
        [Range(0, 1)]
        public float maxFillRatio = 1f;
        /// <summary>
        /// True: value text will display as countdown timer
        /// </summary>
        public bool isTimeCountdown;
        [SerializeField] private float mValue = -1;
        [SerializeField] private float mMax = -1;
        [SerializeField] private float mWidthOffset;

        private int mRank = -1;
        private Vector2 mBackgroundSize;
        private RectTransform mRectProgressValue;

        public virtual float FillAmount
        {
            get => mValue / mMax;
            set
            {
                float fill = maxFillRatio * value;
                if (fill < minFillRatio && fill > 0)
                    fill = minFillRatio;

                FillBar(fill);
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

        private void Start()
        {
        }

        private void Update()
        {
            if (fillByBarWidth)
            {
                if (txtProgessValueLayer2 != null && txtProgessValueLayer2 != txtProgessValueLayer1 && txtProgessValueLayer2 != txtRank)
                    txtProgessValueLayer2.transform.position = txtProgessValueLayer1.transform.position;
                else
                    enabled = false;
            }
            else
                enabled = false;
        }

        private void FillBar(float pValue)
        {
            if (fillByBarWidth)
            {
                mBackgroundSize = new Vector2(imgBackground.rectTransform.rect.width, imgBackground.rectTransform.rect.height);
                mBackgroundSize.x -= mWidthOffset;
                if (mRectProgressValue == null)
                    mRectProgressValue = imgProgressValue.rectTransform;
                if (txtProgessValueLayer2 != null && txtProgessValueLayer2 != txtProgessValueLayer1 && txtProgessValueLayer2 != txtRank)
                    txtProgessValueLayer2.transform.position = txtProgessValueLayer1.transform.position;
            }

            if (fillByBarWidth)
                mRectProgressValue.sizeDelta = new Vector2(mBackgroundSize.x * pValue, mRectProgressValue.sizeDelta.y);
            else
                imgProgressValue.fillAmount = pValue;
        }

        public virtual void Active(bool pValue)
        {
            imgBackground.SetActive(pValue);
            imgProgressValue.SetActive(pValue);
            txtProgessValueLayer1.SetActive(pValue);
            txtProgessValueLayer2.SetActive(pValue);
            if (txtRank != null) txtRank.gameObject.SetActive(pValue);
        }

        protected virtual void SetProgressDisplay()
        {
            if (!isTimeCountdown)
            {
                if (txtProgessValueLayer1 != null)
                    txtProgessValueLayer1.enabled = mMax >= 0;
                if (txtProgessValueLayer2 != null)
                    txtProgessValueLayer2.enabled = mMax >= 0;
                if (mMax > 0)
                {
                    if (txtProgessValueLayer1 != null)
                        txtProgessValueLayer1.text = $"{mValue}/{mMax}";
                    if (txtProgessValueLayer2 != null)
                        txtProgessValueLayer2.text = $"{mValue}/{mMax}";
                }
            }
            else
            {
                if (txtProgessValueLayer1 != null)
                    txtProgessValueLayer1.text = TimeHelper.FormatHHMMss(mMax - mValue, false);
                if (txtProgessValueLayer2 != null)
                    txtProgessValueLayer2.text = TimeHelper.FormatHHMMss(mMax - mValue, false);
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
            Validate();
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

            if (txtProgessValueLayer1 == null)
            {
                var text = imgProgressValue.GetComponentInChildren<TextMeshProUGUI>();
                txtProgessValueLayer1 = text;
            }

            if (txtProgessValueLayer2 != null && txtProgessValueLayer2 != txtProgessValueLayer1 && txtProgessValueLayer2 != txtRank)
            {
                txtProgessValueLayer2.rectTransform.sizeDelta = new Vector2(txtProgessValueLayer1.rectTransform.rect.width, txtProgessValueLayer1.rectTransform.rect.height);
                txtProgessValueLayer2.transform.position = txtProgessValueLayer1.transform.position;
                //SetPivot(txtProgessValueLayer2.rectTransform, txtProgessValueLayer1.rectTransform.pivot);
            }

            //Max = mMax;
            //= mValue;
            FillAmount = Value / Max;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ProgressTextBar))]
        public class ProgressTextBarEditor : UnityEditor.Editor
        {
            ProgressTextBar mBar;

            private void OnEnable()
            {
                mBar = target as ProgressTextBar;
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                if (mBar.txtProgessValueLayer1 != null)
                    mBar.txtProgessValueLayer1.text = EditorGUILayout.TextField("Progress", mBar.txtProgessValueLayer1.text);
                if (mBar.txtProgessValueLayer2 != null)
                    mBar.txtProgessValueLayer2.text = EditorGUILayout.TextField("Progress", mBar.txtProgessValueLayer2.text);
                if (mBar.txtRank != null)
                    mBar.txtRank.text = EditorGUILayout.TextField("Rank", mBar.txtRank.text);

                if (mBar.fillByBarWidth && mBar.imgBackground != null && mBar.imgProgressValue != null)
                {
                    var barTransform = mBar.imgProgressValue.transform as RectTransform;
                    var pivot = new Vector2(0, 0.5f);
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