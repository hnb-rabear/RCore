using RevCore.Inspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RevCore.UI
{
	public class ProgressBar : MonoBehaviour
	{
		public enum FillDirection
		{
			Left,
			Right,
			Top,
			Bottom
		}

		public Image imgBackground;
		public Image imgProgressValue;
		public TextMeshProUGUI txtValue;
		public TextMeshProUGUI txtRank;
		public bool fillByBarSize;
		[Tooltip("If fillByBarSize = true")] public FillDirection fillDirection;
		[Range(0, 1)] public float minFillRatio = 0f;
		[Range(0, 1)] public float maxFillRatio = 1f;
		[SerializeField, Range(0f, 1f)] private float mFill;
		public bool isTimeCountdown;
		public bool isPercent;
		[SerializeField] private float mWidthOffset;
		[SerializeField] private float mHeightOffset;
		[SerializeField, ReadOnly] private float mValue = -1;
		[SerializeField, ReadOnly] private float mMax = -1;

		private int m_rank = -1;
		private Vector2 m_backgroundSize;
		private RectTransform m_rectProgressValue;

		public virtual float FillAmount
		{
			get => mFill;
			set
			{
				mFill = Mathf.Clamp01(value);
				float interpolatedFill = mFill == 0 ? 0 : Mathf.Lerp(minFillRatio, maxFillRatio, mFill);
				FillBar(interpolatedFill);
			}
		}

		public virtual float Value
		{
			get => mValue;
			set
			{
				if (mValue == value)
					return;

				mValue = value;
				if (mMax > 0)
					FillAmount = mValue / mMax;
				SetProgressDisplay();
			}
		}

		public virtual float Max
		{
			get => mMax;
			set
			{
				if (mMax == value)
					return;

				mMax = value;
				FillAmount = mMax > 0 ? mValue / mMax : 0;
				SetProgressDisplay();
			}
		}

		public virtual int Rank
		{
			get => m_rank;
			set
			{
				if (m_rank != value)
				{
					m_rank = value;
					SetRankDisplay();
				}
				if (txtRank != null)
					txtRank.enabled = m_rank >= 0;
			}
		}

		public Vector2 BarSize() => new(imgBackground.rectTransform.rect.width - mWidthOffset, imgBackground.rectTransform.rect.height - mHeightOffset);

		protected virtual void FillBar(float value)
		{
			if (fillByBarSize)
			{
				m_backgroundSize = new Vector2(imgBackground.rectTransform.rect.width, imgBackground.rectTransform.rect.height);
				m_backgroundSize.x -= mWidthOffset;
				m_backgroundSize.y -= mHeightOffset;

				if (m_rectProgressValue == null)
					m_rectProgressValue = imgProgressValue.rectTransform;

				Vector2 newSize = m_rectProgressValue.sizeDelta;
				if (fillDirection == FillDirection.Left || fillDirection == FillDirection.Right)
					newSize.x = m_backgroundSize.x * value;
				else
					newSize.y = m_backgroundSize.y * value;
				m_rectProgressValue.sizeDelta = newSize;
			}
			else
			{
				imgProgressValue.fillAmount = value;
			}
		}

		public virtual void Active(bool value)
		{
			imgBackground.gameObject.SetActive(value);
			imgProgressValue.gameObject.SetActive(value);
			if (txtValue != null)
				txtValue.gameObject.SetActive(value);
			if (txtRank != null)
				txtRank.gameObject.SetActive(value);
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
					txtValue.text = TimeHelper.FormatHhMmSs(mMax - mValue);
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
				txtRank.text = m_rank.ToString();
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (Application.isPlaying)
				return;

			Validate();
			float interpolatedFill = mFill == 0 ? 0 : Mathf.Lerp(minFillRatio, maxFillRatio, mFill);
			FillBar(interpolatedFill);
		}
#endif

		[ContextMenu("Validate")]
		private void Validate()
		{
			if (imgBackground == null || imgProgressValue == null)
			{
				var images = gameObject.GetComponentsInChildren<Image>();
				if (images.Length >= 2)
				{
					imgBackground = images[0];
					imgProgressValue = images[1];
				}
			}

			if (txtValue == null)
				txtValue = GetComponentInChildren<TextMeshProUGUI>();
		}
	}
}
