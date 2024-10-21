/***
 * Author RadBear - nbhung71711 @gmail.com - 2018
 **/

using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCore.UI
{
	[AddComponentMenu("RCore/UI/SimpleTMPButton")]
	public class SimpleTMPButton : JustButton
	{
		[FormerlySerializedAs("mLabelTMP")]
		[SerializeField] protected TextMeshProUGUI m_label;

		public TextMeshProUGUI Label
		{
			get
			{
				if (m_label == null && !m_findLabel)
				{
					m_label = GetComponentInChildren<TextMeshProUGUI>();
					m_findLabel = true;
				}
				return m_label;
			}
		}

		private bool m_findLabel;

		[FormerlySerializedAs("mFontColorSwap")]
		[SerializeField] protected bool m_fontColorOnOffSwap;
		[FormerlySerializedAs("mFontColorActive")]
		[SerializeField] protected Color m_fontColorOn;
		[FormerlySerializedAs("mFontColorInactive")]
		[SerializeField] protected Color m_fontColorOff;

		[FormerlySerializedAs("m_LabelMatSwap")]
		[SerializeField] protected bool m_labelMatOnOffSwap;
		[FormerlySerializedAs("m_LabelMatActive")]
		[SerializeField] public Material m_labelMatOn;
		[FormerlySerializedAs("m_LabelMatInactive")]
		[SerializeField] public Material m_labelMatOff;

#if UNITY_EDITOR
		[ContextMenu("Validate")]
		protected override void OnValidate()
		{
			base.OnValidate();

			if (m_label == null)
				m_label = GetComponentInChildren<TextMeshProUGUI>();
			if (m_label == null)
				m_labelMatOnOffSwap = false;
			if (!m_labelMatOnOffSwap)
			{
				m_labelMatOn = null;
				m_labelMatOff = null;
			}
			else if (m_labelMatOn == null)
			{
				m_labelMatOn = m_label.fontSharedMaterial;
			}
		}
#endif
		
		public override void SetEnable(bool pValue)
		{
			base.SetEnable(pValue);

			if (pValue)
			{
				if (m_fontColorOnOffSwap)
					m_label.color = m_fontColorOn;
				if (m_labelMatOnOffSwap && m_labelMatOn != null && m_labelMatOff != null)
				{
					var labels = gameObject.FindComponentsInChildren<TextMeshProUGUI>();
					foreach (var label in labels)
					{
						if (label.font == m_label.font && label.fontSharedMaterial == m_label.fontSharedMaterial)
							label.fontSharedMaterial = m_labelMatOn;
					}
				}
			}
			else
			{
				if (m_fontColorOnOffSwap)
					m_label.color = m_fontColorOff;
				if (m_labelMatOnOffSwap && m_labelMatOn != null && m_labelMatOff != null)
				{
					var labels = gameObject.FindComponentsInChildren<TextMeshProUGUI>();
					foreach (var label in labels)
					{
						if (label.font == m_label.font && label.fontSharedMaterial == m_label.fontSharedMaterial)
							label.fontSharedMaterial = m_labelMatOff;
					}
				}
			}
		}
	}
}