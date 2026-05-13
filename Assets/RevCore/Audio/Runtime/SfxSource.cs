using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RevCore
{
    public class SfxSource : MonoBehaviour
    {
        [SerializeField] public string[] mClips;
        [SerializeField] private bool mIsLoop;
        [SerializeField, Range(0.5f, 2f)] private float m_PitchRandomMultiplier = 1f;
        [SerializeField] private int m_Limit;

        [Header("Standalone Mode")]
        [SerializeField] private AudioSource m_AudioSource;
        [SerializeField, Range(0, 1f)] private float m_Vol = 1f;

        private bool m_initialized;
        private int[] m_indexes = Array.Empty<int>();

        private void Awake() => Init();
        private void Start() => Init();

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;
            if (m_AudioSource == null)
                m_AudioSource = GetComponent<AudioSource>();
            if (m_AudioSource != null)
            {
                m_AudioSource.loop = false;
                m_AudioSource.playOnAwake = false;
            }
        }

        private void Init()
        {
            if (m_initialized)
                return;
            if (AudioManager.Instance == null)
            {
                Debug.LogError("AudioManager instance not found. SfxSource cannot function.");
                return;
            }
            m_indexes = new int[mClips.Length];
            for (int i = 0; i < mClips.Length; i++)
                AudioManager.Instance.audioCollection.GetSFXClip(mClips[i], out m_indexes[i]);
            m_initialized = true;
        }

        public void SetLoop(bool loop) => mIsLoop = loop;
        public void SetVolume(float val) => m_Vol = val;

        public void SetUp(string[] clips, bool isLoop)
        {
            mClips = clips;
            mIsLoop = isLoop;
            m_initialized = false;
            Init();
        }

        public void PlaySFX()
        {
            if (!m_initialized)
            {
                Debug.LogWarning("SfxSource is not initialized yet.", this);
                return;
            }

            var audioManager = AudioManager.Instance;
            if (!audioManager.EnabledSFX)
                return;

            if (m_indexes.Length > 0)
            {
                int index = m_indexes[Random.Range(0, m_indexes.Length)];

                if (m_AudioSource == null)
                {
                    audioManager.PlaySFX(index, m_Limit, mIsLoop, m_PitchRandomMultiplier);
                }
                else
                {
                    var clip = audioManager.audioCollection.GetSFXClip(index);
                    m_AudioSource.volume = audioManager.SFXVolume * audioManager.MasterVolume * m_Vol;
                    m_AudioSource.loop = mIsLoop;
                    m_AudioSource.clip = clip;
                    m_AudioSource.pitch = 1;
                    if (m_PitchRandomMultiplier != 1)
                    {
                        if (Random.value < .5f)
                            m_AudioSource.pitch *= Random.Range(1 / m_PitchRandomMultiplier, 1);
                        else
                            m_AudioSource.pitch *= Random.Range(1, m_PitchRandomMultiplier);
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
                Debug.LogError($"To stop this sound, SfxSource must have its own AudioSource assigned. GameObject: {gameObject.name}", this);
                return;
            }
            m_AudioSource.Stop();
        }
    }
}
