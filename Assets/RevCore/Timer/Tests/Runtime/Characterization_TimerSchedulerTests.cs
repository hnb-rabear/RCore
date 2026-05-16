// CHARACTERIZATION TESTS — pin current TimerScheduler.Cancel(int) behavior.
// Phase 4 will switch Cancel(int) to dictionary lookup for O(1); the observable
// contract pinned here (no-op on empty, cancel all matching IDs, default id=0
// semantics) must be preserved.

using NUnit.Framework;

namespace RevCore.Tests
{
	public class Characterization_TimerSchedulerTests
	{
		// PIN: Cancel(int) on an empty scheduler does not throw.
		// Loop is `for (i = count-1; i >= 0; i--)` so count==0 is fine, but pinning
		// the contract guards against future refactors that might index unguarded.
		[Test]
		public void Cancel_on_empty_scheduler_is_noop()
		{
			var s = new TimerScheduler();
			Assert.DoesNotThrow(() => s.Cancel(42));
			Assert.AreEqual(0, s.ActiveCount);
		}

		// PIN: WaitForSeconds with id=0 (the DEFAULT) produces a handle with Id == 0.
		// Cancel(0) will then match this timer — meaning ANY caller using default id
		// can be cancelled by ANY OTHER caller invoking Cancel(0). This is a sharp
		// edge of the current API. Phase 4/6 may treat id==0 as "untracked, cannot
		// be cancelled by id" — that is a deliberate breaking change.
		[Test]
		public void Cancel_with_id_zero_cancels_all_default_id_timers()
		{
			var s = new TimerScheduler();
			bool firedA = false, firedB = false;
			var h1 = s.WaitForSeconds(10f, () => firedA = true);  // id=0 by default
			var h2 = s.WaitForSeconds(20f, () => firedB = true);  // id=0 by default
			Assert.AreEqual(0, h1.Id);
			Assert.AreEqual(0, h2.Id);
			Assert.AreEqual(2, s.ActiveCount);

			s.Cancel(0);

			Assert.IsTrue(h1.IsCancelled);
			Assert.IsTrue(h2.IsCancelled);
			Assert.IsFalse(firedA);
			Assert.IsFalse(firedB);
		}

		// PIN: Cancel(int) cancels every timer that matches that id, across both
		// countdown and condition lists. Used for "named" timer slots where a new
		// timer should replace any in-flight timer with the same key.
		[Test]
		public void Cancel_with_id_cancels_only_matching_timers()
		{
			var s = new TimerScheduler();
			bool firedTagged = false, firedOther = false;
			var tagged = s.WaitForSeconds(5f, () => firedTagged = true, false, id: 99);
			var other = s.WaitForSeconds(5f, () => firedOther = true, false, id: 1);

			s.Cancel(99);

			Assert.IsTrue(tagged.IsCancelled);
			Assert.IsFalse(other.IsCancelled, "Other id must remain alive.");
			Assert.IsFalse(firedTagged);
		}

		// PIN: Cancel(int) with an id that matches nothing is a silent no-op.
		[Test]
		public void Cancel_with_unknown_id_is_silent_noop()
		{
			var s = new TimerScheduler();
			var h = s.WaitForSeconds(5f, () => { }, false, id: 1);
			Assert.DoesNotThrow(() => s.Cancel(999));
			Assert.IsFalse(h.IsCancelled);
		}

		// PIN: Cancel(ITimerHandle) tolerates null silently (handle?.Cancel()).
		[Test]
		public void Cancel_with_null_handle_is_silent_noop()
		{
			var s = new TimerScheduler();
			Assert.DoesNotThrow(() => s.Cancel((ITimerHandle)null));
		}
	}
}
