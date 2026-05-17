using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if DOTWEEN
using DG.Tweening;
#endif
using Random = UnityEngine.Random;

namespace RevCore
{
    /// <summary>
    /// Inheritable audio system component. Maintains separate volume axes (master / SFX / music),
    /// a pool of SFX <see cref="AudioSource"/>s for concurrent playback, a dedicated unlimited
    /// SFX source for fire-and-forget effects, and a music source. With the <c>DOTWEEN</c> define,
    /// volume setters can fade smoothly; without it they snap.
    /// </summary>
    /// <remarks>
    /// Single instance is the typical usage — see <see cref="AudioManager"/>. Subclass when you need
    /// to override <see cref="Start"/> or wire up additional event subscriptions.
    /// </remarks>
    public class BaseAudioManager : MonoBehaviour
    {
        /// <summary>The asset that lists all sound effect and music clips this manager can play.</summary>
        [AutoFill] public AudioCollection audioCollection;
        [SerializeField] protected bool m_enabledSfx = true;
        [SerializeField] protected bool m_enabledMusic = true;
        [SerializeField] protected List<AudioSource> m_sfxSources;
        /// <summary>Source used by <see cref="PlaySFX(AudioClip, int, bool, float)"/> overloads that don't care about voice counting.</summary>
        [SerializeField] public AudioSource m_sfxSourceUnlimited;
        /// <summary>Source used for music playback. Auto-created on first use if not assigned in the inspector.</summary>
        [SerializeField] public AudioSource m_musicSource;
        [SerializeField, Range(0, 1f)] protected float m_masterVolume = 1f;
        [SerializeField, Range(0, 1f)] protected float m_sfxVolume = 1f;
        [SerializeField, Range(0, 1f)] protected float m_musicVolume = 1f;

        /// <summary>True when SFX playback is enabled. Toggle via <see cref="EnableSFX"/>.</summary>
        public bool EnabledSFX => m_enabledSfx;
        /// <summary>True when music playback is enabled. Toggle via <see cref="EnableMusic"/>.</summary>
        public bool EnabledMusic => m_enabledMusic;
        /// <summary>Master volume (0..1). Multiplied with both SFX and music volumes at play time.</summary>
        public float MasterVolume => m_masterVolume;
        /// <summary>SFX-only volume axis (0..1).</summary>
        public float SFXVolume => m_sfxVolume;
        /// <summary>Music-only volume axis (0..1).</summary>
        public float MusicVolume => m_musicVolume;

#if DOTWEEN
        private Tweener m_masterTweener;
        private Tweener m_musicTweener;
        private Tweener m_sfxTweener;
#endif
        private Coroutine m_playMusicsCoroutine;

        protected virtual void Start()
        {
            EnsureAudioSources();
            SetMusicSourceVolume();
            SetSfxSourcesVolume();
        }

        #region Common

        private void EnsureAudioSources()
        {
            m_sfxSources ??= new List<AudioSource>();

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

            ConfigureAudioSource(m_musicSource, true);
            ConfigureAudioSource(m_sfxSourceUnlimited, false);

            for (int i = m_sfxSources.Count - 1; i >= 0; i--)
            {
                if (m_sfxSources[i] == null)
                    m_sfxSources.RemoveAt(i);
                else
                    ConfigureAudioSource(m_sfxSources[i], false);
            }
        }

        private static void ConfigureAudioSource(AudioSource source, bool loop)
        {
            if (source == null)
                return;

            source.loop = loop;
            source.playOnAwake = false;
        }

        private void SetMusicSourceVolume()
        {
            if (m_musicSource != null)
                m_musicSource.volume = m_masterVolume * m_musicVolume;
        }

        private void SetSfxSourcesVolume()
        {
            if (m_sfxSourceUnlimited != null)
                m_sfxSourceUnlimited.volume = m_masterVolume * m_sfxVolume;

            if (m_sfxSources == null)
                return;

            foreach (var source in m_sfxSources)
                if (source != null)
                    source.volume = m_masterVolume * m_sfxVolume;
        }

        /// <summary>Sets <see cref="MasterVolume"/>. With <c>DOTWEEN</c>, fades over <paramref name="fadeDuration"/> seconds; without it, snaps. <paramref name="onComplete"/> runs after the fade.</summary>
        public void SetMasterVolume(float value, float fadeDuration = 0, Action onComplete = null)
        {
#if DOTWEEN
            m_masterTweener?.Kill();
#endif
            if (m_masterVolume == value)
            {
                onComplete?.Invoke();
                return;
            }

            EnsureAudioSources();

            if (fadeDuration <= 0)
            {
                m_masterVolume = value;
                SetMusicSourceVolume();
                SetSfxSourcesVolume();
                onComplete?.Invoke();
            }
            else
            {
                float fromVal = m_masterVolume;
#if DOTWEEN
                float lerp = 0;
                m_masterTweener = DOTween.To(() => lerp, x => lerp = x, 1f, fadeDuration)
                    .SetUpdate(true)
                    .OnUpdate(() =>
                    {
                        m_masterVolume = Mathf.Lerp(fromVal, value, lerp);
                        SetMusicSourceVolume();
                        SetSfxSourcesVolume();
                    })
                    .OnComplete(() => onComplete?.Invoke());
#else
                StartCoroutine(LerpCoroutine(fadeDuration,
                    t =>
                    {
                        m_masterVolume = Mathf.Lerp(fromVal, value, t);
                        SetMusicSourceVolume();
                        SetSfxSourcesVolume();
                    }, () => onComplete?.Invoke()));
#endif
            }
        }

        private AudioSource CreateSfxAudioSource()
        {
            EnsureAudioSources();

            if (m_sfxSources.Count == 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    var obj = new GameObject("Sfx_" + m_sfxSources.Count);
                    obj.transform.SetParent(transform);
                    var src = obj.AddComponent<AudioSource>();
                    src.playOnAwake = false;
                    src.loop = false;
                    m_sfxSources.Add(src);
                }
                return m_sfxSources[0];
            }

            var newObj = new GameObject("Sfx_" + m_sfxSources.Count);
            newObj.transform.SetParent(transform);
            var newSrc = newObj.AddComponent<AudioSource>();
            newSrc.playOnAwake = false;
            newSrc.loop = false;
            m_sfxSources.Add(newSrc);
            return newSrc;
        }

        private IEnumerator LerpCoroutine(float duration, Action<float> onUpdate, Action onFinished)
        {
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                onUpdate?.Invoke(time / duration);
                yield return null;
            }
            onFinished?.Invoke();
        }

        #endregion

        #region Music

        /// <summary>Toggles music playback. Disabling stops the music source; enabling resumes from the last clip.</summary>
        public void EnableMusic(bool value)
        {
            m_enabledMusic = value;
            EnsureAudioSources();
            m_musicSource.mute = !value;
        }

        /// <summary>Sets <see cref="MusicVolume"/>. With <c>DOTWEEN</c>, fades over <paramref name="fadeDuration"/> seconds; without it, snaps. <paramref name="onComplete"/> runs after the fade.</summary>
        public void SetMusicVolume(float value, float fadeDuration = 0, Action onComplete = null)
        {
#if DOTWEEN
            m_musicTweener?.Kill();
#endif
            EnsureAudioSources();

            if (!m_enabledMusic || value == m_musicVolume)
            {
                m_musicVolume = value;
                SetMusicSourceVolume();
                onComplete?.Invoke();
                return;
            }

            if (fadeDuration <= 0)
            {
                m_musicVolume = value;
                SetMusicSourceVolume();
                onComplete?.Invoke();
            }
            else
            {
                float fromVal = m_musicVolume;
#if DOTWEEN
                float lerp = 0;
                m_musicTweener = DOTween.To(() => lerp, x => lerp = x, 1f, fadeDuration)
                    .SetUpdate(true)
                    .OnUpdate(() =>
                    {
                        m_musicVolume = Mathf.Lerp(fromVal, value, lerp);
                        SetMusicSourceVolume();
                    })
                    .OnComplete(() => onComplete?.Invoke());
#else
                StartCoroutine(LerpCoroutine(fadeDuration, t =>
                {
                    m_musicVolume = Mathf.Lerp(fromVal, value, t);
                    SetMusicSourceVolume();
                }, () => onComplete?.Invoke()));
#endif
            }
        }

        /// <summary>Stops the music source. With <c>DOTWEEN</c>, fades out over <paramref name="fadeDuration"/>; without it, stops immediately.</summary>
        public void StopMusic(float fadeDuration = 0, Action onComplete = null)
        {
            SetMusicVolume(0, fadeDuration, () =>
            {
                if (m_musicSource != null)
                    m_musicSource.Stop();
                onComplete?.Invoke();
            });
        }

        /// <summary>Resumes playback of the last music clip assigned to <see cref="m_musicSource"/>. Fades in over <paramref name="fadeDuration"/> with DOTWEEN.</summary>
        public void PlayMusic(float fadeDuration = 0, float volume = 1f)
        {
            EnsureAudioSources();
            if (!m_musicSource.isPlaying)
                m_musicSource.Play();
            SetMusicVolume(volume, fadeDuration);
        }

        /// <summary>Plays <paramref name="clip"/> on the music source. <paramref name="loop"/> selects looping; fade and volume control the transition.</summary>
        public void PlayMusic(AudioClip clip, bool loop, float fadeDuration = 0, float volume = 1f)
        {
            if (clip == null)
                return;

            EnsureAudioSources();
            bool play = !(m_musicSource.clip == clip && m_musicSource.isPlaying);
            m_musicSource.clip = clip;
            m_musicSource.loop = loop;
            if (!m_enabledMusic)
                m_musicSource.mute = true;
            if (play)
                m_musicSource.Play();
            SetMusicVolume(volume, fadeDuration);
            RevDiagnostics.Listener?.OnAudioPlayMusic(clip.name, loop);
        }

        /// <summary>Plays <paramref name="clips"/> sequentially via a coroutine, looping the whole sequence.</summary>
        public void PlayMusics(AudioClip[] clips, float fadeDuration = 0, float volume = 1f)
        {
            if (m_playMusicsCoroutine != null)
                StopCoroutine(m_playMusicsCoroutine);
            m_playMusicsCoroutine = StartCoroutine(PlayMusicsCoroutine(clips, fadeDuration, volume));
        }

        private IEnumerator PlayMusicsCoroutine(AudioClip[] clips, float fadeDuration = 0, float volume = 1f)
        {
            if (clips == null || clips.Length == 0)
                yield break;

            if (clips.Length == 1)
            {
                PlayMusic(clips[0], true, fadeDuration, volume);
                yield break;
            }

            EnsureAudioSources();

            int index = 0;
            for (int i = 0; i < clips.Length; i++)
                if (clips[i] == m_musicSource.clip)
                {
                    index = i;
                    break;
                }

            while (true)
            {
                var clip = clips[index];
                if (clip == null)
                {
                    index = (index + 1) % clips.Length;
                    yield return null;
                    continue;
                }

                bool play = !(m_musicSource.clip == clip && m_musicSource.isPlaying);

                if (play)
                {
                    m_musicSource.clip = clip;
                    m_musicSource.loop = false;
                    m_musicSource.Play();
                }

                if (m_enabledMusic)
                    SetMusicVolume(volume, fadeDuration);
                else
                    SetMusicVolume(0);

                yield return new WaitUntil(() => !m_musicSource.isPlaying || m_musicSource.clip == null);
                index = (index + 1) % clips.Length;
            }
        }

        /// <summary>True when the music source is currently playing (any clip).</summary>
        public bool IsPlayingMusic() => m_musicSource != null && m_musicSource.isPlaying;

        /// <summary>Resolves <paramref name="fileName"/> against <see cref="audioCollection"/> and plays it on the music source.</summary>
        public void PlayMusic(string fileName, bool loop = false, float fadeDuration = 0)
        {
            if (audioCollection == null)
                return;

            var clip = audioCollection.GetMusicClip(fileName);
            PlayMusic(clip, loop, fadeDuration);
        }

        /// <summary>Plays the music clip at <see cref="audioCollection"/> index <paramref name="id"/>.</summary>
        public void PlayMusicById(int id, bool loop = false, float fadeDuration = 0, float volume = 1f)
        {
            if (audioCollection == null)
                return;

            var clip = audioCollection.GetMusicClip(id);
            PlayMusic(clip, loop, fadeDuration, volume);
        }

        /// <summary>Plays the music clips at the given <see cref="audioCollection"/> indices in sequence.</summary>
        public void PlayMusicByIds(int[] ids, float fadeDuration = 0, float volume = 1f)
        {
            if (audioCollection == null || ids == null || ids.Length == 0)
                return;

            var clips = new AudioClip[ids.Length];
            for (int i = 0; i < ids.Length; i++)
                clips[i] = audioCollection.GetMusicClip(ids[i]);
            PlayMusics(clips, fadeDuration, volume);
        }

        #endregion

        #region SFX

        /// <summary>Toggles SFX playback. Disabling stops every in-flight SFX source.</summary>
        public void EnableSFX(bool value)
        {
            m_enabledSfx = value;
            EnsureAudioSources();
            foreach (var s in m_sfxSources)
                if (s != null)
                    s.mute = !value;
            m_sfxSourceUnlimited.mute = !value;
        }

        /// <summary>Sets <see cref="SFXVolume"/>. With <c>DOTWEEN</c>, fades over <paramref name="fadeDuration"/>; without it, snaps. <paramref name="onComplete"/> runs after the fade.</summary>
        public void SetSFXVolume(float value, float fadeDuration = 0, Action onComplete = null)
        {
#if DOTWEEN
            m_sfxTweener?.Kill();
#endif
            if (value == m_sfxVolume)
            {
                onComplete?.Invoke();
                return;
            }

            EnsureAudioSources();

            if (fadeDuration <= 0)
            {
                m_sfxVolume = value;
                SetSfxSourcesVolume();
                onComplete?.Invoke();
            }
            else
            {
                float fromVal = m_sfxVolume;
#if DOTWEEN
                float lerp = 0;
                m_sfxTweener = DOTween.To(() => lerp, x => lerp = x, 1f, fadeDuration)
                    .SetUpdate(true)
                    .OnUpdate(() =>
                    {
                        m_sfxVolume = Mathf.Lerp(fromVal, value, lerp);
                        SetSfxSourcesVolume();
                    })
                    .OnComplete(() => onComplete?.Invoke());
#else
                StartCoroutine(LerpCoroutine(fadeDuration, t =>
                {
                    m_sfxVolume = Mathf.Lerp(fromVal, value, t);
                    SetSfxSourcesVolume();
                }, () => onComplete?.Invoke()));
#endif
            }
        }

        /// <summary>Stops every active SFX source currently playing <paramref name="clip"/>.</summary>
        public void StopSFX(AudioClip clip)
        {
            if (clip == null)
                return;

            if (m_sfxSources == null)
                return;

            for (int i = 0; i < m_sfxSources.Count; i++)
            {
                if (m_sfxSources[i] != null && m_sfxSources[i].clip != null && m_sfxSources[i].clip.GetInstanceID() == clip.GetInstanceID())
                {
                    m_sfxSources[i].Stop();
                    m_sfxSources[i].clip = null;
                }
            }

            if (m_sfxSourceUnlimited != null && m_sfxSourceUnlimited.clip == clip)
            {
                m_sfxSourceUnlimited.Stop();
                m_sfxSourceUnlimited.clip = null;
            }
        }

        /// <summary>Stops every active SFX source.</summary>
        public void StopSFXs()
        {
            if (m_sfxSources != null)
            {
                for (int i = 0; i < m_sfxSources.Count; i++)
                {
                    if (m_sfxSources[i] != null)
                    {
                        m_sfxSources[i].Stop();
                        m_sfxSources[i].clip = null;
                    }
                }
            }

            if (m_sfxSourceUnlimited != null)
            {
                m_sfxSourceUnlimited.Stop();
                m_sfxSourceUnlimited.clip = null;
            }
        }

        private AudioSource GetSFXSource(AudioClip clip, int limitNumber, bool loop)
        {
            try
            {
                EnsureAudioSources();

                if (limitNumber > 0 || loop)
                {
                    if (!loop)
                    {
                        int countSameClips = 0;
                        for (int i = m_sfxSources.Count - 1; i >= 0; i--)
                        {
                            var source = m_sfxSources[i];
                            if (source == null)
                                continue;

                            if (source.isPlaying && source.clip != null && source.clip.GetInstanceID() == clip.GetInstanceID())
                                countSameClips++;
                            else if (!source.isPlaying)
                                source.clip = null;
                        }
                        if (countSameClips < limitNumber)
                        {
                            for (int i = m_sfxSources.Count - 1; i >= 0; i--)
                                if (m_sfxSources[i] != null && m_sfxSources[i].clip == null)
                                    return m_sfxSources[i];
                            return CreateSfxAudioSource();
                        }
                    }
                    else
                    {
                        for (int i = m_sfxSources.Count - 1; i >= 0; i--)
                        {
                            var source = m_sfxSources[i];
                            if (source == null)
                                continue;

                            if (source.clip == null
                                || !source.isPlaying
                                || source.clip.GetInstanceID() == clip.GetInstanceID())
                                return source;
                        }
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
        /// Plays <paramref name="clip"/> on an available SFX source. <paramref name="limitNumber"/> > 0
        /// caps the number of concurrent instances of this clip. <paramref name="pitchRandomMultiplier"/>
        /// applies a random pitch within [1/m, m].
        /// </summary>
        /// <returns>The <see cref="AudioSource"/> playing the clip, or <c>null</c> if disabled or no slot available.</returns>
        public AudioSource PlaySFX(AudioClip clip, int limitNumber = 0, bool loop = false, float pitchRandomMultiplier = 1)
        {
            if (clip == null)
                return null;
            var source = GetSFXSource(clip, limitNumber, loop);
            if (source == null)
                return null;
            source.volume = m_masterVolume * m_sfxVolume;
            source.loop = loop;
            source.clip = clip;
            source.pitch = 1;
            if (pitchRandomMultiplier != 1)
            {
                if (Random.value < .5f)
                    source.pitch *= Random.Range(1 / pitchRandomMultiplier, 1);
                else
                    source.pitch *= Random.Range(1, pitchRandomMultiplier);
            }
            if (!loop)
                source.PlayOneShot(clip);
            else
                source.Play();
            RevDiagnostics.Listener?.OnAudioPlaySFX(clip.name);
            return source;
        }

        /// <summary>Plays the SFX clip resolved by name from <see cref="audioCollection"/>.</summary>
        public AudioSource PlaySFX(string fileName, int limitNumber = 0, bool loop = false, float pitchRandomMultiplier = 1)
        {
            if (!m_enabledSfx || audioCollection == null)
                return null;

            var clip = audioCollection.GetSFXClip(fileName);
            return PlaySFX(clip, limitNumber, loop, pitchRandomMultiplier);
        }

        /// <summary>Plays the SFX clip at <see cref="audioCollection"/> index <paramref name="index"/>.</summary>
        public AudioSource PlaySFX(int index, int limitNumber = 0, bool loop = false, float pitchRandomMultiplier = 1)
        {
            if (!m_enabledSfx || audioCollection == null)
                return null;

            var clip = audioCollection.GetSFXClip(index);
            return PlaySFX(clip, limitNumber, loop, pitchRandomMultiplier);
        }

        /// <summary>Stops every active SFX source playing the clip at <see cref="audioCollection"/> index <paramref name="index"/>.</summary>
        public void StopSFX(int index)
        {
            if (audioCollection == null)
                return;

            var clip = audioCollection.GetSFXClip(index);
            StopSFX(clip);
        }

        #endregion

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only: instantiates the "Music" and "Sfx" child GameObjects with their
        /// <see cref="AudioSource"/>s the first time the component is added (or after a manual
        /// Reset). Idempotent. Runtime creates these on demand in <see cref="EnsureAudioSources"/>,
        /// so this exists purely so the hierarchy looks complete in the scene view.
        /// </summary>
        protected virtual void Reset()
        {
            m_sfxSources ??= new List<AudioSource>();
            if (m_musicSource == null)
            {
                var obj = new GameObject("Music");
                obj.transform.SetParent(transform);
                m_musicSource = obj.AddComponent<AudioSource>();
                m_musicSource.loop = true;
                m_musicSource.playOnAwake = false;
            }
            if (m_sfxSourceUnlimited == null)
            {
                var obj = new GameObject("Sfx");
                obj.transform.SetParent(transform);
                m_sfxSourceUnlimited = obj.AddComponent<AudioSource>();
                m_sfxSourceUnlimited.loop = false;
                m_sfxSourceUnlimited.playOnAwake = false;
            }
        }

        protected virtual void OnValidate()
        {
            // No structural mutation here. OnValidate fires inside an Awake / CheckConsistency
            // context that forbids SendMessage; AddComponent and new GameObject() both trigger
            // SendMessage and Unity logs warnings ("Music: OnDidAddComponent" etc). Anything
            // that creates GameObjects or components belongs in Reset() (one-shot, on add)
            // or in runtime EnsureAudioSources().
            m_sfxSources ??= new List<AudioSource>();
            var audioSources = gameObject.GetComponentsInChildren<AudioSource>(true);

            foreach (var source in audioSources)
            {
                if (m_musicSource == null && source.gameObject.name.Contains("Music"))
                    m_musicSource = source;
                if (m_sfxSourceUnlimited == null && source.gameObject.name.Contains("Sfx"))
                    m_sfxSourceUnlimited = source;
            }

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

            if (m_sfxSourceUnlimited != null)
            {
                m_sfxSourceUnlimited.loop = false;
                m_sfxSourceUnlimited.playOnAwake = false;
            }
            if (m_musicSource != null)
            {
                m_musicSource.loop = true;
                m_musicSource.playOnAwake = false;
            }

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
