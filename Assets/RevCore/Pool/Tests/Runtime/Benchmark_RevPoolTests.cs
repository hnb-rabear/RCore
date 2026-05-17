// BENCHMARK TESTS — RevPool hot paths. Phase 4 will optimize RelocateInactive
// and RemoveNullInactiveItems; these baselines catch any regression.

using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;

namespace RevCore.Tests
{
	[Category("Performance")]
	public class Benchmark_RevPoolTests
	{
		private GameObject m_prefab;
		private Transform m_parent;

		[SetUp]
		public void SetUp()
		{
			m_prefab = new GameObject("BenchPrefab");
			m_prefab.AddComponent<PoolObject>();
			m_parent = new GameObject("BenchParent").transform;
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(m_prefab);
			Object.DestroyImmediate(m_parent.gameObject);
		}

		// Baseline: spawn 1000 objects from a pre-warmed pool, then release all.
		// Phase 4 target: keep zero-alloc on the spawn path past the initial prewarm.
		[Test, Performance]
		public void Spawn_and_release_1000()
		{
			var po = m_prefab.GetComponent<PoolObject>();

			Measure.Method(() =>
				{
					var pool = new RevPool<PoolObject>(po, 1000, m_parent);
					var temp = new PoolObject[1000];
					for (int i = 0; i < 1000; i++) temp[i] = pool.Spawn();
					for (int i = 0; i < 1000; i++) pool.Release(temp[i]);
				})
				.WarmupCount(2)
				.MeasurementCount(5)
				.GC()
				.Run();
		}

		// Baseline: with a LimitNumber cap, every Spawn past the cap evicts the oldest.
		// Phase 4 may add a faster eviction strategy (ring buffer) — must not regress.
		[Test, Performance]
		public void Spawn_at_cap_2000_eviction_cycles()
		{
			var po = m_prefab.GetComponent<PoolObject>();
			var pool = new RevPool<PoolObject>(po, 100, m_parent)
			{
				LimitNumber = 100,
			};

			Measure.Method(() =>
				{
					for (int i = 0; i < 2_000; i++)
						pool.Spawn();
				})
				.WarmupCount(2)
				.MeasurementCount(5)
				.GC()
				.Run();
		}
	}
}
