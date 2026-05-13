using UnityEngine;

namespace RevCore.Foundation.Samples
{
    public class SampleBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Services.Register<IAudioService>(new SimpleAudioService());
            Debug.Log("[Bootstrap] Services registered.");
        }

        private void OnDestroy()
        {
            Services.Clear();
            Debug.Log("[Bootstrap] Services cleared.");
        }
    }
}
