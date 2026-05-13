using System;

namespace RevCore
{
	public static class Events
	{
		private static readonly EventBus s_global = new();

		public static IEventBus Global => s_global;

		public static void Subscribe<T>(Action<T> listener) where T : IEvent
			=> s_global.Subscribe(listener);

		public static void Unsubscribe<T>(Action<T> listener) where T : IEvent
			=> s_global.Unsubscribe(listener);

		public static void Publish<T>(T evt) where T : IEvent
			=> s_global.Publish(evt);

		public static void Clear()
			=> s_global.Clear();
	}
}
