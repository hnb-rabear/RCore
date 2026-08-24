using System.Collections.Generic;
using NUnit.Framework;
using RCore.SheetX.Editor;

namespace RCore.SheetX.Tests
{
	public class CollectionSchemaTests
	{
		[Test]
		public void generated_schema_supports_scalars_arrays_and_nested_objects()
		{
			bool ok = SheetXCollectionSchemaParser.TryParse(
				new[] { "id:int", "price:float", "enabled:bool", "tags[]:string", "reward.amount:int" },
				"ShopItemsRow", out var schema, out var error);

			Assert.That(ok, Is.True, error);
			Assert.That(schema.RowTypeName, Is.EqualTo("ShopItemsRow"));
			Assert.That(schema.Columns, Has.Count.EqualTo(5));
			Assert.That(schema.Objects[0].TypeName, Is.EqualTo("Reward"));
		}

		[TestCase("id")]
		[TestCase("id:")]
		[TestCase(":int")]
		public void generated_schema_rejects_missing_annotation_parts(string header)
		{
			AssertParseFails(header, "expected '<path>:<type>'.");
		}

		[TestCase("id:long")]
		[TestCase("id:INT")]
		public void generated_schema_rejects_unsupported_type(string header)
		{
			AssertParseFails(header, "is not supported");
		}

		[TestCase("class:int")]
		[TestCase("item-id:int")]
		[TestCase("9items:int")]
		public void generated_schema_rejects_invalid_path_name(string header)
		{
			AssertParseFails(header, "invalid C# identifier");
		}

		[Test]
		public void generated_schema_rejects_object_leaf_conflicts()
		{
			bool ok = SheetXCollectionSchemaParser.TryParse(
				new[] { "reward:int", "reward.amount:int" },
				"ShopItemsRow", out _, out var error);

			Assert.That(ok, Is.False);
			Assert.That(error, Does.Contain("object/leaf conflict"));
		}

		[Test]
		public void generated_schema_rejects_duplicate_normalized_paths()
		{
			bool ok = SheetXCollectionSchemaParser.TryParse(
				new[] { "itemId:int", "ItemId:int" },
				"ShopItemsRow", out _, out var error);

			Assert.That(ok, Is.False);
			Assert.That(error, Does.Contain("duplicate normalized field path 'itemId'"));
		}

		[Test]
		public void generated_schema_rejects_array_leaf_with_dotted_child()
		{
			bool ok = SheetXCollectionSchemaParser.TryParse(
				new[] { "reward[]:int", "reward.amount:int" },
				"ShopItemsRow", out _, out var error);

			Assert.That(ok, Is.False);
			Assert.That(error, Does.Contain("object/leaf conflict"));
		}

		[Test]
		public void generated_schema_builds_bare_rows_with_nested_values()
		{
			bool parsed = SheetXCollectionSchemaParser.TryParse(
				new[] { "id:int", "reward.amount:int", "reward.currency:string", "tags[]:string", "ignored:string" },
				"ShopItemsRow", out var schema, out var error);
			Assert.That(parsed, Is.True, error);

			bool built = SheetXCollectionSchemaParser.TryBuildRows(
				schema,
				new List<IReadOnlyList<string>>
				{
					new[] { "7", "25", "gold", "sale|starter", "" },
				},
				out var json,
				out error);

			Assert.That(built, Is.True, error);
			Assert.That(json, Is.EqualTo("[{\"id\":7,\"reward\":{\"amount\":25,\"currency\":\"gold\"},\"tags\":[\"sale\",\"starter\"]}]"));
			Assert.That(json, Does.Not.Contain("tags[]"));
		}

		[Test]
		public void generated_schema_keeps_empty_persistent_scalar_defaults()
		{
			bool parsed = SheetXCollectionSchemaParser.TryParse(
				new[] { "id:int", "key:string", "enabled:bool", "tags[]:string" },
				"ShopItemsRow", out var schema, out var error);
			Assert.That(parsed, Is.True, error);

			bool built = SheetXCollectionSchemaParser.TryBuildRows(
				schema,
				new List<IReadOnlyList<string>> { new[] { "", "", "", "" } },
				out var json,
				out error);

			Assert.That(built, Is.True, error);
			Assert.That(json, Is.EqualTo("[{\"id\":0,\"key\":\"\"}]"));
		}

		[TestCase("count:int", "bad")]
		[TestCase("price:float", "NaN")]
		[TestCase("enabled:bool", "yes")]
		public void generated_schema_rejects_malformed_scalar_values(string header, string value)
		{
			bool parsed = SheetXCollectionSchemaParser.TryParse(
				new[] { header }, "ShopItemsRow", out var schema, out var error);
			Assert.That(parsed, Is.True, error);

			bool built = SheetXCollectionSchemaParser.TryBuildRows(
				schema,
				new List<IReadOnlyList<string>> { new[] { value } },
				out _,
				out error);

			Assert.That(built, Is.False);
			Assert.That(error, Does.Contain("Row 1"));
			Assert.That(error, Does.Contain(header));
		}

		private static void AssertParseFails(string header, string expected)
		{
			bool ok = SheetXCollectionSchemaParser.TryParse(
				new[] { header }, "ShopItemsRow", out _, out var error);

			Assert.That(ok, Is.False);
			Assert.That(error, Does.Contain(expected));
		}
	}
}
