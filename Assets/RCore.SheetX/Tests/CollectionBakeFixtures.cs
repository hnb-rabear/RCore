using System;

namespace RCore.SheetX.Tests
{
	[Serializable, SheetXBindable]
	public sealed class BakeItemsRow
	{
		public int id;
		public string name;
	}

	[Serializable, SheetXBindable]
	public struct BakeStatsRow
	{
		public int level;
		public float multiplier;
	}
}
