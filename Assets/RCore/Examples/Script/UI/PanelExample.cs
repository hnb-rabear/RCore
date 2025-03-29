using UnityEngine;
using RCore.UI;
using RCore.Example.Data.KeyValue;
using TMPro;

namespace RCore.Example.UI
{
    public class PanelExample : PanelController
    {
        [SerializeField] private CustomToggleSlider m_toggleSlider;
        [SerializeField] private JustToggle m_tab1;
        [SerializeField] private JustToggle m_tab2;
        [SerializeField] private JustToggle m_tab3;
        [SerializeField] private JustToggle m_tab4;
        [SerializeField] private JustButton m_btnSave;
        [SerializeField] private SimpleTMPButton m_btnLoad;
        [SerializeField] private ProgressBar m_progressBar;
        [SerializeField] private TMP_InputField m_inputField;
        
        private float mTime;

        private ExampleKeyValueDBManager KeyValueDBManager => ExampleKeyValueDBManager.Instance;

        private void Start()
        {
            m_progressBar.Max = 20;
            m_btnSave.onClick.AddListener(SaveData);
            m_btnLoad.onClick.AddListener(LoadData);
        }

        private void OnEnable()
        {
            LoadData();
        }

        private void Update()
        {
            mTime += Time.deltaTime;
            m_progressBar.Value = mTime % 30f;
            //Or
            //mProgressBar.FillAmount = (mTime % 30f) / 30f;
        }

        [InspectorButton]
        private void LoadData()
        {
            m_toggleSlider.isOn = KeyValueDBManager.mainGroup2.toggleIsOn.Value;
            m_inputField.text = KeyValueDBManager.mainGroup2.inputFieldText.Value;
            m_progressBar.Value = KeyValueDBManager.mainGroup2.progressBarValue.Value;
            mTime = m_progressBar.Value;
            switch (KeyValueDBManager.mainGroup2.tabIndex.Value)
            {
                case 1: m_tab1.isOn = true; break;
                case 2: m_tab2.isOn = true; break;
                case 3: m_tab3.isOn = true; break;
                case 4: m_tab4.isOn = true; break;
            }
        }

        [InspectorButton]
        private void SaveData()
        {
            KeyValueDBManager.mainGroup2.toggleIsOn.Value = m_toggleSlider.isOn;
            KeyValueDBManager.mainGroup2.inputFieldText.Value = m_inputField.text;
            KeyValueDBManager.mainGroup2.progressBarValue.Value = m_progressBar.Value;
            if (m_tab1.isOn)
                KeyValueDBManager.mainGroup2.tabIndex.Value = 1;
            else if (m_tab2.isOn)
                KeyValueDBManager.mainGroup2.tabIndex.Value = 2;
            else if (m_tab3.isOn)
                KeyValueDBManager.mainGroup2.tabIndex.Value = 3;
            else if (m_tab4.isOn)
                KeyValueDBManager.mainGroup2.tabIndex.Value = 4;
        }
    }
}