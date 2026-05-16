using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RevCore.UI
{
	/// <summary>
	/// Progress / fill bar with optional rank and value text. Supports two fill modes: scaled
	/// foreground rect (<see cref="fillByBarSize"/> = true) or <see cref="Image"/> fill amount.
	/// Display modes: numeric (<c>value/max</c>), percentage (<see cref="isPercent"/>), or
	/// countdown timer (<see cref="isTimeCountdown"/>).
	/// </summary>
	public class ProgressBar : MonoBehaviour
	{
		/// <summary>Axis the foreground bar grows along when <see cref="fillByBarSize"/> is enabled.</summary>
		public enum FillDirection
		{
			/// <summary>Bar shrinks to the left.</summary>
			Left,
			/// <summary>Bar grows to the right (default).</summary>
			Right,
			/// <summary>Bar grows upward.</summary>
			Top,
			/// <summary>Bar shrinks downward.</summary>
			Bottom
		}

		/// <summary>Background graphic.</summary>
		public Image imgBackground;
		/// <summary>Foreground (fill) graphic.</summary>
		public Image imgProgressValue;
		/// <summary>Optional value label.</summary>
		public TextMeshProUGUI txtValue;
		/// <summary>Optional rank label.</summary>
		public TextMeshProUGUI txtRank;
		/// <summary>When true, fills by resizing the foreground rect rather than animating <see cref="Image.fillAmount"/>.</summary>
		public bool fillByBarSize;
		/// <summary>Axis the foreground bar grows along. Used only when <see cref="fillByBarSize"/> is true.</summary>
		[Tooltip("If fillByBarSize = true")] public FillDirection fillDirection;
		/// <summary>Minimum displayed fill ratio (0..1). Lets you reserve a non-empty floor on the visual.</summary>
		[Range(0, 1)] public float minFillRatio = 0f;
		/// <summary>Maximum displayed fill ratio (0..1). Lets you cap the visual short of full.</summary>
		[Range(0, 1)] public float maxFillRatio = 1f;
		[SerializeField, Range(0f, 1f)] private float mFill;
		/// <summary>When true, <see cref="txtValue"/> renders the remaining time using <see cref="TimeHelper.FormatHhMmSs"/>.</summary>
		public bool isTimeCountdown;
		/// <summary>When true, <see cref="txtValue"/> renders a percentage rather than <c>value/max</c>.</summary>
		public bool isPercent;
		[SerializeField] private float mWidthOffset;
		[SerializeField] private float mHeightOffset;
		[SerializeField, ReadOnly] private float mValue = -1;
		[SerializeField, ReadOnly] private float mMax = -1;

		private int m_rank = -1;
		private Vector2 m_backgroundSize;
		private RectTransform m_rectProgressValue;

		/// <summary>Normalized fill (0..1). Setter applies the min/max-ratio mapping before drawing.</summary>
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

		/// <summary>Current value. Updates <see cref="FillAmount"/> and refreshes the value label.</summary>
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

		/// <summary>Maximum value. Setting it recomputes the fill ratio.</summary>
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

		/// <summary>Optional rank value displayed in <see cref="txtRank"/>. Hidden when negative.</summary>
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

		/// <summary>Returns the bar's usable size (background size minus the inspector-defined width/height offsets).</summary>
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

		/// <summary>Bulk active/inactive on every owned graphic (background, foreground, value, rank).</summary>
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
