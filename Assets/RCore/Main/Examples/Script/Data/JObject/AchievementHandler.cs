using RCore.Data.JObject;

namespace RCore.Example.Data.JObject
{
	public class AchievementHandler : JObjectHandler<ExampleJObjectDBManager>
	{
		public override void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds)
		{
		}
		public override void OnPostLoad(int utcNowTimestamp, int offlineSeconds)
		{
		}
		public override void OnUpdate(float deltaTime)
		{
		}
		public override void OnPreSave(int utcNowTimestamp)
		{
		}
	}
}