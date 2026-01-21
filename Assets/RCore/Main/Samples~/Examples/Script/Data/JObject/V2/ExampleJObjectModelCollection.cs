using System.Collections;
using System.Collections.Generic;
using RCore.Data.JObject;
using UnityEngine;

namespace RCore.Example.Data.JObject
{
	public class ExampleJObjectModelCollection : JObjectModelCollection
	{
		public AchievementModel achievement;
		public DailyRewardModel dailyReward;
		public InventoryModel inventory;
		public InventoryRpgModel inventoryRPG;

		public override void Load()
		{
			base.Load();
			
			CreateModel(achievement, "Achievement");
			CreateModel(dailyReward, "DailyReward");
			CreateModel(inventory, "Inventory");
			CreateModel(inventoryRPG, "InventoryRPG");
		}
	}
}