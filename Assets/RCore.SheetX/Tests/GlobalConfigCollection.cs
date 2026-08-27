using System;

namespace RCore.SheetX.Tests
{
	public sealed class GlobalConfigCollection : GlobalConfigCollectionBase
	{
		[Serializable]
		public sealed class Economy
		{
			public int startingCoins;
		}

		public BakeShopConfigCollection bakeShop;
		public string environment;
		public Economy economy;
	}

	public static partial class SheetXCollectionPaths
	{
		internal const string Configuration = "Assets/SheetXTestsTemp/Editor/Json/Configuration.txt";
	}
}
