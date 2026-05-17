using System;

namespace RevCore
{
	/// <summary>
	/// Pub/sub message bus for events implementing <see cref="IEvent"/>. The default
	/// implementation is <see cref="EventBus"/>; consumers may register a custom
	/// instance via <see cref="IServiceLocator"/>.
	/// </summary>
	public interface IEventBus
	{
		/// <summary>Registers <paramref name="listener"/> to receive events of type <typeparamref name="T"/>. Duplicate registrations are deduplicated.</summary>
		void Subscribe<T>(Action<T> listener) where T : IEvent;

		/// <summary>Removes <paramref name="listener"/> from event type <typeparamref name="T"/>. No-op if not subscribed.</summary>
		void Unsubscribe<T>(Action<T> listener) where T : IEvent;

		/// <summary>Synchronously dispatches <paramref name="evt"/> to every subscriber of type <typeparamref name="T"/>. Silent no-op if no subscribers.</summary>
		void Publish<T>(T evt) where T : IEvent;

		/// <summary>Removes every subscription on every type.</summary>
		void Clear();

		/// <summary>Removes every subscription on type <typeparamref name="T"/> only.</summary>
		void Clear<T>() where T : IEvent;

		/// <summary>Total subscriber count summed across all event types.</summary>
		int ListenerCount { get; }
	}
}
