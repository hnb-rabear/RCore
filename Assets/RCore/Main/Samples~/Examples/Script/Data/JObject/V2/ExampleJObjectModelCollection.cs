using RCore.Data.JObject;
using RCore.Inspector;
using UnityEngine;

namespace RCore.Example.Data.JObject
{
	/// <summary>
	/// Example collection demonstrating the [Inject] dependency injection pattern.
	/// ScriptableObject references are still required for Unity serialization and CreateModel,
	/// but cross-model dependencies (e.g. AchievementModel → InventoryModel) are resolved
	/// automatically via [Inject] attributes during PostLoad — no manual wiring needed.
	/// </summary>
	public class ExampleJObjectModelCollection : JObjectModelCollection
	{
		[CreateScriptableObject, AutoFill] public AchievementModel achievement;
		[CreateScriptableObject, AutoFill] public DailyRewardModel dailyReward;
		[CreateScriptableObject, AutoFill] public InventoryModel inventory;
		[CreateScriptableObject, AutoFill] public InventoryRpgModel inventoryRPG;

		public override void Load()
		{
			base.Load();
			
			// Register all models — order matters for [Inject] resolution:
			// models registered first are available for injection into later models.
			CreateModel(inventory, "Inventory");
			CreateModel(inventoryRPG, "InventoryRPG");
			CreateModel(achievement, "Achievement");
			CreateModel(dailyReward, "DailyReward");
			
			// After all models are registered, InjectDependencies() is called
			// automatically in PostLoad(), resolving all [Inject] fields:
			//   - AchievementModel.m_session    → ISessionModel (from base class)
			//   - AchievementModel.m_inventory   → InventoryModel
			//   - DailyRewardModel.m_session     → ISessionModel
			//   - InventoryRpgModel.m_baseInventory → InventoryModel
		}
	}
}