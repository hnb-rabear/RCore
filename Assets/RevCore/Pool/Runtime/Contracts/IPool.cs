using System.Collections.Generic;
using UnityEngine;

namespace RevCore
{
    /// <summary>
    /// A homogeneous pool of <typeparamref name="T"/> components. The default implementation is
    /// <see cref="RevPool{T}"/>. Pools are owned by an <see cref="IPoolContainer"/> when grouped
    /// across types in a scene.
    /// </summary>
    /// <typeparam name="T">A <see cref="Component"/>-derived type cloned from <see cref="Prefab"/>.</typeparam>
    public interface IPool<T> where T : Component
    {
        /// <summary>The source prefab that <see cref="Spawn()"/> clones from when inactive items are exhausted.</summary>
        T Prefab { get; }

        /// <summary>Transform under which spawned instances are parented when not explicitly reparented.</summary>
        Transform Parent { get; }

        /// <summary>Display name (typically derived from the prefab) for diagnostics.</summary>
        string Name { get; }

        /// <summary>Number of items currently active (spawned and not yet released).</summary>
        int ActiveCount { get; }

        /// <summary>Number of items waiting in the inactive bucket.</summary>
        int InactiveCount { get; }

        /// <summary>Spawns at the parent's local origin and returns the active instance.</summary>
        T Spawn();

        /// <summary>Spawns at <paramref name="position"/>. When <paramref name="worldPosition"/> is <c>true</c> the value is treated as world-space; otherwise as local to <see cref="Parent"/>.</summary>
        T Spawn(Vector3 position, bool worldPosition = true);

        /// <summary>Returns <paramref name="item"/> to the inactive bucket. No-op if the item is not currently active in this pool.</summary>
        void Release(T item);

        /// <summary>Releases every currently active item back to the inactive bucket.</summary>
        void ReleaseAll();

        /// <summary>Live view (not a copy) over the active items. Order matches spawn order; the oldest item is at index 0.</summary>
        IReadOnlyList<T> ActiveItems { get; }

        /// <summary>Live view (not a copy) over the inactive items.</summary>
        IReadOnlyList<T> InactiveItems { get; }
    }
}
