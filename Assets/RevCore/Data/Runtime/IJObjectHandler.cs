namespace RevCore
{
    /// <summary>
    /// Lifecycle callbacks invoked by <see cref="JObjectModelCollection"/> on every model it
    /// owns. <see cref="JObjectModel{T}"/> implements this via <see cref="IJObjectModel"/>;
    /// you usually don't implement this directly.
    /// </summary>
    public interface IJObjectHandler
    {
        /// <summary>Called when the application pauses or resumes. <paramref name="offlineSeconds"/> is the elapsed time across the pause.</summary>
        void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds);

        /// <summary>Called once after all models have loaded — use for cross-model references that <see cref="JObjectData.Load()"/> cannot resolve in isolation.</summary>
        void OnPostLoad(int utcNowTimestamp, int offlineSeconds);

        /// <summary>Called every frame from <see cref="JObjectDBManager{T}.Update"/>.</summary>
        void OnUpdate(float deltaTime);

        /// <summary>Called immediately before <see cref="Save"/> — write computed/cached fields back into <see cref="JObjectData"/> here.</summary>
        void OnPreSave(int utcNowTimestamp);

        /// <summary>Called after remote config has been re-fetched; reapply any tunables.</summary>
        void OnRemoteConfigFetched();

        /// <summary>Persists the model's data to its backend.</summary>
        void Save();
    }
}
