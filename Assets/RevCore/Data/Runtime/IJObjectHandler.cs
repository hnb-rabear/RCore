namespace RevCore
{
    public interface IJObjectHandler
    {
        void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds);
        void OnPostLoad(int utcNowTimestamp, int offlineSeconds);
        void OnUpdate(float deltaTime);
        void OnPreSave(int utcNowTimestamp);
        void OnRemoteConfigFetched();
        void Save();
    }
}
