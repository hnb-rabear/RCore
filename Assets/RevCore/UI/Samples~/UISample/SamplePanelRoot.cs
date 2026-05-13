using UnityEngine;

namespace RevCore.UI.Samples
{
    public class SamplePanelRoot : PanelRoot
    {
        [SerializeField] private PanelController samplePanel;

        protected override PanelController OnReceivedPanelRequest(string panelTypeFullName, object value)
        {
            return null;
        }

        public void ShowSamplePanel()
        {
            PushPanelToTop(ref samplePanel);
        }
    }
}
