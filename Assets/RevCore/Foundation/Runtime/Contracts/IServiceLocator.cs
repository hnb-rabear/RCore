namespace RevCore
{
    public interface IServiceLocator
    {
        int Count { get; }
        void Register<T>(T service) where T : class;
        bool TryGet<T>(out T service) where T : class;
        T Get<T>() where T : class;
        bool Contains<T>() where T : class;
        bool Unregister<T>() where T : class;
        void Clear();
    }
}
