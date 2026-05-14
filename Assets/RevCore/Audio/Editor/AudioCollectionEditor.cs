using System;
using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
    [CustomEditor(typeof(AudioCollection))]
    public class AudioCollectionEditor : UnityEditor.Editor
    {
        private AudioCollection m_collection;

        private void OnEnable()
        {
            m_collection = target as AudioCollection;
            if (m_collection != null)
            {
                m_collection.musicClips ??= Array.Empty<AudioClip>();
                m_collection.sfxClips ??= Array.Empty<AudioClip>();
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Audio IDs Generator", EditorStyles.boldLabel);

            m_collection.generator.inputMusicsFolder = EditorGUILayout.TextField("Musics Sources Path", m_collection.generator.inputMusicsFolder);
            m_collection.generator.inputSfxsFolder = EditorGUILayout.TextField("SFX Sources Path", m_collection.generator.inputSfxsFolder);
            m_collection.generator.outputIDsFolder = EditorGUILayout.TextField("Audio Ids Export Folder", m_collection.generator.outputIDsFolder);

            if (GUILayout.Button("Sort clips"))
            {
                AudioCollectionMenuTools.Sort(m_collection);
                Debug.Log("Audio clips sorted alphabetically.");
            }

            if (GUILayout.Button("Generate"))
                AudioCollectionMenuTools.Generate(m_collection);

#if ADDRESSABLES
            if (GUILayout.Button("Validate Addressable Sounds"))
            {
                AudioCollectionMenuTools.ValidateAddressables(m_collection);
                Debug.Log("Validated Addressable sounds. Direct references were converted.");
            }
#endif

            if (GUI.changed)
            {
                EditorUtility.SetDirty(m_collection);
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.EndVertical();
        }
    }
}
