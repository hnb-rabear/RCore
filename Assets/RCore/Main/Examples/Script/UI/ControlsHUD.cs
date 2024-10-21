using UnityEngine;
using RCore.UI;

namespace RCore.Example.UI
{
    public class ControlsHUD : MonoBehaviour
    {
        [SerializeField] private SimpleButton m_btnPanel1;
        [SerializeField] private SimpleButton m_btnPanel2;
        [SerializeField] private SimpleButton m_btnPanel3;
        [SerializeField] private SimpleButton m_btnPanel4;
        [SerializeField] private SimpleButton m_btnPanel5;
        [SerializeField] private SimpleButton m_btnDataDemo;

        private void Start()
        {
            m_btnPanel1.onClick.AddListener(OnBtnPanel1_Pressed);
            m_btnPanel2.onClick.AddListener(OnBtnPanel2_Pressed);
            m_btnPanel3.onClick.AddListener(OnBtnPanel3_Pressed);
            m_btnPanel4.onClick.AddListener(OnBtnPanel4_Pressed);
            m_btnPanel5.onClick.AddListener(OnBtnPanel5_Pressed);
            m_btnDataDemo.onClick.AddListener(OnBtnDataDemo_Pressed);
        }

        private void OnBtnDataDemo_Pressed()
        {
            MainPanel.instance.ShowDemoDataPanel();
        }

        private void OnBtnPanel5_Pressed()
        {
            MainPanel.instance.ShowPanel5();
        }

        private void OnBtnPanel4_Pressed()
        {
            MainPanel.instance.ShowPanel4();
        }

        private void OnBtnPanel3_Pressed()
        {
            MainPanel.instance.ShowPanel3();
        }

        private void OnBtnPanel2_Pressed()
        {
            MainPanel.instance.ShowPanel2();
        }

        private void OnBtnPanel1_Pressed()
        {
            MainPanel.instance.ShowPanel1();
        }
    }
}