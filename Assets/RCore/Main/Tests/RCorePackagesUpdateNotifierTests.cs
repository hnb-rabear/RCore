using System;
using NUnit.Framework;
using RCore.Editor;

namespace RCore.Main.Tests
{
	public class RCorePackagesUpdateNotifierTests
	{
		private static readonly DateTime NOW = new DateTime(2026, 8, 22, 12, 0, 0, DateTimeKind.Utc);

		[Test]
		public void should_check_when_no_previous_check_exists()
		{
			Assert.IsTrue(RCorePackagesUpdateNotifier.ShouldCheck(NOW, null, null));
		}

		[Test]
		public void should_not_check_when_muted()
		{
			Assert.IsFalse(RCorePackagesUpdateNotifier.ShouldCheck(NOW, NOW.AddHours(-7), NOW.AddHours(1)));
		}

		[Test]
		public void should_not_check_before_six_hours_elapsed()
		{
			Assert.IsFalse(RCorePackagesUpdateNotifier.ShouldCheck(NOW, NOW.AddHours(-5).AddMinutes(-59), null));
		}

		[Test]
		public void should_check_at_six_hours_elapsed()
		{
			Assert.IsTrue(RCorePackagesUpdateNotifier.ShouldCheck(NOW, NOW.AddHours(-6), null));
		}

		[Test]
		public void should_check_when_mute_expired()
		{
			Assert.IsTrue(RCorePackagesUpdateNotifier.ShouldCheck(NOW, NOW.AddHours(-7), NOW));
		}

		[Test]
		public void should_not_check_when_last_check_is_one_tick_before_interval()
		{
			Assert.IsFalse(RCorePackagesUpdateNotifier.ShouldCheck(NOW, NOW - TimeSpan.FromHours(6) + TimeSpan.FromTicks(1), null));
		}

		[Test]
		public void should_not_check_when_mute_is_one_tick_in_future()
		{
			Assert.IsFalse(RCorePackagesUpdateNotifier.ShouldCheck(NOW, null, NOW.AddTicks(1)));
		}
	}
}
