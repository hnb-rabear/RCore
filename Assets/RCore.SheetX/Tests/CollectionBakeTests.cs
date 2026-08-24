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
		private const string TempRoot = "Assets/SheetXTestsTemp/Editor";
		private const string JsonFolder = TempRoot + "/Json";
		private const string AssetFolder = TempRoot + "/Collections";
		private const string ResourcesFolder = TempRoot + "/Resources";
		private const string FeaturePath = AssetFolder + "/BakeShopConfigCollection.asset";
		private const string GlobalPath = ResourcesFolder + "/GlobalConfigCollection.asset";
		private const string JsonPath = JsonFolder + "/BakeItems.txt";

		private class TestGlobalCollection : GlobalConfigCollectionBase
		{
		}

		private class TestPlainCollection : SheetXConfigCollectionBase
		{
		}

		[SetUp]
		public void SetUp()
		{
			AssetDatabase.DeleteAsset("Assets/SheetXTestsTemp");
			Directory.CreateDirectory(JsonFolder);
			Directory.CreateDirectory(AssetFolder);
			Directory.CreateDirectory(ResourcesFolder);
			AssetDatabase.Refresh();
		}

		[TearDown]
		public void TearDown()
		{
			SheetXCollectionBaker.TestBeforeSave = null;
			AssetDatabase.DeleteAsset("Assets/SheetXTestsTemp");
			AssetDatabase.Refresh();
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
				outputMode = SheetXSheetOutputMode.CollectionExistingModel,
				collectionName = "BakeShop",
				rowTypeName = typeof(BakeItemsRow).AssemblyQualifiedName,
				fieldName = "items",
			});
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
