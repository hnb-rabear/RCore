using RCore.Data.JObject;

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
			// Example of basic inventory collection
			inventory = CreateCollection<InventoryCollection<InvItemData>>("Inventory");
			inventoryHandler = CreateController<InventoryHandler, ExampleJObjectDBManager>();
			
			// Example of a rpg inventory collection
			inventoryRpg = CreateCollection<InventoryRPGCollection<InvRPGItemData>>("InventoryRPG");
			inventoryRpgHandler = CreateController<InventoryRPGHandler, ExampleJObjectDBManager>();

			// Example of Achievement collection
			achievement = CreateCollection<AchievementCollection>("Achievement");
			achievementHandler = CreateController<AchievementHandler, ExampleJObjectDBManager>();
			
			// Example of DailyReward collection
			dailyReward = CreateCollection<DailyRewardCollection>("DailyReward");
			dailyRewardHandler = CreateController<DailyRewardHandler, ExampleJObjectDBManager>();
		}
	}
}