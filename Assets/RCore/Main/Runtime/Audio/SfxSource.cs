using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RCore.Audio
{
    public class SfxSource : MonoBehaviour
    {
        [SerializeField] public string[] mClips;
        [SerializeField] private bool mIsLoop;
        [SerializeField, Range(0.5f, 2f)] private float m_PitchRandomMultiplier = 1f;
        [SerializeField] private int m_Limit;
        /// <summary>
        /// Standalone audio source
        /// </summary>
        [SerializeField] private AudioSource m_AudioSource;
        /// <summary>
        /// Standalone audio volume
        /// </summary>
        [SerializeField, Range(0, 1f)] private float m_Vol = 1f;

        private bool m_initialized;
        private int[] m_indexes = Array.Empty<int>();

        private void Awake()
        {
            Init();
        }

        private void Start()
        {
            Init();
        }

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
                Debug.LogError("Not found Audio Manager!");
                return;
            }

            m_indexes = new int[mClips.Length];
            for (int i = 0; i < mClips.Length; i++)
                AudioManager.Instance.audioCollection.GetSFXClip(mClips[i], out m_indexes[i]);

            m_initialized = true;
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
            if (!m_initialized)
                return;
            var audioManager = AudioManager.Instance;
            if (!audioManager.EnabledSFX)
                return;

            if (m_indexes.Length > 0)
            {
                int index = m_indexes[Random.Range(0, mClips.Length)];
                if (m_AudioSource == null)
                {
                    if (audioManager != null)
                        audioManager.PlaySFX(index, m_Limit, mIsLoop, m_PitchRandomMultiplier);
                }
                else
                {
                    if (!audioManager.EnabledSFX)
                        return;
                    var clip = audioManager.audioCollection.GetSFXClip(index);
                    m_AudioSource.volume = audioManager.SFXVolume * audioManager.MasterVolume * m_Vol;
                    m_AudioSource.loop = mIsLoop;
                    m_AudioSource.clip = clip;
                    m_AudioSource.pitch = 1;
                    if (m_PitchRandomMultiplier != 1)
                    {
                        if (Random.value < .5)
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
                UnityEngine.Debug.LogError($"This AudioSFX must have independent to able to stop, {gameObject.name}");
                return;
            }

            m_AudioSource.Stop();
        }
    }
}