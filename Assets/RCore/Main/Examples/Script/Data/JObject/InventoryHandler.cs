using RCore.Data.JObject;

namespace RCore.Example.Data.JObject
{
	public class InventoryHandler : JObjectHandler<ExampleJObjectsCollection>
	{
		public override void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds)
		{
		}
		public override void OnPostLoad(int utcNowTimestamp, int offlineSeconds)
		{
			collection.inventory.Insert(new InvItemData()
			{
				fk = 2,
				id = 2
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