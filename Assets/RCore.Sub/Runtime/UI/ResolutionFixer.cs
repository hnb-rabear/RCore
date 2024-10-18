using UnityEngine;
using UnityEngine.UI;

namespace RCore.UI
{
    public class ResolutionFixer : MonoBehaviour
    {
        public CanvasScaler canvasScaler;
        public int screenWithStandard = 1920;
        public int screenHeightStandard = 1080;

        private void OnEnable()
        {
            Fix();
        }

        public void Fix()
        {
            var resolution = Screen.currentResolution;
            float screenAspect = resolution.width * 1f / resolution.height;
            float preferAspect = screenWithStandard * 1f / screenHeightStandard;
#if UNITY_EDITOR
            screenAspect = Screen.width * 1f / Screen.height;
            preferAspect = screenWithStandard * 1f / screenHeightStandard;
#endif
            if (screenAspect <= preferAspect)
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
            else
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            Debug.Log($"Resolution: {screenAspect}/{preferAspect}");
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