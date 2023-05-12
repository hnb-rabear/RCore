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
    public class HybirdAudioSFX : MonoBehaviour
    {
        [SerializeField] private string[] mIdStrings;
        [SerializeField] private bool mIsLoop;
        [SerializeField, ReadOnly] private int[] mIndexs;
        [SerializeField, Range(0.5f, 2f)] private float mPitchRandomMultiplier = 1f;
        [SerializeField] private AudioSource m_AudioSource;

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
            if (mInitialized) return;
            mIndexs = new int[mIdStrings.Length];
            for (int i = 0; i < mIdStrings.Length; i++)
            {
                var sound = HybirdAudioCollection.Instance.GetClip(mIdStrings[i], false, out mIndexs[i]);
                if (sound == null)
                    UnityEngine.Debug.LogError("Not found SFX id " + mIdStrings[i]);
            }
            mInitialized = true;
        }

        public void PlaySFX()
        {
            if (!HybirdAudioManager.Instance.EnabledSFX)
                return;

            if (mIndexs.Length > 0)
            {
                var index = mIndexs[Random.Range(0, mIndexs.Length)];
                if (m_AudioSource == null)
                {
                    if (HybirdAudioManager.Instance != null)
                        HybirdAudioManager.Instance.PlaySFXByIndex(index, mIsLoop, mPitchRandomMultiplier);
                }
                else
                {
                    if (!HybirdAudioManager.Instance.EnabledSFX)
                        return;
                    var clip = HybirdAudioManager.Instance.audioCollection.GetSFXClip(index);
                    m_AudioSource.volume = HybirdAudioManager.Instance.SFXVolume * HybirdAudioManager.Instance.MasterVolume;
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
        [CustomEditor(typeof(HybirdAudioSFX))]
        private class HybirdAudioSFXEditor : Editor
        {
            private HybirdAudioSFX mScript;
            private string mSearch = "";
            private UnityEngine.UI.Button mButton;

            private void OnEnable()
            {
                mScript = target as HybirdAudioSFX;

                mScript.mIdStrings ??= new string[] { };

                mButton = mScript.GetComponent<UnityEngine.UI.Button>();
            }

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                if (HybirdAudioCollection.Instance == null)
                {
                    if (HybirdAudioCollection.Instance == null)
                        EditorGUILayout.HelpBox("HybridAudioSFX require HybridAudioCollection. " +
                            "To create HybridAudioCollection,select Resources folder then from Create Menu " +
                            "select RUtilities/Create Hybrid Audio Collection", MessageType.Error);
                    return;
                }

                if (mScript.mIdStrings.Length > 0)
                    EditorHelper.BoxVertical(() =>
                    {
                        for (int i = 0; i < mScript.mIdStrings.Length; i++)
                        {
                            int i1 = i;
                            EditorHelper.BoxHorizontal(() =>
                            {
                                EditorHelper.TextField(mScript.mIdStrings[i1], "SoundId");
                                if (EditorHelper.ButtonColor("x", Color.red, 24))
                                {
                                    var list = mScript.mIdStrings.ToList();
                                    list.Remove(mScript.mIdStrings[i1]);
                                    mScript.mIdStrings = list.ToArray();
                                }
                            });
                        }
                    }, Color.yellow, true);

                EditorHelper.BoxVertical(() =>
                {
                    mSearch = EditorHelper.TextField(mSearch, "Search");
                    if (!string.IsNullOrEmpty(mSearch))
                    {
                        var clips = HybirdAudioCollection.Instance.SFXClips;
                        if (clips != null && clips.Count > 0)
                        {
                            foreach (var clip in clips)
                            {
                                if (clip.fileName.ToLower().Contains(mSearch.ToLower()))
                                {
                                    if (GUILayout.Button(clip.fileName))
                                    {
                                        var list = mScript.mIdStrings.ToList();
                                        if (!list.Contains(clip.fileName))
                                        {
                                            list.Add(clip.fileName);
                                            mScript.mIdStrings = list.ToArray();
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

                if (mButton != null)
                {
                    if (EditorHelper.ButtonColor("Add to OnClick event"))
                    {
                        UnityAction action = mScript.PlaySFX;
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