using UnityEngine;
using RCore.Common;
using Random = UnityEngine.Random;
using UnityEngine.Events;
using RCore.Inspector;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
#endif

namespace RCore.Components
{
    public class SFXSource : MonoBehaviour
    {
        [SerializeField, ReadOnly] private int[] m_Indexs;
        [SerializeField] private string[] mClips;
        [SerializeField] private bool m_IsLoop;
        [SerializeField, Range(0.5f, 2f)] private float m_PitchRandomMultiplier = 1f;
        [SerializeField] private int m_Limit;
        [SerializeField] private AudioSource m_AudioSource;
        [SerializeField, Range(0, 1f)] private float m_Vol = 1f;

        private bool m_Initialized;

        private void Awake()
        {
            Init();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            if (m_AudioSource != null)
            {
                m_AudioSource.loop = false;
                m_AudioSource.playOnAwake = false;
            }
        }

        private void Init()
        {
            if (m_Initialized)
                return;

            m_Indexs = new int[mClips.Length];
            for (int i = 0; i < mClips.Length; i++)
            {
                var clips = AudioCollection.Instance.sfxClips;
                for (int j = 0; j < clips.Length; j++)
                {
                    if (clips[j] != null && clips[j].name.ToLower() == mClips[i].ToLower())
                    {
                        m_Indexs[i] = j;
                        break;
                    }
                }
            }

            m_Initialized = true;
        }

        public void SetLoop(bool pLoop) => m_IsLoop = pLoop;

        public void SetVolume(float pVal) => m_Vol = pVal;

        public void SetUp(string[] pClips, bool pIsLoop)
        {
            mClips = pClips;
            m_IsLoop = pIsLoop;
        }

        public void PlaySFX()
        {
            if (!AudioManager.Instance.EnabledSFX)
                return;

            if (m_Indexs.Length > 0)
            {
                int index = m_Indexs[Random.Range(0, mClips.Length)];
                if (m_AudioSource == null)
                {
                    AudioManager.Instance?.PlaySFX(index, m_Limit, m_IsLoop, m_PitchRandomMultiplier);
                }
                else
                {
                    if (!AudioManager.Instance.EnabledSFX)
                        return;
                    var clip = AudioCollection.Instance.GetSFXClip(index);
                    m_AudioSource.volume = AudioManager.Instance.SFXVolume * AudioManager.Instance.MasterVolume * m_Vol;
                    m_AudioSource.loop = m_IsLoop;
                    m_AudioSource.clip = clip;
                    m_AudioSource.pitch = 1;
                    if (m_PitchRandomMultiplier != 1)
                    {
                        if (Random.value < .5)
                            m_AudioSource.pitch *= Random.Range(1 / m_PitchRandomMultiplier, 1);
                        else
                            m_AudioSource.pitch *= Random.Range(1, m_PitchRandomMultiplier);
                    }
                    if (!m_IsLoop)
                        m_AudioSource.PlayOneShot(clip);
                    else
                        m_AudioSource.Play();
                }
            }
        }

        public void StopSFX()
        {
            if (m_AudioSource == null)
            {
                UnityEngine.Debug.LogError($"This AudioSFX must have independent to able to stop, {gameObject.name}");
                return;
            }

            m_AudioSource.Stop();
        }

#if UNITY_EDITOR
        [CanEditMultipleObjects]
        [CustomEditor(typeof(SFXSource))]
        private class AudioSFXEditor : Editor
        {
            private SFXSource m_Script;
            private string m_Search = "";
            private UnityEngine.UI.Button m_Button;

            private void OnEnable()
            {
                m_Script = target as SFXSource;

                if (m_Script.mClips == null)
                    m_Script.mClips = new string[0];

                m_Button = m_Script.GetComponent<UnityEngine.UI.Button>();
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                if (AudioCollection.Instance == null)
                {
                    if (AudioCollection.Instance == null)
                        EditorGUILayout.HelpBox("AudioSFX require AudioCollection. " +
                            "To create AudioCollection, select Resources folder then from Create Menu " +
                            "select RUtilities/Create Audio Collection", MessageType.Error);
                    return;
                }

                if (m_Script.mClips.Length > 0)
                    EditorHelper.BoxVertical(() =>
                    {
                        for (int i = 0; i < m_Script.mClips.Length; i++)
                        {
                            EditorHelper.BoxHorizontal(() =>
                            {
                                EditorHelper.TextField(m_Script.mClips[i], "");
                                if (EditorHelper.ButtonColor("x", Color.red, 24))
                                {
                                    var list = m_Script.mClips.ToList();
                                    list.Remove(m_Script.mClips[i]);
                                    m_Script.mClips = list.ToArray();
                                }
                            });
                        }
                    }, Color.yellow, true);

                EditorHelper.BoxVertical(() =>
                {
                    m_Search = EditorHelper.TextField(m_Search, "Search");
                    if (!string.IsNullOrEmpty(m_Search))
                    {
                        var clips = AudioCollection.Instance.sfxClips;
                        if (clips != null && clips.Length > 0)
                        {
                            for (int i = 0; i < clips.Length; i++)
                            {
                                if (clips[i].name.ToLower().Contains(m_Search.ToLower()))
                                {
                                    if (GUILayout.Button(clips[i].name))
                                    {
                                        var list = m_Script.mClips.ToList();
                                        if (!list.Contains(clips[i].name))
                                        {
                                            list.Add(clips[i].name);
                                            m_Script.mClips = list.ToArray();
                                            m_Search = "";
                                            EditorGUI.FocusTextInControl(null);
                                        }
                                    }
                                }
                            }
                        }
                        else
                            EditorGUILayout.HelpBox("No results", MessageType.Warning);
                    }
                }, Color.white, true);

                if (EditorHelper.ButtonColor("Open Sounds Collection"))
                    Selection.activeObject = AudioCollection.Instance;

                if (m_Button != null)
                {
                    if (EditorHelper.ButtonColor("Add to OnClick event"))
                    {
                        UnityAction action = new UnityAction(m_Script.PlaySFX);
                        UnityEventTools.AddVoidPersistentListener(m_Button.onClick, action);
                    }
                }

                if (GUI.changed)
                    EditorUtility.SetDirty(m_Script);
            }
        }
#endif
    }
}
