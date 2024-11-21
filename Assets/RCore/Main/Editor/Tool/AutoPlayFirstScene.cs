using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace RCore.Editor.Tool
{
	[InitializeOnLoad]
	public class AutoPlayFirstScene
	{
		private static int m_PreviousSceneIndex;
		private static EditorPrefsBool m_Active;

		static AutoPlayFirstScene()
		{
			m_Active = new EditorPrefsBool(nameof(AutoPlayFirstScene), true);

			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (!m_Active.Value)
				return;
			if (state == PlayModeStateChange.ExitingEditMode)
			{
				if (SceneManager.GetActiveScene().buildIndex == 0)
					return;

				m_PreviousSceneIndex = SceneManager.GetActiveScene().buildIndex;

				EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
				EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(0));
			}
			else if (state == PlayModeStateChange.EnteredEditMode && m_PreviousSceneIndex > 0)
			{
				EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(m_PreviousSceneIndex));

				m_PreviousSceneIndex = 0;
			}
		}

		[MenuItem("RCore/Asset Database/Toggle Auto play first Scene")]
		private static void ToggleAutoLoadScene()
		{
			m_Active.Value = !m_Active.Value;
		}

		[MenuItem("RCore/Asset Database/Toggle Auto play first Scene", true)]
		private static bool ToggleAutoLoadSceneValidate()
		{
			Menu.SetChecked("RCore/Asset Database/Toggle Always play first Scene", m_Active.Value);
			return true;
		}
	}
}