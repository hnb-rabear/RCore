using TMPro;
using UnityEngine;

namespace RevCore.UI
{
    /// <summary>
    /// <see cref="JustButton"/> with a <see cref="TextMeshProUGUI"/> label. Adds optional font-color
    /// and font-material swap that mirrors the on/off sprite swap on <see cref="JustButton"/>, so the
    /// label tracks the button's interactable state without a separate component.
    /// </summary>
    [AddComponentMenu("RevCore/UI/SimpleTMPButton")]
    public class SimpleTMPButton : JustButton
    {
        /// <summary>The button's label.</summary>
        [Tooltip("A direct reference to the TextMeshProUGUI component used as this button's label.")]
        public TextMeshProUGUI label;

        /// <summary>Swap the label's font color between <see cref="fontColorOn"/> and <see cref="fontColorOff"/> as the button enables/disables.</summary>
        [Tooltip("If true, the label's font color will swap between 'fontColorOn' and 'fontColorOff' based on the enabled state.")]
        public bool fontColorOnOffSwap;

        /// <summary>Label font color when the button is enabled.</summary>
        [Tooltip("The font color to use when the button is enabled.")]
        public Color fontColorOn;

        /// <summary>Label font color when the button is disabled.</summary>
        [Tooltip("The font color to use when the button is disabled.")]
        public Color fontColorOff;

        /// <summary>Swap the label's font material between <see cref="labelMatOn"/> and <see cref="labelMatOff"/> as the button enables/disables.</summary>
        [Tooltip("If true, the label's font material will swap between 'labelMatOn' and 'labelMatOff' based on the enabled state.")]
        public bool labelMatOnOffSwap;

        /// <summary>Label font material when the button is enabled.</summary>
        [Tooltip("The font material to use when the button is enabled.")]
        public Material labelMatOn;

        /// <summary>Label font material when the button is disabled.</summary>
        [Tooltip("The font material to use when the button is disabled.")]
        public Material labelMatOff;

        private bool m_findLabel;

        /// <summary>Lazily-resolved label reference. Searches children on first access.</summary>
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
            else if (labelMatOn == null && label != null)
            {
                labelMatOn = label.fontSharedMaterial;
            }
        }
#endif

        /// <inheritdoc />
        public override void SetEnable(bool value)
        {
            base.SetEnable(value);

            if (label == null)
                return;

            if (value)
            {
                if (fontColorOnOffSwap)
                    label.color = fontColorOn;

                if (labelMatOnOffSwap && labelMatOn != null && labelMatOff != null)
                    SwapLabelMaterial(labelMatOn);
            }
            else
            {
                if (fontColorOnOffSwap)
                    label.color = fontColorOff;

                if (labelMatOnOffSwap && labelMatOn != null && labelMatOff != null)
                    SwapLabelMaterial(labelMatOff);
            }
        }

        private void SwapLabelMaterial(Material material)
        {
            var labels = gameObject.FindComponentsInChildren<TextMeshProUGUI>();
            foreach (var textLabel in labels)
                if (textLabel.font == label.font && textLabel.fontSharedMaterial == label.fontSharedMaterial)
                    textLabel.fontSharedMaterial = material;
        }
    }
}
