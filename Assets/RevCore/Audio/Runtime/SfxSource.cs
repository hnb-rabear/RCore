using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RevCore
{
    /// <summary>
    /// Component that picks a random clip name from <see cref="mClips"/> and plays it via the
    /// active <see cref="AudioManager"/>. Attach to a UI/effect prefab to play sounds without
    /// referencing the audio manager from script. In "Standalone Mode" (when <c>m_AudioSource</c>
    /// is assigned) plays through the local AudioSource instead of the shared pool.
    /// </summary>
    public class SfxSource : MonoBehaviour
    {
        /// <summary>Pool of clip names to choose from at random. Empty means the component is silent.</summary>
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

            var audioManager = AudioManager.Instance;
            if (audioManager == null)
            {
                Debug.LogError("AudioManager instance not found. SfxSource cannot function.");
                return;
            }

            if (audioManager.audioCollection == null)
            {
                Debug.LogError("AudioManager AudioCollection not found. SfxSource cannot function.");
                return;
            }

            mClips ??= Array.Empty<string>();
            m_indexes = new int[mClips.Length];
            for (int i = 0; i < mClips.Length; i++)
                audioManager.audioCollection.GetSFXClip(mClips[i], out m_indexes[i]);
            m_initialized = true;
        }

        /// <summary>Toggles looping playback.</summary>
        public void SetLoop(bool loop) => mIsLoop = loop;

        /// <summary>Sets the local volume (multiplied with master and SFX volume at play time). Standalone-mode only.</summary>
        public void SetVolume(float val) => m_Vol = val;

        /// <summary>Replaces the clip pool and re-resolves indices against the active <see cref="AudioCollection"/>.</summary>
        public void SetUp(string[] clips, bool isLoop)
        {
            mClips = clips ?? Array.Empty<string>();
            mIsLoop = isLoop;
            m_initialized = false;
            Init();
        }

        /// <summary>Picks a random clip from <see cref="mClips"/> and plays it. No-op when <see cref="AudioManager.EnabledSFX"/> is false.</summary>
        public void PlaySFX()
        {
            if (!m_initialized)
            {
                Debug.LogWarning("SfxSource is not initialized yet.", this);
                return;
            }

            var audioManager = AudioManager.Instance;
            if (audioManager == null || audioManager.audioCollection == null)
            {
                Debug.LogWarning("SfxSource cannot play because AudioManager or AudioCollection is missing.", this);
                return;
            }

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
                    if (clip == null)
                        return;

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

        /// <summary>Stops looping playback. Only valid in Standalone Mode (when a local <see cref="AudioSource"/> is assigned); logs an error otherwise.</summary>
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
