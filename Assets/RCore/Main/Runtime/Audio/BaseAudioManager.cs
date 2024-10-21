using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Serialization;
#if DOTWEEN
using DG.Tweening;
#endif
using Random = UnityEngine.Random;

namespace RCore.Audio
{
	/// <summary>
	/// Used to trigger sfx on UI component
	/// </summary>
	public struct SFXTriggeredEvent : BaseEvent
	{
		public string sfx;
		public SFXTriggeredEvent(string val)
		{
			sfx = val;
		}
	}

    public class BaseAudioManager : MonoBehaviour
    {
        public AudioCollection audioCollection;
        [FormerlySerializedAs("m_EnabledSFX")]
        [SerializeField] protected bool m_enabledSfx = true;
        [FormerlySerializedAs("m_EnabledMusic")]
        [SerializeField] protected bool m_enabledMusic = true;
        [FormerlySerializedAs("mSFXSources")]
        [SerializeField] protected AudioSource[] m_sfxSources;
        [FormerlySerializedAs("mSFXSourceUnlimited")]
        [SerializeField] public AudioSource m_sfxSourceUnlimited;
        [FormerlySerializedAs("mMusicSource")]
        [SerializeField] public AudioSource m_musicSource;
        [FormerlySerializedAs("m_MasterVolume")]
        [SerializeField, Range(0, 1f)] protected float m_masterVolume = 1f;
        [FormerlySerializedAs("m_SFXVolume")]
        [SerializeField, Range(0, 1f)] protected float m_sfxVolume = 1f;
        [FormerlySerializedAs("m_MusicVolume")]
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

        private void Awake()
        {
            m_musicSource.volume = m_masterVolume * m_musicVolume;
            m_sfxSourceUnlimited.volume = m_masterVolume * m_sfxVolume;
            foreach (var sound in m_sfxSources)
                sound.volume = m_masterVolume * m_sfxVolume;
        }
        
#region Common

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

        public void CreateAudioSources()
        {
            var sfxSources = new List<AudioSource>();
            var audioSources = gameObject.FindComponentsInChildren<AudioSource>();
            for (int i = 0; i < audioSources.Count; i++)
            {
                if (i == 0)
                {
                    m_musicSource = audioSources[i];
                    m_musicSource.name = "Music";
                }
                else
                {
                    sfxSources.Add(audioSources[i]);
                    audioSources[i].name = "SFX_" + i;
                }
            }
            if (sfxSources.Count < 5)
                for (int i = sfxSources.Count; i <= 15; i++)
                {
                    var obj = new GameObject("SFX_" + i);
                    obj.transform.SetParent(transform);
                    sfxSources.Add(obj.AddComponent<AudioSource>());
                }
            m_sfxSources = sfxSources.ToArray();
        }

        public AudioSource CreateMoreSFXSource()
        {
            var obj = new GameObject("SFX_" + m_sfxSources.Length);
            obj.transform.SetParent(transform);
            var newAudioSource = obj.AddComponent<AudioSource>();
            m_sfxSources.Add(newAudioSource, out m_sfxSources);
            return newAudioSource;
        }

        private IEnumerator IELerp(float pTime, Action<float> pOnUpdate, Action pOnFinished)
        {
            float time = 0;
            while (true)
            {
                time += Time.deltaTime;
                if (pTime > time)
                    break;
                pOnUpdate.Raise(time / pTime);
                yield return null;
            }
            pOnFinished.Raise();
        }
        
#endregion

#region Musics

        public void EnableMusic(bool pValue)
        {
            m_enabledMusic = pValue;
            m_musicSource.mute = !pValue;
        }

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

        public void StopMusic(float pFadeDuration = 0, Action pOnComplete = null)
        {
            SetMusicVolume(0, pFadeDuration, () =>
            {
                m_musicSource.Stop();
                pOnComplete?.Invoke();
            });
        }

        public void PlayMusic(float pFadeDuration = 0, float pVolume = 1f)
        {
            if (!m_musicSource.isPlaying)
                m_musicSource.Play();
            SetMusicVolume(pVolume, pFadeDuration);
        }

        public void PlayMusic(AudioClip pClip, bool pLoop, float pFadeDuration = 0, float pVolume = 1f)
        {
            if (pClip == null)
                return;

            bool play = !(m_musicSource.clip == pClip && m_musicSource.isPlaying);

            m_musicSource.clip = pClip;
            m_musicSource.loop = pLoop;
            if (!m_enabledMusic) return;

            if (play)
                m_musicSource.Play();
            SetMusicVolume(pVolume, pFadeDuration);
        }

        public void PlayMusics(AudioClip[] pClips, float pFadeDuration = 0, float pVolume = 1f)
        {
            if (m_playMusicsCoroutine != null)
                StopCoroutine(m_playMusicsCoroutine);
            m_playMusicsCoroutine = StartCoroutine(IEPlayMusics(pClips, pFadeDuration, pVolume));
        }

        private IEnumerator IEPlayMusics(AudioClip[] pClips, float pFadeDuration = 0, float pVolume = 1f)
        {
            if (pClips == null)
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

        public AudioSource PlaySFX(AudioClip pClip, int limitNumber, bool pLoop, float pPitchRandomMultiplier = 1)
        {
            if (pClip == null)
                return null;
            var source = GetSFXSouce(pClip, limitNumber, pLoop);
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

        public bool IsPlayingMusic() => m_musicSource.isPlaying;

        public void PlayMusic(string pFileName, bool pLoop = false, float pFadeDuration = 0)
        {
            var clip = audioCollection.GetMusicClip(pFileName);
            PlayMusic(clip, pLoop, pFadeDuration);
        }

        public void PlayMusicById(int pId, bool pLoop = false, float pFadeDuration = 0, float pVolume = 1f)
        {
            var clip = audioCollection.GetMusicClip(pId);
            PlayMusic(clip, pLoop, pFadeDuration, pVolume);
        }

        public void PlayMusicByIds(int[] pIds, float pFadeDuration = 0, float pVolume = 1f)
        {
            var clips = new AudioClip[pIds.Length];
            for (int i = 0; i < pIds.Length; i++)
                clips[i] = audioCollection.GetMusicClip(pIds[i]);
            PlayMusics(clips, pFadeDuration, pVolume);
        }

#endregion

#region SFX

        public void EnableSFX(bool pValue)
        {
            m_enabledSfx = pValue;
            foreach (var s in m_sfxSources)
                s.mute = !pValue;
            m_sfxSourceUnlimited.mute = !pValue;
        }

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

        public void StopSFX(AudioClip pClip)
        {
            if (pClip == null)
                return;

            for (int i = 0; i < m_sfxSources.Length; i++)
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

        public void StopSFXs()
        {
            for (int i = 0; i < m_sfxSources.Length; i++)
            {
                m_sfxSources[i].Stop();
                m_sfxSources[i].clip = null;
            }

            m_sfxSourceUnlimited.Stop();
            m_sfxSourceUnlimited.clip = null;
        }

        private AudioSource GetSFXSouce(AudioClip pClip, int pLimitNumber, bool pLoop)
        {
            try
            {
                if (pLimitNumber > 0 || pLoop)
                {
                    if (!pLoop)
                    {
                        int countSameClips = 0;
                        for (int i = m_sfxSources.Length - 1; i >= 0; i--)
                        {
                            if (m_sfxSources[i].isPlaying && m_sfxSources[i].clip != null && m_sfxSources[i].clip.GetInstanceID() == pClip.GetInstanceID())
                                countSameClips++;
                            else if (!m_sfxSources[i].isPlaying)
                                m_sfxSources[i].clip = null;
                        }
                        if (countSameClips < pLimitNumber)
                        {
                            for (int i = m_sfxSources.Length - 1; i >= 0; i--)
                                if (m_sfxSources[i].clip == null)
                                    return m_sfxSources[i];

                            return CreateMoreSFXSource();
                        }
                    }
                    else
                    {
                        for (int i = m_sfxSources.Length - 1; i >= 0; i--)
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

        public AudioSource PlaySFX(string pFileName, int limitNumber, bool pLoop = false, float pPitchRandomMultiplier = 1)
        {
            if (!m_enabledSfx) return null;
            var clip = audioCollection.GetSFXClip(pFileName);
            return PlaySFX(clip, limitNumber, pLoop, pPitchRandomMultiplier);
        }

        public AudioSource PlaySFX(int pIndex, int limitNumber, bool pLoop = false, float pPitchRandomMultiplier = 1)
        {
            if (!m_enabledSfx) return null;
            var clip = audioCollection.GetSFXClip(pIndex);
            return PlaySFX(clip, limitNumber, pLoop, pPitchRandomMultiplier);
        }

        public void StopSFX(int pIndex)
        {
            var clip = audioCollection.GetSFXClip(pIndex);
            StopSFX(clip);
        }

#endregion


#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            m_sfxSources ??= Array.Empty<AudioSource>();
            var audioSources = gameObject.FindComponentsInChildren<AudioSource>();
            for (int i = m_sfxSources.Length - 1; i >= 0; i--)
            {
                audioSources[i].playOnAwake = false;

                if (audioSources.Contains(m_sfxSources[i]))
                    audioSources.Remove(m_sfxSources[i]);
            }
            if (m_musicSource == null && audioSources.Count > 0)
            {
                m_musicSource = audioSources[0];
                audioSources.RemoveAt(0);
                m_musicSource.name = "Music";
            }
            else if (m_musicSource == null)
            {
                var obj = new GameObject("Music");
                obj.AddComponent<AudioSource>();
                obj.transform.SetParent(transform);
                m_musicSource = obj.GetComponent<AudioSource>();
            }
            if (m_sfxSourceUnlimited == null && audioSources.Count > 0)
            {
                m_sfxSourceUnlimited = audioSources[0];
                audioSources.RemoveAt(0);
                m_sfxSourceUnlimited.name = "SFXUnlimited";
            }
            else if (m_sfxSourceUnlimited == null || m_sfxSourceUnlimited == m_musicSource)
            {
                var obj = new GameObject("SFXUnlimited");
                obj.AddComponent<AudioSource>();
                obj.transform.SetParent(transform);
                m_sfxSourceUnlimited = obj.GetComponent<AudioSource>();
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