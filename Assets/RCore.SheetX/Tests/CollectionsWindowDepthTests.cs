using NUnit.Framework;
using RCore.SheetX.Editor;
using UnityEngine;

namespace RCore.SheetX.Tests
{
	public class CollectionsWindowDepthTests
	{
		[Test]
		public void inline_collection_with_global_auto_load_on_shows_on_and_locked()
		{
			var settings = Settings(globalAutoLoad: true);
			var collection = settings.collections[1];

			Assert.That(SheetXCollectionsWindow.ShowsAutoLoadAsOn(settings, collection), Is.True);
			Assert.That(SheetXCollectionsWindow.AllowsAutoLoadEdit(collection), Is.False);
			Assert.That(collection.autoLoad, Is.False);
		}

		[Test]
		public void inline_collection_with_global_auto_load_off_shows_off_without_mutating_stored_value()
		{
			var settings = Settings(globalAutoLoad: false);
			var collection = settings.collections[1];

			Assert.That(SheetXCollectionsWindow.ShowsAutoLoadAsOn(settings, collection), Is.False);
			Assert.That(collection.autoLoad, Is.False);
		}

		[Test]
		public void separate_asset_collection_shows_and_edits_its_own_auto_load()
		{
			var settings = Settings(globalAutoLoad: true);
			var collection = settings.collections[1];
			collection.depth = SheetXCollectionDepth.SeparateAsset;

			Assert.That(SheetXCollectionsWindow.ShowsAutoLoadAsOn(settings, collection), Is.False);
			Assert.That(SheetXCollectionsWindow.AllowsAutoLoadEdit(collection), Is.True);

			collection.autoLoad = true;
			Assert.That(SheetXCollectionsWindow.ShowsAutoLoadAsOn(settings, collection), Is.True);
		}

		private static SheetXSettings Settings(bool globalAutoLoad)
		{
			var settings = ScriptableObject.CreateInstance<SheetXSettings>();
			settings.ResetToDefault();
			settings.collections[0].autoLoad = globalAutoLoad;
			settings.collections.Add(new SheetXCollectionDefinition
			{
				name = "Player",
				depth = SheetXCollectionDepth.Inline,
				autoLoad = false,
			});
			return settings;
		}
	}
}
