using UnityEngine;
using RCore.Components;
using RCore.Framework.UI;
using RCore.Inspector;
using RCore.Common;

namespace RCore.Demo
{
    public class MainPanel : PanelController
    {
        public static MainPanel mInstance;
        public static MainPanel instance
        {
            get
            {
                if (mInstance == null)
                    mInstance = FindObjectOfType<MainPanel>();
                return mInstance;
            }
        }

        [SerializeField] private JustButton mBtnGlobalBack;

        [Separator("Example using prefabs")]
        [SerializeField] private Panel1 mPanel1;
        [SerializeField] private Panel2 mPanel2;
        [SerializeField] private DemoDataPanel mDemoDataPanel;

        [Separator("Example using build-in prefabs")]
        [SerializeField] private Panel3 mPanel3;

        [Separator("Example using once-used panel prefabs")]
        [SerializeField] private Panel4 mPanel4;
        [SerializeField] private Panel5 mPanel5;

        public Panel1 Panel1 => GetCachedPanel(mPanel1);
        public Panel2 Panel2 => GetCachedPanel(mPanel2);
        public Panel3 Panel3 => GetCachedPanel(mPanel3);
        public Panel4 Panel4 => GetCachedPanel(mPanel4);
        public Panel5 Panel5 => GetCachedPanel(mPanel5);

        private void Start()
        {
            mBtnGlobalBack.onClick.AddListener(OnBtnBack_Pressed);
        }

        private void Update()
        {
            //Close panel by back button
            if (Input.GetKey(KeyCode.Escape))
            {
                if (TopPanel != null)
                    TopPanel.Back();
            }
        }

        private void OnBtnBack_Pressed()
        {
            if (TopPanel != null)
                TopPanel.Back();
        }

        protected override void OnAnyChildHide(PanelController pPanel)
        {
            base.OnAnyChildHide(pPanel);

            //Move global back button to beneath top panel
            if (TopPanel != null)
            {
                mBtnGlobalBack.gameObject.SetActive(true);
                mBtnGlobalBack.transform.SetSiblingIndex(TopPanel.transform.GetSiblingIndex() - 1);
            }
            else
            {
                mBtnGlobalBack.gameObject.SetActive(false);
            }
        }

        protected override void OnAnyChildShow(PanelController pPanel)
        {
            base.OnAnyChildShow(pPanel);

            //Move global back button to beneath top panel
            if (TopPanel != null)
            {
                mBtnGlobalBack.gameObject.SetActive(true);
                mBtnGlobalBack.transform.SetSiblingIndex(TopPanel.transform.GetSiblingIndex() - 1);
            }
            else
            {
                mBtnGlobalBack.gameObject.SetActive(false);
            }
        }

        internal override void Init()
        {
            var panels = gameObject.FindComponentsInChildren<PanelController>();
            foreach (var panel in panels)
                if (panel != this)
                {
                    panel.SetActive(false);
                    panel.Init();
                }
        }

        public void ShowPanel1()
        {
            //Hide and pop current top panel, then push new one to top
            PushPanel(ref mPanel1, false);
        }

        public void ShowPanel2()
        {
            //Hide but still keep current top panel in stack, then Push new one to top. 
            //NOTE: Inactive panels in stack will be auto active when it's upper panel is pop
            PushPanel(ref mPanel2, true);
        }

        public void ShowPanel3()
        {
            //Push new one to top without hide current top
            PushPanelToTop(ref mPanel3);
        }

        public void ShowPanel4()
        {
            //Push once-used panel
            PushPanel(ref mPanel4, false);
        }

        public void ShowPanel5()
        {
            PushPanelToTop(ref mPanel5);
        }

        public void ShowDemoDataPanel()
        {
            PushPanelToTop(ref mDemoDataPanel);
        }
    }
}