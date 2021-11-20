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
    public class AudioSFX : MonoBehaviour
    {
        [SerializeField, ReadOnly] private int[] mIndexs;
        [SerializeField] private string[] mClips;
        [SerializeField] private bool mIsLoop;
        [SerializeField, Range(0.5f, 2f)] private float mPitchRandomMultiplier = 1f;
        [SerializeField] private int m_Limit;
        [SerializeField] private AudioSource m_AudioSource;
        [SerializeField, Range(0, 1f)] private float m_Vol = 1f;

        private bool mInitialized;

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
            if (mInitialized)
                return;

            mIndexs = new int[mClips.Length];
            for (int i = 0; i < mClips.Length; i++)
            {
                var clips = AudioCollection.Instance.sfxClips;
                for (int j = 0; j < clips.Length; j++)
                {
                    if (clips[j] != null && clips[j].name == mClips[i])
                    {
                        mIndexs[i] = j;
                        break;
                    }
                }
            }

            mInitialized = true;
        }

        public void SetLoop(bool pLoop) => mIsLoop = pLoop;

        public void SetVolume(float pVal) => m_Vol = pVal;

        public void SetUp(string[] pClips, bool pIsLoop)
        {
            mClips = pClips;
            mIsLoop = pIsLoop;
        }

        public void PlaySFX()
        {
            if (!AudioManager.Instance.EnabledSFX)
                return;

            if (mIndexs.Length > 0)
            {
                int index = mIndexs[Random.Range(0, mClips.Length)];
                if (m_AudioSource == null)
                {
                    AudioManager.Instance?.PlaySFX(index, m_Limit, mIsLoop, mPitchRandomMultiplier);
                }
                else
                {
                    if (!AudioManager.Instance.EnabledSFX)
                        return;
                    var clip = AudioCollection.Instance.GetSFXClip(index);
                    m_AudioSource.volume = AudioManager.Instance.SFXVolume * AudioManager.Instance.MasterVolume * m_Vol;
                    m_AudioSource.loop = mIsLoop;
                    m_AudioSource.clip = clip;
                    m_AudioSource.pitch = 1;
                    if (mPitchRandomMultiplier != 1)
                    {
                        if (Random.value < .5)
                            m_AudioSource.pitch *= Random.Range(1 / mPitchRandomMultiplier, 1);
                        else
                            m_AudioSource.pitch *= Random.Range(1, mPitchRandomMultiplier);
                    }
                    if (!mIsLoop)
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
        [CustomEditor(typeof(AudioSFX))]
        private class AudioSFXEditor : Editor
        {
            private AudioSFX mScript;
            private string mSearch = "";
            private UnityEngine.UI.Button mButton;

            private void OnEnable()
            {
                mScript = target as AudioSFX;

                if (mScript.mClips == null)
                    mScript.mClips = new string[0];

                mButton = mScript.GetComponent<UnityEngine.UI.Button>();
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

                if (mScript.mClips.Length > 0)
                    EditorHelper.BoxVertical(() =>
                    {
                        for (int i = 0; i < mScript.mClips.Length; i++)
                        {
                            EditorHelper.BoxHorizontal(() =>
                            {
                                EditorHelper.TextField(mScript.mClips[i], "");
                                if (EditorHelper.ButtonColor("x", Color.red, 24))
                                {
                                    var list = mScript.mClips.ToList();
                                    list.Remove(mScript.mClips[i]);
                                    mScript.mClips = list.ToArray();
                                }
                            });
                        }
                    }, Color.yellow, true);

                EditorHelper.BoxVertical(() =>
                {
                    mSearch = EditorHelper.TextField(mSearch, "Search");
                    if (!string.IsNullOrEmpty(mSearch))
                    {
                        var clips = AudioCollection.Instance.sfxClips;
                        if (clips != null && clips.Length > 0)
                        {
                            for (int i = 0; i < clips.Length; i++)
                            {
                                if (clips[i].name.ToLower().Contains(mSearch.ToLower()))
                                {
                                    if (GUILayout.Button(clips[i].name))
                                    {
                                        var list = mScript.mClips.ToList();
                                        if (!list.Contains(clips[i].name))
                                        {
                                            list.Add(clips[i].name);
                                            mScript.mClips = list.ToArray();
                                            mSearch = "";
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

                if (mButton != null)
                {
                    if (EditorHelper.ButtonColor("Add to OnClick event"))
                    {
                        UnityAction action = new UnityAction(mScript.PlaySFX);
                        UnityEventTools.AddVoidPersistentListener(mButton.onClick, action);
                    }
                }

                if (GUI.changed)
                    EditorUtility.SetDirty(mScript);
            }
        }
#endif
    }
}
