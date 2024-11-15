/***
 * Author HNB-RaBear - 2018
 **/

using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCore.UI
{
	[AddComponentMenu("RCore/UI/SimpleTMPButton")]
	public class SimpleTMPButton : JustButton
	{
		public TextMeshProUGUI label;
		public bool fontColorOnOffSwap;
		public Color fontColorOn;
		public Color fontColorOff;

		public bool labelMatOnOffSwap;
		public Material labelMatOn;
		public Material labelMatOff;

		public TextMeshProUGUI Label
		{
			get
			{
				if (label == null && !m_findLabel)
				{
					label = GetComponentInChildren<TextMeshProUGUI>();
					m_findLabel = true;
				}
				return label;
			}
		}

		private bool m_findLabel;
		
#if UNITY_EDITOR
		[ContextMenu("Validate")]
		protected override void OnValidate()
		{
			base.OnValidate();

			if (label == null)
				label = GetComponentInChildren<TextMeshProUGUI>();
			if (label == null)
				labelMatOnOffSwap = false;
			if (!labelMatOnOffSwap)
			{
				labelMatOn = null;
				labelMatOff = null;
			}
			else if (labelMatOn == null)
			{
				labelMatOn = label.fontSharedMaterial;
			}
		}
#endif
		
		public override void SetEnable(bool pValue)
		{
			base.SetEnable(pValue);

			if (pValue)
			{
				if (fontColorOnOffSwap)
					label.color = fontColorOn;
				if (labelMatOnOffSwap && labelMatOn != null && labelMatOff != null)
				{
					var labels = gameObject.FindComponentsInChildren<TextMeshProUGUI>();
					foreach (var label in labels)
					{
						if (label.font == this.label.font && label.fontSharedMaterial == this.label.fontSharedMaterial)
							label.fontSharedMaterial = labelMatOn;
					}
				}
			}
			else
			{
				if (fontColorOnOffSwap)
					label.color = fontColorOff;
				if (labelMatOnOffSwap && labelMatOn != null && labelMatOff != null)
				{
					var labels = gameObject.FindComponentsInChildren<TextMeshProUGUI>();
					foreach (var label in labels)
					{
						if (label.font == this.label.font && label.fontSharedMaterial == this.label.fontSharedMaterial)
							label.fontSharedMaterial = labelMatOff;
					}
				}
			}
		}
	}
}