using System;
using UnityEngine;
using RCore.Common;
using Random = UnityEngine.Random;
using UnityEngine.Events;
using RCore.Inspector;
using UnityEngine.Serialization;
using Debug = RCore.Common.Debug;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
#endif

namespace RCore.Components
{
	public class SfxSource : MonoBehaviour
	{
		[FormerlySerializedAs("m_Indexs")] [SerializeField, ReadOnly] private int[] m_Indexes;
		[SerializeField] private string[] mClips;
		[SerializeField] private bool mIsLoop;
		[SerializeField, Range(0.5f, 2f)] private float m_PitchRandomMultiplier = 1f;
		[SerializeField] private int m_Limit;
		[SerializeField] private AudioSource m_AudioSource;
		[SerializeField, Range(0, 1f)] private float m_Vol = 1f;

		private bool m_Initialized;

		private void Awake()
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
			if (m_Initialized)
				return;

			if (AudioManager.Instance == null)
			{
				Debug.LogError("Not found Audio Manager!");
				return;
			}

			m_Indexes = new int[mClips.Length];
			for (int i = 0; i < mClips.Length; i++)
			{
				var clips = AudioManager.Instance.audioCollection.sfxClips;
				for (int j = 0; j < clips.Length; j++)
				{
					if (clips[j] != null && clips[j].name.ToLower() == mClips[i].ToLower())
					{
						m_Indexes[i] = j;
						break;
					}
				}
			}

			m_Initialized = true;
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
			if (!AudioManager.Instance.EnabledSFX)
				return;

			if (m_Indexes.Length > 0)
			{
				int index = m_Indexes[Random.Range(0, mClips.Length)];
				if (m_AudioSource == null)
				{
					if (AudioManager.Instance != null)
						AudioManager.Instance.PlaySFX(index, m_Limit, mIsLoop, m_PitchRandomMultiplier);
				}
				else
				{
					if (!AudioManager.Instance.EnabledSFX)
						return;
					var clip = AudioManager.Instance.audioCollection.GetSFXClip(index);
					m_AudioSource.volume = AudioManager.Instance.SFXVolume * AudioManager.Instance.MasterVolume * m_Vol;
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

#if UNITY_EDITOR
		[CanEditMultipleObjects]
		[CustomEditor(typeof(SfxSource))]
		private class AudioSFXEditor : UnityEditor.Editor
		{
			private AudioCollection m_TempAudioCollection;
			private EditorPrefsString m_AudioCollectionPath;
			private SfxSource m_Script;
			private string m_Search = "";
			private UnityEngine.UI.Button m_Button;

			private void OnEnable()
			{
				m_Script = target as SfxSource;

				m_Script.mClips ??= Array.Empty<string>();

				m_Button = m_Script.GetComponent<UnityEngine.UI.Button>();
				m_AudioCollectionPath = new EditorPrefsString($"{typeof(AudioCollection).FullName}");

				if (m_TempAudioCollection == null)
				{
					if (!string.IsNullOrEmpty(m_AudioCollectionPath.Value))
						m_TempAudioCollection = (AudioCollection)AssetDatabase.LoadAssetAtPath(m_AudioCollectionPath.Value, typeof(AudioCollection));
				}
				if (m_TempAudioCollection == null)
				{
					var audioManager = FindObjectOfType<BaseAudioManager>();
					if (audioManager != null)
					{
						m_TempAudioCollection = audioManager.audioCollection;
						m_AudioCollectionPath.Value = AssetDatabase.GetAssetPath(m_TempAudioCollection);
					}
				}
			}

			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				if (m_TempAudioCollection == null)
				{
					if (m_TempAudioCollection == null)
						EditorGUILayout.HelpBox("AudioSFX require AudioCollection. " +
							"To create AudioCollection, select Project windows RUtilities/Create Audio Collection", MessageType.Error);

					var asset = (AudioCollection)EditorHelper.ObjectField<AudioCollection>(m_TempAudioCollection, "Audio Collection", 120);
					if (asset != m_TempAudioCollection)
					{
						m_TempAudioCollection = asset;
						if (m_TempAudioCollection != null)
							m_AudioCollectionPath.Value = AssetDatabase.GetAssetPath(m_TempAudioCollection);
					}
					return;
				}

				if (m_Script.mClips.Length > 0)
					EditorHelper.BoxVertical(() =>
					{
						for (int i = 0; i < m_Script.mClips.Length; i++)
						{
							int i1 = i;
							EditorHelper.BoxHorizontal(() =>
							{
								EditorHelper.TextField(m_Script.mClips[i1], "");
								if (EditorHelper.ButtonColor("x", Color.red, 24))
								{
									var list = m_Script.mClips.ToList();
									list.Remove(m_Script.mClips[i1]);
									m_Script.mClips = list.ToArray();
								}
							});
						}
					}, Color.yellow, true);

				EditorHelper.BoxVertical(() =>
				{
					m_Search = EditorHelper.TextField(m_Search, "Search");
					if (!string.IsNullOrEmpty(m_Search))
					{
						var clips = m_TempAudioCollection.sfxClips;
						if (clips != null && clips.Length > 0)
						{
							for (int i = 0; i < clips.Length; i++)
							{
								if (clips[i].name.ToLower().Contains(m_Search.ToLower()))
								{
									if (GUILayout.Button(clips[i].name))
									{
										var list = m_Script.mClips.ToList();
										if (!list.Contains(clips[i].name))
										{
											list.Add(clips[i].name);
											m_Script.mClips = list.ToArray();
											m_Search = "";
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

				if (EditorHelper.ButtonColor("Open Sounds Collection"))
					Selection.activeObject = m_TempAudioCollection;

				if (m_Button != null)
				{
					if (EditorHelper.ButtonColor("Add to OnClick event"))
					{
						UnityAction action = m_Script.PlaySFX;
						UnityEventTools.AddVoidPersistentListener(m_Button.onClick, action);
					}
				}

				if (GUI.changed)
					EditorUtility.SetDirty(m_Script);
			}
		}
#endif
	}
}