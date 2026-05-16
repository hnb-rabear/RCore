// CHARACTERIZATION TESTS — pin current EventBus behavior. Phase 4 will rework
// ListenerCount to O(1); these tests document the observable contract that
// must continue to hold after that change.

using System;
using NUnit.Framework;

namespace RevCore.Tests
{
	public class Characterization_EventBusTests
	{
		private struct PingEvent : IEvent { public int Value; }
		private struct OtherEvent : IEvent { public string Tag; }

		// PIN: Publish with no listeners is a silent no-op. Critical contract:
		// consumer code calls Publish() in hot paths and relies on no exception when
		// no subscriber is registered yet.
		[Test]
		public void Publish_with_no_listeners_is_silent_noop()
		{
			var bus = new EventBus();
			Assert.DoesNotThrow(() => bus.Publish(new PingEvent { Value = 1 }));
		}

		// PIN: Publish before any Subscribe call for that type is no-op (different code path
		// than "had listeners then unsubscribed all" — pin both).
		[Test]
		public void Publish_before_any_subscribe_for_type_is_noop()
		{
			var bus = new EventBus();
			bus.Subscribe<OtherEvent>(_ => { });
			Assert.DoesNotThrow(() => bus.Publish(new PingEvent { Value = 7 }));
		}

		// PIN: After unsubscribing the last listener for a type, Publish stays silent.
		// (EventBus removes the dictionary entry when delegate becomes null — see Unsubscribe.)
		[Test]
		public void Publish_after_last_unsubscribe_is_noop()
		{
			var bus = new EventBus();
			Action<PingEvent> handler = _ => { };
			bus.Subscribe(handler);
			bus.Unsubscribe(handler);
			Assert.DoesNotThrow(() => bus.Publish(new PingEvent { Value = 3 }));
		}

		// PIN: Subscribing the same handler twice is deduped — handler runs once.
		[Test]
		public void Duplicate_subscribe_does_not_double_invoke()
		{
			var bus = new EventBus();
			int hits = 0;
			Action<PingEvent> handler = _ => hits++;
			bus.Subscribe(handler);
			bus.Subscribe(handler);
			bus.Publish(new PingEvent());
			Assert.AreEqual(1, hits);
		}

		// PIN: ListenerCount today walks invocation lists across all subscribed types.
		// Phase 4 will keep this property but make the underlying access O(1) — the
		// observable contract (the number) must match.
		[Test]
		public void ListenerCount_sums_invocation_lists_across_types()
		{
			var bus = new EventBus();
			Assert.AreEqual(0, bus.ListenerCount);

			bus.Subscribe<PingEvent>(_ => { });
			bus.Subscribe<PingEvent>(_ => { });
			bus.Subscribe<OtherEvent>(_ => { });
			Assert.AreEqual(3, bus.ListenerCount);

			bus.Clear<PingEvent>();
			Assert.AreEqual(1, bus.ListenerCount);

			bus.Clear();
			Assert.AreEqual(0, bus.ListenerCount);
		}
	}
}
