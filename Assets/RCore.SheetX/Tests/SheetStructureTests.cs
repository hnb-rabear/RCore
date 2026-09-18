/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Linq;
using NUnit.Framework;
using RCore.SheetX.Editor;

namespace RCore.SheetX.Tests
{
	public class SheetStructureTests
	{
		[Test]
		public void keys_from_later_rows_and_valid_legacy_spelling_survive()
		{
			Assert.That(SheetXSheetStructure.TryFromJson(
				"[{\"id\":1},{\"bot_ids\":[4,7],\"Attributes\":[{\"id\":3}]}]",
				"PoolsSX", out var draft, out string error), Is.True, error);
			Assert.That(draft.RootMemberNames,
				Is.EqualTo(new[] { "id", "bot_ids", "Attributes" }));
			string code = draft.ToClassCode("");
			Assert.That(code, Does.Contain("public int[] bot_ids;"));
			Assert.That(code, Does.Contain("public Attributes[] Attributes;"));
		}

		[TestCase("[]")]
		[TestCase("[{}]")]
		public void empty_observations_are_not_a_complete_class(string json)
		{
			Assert.That(SheetXSheetStructure.TryFromJson(json, "RowSX", out _, out string error), Is.False);
			Assert.That(error, Is.EqualTo("Cannot infer structure from empty exported data"));
		}

		[TestCase("{}")]
		[TestCase("[1]")]
		[TestCase("[{\"id\":1},false]")]
		[TestCase("[{\"id\":1}] []")]
		public void unsupported_roots_rows_and_trailing_documents_are_rejected(string json)
		{
			Assert.That(SheetXSheetStructure.TryFromJson(json, "RowSX", out _, out string error), Is.False);
			Assert.That(error, Is.Not.Null.And.Not.Empty);
		}

		[Test]
		public void observations_widen_numbers_and_label_unknown_shapes()
		{
			const string json = "[{\"large\":3000000000,\"ratio\":1,\"empty\":[],\"unknown\":null},"
				+ "{\"ratio\":2.5,\"mixed\":false},{\"mixed\":\"text\"}]";
			Assert.That(SheetXSheetStructure.TryFromJson(json, "RowSX", out var draft, out string error), Is.True, error);
			string code = draft.ToClassCode("");
			Assert.That(code, Does.Contain("public long large;"));
			Assert.That(code, Does.Contain("public double ratio;"));
			Assert.That(code, Does.Contain("public object[] empty;"));
			Assert.That(code, Does.Contain("public object unknown;"));
			Assert.That(code, Does.Contain("public object mixed;"));
			Assert.That(draft.Diagnostics, Is.Not.Empty);
		}

		[Test]
		public void names_are_legal_distinct_and_deterministic()
		{
			const string json = "[{"
				+ "\"class\":1,"
				+ "\"_class\":2,"
				+ "\"a-b\":3,"
				+ "\"aB\":4,"
				+ "\"RowSX\":5,"
				+ "\"quote\\\"\\nkey\":6,"
				+ "\"nested\":{\"reward\":10,\"Reward\":20}"
				+ "}]";

			Assert.That(SheetXSheetStructure.TryFromJson(json, "RowSX", out var draft, out string error), Is.True, error);
			string code = draft.ToClassCode("");
			string repeatedCode = draft.ToClassCode("");
			string[] fields = code.Split(new[] { "\r\n" }, StringSplitOptions.None)
				.Where(line => line.StartsWith("\tpublic "))
				.Select(line => line.Trim().TrimEnd(';').Split(' ')[2])
				.ToArray();

			Assert.That(fields.Distinct().Count(), Is.EqualTo(fields.Length));
			Assert.That(fields, Does.Not.Contain("RowSX"));
			Assert.That(code, Does.Contain("[global::Newtonsoft.Json.JsonProperty(\"class\")]"));
			Assert.That(code, Does.Contain("[global::Newtonsoft.Json.JsonProperty(\"a-b\")]"));
			Assert.That(code, Does.Contain("[global::Newtonsoft.Json.JsonProperty(\"quote\\\"\\nkey\")]"));
			Assert.That(code, Is.EqualTo(repeatedCode));
		}

		[Test]
		public void strings_resembling_dates_remain_strings()
		{
			const string json = "[{\"created_at\":\"2026-09-16T12:00:00Z\",\"date\":\"2026-01-01\"}]";
			Assert.That(SheetXSheetStructure.TryFromJson(json, "RowSX", out var draft, out string error), Is.True, error);
			string code = draft.ToClassCode("");
			Assert.That(code, Does.Contain("public string created_at;"));
			Assert.That(code, Does.Contain("public string date;"));
		}

		[Test]
		public void integer_overflow_beyond_int64_falls_back_to_object_with_diagnostic()
		{
			const string json = "[{\"huge\":99999999999999999999999999999999999999}]";
			Assert.That(SheetXSheetStructure.TryFromJson(json, "RowSX", out var draft, out string error), Is.True, error);
			string code = draft.ToClassCode("");
			Assert.That(code, Does.Contain("public object huge;"));
			Assert.That(draft.Diagnostics, Has.Count.GreaterThan(0));
			Assert.That(draft.Diagnostics[0], Does.Contain("beyond Int64"));
		}

		[Test]
		public void nested_arrays_fall_back_to_object_array_with_diagnostic()
		{
			const string json = "[{\"matrix\":[[1,2],[3,4]]}]";
			Assert.That(SheetXSheetStructure.TryFromJson(json, "RowSX", out var draft, out string error), Is.True, error);
			string code = draft.ToClassCode("");
			Assert.That(code, Does.Contain("public object[] matrix;"));
			Assert.That(draft.Diagnostics, Has.Count.GreaterThan(0));
			Assert.That(draft.Diagnostics[0], Does.Contain("nested arrays"));
		}

		[Test]
		public void null_mixed_with_scalar_falls_back_to_object_with_diagnostic()
		{
			const string json = "[{\"count\":10,\"flag\":true},{\"count\":null,\"flag\":null}]";
			Assert.That(SheetXSheetStructure.TryFromJson(json, "RowSX", out var draft, out string error), Is.True, error);
			string code = draft.ToClassCode("");
			Assert.That(code, Does.Contain("public object count;"));
			Assert.That(code, Does.Contain("public object flag;"));
			Assert.That(draft.Diagnostics, Has.Count.GreaterThanOrEqualTo(2));
		}

		[Test]
		public void sparse_omitted_keys_do_not_become_null()
		{
			const string json = "[{\"id\":1,\"sparse\":42},{\"id\":2}]";
			Assert.That(SheetXSheetStructure.TryFromJson(json, "RowSX", out var draft, out string error), Is.True, error);
			string code = draft.ToClassCode("");
			Assert.That(code, Does.Contain("public int sparse;"));
			Assert.That(draft.Diagnostics, Is.Empty);
		}

		[TestCase("")]
		[TestCase("123Bad")]
		[TestCase("class")]
		[TestCase("Bad-Name")]
		public void invalid_root_type_name_is_rejected(string rootTypeName)
		{
			Assert.That(SheetXSheetStructure.TryFromJson("[{\"id\":1}]", rootTypeName, out _, out string error), Is.False);
			Assert.That(error, Does.Contain("not a valid C# identifier"));
		}

		[TestCase("Invalid..Namespace")]
		[TestCase("123Bad")]
		[TestCase("namespace.class")]
		public void invalid_namespace_throws_argument_exception(string ns)
		{
			Assert.That(SheetXSheetStructure.TryFromJson("[{\"id\":1}]", "RowSX", out var draft, out _), Is.True);
			Assert.Throws<ArgumentException>(() => draft.ToClassCode(ns));
		}

		[Test]
		public void valid_namespace_wraps_classes_with_indentation()
		{
			Assert.That(SheetXSheetStructure.TryFromJson("[{\"id\":1}]", "RowSX", out var draft, out _), Is.True);
			string code = draft.ToClassCode("MyCompany.GameData");
			Assert.That(code, Does.Contain("namespace MyCompany.GameData\r\n{\r\n"));
			Assert.That(code, Does.EndWith("}\r\n"));
			Assert.That(code, Does.Contain("\t\tpublic int id;\r\n"));
		}

		[Test]
		public void duplicate_json_property_names_are_rejected()
		{
			const string json = "[{\"id\":1,\"id\":2}]";
			Assert.That(SheetXSheetStructure.TryFromJson(json, "RowSX", out _, out string error), Is.False);
			Assert.That(error, Is.Not.Null.And.Not.Empty);
		}

		[Test]
		public void two_invalid_keys_sharing_a_sanitized_identifier_stay_distinct()
		{
			// 'a-b' and 'a.b' both sanitize to AB, unlike 'a-b' versus 'aB' where C# casing already
			// separates them. Only this shape exercises the collision counter.
			const string json = "[{\"a-b\":1,\"a.b\":2}]";
			Assert.That(SheetXSheetStructure.TryFromJson(json, "RowSX", out var draft, out string error), Is.True, error);

			string code = draft.ToClassCode("");
			var names = draft.Fields.Select(f => f.FieldName).ToArray();

			Assert.That(names.Distinct().Count(), Is.EqualTo(2), "Both keys collapsed onto one field name.");
			Assert.That(code, Does.Contain("[global::Newtonsoft.Json.JsonProperty(\"a-b\")]"));
			Assert.That(code, Does.Contain("[global::Newtonsoft.Json.JsonProperty(\"a.b\")]"));
			Assert.That(code, Is.EqualTo(draft.ToClassCode("")), "Collision naming must be deterministic.");
		}

		[Test]
		public void an_array_mixing_nulls_with_objects_falls_back_rather_than_claiming_a_type()
		{
			// nonNullElements filters the null out, so the elements are all objects; the question is whether
			// the draft then claims a supporting type for a column that also holds nulls.
			const string json = "[{\"rewards\":[{\"id\":1},null]}]";
			Assert.That(SheetXSheetStructure.TryFromJson(json, "RowSX", out var draft, out string error), Is.True, error);

			string code = draft.ToClassCode("");
			var field = draft.Fields.Single();

			Assert.That(field.JsonName, Is.EqualTo("rewards"));
			Assert.That(field.FieldType, Does.EndWith("[]"), "An array column must stay an array.");
			Assert.That(code, Does.Contain($"public {field.FieldType} {field.FieldName};"),
				"The table and the emitted declaration must agree for this shape too.");
		}

		#region Draft header and per-field comments

		[Test]
		public void the_draft_opens_with_a_header_naming_it_inferred_rather_than_recovered()
		{
			Assert.That(SheetXSheetStructure.TryFromJson("[{\"id\":1}]", "RowSX", out var draft, out _), Is.True);

			string code = draft.ToClassCode("");

			Assert.That(code, Does.StartWith("// "),
				"A reader who copies this out of the window must be told it is a draft before any code.");
			Assert.That(code, Does.Contain("Inferred from this sheet's exported JSON"));
			Assert.That(code, Does.Contain("not a recovered sheet schema"));
			Assert.That(code, Does.Contain("blank or omitted"));
		}

		[Test]
		public void an_ordinary_declaration_carries_no_comment()
		{
			Assert.That(SheetXSheetStructure.TryFromJson(
				"[{\"id\":1,\"name\":\"Alpha\"}]", "RowSX", out var draft, out _), Is.True);

			string code = draft.ToClassCode("");
			string body = code.Substring(code.IndexOf("public class", StringComparison.Ordinal));

			Assert.That(body, Does.Not.Contain("//"),
				"Restating every plain declaration is noise; only renames and fallbacks earn a comment.");
			Assert.That(body, Does.Contain("public int id;"));
			Assert.That(body, Does.Contain("public string name;"));
		}

		[Test]
		public void a_renamed_field_comment_names_the_exact_json_key()
		{
			Assert.That(SheetXSheetStructure.TryFromJson("[{\"a-b\":3}]", "RowSX", out var draft, out _), Is.True);

			string code = draft.ToClassCode("");

			Assert.That(code, Does.Contain("JSON key \"a-b\""),
				"The rename is only explained if the exact exported key is named.");
			Assert.That(code, Does.Not.Contain("column"),
				"The original spreadsheet header cannot be recovered from JSON, so it must not be claimed.");
		}

		[Test]
		public void a_key_containing_newlines_and_quotes_cannot_break_out_of_its_comment()
		{
			Assert.That(SheetXSheetStructure.TryFromJson(
				"[{\"quote\\\"\\nkey\":6}]", "RowSX", out var draft, out _), Is.True);

			string code = draft.ToClassCode("");
			string[] lines = code.Split(new[] { "\r\n" }, StringSplitOptions.None);

			foreach (string line in lines)
			{
				Assert.That(line, Does.Not.Contain("\n").And.Not.Contain("\r"),
					"A raw newline inside a comment would start an executable line.");
				Assert.That(line, Does.Not.Contain("\u2028").And.Not.Contain("\u2029"),
					"Unicode line separators end a C# comment just as CR/LF do.");
			}
			Assert.That(code, Does.Contain("\\n"), "The newline must survive as an escape, not vanish.");
		}

		[Test]
		public void a_null_only_fallback_comment_states_that_reason_and_not_another()
		{
			Assert.That(SheetXSheetStructure.TryFromJson(
				"[{\"unknown\":null}]", "RowSX", out var draft, out _), Is.True);

			string code = draft.ToClassCode("");

			Assert.That(code, Does.Contain("public object unknown;"));
			Assert.That(code, Does.Contain("only null"),
				"A null-only fallback and an empty-array fallback are different observations.");
			Assert.That(code, Does.Not.Contain("empty array"));
		}

		[Test]
		public void an_empty_array_fallback_comment_states_that_reason_and_not_another()
		{
			Assert.That(SheetXSheetStructure.TryFromJson("[{\"empty\":[]}]", "RowSX", out var draft, out _), Is.True);

			string code = draft.ToClassCode("");

			Assert.That(code, Does.Contain("public object[] empty;"));
			Assert.That(code, Does.Contain("only empty arrays"));
			Assert.That(code, Does.Not.Contain("only null"));
		}

		[Test]
		public void a_mixed_kind_fallback_comment_reuses_the_reason_inference_recorded()
		{
			const string json = "[{\"mixed\":false},{\"mixed\":\"text\"}]";
			Assert.That(SheetXSheetStructure.TryFromJson(json, "RowSX", out var draft, out _), Is.True);

			string code = draft.ToClassCode("");

			Assert.That(code, Does.Contain("public object mixed;"));
			Assert.That(code, Does.Contain("incompatible"),
				"The comment must restate the reason inference already determined, not guess from 'object'.");
		}

		[Test]
		public void comments_do_not_change_the_emitted_declarations_or_determinism()
		{
			const string json = "[{\"a-b\":3,\"empty\":[],\"id\":1}]";
			Assert.That(SheetXSheetStructure.TryFromJson(json, "RowSX", out var draft, out _), Is.True);

			string code = draft.ToClassCode("");

			Assert.That(code, Is.EqualTo(draft.ToClassCode("")), "Emission must stay deterministic.");
			Assert.That(code, Does.Contain("[global::Newtonsoft.Json.JsonProperty(\"a-b\")]"));
			Assert.That(code, Does.Contain("public object[] empty;"));
			foreach (string line in code.Split(new[] { "\r\n" }, StringSplitOptions.None))
				Assert.That(line, Is.EqualTo(line.TrimEnd()), "No trailing whitespace: " + line);
		}

		#endregion

		#region Field metadata

		[Test]
		public void field_metadata_matches_the_emitted_declarations()
		{
			const string json = "[{\"id\":1,\"a-b\":3,\"empty\":[]}]";
			Assert.That(SheetXSheetStructure.TryFromJson(json, "RowSX", out var draft, out _), Is.True);

			var fields = draft.Fields;
			string code = draft.ToClassCode("");

			Assert.That(fields.Select(f => f.JsonName), Is.EqualTo(new[] { "id", "a-b", "empty" }),
				"The table must list top-level keys in the same first-seen order the code emits.");
			foreach (var field in fields)
			{
				Assert.That(code, Does.Contain($"public {field.FieldType} {field.FieldName};"),
					$"Table row '{field.JsonName}' disagrees with the emitted declaration.");
			}
		}

		[Test]
		public void field_metadata_covers_top_level_keys_only()
		{
			const string json = "[{\"id\":1,\"nested\":{\"reward\":10}}]";
			Assert.That(SheetXSheetStructure.TryFromJson(json, "RowSX", out var draft, out _), Is.True);

			Assert.That(draft.Fields.Select(f => f.JsonName), Is.EqualTo(new[] { "id", "nested" }),
				"Supporting-class members stay in the code; the table is top-level only.");
		}

		[Test]
		public void a_renamed_field_declaration_hint_carries_its_json_property_mapping()
		{
			Assert.That(SheetXSheetStructure.TryFromJson("[{\"a-b\":3}]", "RowSX", out var draft, out _), Is.True);

			var field = draft.Fields.Single();

			Assert.That(field.IsRenamed, Is.True);
			Assert.That(field.Declaration, Does.Contain("JsonProperty(\"a-b\")"),
				"A plain declaration would not receive the JSON key, so suggesting it alone would be wrong.");
			Assert.That(field.Declaration, Does.Contain("public int AB;"));
		}

		[Test]
		public void a_plain_field_declaration_hint_is_just_the_declaration()
		{
			Assert.That(SheetXSheetStructure.TryFromJson("[{\"id\":1}]", "RowSX", out var draft, out _), Is.True);

			var field = draft.Fields.Single();

			Assert.That(field.IsRenamed, Is.False);
			Assert.That(field.Declaration, Is.EqualTo("public int id;"));
		}

		#endregion
	}
}
