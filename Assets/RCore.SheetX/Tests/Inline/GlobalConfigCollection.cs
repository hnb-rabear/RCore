namespace RCore.SheetX.Tests.Inline
{
	/// <summary>Stands in for a generated Global that holds one inline group and one asset reference.</summary>
	public sealed class GlobalConfigCollection : GlobalConfigCollectionBase
	{
		public PlayerConfigCollection player;
		public BakeShopConfigCollection bakeShop;
		public string environment;
	}

	public static partial class SheetXCollectionPaths
	{
		internal const string Configuration = "Assets/SheetXTestsTemp/Editor/Json/Configuration.txt";
	}
}
