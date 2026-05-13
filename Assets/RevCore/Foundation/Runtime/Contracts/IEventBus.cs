using System;

namespace RevCore
{
	public interface IEventBus
	{
		void Subscribe<T>(Action<T> listener) where T : IEvent;
		void Unsubscribe<T>(Action<T> listener) where T : IEvent;
		void Publish<T>(T evt) where T : IEvent;
		void Clear();
		void Clear<T>() where T : IEvent;
		int ListenerCount { get; }
	}
}
