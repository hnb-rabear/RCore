using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RCore.SheetX.Editor;
using UnityEngine;

namespace RCore.SheetX.Tests
{
	public class CollectionSettingsTests
	{
		private static SheetXSettings CreateSettings()
		{
			var settings = ScriptableObject.CreateInstance<SheetXSettings>();
			settings.ResetToDefault();
			return settings;
		}

		private static void SetValidPaths(SheetXSettings settings)
		{
			settings.collectionNamespace = "Game.Config";
			settings.collectionCodeFolder = "Assets/Game/Generated";
			settings.collectionAssetFolder = "Assets/Game/Collections";
			settings.collectionJsonFolder = "Assets/Game/Editor/CollectionJson";
			settings.globalResourcesFolder = "Assets/Game/Resources";
		}

		[Test]
		public void defaults_disable_collections_and_seed_global()
		{
			var settings = CreateSettings();
			try
			{
				Assert.That(settings.enableCollections, Is.False);
				Assert.That(settings.collections.Single().name, Is.EqualTo("Global"));
				Assert.That(settings.collections.Single().builtInGlobal, Is.True);
				Assert.That(settings.autoLoadAfterExport, Is.True);
				Assert.That(settings.autoLoadBeforePlay, Is.True);
			}
			finally
			{
				Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void ensure_global_repairs_missing_collection_and_dangling_bindings()
		{
			var settings = CreateSettings();
			try
			{
				settings.collections = new List<SheetXCollectionDefinition>
				{
					new SheetXCollectionDefinition { name = "Shop", builtInGlobal = true },
				};
				settings.sheetBindings = new List<SheetXSheetBinding>
				{
					new SheetXSheetBinding { collectionName = "" },
					new SheetXSheetBinding { collectionName = "Gone" },
				};

				SheetXCollectionSettings.EnsureGlobal(settings);

				Assert.That(settings.collections.Count(c => c.name == "Global"), Is.EqualTo(1));
				Assert.That(settings.collections.Single(c => c.name == "Global").builtInGlobal, Is.True);
				Assert.That(settings.collections.Single(c => c.name == "Shop").builtInGlobal, Is.False);
				Assert.That(settings.sheetBindings.All(b => b.collectionName == "Global"), Is.True);
			}
			finally
			{
				Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void rename_moves_bound_sheets()
		{
			var settings = CreateSettings();
			try
			{
				settings.collections.Add(new SheetXCollectionDefinition { name = "Shop" });
				settings.sheetBindings.Add(new SheetXSheetBinding { collectionName = "Shop" });

				Assert.That(SheetXCollectionSettings.RenameCollection(settings, "Shop", "Store", out _), Is.True);
				Assert.That(settings.sheetBindings.Single().collectionName, Is.EqualTo("Store"));
			}
			finally
			{
				Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void rename_rejects_global_keywords_invalid_and_duplicate_names()
		{
			var settings = CreateSettings();
			try
			{
				settings.collections.Add(new SheetXCollectionDefinition { name = "Shop" });
				settings.collections.Add(new SheetXCollectionDefinition { name = "Store" });

				Assert.That(SheetXCollectionSettings.RenameCollection(settings, "Global", "Root", out _), Is.False);
				Assert.That(SheetXCollectionSettings.RenameCollection(settings, "Shop", "class", out _), Is.False);
				Assert.That(SheetXCollectionSettings.RenameCollection(settings, "Shop", "1Store", out _), Is.False);
				Assert.That(SheetXCollectionSettings.RenameCollection(settings, "Shop", "Store", out _), Is.False);
			}
			finally
			{
				Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void delete_moves_bound_sheets_to_global()
		{
			var settings = CreateSettings();
			try
			{
				settings.collections.Add(new SheetXCollectionDefinition { name = "Shop" });
				settings.sheetBindings.Add(new SheetXSheetBinding { collectionName = "Shop" });

				Assert.That(SheetXCollectionSettings.DeleteCollection(settings, "Shop", out _), Is.True);
				Assert.That(settings.sheetBindings.Single().collectionName, Is.EqualTo("Global"));
				Assert.That(settings.collections.Any(c => c.name == "Shop"), Is.False);
			}
			finally
			{
				Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void delete_rejects_global()
		{
			var settings = CreateSettings();
			try
			{
				Assert.That(SheetXCollectionSettings.DeleteCollection(settings, "Global", out _), Is.False);
				Assert.That(settings.collections.Single(c => c.name == "Global"), Is.Not.Null);
			}
			finally
			{
				Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void binding_identity_is_ordinal_source_and_sheet_pair()
		{
			var settings = CreateSettings();
			try
			{
				var first = SheetXCollectionSettings.GetOrCreateBinding(settings, "Book", "Items");
				var second = SheetXCollectionSettings.GetOrCreateBinding(settings, "Book", "Items");
				var caseDifferent = SheetXCollectionSettings.GetOrCreateBinding(settings, "book", "Items");

				Assert.That(first, Is.SameAs(second));
				Assert.That(caseDifferent, Is.Not.SameAs(first));
				Assert.That(settings.sheetBindings.Count, Is.EqualTo(2));
				Assert.That(first.outputMode, Is.EqualTo(SheetXSheetOutputMode.JsonOnly));
				Assert.That(first.collectionName, Is.EqualTo("Global"));
			}
			finally
			{
				Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void validation_is_silent_when_disabled_or_correctly_configured()
		{
			var settings = CreateSettings();
			try
			{
				SetValidPaths(settings);
				// Off by default: nothing to report even before paths are filled in.
				Assert.That(SheetXCollectionSettings.Validate(settings, null), Is.Empty);

				settings.enableCollections = true;
				Assert.That(SheetXCollectionSettings.Validate(settings, null), Is.Empty);
			}
			finally
			{
				Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void validation_reports_resources_json_and_overlap_problems()
		{
			var settings = CreateSettings();
			try
			{
				settings.enableCollections = true;
				SetValidPaths(settings);
				settings.globalResourcesFolder = "Assets/Game/Resources/Generated";
				settings.collectionJsonFolder = "Assets/Game/ConfigJson";
				settings.collectionCodeFolder = "Assets/Game/Generated";
				settings.collectionAssetFolder = "Assets/Game/Generated/Assets";

				var issues = SheetXCollectionSettings.Validate(settings, null);

				Assert.That(issues.Any(i => i.Message.Contains("must end with a 'Resources' folder")), Is.True);
				Assert.That(issues.Any(i => i.Message.Contains("must sit under an 'Editor' folder")), Is.True);
				Assert.That(issues.Any(i => i.Message.Contains("must not contain each other")), Is.True);
			}
			finally
			{
				Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void validation_reports_orphan_binding_and_duplicate_collection_field()
		{
			var settings = CreateSettings();
			try
			{
				settings.enableCollections = true;
				SetValidPaths(settings);
				settings.collections.Add(new SheetXCollectionDefinition { name = "Shop" });
				settings.sheetBindings.Add(new SheetXSheetBinding
				{
					sourceId = "Book",
					sheetName = "Items",
					collectionName = "Shop",
					outputMode = SheetXSheetOutputMode.CollectionGeneratedModel,
					fieldName = "Rows",
				});
				var orphan = new SheetXSheetBinding
				{
					sourceId = "External",
					sheetName = "Items",
					collectionName = "Shop",
					outputMode = SheetXSheetOutputMode.CollectionGeneratedModel,
					fieldName = "Rows",
				};

				var issues = SheetXCollectionSettings.Validate(settings, new[] { orphan });

				Assert.That(issues.Any(i => i.Message.Contains("not saved in the settings asset")), Is.True);
				Assert.That(issues.Any(i => i.Message.Contains("both write collection field 'Rows'")), Is.True);
			}
			finally
			{
				Object.DestroyImmediate(settings);
			}
		}
	}
}
