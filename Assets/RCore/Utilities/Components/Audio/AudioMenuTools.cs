#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace RCore.Components
{
    public class AudioMenuTools : UnityEditor.Editor
    {
        [MenuItem("RCore/Audio/Add Audio Manager")]
        private static void AddAudioManager()
        {
            var manager = FindObjectOfType<AudioManager>();
			if (manager != null)
				return;

			var obj = new GameObject("AudioManager");
			obj.AddComponent<AudioManager>();
		}

        [MenuItem("RCore/Audio/Add Hybrid Audio Manager")]
        private static void AddHybridAudioManager()
        {
            var manager = FindObjectOfType<HybridAudioManager>();
			if (manager != null)
				return;

			var obj = new GameObject("HybridAudioManager");
			obj.AddComponent<HybridAudioManager>();
		}

        [MenuItem("RCore/Audio/Open Hybrid Audio Collection")]
        private static void OpenHybridAudioCollection()
        {
            Selection.activeObject = HybridAudioCollection.Instance;
        }
    }
}
#endif