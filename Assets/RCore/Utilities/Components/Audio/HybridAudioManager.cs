//#define USE_DOTWEEN

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RCore.Common;
using System;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if USE_DOTWEEN
using DG.Tweening;
#endif

namespace RCore.Components
{
	/// <summary>
	/// Hybrid audio collection
	/// Require Pre-setup in Json Data (See SoundsCollection for more information)
	/// It is suitable for game with many sounds and managed by Excel
	/// </summary>
	public class HybridAudioManager : BaseAudioManager
	{
#region Members

		private static HybridAudioManager mInstance;
		public static HybridAudioManager Instance => mInstance;

#endregion

		//=====================================

#region MonoBehaviour

		private void Awake()
		{
			if (mInstance == null)
				mInstance = this;
			else if (mInstance != this)
				Destroy(gameObject);
		}

#endregion

		//=====================================

#region Public

		public void PlaySFX(string pFileName, bool pLoop = false, float pPitchRandomMultiplier = 1)
		{
			if (!m_EnabledSFX) return;
			var clip = HybridAudioCollection.Instance.GetClip(pFileName, false, out int _);
			if (clip != null)
				PlaySFX(clip.clip, clip.limitNumber, pLoop, pPitchRandomMultiplier);
		}

		public void PlaySFXById(int pId, bool pLoop = false, float pPitchRandomMultiplier = 1)
		{
			if (!m_EnabledSFX) return;
			var clip = HybridAudioCollection.Instance.GetClipById(pId, false, out int _);
			if (clip != null)
				PlaySFX(clip.clip, clip.limitNumber, pLoop, pPitchRandomMultiplier);
		}

		public void PlaySFXByIndex(int pIndex, bool pLoop = false, float pPitchRandomMultiplier = 1)
		{
			if (!m_EnabledSFX) return;
			var clip = HybridAudioCollection.Instance.GetClipByIndex(pIndex, false);
			if (clip != null)
				PlaySFX(clip.clip, clip.limitNumber, pLoop, pPitchRandomMultiplier);
		}

		public void StopSFXById(int pId)
		{
			var clip = HybridAudioCollection.Instance.GetClipById(pId, false, out int _);
			if (clip != null)
				StopSFX(clip.clip);
		}

		public void StopSFXByIndex(int pIndex)
		{
			var clip = HybridAudioCollection.Instance.GetClipByIndex(pIndex, false);
			if (clip != null)
				StopSFX(clip.clip);
		}

		public void PlayMusic(string pFileName, bool pLoop = false, float pFadeDuration = 0)
		{
			var clip = HybridAudioCollection.Instance.GetClip(pFileName, true, out int _);
			if (clip != null)
				PlayMusic(clip.clip, pLoop, pFadeDuration);
		}

		public void PlayMusicById(int pId, bool pLoop = false, float pFadeDuration = 0, float pVolume = 1f)
		{
			var clip = HybridAudioCollection.Instance.GetClipById(pId, true, out int _);
			if (clip != null)
				PlayMusic(clip.clip, pLoop, pFadeDuration, pVolume);
		}

		public void PlayMusicByIndex(int pIndex, bool pLoop = false, float pFadeDuration = 0, float pVolume = 1f)
		{
			var clip = HybridAudioCollection.Instance.GetClipByIndex(pIndex, true);
			if (clip != null)
				PlayMusic(clip.clip, pLoop, pFadeDuration, pVolume);
		}

#endregion

		//=====================================

#region Private

#if UNITY_EDITOR

		[CustomEditor(typeof(HybridAudioManager))]
		protected class HybridAudioManagerEditor : BaseAudioManager.BaseAudioManagerEditor
		{
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				if (HybridAudioCollection.Instance == null)
					EditorGUILayout.HelpBox("HybridAudioManager require HybridAudioCollection. " +
						"To create HybridAudioCollection,select Resources folder then from Create Menu " +
						"select RUtilities/Create Hybrid Audio Collection", MessageType.Warning);
				else if (GUILayout.Button("Open Audio Collection"))
					Selection.activeObject = HybridAudioCollection.Instance;
			}
		}

#endif

#endregion
	}
}