using TMPro;
using UnityEngine;

namespace RevCore.UI.Samples
{
    public class SampleScrollItem : OptimizedScrollItem
    {
        [SerializeField] private TMP_Text m_label;

        protected override void OnUpdateContent()
        {
            if (m_label != null)
                m_label.text = $"Item {m_index}";
        }
    }
}
