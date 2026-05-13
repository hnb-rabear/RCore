using NUnit.Framework;
using UnityEngine;

namespace RevCore.Tests
{
    public class RevPoolTests
    {
        private GameObject m_prefab;
        private Transform m_parent;

        [SetUp]
        public void SetUp()
        {
            m_prefab = new GameObject("BulletPrefab");
            m_prefab.AddComponent<PoolObject>();
            m_parent = new GameObject("PoolParent").transform;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_prefab);
            Object.DestroyImmediate(m_parent.gameObject);
        }

        [Test]
        public void Constructor_prewarms_inactive_items()
        {
            var pool = new RevPool<PoolObject>(m_prefab.GetComponent<PoolObject>(), 2, m_parent);

            Assert.AreEqual(0, pool.ActiveCount);
            Assert.AreEqual(2, pool.InactiveCount);
            Assert.AreEqual("BulletPrefab", pool.Name);
        }

        [Test]
        public void Spawn_activates_item_and_reuses_inactive()
        {
            var pool = new RevPool<PoolObject>(m_prefab.GetComponent<PoolObject>(), 1, m_parent);

            var item = pool.Spawn(new Vector3(1f, 2f, 3f));

            Assert.IsTrue(item.gameObject.activeSelf);
            Assert.AreEqual(1, pool.ActiveCount);
            Assert.AreEqual(0, pool.InactiveCount);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), item.transform.position);
        }

        [Test]
        public void Release_moves_item_to_inactive()
        {
            var pool = new RevPool<PoolObject>(m_prefab.GetComponent<PoolObject>(), 1, m_parent);
            var item = pool.Spawn();

            pool.Release(item);

            Assert.IsFalse(item.gameObject.activeSelf);
            Assert.AreEqual(0, pool.ActiveCount);
            Assert.AreEqual(1, pool.InactiveCount);
        }

        [Test]
        public void Limit_reuses_oldest_active_item()
        {
            var pool = new RevPool<PoolObject>(m_prefab.GetComponent<PoolObject>(), 0, m_parent)
            {
                LimitNumber = 1
            };

            var first = pool.Spawn();
            var second = pool.Spawn();

            Assert.AreSame(first, second);
            Assert.AreEqual(1, pool.ActiveCount);
            Assert.AreEqual(0, pool.InactiveCount);
        }

        [Test]
        public void Delayed_release_uses_timers_facade()
        {
            var previous = Timers.Scheduler;
            var scheduler = new TimerScheduler();
            Timers.Scheduler = scheduler;

            try
            {
                var pool = new RevPool<PoolObject>(m_prefab.GetComponent<PoolObject>(), 1, m_parent);
                var item = pool.Spawn();

                pool.Release(item, 1f);
                scheduler.Tick(1f, 1f);

                Assert.AreEqual(0, pool.ActiveCount);
                Assert.AreEqual(1, pool.InactiveCount);
            }
            finally
            {
                Timers.Scheduler = previous;
            }
        }
    }
}
