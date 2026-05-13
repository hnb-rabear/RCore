using System;
using UnityEngine;
using UnityEngine.UI;

namespace RevCore.UI
{
    [Obsolete("Use SimpleTMPButton instead")]
    [AddComponentMenu("RevCore/UI/SimpleButton")]
    public class SimpleButton : JustButton
    {
        [Tooltip("A direct reference to the legacy UI Text component used as this button's label.")]
        public Text label;

        private bool m_findLabel;

        public Text Label
        {
            get
            {
                if (label == null && !m_findLabel)
                {
                    label = GetComponentInChildren<Text>();
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
                label = GetComponentInChildren<Text>();
        }
#endif
    }
}
