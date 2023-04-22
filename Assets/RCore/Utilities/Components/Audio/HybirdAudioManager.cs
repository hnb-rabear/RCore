//#define USE_DOTWEEN

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if USE_DOTWEEN
using DG.Tweening;
#endif

namespace RCore.Components
{
    /// <summary>
    /// Hybird audio collection
    /// Require Pre-setup in Json Data (See SoundsCollection for more infomation)
    /// It is suitable for game with many sounds and managed by Excel
    /// </summary>
    public class HybirdAudioManager : BaseAudioManager
    {
        #region Members

        private static HybirdAudioManager mInstance;
        public static HybirdAudioManager Instance => mInstance;

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
            int outIndex = -1;
            var clip = HybirdAudioCollection.Instance.GetClip(pFileName, false, out outIndex);
            if (clip != null)
                PlaySFX(clip.clip, clip.limitNumber, pLoop, pPitchRandomMultiplier);
        }

        public void PlaySFXById(int pId, bool pLoop = false, float pPitchRandomMultiplier = 1)
        {
            if (!m_EnabledSFX) return;
            int outIndex = -1;
            var clip = HybirdAudioCollection.Instance.GetClipById(pId, false, out outIndex);
            if (clip != null)
                PlaySFX(clip.clip, clip.limitNumber, pLoop, pPitchRandomMultiplier);
        }

        public void PlaySFXByIndex(int pIndex, bool pLoop = false, float pPitchRandomMultiplier = 1)
        {
            if (!m_EnabledSFX) return;
            var clip = HybirdAudioCollection.Instance.GetClipByIndex(pIndex, false);
            if (clip != null)
                PlaySFX(clip.clip, clip.limitNumber, pLoop, pPitchRandomMultiplier);
        }

        public void StopSFXById(int pId)
        {
            int outIndex = -1;
            var clip = HybirdAudioCollection.Instance.GetClipById(pId, false, out outIndex);
            if (clip != null)
                StopSFX(clip.clip);
        }

        public void StopSFXByIndex(int pIndex)
        {
            var clip = HybirdAudioCollection.Instance.GetClipByIndex(pIndex, false);
            if (clip != null)
                StopSFX(clip.clip);
        }

        public void PlayMusic(string pFileName, bool pLoop = false, float pFadeDuration = 0)
        {
            int outIndex = -1;
            var clip = HybirdAudioCollection.Instance.GetClip(pFileName, true, out outIndex);
            if (clip != null)
                PlayMusic(clip.clip, pLoop, pFadeDuration);
        }

        public void PlayMusicById(int pId, bool pLoop = false, float pFadeDuration = 0, float pVolume = 1f)
        {
            int outIndex = -1;
            var clip = HybirdAudioCollection.Instance.GetClipById(pId, true, out outIndex);
            if (clip != null)
                PlayMusic(clip.clip, pLoop, pFadeDuration, pVolume);
        }

        public void PlayMusicByIndex(int pIndex, bool pLoop = false, float pFadeDuration = 0, float pVolume = 1f)
        {
            var clip = HybirdAudioCollection.Instance.GetClipByIndex(pIndex, true);
            if (clip != null)
                PlayMusic(clip.clip, pLoop, pFadeDuration, pVolume);
        }

        #endregion

        //=====================================

        #region Private

#if UNITY_EDITOR

        [CustomEditor(typeof(HybirdAudioManager))]
        protected class HybirdAudioManagerEditor : BaseAudioManagerEditor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                if (HybirdAudioCollection.Instance == null)
                    EditorGUILayout.HelpBox("HybirdAudioManager require HybirdAudioCollection. " +
                        "To create HybirdAudioCollection,select Resources folder then from Create Menu " +
                        "select RUtilities/Create Hybird Audio Collection", MessageType.Warning);
                else if (GUILayout.Button("Open Audio Collection"))
                    Selection.activeObject = HybirdAudioCollection.Instance;
            }
        }

#endif

        #endregion
    }
}