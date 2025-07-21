namespace RCore.Data.JObject
{
	public interface IJObjectHandler
	{
		public void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds);
		public void OnPostLoad(int utcNowTimestamp, int offlineSeconds);
		public void OnUpdate(float deltaTime);
		public void OnPreSave(int utcNowTimestamp);
		public abstract void OnRemoteConfigFetched();
		public void Save();
	}
}