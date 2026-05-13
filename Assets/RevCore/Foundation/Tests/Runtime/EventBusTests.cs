using NUnit.Framework;

namespace RevCore.Tests
{
	public class EventBusTests
	{
		private struct TestEvent : IEvent
		{
			public int value;
		}

		private struct OtherEvent : IEvent
		{
			public string message;
		}

		private IEventBus m_bus;

		[SetUp]
		public void SetUp()
		{
			m_bus = new EventBus();
		}

		[Test]
		public void Subscribe_and_publish_delivers_event()
		{
			int received = 0;
			m_bus.Subscribe<TestEvent>(e => received = e.value);
			m_bus.Publish(new TestEvent { value = 42 });
			Assert.AreEqual(42, received);
		}

		[Test]
		public void Unsubscribe_stops_delivery()
		{
			int received = 0;
			void handler(TestEvent e) => received = e.value;
			m_bus.Subscribe<TestEvent>(handler);
			m_bus.Unsubscribe<TestEvent>(handler);
			m_bus.Publish(new TestEvent { value = 99 });
			Assert.AreEqual(0, received);
		}

		[Test]
		public void Double_subscribe_same_handler_ignored()
		{
			int count = 0;
			void handler(TestEvent e) => count++;
			m_bus.Subscribe<TestEvent>(handler);
			m_bus.Subscribe<TestEvent>(handler);
			m_bus.Publish(new TestEvent { value = 1 });
			Assert.AreEqual(1, count);
		}

		[Test]
		public void Different_event_types_isolated()
		{
			int testReceived = 0;
			string otherReceived = null;
			m_bus.Subscribe<TestEvent>(e => testReceived = e.value);
			m_bus.Subscribe<OtherEvent>(e => otherReceived = e.message);
			m_bus.Publish(new TestEvent { value = 7 });
			Assert.AreEqual(7, testReceived);
			Assert.IsNull(otherReceived);
		}

		[Test]
		public void Clear_removes_all_listeners()
		{
			int received = 0;
			m_bus.Subscribe<TestEvent>(e => received = e.value);
			m_bus.Clear();
			m_bus.Publish(new TestEvent { value = 50 });
			Assert.AreEqual(0, received);
			Assert.AreEqual(0, m_bus.ListenerCount);
		}

		[Test]
		public void Clear_generic_removes_one_type()
		{
			int testReceived = 0;
			string otherReceived = null;
			m_bus.Subscribe<TestEvent>(e => testReceived = e.value);
			m_bus.Subscribe<OtherEvent>(e => otherReceived = e.message);
			m_bus.Clear<TestEvent>();
			m_bus.Publish(new TestEvent { value = 50 });
			m_bus.Publish(new OtherEvent { message = "hi" });
			Assert.AreEqual(0, testReceived);
			Assert.AreEqual("hi", otherReceived);
		}

		[Test]
		public void Unsubscribe_nonexistent_handler_no_error()
		{
			void handler(TestEvent e) { }
			Assert.DoesNotThrow(() => m_bus.Unsubscribe<TestEvent>(handler));
		}
	}
}
