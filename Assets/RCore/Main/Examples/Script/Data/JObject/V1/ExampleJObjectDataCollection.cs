using System.Collections;
using System.Collections.Generic;
using RCore.Data.JObject;
using UnityEngine;

namespace RCore.Example.Data.JObject
{
	[CreateAssetMenu(fileName = "ExampleJObjectsCollection", menuName = "RCore/ExampleJObjectsCollection")]
	public class ExampleJObjectDataCollection : JObjectDataCollection
	{
		public InventoryModel inventoryModel;
		public InventoryRpgModel inventoryRpgModel;
		public AchievementModel achievementModel;
		public DailyRewardModel dailyRewardModel;
		
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
			(inventory, inventoryHandler) = CreateModel<InventoryData<InvItemData>, InventoryHandler, ExampleJObjectDataCollection>("Inventory");

			// Example of a rpg inventory module
			inventoryRpg = CreateJObjectData<InventoryRpgData<InvRPGItemData>>("InventoryRPG");
			inventoryRpgHandler = CreateJObjectHandler<InventoryRPGHandler, ExampleJObjectDataCollection>();

			// Example of Achievement module
			achievement = CreateJObjectData<AchievementData>("Achievement");
			achievementHandler = CreateJObjectHandler<AchievementHandler, ExampleJObjectDataCollection>();

			// Example of Daily reward module
			(dailyReward, dailyRewardHandler) = CreateModel<DailyRewardData, DailyRewardHandler, ExampleJObjectDataCollection>("DailyReward");
		}
	}
}