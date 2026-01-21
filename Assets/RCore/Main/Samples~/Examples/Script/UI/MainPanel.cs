using UnityEngine;
using RCore.UI;
using RCore.Inspector;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace RCore.Example.UI
{
    public class MainPanel : PanelRoot
    {
	    private static MainPanel m_Instance;
        public static MainPanel Instance => m_Instance;

        [Separator("Example using prefabs")]
        [SerializeField] private Panel1 m_panel1;
        [SerializeField] private Panel2 m_panel2;
        [SerializeField] private PanelExample m_panelExample;

        [Separator("Example using build-in prefabs")]
        [SerializeField] private Panel3 m_panel3;

        [Separator("Example using once-used panel prefabs")]
        [SerializeField] private Panel4 m_panel4;
        [SerializeField] private Panel5 m_panel5;

        public Panel1 Panel1 => m_panel1;
        public Panel2 Panel2 => m_panel2;
        public Panel3 Panel3 => m_panel3;
        public Panel4 Panel4 => GetCachedPanel(m_panel4);
        public Panel5 Panel5 => GetCachedPanel(m_panel5);

        protected override void Awake()
        {
            if (m_Instance == null)
                m_Instance = this;
            else if (m_Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            base.Awake();
        }
        
        private void Update()
        {
            // Close panel by back button
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

        /// <summary>
        /// Remove and hide the current top panel, then bring a new one to the top.
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#endif
        public void ShowPanel1() => PushPanel(ref m_panel1, false);
        
        /// <summary>
        /// Temporarily hides the current top panel while retaining it in the stack, then pushes a new panel to the top.
        /// NOTE: Inactive panels in the stack will automatically become active when the panel above them is popped.
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#endif
        public void ShowPanel2() => PushPanel(ref m_panel2, true);

        /// <summary>
        /// Push a new one at the top while keeping the current top visible.
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#endif
        public void ShowPanel3() => PushPanelToTop(ref m_panel3);
        
        /// <summary>
        /// Delay pushing the new panel until all panels on top are removed. It uses PushPanelToTop
        /// </summary>
#if ODIN_INSPECTOR
        [Button]
#endif
        public void QueuePanel3() => AddPanelToQueue(ref m_panel3);
        
        /// <summary>
        /// Push once-used panel
        /// </summary>
        public void ShowPanel4() => PushPanel(ref m_panel4, false);
#if ODIN_INSPECTOR
        [Button]
#endif
        public void ShowPanel5() => PushPanelToTop(ref m_panel5);
#if ODIN_INSPECTOR
        [Button]
#endif
        public void QueuePanel5() => AddPanelToQueue(ref m_panel5);
#if ODIN_INSPECTOR
        [Button]
#endif
        public void ShowDemoDataPanel() => PushPanelToTop(ref m_panelExample);
#if ODIN_INSPECTOR
        [Button]
#endif
        public void QueueDemoDataPanel() => AddPanelToQueue(ref m_panelExample);
        
        protected override PanelController OnReceivedPanelRequest(string panelTypeFullName, object value)
        {
	        throw new System.NotImplementedException();
        }
    }
}