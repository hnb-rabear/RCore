//#define USE_DOTWEEN

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RCore.Common;
using System;
#if USE_DOTWEEN
using DG.Tweening;
#endif
using Random = UnityEngine.Random;
using Debug = RCore.Common.Debug;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Components
{
	public class BaseAudioManager : MonoBehaviour
	{
		public AudioCollection audioCollection;
		[SerializeField] protected bool m_EnabledSFX = true;
		[SerializeField] protected bool m_EnabledMusic = true;
		[SerializeField] protected AudioSource[] mSFXSources;
		[SerializeField] protected AudioSource mSFXSourceUnlimited;
		[SerializeField] protected AudioSource mMusicSource;
		[SerializeField, Range(0, 1f)] protected float m_MasterVolume = 1f;
		[SerializeField, Range(0, 1f)] protected float m_SFXVolume = 1f;
		[SerializeField, Range(0, 1f)] protected float m_MusicVolume = 1f;

		public bool EnabledSFX => m_EnabledSFX;
		public bool EnabledMusic => m_EnabledMusic;
		public float MasterVolume => m_MasterVolume;
		public float SFXVolume => m_SFXVolume;
		public float MusicVolume => m_MusicVolume;

#if USE_DOTWEEN
		private Tweener m_MasterTweener;
		private Tweener m_MusicTweener;
		private Tweener m_SFXTweener;
#endif
		private Coroutine m_IEPlayMusics;

		private void Awake()
		{
			mMusicSource.volume = m_MasterVolume * m_MusicVolume;
			mSFXSourceUnlimited.volume = m_MasterVolume * m_SFXVolume;
			foreach (var sound in mSFXSources)
				sound.volume = m_MasterVolume * m_SFXVolume;
		}

		public void EnableMusic(bool pValue)
		{
			m_EnabledMusic = pValue;
			mMusicSource.mute = !pValue;
		}

		public void EnableSFX(bool pValue)
		{
			m_EnabledSFX = pValue;
			foreach (var s in mSFXSources)
				s.mute = !pValue;
			mSFXSourceUnlimited.mute = !pValue;
		}

		public void SetMasterVolume(float pValue, float pFadeDuration = 0, Action pOnComplete = null)
		{
#if USE_DOTWEEN
			m_MasterTweener.Kill();
#endif
			if (m_MasterVolume == pValue)
			{
				pOnComplete?.Invoke();
				return;
			}

			if (pFadeDuration <= 0)
			{
				m_MasterVolume = pValue;
				mMusicSource.volume = m_MasterVolume * m_MusicVolume;
				mSFXSourceUnlimited.volume = m_MasterVolume * m_SFXVolume;
				foreach (var source in mSFXSources)
					source.volume = m_MasterVolume * m_SFXVolume;
				pOnComplete?.Invoke();
			}
			else
			{
				float fromVal = m_MasterVolume;
#if USE_DOTWEEN
				float lerp = 0;
				m_MasterTweener = DOTween.To(() => lerp, x => lerp = x, 1f, pFadeDuration)
					.SetUpdate(true)
					.OnUpdate(() =>
					{
						m_MasterVolume = Mathf.Lerp(fromVal, pValue, lerp);
						mMusicSource.volume = m_MasterVolume * m_MusicVolume;
						mSFXSourceUnlimited.volume = m_MasterVolume * m_SFXVolume;
						foreach (var source in mSFXSources)
							source.volume = m_MasterVolume * m_SFXVolume;
					})
					.OnComplete(() =>
					{
						pOnComplete?.Invoke();
					});
#else
				StartCoroutine(IELerp(pFadeDuration,
					(lerp) =>
					{
						m_MasterVolume = Mathf.Lerp(fromVal, pValue, lerp);
						mMusicSource.volume = m_MasterVolume * m_MusicVolume;
						mSFXSourceUnlimited.volume = m_MasterVolume * m_SFXVolume;
						foreach (var source in mSFXSources)
							source.volume = m_MasterVolume * m_SFXVolume;
					}, () =>
					{
						pOnComplete?.Invoke();
					}));

#endif
			}
		}

		public void SetMusicVolume(float pValue, float pFadeDuration = 0, Action pOnComplete = null)
		{
#if USE_DOTWEEN
			m_MusicTweener.Kill();
#endif
			if (!m_EnabledMusic || pValue == m_MusicVolume)
			{
				m_MusicVolume = pValue;
				mMusicSource.volume = m_MasterVolume * m_MusicVolume;
				pOnComplete?.Invoke();
				return;
			}

			if (pFadeDuration <= 0)
			{
				m_MusicVolume = pValue;
				mMusicSource.volume = m_MasterVolume * m_MusicVolume;
				pOnComplete?.Invoke();
			}
			else
			{
				float fromVal = m_MusicVolume;
#if USE_DOTWEEN
				float lerp = 0;
				m_MusicTweener = DOTween.To(() => lerp, x => lerp = x, 1f, pFadeDuration)
					.SetUpdate(true)
					.OnUpdate(() =>
					{
						m_MusicVolume = Mathf.Lerp(fromVal, pValue, lerp);
						mMusicSource.volume = m_MasterVolume * m_MusicVolume;
					})
					.OnComplete(() =>
					{
						pOnComplete?.Invoke();
					});
#else
				StartCoroutine(IELerp(pFadeDuration, (lerp) =>
				{
					m_MusicVolume = Mathf.Lerp(fromVal, pValue, lerp);
					mMusicSource.volume = m_MasterVolume * m_MusicVolume;
				}, () =>
				{
					pOnComplete?.Invoke();
				}));
#endif
			}
		}

		public void SetSFXVolume(float pValue, float pFadeDuration = 0, Action pOnComplete = null)
		{
#if USE_DOTWEEN
			m_SFXTweener.Kill();
#endif
			if (pValue == m_SFXVolume)
			{
				pOnComplete?.Invoke();
				return;
			}

			if (pFadeDuration <= 0)
			{
				m_SFXVolume = pValue;
				mSFXSourceUnlimited.volume = m_MasterVolume * pValue;
				foreach (var sound in mSFXSources)
					sound.volume = m_MasterVolume * pValue;
				pOnComplete?.Invoke();
			}
			else
			{
				float fromVal = m_SFXVolume;
#if USE_DOTWEEN
				float lerp = 0;
				m_SFXTweener = DOTween.To(() => lerp, x => lerp = x, 1f, pFadeDuration)
					.SetUpdate(true)
					.OnUpdate(() =>
					{
						m_SFXVolume = Mathf.Lerp(fromVal, pValue, lerp);
						mSFXSourceUnlimited.volume = m_MasterVolume * m_SFXVolume;
						foreach (var sound in mSFXSources)
							sound.volume = m_MasterVolume * m_SFXVolume;
					})
					.OnComplete(() =>
					{
						pOnComplete?.Invoke();
					});
#else
				StartCoroutine(IELerp(pFadeDuration, (lerp) =>
				{
					m_SFXVolume = Mathf.Lerp(fromVal, pValue, lerp);
					mSFXSourceUnlimited.volume = m_MasterVolume * m_SFXVolume;
					foreach (var sound in mSFXSources)
						sound.volume = m_MasterVolume * m_SFXVolume;
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
				mMusicSource.Stop();
				pOnComplete?.Invoke();
			});
		}

		public void StopSFX(AudioClip pClip)
		{
			if (pClip == null)
				return;

			for (int i = 0; i < mSFXSources.Length; i++)
			{
				if (mSFXSources[i].clip != null && mSFXSources[i].clip.GetInstanceID() == pClip.GetInstanceID())
				{
					mSFXSources[i].Stop();
					mSFXSources[i].clip = null;
				}
			}

			if (mSFXSourceUnlimited.clip == pClip)
			{
				mSFXSourceUnlimited.Stop();
				mSFXSourceUnlimited.clip = null;
			}
		}

		public void StopSFXs()
		{
			for (int i = 0; i < mSFXSources.Length; i++)
			{
				mSFXSources[i].Stop();
				mSFXSources[i].clip = null;
			}

			mSFXSourceUnlimited.Stop();
			mSFXSourceUnlimited.clip = null;
		}

		protected void CreateAudioSources()
		{
			var sfxSources = new List<AudioSource>();
			var audioSources = gameObject.FindComponentsInChildren<AudioSource>();
			for (int i = 0; i < audioSources.Count; i++)
			{
				if (i == 0)
				{
					mMusicSource = audioSources[i];
					mMusicSource.name = "Music";
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
			mSFXSources = sfxSources.ToArray();
		}

		protected AudioSource CreateMoreSFXSource()
		{
			var obj = new GameObject("SFX_" + mSFXSources.Length);
			obj.transform.SetParent(transform);
			var newAudioSource = obj.AddComponent<AudioSource>();
			mSFXSources.Add(newAudioSource, out mSFXSources);
			return newAudioSource;
		}

		protected IEnumerator IELerp(float pTime, Action<float> pOnUpdate, Action pOnFinished)
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

		protected AudioSource GetSFXSouce(AudioClip pClip, int pLimitNumber, bool pLoop)
		{
			try
			{
				if (pLimitNumber > 0 || pLoop)
				{
					if (!pLoop)
					{
						int countSameClips = 0;
						for (int i = mSFXSources.Length - 1; i >= 0; i--)
						{
							if (mSFXSources[i].isPlaying && mSFXSources[i].clip != null && mSFXSources[i].clip.GetInstanceID() == pClip.GetInstanceID())
								countSameClips++;
							else if (!mSFXSources[i].isPlaying)
								mSFXSources[i].clip = null;
						}
						if (countSameClips < pLimitNumber)
						{
							for (int i = mSFXSources.Length - 1; i >= 0; i--)
								if (mSFXSources[i].clip == null)
									return mSFXSources[i];

							return CreateMoreSFXSource();
						}
					}
					else
					{
						for (int i = mSFXSources.Length - 1; i >= 0; i--)
							if (mSFXSources[i].clip == null || !mSFXSources[i].isPlaying
								|| mSFXSources[i].clip.GetInstanceID() == pClip.GetInstanceID())
								return mSFXSources[i];
					}
				}
				else
				{
					return mSFXSourceUnlimited;
				}
				return null;
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.ToString());
				return null;
			}
		}

		public void PlayMusic(float pFadeDuration = 0, float pVolume = 1f)
		{
			if (!mMusicSource.isPlaying)
				mMusicSource.Play();
			SetMusicVolume(pVolume, pFadeDuration);
		}

		public void PlayMusic(AudioClip pClip, bool pLoop, float pFadeDuration = 0, float pVolume = 1f)
		{
			if (pClip == null)
				return;

			bool play = !(mMusicSource.clip == pClip && mMusicSource.isPlaying);

			mMusicSource.clip = pClip;
			mMusicSource.loop = pLoop;
			if (!m_EnabledMusic) return;

			if (play)
				mMusicSource.Play();
			SetMusicVolume(pVolume, pFadeDuration);
		}

		public void PlayMusics(AudioClip[] pClips, float pFadeDuration = 0, float pVolume = 1f)
		{
			if (m_IEPlayMusics != null)
				StopCoroutine(m_IEPlayMusics);
			m_IEPlayMusics = StartCoroutine(IEPlayMusics(pClips, pFadeDuration, pVolume));
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
				if (pClips[i] == mMusicSource.clip)
				{
					index = i;
					break;
				}
			while (true)
			{
				bool play = true;
				var clip = pClips[index];

				if (mMusicSource.clip == clip && mMusicSource.isPlaying)
					play = false;

				if (play)
				{
					mMusicSource.clip = clip;
					mMusicSource.loop = false;
					mMusicSource.Play();
				}
				if (m_EnabledMusic)
					SetMusicVolume(pVolume, pFadeDuration);
				else
					SetMusicVolume(0);
				yield return new WaitUntil(() => !mMusicSource.isPlaying || mMusicSource.clip == null);
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
			source.volume = m_MasterVolume * m_SFXVolume;
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

		public bool IsPlayingMusic() => mMusicSource.isPlaying;

#if UNITY_EDITOR
		protected virtual void OnValidate()
		{
			mSFXSources ??= new AudioSource[0];
			var audioSources = gameObject.FindComponentsInChildren<AudioSource>();
			for (int i = mSFXSources.Length - 1; i >= 0; i--)
			{
				audioSources[i].playOnAwake = false;

				if (audioSources.Contains(mSFXSources[i]))
					audioSources.Remove(mSFXSources[i]);
			}
			if (mMusicSource == null && audioSources.Count > 0)
			{
				mMusicSource = audioSources[0];
				audioSources.RemoveAt(0);
				mMusicSource.name = "Music";
			}
			else if (mMusicSource == null)
			{
				var obj = new GameObject("Music");
				obj.AddComponent<AudioSource>();
				obj.transform.SetParent(transform);
				mMusicSource = obj.GetComponent<AudioSource>();
			}
			if (mSFXSourceUnlimited == null && audioSources.Count > 0)
			{
				mSFXSourceUnlimited = audioSources[0];
				audioSources.RemoveAt(0);
				mSFXSourceUnlimited.name = "SFXUnlimited";
			}
			else if (mSFXSourceUnlimited == null || mSFXSourceUnlimited == mMusicSource)
			{
				var obj = new GameObject("SFXUnlimited");
				obj.AddComponent<AudioSource>();
				obj.transform.SetParent(transform);
				mSFXSourceUnlimited = obj.GetComponent<AudioSource>();
			}

#if USE_DOTWEEN
			if (m_MasterTweener == null || !m_MasterTweener.IsPlaying())
				SetMasterVolume(m_MasterVolume);
			if (m_MusicTweener == null || !m_MusicTweener.IsPlaying())
				SetMusicVolume(m_MusicVolume);
			if (m_SFXTweener == null || !m_SFXTweener.IsPlaying())
				SetSFXVolume(m_SFXVolume);
#endif
		}

		[CustomEditor(typeof(BaseAudioManager), true)]
		protected class BaseAudioManagerEditor : UnityEditor.Editor
		{
			private BaseAudioManager m_Script;
			private EditorPrefsString m_AudioCollectionPath;

			protected virtual void OnEnable()
			{
				m_Script = target as BaseAudioManager;
				m_AudioCollectionPath = new EditorPrefsString($"{typeof(AudioCollection).FullName}");

				if (m_Script.audioCollection != null)
					m_AudioCollectionPath.Value = AssetDatabase.GetAssetPath(m_Script.audioCollection);
			}

			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				if (EditorHelper.ButtonColor("Add Music Audio Source", m_Script.mMusicSource == null ? Color.green : Color.grey))
				{
					if (m_Script.mMusicSource == null)
					{
						var obj = new GameObject("Music");
						obj.transform.SetParent(m_Script.transform);
						obj.AddComponent<AudioSource>();
						m_Script.mMusicSource = obj.GetComponent<AudioSource>();
					}
					if (m_Script.mSFXSourceUnlimited == null)
					{
						var obj = new GameObject("SFX_Unlimited");
						obj.transform.SetParent(m_Script.transform);
						obj.AddComponent<AudioSource>();
						m_Script.mMusicSource = obj.GetComponent<AudioSource>();
					}
				}
				if (EditorHelper.ButtonColor("Add SFX Audio Source"))
					m_Script.CreateMoreSFXSource();
				if (EditorHelper.ButtonColor("Create Audio Sources", Color.green))
					m_Script.CreateAudioSources();
				if (EditorHelper.Button("Stop Music"))
					m_Script.StopMusic(1f);
				if (EditorHelper.Button("Play Music"))
					m_Script.PlayMusic();

				if (GUI.changed)
					EditorUtility.SetDirty(m_Script);
			}
		}
#endif
	}
}