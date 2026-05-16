// BENCHMARK TESTS — establish performance baselines for hot paths in EventBus.
// Phase 4 will optimize ListenerCount to O(1) and tighten Publish hot path;
// baselines captured here are the reference the CI workflow compares against.
//
// Run via Unity Test Runner → PerformanceTest category. Results land in
// PerformanceTestResults.xml which the CI workflow exports as an artifact.

using NUnit.Framework;
using Unity.PerformanceTesting;

namespace RevCore.Tests
{
	[Category("Performance")]
	public class Benchmark_EventBusTests
	{
		private struct EvtA : IEvent { public int Value; }
		private struct EvtB : IEvent { public int Value; }
		private struct EvtC : IEvent { public int Value; }
		private struct EvtD : IEvent { public int Value; }
		private struct EvtE : IEvent { public int Value; }

		// Baseline: Publish a single event type to 100 listeners, 10k times.
		// Phase 4 target: tighten hot path (consider zero-alloc cast removal); must not regress.
		[Test, Performance]
		public void Publish_100_listeners_10k_events()
		{
			var bus = new EventBus();
			// Per-iteration `idx` capture forces a distinct closure instance per Subscribe call.
			// Without it the compiler caches the static `_ => { }` delegate and Subscribe's
			// dedup short-circuits all 100 calls down to 1 listener — quietly turning this
			// into a 1-listener benchmark.
			for (int i = 0; i < 100; i++)
			{
				int idx = i;
				bus.Subscribe<EvtA>(e => { _ = idx; });
			}

			Measure.Method(() =>
				{
					var evt = new EvtA { Value = 1 };
					for (int i = 0; i < 10_000; i++)
						bus.Publish(evt);
				})
				.WarmupCount(3)
				.MeasurementCount(10)
				.GC()
				.Run();
		}

		// Baseline: ListenerCount today walks invocation lists across every subscribed type.
		// Phase 4 will switch to a cached counter; the delta on this test is the proof.
		// The test stays after the optimization to catch regressions back to the slow path.
		[Test, Performance]
		public void ListenerCount_5_types_5_listeners_each_1k_lookups()
		{
			var bus = new EventBus();
			// Per-iteration `idx` capture forces a distinct delegate per Subscribe call — see
			// Publish_100_listeners_10k_events above for the reason.
			for (int i = 0; i < 5; i++) { int idx = i; bus.Subscribe<EvtA>(e => { _ = idx; }); }
			for (int i = 0; i < 5; i++) { int idx = i; bus.Subscribe<EvtB>(e => { _ = idx; }); }
			for (int i = 0; i < 5; i++) { int idx = i; bus.Subscribe<EvtC>(e => { _ = idx; }); }
			for (int i = 0; i < 5; i++) { int idx = i; bus.Subscribe<EvtD>(e => { _ = idx; }); }
			for (int i = 0; i < 5; i++) { int idx = i; bus.Subscribe<EvtE>(e => { _ = idx; }); }

			Measure.Method(() =>
				{
					int sum = 0;
					for (int i = 0; i < 1_000; i++)
						sum += bus.ListenerCount;
					Assert.AreEqual(25_000, sum, "Sanity: 25 listeners × 1000 lookups.");
				})
				.WarmupCount(3)
				.MeasurementCount(10)
				.Run();
		}

		// Baseline: Subscribe + Unsubscribe pair, repeated. Dedup check walks invocation list
		// each time — should remain stable in cost.
		[Test, Performance]
		public void Subscribe_Unsubscribe_pair_1k_iterations()
		{
			var bus = new EventBus();

			Measure.Method(() =>
				{
					System.Action<EvtA> handler = _ => { };
					for (int i = 0; i < 1_000; i++)
					{
						bus.Subscribe(handler);
						bus.Unsubscribe(handler);
					}
				})
				.WarmupCount(3)
				.MeasurementCount(10)
				.GC()
				.Run();
		}
	}
}
