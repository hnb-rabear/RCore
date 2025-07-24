/***
 * Author HNB-RaBear - 2018
 **/

using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCore.UI
{
	/// <summary>
	/// An extension of the JustButton class that is specifically designed to work with TextMeshPro labels.
	/// It adds functionality to swap the font color and/or font material of the label based on the button's enabled state.
	/// This is the recommended button class to use for projects utilizing TextMeshPro for UI text.
	/// </summary>
	[AddComponentMenu("RCore/UI/SimpleTMPButton")]
	public class SimpleTMPButton : JustButton
	{
		/// <summary>
		/// A direct reference to the TextMeshProUGUI component used as this button's label.
		/// </summary>
		[Tooltip("A direct reference to the TextMeshProUGUI component used as this button's label.")]
		public TextMeshProUGUI label;

		/// <summary>
		/// If true, the label's font color will swap between 'fontColorOn' and 'fontColorOff' based on the enabled state.
		/// </summary>
		[Tooltip("If true, the label's font color will swap between 'fontColorOn' and 'fontColorOff' based on the enabled state.")]
		public bool fontColorOnOffSwap;

		/// <summary>
		/// The font color to use when the button is enabled.
		/// </summary>
		[Tooltip("The font color to use when the button is enabled.")]
		public Color fontColorOn;

		/// <summary>
		/// The font color to use when the button is disabled.
		/// </summary>
		[Tooltip("The font color to use when the button is disabled.")]
		public Color fontColorOff;

		/// <summary>
		/// If true, the label's font material will swap between 'labelMatOn' and 'labelMatOff' based on the enabled state.
		/// </summary>
		[Tooltip("If true, the label's font material will swap between 'labelMatOn' and 'labelMatOff' based on the enabled state.")]
		public bool labelMatOnOffSwap;

		/// <summary>
		/// The font material to use when the button is enabled.
		/// </summary>
		[Tooltip("The font material to use when the button is enabled.")]
		public Material labelMatOn;

		/// <summary>
		/// The font material to use when the button is disabled.
		/// </summary>
		[Tooltip("The font material to use when the button is disabled.")]
		public Material labelMatOff;

		/// <summary>
		/// A property to get the label TextMeshProUGUI component.
		/// If the 'label' field is not assigned, it will attempt to find the component in its children once.
		/// </summary>
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

		/// <summary>A flag to ensure the search for the label component only happens once.</summary>
		private bool m_findLabel;
		
#if UNITY_EDITOR
		/// <summary>
		/// (Editor-only) Automatically called when the script is loaded or a value is changed in the Inspector.
		/// It attempts to find the child TextMeshProUGUI component if 'label' is not set. It also helps configure
		/// the material swap properties by assigning the current font material as the default 'On' material.
		/// </summary>
		[ContextMenu("Validate")]
		protected override void OnValidate()
		{
			base.OnValidate();

			if (label == null)
				label = GetComponentInChildren<TextMeshProUGUI>();

			// If no label is found, disable material swapping to prevent errors.
			if (label == null)
				labelMatOnOffSwap = false;
				
			if (!labelMatOnOffSwap)
			{
				labelMatOn = null;
				labelMatOff = null;
			}
			// If material swapping is enabled but the 'On' material is not set, use the label's current material as the default.
			else if (labelMatOn == null && label != null)
			{
				labelMatOn = label.fontSharedMaterial;
			}
		}
#endif
		
		/// <summary>
		/// Sets the button's logical enabled state, overriding the base method to add functionality
		/// for changing the label's appearance (color and material).
		/// </summary>
		/// <param name="pValue">True to enable the button, false to disable it.</param>
		public override void SetEnable(bool pValue)
		{
			base.SetEnable(pValue);

			// Return early if there's no label to modify.
			if (label == null) return;
			
			if (pValue) // Button is being enabled
			{
				if (fontColorOnOffSwap)
					label.color = fontColorOn;
					
				if (labelMatOnOffSwap && labelMatOn != null && labelMatOff != null)
				{
					// This logic finds all TMP components that use the same font and material as the main label and updates them.
					// This is useful for complex buttons with multiple text elements that should change together.
					var labels = gameObject.FindComponentsInChildren<TextMeshProUGUI>();
					foreach (var textLabel in labels)
					{
						if (textLabel.font == this.label.font && textLabel.fontSharedMaterial == this.label.fontSharedMaterial)
							textLabel.fontSharedMaterial = labelMatOn;
					}
				}
			}
			else // Button is being disabled
			{
				if (fontColorOnOffSwap)
					label.color = fontColorOff;
					
				if (labelMatOnOffSwap && labelMatOn != null && labelMatOff != null)
				{
					var labels = gameObject.FindComponentsInChildren<TextMeshProUGUI>();
					foreach (var textLabel in labels)
					{
						if (textLabel.font == this.label.font && textLabel.fontSharedMaterial == this.label.fontSharedMaterial)
							textLabel.fontSharedMaterial = labelMatOff;
					}
				}
			}
		}
	}
}