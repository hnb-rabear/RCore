using System.Collections;
using System.Collections.Generic;
using RCore.Data.JObject;
using UnityEngine;

namespace RCore.Example.Data.JObject
{
	[CreateAssetMenu(fileName = "ExampleJObjectsCollection", menuName = "RCore/ExampleJObjectsCollection")]
	public class ExampleJObjectsCollection : JObjectsCollection
	{
		public InventoryData<InvItemData> inventory;
		public InventoryHandler inventoryHandler;

		public InventoryRpgData<InvRPGItemData> inventoryRpg;
		public InventoryRPGHandler inventoryRpgHandler;

		public AchievementData achievement;
		public AchievementHandler achievementHandler;

		public DailyRewardData dailyReward;
		public DailyRewardHandler dailyRewardHandler;

		public override void Load()
		{
			base.Load();

			// Example of basic inventory module
			(inventory, inventoryHandler) = CreateModule<InventoryData<InvItemData>, InventoryHandler, ExampleJObjectsCollection>("Inventory");

			// Example of a rpg inventory module
			inventoryRpg = CreateCollection<InventoryRpgData<InvRPGItemData>>("InventoryRPG");
			inventoryRpgHandler = CreateController<InventoryRPGHandler, ExampleJObjectsCollection>();

			// Example of Achievement module
			achievement = CreateCollection<AchievementData>("Achievement");
			achievementHandler = CreateController<AchievementHandler, ExampleJObjectsCollection>();

			// Example of Daily reward module
			(dailyReward, dailyRewardHandler) = CreateModule<DailyRewardData, DailyRewardHandler, ExampleJObjectsCollection>("DailyReward");
		}
	}
}