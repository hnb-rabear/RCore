using NUnit.Framework;

namespace RevCore.Tests
{
	public class TimerSchedulerTests
	{
		[Test]
		public void WaitForSeconds_returns_running_handle()
		{
			var scheduler = new TimerScheduler();
			var handle = scheduler.WaitForSeconds(1f, () => { });

			Assert.AreEqual(1, scheduler.ActiveCount);
			Assert.IsTrue(handle.IsRunning);
			Assert.IsFalse(handle.IsCompleted);
			Assert.IsFalse(handle.IsCancelled);
			Assert.AreEqual(1f, handle.Duration);
			Assert.AreEqual(0f, handle.Elapsed);
			Assert.AreEqual(1f, handle.Remaining);
		}

		[Test]
		public void Cancel_handle_removes_timer()
		{
			var scheduler = new TimerScheduler();
			var handle = scheduler.WaitForSeconds(1f, () => { });

			handle.Cancel();

			Assert.AreEqual(0, scheduler.ActiveCount);
			Assert.IsFalse(handle.IsRunning);
			Assert.IsTrue(handle.IsCancelled);
		}

		[Test]
		public void Tick_completes_countdown_once()
		{
			int calls = 0;
			float overtime = 0f;
			var scheduler = new TimerScheduler();
			var handle = scheduler.WaitForSeconds(1f, value => { calls++; overtime = value; });

			scheduler.Tick(0.4f, 0.4f);
			scheduler.Tick(0.7f, 0.7f);
			scheduler.Tick(1f, 1f);

			Assert.AreEqual(1, calls);
			Assert.AreEqual(0.1f, overtime, 0.0001f);
			Assert.IsTrue(handle.IsCompleted);
			Assert.AreEqual(0, scheduler.ActiveCount);
		}

		[Test]
		public void Tick_uses_unscaled_delta_for_unscaled_timer()
		{
			int calls = 0;
			var scheduler = new TimerScheduler();
			scheduler.WaitForSeconds(1f, () => calls++, true);

			scheduler.Tick(0f, 1f);

			Assert.AreEqual(1, calls);
		}

		[Test]
		public void WaitForCondition_completes_when_condition_true()
		{
			bool ready = false;
			int calls = 0;
			var scheduler = new TimerScheduler();
			var handle = scheduler.WaitForCondition(() => ready, () => calls++);

			scheduler.Tick(1f, 1f);
			ready = true;
			scheduler.Tick(1f, 1f);

			Assert.AreEqual(1, calls);
			Assert.IsTrue(handle.IsCompleted);
			Assert.AreEqual(0, scheduler.ActiveCount);
		}

		[Test]
		public void Same_non_zero_id_replaces_existing_timer()
		{
			int first = 0;
			int second = 0;
			var scheduler = new TimerScheduler();

			scheduler.WaitForSeconds(1f, () => first++, false, 7);
			scheduler.WaitForSeconds(1f, () => second++, false, 7);
			scheduler.Tick(1f, 1f);

			Assert.AreEqual(0, first);
			Assert.AreEqual(1, second);
			Assert.AreEqual(0, scheduler.ActiveCount);
		}

		[Test]
		public void Enqueue_runs_action_on_next_tick()
		{
			int calls = 0;
			var scheduler = new TimerScheduler();

			scheduler.Enqueue(() => calls++);

			Assert.AreEqual(1, scheduler.ActiveCount);
			scheduler.Tick(0f, 0f);

			Assert.AreEqual(1, calls);
			Assert.AreEqual(0, scheduler.ActiveCount);
		}

		[Test]
		public void Clear_removes_all_pending_work()
		{
			int calls = 0;
			var scheduler = new TimerScheduler();

			scheduler.WaitForSeconds(1f, () => calls++);
			scheduler.WaitForCondition(() => true, () => calls++);
			scheduler.Enqueue(() => calls++);
			scheduler.Clear();
			scheduler.Tick(1f, 1f);

			Assert.AreEqual(0, calls);
			Assert.AreEqual(0, scheduler.ActiveCount);
		}
	}
}
