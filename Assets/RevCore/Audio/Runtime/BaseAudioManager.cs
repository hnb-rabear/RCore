using System;
using System.Collections;
using System.Collections.Generic;
using RevCore.Inspector;
using UnityEngine;
#if DOTWEEN
using DG.Tweening;
#endif
using Random = UnityEngine.Random;

namespace RevCore
{
    public class BaseAudioManager : MonoBehaviour
    {
        [AutoFill] public AudioCollection audioCollection;
        [SerializeField] protected bool m_enabledSfx = true;
        [SerializeField] protected bool m_enabledMusic = true;
        [SerializeField] protected List<AudioSource> m_sfxSources;
        [SerializeField] public AudioSource m_sfxSourceUnlimited;
        [SerializeField] public AudioSource m_musicSource;
        [SerializeField, Range(0, 1f)] protected float m_masterVolume = 1f;
        [SerializeField, Range(0, 1f)] protected float m_sfxVolume = 1f;
        [SerializeField, Range(0, 1f)] protected float m_musicVolume = 1f;

        public bool EnabledSFX => m_enabledSfx;
        public bool EnabledMusic => m_enabledMusic;
        public float MasterVolume => m_masterVolume;
        public float SFXVolume => m_sfxVolume;
        public float MusicVolume => m_musicVolume;

#if DOTWEEN
        private Tweener m_masterTweener;
        private Tweener m_musicTweener;
        private Tweener m_sfxTweener;
#endif
        private Coroutine m_playMusicsCoroutine;

        protected virtual void Start()
        {
            m_musicSource.volume = m_masterVolume * m_musicVolume;
            m_sfxSourceUnlimited.volume = m_masterVolume * m_sfxVolume;
            foreach (var sound in m_sfxSources)
                sound.volume = m_masterVolume * m_sfxVolume;
        }

        #region Common

        public void SetMasterVolume(float value, float fadeDuration = 0, Action onComplete = null)
        {
#if DOTWEEN
            m_masterTweener.Kill();
#endif
            if (m_masterVolume == value)
            {
                onComplete?.Invoke();
                return;
            }

            if (fadeDuration <= 0)
            {
                m_masterVolume = value;
                m_musicSource.volume = m_masterVolume * m_musicVolume;
                m_sfxSourceUnlimited.volume = m_masterVolume * m_sfxVolume;
                foreach (var source in m_sfxSources)
                    source.volume = m_masterVolume * m_sfxVolume;
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
                        m_musicSource.volume = m_masterVolume * m_musicVolume;
                        m_sfxSourceUnlimited.volume = m_masterVolume * m_sfxVolume;
                        foreach (var source in m_sfxSources)
                            source.volume = m_masterVolume * m_sfxVolume;
                    })
                    .OnComplete(() => onComplete?.Invoke());
#else
                StartCoroutine(LerpCoroutine(fadeDuration,
                    t =>
                    {
                        m_masterVolume = Mathf.Lerp(fromVal, value, t);
                        m_musicSource.volume = m_masterVolume * m_musicVolume;
                        m_sfxSourceUnlimited.volume = m_masterVolume * m_sfxVolume;
                        foreach (var source in m_sfxSources)
                            source.volume = m_masterVolume * m_sfxVolume;
                    }, () => onComplete?.Invoke()));
#endif
            }
        }

        private AudioSource CreateSfxAudioSource()
        {
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

        public void EnableMusic(bool value)
        {
            m_enabledMusic = value;
            m_musicSource.mute = !value;
        }

        public void SetMusicVolume(float value, float fadeDuration = 0, Action onComplete = null)
        {
#if DOTWEEN
            m_musicTweener.Kill();
#endif
            if (!m_enabledMusic || value == m_musicVolume)
            {
                m_musicVolume = value;
                m_musicSource.volume = m_masterVolume * m_musicVolume;
                onComplete?.Invoke();
                return;
            }

            if (fadeDuration <= 0)
            {
                m_musicVolume = value;
                m_musicSource.volume = m_masterVolume * m_musicVolume;
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
                        m_musicSource.volume = m_masterVolume * m_musicVolume;
                    })
                    .OnComplete(() => onComplete?.Invoke());
#else
                StartCoroutine(LerpCoroutine(fadeDuration, t =>
                {
                    m_musicVolume = Mathf.Lerp(fromVal, value, t);
                    m_musicSource.volume = m_masterVolume * m_musicVolume;
                }, () => onComplete?.Invoke()));
#endif
            }
        }

        public void StopMusic(float fadeDuration = 0, Action onComplete = null)
        {
            SetMusicVolume(0, fadeDuration, () =>
            {
                m_musicSource.Stop();
                onComplete?.Invoke();
            });
        }

        public void PlayMusic(float fadeDuration = 0, float volume = 1f)
        {
            if (!m_musicSource.isPlaying)
                m_musicSource.Play();
            SetMusicVolume(volume, fadeDuration);
        }

        public void PlayMusic(AudioClip clip, bool loop, float fadeDuration = 0, float volume = 1f)
        {
            if (clip == null)
                return;

            bool play = !(m_musicSource.clip == clip && m_musicSource.isPlaying);
            m_musicSource.clip = clip;
            m_musicSource.loop = loop;
            if (!m_enabledMusic)
                m_musicSource.mute = true;
            if (play)
                m_musicSource.Play();
            SetMusicVolume(volume, fadeDuration);
        }

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

            int index = 0;
            for (int i = 0; i < clips.Length; i++)
                if (clips[i] == m_musicSource.clip)
                {
                    index = i;
                    break;
                }

            while (true)
            {
                bool play = true;
                var clip = clips[index];
                if (m_musicSource.clip == clip && m_musicSource.isPlaying)
                    play = false;

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

        public bool IsPlayingMusic() => m_musicSource.isPlaying;

        public void PlayMusic(string fileName, bool loop = false, float fadeDuration = 0)
        {
            var clip = audioCollection.GetMusicClip(fileName);
            PlayMusic(clip, loop, fadeDuration);
        }

        public void PlayMusicById(int id, bool loop = false, float fadeDuration = 0, float volume = 1f)
        {
            var clip = audioCollection.GetMusicClip(id);
            PlayMusic(clip, loop, fadeDuration, volume);
        }

        public void PlayMusicByIds(int[] ids, float fadeDuration = 0, float volume = 1f)
        {
            var clips = new AudioClip[ids.Length];
            for (int i = 0; i < ids.Length; i++)
                clips[i] = audioCollection.GetMusicClip(ids[i]);
            PlayMusics(clips, fadeDuration, volume);
        }

        #endregion

        #region SFX

        public void EnableSFX(bool value)
        {
            m_enabledSfx = value;
            foreach (var s in m_sfxSources)
                s.mute = !value;
            m_sfxSourceUnlimited.mute = !value;
        }

        public void SetSFXVolume(float value, float fadeDuration = 0, Action onComplete = null)
        {
#if DOTWEEN
            m_sfxTweener.Kill();
#endif
            if (value == m_sfxVolume)
            {
                onComplete?.Invoke();
                return;
            }

            if (fadeDuration <= 0)
            {
                m_sfxVolume = value;
                m_sfxSourceUnlimited.volume = m_masterVolume * value;
                foreach (var sound in m_sfxSources)
                    sound.volume = m_masterVolume * value;
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
                        m_sfxSourceUnlimited.volume = m_masterVolume * m_sfxVolume;
                        foreach (var sound in m_sfxSources)
                            sound.volume = m_masterVolume * m_sfxVolume;
                    })
                    .OnComplete(() => onComplete?.Invoke());
#else
                StartCoroutine(LerpCoroutine(fadeDuration, t =>
                {
                    m_sfxVolume = Mathf.Lerp(fromVal, value, t);
                    m_sfxSourceUnlimited.volume = m_masterVolume * m_sfxVolume;
                    foreach (var sound in m_sfxSources)
                        sound.volume = m_masterVolume * m_sfxVolume;
                }, () => onComplete?.Invoke()));
#endif
            }
        }

        public void StopSFX(AudioClip clip)
        {
            if (clip == null)
                return;

            for (int i = 0; i < m_sfxSources.Count; i++)
            {
                if (m_sfxSources[i].clip != null && m_sfxSources[i].clip.GetInstanceID() == clip.GetInstanceID())
                {
                    m_sfxSources[i].Stop();
                    m_sfxSources[i].clip = null;
                }
            }

            if (m_sfxSourceUnlimited.clip == clip)
            {
                m_sfxSourceUnlimited.Stop();
                m_sfxSourceUnlimited.clip = null;
            }
        }

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

        private AudioSource GetSFXSource(AudioClip clip, int limitNumber, bool loop)
        {
            try
            {
                if (limitNumber > 0 || loop)
                {
                    if (!loop)
                    {
                        int countSameClips = 0;
                        for (int i = m_sfxSources.Count - 1; i >= 0; i--)
                        {
                            if (m_sfxSources[i].isPlaying && m_sfxSources[i].clip != null && m_sfxSources[i].clip.GetInstanceID() == clip.GetInstanceID())
                                countSameClips++;
                            else if (!m_sfxSources[i].isPlaying)
                                m_sfxSources[i].clip = null;
                        }
                        if (countSameClips < limitNumber)
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
                                || m_sfxSources[i].clip.GetInstanceID() == clip.GetInstanceID())
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
            return source;
        }

        public AudioSource PlaySFX(string fileName, int limitNumber = 0, bool loop = false, float pitchRandomMultiplier = 1)
        {
            if (!m_enabledSfx) return null;
            var clip = audioCollection.GetSFXClip(fileName);
            return PlaySFX(clip, limitNumber, loop, pitchRandomMultiplier);
        }

        public AudioSource PlaySFX(int index, int limitNumber = 0, bool loop = false, float pitchRandomMultiplier = 1)
        {
            if (!m_enabledSfx) return null;
            var clip = audioCollection.GetSFXClip(index);
            return PlaySFX(clip, limitNumber, loop, pitchRandomMultiplier);
        }

        public void StopSFX(int index)
        {
            var clip = audioCollection.GetSFXClip(index);
            StopSFX(clip);
        }

        #endregion

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            m_sfxSources ??= new List<AudioSource>();
            var audioSources = gameObject.GetComponentsInChildren<AudioSource>(true);

            foreach (var source in audioSources)
            {
                if (m_musicSource == null && source.gameObject.name.Contains("Music"))
                    m_musicSource = source;
                if (m_sfxSourceUnlimited == null && source.gameObject.name.Contains("Sfx"))
                    m_sfxSourceUnlimited = source;
            }

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

            m_sfxSourceUnlimited.loop = false;
            m_sfxSourceUnlimited.playOnAwake = false;
            m_musicSource.loop = true;
            m_musicSource.playOnAwake = false;

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
