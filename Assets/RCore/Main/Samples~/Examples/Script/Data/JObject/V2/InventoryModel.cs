using RCore.Data.JObject;

namespace RCore.Example.Data.JObject
{
	/// <summary>
	/// A simple model with no cross-model dependencies.
	/// Other models can inject this via [Inject] InventoryModel.
	/// </summary>
	public class InventoryModel : JObjectModel<InventoryData<InvItemData>>
	{
		public override void Init() { }
		public override void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds) { }
		public override void OnPostLoad(int utcNowTimestamp, int offlineSeconds) { }
		public override void OnUpdate(float deltaTime) { }
		public override void OnPreSave(int utcNowTimestamp) { }
		public override void OnRemoteConfigFetched() { }
	}
}