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
				"ShopItemsSX", out var schema, out var error);

			Assert.That(ok, Is.True, error);
			Assert.That(schema.RowTypeName, Is.EqualTo("ShopItemsSX"));
			Assert.That(schema.Columns, Has.Count.EqualTo(5));
			Assert.That(schema.Objects[0].TypeName, Is.EqualTo("Reward"));
		}

		[Test]
		public void generated_schema_infers_scalar_types_from_longest_column_cells()
		{
			bool ok = SheetXCollectionSchemaParser.TryParse(
				new[] { "count", "price", "enabled", "name", "empty" },
				new IReadOnlyList<string>[]
				{
					new[] { "1", "1.5", "true", "Hero", "" },
					new[] { "100", "20", "false", "Potion", "" },
				},
				"ShopItemsSX", out var schema, out var error);

			Assert.That(ok, Is.True, error);
			Assert.That(schema.Columns[0].TypeName, Is.EqualTo("int"));
			Assert.That(schema.Columns[1].TypeName, Is.EqualTo("float"));
			Assert.That(schema.Columns[2].TypeName, Is.EqualTo("bool"));
			Assert.That(schema.Columns[3].TypeName, Is.EqualTo("string"));
			Assert.That(schema.Columns[4].TypeName, Is.EqualTo("string"));
		}

		[Test]
		public void generated_schema_uses_longest_cell_for_inference()
		{
			bool ok = SheetXCollectionSchemaParser.TryParse(
				new[] { "value" },
				new IReadOnlyList<string>[] { new[] { "1" }, new[] { "false" } },
				"ShopItemsSX", out var schema, out var error);

			Assert.That(ok, Is.True, error);
			Assert.That(schema.Columns[0].TypeName, Is.EqualTo("bool"));
		}

		[TestCase("NaN")]
		[TestCase("Infinity")]
		public void generated_schema_infers_non_finite_values_as_string(string value)
		{
			bool ok = SheetXCollectionSchemaParser.TryParse(
				new[] { "value" },
				new IReadOnlyList<string>[] { new[] { value } },
				"ShopItemsSX", out var schema, out var error);

			Assert.That(ok, Is.True, error);
			Assert.That(schema.Columns[0].TypeName, Is.EqualTo("string"));
		}

		[Test]
		public void generated_schema_requires_rows_to_infer_unannotated_headers()
		{
			bool ok = SheetXCollectionSchemaParser.TryParse(
				new[] { "value" }, "ShopItemsSX", out _, out var error);

			Assert.That(ok, Is.False);
			Assert.That(error, Does.Contain("Rows are required"));
		}

		[Test]
		public void generated_schema_annotation_overrides_inference()
		{
			bool ok = SheetXCollectionSchemaParser.TryParse(
				new[] { "value:float" },
				new IReadOnlyList<string>[] { new[] { "1" } },
				"ShopItemsSX", out var schema, out var error);

			Assert.That(ok, Is.True, error);
			Assert.That(schema.Columns[0].TypeName, Is.EqualTo("float"));
		}

		[TestCase("id:")]
		[TestCase(":int")]
		public void generated_schema_rejects_incomplete_annotation(string header)
		{
			AssertParseFails(header, "expected '<path>' or '<path>:<type>'.");
		}

		[TestCase("id:long")]
		[TestCase("id:INT")]
		public void generated_schema_rejects_unsupported_type(string header)
		{
			AssertParseFails(header, "is not supported");
		}

		[TestCase("item-id:int")]
		[TestCase("9items:int")]
		public void generated_schema_rejects_invalid_path_name(string header)
		{
			bool ok = SheetXCollectionSchemaParser.TryParse(
				new[] { header }, "ShopItemsSX", out _, out var error);

			Assert.That(ok, Is.False);
			Assert.That(error, Does.Contain($"Header '{header}' (column 1)"));
			Assert.That(error, Does.Contain("invalid C# identifier"));
			Assert.That(error, Does.Contain("Fix:"));
		}

		[TestCase("fixed")]
		[TestCase("fixed:int")]
		[TestCase("reward.class:int")]
		public void generated_schema_skips_keyword_paths_with_actionable_warning(string header)
		{
			bool ok = SheetXCollectionSchemaParser.TryParse(
				new[] { "id:int", header, "name:string" }, null, "ShopItemsSX",
				out var schema, out var warnings, out var error);

			Assert.That(ok, Is.True, error);
			Assert.That(schema.Columns, Has.Count.EqualTo(2));
			Assert.That(warnings, Has.Count.EqualTo(1));
			Assert.That(warnings[0], Does.Contain($"Header '{header}' (column 2)"));
			Assert.That(warnings[0], Does.Contain("C# keyword"));
			Assert.That(warnings[0], Does.Contain("Fix:"));
			Assert.That(warnings[0], Does.Contain("[x]"));
		}

		[TestCase("note[x]")]
		[TestCase("[x] min atk")]
		[TestCase("price[x]:float")]
		public void generated_schema_ignores_x_marker_anywhere(string header)
		{
			bool ok = SheetXCollectionSchemaParser.TryParse(
				new[] { "id:int", header, "name:string" }, null, "ShopItemsSX",
				out var schema, out var warnings, out var error);

			Assert.That(ok, Is.True, error);
			Assert.That(schema.Columns, Has.Count.EqualTo(2));
			Assert.That(warnings, Is.Empty);
		}

		[Test]
		public void generated_schema_rejects_when_every_header_is_ignored()
		{
			bool ok = SheetXCollectionSchemaParser.TryParse(
				new[] { "fixed", "note[x]" }, null, "ShopItemsSX",
				out _, out var warnings, out var error);

			Assert.That(ok, Is.False);
			Assert.That(warnings, Has.Count.EqualTo(1));
			Assert.That(error, Does.Contain("no usable generated fields"));
			Assert.That(error, Does.Contain("Fix:"));
		}

		[Test]
		public void generated_schema_skips_keyword_without_rows_when_other_headers_are_annotated()
		{
			bool ok = SheetXCollectionSchemaParser.TryParse(
				new[] { "id:int", "fixed" }, "ShopItemsSX", out var schema, out var error);

			Assert.That(ok, Is.True, error);
			Assert.That(schema.Columns, Has.Count.EqualTo(1));
		}

		[Test]
		public void generated_schema_rejects_object_leaf_conflicts()
		{
			bool ok = SheetXCollectionSchemaParser.TryParse(
				new[] { "reward:int", "reward.amount:int" },
				"ShopItemsSX", out _, out var error);

			Assert.That(ok, Is.False);
			Assert.That(error, Does.Contain("object/leaf conflict"));
			Assert.That(error, Does.Contain("Fix:"));
		}

		[Test]
		public void generated_schema_reports_nested_type_collision_location_and_repair()
		{
			bool ok = SheetXCollectionSchemaParser.TryParse(
				new[] { "reward.amount:int" }, "Reward", out _, out var error);

			Assert.That(ok, Is.False);
			Assert.That(error, Does.Contain("Header 'reward.amount:int' (column 1)"));
			Assert.That(error, Does.Contain("collides with generated type 'Reward'"));
			Assert.That(error, Does.Contain("Fix:"));
		}

		[Test]
		public void generated_schema_rejects_duplicate_normalized_paths()
		{
			bool ok = SheetXCollectionSchemaParser.TryParse(
				new[] { "itemId:int", "ItemId:int" },
				"ShopItemsSX", out _, out var error);

			Assert.That(ok, Is.False);
			Assert.That(error, Does.Contain("duplicate normalized field path 'itemId'"));
			Assert.That(error, Does.Contain("Fix:"));
		}

		[Test]
		public void generated_schema_rejects_array_leaf_with_dotted_child()
		{
			bool ok = SheetXCollectionSchemaParser.TryParse(
				new[] { "reward[]:int", "reward.amount:int" },
				"ShopItemsSX", out _, out var error);

			Assert.That(ok, Is.False);
			Assert.That(error, Does.Contain("object/leaf conflict"));
		}

		[Test]
		public void generated_schema_infers_plain_array_and_nested_fields()
		{
			bool ok = SheetXCollectionSchemaParser.TryParse(
				new[] { "id", "reward.amount", "tags[]" },
				new IReadOnlyList<string>[] { new[] { "7", "25", "sale|starter" } },
				"ShopItemsSX", out var schema, out var error);

			Assert.That(ok, Is.True, error);
			Assert.That(schema.Columns[0].TypeName, Is.EqualTo("int"));
			Assert.That(schema.Columns[1].TypeName, Is.EqualTo("int"));
			Assert.That(schema.Columns[2].TypeName, Is.EqualTo("string[]"));
			Assert.That(schema.Objects[0].TypeName, Is.EqualTo("Reward"));
		}

		[Test]
		public void generated_schema_preserves_source_indexes_after_ignored_columns()
		{
			bool parsed = SheetXCollectionSchemaParser.TryParse(
				new[] { "id:int", "fixed", "note[x]", "name:string" }, null, "ShopItemsSX",
				out var schema, out _, out var error);
			Assert.That(parsed, Is.True, error);

			bool built = SheetXCollectionSchemaParser.TryBuildRows(
				schema,
				new List<IReadOnlyList<string>> { new[] { "7", "skip-keyword", "skip-marker", "hero" } },
				out var json,
				out error);

			Assert.That(built, Is.True, error);
			Assert.That(json, Is.EqualTo("[{\"id\":7,\"name\":\"hero\"}]"));
		}

		[Test]
		public void generated_schema_builds_bare_rows_with_nested_values()
		{
			bool parsed = SheetXCollectionSchemaParser.TryParse(
				new[] { "id:int", "reward.amount:int", "reward.currency:string", "tags[]:string", "ignored:string" },
				"ShopItemsSX", out var schema, out var error);
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
				"ShopItemsSX", out var schema, out var error);
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
				new[] { header }, "ShopItemsSX", out var schema, out var error);
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
				new[] { header }, "ShopItemsSX", out _, out var error);

			Assert.That(ok, Is.False);
			Assert.That(error, Does.Contain(expected));
		}
	}
}
