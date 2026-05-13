using UnityEngine;

namespace RevCore.Foundation.Samples
{
    public class SampleConsumer : MonoBehaviour
    {
        private void Start()
        {
            if (Services.TryGet(out IAudioService audio))
            {
                audio.PlaySfx("click");
            }
            else
            {
                Debug.LogWarning("[Consumer] IAudioService not registered.");
            }
        }
    }
}
