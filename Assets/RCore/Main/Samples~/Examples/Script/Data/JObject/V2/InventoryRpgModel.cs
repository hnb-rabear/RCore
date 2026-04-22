using RCore.Data.JObject;
using UnityEngine;

namespace RCore.Example.Data.JObject
{
	/// <summary>
	/// Demonstrates [Inject] to access a sibling model.
	/// InventoryRpgModel injects the base InventoryModel to share item ID allocation
	/// or cross-reference basic inventory data.
	/// </summary>
	public class InventoryRpgModel : JObjectModel<InventoryRpgData<InvRPGItemData>>
	{
		[Inject] private InventoryModel m_baseInventory;

		public override void Init() { }

		public override void OnPostLoad(int utcNowTimestamp, int offlineSeconds)
		{
			if (m_baseInventory != null)
				Debug.Log($"[InventoryRPG] Base inventory has {m_baseInventory.data.Count} items");
		}

		public override void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds) { }
		public override void OnUpdate(float deltaTime) { }
		public override void OnPreSave(int utcNowTimestamp) { }
		public override void OnRemoteConfigFetched() { }
	}
}