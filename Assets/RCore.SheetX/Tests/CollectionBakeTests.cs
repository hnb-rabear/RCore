using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RCore.SheetX.Editor;
using UnityEditor;
using UnityEngine;

namespace RCore.SheetX.Tests
{
	public class CollectionBakeTests
	{
		private const string TestRoot = "Assets/SheetXTestsTemp";
		private const string TempRoot = TestRoot + "/Editor";
		private const string JsonFolder = TempRoot + "/Json";
		private const string AssetFolder = TempRoot + "/Collections";
		private const string ResourcesFolder = TempRoot + "/Resources";
		private const string FeaturePath = AssetFolder + "/BakeShopConfigCollection.asset";
		private const string GlobalPath = ResourcesFolder + "/GlobalConfigCollection.asset";
		private const string JsonPath = JsonFolder + "/BakeItems.txt";
		private const string StatsJsonPath = JsonFolder + "/BakeStats.txt";
		private const string InlineGlobalPath = ResourcesFolder + "/GlobalConfigCollection.asset";
		private const string InlineJsonPath = JsonFolder + "/InlineCharacters.txt";

		private class TestGlobalCollection : GlobalConfigCollectionBase
		{
		}

		private class TestPlainCollection : SheetXConfigCollectionBase
		{
		}

		[SetUp]
		public void SetUp()
		{
			AssetDatabase.DeleteAsset(TempRoot);
			EnsureFolder(TestRoot);
			EnsureFolder(TempRoot);
			EnsureFolder(JsonFolder);
			EnsureFolder(AssetFolder);
			EnsureFolder(ResourcesFolder);
			File.WriteAllText(JsonFolder + "/Configuration.txt", "{}");
			AssetDatabase.ImportAsset(JsonFolder + "/Configuration.txt");
		}

		[TearDown]
		public void TearDown()
		{
			SheetXCollectionBaker.TestBeforeSave = null;
			AssetDatabase.DeleteAsset(TempRoot);
		}

		[OneTimeTearDown]
		public void OneTimeTearDown()
		{
			AssetDatabase.DeleteAsset(TestRoot);
		}

		[Test]
		public void global_override_returns_injected_instance()
		{
			var root = ScriptableObject.CreateInstance<TestGlobalCollection>();
			try
			{
				GlobalConfigCollectionBase.SetInstance(root);
				Assert.That(GlobalConfigCollectionBase.Instance<TestGlobalCollection>(), Is.SameAs(root));
			}
			finally
			{
				GlobalConfigCollectionBase.SetInstance<TestGlobalCollection>(null);
				Assert.That(
					GlobalConfigCollectionBase.Instance<TestGlobalCollection>(),
					Is.Not.SameAs(root));
				UnityEngine.Object.DestroyImmediate(root);
			}
		}

		[Test]
		public void loaded_state_lifecycle_transitions_correctly()
		{
			var collection = ScriptableObject.CreateInstance<TestPlainCollection>();
			try
			{
				Assert.That(collection.IsLoaded, Is.False);
				collection.SetLoaded();
				Assert.That(collection.IsLoaded, Is.True);
				collection.ResetLoaded();
				Assert.That(collection.IsLoaded, Is.False);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(collection);
			}
		}

		[Test]
		public void load_data_for_collection_bakes_typed_rows_without_text_asset_reference()
		{
			File.WriteAllText(JsonPath, "[{\"id\":1,\"name\":\"hero\"}]");
			var settings = CreateSettings();
			try
			{
				Assert.That(SheetXCollectionBaker.TryLoadData(settings, "BakeShop", out string error), Is.True, error);

				var feature = AssetDatabase.LoadAssetAtPath<BakeShopConfigCollection>(FeaturePath);
				Assert.That(feature, Is.Not.Null);
				Assert.That(feature.items, Has.Length.EqualTo(1));
				Assert.That(feature.items[0].id, Is.EqualTo(1));
				Assert.That(feature.items[0].name, Is.EqualTo("hero"));
				Assert.That(feature.IsLoaded, Is.True);
				Assert.That(AssetDatabase.GetAssetPath(feature), Is.EqualTo(FeaturePath));
				Assert.That(ContainsTextAssetReference(feature), Is.False);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void load_data_bakes_struct_rows_into_the_collection()
		{
			File.WriteAllText(JsonPath, "[]");
			File.WriteAllText(StatsJsonPath, "[{\"level\":3,\"multiplier\":1.5}]");
			var settings = CreateSettings();
			settings.sheetBindings.Add(new SheetXSheetBinding
			{
				sourceId = "book.xlsx",
				sheetName = "BakeStats",
				outputMode = SheetXSheetOutputMode.ExistingDataClass,
				collectionName = "BakeShop",
				rowTypeName = typeof(BakeStatsRow).AssemblyQualifiedName,
				fieldName = "stats",
			});
			try
			{
				Assert.That(SheetXCollectionBaker.TryLoadData(settings, "BakeShop", out string error), Is.True, error);

				var feature = AssetDatabase.LoadAssetAtPath<BakeShopConfigCollection>(FeaturePath);
				Assert.That(feature, Is.Not.Null);
				Assert.That(feature.stats, Has.Length.EqualTo(1));
				Assert.That(feature.stats[0].level, Is.EqualTo(3));
				Assert.That(feature.stats[0].multiplier, Is.EqualTo(1.5f).Within(0.0001f));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void load_data_rejects_a_row_type_without_the_bindable_marker()
		{
			// TryFindRowType runs before the JSON is read, so valid JSON proves the marker is what rejects.
			File.WriteAllText(JsonPath, "[]");
			var settings = CreateSettings();
			settings.sheetBindings.Single(binding => binding.sheetName == "BakeItems").rowTypeName
				= typeof(UnmarkedBakeRow).AssemblyQualifiedName;
			try
			{
				Assert.That(SheetXCollectionBaker.TryLoadData(settings, "BakeShop", out string error), Is.False);
				Assert.That(error, Does.Contain("SheetXBindable"));
				Assert.That(error, Does.Contain("UnmarkedBakeRow"));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void auto_load_skips_disabled_collection_rows_but_refreshes_global_reference()
		{
			var feature = CreateFeatureAsset(7, "old");
			File.WriteAllText(JsonPath, "[{\"id\":9,\"name\":\"new\"}]");
			var settings = CreateSettings();
			settings.collections.Single(collection => collection.name == "BakeShop").autoLoad = false;
			try
			{
				Assert.That(SheetXCollectionBaker.TryLoadData(settings, true, out string error), Is.True, error);

				var global = AssetDatabase.LoadAssetAtPath<GlobalConfigCollection>(GlobalPath);
				Assert.That(feature.items[0].id, Is.EqualTo(7));
				Assert.That(global.bakeShop, Is.SameAs(feature));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void try_load_data_assigns_feature_asset_to_global_root()
		{
			File.WriteAllText(JsonPath, "[{\"id\":2,\"name\":\"potion\"}]");
			var settings = CreateSettings();
			try
			{
				Assert.That(SheetXCollectionBaker.TryLoadData(settings, false, out string error), Is.True, error);

				var feature = AssetDatabase.LoadAssetAtPath<BakeShopConfigCollection>(FeaturePath);
				var global = AssetDatabase.LoadAssetAtPath<GlobalConfigCollection>(GlobalPath);
				Assert.That(global, Is.Not.Null);
				Assert.That(global.bakeShop, Is.SameAs(feature));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void pending_bake_without_auto_load_refreshes_global_without_marking_it_loaded()
		{
			var feature = CreateFeatureAsset(7, "old");
			File.WriteAllText(JsonPath, "[{\"id\":9,\"name\":\"new\"}]");
			var settings = CreateSettings();
			try
			{
				Assert.That(SheetXCollectionBaker.TryFinishPendingBake(
					settings, autoLoadAfterExport: false, out string error), Is.True, error);

				var global = AssetDatabase.LoadAssetAtPath<GlobalConfigCollection>(GlobalPath);
				Assert.That(feature.items[0].id, Is.EqualTo(7));
				Assert.That(feature.items[0].name, Is.EqualTo("old"));
				Assert.That(global.bakeShop, Is.SameAs(feature));
				Assert.That(global.IsLoaded, Is.False);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}


		[Test]
		public void export_bake_reads_only_accepted_bindings()
		{
			File.WriteAllText(JsonPath, "[{\"id\":1,\"name\":\"hero\"}]");
			File.WriteAllText(JsonFolder + "/Broken.txt", "{stale");
			var settings = CreateSettings();
			settings.sheetBindings.Add(new SheetXSheetBinding
			{
				sourceId = "book.xlsx",
				sheetName = "Broken",
				outputMode = SheetXSheetOutputMode.GeneratedDataClass,
				collectionName = "BakeShop",
				fieldName = "broken",
			});
			var accepted = new[]
			{
				new PendingCollectionBakeBinding { SourceId = "book.xlsx", SheetName = "BakeItems" },
			};
			try
			{
				Assert.That(SheetXCollectionBaker.TryLoadData(
					settings, autoLoadOnly: false, accepted, out string error), Is.True, error);

				var feature = AssetDatabase.LoadAssetAtPath<BakeShopConfigCollection>(FeaturePath);
				Assert.That(feature.items, Has.Length.EqualTo(1));
				Assert.That(feature.items[0].id, Is.EqualTo(1));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void pending_bake_store_round_trips_accepted_binding_identity()
		{
			var store = new PendingCollectionBakeStore
			{
				Entries =
				{
					new PendingCollectionBakeEntry
					{
						SettingsAssetPath = "Assets/Settings.asset",
						AutoLoadAfterExport = true,
						AcceptedBindings =
						{
							new PendingCollectionBakeBinding
							{
								SourceId = "book.xlsx",
								SheetName = "Items",
							},
						},
					},
				},
			};

			var restored = JsonUtility.FromJson<PendingCollectionBakeStore>(JsonUtility.ToJson(store));

			Assert.That(restored.Entries[0].AcceptedBindings, Has.Count.EqualTo(1));
			Assert.That(restored.Entries[0].AcceptedBindings[0].SourceId, Is.EqualTo("book.xlsx"));
			Assert.That(restored.Entries[0].AcceptedBindings[0].SheetName, Is.EqualTo("Items"));
		}

		[Test]
		public void invalid_json_leaves_existing_assets_unchanged()
		{
			var feature = CreateFeatureAsset(7, "old");
			File.WriteAllText(JsonPath, "{broken");
			var settings = CreateSettings();
			try
			{
				Assert.That(SheetXCollectionBaker.TryLoadData(settings, false, out string error), Is.False);
				Assert.That(error, Does.Contain("BakeItems"));
				Assert.That(feature.items, Has.Length.EqualTo(1));
				Assert.That(feature.items[0].id, Is.EqualTo(7));
				Assert.That(feature.items[0].name, Is.EqualTo("old"));
				Assert.That(AssetDatabase.LoadAssetAtPath<GlobalConfigCollection>(GlobalPath), Is.Null);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void configuration_object_populates_root_and_nested_global_fields()
		{
			File.WriteAllText(JsonPath, "[{\"id\":1,\"name\":\"hero\"}]");
			File.WriteAllText(JsonFolder + "/Configuration.txt",
				"{\"environment\":\"production\",\"economy\":{\"startingCoins\":42}}");
			var settings = CreateSettings();
			try
			{
				Assert.That(SheetXCollectionBaker.TryLoadData(settings, false, out string error), Is.True, error);

				var global = AssetDatabase.LoadAssetAtPath<GlobalConfigCollection>(GlobalPath);
				Assert.That(global.environment, Is.EqualTo("production"));
				Assert.That(global.economy.startingCoins, Is.EqualTo(42));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void configuration_json_root_must_be_object()
		{
			File.WriteAllText(JsonPath, "[{\"id\":1,\"name\":\"hero\"}]");
			File.WriteAllText(JsonFolder + "/Configuration.txt", "[]");
			var settings = CreateSettings();
			try
			{
				Assert.That(SheetXCollectionBaker.TryLoadData(settings, false, out string error), Is.False);
				Assert.That(error, Does.Contain("Configuration JSON root must be an object."));
				Assert.That(AssetDatabase.LoadAssetAtPath<GlobalConfigCollection>(GlobalPath), Is.Null);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void missing_configuration_json_aborts_before_asset_mutation()
		{
			File.Delete(JsonFolder + "/Configuration.txt");
			var feature = CreateFeatureAsset(7, "old");
			var settings = CreateSettings();
			try
			{
				Assert.That(SheetXCollectionBaker.TryLoadData(settings, false, out string error), Is.False);
				Assert.That(error, Does.Contain("Configuration JSON was not found."));
				Assert.That(feature.items[0].id, Is.EqualTo(7));
				Assert.That(AssetDatabase.LoadAssetAtPath<GlobalConfigCollection>(GlobalPath), Is.Null);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void stale_configuration_json_is_ignored_without_compiled_marker()
		{
			File.WriteAllText(JsonFolder + "/Configuration.txt", "{\"environment\":\"stale\"}");
			var settings = CreateMarkerAbsentSettings();
			try
			{
				Assert.That(SheetXCollectionBaker.TryLoadData(settings, false, out string error), Is.True, error);

				var global = AssetDatabase.LoadAssetAtPath<MarkerAbsent.GlobalConfigCollection>(GlobalPath);
				Assert.That(global, Is.Not.Null);
				Assert.That(global.environment, Is.Null);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void configuration_only_bake_preserves_table_arrays_and_feature_references()
		{
			var feature = CreateFeatureAsset(7, "old");
			var global = CreateGlobalAsset("old", feature);
			File.WriteAllText(JsonFolder + "/Configuration.txt", "{\"environment\":\"production\"}");
			var settings = CreateSettings();
			try
			{
				Assert.That(SheetXCollectionBaker.TryLoadData(
					settings, false, Array.Empty<PendingCollectionBakeBinding>(), out string error), Is.True, error);

				Assert.That(feature.items[0].id, Is.EqualTo(7));
				Assert.That(feature.items[0].name, Is.EqualTo("old"));
				Assert.That(global.environment, Is.EqualTo("production"));
				Assert.That(global.bakeShop, Is.SameAs(feature));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void configuration_failure_rolls_back_global_and_feature_assets()
		{
			var feature = CreateFeatureAsset(7, "old");
			var global = CreateGlobalAsset("old", feature);
			File.WriteAllText(JsonPath, "[{\"id\":9,\"name\":\"new\"}]");
			File.WriteAllText(JsonFolder + "/Configuration.txt", "{\"environment\":\"new\"}");
			string featureSnapshot = EditorJsonUtility.ToJson(feature);
			string globalSnapshot = EditorJsonUtility.ToJson(global);
			SheetXCollectionBaker.TestBeforeSave = path =>
			{
				if (string.Equals(path, FeaturePath, StringComparison.Ordinal))
					throw new IOException("Injected feature write failure");
			};
			var settings = CreateSettings();
			try
			{
				Assert.That(SheetXCollectionBaker.TryLoadData(settings, false, out string error), Is.False);
				Assert.That(error, Does.Contain("Injected feature write failure"));
				Assert.That(EditorJsonUtility.ToJson(feature), Is.EqualTo(featureSnapshot));
				Assert.That(EditorJsonUtility.ToJson(global), Is.EqualTo(globalSnapshot));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void load_data_repairs_missing_collection_scripts_without_recreating_assets()
		{
			var feature = CreateFeatureAsset(7, "old");
			var global = CreateGlobalAsset("old", null);
			File.WriteAllText(JsonPath, "[{\"id\":9,\"name\":\"new\"}]");
			var settings = CreateSettings();
			try
			{
				var featureScript = MonoScript.FromScriptableObject(feature);
				var globalScript = MonoScript.FromScriptableObject(global);
				Assert.That(featureScript, Is.Not.Null);
				Assert.That(globalScript, Is.Not.Null);
				Assert.That(ScriptOf(feature), Is.SameAs(featureScript));
				Assert.That(ScriptOf(global), Is.SameAs(globalScript));

				Assert.That(SheetXCollectionBaker.TryLoadData(settings, false, out string error), Is.True, error);

				var reloadedFeature = AssetDatabase.LoadAssetAtPath<BakeShopConfigCollection>(FeaturePath);
				var reloadedGlobal = AssetDatabase.LoadAssetAtPath<GlobalConfigCollection>(GlobalPath);
				Assert.That(reloadedFeature, Is.SameAs(feature));
				Assert.That(reloadedGlobal, Is.SameAs(global));
				Assert.That(ScriptOf(reloadedFeature), Is.SameAs(featureScript));
				Assert.That(ScriptOf(reloadedGlobal), Is.SameAs(globalScript));
				Assert.That(reloadedFeature.items, Has.Length.EqualTo(1));
				Assert.That(reloadedFeature.items[0].id, Is.EqualTo(9));
				Assert.That(reloadedFeature.items[0].name, Is.EqualTo("new"));
				Assert.That(reloadedGlobal.bakeShop, Is.SameAs(reloadedFeature));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void unknown_existing_asset_is_not_overwritten_when_collection_cannot_load()
		{
			var sentinel = new TextAsset("keep me");
			AssetDatabase.CreateAsset(sentinel, FeaturePath);
			File.WriteAllText(JsonPath, "[{\"id\":9,\"name\":\"new\"}]");
			var settings = CreateSettings();
			try
			{
				Assert.That(SheetXCollectionBaker.TryLoadData(settings, false, out string error), Is.False);
				Assert.That(error, Does.Contain(FeaturePath));
				Assert.That(error, Does.Contain("will not overwrite"));
				Assert.That(AssetDatabase.LoadAssetAtPath<TextAsset>(FeaturePath), Is.SameAs(sentinel));
				Assert.That(sentinel.text, Is.EqualTo("keep me"));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void write_failure_restores_existing_assets_and_deletes_created_assets()
		{
			var feature = CreateFeatureAsset(7, "old");
			File.WriteAllText(JsonPath, "[{\"id\":9,\"name\":\"new\"}]");
			SheetXCollectionBaker.TestBeforeSave = path =>
			{
				if (string.Equals(path, GlobalPath, StringComparison.Ordinal))
					throw new IOException("Injected write failure");
			};
			var settings = CreateSettings();
			try
			{
				Assert.That(SheetXCollectionBaker.TryLoadData(settings, false, out string error), Is.False);
				Assert.That(error, Does.Contain("Injected write failure"));
				Assert.That(feature.items, Has.Length.EqualTo(1));
				Assert.That(feature.items[0].id, Is.EqualTo(7));
				Assert.That(feature.items[0].name, Is.EqualTo("old"));
				Assert.That(AssetDatabase.LoadAssetAtPath<GlobalConfigCollection>(GlobalPath), Is.Null);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void explicit_empty_accepted_bindings_round_trip_as_empty()
		{
			var settings = CreateSettings();
			AssetDatabase.CreateAsset(settings, "Assets/SheetXTestsTemp/SheetXSettings.asset");
			try
			{
				SheetXCollectionBaker.RegisterPendingBake(
					settings, true, Array.Empty<PendingCollectionBakeBinding>());
				string raw = SessionState.GetString(SheetXCollectionBaker.PendingKey, "");
				var store = JsonUtility.FromJson<PendingCollectionBakeStore>(raw);
				var entry = store.Entries.Single();
				Assert.That(entry.HasAcceptedBindingFilter, Is.True);
				Assert.That(entry.AcceptedBindings, Is.Empty);
			}
			finally
			{
				SessionState.EraseString(SheetXCollectionBaker.PendingKey);
				AssetDatabase.DeleteAsset("Assets/SheetXTestsTemp/SheetXSettings.asset");
			}
		}

		[Test]
		public void legacy_pending_entry_without_filter_still_means_all_saved_bindings()
		{
			var legacyStore = new PendingCollectionBakeStore
			{
				Entries = new System.Collections.Generic.List<PendingCollectionBakeEntry>
				{
					new PendingCollectionBakeEntry
					{
						SettingsAssetPath = "Assets/Settings.asset",
						AutoLoadAfterExport = true,
						HasAcceptedBindingFilter = false,
						AcceptedBindings = new System.Collections.Generic.List<PendingCollectionBakeBinding>(),
					}
				}
			};
			string json = JsonUtility.ToJson(legacyStore);
			var deserialized = JsonUtility.FromJson<PendingCollectionBakeStore>(json);
			var entry = deserialized.Entries.Single();
			Assert.That(entry.HasAcceptedBindingFilter, Is.False);
		}

		[Test]
		public void inline_depth_bakes_rows_into_global_and_creates_no_feature_asset()
		{
			File.WriteAllText(InlineJsonPath, "[{\"id\":7,\"name\":\"mage\"}]");
			var settings = CreateInlineSettings();
			try
			{
				Assert.That(SheetXCollectionBaker.TryLoadData(settings, "Player", out string error), Is.True, error);

				var global = AssetDatabase.LoadAssetAtPath<Inline.GlobalConfigCollection>(InlineGlobalPath);
				Assert.That(global, Is.Not.Null);
				Assert.That(global.player, Is.Not.Null);
				Assert.That(global.player.Characters, Has.Length.EqualTo(1));
				Assert.That(global.player.Characters[0].id, Is.EqualTo(7));
				Assert.That(global.player.Characters[0].name, Is.EqualTo("mage"));

				// An inline group has no asset of its own. Creating one would defeat the whole point.
				Assert.That(
					AssetDatabase.LoadMainAssetAtPath(AssetFolder + "/PlayerConfigCollection.asset"),
					Is.Null);
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void inline_bake_leaves_sibling_global_data_untouched()
		{
			File.WriteAllText(InlineJsonPath, "[{\"id\":7,\"name\":\"mage\"}]");
			var settings = CreateInlineSettings();
			try
			{
				EnsureFolder(ResourcesFolder);
				var seeded = ScriptableObject.CreateInstance<Inline.GlobalConfigCollection>();
				seeded.environment = "staging";
				AssetDatabase.CreateAsset(seeded, InlineGlobalPath);
				AssetDatabase.SaveAssets();

				Assert.That(SheetXCollectionBaker.TryLoadData(settings, "Player", out string error), Is.True, error);

				var global = AssetDatabase.LoadAssetAtPath<Inline.GlobalConfigCollection>(InlineGlobalPath);
				// PopulateObject writes only the keys present in the JSON, so a sibling group that
				// this bake did not touch must survive it verbatim.
				Assert.That(global.environment, Is.EqualTo("staging"));
				Assert.That(global.player.Characters, Has.Length.EqualTo(1));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void inline_bake_builds_the_group_when_global_is_created_fresh()
		{
			File.WriteAllText(InlineJsonPath, "[{\"id\":1,\"name\":\"rogue\"}]");
			var settings = CreateInlineSettings();
			try
			{
				// No Global asset exists yet, so `player` starts null and Newtonsoft's default
				// ObjectCreationHandling.Auto has to construct it rather than reuse one.
				Assert.That(AssetDatabase.LoadMainAssetAtPath(InlineGlobalPath), Is.Null);

				Assert.That(SheetXCollectionBaker.TryLoadData(settings, "Player", out string error), Is.True, error);

				var global = AssetDatabase.LoadAssetAtPath<Inline.GlobalConfigCollection>(InlineGlobalPath);
				Assert.That(global.player.Characters[0].name, Is.EqualTo("rogue"));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void mixed_depths_put_a_nested_group_and_an_asset_reference_on_one_global()
		{
			File.WriteAllText(InlineJsonPath, "[{\"id\":7,\"name\":\"mage\"}]");
			File.WriteAllText(JsonPath, "[{\"id\":2,\"name\":\"potion\"}]");
			var settings = CreateInlineSettings();
			try
			{
				settings.collections.Add(new SheetXCollectionDefinition
				{
					name = "BakeShop",
					autoLoad = true,
					depth = SheetXCollectionDepth.SeparateAsset,
				});
				settings.sheetBindings.Add(new SheetXSheetBinding
				{
					sourceId = "book.xlsx",
					sheetName = "BakeItems",
					outputMode = SheetXSheetOutputMode.ExistingDataClass,
					collectionName = "BakeShop",
					rowTypeName = typeof(BakeItemsRow).AssemblyQualifiedName,
					fieldName = "items",
				});

				Assert.That(SheetXCollectionBaker.TryLoadData(settings, autoLoadOnly: false, out string error),
					Is.True, error);

				var global = AssetDatabase.LoadAssetAtPath<Inline.GlobalConfigCollection>(InlineGlobalPath);
				// This is the feature as it will actually be used: one of each on the same root.
				Assert.That(global.player.Characters[0].id, Is.EqualTo(7));
				Assert.That(global.bakeShop, Is.Not.Null);
				Assert.That(global.bakeShop.items[0].id, Is.EqualTo(2));
				Assert.That(AssetDatabase.GetAssetPath(global.bakeShop), Is.EqualTo(FeaturePath));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void inline_group_bakes_under_auto_load_even_when_its_own_flag_is_false()
		{
			File.WriteAllText(InlineJsonPath, "[{\"id\":9,\"name\":\"ranger\"}]");
			var settings = CreateInlineSettings();
			try
			{
				// A collection switched to Inline keeps whatever autoLoad it had. Honouring that flag
				// would skip its rows while Global is saved anyway, persisting a stale group.
				settings.collections.First(c => c.name == "Player").autoLoad = false;
				settings.collections.First(c => c.builtInGlobal).autoLoad = true;

				Assert.That(SheetXCollectionBaker.TryLoadData(settings, autoLoadOnly: true, out string error),
					Is.True, error);

				var global = AssetDatabase.LoadAssetAtPath<Inline.GlobalConfigCollection>(InlineGlobalPath);
				Assert.That(global.player.Characters, Has.Length.EqualTo(1));
				Assert.That(global.player.Characters[0].id, Is.EqualTo(9));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void configuration_and_inline_group_do_not_clobber_each_other()
		{
			File.WriteAllText(InlineJsonPath, "[{\"id\":4,\"name\":\"cleric\"}]");
			// A stale Configuration export can still carry a key that later became an inline group.
			// Nothing validates that at bake time, so order is what decides who wins.
			File.WriteAllText(JsonFolder + "/Configuration.txt",
				"{\"environment\":\"prod\",\"player\":{\"Characters\":[]}}");
			var settings = CreateInlineSettings();
			try
			{
				Assert.That(SheetXCollectionBaker.TryLoadData(settings, autoLoadOnly: false, out string error),
					Is.True, error);

				var global = AssetDatabase.LoadAssetAtPath<Inline.GlobalConfigCollection>(InlineGlobalPath);
				// Configuration is applied to Global at Baker.cs:238, before inline rows. Both write
				// into the same asset, so each must survive the other.
				Assert.That(global.environment, Is.EqualTo("prod"));
				Assert.That(global.player.Characters[0].name, Is.EqualTo("cleric"));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		[Test]
		public void returning_to_separate_asset_reuses_the_original_asset_and_guid()
		{
			File.WriteAllText(JsonPath, "[{\"id\":2,\"name\":\"potion\"}]");
			var settings = CreateSettings();
			try
			{
				Assert.That(SheetXCollectionBaker.TryLoadData(settings, "BakeShop", out string first), Is.True, first);
				string guid = AssetDatabase.AssetPathToGUID(FeaturePath);
				Assert.That(guid, Is.Not.Empty);

				// Simulate the round trip: the asset is deliberately never deleted when a collection
				// goes Inline, so coming back must find it rather than mint a new GUID. That is what
				// lets Prefab references that were left alone start resolving again.
				Assert.That(SheetXCollectionBaker.TryLoadData(settings, "BakeShop", out string second), Is.True, second);

				Assert.That(AssetDatabase.AssetPathToGUID(FeaturePath), Is.EqualTo(guid));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(settings);
			}
		}

		private static void EnsureFolder(string path)
		{
			if (AssetDatabase.IsValidFolder(path))
				return;

			string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
			AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
		}

		private static SheetXSettings CreateSettings()
		{
			var settings = ScriptableObject.CreateInstance<SheetXSettings>();
			settings.ResetToDefault();
			settings.enableCollections = true;
			settings.collectionNamespace = "RCore.SheetX.Tests";
			settings.collectionCodeFolder = TempRoot + "/Code";
			settings.collectionAssetFolder = AssetFolder;
			settings.collectionJsonFolder = JsonFolder;
			settings.globalResourcesFolder = ResourcesFolder;
			settings.collections.Add(new SheetXCollectionDefinition
			{
				name = "BakeShop",
				autoLoad = true,
			});
			settings.sheetBindings.Add(new SheetXSheetBinding
			{
				sourceId = "book.xlsx",
				sheetName = "BakeItems",
				outputMode = SheetXSheetOutputMode.ExistingDataClass,
				collectionName = "BakeShop",
				rowTypeName = typeof(BakeItemsRow).AssemblyQualifiedName,
				fieldName = "items",
			});
			return settings;
		}

		private static SheetXSettings CreateInlineSettings()
		{
			var settings = ScriptableObject.CreateInstance<SheetXSettings>();
			settings.ResetToDefault();
			settings.enableCollections = true;
			settings.collectionNamespace = "RCore.SheetX.Tests.Inline";
			settings.collectionCodeFolder = TempRoot + "/Code";
			settings.collectionAssetFolder = AssetFolder;
			settings.collectionJsonFolder = JsonFolder;
			settings.globalResourcesFolder = ResourcesFolder;
			settings.collections.Add(new SheetXCollectionDefinition
			{
				name = "Player",
				autoLoad = true,
				depth = SheetXCollectionDepth.Inline,
			});
			settings.sheetBindings.Add(new SheetXSheetBinding
			{
				sourceId = "book.xlsx",
				sheetName = "InlineCharacters",
				outputMode = SheetXSheetOutputMode.ExistingDataClass,
				collectionName = "Player",
				rowTypeName = typeof(BakeItemsRow).AssemblyQualifiedName,
				fieldName = "Characters",
			});
			return settings;
		}

		private static SheetXSettings CreateMarkerAbsentSettings()
		{
			var settings = ScriptableObject.CreateInstance<SheetXSettings>();
			settings.ResetToDefault();
			settings.enableCollections = true;
			settings.collectionNamespace = "RCore.SheetX.Tests.MarkerAbsent";
			settings.collectionCodeFolder = TempRoot + "/Code";
			settings.collectionAssetFolder = AssetFolder;
			settings.collectionJsonFolder = JsonFolder;
			settings.globalResourcesFolder = ResourcesFolder;
			return settings;
		}

		private static BakeShopConfigCollection CreateFeatureAsset(int id, string name)
		{
			var feature = ScriptableObject.CreateInstance<BakeShopConfigCollection>();
			feature.items = new[] { new BakeItemsRow { id = id, name = name } };
			AssetDatabase.CreateAsset(feature, FeaturePath);
			AssetDatabase.SaveAssetIfDirty(feature);
			return feature;
		}

		private static GlobalConfigCollection CreateGlobalAsset(string environment, BakeShopConfigCollection feature)
		{
			var global = ScriptableObject.CreateInstance<GlobalConfigCollection>();
			global.environment = environment;
			global.bakeShop = feature;
			AssetDatabase.CreateAsset(global, GlobalPath);
			AssetDatabase.SaveAssetIfDirty(global);
			return global;
		}

		private static void SetScript(ScriptableObject asset, MonoScript script)
		{
			var serializedObject = new SerializedObject(asset);
			var property = serializedObject.FindProperty("m_Script");
			property.objectReferenceValue = script;
			serializedObject.ApplyModifiedPropertiesWithoutUndo();
		}

		private static MonoScript ScriptOf(ScriptableObject asset)
		{
			var serializedObject = new SerializedObject(asset);
			return serializedObject.FindProperty("m_Script").objectReferenceValue as MonoScript;
		}

		private static bool ContainsTextAssetReference(ScriptableObject asset)
		{
			var serializedObject = new SerializedObject(asset);
			var property = serializedObject.GetIterator();
			bool enterChildren = true;
			while (property.NextVisible(enterChildren))
			{
				enterChildren = false;
				if (property.propertyPath == "m_Script"
					|| property.propertyType != SerializedPropertyType.ObjectReference
					|| !(property.objectReferenceValue is TextAsset))
				{
					continue;
				}
				return true;
			}
			return false;
		}
	}
}
