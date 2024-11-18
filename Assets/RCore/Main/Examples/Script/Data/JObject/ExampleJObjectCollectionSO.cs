using System.Collections;
using System.Collections.Generic;
using RCore.Data.JObject;
using UnityEngine;

namespace RCore.Example.Data.JObject
{
	[CreateAssetMenu(fileName = "ExampleJObjectCollectionSO", menuName = "RCore/ExampleJObjectCollectionSO")]
	public class ExampleJObjectCollectionSO : JObjectCollectionSO
	{
		public InventoryCollection<InvItemData> inventory;
		public InventoryHandler inventoryHandler;

		public InventoryRPGCollection<InvRPGItemData> inventoryRpg;
		public InventoryRPGHandler inventoryRpgHandler;

		public AchievementCollection achievement;
		public AchievementHandler achievementHandler;

		public DailyRewardCollection dailyReward;
		public DailyRewardHandler dailyRewardHandler;

		public override void Load()
		{
			base.Load();

			// Example of basic inventory module
			(inventory, inventoryHandler) = CreateModule<InventoryCollection<InvItemData>, InventoryHandler, ExampleJObjectCollectionSO>("Inventory");

			// Example of a rpg inventory module
			inventoryRpg = CreateCollection<InventoryRPGCollection<InvRPGItemData>>("InventoryRPG");
			inventoryRpgHandler = CreateController<InventoryRPGHandler, ExampleJObjectCollectionSO>();

			// Example of Achievement module
			achievement = CreateCollection<AchievementCollection>("Achievement");
			achievementHandler = CreateController<AchievementHandler, ExampleJObjectCollectionSO>();

			// Example of Daily reward module
			(dailyReward, dailyRewardHandler) = CreateModule<DailyRewardCollection, DailyRewardHandler, ExampleJObjectCollectionSO>("DailyReward");
		}
	}
}