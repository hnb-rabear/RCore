using NUnit.Framework;

namespace RevCore.Tests
{
	public class EventBusListenerCountForTests
	{
		private struct EvtA : IEvent { public int Value; }
		private struct EvtB : IEvent { public int Value; }

		[Test]
		public void Zero_when_no_listeners_for_type()
		{
			var bus = new EventBus();
			Assert.AreEqual(0, bus.ListenerCountFor<EvtA>());
		}

		[Test]
		public void Counts_listeners_for_requested_type_only()
		{
			var bus = new EventBus();
			bus.Subscribe<EvtA>(_ => { });
			bus.Subscribe<EvtA>(_ => { });
			bus.Subscribe<EvtB>(_ => { });

			Assert.AreEqual(2, bus.ListenerCountFor<EvtA>());
			Assert.AreEqual(1, bus.ListenerCountFor<EvtB>());
			Assert.AreEqual(3, bus.ListenerCount, "Aggregate ListenerCount contract unchanged.");
		}

		[Test]
		public void Returns_zero_after_clearing_type()
		{
			var bus = new EventBus();
			bus.Subscribe<EvtA>(_ => { });
			bus.Clear<EvtA>();
			Assert.AreEqual(0, bus.ListenerCountFor<EvtA>());
		}
	}
}
