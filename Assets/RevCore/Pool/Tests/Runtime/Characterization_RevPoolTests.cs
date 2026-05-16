// CHARACTERIZATION TESTS — pin current RevPool behavior under capacity pressure.
// The "evict oldest active item" policy is part of the public contract that
// consumer projects depend on (e.g., bullet pools rotating oldest off-screen).
// Phase 3/4 may add diagnostic hooks but must not change this observable behavior
// without a major version bump.

using NUnit.Framework;
using UnityEngine;

namespace RevCore.Tests
{
	public class Characterization_RevPoolTests
	{
		private GameObject m_prefab;
		private Transform m_parent;

		[SetUp]
		public void SetUp()
		{
			m_prefab = new GameObject("Prefab");
			m_prefab.AddComponent<PoolObject>();
			m_parent = new GameObject("PoolParent").transform;
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(m_prefab);
			Object.DestroyImmediate(m_parent.gameObject);
		}

		// PIN: when active count reaches LimitNumber, the next Spawn() evicts the OLDEST
		// active item (m_activeList[0]) — but because the eviction moves it to the inactive
		// list and Spawn then immediately picks from inactive, the evicted item is REUSED
		// as the new spawn. Net effect: caller's reference to the oldest is still on an
		// active instance (now rotated to the back of m_activeList), no allocation
		// happens, and ActiveCount stays at the cap.
		[Test]
		public void Spawn_at_limit_evicts_oldest_and_reuses_it_as_new_spawn()
		{
			var pool = new RevPool<PoolObject>(m_prefab.GetComponent<PoolObject>(), 0, m_parent)
			{
				LimitNumber = 2,
			};

			var first = pool.Spawn();
			var second = pool.Spawn();
			Assert.AreEqual(2, pool.ActiveCount);

			var third = pool.Spawn();

			Assert.AreEqual(2, pool.ActiveCount,
				"Active count stays capped at LimitNumber after eviction.");
			Assert.AreSame(first, third,
				"Evicted oldest is the only inactive item available, so Spawn reuses it (no allocation).");
			Assert.IsTrue(first.gameObject.activeSelf,
				"first/third is active again after being reused as the new spawn.");
			Assert.IsTrue(second.gameObject.activeSelf, "Second remains active.");
		}

		// PIN: LimitNumber == 0 means "no cap" — Spawn never evicts.
		[Test]
		public void Spawn_with_zero_limit_never_evicts()
		{
			var pool = new RevPool<PoolObject>(m_prefab.GetComponent<PoolObject>(), 0, m_parent)
			{
				LimitNumber = 0,
			};

			for (int i = 0; i < 5; i++)
				pool.Spawn();

			Assert.AreEqual(5, pool.ActiveCount);
		}

		// PIN: ActiveItems is a *live* view (IReadOnlyList over m_activeList).
		// Phase 4 may keep this property — must remain a live view, not a snapshot.
		[Test]
		public void ActiveItems_reflects_live_active_list()
		{
			var pool = new RevPool<PoolObject>(m_prefab.GetComponent<PoolObject>(), 0, m_parent);
			var view = pool.ActiveItems;
			Assert.AreEqual(0, view.Count);
			pool.Spawn();
			Assert.AreEqual(1, view.Count, "Same reference observed after spawn → view, not copy.");
		}
	}
}
