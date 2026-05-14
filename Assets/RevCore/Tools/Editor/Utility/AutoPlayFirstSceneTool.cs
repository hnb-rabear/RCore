using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RevCore.Tools.Editor
{
    [InitializeOnLoad]
    internal sealed class AutoPlayFirstSceneTool : RevCoreTool
    {
        private const string MenuPath = "RevCore/Tools/Toggle Auto Play First Scene";
        private static readonly EditorPrefsValue<bool> s_active = new("RevCore.Tools.AutoPlayFirstScene.Active");
        private static int s_previousSceneIndex;

        static AutoPlayFirstSceneTool()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public override string Name => "Auto Play First Scene";
        public override string Category => "Utility";
        public override bool IsQuickAction => true;

        public override void OnGUI()
        {
            bool value = s_active.Value;
            bool newValue = GUILayout.Toggle(value, value ? "Auto Play: ON" : "Auto Play: OFF");
            if (newValue != value)
                s_active.Value = newValue;
        }

        [MenuItem(MenuPath)]
        private static void ToggleActive()
        {
            s_active.Value = !s_active.Value;
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleActiveValidate()
        {
            Menu.SetChecked(MenuPath, s_active.Value);
            return true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!s_active.Value)
                return;

            if (state == PlayModeStateChange.ExitingEditMode)
            {
                if (!IsSceneInBuildSettings(SceneManager.GetActiveScene().path))
                    return;
                if (SceneManager.GetActiveScene().buildIndex == 0)
                    return;

                s_previousSceneIndex = SceneManager.GetActiveScene().buildIndex;
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(0));
            }
            else if (state == PlayModeStateChange.EnteredEditMode && s_previousSceneIndex > 0)
            {
                EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(s_previousSceneIndex));
                s_previousSceneIndex = 0;
            }
        }

        private static bool IsSceneInBuildSettings(string scenePath)
        {
            foreach (var scene in EditorBuildSettings.scenes)
                if (scene.path == scenePath) return true;
            return false;
        }
    }
}
