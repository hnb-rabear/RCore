using UnityEngine;

namespace RevCore
{
    /// <summary>
    /// Manages multiple <see cref="IPool{T}"/> instances keyed by prefab. Lets callers spawn off
    /// any prefab without explicitly creating a pool first. The default implementation is
    /// <see cref="PoolsContainer{T}"/>.
    /// </summary>
    /// <typeparam name="T">Component type shared across the contained pools.</typeparam>
    public interface IPoolContainer<T> where T : Component
    {
        /// <summary>Number of distinct prefab pools currently held.</summary>
        int PoolCount { get; }

        /// <summary>Returns the pool for <paramref name="prefab"/>, creating it on first request.</summary>
        RevPool<T> Get(T prefab);

        /// <summary>Spawns from <paramref name="prefab"/>'s pool, creating the pool on first call.</summary>
        T Spawn(T prefab);

        /// <summary>Spawns at <paramref name="position"/> from <paramref name="prefab"/>'s pool. <paramref name="worldPosition"/> selects world vs local space.</summary>
        T Spawn(T prefab, Vector3 position, bool worldPosition = true);

        /// <summary>Routes <paramref name="item"/> back to whichever pool owns it. No-op if not owned by this container.</summary>
        void Release(T item);

        /// <summary>Releases every active item across every contained pool.</summary>
        void ReleaseAll();
    }
}
