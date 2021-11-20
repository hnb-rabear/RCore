using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RCore.Components
{
    public class ResolutionFixer : MonoBehaviour
    {
        public CanvasScaler canvasScaler;
        public int screenWithStandard = 1920;
        public int screenHeightStandard = 1080;
        public bool enableTest;
        public int screenWidthTest = 1920;
        public int screenHeightTest = 1080;

        private void OnEnable()
        {
            Fix();
        }

        public void Fix()
        {
            var resolution = Screen.currentResolution;
            float screenAspect = resolution.width * 1f / resolution.height;
            float preferAspect = screenWithStandard * 1f / screenHeightStandard;
            if (screenAspect <= preferAspect)
                canvasScaler.matchWidthOrHeight = 0f;
            else
                canvasScaler.matchWidthOrHeight = 1f;

#if UNITY_EDITOR
            if (enableTest)
            {
                screenAspect = screenWidthTest * 1f / screenHeightTest;
                preferAspect = screenWithStandard * 1f / screenHeightStandard;
                if (screenAspect <= preferAspect)
                    canvasScaler.matchWidthOrHeight = 0f;
                else
                    canvasScaler.matchWidthOrHeight = 1f;
            }
            else
            {
                screenAspect = Screen.width * 1f / Screen.height;
                preferAspect = screenWithStandard * 1f / screenHeightStandard;
                if (screenAspect <= preferAspect)
                    canvasScaler.matchWidthOrHeight = 0f;
                else
                    canvasScaler.matchWidthOrHeight = 1f;
            }
            Debug.Log($"Resolution: {screenAspect}/{preferAspect}");
#endif
        }
    }

#if UNITY_EDITOR

    [UnityEditor.CustomEditor(typeof(ResolutionFixer))]
    public class CanvasResolutionFixerEditor : UnityEditor.Editor
    {
        private ResolutionFixer mScript;

        private void OnEnable()
        {
            mScript = (ResolutionFixer)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Fix"))
                mScript.Fix();
        }
    }
#endif
}