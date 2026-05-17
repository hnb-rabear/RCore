namespace RevCore
{
	/// <summary>
	/// Type-keyed registry of singletons. RevCore uses this to swap in custom implementations of
	/// <see cref="IEventBus"/>, <see cref="IRevLogger"/>, audio managers, and other services without
	/// reaching for static state. The default implementation is <see cref="ServiceLocator"/>.
	/// </summary>
	public interface IServiceLocator
	{
		/// <summary>Number of registered services.</summary>
		int Count { get; }

		/// <summary>Registers <paramref name="service"/> under its concrete type <typeparamref name="T"/>. Replaces any previous registration of the same key.</summary>
		void Register<T>(T service) where T : class;

		/// <summary>Tries to retrieve a previously registered service. Returns <c>true</c> when found.</summary>
		bool TryGet<T>(out T service) where T : class;

		/// <summary>Retrieves a previously registered service. Throws if not registered — use <see cref="TryGet{T}"/> when the service is optional.</summary>
		T Get<T>() where T : class;

		/// <summary>Returns <c>true</c> if a service of type <typeparamref name="T"/> is registered.</summary>
		bool Contains<T>() where T : class;

		/// <summary>Removes the registration for <typeparamref name="T"/>. Returns <c>true</c> if a registration was removed.</summary>
		bool Unregister<T>() where T : class;

		/// <summary>Removes every registration.</summary>
		void Clear();
	}
}
