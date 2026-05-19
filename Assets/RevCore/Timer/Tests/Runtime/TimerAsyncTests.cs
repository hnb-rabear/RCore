using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace RevCore.Tests
{
	public class TimerAsyncTests
	{
		private TimerScheduler m_scheduler;

		[SetUp]
		public void SetUp()
		{
			m_scheduler = new TimerScheduler();
			Timers.Scheduler = m_scheduler;
		}

		[TearDown]
		public void TearDown()
		{
			Timers.Scheduler = null;
		}

		[Test]
		public async UniTaskVoid DelayAsync_returns_after_duration()
		{
			var task = Timers.DelayAsync(0.05f);
			Assert.IsFalse(task.Status.IsCompleted(), "Should not complete before Tick.");

			for (int i = 0; i < 6; i++)
				m_scheduler.Tick(0.01f, 0.01f);

			await task;
			Assert.IsTrue(task.Status == UniTaskStatus.Succeeded);
		}

		[Test]
		public void DelayAsync_pre_cancelled_token_returns_cancelled()
		{
			var cts = new CancellationTokenSource();
			cts.Cancel();

			var task = Timers.DelayAsync(1f, false, cts.Token);

			Assert.AreEqual(UniTaskStatus.Canceled, task.Status);
			Assert.AreEqual(0, m_scheduler.ActiveCount, "Pre-cancelled token must not schedule a timer.");
		}

		[Test]
		public async UniTaskVoid DelayAsync_mid_flight_cancellation_throws_OperationCanceledException()
		{
			var cts = new CancellationTokenSource();
			var task = Timers.DelayAsync(1f, false, cts.Token);

			m_scheduler.Tick(0.1f, 0.1f);
			Assert.AreEqual(1, m_scheduler.ActiveCount);

			cts.Cancel();
			m_scheduler.Tick(0.0f, 0.0f);

			Assert.AreEqual(0, m_scheduler.ActiveCount, "Cancellation must drop the handle.");

			try { await task; Assert.Fail("Expected OperationCanceledException"); }
			catch (OperationCanceledException) { /* expected */ }
		}

		[Test]
		public async UniTaskVoid DelayAsync_zero_seconds_completes_on_next_tick()
		{
			var task = Timers.DelayAsync(0f);
			await task;
			Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
		}

		[Test]
		public async UniTaskVoid WaitForConditionAsync_returns_when_predicate_true()
		{
			bool flag = false;
			var task = Timers.WaitForConditionAsync(() => flag);

			m_scheduler.Tick(0f, 0f);
			Assert.IsFalse(task.Status.IsCompleted());

			flag = true;
			m_scheduler.Tick(0f, 0f);

			await task;
			Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
		}

		[Test]
		public void WaitForConditionAsync_null_predicate_returns_faulted()
		{
			var task = Timers.WaitForConditionAsync(null);
			Assert.AreEqual(UniTaskStatus.Faulted, task.Status);
		}

		[Test]
		public async UniTaskVoid WaitForConditionAsync_predicate_exception_propagates()
		{
			var task = Timers.WaitForConditionAsync(() => throw new InvalidOperationException("predicate boom"));

			m_scheduler.Tick(0f, 0f);

			try { await task; Assert.Fail("Expected InvalidOperationException"); }
			catch (InvalidOperationException ex) { Assert.AreEqual("predicate boom", ex.Message); }
		}

		[Test]
		public async UniTaskVoid WaitForConditionAsync_mid_flight_cancellation()
		{
			var cts = new CancellationTokenSource();
			var task = Timers.WaitForConditionAsync(() => false, cts.Token);

			m_scheduler.Tick(0f, 0f);
			Assert.AreEqual(1, m_scheduler.ActiveCount);

			cts.Cancel();
			m_scheduler.Tick(0f, 0f);

			try { await task; Assert.Fail("Expected OperationCanceledException"); }
			catch (OperationCanceledException) { }
		}

		[Test]
		public async UniTaskVoid WaitForFramesAsync_returns_after_n_ticks()
		{
			var task = Timers.WaitForFramesAsync(3);

			m_scheduler.Tick(0f, 0f);
			Assert.IsFalse(task.Status.IsCompleted());
			m_scheduler.Tick(0f, 0f);
			Assert.IsFalse(task.Status.IsCompleted());
			m_scheduler.Tick(0f, 0f);

			await task;
			Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
		}

		[Test]
		public void WaitForFramesAsync_zero_frames_completes_synchronously()
		{
			var task = Timers.WaitForFramesAsync(0);
			Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
			Assert.AreEqual(0, m_scheduler.ActiveCount);
		}

		[Test]
		public void WaitForFramesAsync_negative_frames_completes_synchronously()
		{
			var task = Timers.WaitForFramesAsync(-5);
			Assert.AreEqual(UniTaskStatus.Succeeded, task.Status);
		}

		[Test]
		public async UniTaskVoid WaitForFramesAsync_mid_flight_cancellation()
		{
			var cts = new CancellationTokenSource();
			var task = Timers.WaitForFramesAsync(100, cts.Token);

			m_scheduler.Tick(0f, 0f);
			cts.Cancel();
			m_scheduler.Tick(0f, 0f);

			try { await task; Assert.Fail("Expected OperationCanceledException"); }
			catch (OperationCanceledException) { }
		}
	}
}
