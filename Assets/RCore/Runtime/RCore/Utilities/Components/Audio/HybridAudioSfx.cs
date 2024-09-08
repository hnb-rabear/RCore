using UnityEngine;
using RCore.Common;
using Random = UnityEngine.Random;
using UnityEngine.Events;
using RCore.Inspector;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
#endif

namespace RCore.Components
{
	public class HybridAudioSfx : MonoBehaviour
	{
		[SerializeField] private string[] mIdStrings;
		[SerializeField] private bool mIsLoop;
		[SerializeField, ReadOnly] private int[] mIndexs;
		[SerializeField, Range(0.5f, 2f)] private float mPitchRandomMultiplier = 1f;
		[SerializeField] private AudioSource m_AudioSource;

		private bool mInitialized;

		private void Awake()
		{
			Init();
		}

		private void OnValidate()
		{
			if (Application.isPlaying)
				return;

			if (m_AudioSource != null)
			{
				m_AudioSource.loop = false;
				m_AudioSource.playOnAwake = false;
			}
		}


		private void Init()
		{
			if (mInitialized) return;
			mIndexs = new int[mIdStrings.Length];
			for (int i = 0; i < mIdStrings.Length; i++)
			{
				var sound = HybridAudioCollection.Instance.GetClip(mIdStrings[i], false, out mIndexs[i]);
				if (sound == null)
					UnityEngine.Debug.LogError("Not found SFX id " + mIdStrings[i]);
			}
			mInitialized = true;
		}

		public void PlaySFX()
		{
			if (!HybridAudioManager.Instance.EnabledSFX)
				return;

			if (mIndexs.Length > 0)
			{
				var index = mIndexs[Random.Range(0, mIndexs.Length)];
				if (m_AudioSource == null)
				{
					if (HybridAudioManager.Instance != null)
						HybridAudioManager.Instance.PlaySFXByIndex(index, mIsLoop, mPitchRandomMultiplier);
				}
				else
				{
					if (!HybridAudioManager.Instance.EnabledSFX)
						return;
					var clip = HybridAudioManager.Instance.audioCollection.GetSFXClip(index);
					m_AudioSource.volume = HybridAudioManager.Instance.SFXVolume * HybridAudioManager.Instance.MasterVolume;
					m_AudioSource.loop = mIsLoop;
					m_AudioSource.clip = clip;
					m_AudioSource.pitch = 1;
					if (mPitchRandomMultiplier != 1)
					{
						if (Random.value < .5)
							m_AudioSource.pitch *= Random.Range(1 / mPitchRandomMultiplier, 1);
						else
							m_AudioSource.pitch *= Random.Range(1, mPitchRandomMultiplier);
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
		[CustomEditor(typeof(HybridAudioSfx))]
		private class HybirdAudioSFXEditor : UnityEditor.Editor
		{
			private HybridAudioSfx mScript;
			private string mSearch = "";
			private UnityEngine.UI.Button mButton;

			private void OnEnable()
			{
				mScript = target as HybridAudioSfx;

				mScript.mIdStrings ??= new string[] { };

				mButton = mScript.GetComponent<UnityEngine.UI.Button>();
			}

			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				if (HybridAudioCollection.Instance == null)
				{
					if (HybridAudioCollection.Instance == null)
						EditorGUILayout.HelpBox("HybridAudioSFX require HybridAudioCollection. " +
							"To create HybridAudioCollection,select Resources folder then from Create Menu " +
							"select RUtilities/Create Hybrid Audio Collection", MessageType.Error);
					return;
				}

				if (mScript.mIdStrings.Length > 0)
					EditorHelper.BoxVertical(() =>
					{
						for (int i = 0; i < mScript.mIdStrings.Length; i++)
						{
							int i1 = i;
							EditorHelper.BoxHorizontal(() =>
							{
								EditorHelper.TextField(mScript.mIdStrings[i1], "SoundId");
								if (EditorHelper.ButtonColor("x", Color.red, 24))
								{
									var list = mScript.mIdStrings.ToList();
									list.Remove(mScript.mIdStrings[i1]);
									mScript.mIdStrings = list.ToArray();
								}
							});
						}
					}, Color.yellow, true);

				EditorHelper.BoxVertical(() =>
				{
					mSearch = EditorHelper.TextField(mSearch, "Search");
					if (!string.IsNullOrEmpty(mSearch))
					{
						var clips = HybridAudioCollection.Instance.SFXClips;
						if (clips != null && clips.Count > 0)
						{
							for (int i = 0; i < clips.Count; i++)
							{
								if (clips[i].fileName.ToLower().Contains(mSearch.ToLower()))
								{
									if (GUILayout.Button(clips[i].fileName))
									{
										var list = mScript.mIdStrings.ToList();
										if (!list.Contains(clips[i].fileName))
										{
											list.Add(clips[i].fileName);
											mScript.mIdStrings = list.ToArray();
											mSearch = "";
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

				if (mButton != null)
				{
					if (EditorHelper.ButtonColor("Add to OnClick event"))
					{
						UnityAction action = new UnityAction(mScript.PlaySFX);
						UnityEventTools.AddVoidPersistentListener(mButton.onClick, action);
					}
				}

				if (GUI.changed)
					EditorUtility.SetDirty(mScript);
			}
		}
#endif
	}
}