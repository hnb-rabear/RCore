/**
 * Author HNB-RaBear - 2021
 **/

using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RCore.Audio
{
    /// <summary>
    /// A flexible component for playing sound effects. It can operate in two modes:
    /// 1. **Managed Mode**: If m_AudioSource is null, it acts as a trigger, requesting the central AudioManager to play a sound from its managed pool.
    /// 2. **Standalone Mode**: If m_AudioSource is assigned, it directly controls that AudioSource component to play the sound. This is useful for sounds tied to a specific object's position or for more granular control.
    /// </summary>
    public class SfxSource : MonoBehaviour
    {
        [Tooltip("An array of sound effect names from the AudioCollection. A random clip from this list will be played.")]
        [SerializeField] public string[] mClips;
        [Tooltip("Should the sound effect loop when played?")]
        [SerializeField] private bool mIsLoop;
        [Tooltip("Adds pitch variation to the sound, making it less repetitive. A value of 1.1 allows for a 10% variance up or down.")]
        [SerializeField, Range(0.5f, 2f)] private float m_PitchRandomMultiplier = 1f;
        [Tooltip("Limits the number of concurrent instances of this sound. Only effective in Managed Mode (when m_AudioSource is null).")]
        [SerializeField] private int m_Limit;
        
        [Header("Standalone Mode")]
        [Tooltip("If assigned, this component will control this AudioSource directly. If null, it uses the central AudioManager.")]
        [SerializeField] private AudioSource m_AudioSource;
        [Tooltip("A local volume multiplier for the standalone AudioSource. The final volume is (MasterVol * SfxVol * this.Vol).")]
        [SerializeField, Range(0, 1f)] private float m_Vol = 1f;

        private bool m_initialized;
        // Caches the integer indices of the clips for performance, avoiding string lookups on play.
        private int[] m_indexes = Array.Empty<int>();

        /// <summary>
        /// Ensures the component is initialized on Awake.
        /// </summary>
        private void Awake()
        {
            Init();
        }

        /// <summary>
        /// A fallback to ensure the component is initialized on Start.
        /// </summary>
        private void Start()
        {
            Init();
        }

        /// <summary>
        /// In the editor, this automatically finds the attached AudioSource component
        /// and sets default values for a better workflow.
        /// </summary>
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

        /// <summary>
        /// Initializes the component by caching the integer IDs of the specified clip names from the AudioManager.
        /// This is a performance optimization.
        /// </summary>
        private void Init()
        {
            if (m_initialized)
                return;

            if (AudioManager.Instance == null)
            {
                Debug.LogError("AudioManager instance not found! SfxSource cannot function.");
                return;
            }

            // Convert clip names (strings) to clip indices (integers) for faster access.
            m_indexes = new int[mClips.Length];
            for (int i = 0; i < mClips.Length; i++)
                AudioManager.Instance.audioCollection.GetSFXClip(mClips[i], out m_indexes[i]);

            m_initialized = true;
        }

        /// <summary>
        /// Sets the loop property at runtime.
        /// </summary>
        /// <param name="pLoop">True to enable looping, false to disable.</param>
        public void SetLoop(bool pLoop) => mIsLoop = pLoop;

        /// <summary>
        /// Sets the local volume multiplier at runtime (only effective in Standalone Mode).
        /// </summary>
        /// <param name="pVal">The new volume multiplier (0.0 to 1.0).</param>
        public void SetVolume(float pVal) => m_Vol = pVal;

        /// <summary>
        /// Programmatically configures the component with a new set of clips and loop setting.
        /// </summary>
        /// <param name="pClips">An array of clip names to use.</param>
        /// <param name="pIsLoop">The new loop setting.</param>
        public void SetUp(string[] pClips, bool pIsLoop)
        {
            mClips = pClips;
            mIsLoop = pIsLoop;
            // Re-initialize to cache the new clip indices.
            m_initialized = false;
            Init();
        }

        /// <summary>
        /// Plays a random sound effect from the mClips list, using either the AudioManager or the standalone AudioSource.
        /// </summary>
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
                // Select a random clip index from the cached list.
                int index = m_indexes[Random.Range(0, m_indexes.Length)];

                // --- Managed Mode ---
                if (m_AudioSource == null)
                {
                    if (audioManager != null)
                        audioManager.PlaySFX(index, m_Limit, mIsLoop, m_PitchRandomMultiplier);
                }
                // --- Standalone Mode ---
                else
                {
                    var clip = audioManager.audioCollection.GetSFXClip(index);
                    // Calculate final volume based on global settings and local multiplier.
                    m_AudioSource.volume = audioManager.SFXVolume * audioManager.MasterVolume * m_Vol;
                    m_AudioSource.loop = mIsLoop;
                    m_AudioSource.clip = clip;
                    
                    // Apply pitch randomization.
                    m_AudioSource.pitch = 1;
                    if (m_PitchRandomMultiplier != 1)
                    {
                        if (Random.value < .5)
                            m_AudioSource.pitch *= Random.Range(1 / m_PitchRandomMultiplier, 1);
                        else
                            m_AudioSource.pitch *= Random.Range(1, m_PitchRandomMultiplier);
                    }

                    // Play the sound.
                    if (!mIsLoop)
                        m_AudioSource.PlayOneShot(clip);
                    else
                        m_AudioSource.Play();
                }
            }
        }

        /// <summary>
        /// Stops the currently playing sound. This method only works in Standalone Mode.
        /// </summary>
        public void StopSFX()
        {
            if (m_AudioSource == null)
            {
                // Cannot stop a sound managed by the central AudioManager this way.
                Debug.LogError($"To stop this sound, SfxSource must have its own AudioSource assigned. GameObject: {gameObject.name}", this);
                return;
            }

            m_AudioSource.Stop();
        }
    }
}