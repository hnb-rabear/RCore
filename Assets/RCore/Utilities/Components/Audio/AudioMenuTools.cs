#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace RCore.Components
{
    public class AudioMenuTools : Editor
    {
        [MenuItem("RUtilities/Audio/Add Audio Manager")]
        private static void AddAudioManager()
        {
            var manager = FindObjectOfType<AudioManager>();
            if (manager == null)
            {
                var obj = new GameObject("AudioManager");
                obj.AddComponent<AudioManager>();
            }
        }

        [MenuItem("RUtilities/Audio/Add Hybird Audio Manager")]
        private static void AddHybirdAudioManager()
        {
            var manager = FindObjectOfType<HybirdAudioManager>();
            if (manager == null)
            {
                var obj = new GameObject("HybirdAudioManager");
                obj.AddComponent<HybirdAudioManager>();
            }
        }

        [MenuItem("RUtilities/Audio/Open Hybird Audio Collection")]
        private static void OpenHybirdAudioCollection()
        {
            Selection.activeObject = HybirdAudioCollection.Instance;
        }
    }
}
#endif