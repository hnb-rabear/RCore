using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace RevCore.Tests
{
	/// <summary>
	/// Foundation-side tests for <see cref="IRevDiagnostics"/>. Covers the contract (null listener
	/// is a no-op, switching listener takes immediate effect) and the EventBus hook sites. Timer,
	/// Pool, and Audio hook coverage lives outside Foundation.Tests since those modules are not
	/// referenced from this assembly.
	/// </summary>
	public class RevDiagnosticsTests
	{
		private sealed class FakeDiagnostics : IRevDiagnostics
		{
			public readonly List<string> Calls = new();
			public void OnTimerScheduled(int id, float duration, bool unscaled) => Calls.Add($"TimerScheduled({id},{duration:F2},{unscaled})");
			public void OnTimerCancelled(int id) => Calls.Add($"TimerCancelled({id})");
			public void OnTimerCompleted(int id, float overtime) => Calls.Add($"TimerCompleted({id},{overtime:F2})");
			public void OnEventPublished(Type eventType, int listenerCount) => Calls.Add($"EventPublished({eventType.Name},{listenerCount})");
			public void OnEventSubscribed(Type eventType, int newCount) => Calls.Add($"EventSubscribed({eventType.Name},{newCount})");
			public void OnEventUnsubscribed(Type eventType, int newCount) => Calls.Add($"EventUnsubscribed({eventType.Name},{newCount})");
			public void OnPoolSpawn(string poolName, bool reused) => Calls.Add($"PoolSpawn({poolName},{reused})");
			public void OnPoolRelease(string poolName) => Calls.Add($"PoolRelease({poolName})");
			public void OnAudioPlaySFX(string clipName) => Calls.Add($"AudioPlaySFX({clipName})");
			public void OnAudioPlayMusic(string clipName, bool looping) => Calls.Add($"AudioPlayMusic({clipName},{looping})");
		}

		private struct PingEvent : IEvent { public int Value; }

		[TearDown]
		public void TearDown() => RevDiagnostics.Listener = null;

		[Test]
		public void Default_listener_is_null()
		{
			Assert.IsNull(RevDiagnostics.Listener);
		}

		[Test]
		public void EventBus_no_listener_does_not_throw()
		{
			var bus = new EventBus();
			Assert.DoesNotThrow(() => bus.Subscribe<PingEvent>(_ => { }));
			Assert.DoesNotThrow(() => bus.Publish(new PingEvent { Value = 1 }));
			Assert.DoesNotThrow(() => bus.Unsubscribe<PingEvent>(_ => { }));
		}

		[Test]
		public void EventBus_subscribe_fires_OnEventSubscribed_with_new_count()
		{
			var diag = new FakeDiagnostics();
			RevDiagnostics.Listener = diag;
			var bus = new EventBus();

			bus.Subscribe<PingEvent>(_ => { });

			Assert.AreEqual(1, diag.Calls.Count);
			Assert.AreEqual("EventSubscribed(PingEvent,1)", diag.Calls[0]);
		}

		[Test]
		public void EventBus_subscribe_dedup_does_not_fire_hook()
		{
			var diag = new FakeDiagnostics();
			RevDiagnostics.Listener = diag;
			var bus = new EventBus();
			Action<PingEvent> handler = _ => { };

			bus.Subscribe(handler);
			bus.Subscribe(handler); // dedup hit

			Assert.AreEqual(1, diag.Calls.Count, "Dedup'd second Subscribe must not fire OnEventSubscribed.");
			Assert.AreEqual("EventSubscribed(PingEvent,1)", diag.Calls[0]);
		}

		[Test]
		public void EventBus_publish_fires_OnEventPublished_even_with_no_listeners()
		{
			var diag = new FakeDiagnostics();
			RevDiagnostics.Listener = diag;
			var bus = new EventBus();

			bus.Publish(new PingEvent { Value = 1 });

			Assert.AreEqual("EventPublished(PingEvent,0)", diag.Calls[0]);
		}

		[Test]
		public void EventBus_publish_fires_with_listener_count()
		{
			var diag = new FakeDiagnostics();
			RevDiagnostics.Listener = diag;
			var bus = new EventBus();
			bus.Subscribe<PingEvent>(_ => { });
			bus.Subscribe<PingEvent>(_ => { });
			diag.Calls.Clear();

			bus.Publish(new PingEvent { Value = 1 });

			Assert.AreEqual("EventPublished(PingEvent,2)", diag.Calls[0]);
		}

		[Test]
		public void EventBus_unsubscribe_fires_OnEventUnsubscribed()
		{
			var diag = new FakeDiagnostics();
			RevDiagnostics.Listener = diag;
			var bus = new EventBus();
			Action<PingEvent> handler = _ => { };
			bus.Subscribe(handler);
			diag.Calls.Clear();

			bus.Unsubscribe(handler);

			Assert.AreEqual("EventUnsubscribed(PingEvent,0)", diag.Calls[0]);
		}

		[Test]
		public void EventBus_unsubscribe_not_registered_does_not_fire_hook()
		{
			var diag = new FakeDiagnostics();
			RevDiagnostics.Listener = diag;
			var bus = new EventBus();

			bus.Unsubscribe<PingEvent>(_ => { });

			Assert.AreEqual(0, diag.Calls.Count);
		}

		[Test]
		public void Switching_listener_takes_immediate_effect()
		{
			var firstDiag = new FakeDiagnostics();
			var secondDiag = new FakeDiagnostics();
			var bus = new EventBus();

			RevDiagnostics.Listener = firstDiag;
			bus.Subscribe<PingEvent>(_ => { });
			RevDiagnostics.Listener = secondDiag;
			bus.Publish(new PingEvent());

			Assert.AreEqual(1, firstDiag.Calls.Count, "First listener captured only its Subscribe.");
			Assert.AreEqual(1, secondDiag.Calls.Count, "Second listener captured only its Publish.");
			Assert.AreEqual("EventSubscribed(PingEvent,1)", firstDiag.Calls[0]);
			Assert.AreEqual("EventPublished(PingEvent,1)", secondDiag.Calls[0]);
		}
	}
}
