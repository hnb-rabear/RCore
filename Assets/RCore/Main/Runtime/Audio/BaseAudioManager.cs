/**
 * Author HNB-RaBear - 2021
 **/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using RCore.Inspector;
#if DOTWEEN
using DG.Tweening;
#endif
using Random = UnityEngine.Random;

namespace RCore.Audio
{
	/// <summary>
	/// An event dispatched to request playing a UI sound effect.
	/// </summary>
	public struct UISfxTriggeredEvent : BaseEvent
	{
		/// <summary>
		/// The name or key of the sound effect to be played.
		/// </summary>
		public string sfx;

		/// <summary>
		/// Initializes a new instance of the <see cref="UISfxTriggeredEvent"/> struct.
		/// </summary>
		/// <param name="val">The name of the sound effect.</param>
		public UISfxTriggeredEvent(string val)
		{
			sfx = val;
		}
	}

	/// <summary>
	/// A base class for managing audio playback for both music and sound effects.
	/// It handles volume control, fading, and pooling of AudioSources.
	/// </summary>
	public class BaseAudioManager : MonoBehaviour
    {
		[Tooltip("The collection of all audio clips available to the manager.")]
        [AutoFill] public AudioCollection audioCollection;
        [SerializeField] protected bool m_enabledSfx = true;
        [SerializeField] protected bool m_enabledMusic = true;
        [SerializeField] protected List<AudioSource> m_sfxSources;
        [SerializeField] public AudioSource m_sfxSourceUnlimited;
        [SerializeField] public AudioSource m_musicSource;
        [SerializeField, Range(0, 1f)] protected float m_masterVolume = 1f;
        [SerializeField, Range(0, 1f)] protected float m_sfxVolume = 1f;
        [SerializeField, Range(0, 1f)] protected float m_musicVolume = 1f;

		/// <summary>
		/// Gets a value indicating whether sound effects are currently enabled.
		/// </summary>
		public bool EnabledSFX => m_enabledSfx;
		/// <summary>
		/// Gets a value indicating whether music is currently enabled.
		/// </summary>
		public bool EnabledMusic => m_enabledMusic;
		/// <summary>
		/// Gets the current master volume level.
		/// </summary>
		public float MasterVolume => m_masterVolume;
		/// <summary>
		/// Gets the current sound effects volume level.
		/// </summary>
		public float SFXVolume => m_sfxVolume;
		/// <summary>
		/// Gets the current music volume level.
		/// </summary>
		public float MusicVolume => m_musicVolume;

#if DOTWEEN
        private Tweener m_masterTweener;
        private Tweener m_musicTweener;
        private Tweener m_sfxTweener;
#endif
        private Coroutine m_playMusicsCoroutine;

		/// <summary>
		/// Initializes the volume of all audio sources at the start.
		/// </summary>
		protected virtual void Start()
        {
            m_musicSource.volume = m_masterVolume * m_musicVolume;
            m_sfxSourceUnlimited.volume = m_masterVolume * m_sfxVolume;
            foreach (var sound in m_sfxSources)
                sound.volume = m_masterVolume * m_sfxVolume;
        }

#region Common

		/// <summary>
		/// Sets the master volume for all audio.
		/// </summary>
		/// <param name="pValue">The target volume level (0.0 to 1.0).</param>
		/// <param name="pFadeDuration">The duration of the volume fade in seconds. If 0, the change is instant.</param>
		/// <param name="pOnComplete">An optional action to invoke when the fade is complete.</param>
		public void SetMasterVolume(float pValue, float pFadeDuration = 0, Action pOnComplete = null)
        {
#if DOTWEEN
            m_masterTweener.Kill();
#endif
            if (m_masterVolume == pValue)
            {
                pOnComplete?.Invoke();
                return;
            }

            if (pFadeDuration <= 0)
            {
                m_masterVolume = pValue;
                m_musicSource.volume = m_masterVolume * m_musicVolume;
                m_sfxSourceUnlimited.volume = m_masterVolume * m_sfxVolume;
                foreach (var source in m_sfxSources)
                    source.volume = m_masterVolume * m_sfxVolume;
                pOnComplete?.Invoke();
            }
            else
            {
                float fromVal = m_masterVolume;
#if DOTWEEN
                // Smoothly transition the volume using DOTween.
                float lerp = 0;
                m_masterTweener = DOTween.To(() => lerp, x => lerp = x, 1f, pFadeDuration)
                    .SetUpdate(true)
                    .OnUpdate(() =>
                    {
                        m_masterVolume = Mathf.Lerp(fromVal, pValue, lerp);
                        m_musicSource.volume = m_masterVolume * m_musicVolume;
                        m_sfxSourceUnlimited.volume = m_masterVolume * m_sfxVolume;
                        foreach (var source in m_sfxSources)
                            source.volume = m_masterVolume * m_sfxVolume;
                    })
                    .OnComplete(() =>
                    {
                        pOnComplete?.Invoke();
                    });
#else
				// Fallback to a coroutine if DOTween is not available.
				StartCoroutine(IELerp(pFadeDuration,
					lerp =>
					{
						m_masterVolume = Mathf.Lerp(fromVal, pValue, lerp);
						m_musicSource.volume = m_masterVolume * m_musicVolume;
						m_sfxSourceUnlimited.volume = m_masterVolume * m_sfxVolume;
						foreach (var source in m_sfxSources)
							source.volume = m_masterVolume * m_sfxVolume;
					}, () => pOnComplete?.Invoke()));
#endif
            }
        }

		/// <summary>
		/// Creates a new AudioSource for the SFX pool.
		/// </summary>
		/// <returns>The newly created AudioSource.</returns>
		private AudioSource CreateSfxAudioSource()
        {
            if (m_sfxSources.Count == 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    var obj = new GameObject("Sfx_" + m_sfxSources.Count);
                    obj.transform.SetParent(transform);
                    var newAudioSource = obj.AddComponent<AudioSource>();
                    newAudioSource.playOnAwake = false;
                    newAudioSource.loop = false;
                    m_sfxSources.Add(newAudioSource);
                }
                return m_sfxSources[0];
            }
            else
            {
                var obj = new GameObject("Sfx_" + m_sfxSources.Count);
                obj.transform.SetParent(transform);
                var newAudioSource = obj.AddComponent<AudioSource>();
                newAudioSource.playOnAwake = false;
                newAudioSource.loop = false;
                m_sfxSources.Add(newAudioSource);
                return newAudioSource;
            }
        }

		/// <summary>
		/// A coroutine to linearly interpolate a value over time. Used as a fallback when DOTween is unavailable.
		/// </summary>
		private IEnumerator IELerp(float pTime, Action<float> pOnUpdate, Action pOnFinished)
        {
            float time = 0;
            while (time < pTime)
            {
                time += Time.deltaTime;
                pOnUpdate?.Invoke(time / pTime);
                yield return null;
            }
            pOnFinished?.Invoke();
        }

#endregion

#region Musics

		/// <summary>
		/// Enables or disables music playback.
		/// </summary>
		/// <param name="pValue">True to enable music, false to disable.</param>
		public void EnableMusic(bool pValue)
        {
            m_enabledMusic = pValue;
            m_musicSource.mute = !pValue;
        }

		/// <summary>
		/// Sets the volume for music.
		/// </summary>
		/// <param name="pValue">The target volume level (0.0 to 1.0).</param>
		/// <param name="pFadeDuration">The duration of the volume fade in seconds.</param>
		/// <param name="pOnComplete">An optional action to invoke when the fade is complete.</param>
		public void SetMusicVolume(float pValue, float pFadeDuration = 0, Action pOnComplete = null)
        {
#if DOTWEEN
            m_musicTweener.Kill();
#endif
            if (!m_enabledMusic || pValue == m_musicVolume)
            {
                m_musicVolume = pValue;
                m_musicSource.volume = m_masterVolume * m_musicVolume;
                pOnComplete?.Invoke();
                return;
            }

            if (pFadeDuration <= 0)
            {
                m_musicVolume = pValue;
                m_musicSource.volume = m_masterVolume * m_musicVolume;
                pOnComplete?.Invoke();
            }
            else
            {
                float fromVal = m_musicVolume;
#if DOTWEEN
                float lerp = 0;
                m_musicTweener = DOTween.To(() => lerp, x => lerp = x, 1f, pFadeDuration)
                    .SetUpdate(true)
                    .OnUpdate(() =>
                    {
                        m_musicVolume = Mathf.Lerp(fromVal, pValue, lerp);
                        m_musicSource.volume = m_masterVolume * m_musicVolume;
                    })
                    .OnComplete(() =>
                    {
                        pOnComplete?.Invoke();
                    });
#else
				StartCoroutine(IELerp(pFadeDuration, lerp =>
				{
					m_musicVolume = Mathf.Lerp(fromVal, pValue, lerp);
					m_musicSource.volume = m_masterVolume * m_musicVolume;
				}, () =>
				{
					pOnComplete?.Invoke();
				}));
#endif
            }
        }

		/// <summary>
		/// Stops the currently playing music, with an optional fade out.
		/// </summary>
		/// <param name="pFadeDuration">The duration of the fade out in seconds.</param>
		/// <param name="pOnComplete">An optional action to invoke when the music has stopped.</param>
		public void StopMusic(float pFadeDuration = 0, Action pOnComplete = null)
        {
            SetMusicVolume(0, pFadeDuration, () =>
            {
                m_musicSource.Stop();
                pOnComplete?.Invoke();
            });
        }

		/// <summary>
		/// Plays the music that is currently assigned to the music source.
		/// </summary>
		/// <param name="pFadeDuration">The duration of the fade in seconds.</param>
		/// <param name="pVolume">The target volume for the music.</param>
		public void PlayMusic(float pFadeDuration = 0, float pVolume = 1f)
        {
            if (!m_musicSource.isPlaying)
                m_musicSource.Play();
            SetMusicVolume(pVolume, pFadeDuration);
        }

		/// <summary>
		/// Plays a specific music clip.
		/// </summary>
		/// <param name="pClip">The audio clip to play.</param>
		/// <param name="pLoop">Whether the music should loop.</param>
		/// <param name="pFadeDuration">The duration of the fade in seconds.</param>
		/// <param name="pVolume">The target volume for the music.</param>
		public void PlayMusic(AudioClip pClip, bool pLoop, float pFadeDuration = 0, float pVolume = 1f)
        {
            if (pClip == null)
                return;

            bool play = !(m_musicSource.clip == pClip && m_musicSource.isPlaying);

            m_musicSource.clip = pClip;
            m_musicSource.loop = pLoop;
            if (!m_enabledMusic)
	            m_musicSource.mute = true;
            if (play)
                m_musicSource.Play();
            SetMusicVolume(pVolume, pFadeDuration);
        }

		/// <summary>
		/// Plays a sequence of music clips one after another.
		/// </summary>
		/// <param name="pClips">An array of audio clips to play in sequence.</param>
		/// <param name="pFadeDuration">The fade duration for each clip transition.</param>
		/// <param name="pVolume">The target volume for the music.</param>
		public void PlayMusics(AudioClip[] pClips, float pFadeDuration = 0, float pVolume = 1f)
        {
            if (m_playMusicsCoroutine != null)
                StopCoroutine(m_playMusicsCoroutine);
            m_playMusicsCoroutine = StartCoroutine(IEPlayMusics(pClips, pFadeDuration, pVolume));
        }

		/// <summary>
		/// Coroutine to handle the sequential playback of music clips.
		/// </summary>
		private IEnumerator IEPlayMusics(AudioClip[] pClips, float pFadeDuration = 0, float pVolume = 1f)
        {
            if (pClips == null || pClips.Length == 0)
                yield break;

            if (pClips.Length == 1)
            {
                PlayMusic(pClips[0], true, pFadeDuration, pVolume);
                yield break;
            }

            int index = 0;
            for (int i = 0; i < pClips.Length; i++)
                if (pClips[i] == m_musicSource.clip)
                {
                    index = i;
                    break;
                }
            while (true)
            {
                bool play = true;
                var clip = pClips[index];

                if (m_musicSource.clip == clip && m_musicSource.isPlaying)
                    play = false;

                if (play)
                {
                    m_musicSource.clip = clip;
                    m_musicSource.loop = false;
                    m_musicSource.Play();
                }

                if (m_enabledMusic)
                    SetMusicVolume(pVolume, pFadeDuration);
                else
                    SetMusicVolume(0);

                yield return new WaitUntil(() => !m_musicSource.isPlaying || m_musicSource.clip == null);
                index = (index + 1) % pClips.Length;
            }
        }

		/// <summary>
		/// Checks if the music source is currently playing.
		/// </summary>
		/// <returns>True if music is playing, false otherwise.</returns>
		public bool IsPlayingMusic() => m_musicSource.isPlaying;

		/// <summary>
		/// Plays a music clip by its file name from the AudioCollection.
		/// </summary>
		/// <param name="pFileName">The name of the music clip.</param>
		/// <param name="pLoop">Whether the music should loop.</param>
		/// <param name="pFadeDuration">The duration of the fade in seconds.</param>
		public void PlayMusic(string pFileName, bool pLoop = false, float pFadeDuration = 0)
        {
            var clip = audioCollection.GetMusicClip(pFileName);
            PlayMusic(clip, pLoop, pFadeDuration);
        }

		/// <summary>
		/// Plays a music clip by its ID from the AudioCollection.
		/// </summary>
		/// <param name="pId">The ID of the music clip.</param>
		/// <param name="pLoop">Whether the music should loop.</param>
		/// <param name="pFadeDuration">The duration of the fade in seconds.</param>
		/// <param name="pVolume">The target volume for the music.</param>
		public void PlayMusicById(int pId, bool pLoop = false, float pFadeDuration = 0, float pVolume = 1f)
        {
            var clip = audioCollection.GetMusicClip(pId);
            PlayMusic(clip, pLoop, pFadeDuration, pVolume);
        }

		/// <summary>
		/// Plays a sequence of music clips by their IDs from the AudioCollection.
		/// </summary>
		/// <param name="pIds">An array of music clip IDs.</param>
		/// <param name="pFadeDuration">The fade duration for each clip transition.</param>
		/// <param name="pVolume">The target volume for the music.</param>
		public void PlayMusicByIds(int[] pIds, float pFadeDuration = 0, float pVolume = 1f)
        {
            var clips = new AudioClip[pIds.Length];
            for (int i = 0; i < pIds.Length; i++)
                clips[i] = audioCollection.GetMusicClip(pIds[i]);
            PlayMusics(clips, pFadeDuration, pVolume);
        }

#endregion

#region SFX

		/// <summary>
		/// Enables or disables sound effect playback.
		/// </summary>
		/// <param name="pValue">True to enable SFX, false to disable.</param>
		public void EnableSFX(bool pValue)
        {
            m_enabledSfx = pValue;
            foreach (var s in m_sfxSources)
                s.mute = !pValue;
            m_sfxSourceUnlimited.mute = !pValue;
        }

		/// <summary>
		/// Sets the volume for sound effects.
		/// </summary>
		/// <param name="pValue">The target volume level (0.0 to 1.0).</param>
		/// <param name="pFadeDuration">The duration of the volume fade in seconds.</param>
		/// <param name="pOnComplete">An optional action to invoke when the fade is complete.</param>
		public void SetSFXVolume(float pValue, float pFadeDuration = 0, Action pOnComplete = null)
        {
#if DOTWEEN
            m_sfxTweener.Kill();
#endif
            if (pValue == m_sfxVolume)
            {
                pOnComplete?.Invoke();
                return;
            }

            if (pFadeDuration <= 0)
            {
                m_sfxVolume = pValue;
                m_sfxSourceUnlimited.volume = m_masterVolume * pValue;
                foreach (var sound in m_sfxSources)
                    sound.volume = m_masterVolume * pValue;
                pOnComplete?.Invoke();
            }
            else
            {
                float fromVal = m_sfxVolume;
#if DOTWEEN
                float lerp = 0;
                m_sfxTweener = DOTween.To(() => lerp, x => lerp = x, 1f, pFadeDuration)
                    .SetUpdate(true)
                    .OnUpdate(() =>
                    {
                        m_sfxVolume = Mathf.Lerp(fromVal, pValue, lerp);
                        m_sfxSourceUnlimited.volume = m_masterVolume * m_sfxVolume;
                        foreach (var sound in m_sfxSources)
                            sound.volume = m_masterVolume * m_sfxVolume;
                    })
                    .OnComplete(() =>
                    {
                        pOnComplete?.Invoke();
                    });
#else
				StartCoroutine(IELerp(pFadeDuration, lerp =>
				{
					m_sfxVolume = Mathf.Lerp(fromVal, pValue, lerp);
					m_sfxSourceUnlimited.volume = m_masterVolume * m_sfxVolume;
					foreach (var sound in m_sfxSources)
						sound.volume = m_masterVolume * m_sfxVolume;
				}, () => pOnComplete?.Invoke()));
#endif
            }
        }

		/// <summary>
		/// Stops all playing instances of a specific sound effect clip.
		/// </summary>
		/// <param name="pClip">The sound effect clip to stop.</param>
		public void StopSFX(AudioClip pClip)
        {
            if (pClip == null)
                return;

            for (int i = 0; i < m_sfxSources.Count; i++)
            {
                if (m_sfxSources[i].clip != null && m_sfxSources[i].clip.GetInstanceID() == pClip.GetInstanceID())
                {
                    m_sfxSources[i].Stop();
                    m_sfxSources[i].clip = null;
                }
            }

            if (m_sfxSourceUnlimited.clip == pClip)
            {
                m_sfxSourceUnlimited.Stop();
                m_sfxSourceUnlimited.clip = null;
            }
        }

		/// <summary>
		/// Stops all currently playing sound effects.
		/// </summary>
		public void StopSFXs()
        {
            for (int i = 0; i < m_sfxSources.Count; i++)
            {
                m_sfxSources[i].Stop();
                m_sfxSources[i].clip = null;
            }

            m_sfxSourceUnlimited.Stop();
            m_sfxSourceUnlimited.clip = null;
        }
		
		/// <summary>
		/// Retrieves an available AudioSource for playing a sound effect.
		/// Implements logic to limit the number of concurrently playing instances of the same clip.
		/// </summary>
		/// <returns>An available AudioSource, or null if none are available.</returns>
		private AudioSource GetSFXSource(AudioClip pClip, int pLimitNumber, bool pLoop)
        {
            try
            {
                if (pLimitNumber > 0 || pLoop)
                {
                    if (!pLoop)
                    {
                        int countSameClips = 0;
                        for (int i = m_sfxSources.Count - 1; i >= 0; i--)
                        {
                            if (m_sfxSources[i].isPlaying && m_sfxSources[i].clip != null && m_sfxSources[i].clip.GetInstanceID() == pClip.GetInstanceID())
                                countSameClips++;
                            else if (!m_sfxSources[i].isPlaying)
                                m_sfxSources[i].clip = null;
                        }
                        if (countSameClips < pLimitNumber)
                        {
                            for (int i = m_sfxSources.Count - 1; i >= 0; i--)
                                if (m_sfxSources[i].clip == null)
                                    return m_sfxSources[i];

                            return CreateSfxAudioSource();
                        }
                    }
                    else
                    {
                        for (int i = m_sfxSources.Count - 1; i >= 0; i--)
                            if (m_sfxSources[i].clip == null
                                || !m_sfxSources[i].isPlaying
                                || m_sfxSources[i].clip.GetInstanceID() == pClip.GetInstanceID())
                                return m_sfxSources[i];
                    }
                }
                else
                {
                    return m_sfxSourceUnlimited;
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
                return null;
            }
        }

		/// <summary>
		/// Plays a sound effect clip.
		/// </summary>
		/// <param name="pClip">The audio clip to play.</param>
		/// <param name="limitNumber">The maximum number of concurrent instances of this clip. 0 for unlimited.</param>
		/// <param name="pLoop">Whether the sound effect should loop.</param>
		/// <param name="pPitchRandomMultiplier">A multiplier for pitch randomization. e.g., 1.1 for a 10% variance.</param>
		/// <returns>The AudioSource that is playing the sound, or null if it could not be played.</returns>
		public AudioSource PlaySFX(AudioClip pClip, int limitNumber = 0, bool pLoop = false, float pPitchRandomMultiplier = 1)
        {
            if (pClip == null)
                return null;
            var source = GetSFXSource(pClip, limitNumber, pLoop);
            if (source == null)
                return null;
            source.volume = m_masterVolume * m_sfxVolume;
            source.loop = pLoop;
            source.clip = pClip;
            source.pitch = 1;
            if (pPitchRandomMultiplier != 1)
            {
                if (Random.value < .5)
                    source.pitch *= Random.Range(1 / pPitchRandomMultiplier, 1);
                else
                    source.pitch *= Random.Range(1, pPitchRandomMultiplier);
            }
            if (!pLoop)
                source.PlayOneShot(pClip);
            else
                source.Play();
            return source;
        }

		/// <summary>
		/// Plays a sound effect by its file name from the AudioCollection.
		/// </summary>
		public AudioSource PlaySFX(string pFileName, int limitNumber = 0, bool pLoop = false, float pPitchRandomMultiplier = 1)
        {
            if (!m_enabledSfx) return null;
            var clip = audioCollection.GetSFXClip(pFileName);
            return PlaySFX(clip, limitNumber, pLoop, pPitchRandomMultiplier);
        }

		/// <summary>
		/// Plays a sound effect by its index from the AudioCollection.
		/// </summary>
		public AudioSource PlaySFX(int pIndex, int limitNumber = 0, bool pLoop = false, float pPitchRandomMultiplier = 1)
        {
            if (!m_enabledSfx) return null;
            var clip = audioCollection.GetSFXClip(pIndex);
            return PlaySFX(clip, limitNumber, pLoop, pPitchRandomMultiplier);
        }

		/// <summary>
		/// Stops a sound effect by its index from the AudioCollection.
		/// </summary>
		/// <param name="pIndex">The index of the sound effect to stop.</param>
		public void StopSFX(int pIndex)
        {
            var clip = audioCollection.GetSFXClip(pIndex);
            StopSFX(clip);
        }

#endregion

#if UNITY_EDITOR
		/// <summary>
		/// Editor-only method to automatically find and set up AudioSource components.
		/// This runs when the script is loaded or a value is changed in the Inspector.
		/// </summary>
		protected virtual void OnValidate()
        {
            m_sfxSources ??= new List<AudioSource>();
            var audioSources = gameObject.GetComponentsInChildren<AudioSource>(true);

			// Find and assign the main music and sfx sources by name
            foreach (var source in audioSources)
            {
                if (m_musicSource == null && source.gameObject.name.Contains("Music"))
                    m_musicSource = source;
                if (m_sfxSourceUnlimited == null && source.gameObject.name.Contains("Sfx"))
                    m_sfxSourceUnlimited = source;
            }

			// If they don't exist, create them
            if (m_musicSource == null)
            {
                var obj = new GameObject("Music");
                obj.transform.SetParent(transform);
                m_musicSource = obj.AddComponent<AudioSource>();
            }
            if (m_sfxSourceUnlimited == null)
            {
                var obj = new GameObject("Sfx");
                obj.transform.SetParent(transform);
                m_sfxSourceUnlimited = obj.AddComponent<AudioSource>();
            }

			// Populate the limited SFX sources list
            foreach (var source in audioSources)
            {
                if (source != m_musicSource && source != m_sfxSourceUnlimited)
                {
                    if (!m_sfxSources.Contains(source))
                        m_sfxSources.Add(source);
                }
                else
                    m_sfxSources.Remove(source);
            }

			// Set default properties for the main sources
            m_sfxSourceUnlimited.loop = false;
            m_sfxSourceUnlimited.playOnAwake = false;
            m_musicSource.loop = true;
            m_musicSource.playOnAwake = false;

			// Apply volume changes in the editor in real-time
#if DOTWEEN
            if (m_masterTweener == null || !m_masterTweener.IsPlaying())
                SetMasterVolume(m_masterVolume);
            if (m_musicTweener == null || !m_musicTweener.IsPlaying())
                SetMusicVolume(m_musicVolume);
            if (m_sfxTweener == null || !m_sfxTweener.IsPlaying())
                SetSFXVolume(m_sfxVolume);
#endif
        }
#endif
    }
}