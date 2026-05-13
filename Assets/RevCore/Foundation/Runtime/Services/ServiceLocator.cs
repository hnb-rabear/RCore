using System;
using System.Collections.Generic;

namespace RevCore
{
    public sealed class ServiceLocator : IServiceLocator
    {
        private readonly Dictionary<Type, object> m_services = new();

        public int Count => m_services.Count;

        public void Register<T>(T service) where T : class
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            m_services[typeof(T)] = service;
        }

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

        public T Get<T>() where T : class
        {
            if (TryGet(out T service))
                return service;

            throw new InvalidOperationException($"Service '{typeof(T).FullName}' is not registered.");
        }

        public bool Contains<T>() where T : class
            => m_services.ContainsKey(typeof(T));

        public bool Unregister<T>() where T : class
            => m_services.Remove(typeof(T));

        public void Clear()
            => m_services.Clear();
    }

    public static class Services
    {
        private static readonly ServiceLocator s_global = new();

        public static IServiceLocator Global => s_global;

        public static void Register<T>(T service) where T : class
            => s_global.Register(service);

        public static bool TryGet<T>(out T service) where T : class
            => s_global.TryGet(out service);

        public static T Get<T>() where T : class
            => s_global.Get<T>();

        public static bool Contains<T>() where T : class
            => s_global.Contains<T>();

        public static bool Unregister<T>() where T : class
            => s_global.Unregister<T>();

        public static void Clear()
            => s_global.Clear();
    }
}
