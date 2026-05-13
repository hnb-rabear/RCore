using System.Collections.Generic;
using UnityEngine;

namespace RevCore
{
    public interface IPool<T> where T : Component
    {
        T Prefab { get; }
        Transform Parent { get; }
        string Name { get; }
        int ActiveCount { get; }
        int InactiveCount { get; }
        T Spawn();
        T Spawn(Vector3 position, bool worldPosition = true);
        void Release(T item);
        void ReleaseAll();
        IReadOnlyList<T> ActiveItems { get; }
        IReadOnlyList<T> InactiveItems { get; }
    }
}
