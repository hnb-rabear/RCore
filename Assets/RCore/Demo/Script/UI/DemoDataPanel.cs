using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RCore.Pattern.UI;
using RCore.Components;
using RCore.Common;
using TMPro;

namespace RCore.Demo
{
    public class DemoDataPanel : PanelController
    {
        [SerializeField] private TextMeshProUGUI mTxtFPS;
        [SerializeField] private CustomToggleSlider mTogSlider;
        [SerializeField] private CustomToggleTab mTab1;
        [SerializeField] private CustomToggleTab mTab2;
        [SerializeField] private CustomToggleTab mTab3;
        [SerializeField] private CustomToggleTab mTab4;
        [SerializeField] private JustButton mBtnSave;
        [SerializeField] private SimpleTMPButton mBtnLoad;
        [SerializeField] private ProgressBar mProgressBar;
        [SerializeField] private TMP_InputField mInputFiled;

        private FPSCounter mFPSCounter = new FPSCounter();
        private float mTime;

        private ExampleGameData GameData => ExampleGameData.Instance;

        private void Start()
        {
            mProgressBar.Max = 20;
            mBtnSave.onClick.AddListener(SaveData);
            mBtnLoad.onClick.AddListener(LoadData);
        }

        private void OnEnable()
        {
            LoadData();
        }

        private void Update()
        {
            mTime += Time.deltaTime;
            mProgressBar.Value = mTime % 30f;
            //Or
            //mProgressBar.FillAmount = (mTime % 30f) / 30f;

            mFPSCounter.Update(Time.deltaTime);
            if (mFPSCounter.updated)
                mTxtFPS.text = mFPSCounter.fps.ToString();
        }

        [InspectorButton]
        private void LoadData()
        {
            mTogSlider.isOn = GameData.demoGroup.toggleIsOn.Value;
            mInputFiled.text = GameData.demoGroup.inputFieldText.Value;
            mProgressBar.Value = GameData.demoGroup.progressBarValue.Value;
            mTime = mProgressBar.Value;
            switch (GameData.demoGroup.tabIndex.Value)
            {
                case 1: mTab1.isOn = true; break;
                case 2: mTab2.isOn = true; break;
                case 3: mTab3.isOn = true; break;
                case 4: mTab4.isOn = true; break;
            }
        }

        [InspectorButton]
        private void SaveData()
        {
            GameData.demoGroup.toggleIsOn.Value = mTogSlider.isOn;
            GameData.demoGroup.inputFieldText.Value = mInputFiled.text;
            GameData.demoGroup.progressBarValue.Value = mProgressBar.Value;
            if (mTab1.isOn)
                GameData.demoGroup.tabIndex.Value = 1;
            else if (mTab2.isOn)
                GameData.demoGroup.tabIndex.Value = 2;
            else if (mTab3.isOn)
                GameData.demoGroup.tabIndex.Value = 3;
            else if (mTab4.isOn)
                GameData.demoGroup.tabIndex.Value = 4;
        }
    }
}