using System;
using System.Collections.Generic;

namespace RevCore
{
    /// <summary>
    /// Default in-memory <see cref="IServiceLocator"/> implementation. Type-keyed by the generic
    /// parameter at registration time — register interface types to enable swap-in mocking.
    /// </summary>
    public sealed class ServiceLocator : IServiceLocator
    {
        private readonly Dictionary<Type, object> m_services = new();

        /// <inheritdoc />
        public int Count => m_services.Count;

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="service"/> is <c>null</c>.</exception>
        public void Register<T>(T service) where T : class
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            m_services[typeof(T)] = service;
        }

        /// <inheritdoc />
        public bool TryGet<T>(out T service) where T : class
        {
            if (m_services.TryGetValue(typeof(T), out object value))
            {
                service = value as T;
                return service != null;
            }

            service = null;
            return false;
        }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">No service of type <typeparamref name="T"/> is registered.</exception>
        public T Get<T>() where T : class
        {
            if (TryGet(out T service))
                return service;

            throw new InvalidOperationException($"Service '{typeof(T).FullName}' is not registered.");
        }

        /// <inheritdoc />
        public bool Contains<T>() where T : class
            => m_services.ContainsKey(typeof(T));

        /// <inheritdoc />
        public bool Unregister<T>() where T : class
            => m_services.Remove(typeof(T));

        /// <inheritdoc />
        public void Clear()
            => m_services.Clear();
    }

    /// <summary>
    /// Static facade over a process-global <see cref="ServiceLocator"/>. Mirrors <see cref="IServiceLocator"/>
    /// for convenience; consumers who care about testability should resolve <see cref="IServiceLocator"/>
    /// through dependency injection and skip this facade.
    /// </summary>
    public static class Services
    {
        private static readonly ServiceLocator s_global = new();

        /// <summary>The global service registry.</summary>
        public static IServiceLocator Global => s_global;

        /// <summary>Registers <paramref name="service"/> on the global registry. See <see cref="IServiceLocator.Register{T}"/>.</summary>
        public static void Register<T>(T service) where T : class
            => s_global.Register(service);

        /// <summary>Tries to retrieve a service from the global registry.</summary>
        public static bool TryGet<T>(out T service) where T : class
            => s_global.TryGet(out service);

        /// <summary>Retrieves a required service from the global registry. Throws if not registered.</summary>
        public static T Get<T>() where T : class
            => s_global.Get<T>();

        /// <summary>Returns <c>true</c> if <typeparamref name="T"/> is registered on the global registry.</summary>
        public static bool Contains<T>() where T : class
            => s_global.Contains<T>();

        /// <summary>Removes the registration for <typeparamref name="T"/> from the global registry.</summary>
        public static bool Unregister<T>() where T : class
            => s_global.Unregister<T>();

        /// <summary>Removes every registration from the global registry.</summary>
        public static void Clear()
            => s_global.Clear();
    }
}
