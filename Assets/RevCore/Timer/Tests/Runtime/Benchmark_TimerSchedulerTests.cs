// BENCHMARK TESTS — TimerScheduler hot paths. Phase 4 will switch Cancel(int)
// from O(n) linear scan to O(1) dictionary lookup; this baseline captures
// the gap and the eventual win.

using NUnit.Framework;
using Unity.PerformanceTesting;

namespace RevCore.Tests
{
	[Category("Performance")]
	public class Benchmark_TimerSchedulerTests
	{
		// Baseline: Cancel one ID out of 1000 active countdown timers.
		// Current implementation walks the full list. Phase 4 target: O(1) via dict.
		// We expect this number to drop by ~3 orders of magnitude after Phase 4.
		[Test, Performance]
		public void Cancel_one_id_among_1000_timers()
		{
			var s = new TimerScheduler();
			for (int i = 1; i <= 1000; i++)
				s.WaitForSeconds(60f, () => { }, false, id: i);

			Measure.Method(() =>
				{
					// Cancel an id in the middle. Re-create the cancelled timer so subsequent
					// measurements still have an id-500 to cancel.
					s.Cancel(500);
					s.WaitForSeconds(60f, () => { }, false, id: 500);
				})
				.WarmupCount(5)
				.MeasurementCount(20)
				.Run();
		}

		// Baseline: Tick 1000 active timers across 1000 frames (1M timer-frames).
		// Phase 4 will not change Tick path materially — this is a regression guard.
		[Test, Performance]
		public void Tick_1000_timers_for_1000_frames()
		{
			var s = new TimerScheduler();
			for (int i = 0; i < 1000; i++)
				s.WaitForSeconds(1000f, () => { }, false, id: i + 1);

			Measure.Method(() =>
				{
					for (int frame = 0; frame < 1000; frame++)
						s.Tick(0.016f, 0.016f);
				})
				.WarmupCount(2)
				.MeasurementCount(5)
				.GC()
				.Run();
		}

		// Baseline: WaitForSeconds creation cost — allocates TimerHandle + CountdownTimer.
		// Phase 4 may pool these — measure to catch the win.
		[Test, Performance]
		public void Create_10000_timers()
		{
			Measure.Method(() =>
				{
					var s = new TimerScheduler();
					for (int i = 0; i < 10_000; i++)
						s.WaitForSeconds(60f, () => { }, false, id: i + 1);
				})
				.WarmupCount(2)
				.MeasurementCount(5)
				.GC()
				.Run();
		}
	}
}
