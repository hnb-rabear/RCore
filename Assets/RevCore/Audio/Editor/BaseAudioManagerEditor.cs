using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
    [CustomEditor(typeof(BaseAudioManager), true)]
    public class BaseAudioManagerEditor : UnityEditor.Editor
    {
        private BaseAudioManager m_script;
        private EditorPrefString m_audioCollectionPath;
        private int m_sfxIndex;

        protected virtual void OnEnable()
        {
            m_script = target as BaseAudioManager;
            m_audioCollectionPath = new EditorPrefString($"{typeof(AudioCollection).FullName}");

            if (m_script.audioCollection != null)
                m_audioCollectionPath.Value = AssetDatabase.GetAssetPath(m_script.audioCollection);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (m_script.audioCollection == null)
                EditorGUILayout.HelpBox("AudioManager requires AudioCollection. Create one via Create > RevCore > Audio Collection", MessageType.Error);
            else if (GUILayout.Button("Open Audio Collection"))
                Selection.activeObject = m_script.audioCollection;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Stop Music"))
                m_script.StopMusic(1f);
            if (GUILayout.Button("Play Music"))
                m_script.PlayMusic();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Play Sfx"))
                m_script.PlaySFX(m_sfxIndex, 0);
            if (GUILayout.Button("Play Next Sfx"))
            {
                m_sfxIndex++;
                m_script.PlaySFX(m_sfxIndex, 0);
            }
            EditorGUILayout.EndHorizontal();

            if (GUI.changed)
                EditorUtility.SetDirty(m_script);
        }
    }
}
