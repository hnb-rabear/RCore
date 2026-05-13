using UnityEngine;

namespace RevCore
{
    public interface IPoolContainer<T> where T : Component
    {
        int PoolCount { get; }
        RevPool<T> Get(T prefab);
        T Spawn(T prefab);
        T Spawn(T prefab, Vector3 position, bool worldPosition = true);
        void Release(T item);
        void ReleaseAll();
    }
}
