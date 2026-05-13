using UnityEngine;

namespace RevCore.Samples
{
    public class PoolSample : MonoBehaviour
    {
        [SerializeField] private PoolObject prefab;
        private PoolsContainer<PoolObject> m_pools;

        private void Awake()
        {
            m_pools = new PoolsContainer<PoolObject>("SamplePools", 3, transform);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var item = m_pools.Spawn(prefab, Random.insideUnitSphere * 3f);
                m_pools.Get(prefab).Release(item, 2f);
            }
        }
    }
}
