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
		// active item (index 0 of m_activeList), making room for the new one.
		// Active count stays at the limit; total spawned never exceeds limit.
		[Test]
		public void Spawn_at_limit_evicts_oldest_active()
		{
			var pool = new RevPool<PoolObject>(m_prefab.GetComponent<PoolObject>(), 0, m_parent)
			{
				LimitNumber = 2,
			};

			var first = pool.Spawn();
			var second = pool.Spawn();
			Assert.AreEqual(2, pool.ActiveCount);

			// First object is the "oldest" — should be evicted by the third spawn.
			var third = pool.Spawn();

			Assert.AreEqual(2, pool.ActiveCount,
				"Active count stays capped at LimitNumber after eviction.");
			Assert.IsFalse(first.gameObject.activeSelf,
				"Oldest (first) should now be inactive — evicted by the cap.");
			Assert.IsTrue(second.gameObject.activeSelf, "Second remains active.");
			Assert.IsTrue(third.gameObject.activeSelf, "Newly spawned item is active.");
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
