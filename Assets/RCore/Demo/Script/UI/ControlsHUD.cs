using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RCore.Components;

namespace RCore.Demo
{
    public class ControlsHUD : MonoBehaviour
    {
        [SerializeField] private SimpleButton mBtnPanel1;
        [SerializeField] private SimpleButton mBtnPanel2;
        [SerializeField] private SimpleButton mBtnPanel3;
        [SerializeField] private SimpleButton mBtnPanel4;
        [SerializeField] private SimpleButton mBtnPanel5;
        [SerializeField] private SimpleButton mBtnDataDemo;

        private void Start()
        {
            mBtnPanel1.onClick.AddListener(OnBtnPanel1_Pressed);
            mBtnPanel2.onClick.AddListener(OnBtnPanel2_Pressed);
            mBtnPanel3.onClick.AddListener(OnBtnPanel3_Pressed);
            mBtnPanel4.onClick.AddListener(OnBtnPanel4_Pressed);
            mBtnPanel5.onClick.AddListener(OnBtnPanel5_Pressed);
            mBtnDataDemo.onClick.AddListener(OnBtnDataDemo_Pressed);
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