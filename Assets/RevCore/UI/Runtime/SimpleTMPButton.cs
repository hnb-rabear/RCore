using TMPro;
using UnityEngine;

namespace RevCore.UI
{
    [AddComponentMenu("RevCore/UI/SimpleTMPButton")]
    public class SimpleTMPButton : JustButton
    {
        [Tooltip("A direct reference to the TextMeshProUGUI component used as this button's label.")]
        public TextMeshProUGUI label;

        [Tooltip("If true, the label's font color will swap between 'fontColorOn' and 'fontColorOff' based on the enabled state.")]
        public bool fontColorOnOffSwap;

        [Tooltip("The font color to use when the button is enabled.")]
        public Color fontColorOn;

        [Tooltip("The font color to use when the button is disabled.")]
        public Color fontColorOff;

        [Tooltip("If true, the label's font material will swap between 'labelMatOn' and 'labelMatOff' based on the enabled state.")]
        public bool labelMatOnOffSwap;

        [Tooltip("The font material to use when the button is enabled.")]
        public Material labelMatOn;

        [Tooltip("The font material to use when the button is disabled.")]
        public Material labelMatOff;

        private bool m_findLabel;

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
