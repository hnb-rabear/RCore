using NUnit.Framework;
using UnityEngine;

namespace RevCore.Tests
{
    public class PoolsContainerTests
    {
        private GameObject m_prefab;
        private PoolsContainer<PoolObject> m_container;

        [SetUp]
        public void SetUp()
        {
            m_prefab = new GameObject("EnemyPrefab");
            m_prefab.AddComponent<PoolObject>();
            m_container = new PoolsContainer<PoolObject>("Pools", 1);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(m_prefab);
            UnityEngine.Object.DestroyImmediate(m_container.Container.gameObject);
        }

        [Test]
        public void Get_creates_one_pool_per_prefab()
        {
            var prefab = m_prefab.GetComponent<PoolObject>();

            var first = m_container.Get(prefab);
            var second = m_container.Get(prefab);

            Assert.AreSame(first, second);
            Assert.AreEqual(1, m_container.PoolCount);
        }

        [Test]
        public void Spawn_tracks_clone_to_pool_for_release()
        {
            var prefab = m_prefab.GetComponent<PoolObject>();
            var clone = m_container.Spawn(prefab, new Vector3(4f, 5f, 6f));

            m_container.Release(clone);

            Assert.IsFalse(clone.gameObject.activeSelf);
            Assert.AreEqual(0, m_container.Get(prefab).ActiveCount);
        }

        [Test]
        public void ReleaseAll_releases_all_pools()
        {
            var prefab = m_prefab.GetComponent<PoolObject>();
            m_container.Spawn(prefab);
            m_container.Spawn(prefab);

            m_container.ReleaseAll();

            Assert.AreEqual(0, m_container.Get(prefab).ActiveCount);
        }
    }
}
