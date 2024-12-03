namespace RCore.Data.JObject
{
	public interface IJObjectController
	{
		public void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds);
		public void OnPostLoad(int utcNowTimestamp, int offlineSeconds);
		public void OnUpdate(float deltaTime);
		public void OnPreSave(int utcNowTimestamp);
		public void Save();
	}
}