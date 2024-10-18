using RCore.Data.JObject;

namespace RCore.Example.Data.JObject
{
	public class InventoryHandler : JObjectHandler<ExampleJObjectDBManager>
	{
		public override void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds)
		{
		}
		public override void OnPostLoad(int utcNowTimestamp, int offlineSeconds)
		{
			manager.inventory.Insert(new InvItemData()
			{
				fk = 1,
				id = 1
			});
		}
		public override void OnUpdate(float deltaTime)
		{
		}
		public override void OnPreSave(int utcNowTimestamp)
		{
		}
	}
}