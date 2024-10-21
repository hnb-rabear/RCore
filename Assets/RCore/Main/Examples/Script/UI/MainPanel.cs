using UnityEngine;
using RCore.UI;
using RCore.Inspector;

namespace RCore.Example.UI
{
    public class MainPanel : PanelController
    {
        public static MainPanel m_instance;
        public static MainPanel instance => m_instance;

        [SerializeField] private JustButton m_btnBackgroundBack;

        [Separator("Example using prefabs")]
        [SerializeField] private Panel1 m_panel1;
        [SerializeField] private Panel2 m_panel2;
        [SerializeField] private PanelExample m_panelExample;

        [Separator("Example using build-in prefabs")]
        [SerializeField] private Panel3 m_panel3;

        [Separator("Example using once-used panel prefabs")]
        [SerializeField] private Panel4 m_panel4;
        [SerializeField] private Panel5 m_panel5;

        public Panel1 Panel1 => GetCachedPanel(m_panel1);
        public Panel2 Panel2 => GetCachedPanel(m_panel2);
        public Panel3 Panel3 => GetCachedPanel(m_panel3);
        public Panel4 Panel4 => GetCachedPanel(m_panel4);
        public Panel5 Panel5 => GetCachedPanel(m_panel5);

        protected override void Awake()
        {
            if (m_instance == null)
                m_instance = this;
            else if (m_instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            base.Awake();
        }

        private void Start()
        {
            m_btnBackgroundBack.onClick.AddListener(OnBtnBack_Pressed);
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
                m_btnBackgroundBack.gameObject.SetActive(true);
                m_btnBackgroundBack.transform.SetSiblingIndex(TopPanel.transform.GetSiblingIndex() - 1);
            }
            else
            {
                m_btnBackgroundBack.gameObject.SetActive(false);
            }
        }

        protected override void OnAnyChildShow(PanelController pPanel)
        {
            base.OnAnyChildShow(pPanel);

            //Move global back button to beneath top panel
            if (TopPanel != null)
            {
                m_btnBackgroundBack.gameObject.SetActive(true);
                m_btnBackgroundBack.transform.SetSiblingIndex(TopPanel.transform.GetSiblingIndex() - 1);
            }
            else
            {
                m_btnBackgroundBack.gameObject.SetActive(false);
            }
        }

        public override void Init()
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
            PushPanel(ref m_panel1, false);
        }

        public void ShowPanel2()
        {
            //Hide but still keep current top panel in stack, then Push new one to top. 
            //NOTE: Inactive panels in stack will be auto active when it's upper panel is pop
            PushPanel(ref m_panel2, true);
        }

        public void ShowPanel3()
        {
            //Push new one to top without hide current top
            PushPanelToTop(ref m_panel3);
        }

        public void ShowPanel4()
        {
            //Push once-used panel
            PushPanel(ref m_panel4, false);
        }

        public void ShowPanel5()
        {
            PushPanelToTop(ref m_panel5);
        }

        public void ShowDemoDataPanel()
        {
            PushPanelToTop(ref m_panelExample);
        }
    }
}