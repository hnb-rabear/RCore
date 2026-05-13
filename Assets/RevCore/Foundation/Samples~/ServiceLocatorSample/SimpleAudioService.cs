using UnityEngine;

namespace RevCore.Foundation.Samples
{
    public sealed class SimpleAudioService : IAudioService
    {
        public void PlaySfx(string clipName)
        {
            Debug.Log($"[AudioService] Play: {clipName}");
        }

        public void StopAll()
        {
            Debug.Log("[AudioService] StopAll");
        }
    }
}
