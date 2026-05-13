using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

namespace RevCore.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SfxSource))]
    public class SfxSourceEditor : UnityEditor.Editor
    {
        private AudioCollection m_tempAudioCollection;
        private EditorPrefString m_audioCollectionPath;
        private SfxSource m_script;
        private string m_search = "";
        private UnityEngine.UI.Button m_button;

        private void OnEnable()
        {
            m_script = target as SfxSource;
            m_script.mClips ??= Array.Empty<string>();
            m_button = m_script.GetComponent<UnityEngine.UI.Button>();
            m_audioCollectionPath = new EditorPrefString($"{typeof(AudioCollection).FullName}");

            if (m_tempAudioCollection == null && !string.IsNullOrEmpty(m_audioCollectionPath.Value))
                m_tempAudioCollection = AssetDatabase.LoadAssetAtPath<AudioCollection>(m_audioCollectionPath.Value);

            if (m_tempAudioCollection == null)
            {
                var audioManager = FindObjectOfType<BaseAudioManager>();
                if (audioManager != null)
                {
                    m_tempAudioCollection = audioManager.audioCollection;
                    m_audioCollectionPath.Value = AssetDatabase.GetAssetPath(m_tempAudioCollection);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editor", EditorStyles.boldLabel);

            if (m_tempAudioCollection == null)
            {
                EditorGUILayout.HelpBox("SfxSource requires AudioCollection. Create one via Create > RevCore > Audio Collection", MessageType.Error);
                var asset = (AudioCollection)EditorGUILayout.ObjectField("Audio Collection", m_tempAudioCollection, typeof(AudioCollection), false);
                if (asset != m_tempAudioCollection)
                {
                    m_tempAudioCollection = asset;
                    if (m_tempAudioCollection != null)
                        m_audioCollectionPath.Value = AssetDatabase.GetAssetPath(m_tempAudioCollection);
                }
                return;
            }

            m_tempAudioCollection = (AudioCollection)EditorGUILayout.ObjectField("Audio Collection", m_tempAudioCollection, typeof(AudioCollection), false);

            if (m_script.mClips.Length > 0)
            {
                EditorGUILayout.BeginVertical("box");
                for (int i = 0; i < m_script.mClips.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.TextField(m_script.mClips[i]);
                    var oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("x", GUILayout.Width(24)))
                    {
                        var list = m_script.mClips.ToList();
                        list.RemoveAt(i);
                        m_script.mClips = list.ToArray();
                        i--;
                    }
                    GUI.backgroundColor = oldColor;
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.BeginVertical("box");
            m_search = EditorGUILayout.TextField("Search", m_search);
            if (!string.IsNullOrEmpty(m_search))
            {
                var sfxNames = m_tempAudioCollection.GetSFXNames();
                if (sfxNames != null && sfxNames.Length > 0)
                {
                    bool found = false;
                    for (int i = 0; i < sfxNames.Length; i++)
                    {
                        if (sfxNames[i] != null && sfxNames[i].ToLower().Contains(m_search.ToLower()))
                        {
                            found = true;
                            if (GUILayout.Button(sfxNames[i]))
                            {
                                var list = m_script.mClips.ToList();
                                if (!list.Contains(sfxNames[i]))
                                {
                                    list.Add(sfxNames[i]);
                                    m_script.mClips = list.ToArray();
                                    m_search = "";
                                    EditorGUI.FocusTextInControl(null);
                                }
                            }
                        }
                    }
                    if (!found)
                        EditorGUILayout.HelpBox("No results", MessageType.Warning);
                }
                else
                    EditorGUILayout.HelpBox("No results", MessageType.Warning);
            }
            EditorGUILayout.EndVertical();

            if (GUILayout.Button("Open Sounds Collection"))
                Selection.activeObject = m_tempAudioCollection;

            if (m_button != null)
            {
                if (GUILayout.Button("Add to OnClick event"))
                {
                    UnityAction action = m_script.PlaySFX;
                    UnityEventTools.AddVoidPersistentListener(m_button.onClick, action);
                }
            }

            if (GUI.changed)
                EditorUtility.SetDirty(m_script);
        }
    }
}
