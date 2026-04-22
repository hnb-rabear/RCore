using RCore.Data.JObject;
using UnityEngine;

namespace RCore.Example.Data.JObject
{
	/// <summary>
	/// Demonstrates [Inject] with a concrete model type.
	/// AchievementModel injects InventoryModel to track inventory-based achievements
	/// without requiring a direct ScriptableObject reference in the Inspector.
	/// </summary>
	public class AchievementModel : JObjectModel<AchievementData>
	{
		[Inject] private ISessionModel m_session;
		[Inject] private InventoryModel m_inventory;

		public override void Init() { }

		public override void OnPostLoad(int utcNowTimestamp, int offlineSeconds)
		{
			// Use injected models for achievement evaluation
			if (m_inventory != null)
				Debug.Log($"[Achievement] Inventory has {m_inventory.data.Count} items");
			if (m_session != null)
				Debug.Log($"[Achievement] Total sessions: {m_session.SessionsTotal}");
		}

		public override void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds) { }
		public override void OnUpdate(float deltaTime) { }
		public override void OnPreSave(int utcNowTimestamp) { }
		public override void OnRemoteConfigFetched() { }
	}
}