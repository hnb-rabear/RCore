using NUnit.Framework;

namespace RevCore.Tests
{
	public class TimedActionTests
	{
		[Test]
		public void Start_sets_running_state()
		{
			var action = new TimedAction();

			action.Start(2f);

			Assert.IsTrue(action.IsRunning);
			Assert.AreEqual(2f, action.TimeTarget);
			Assert.AreEqual(2f, action.RemainTime);
		}

		[Test]
		public void Update_finishes_after_target_time()
		{
			int calls = 0;
			var action = new TimedAction { OnFinished = () => calls++ };

			action.Start(1f);
			action.Update(0.4f);
			action.Update(0.6f);

			Assert.AreEqual(1, calls);
			Assert.IsFalse(action.IsRunning);
			Assert.AreEqual(0f, action.RemainTime);
		}

		[Test]
		public void Stop_does_not_invoke_callback()
		{
			int calls = 0;
			var action = new TimedAction { OnFinished = () => calls++ };

			action.Start(1f);
			action.Stop();

			Assert.AreEqual(0, calls);
			Assert.IsFalse(action.IsRunning);
			Assert.AreEqual(0f, action.GetElapsedTime());
		}
	}
}
