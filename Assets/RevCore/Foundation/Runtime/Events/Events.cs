using System;

namespace RevCore
{
	/// <summary>
	/// Static facade over a process-global <see cref="EventBus"/>. Convenient for game-wide events
	/// where threading through a registered <see cref="IEventBus"/> would add ceremony. For scoped
	/// buses (per-scene, per-system), instantiate <see cref="EventBus"/> directly and pass it
	/// through dependency injection instead.
	/// </summary>
	public static class Events
	{
		private static readonly EventBus s_global = new();

		/// <summary>The global event bus instance.</summary>
		public static IEventBus Global => s_global;

		/// <summary>Subscribes <paramref name="listener"/> on the global bus.</summary>
		public static void Subscribe<T>(Action<T> listener) where T : IEvent
			=> s_global.Subscribe(listener);

		/// <summary>Unsubscribes <paramref name="listener"/> from the global bus.</summary>
		public static void Unsubscribe<T>(Action<T> listener) where T : IEvent
			=> s_global.Unsubscribe(listener);

		/// <summary>Publishes <paramref name="evt"/> on the global bus.</summary>
		public static void Publish<T>(T evt) where T : IEvent
			=> s_global.Publish(evt);

		/// <summary>Removes every subscription on the global bus.</summary>
		public static void Clear()
			=> s_global.Clear();
	}
}
