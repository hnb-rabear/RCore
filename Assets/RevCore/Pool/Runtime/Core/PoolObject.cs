using UnityEngine;

namespace RevCore
{
    /// <summary>
    /// Base component that any pooled prefab must carry. <see cref="RevPool{T}"/> constrains to
    /// <see cref="Component"/>-derived types, but the timer-based <c>Release(delaySeconds)</c>
    /// overload requires this concrete component on the spawned object so it can stash the
    /// scheduled release handle.
    /// </summary>
    public class PoolObject : MonoBehaviour
    {
        /// <summary>Pending release timer handle, or <c>null</c>. Set internally by the pool when
        /// the timed-release overload is used; readers should not assume thread-safety.</summary>
        public ITimerHandle ReleaseHandle { get; internal set; }
    }
}
