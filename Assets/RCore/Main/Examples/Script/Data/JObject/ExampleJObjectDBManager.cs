using RCore.Data.JObject;
using System;

namespace RCore.Example.Data.JObject
{
	public class ExampleJObjectDBManager : JObjectDBManager
	{
		public InventoryCollection<InvItemData> inventory;
		public InventoryHandler inventoryHandler;
		
		public InventoryRPGCollection<InvRPGItemData> inventoryRpg;
		public InventoryRPGHandler inventoryRpgHandler;

		public AchievementCollection achievement;
		public AchievementHandler achievementHandler;

		public DailyRewardCollection dailyReward;
		public DailyRewardHandler dailyRewardHandler;

		private void Start()
		{
			Init();
		}

		protected override void Load()
		{
			// Example of basic inventory module
			(inventory, inventoryHandler) = CreateModule<InventoryCollection<InvItemData>, InventoryHandler, ExampleJObjectDBManager>("Inventory");
			
			// Example of a rpg inventory module
			inventoryRpg = CreateCollection<InventoryRPGCollection<InvRPGItemData>>("InventoryRPG");
			inventoryRpgHandler = CreateController<InventoryRPGHandler, ExampleJObjectDBManager>();

			// Example of Achievement module
			achievement = CreateCollection<AchievementCollection>("Achievement");
			achievementHandler = CreateController<AchievementHandler, ExampleJObjectDBManager>();
			
			// Example of Daily reward module
			(dailyReward, dailyRewardHandler) = CreateModule<DailyRewardCollection, DailyRewardHandler, ExampleJObjectDBManager>("DailyReward");
		}
	}
}