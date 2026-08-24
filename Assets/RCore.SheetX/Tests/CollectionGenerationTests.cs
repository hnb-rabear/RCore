using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RCore.SheetX.Editor;
using UnityEditor;
using UnityEngine;

namespace RCore.SheetX.Tests
{
	/// <summary>Stands in for a row type a consuming project owns, for Existing Model coverage.</summary>
	[Serializable]
	public sealed class CollectionGenerationExistingRow
	{
		public int id;
		public string name;
	}

	public class CollectionGenerationTests
	{
		[Test]
		public void generator_emits_data_only_partial_source()
		{
			var settings = Settings();
			try
			{
				var source = SheetXCollectionGenerator.Emit(settings, new[]
				{
					Generated("excel-b", "ShopItems", "Shop", "shopItems",
						"id:int", "tags[]:string", "reward.amount:int"),
					Existing("google-a", "Missions", "Shop", "missions", "MissionData"),
					Generated("excel-a", "GameSettings", "Global", "gameSettings", "key:string"),
				});

				Assert.That(source, Does.Contain("public partial class ShopItemsRow"));
				Assert.That(source, Does.Contain("public int id;"));
				Assert.That(source, Does.Contain("public string[] tags;"));
				Assert.That(source, Does.Contain("public Reward reward;"));
				Assert.That(source, Does.Contain("public partial class ShopConfigCollection : SheetXConfigCollectionBase"));
				Assert.That(source, Does.Contain("public ShopItemsRow[] shopItems;"));
				Assert.That(source, Does.Contain("public MissionData[] missions;"));
				Assert.That(source, Does.Contain("public partial class GlobalConfigCollection : GlobalConfigCollectionBase"));
				Assert.That(source, Does.Contain("public ShopConfigCollection shop;"));
				Assert.That(source, Does.Contain(
					"internal const string ShopItems = \"Assets/Game/DataConfig/Editor/Json/ShopItems.txt\";"));

				Assert.That(source, Does.Contain("using RCore.SheetX;"));
				Assert.That(source, Does.Not.Contain("UnityEditor"));
				Assert.That(source, Does.Not.Contain("Newtonsoft"));
				Assert.That(source, Does.Not.Contain("LoadData"));
				Assert.That(source, Does.Not.Contain("Dictionary"));
				Assert.That(source, Does.Not.Contain("get;"));
				Assert.That(source, Does.Not.Contain("set;"));
				Assert.That(source.Replace("\r\n", ""), Does.Not.Contain("\n"));
			}
			finally
			{
				ScriptableObject.DestroyImmediate(settings);
			}
		}

		[Test]
		public void generator_stamps_each_table_with_the_json_path_it_emitted()
		{
			var settings = Settings();
			try
			{
				var table = Existing("a", "Shop Items", "Shop", "shopItems", "ShopItemsRow");

				SheetXCollectionGenerator.Emit(settings, new[] { table });

				Assert.That(table.JsonPath, Is.EqualTo("Assets/Game/DataConfig/Editor/Json/Shop_Items.txt"));
			}
			finally
			{
				ScriptableObject.DestroyImmediate(settings);
			}
		}

		[Test]
		public void generator_orders_global_then_collections_and_tables_ordinally()
		{
			var settings = Settings();
			try
			{
				string source = SheetXCollectionGenerator.Emit(settings, new[]
				{
					Existing("z", "Zulu", "Shop", "zulu", "ZuluRow"),
					Existing("b", "Beta", "Global", "beta", "BetaRow"),
					Existing("a", "Alpha", "Global", "alpha", "AlphaRow"),
					Existing("a", "AlphaFeature", "Audio", "alphaFeature", "AlphaFeatureRow"),
				});

				Assert.That(IndexOf(source, "class GlobalConfigCollection"),
					Is.LessThan(IndexOf(source, "class AudioConfigCollection")));
				Assert.That(IndexOf(source, "class AudioConfigCollection"),
					Is.LessThan(IndexOf(source, "class ShopConfigCollection")));
				Assert.That(IndexOf(source, "public AlphaRow[] alpha;"),
					Is.LessThan(IndexOf(source, "public BetaRow[] beta;")));
			}
			finally
			{
				ScriptableObject.DestroyImmediate(settings);
			}
		}

		[Test]
		public void generator_emits_empty_defined_collection_and_global_reference()
		{
			var settings = Settings();
			try
			{
				settings.collections.Add(new SheetXCollectionDefinition { name = "Shop" });

				string source = SheetXCollectionGenerator.Emit(settings,
					new[] { Existing("a", "Items", "Global", "items", "ItemRow") });

				Assert.That(source, Does.Contain("class ShopConfigCollection : SheetXConfigCollectionBase"));
				Assert.That(source, Does.Contain("public ShopConfigCollection shop;"));
			}
			finally
			{
				ScriptableObject.DestroyImmediate(settings);
			}
		}

		[Test]
		public void generator_rejects_row_type_collision_after_sheet_normalization()
		{
			var settings = Settings();
			try
			{
				Assert.That(
					() => SheetXCollectionGenerator.Emit(settings, new[]
					{
						Generated("a", "Shop Items", "Global", "shopItemsA", "id:int"),
						Generated("b", "ShopItems", "Shop", "shopItemsB", "id:int"),
					}),
					Throws.TypeOf<InvalidOperationException>().With.Message.Contains("ShopItemsRow"));
			}
			finally
			{
				ScriptableObject.DestroyImmediate(settings);
			}
		}

		[Test]
		public void generator_rejects_duplicate_field_and_path_constant_names()
		{
			var settings = Settings();
			try
			{
				Assert.That(
					() => SheetXCollectionGenerator.Emit(settings, new[]
					{
						Existing("a", "One", "Shop", "items", "OneRow"),
						Existing("b", "Two", "Shop", "items", "TwoRow"),
					}),
					Throws.TypeOf<InvalidOperationException>().With.Message.Contains("declares field 'items' twice"));

				Assert.That(
					() => SheetXCollectionGenerator.Emit(settings, new[]
					{
						Existing("a", "Shop Items", "Global", "itemsA", "OneRow"),
						Existing("b", "ShopItems", "Shop", "itemsB", "TwoRow"),
					}),
					Throws.TypeOf<InvalidOperationException>().With.Message.Contains("path constant 'ShopItems'"));
			}
			finally
			{
				ScriptableObject.DestroyImmediate(settings);
			}
		}

		[Test]
		public void generator_rejects_global_table_field_that_shadows_a_feature_reference()
		{
			var settings = Settings();
			try
			{
				Assert.That(
					() => SheetXCollectionGenerator.Emit(settings, new[]
					{
						Existing("a", "Shop", "Global", "shop", "ShopRow"),
						Existing("b", "ShopItems", "Shop", "shopItems", "ShopItemsRow"),
					}),
					Throws.TypeOf<InvalidOperationException>().With.Message
						.Contains("Global table field 'shop' collides with a feature collection reference"));
			}
			finally
			{
				ScriptableObject.DestroyImmediate(settings);
			}
		}

		[Test]
		public void generator_rejects_generated_type_named_after_the_path_constants_class()
		{
			var settings = Settings();
			try
			{
				Assert.That(
					() => SheetXCollectionGenerator.Emit(settings, new[]
					{
						Generated("a", "Items", "Global", "items", "sheetXCollectionPaths.id:int"),
					}),
					Throws.TypeOf<InvalidOperationException>().With.Message.Contains("SheetXCollectionPaths"));
			}
			finally
			{
				ScriptableObject.DestroyImmediate(settings);
			}
		}

		[Test]
		public void json_only_binding_produces_no_generated_table_declaration()
		{
			var settings = Settings();
			try
			{
				var tables = new List<SheetXCollectionGeneratedTable>();
				var binding = new SheetXSheetBinding
				{
					outputMode = SheetXSheetOutputMode.JsonOnly,
					sheetName = "RawData",
				};
				if (binding.outputMode != SheetXSheetOutputMode.JsonOnly)
					tables.Add(Existing("source", binding.sheetName, "Global", "rawData", "RawDataRow"));

				string source = SheetXCollectionGenerator.Emit(settings, tables);

				Assert.That(source, Does.Not.Contain("RawData"));
			}
			finally
			{
				ScriptableObject.DestroyImmediate(settings);
			}
		}

		[Test]
		public void google_collection_table_reader_preserves_headers_and_empty_cells()
		{
			IList<IList<object>> values = new List<IList<object>>
			{
				new List<object> { "id:int", "name:string", "enabled:bool" },
				new List<object> { "1", "hero" },
				new List<object> { "2", null, "true" },
			};

			GoogleSheetHandler.ReadCollectionTable(values, out var headers, out var rows);

			Assert.That(headers, Is.EqualTo(new[] { "id:int", "name:string", "enabled:bool" }));
			Assert.That(rows, Has.Count.EqualTo(2));
			Assert.That(rows[0], Is.EqualTo(new[] { "1", "hero", "" }));
			Assert.That(rows[1], Is.EqualTo(new[] { "2", "", "true" }));
		}

		[Test]
		public void session_writes_json_and_source_only_on_flush()
		{
			var settings = SessionSettings();
			try
			{
				Bind(settings, "GenTable", SheetXSheetOutputMode.CollectionGeneratedModel);
				Bind(settings, "ExistingTable", SheetXSheetOutputMode.CollectionExistingModel,
					typeof(CollectionGenerationExistingRow).AssemblyQualifiedName);

				var session = new SheetXCollectionExportSession(settings);
				Assert.That(
					session.TryAddGeneratedTable(
						SourceId, "GenTable", new[] { "id:int", "name:string" },
						new IReadOnlyList<string>[] { new[] { "1", "hero" } }, out string error),
					Is.True, error);
				Assert.That(
					session.TryAddExistingTable(
						SourceId, "ExistingTable", "[{\"id\":2,\"name\":\"potion\"}]", out error),
					Is.True, error);

				// Nothing may exist yet: a later sheet can still fail and must not leave partial output.
				Assert.That(File.Exists(GeneratedJson), Is.False);
				Assert.That(File.Exists(ExistingJson), Is.False);
				Assert.That(File.Exists(GeneratedSource), Is.False);

				Assert.That(session.Flush(out error), Is.True, error);

				Assert.That(session.WroteArtifacts, Is.True);
				Assert.That(File.ReadAllText(GeneratedJson), Is.EqualTo("[{\"id\":1,\"name\":\"hero\"}]"));
				Assert.That(File.ReadAllText(ExistingJson), Is.EqualTo("[{\"id\":2,\"name\":\"potion\"}]"));
				string source = File.ReadAllText(GeneratedSource);
				Assert.That(source, Does.Contain("public GenTableRow[] GenTable;"));
				Assert.That(source, Does.Contain(
					"public RCore.SheetX.Tests.CollectionGenerationExistingRow[] ExistingTable;"));
			}
			finally
			{
				Cleanup(settings);
			}
		}

		[Test]
		public void partial_collection_export_from_another_source_does_not_replace_shared_generated_source()
		{
			var settings = SessionSettings();
			try
			{
				Bind(settings, "GenTable", SheetXSheetOutputMode.CollectionGeneratedModel);
				Bind(settings, "other-book.xlsx", "OtherTable", SheetXSheetOutputMode.CollectionGeneratedModel);

				Directory.CreateDirectory(CodeFolder);
				File.WriteAllText(GeneratedSource, "// prior source");

				var session = new SheetXCollectionExportSession(settings);
				Assert.That(session.TryAddGeneratedTable(
					SourceId, "GenTable", new[] { "id:int" },
					new IReadOnlyList<string>[] { new[] { "1" } }, out string error), Is.True, error);

				Assert.That(session.Flush(out error), Is.False);
				Assert.That(error, Does.Contain("OtherTable"));
				Assert.That(File.Exists(GeneratedJson), Is.False);
				Assert.That(File.ReadAllText(GeneratedSource), Is.EqualTo("// prior source"));
			}
			finally
			{
				Cleanup(settings);
			}
		}

		[Test]
		public void existing_only_session_does_not_require_script_reload()
		{
			var settings = SessionSettings();
			try
			{
				Bind(settings, "ExistingTable", SheetXSheetOutputMode.CollectionExistingModel,
					typeof(CollectionGenerationExistingRow).AssemblyQualifiedName);
				Directory.CreateDirectory(CodeFolder);
				File.WriteAllText(GeneratedSource, SheetXCollectionGenerator.Emit(settings,
					new[] { Existing(SourceId, "ExistingTable", "Global", "ExistingTable",
						typeof(CollectionGenerationExistingRow).FullName) }));

				var session = new SheetXCollectionExportSession(settings);
				Assert.That(session.TryAddExistingTable(
					SourceId, "ExistingTable", "[{\"id\":2,\"name\":\"potion\"}]", out string error), Is.True, error);
				Assert.That(session.Flush(out error), Is.True, error);

				Assert.That(session.RequiresScriptReload, Is.False);
			}
			finally
			{
				Cleanup(settings);
			}
		}

		[Test]
		public void session_ignores_a_json_only_sheet()
		{
			var settings = SessionSettings();
			try
			{
				Bind(settings, "RawData", SheetXSheetOutputMode.JsonOnly);

				var session = new SheetXCollectionExportSession(settings);

				// False with no error: the sheet is not a collection sheet, which is not a failure.
				Assert.That(
					session.TryAddGeneratedTable(
						SourceId, "RawData", new[] { "id:int" },
						new IReadOnlyList<string>[] { new[] { "1" } }, out string error),
					Is.False);
				Assert.That(error, Is.Null);
				Assert.That(session.Flush(out error), Is.True, error);
				Assert.That(session.WroteArtifacts, Is.False);
				Assert.That(Directory.Exists(JsonFolder), Is.False);
				Assert.That(File.Exists(GeneratedSource), Is.False);
			}
			finally
			{
				Cleanup(settings);
			}
		}

		[Test]
		public void empty_collection_session_reports_invalid_settings()
		{
			var settings = SessionSettings();
			try
			{
				settings.collectionNamespace = "";

				var session = new SheetXCollectionExportSession(settings);

				Assert.That(session.Flush(out string error), Is.False);
				Assert.That(error, Does.Contain("Collection namespace is empty"));
			}
			finally
			{
				Cleanup(settings);
			}
		}

		[Test]
		public void one_bad_schema_discards_the_whole_session()
		{
			var settings = SessionSettings();
			try
			{
				Bind(settings, "GenTable", SheetXSheetOutputMode.CollectionGeneratedModel);
				Bind(settings, "Broken", SheetXSheetOutputMode.CollectionGeneratedModel);

				var session = new SheetXCollectionExportSession(settings);
				Assert.That(
					session.TryAddGeneratedTable(
						SourceId, "GenTable", new[] { "id:int", "name:string" },
						new IReadOnlyList<string>[] { new[] { "1", "hero" } }, out string error),
					Is.True, error);
				Assert.That(
					session.TryAddGeneratedTable(
						SourceId, "Broken", new[] { "id" },
						new IReadOnlyList<string>[] { new[] { "1" } }, out error),
					Is.False);
				Assert.That(error, Does.Contain("Broken"));

				Assert.That(session.Flush(out error), Is.False);
				Assert.That(session.WroteArtifacts, Is.False);
				Assert.That(File.Exists(GeneratedJson), Is.False);
				Assert.That(File.Exists(GeneratedSource), Is.False);
			}
			finally
			{
				Cleanup(settings);
			}
		}

		private const string TempFolder = "Assets/SheetXTestsTemp";
		private const string CodeFolder = TempFolder + "/Code";
		private const string JsonFolder = TempFolder + "/Editor/Json";
		private const string SourceId = "book.xlsx";
		private const string GeneratedJson = JsonFolder + "/GenTable.txt";
		private const string ExistingJson = JsonFolder + "/ExistingTable.txt";
		private const string CodeFile = "/" + SheetXCollectionGenerator.FileName;
		private const string GeneratedSource = CodeFolder + CodeFile;

		private static SheetXSettings SessionSettings()
		{
			var settings = ScriptableObject.CreateInstance<SheetXSettings>();
			settings.ResetToDefault();
			settings.enableCollections = true;
			settings.collectionNamespace = "Game.DataConfig";
			settings.collectionCodeFolder = CodeFolder;
			settings.collectionAssetFolder = TempFolder + "/Collections";
			settings.collectionJsonFolder = JsonFolder;
			settings.globalResourcesFolder = TempFolder + "/Resources";
			return settings;
		}

		private static void Bind(
			SheetXSettings settings, string sheetName, SheetXSheetOutputMode mode, string rowTypeName = null)
			=> Bind(settings, SourceId, sheetName, mode, rowTypeName);

		private static void Bind(
			SheetXSettings settings, string sourceId, string sheetName,
			SheetXSheetOutputMode mode, string rowTypeName = null)
		{
			var binding = SheetXCollectionSettings.GetOrCreateBinding(settings, sourceId, sheetName);
			binding.outputMode = mode;
			binding.rowTypeName = rowTypeName;
		}

		private static void Cleanup(SheetXSettings settings)
		{
			ScriptableObject.DestroyImmediate(settings);
			// Only this test's own folder: the session writes real files through SheetXFileOutput.
			if (Directory.Exists(TempFolder))
				Directory.Delete(TempFolder, true);
			if (File.Exists(TempFolder + ".meta"))
				File.Delete(TempFolder + ".meta");
			AssetDatabase.Refresh();
		}

		private static SheetXSettings Settings()
		{
			var settings = ScriptableObject.CreateInstance<SheetXSettings>();
			settings.ResetToDefault();
			settings.collectionNamespace = "Game.DataConfig";
			settings.collectionJsonFolder = "Assets/Game/DataConfig/Editor/Json";
			return settings;
		}

		private static SheetXCollectionGeneratedTable Generated(
			string sourceId, string sheetName, string collectionName, string fieldName, params string[] headers)
		{
			bool ok = SheetXCollectionSchemaParser.TryParse(
				headers,
				SheetXCollectionNaming.RowTypeName(sheetName),
				out var schema,
				out var error);
			Assert.That(ok, Is.True, error);
			return new SheetXCollectionGeneratedTable
			{
				SourceId = sourceId,
				SheetName = sheetName,
				CollectionName = collectionName,
				FieldName = fieldName,
				Schema = schema,
			};
		}

		private static SheetXCollectionGeneratedTable Existing(
			string sourceId, string sheetName, string collectionName, string fieldName, string rowTypeName)
		{
			return new SheetXCollectionGeneratedTable
			{
				SourceId = sourceId,
				SheetName = sheetName,
				CollectionName = collectionName,
				FieldName = fieldName,
				ExistingRowTypeName = rowTypeName,
			};
		}

		private static int IndexOf(string source, string value)
		{
			int index = source.IndexOf(value, StringComparison.Ordinal);
			Assert.That(index, Is.GreaterThanOrEqualTo(0), value);
			return index;
		}
	}
}
