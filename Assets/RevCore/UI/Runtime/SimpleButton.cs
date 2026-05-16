using System;
using UnityEngine;
using UnityEngine.UI;

namespace RevCore.UI
{
    /// <summary>
    /// <see cref="JustButton"/> with a legacy <see cref="UnityEngine.UI.Text"/> label. Deprecated in
    /// favour of <see cref="SimpleTMPButton"/>; kept for prefabs that have not migrated to TextMeshPro.
    /// </summary>
    [Obsolete("Use SimpleTMPButton instead")]
    [AddComponentMenu("RevCore/UI/SimpleButton")]
    public class SimpleButton : JustButton
    {
        /// <summary>Legacy UI Text used as the button's label.</summary>
        [Tooltip("A direct reference to the legacy UI Text component used as this button's label.")]
        public Text label;

        private bool m_findLabel;

        /// <summary>Lazily-resolved label reference. Searches children on first access.</summary>
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
