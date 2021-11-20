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
    public class AudioManager : BaseAudioManager
    {
        #region Members

        private static AudioManager mInstance;
        public static AudioManager Instance { get { return mInstance; } }

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

        private void OnValidate()
        {
            mSFXSourceUnlimited.loop = false;
            mSFXSourceUnlimited.playOnAwake = false;
            mMusicSource.loop = true;
            mMusicSource.playOnAwake = false;
        }

        #endregion

        //=====================================

        #region Public

        public AudioSource PlaySFX(string pFileName, int limitNumber, bool pLoop = false, float pPitchRandomMultiplier = 1)
        {
            if (!m_EnabledSFX) return null;
            var clip = AudioCollection.Instance.GetSFXClip(pFileName);
            return PlaySFX(clip, limitNumber, pLoop, pPitchRandomMultiplier);
        }

        public AudioSource PlaySFX(int pIndex, int limitNumber, bool pLoop = false, float pPitchRandomMultiplier = 1)
        {
            if (!m_EnabledSFX) return null;
            var clip = AudioCollection.Instance.GetSFXClip(pIndex);
            return PlaySFX(clip, limitNumber, pLoop, pPitchRandomMultiplier);
        }

        public void StopSFX(int pIndex)
        {
            var clip = AudioCollection.Instance.GetSFXClip(pIndex);
            StopSFX(clip);
        }

        public void PlayMusic(string pFileName, bool pLoop = false, float pFadeDuration = 0)
        {
            var clip = AudioCollection.Instance.GetMusicClip(pFileName);
            PlayMusic(clip, pLoop, pFadeDuration);
        }

        public void PlayMusicById(int pId, bool pLoop = false, float pFadeDuration = 0, float pVolume = 1f)
        {
            var clip = AudioCollection.Instance.GetMusicClip(pId);
            PlayMusic(clip, pLoop, pFadeDuration, pVolume);
        }

        #endregion

        //=====================================

        #region Private

#if UNITY_EDITOR

        [CustomEditor(typeof(AudioManager))]
        protected class AudioManagerEditor : BaseAudioManagerEditor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                if (AudioCollection.Instance == null)
                    EditorGUILayout.HelpBox("AudioManager require AudioCollection. " +
                        "To create AudioCollection, select Resources folder then from Create Menu " +
                        "select RUtilities/Create Audio Collection", MessageType.Error);
                else if (GUILayout.Button("Open Audio Collection"))
                    Selection.activeObject = AudioCollection.Instance;
            }
        }

#endif

        #endregion
    }
}