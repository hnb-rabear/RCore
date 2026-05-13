using NUnit.Framework;

namespace RevCore.Tests
{
	public class GlobalTimersTests
	{
		[Test]
		public void Timers_facade_uses_replaceable_scheduler()
		{
			var previous = Timers.Scheduler;
			var scheduler = new TimerScheduler();
			Timers.Scheduler = scheduler;
			int calls = 0;

			Timers.WaitForSeconds(1f, () => calls++);
			Timers.Tick(1f, 1f);

			Assert.AreEqual(1, calls);
			Assert.AreSame(scheduler, Timers.Scheduler);
			Timers.Scheduler = previous;
		}
	}
}
