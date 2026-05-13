using System;
using UnityEngine;

namespace RevCore.UI.Samples
{
    public class SamplePanelRoot : PanelRoot
    {
        [SerializeField] private PanelController samplePanel;

        protected override PanelController OnReceivedPanelRequest(Type panelType, object value)
        {
            return panelType == samplePanel.GetType() ? samplePanel : null;
        }

        public void ShowSamplePanel()
        {
            PushPanelToTop(ref samplePanel);
        }
    }
}
