using UnityEngine;

namespace RevCore
{
    public class PoolObject : MonoBehaviour
    {
        public ITimerHandle ReleaseHandle { get; internal set; }
    }
}
