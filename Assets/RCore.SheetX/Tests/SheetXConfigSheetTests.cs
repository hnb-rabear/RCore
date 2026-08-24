/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NPOI.XSSF.UserModel;
using NUnit.Framework;
using RCore.SheetX.Editor;
using UnityEngine;

namespace RCore.SheetX.Tests
{
	public class SheetXConfigSheetTests
	{
		[Test]
		public void config_script_artifact_type_is_appended_after_existing_types()
		{
			Assert.That(
				(int)SheetXExportFileType.ConfigScript,
				Is.GreaterThan((int)SheetXExportFileType.LocalizationComponent));
		}

		[Test]
		public void existing_artifact_type_ordinals_are_unchanged()
		{
			Assert.That((int)SheetXExportFileType.Ids, Is.EqualTo(0));
			Assert.That((int)SheetXExportFileType.Constants, Is.EqualTo(1));
			Assert.That((int)SheetXExportFileType.Json, Is.EqualTo(2));
			Assert.That((int)SheetXExportFileType.Localization, Is.EqualTo(3));
			Assert.That((int)SheetXExportFileType.CharacterSet, Is.EqualTo(4));
			Assert.That((int)SheetXExportFileType.LocalizationManager, Is.EqualTo(5));
			Assert.That((int)SheetXExportFileType.LocalizationConstants, Is.EqualTo(6));
			Assert.That((int)SheetXExportFileType.LocalizationComponent, Is.EqualTo(7));
		}

		[Test]
		public void parse_groups_and_root_fields_after_blank_separator()
		{
			bool ok = SheetXConfigSheet.TryParse(new List<string[]>
			{
				Header(),
				Row("economy", "startingCoins", "int", "1000"),
				Row("", "startingGems", "int", "50"),
				Row("", "", "", ""),
				Row("", "position2", "vector2", "1|2"),
			}, "GameDataConfig", Fail, out var data);

			Assert.That(ok, Is.True);
			Assert.That(data.Groups, Has.Count.EqualTo(1));
			Assert.That(data.Groups[0].Key, Is.EqualTo("economy"));
			Assert.That(data.Groups[0].ClassName, Is.EqualTo("Economy"));
			Assert.That(data.Groups[0].Fields, Has.Count.EqualTo(2));
			Assert.That(data.RootFields, Has.Count.EqualTo(1));
			Assert.That(data.RootFields[0].Name, Is.EqualTo("position2"));
		}

		[TestCase("int", "42")]
		[TestCase("float", "1.25")]
		[TestCase("boolean", "true")]
		[TestCase("string", "hello")]
		[TestCase("int-array", "1|2|3")]
		[TestCase("float-array", "1.5|2.25")]
		[TestCase("string-array", "one|two")]
		[TestCase("vector2", "1|2")]
		[TestCase("vector3", "1|2|3")]
		public void parse_accepts_every_supported_type(string type, string value)
		{
			bool ok = SheetXConfigSheet.TryParse(new List<string[]>
			{
				Header(),
				Row("group", "value", type, value),
			}, "GameDataConfig", Fail, out _);

			Assert.That(ok, Is.True);
		}

		[TestCase("INT", "7")]
		[TestCase("Vector3", "1|2|3")]
		[TestCase("FLOAT-ARRAY", "1|2")]
		public void parse_type_names_are_case_insensitive(string type, string value)
		{
			bool ok = SheetXConfigSheet.TryParse(new List<string[]>
			{
				Header(),
				Row("group", "value", type, value),
			}, "GameDataConfig", Fail, out _);

			Assert.That(ok, Is.True);
		}

		[Test]
		public void parse_strips_only_trailing_display_array_suffix()
		{
			bool ok = SheetXConfigSheet.TryParse(new List<string[]>
			{
				Header(),
				Row("group", "rates[]", "float-array", "0.5|0.25"),
			}, "GameDataConfig", Fail, out var data);

			Assert.That(ok, Is.True);
			Assert.That(data.Groups[0].Fields[0].Name, Is.EqualTo("rates"));
			Assert.That(data.Groups[0].Fields[0].Type, Is.EqualTo(ConfigFieldType.FloatArray));
		}

		[Test]
		public void parse_ignores_extra_columns_beyond_value()
		{
			bool ok = SheetXConfigSheet.TryParse(new List<string[]>
			{
				new[] { "Sub Class", "Field Name", "Type", "Value", "Note" },
				new[] { "group", "count", "int", "3", "ignored" },
			}, "GameDataConfig", Fail, out var data);

			Assert.That(ok, Is.True);
			Assert.That(data.Groups[0].Fields[0].Value, Is.EqualTo(3));
		}

		[TestCase("Sub Class", "Field", "Type", "Value")]
		[TestCase("SubClass", "Field Name", "Type", "Value")]
		public void parse_rejects_wrong_header(string a, string b, string c, string d)
		{
			var errors = ParseErrors(new List<string[]>
			{
				new[] { a, b, c, d },
				Row("group", "count", "int", "1"),
			});

			Assert.That(errors, Has.Count.EqualTo(1));
			Assert.That(errors[0], Does.Contain("Config header"));
		}

		[Test]
		public void parse_rejects_missing_table()
		{
			var errors = ParseErrors(new List<string[]>());

			Assert.That(errors, Has.Count.EqualTo(1));
			Assert.That(errors[0], Does.Contain("header"));
		}

		[Test]
		public void parse_rejects_invalid_root_type_name()
		{
			var errors = ParseErrors(new List<string[]>
			{
				Header(),
				Row("group", "count", "int", "1"),
			}, "9GameConfig");

			Assert.That(errors, Has.Some.Contains("9GameConfig"));
		}

		[TestCase("2bad")]
		[TestCase("has space")]
		[TestCase("class")]
		public void parse_rejects_invalid_field_identifier(string fieldName)
		{
			var errors = ParseErrors(new List<string[]>
			{
				Header(),
				Row("group", fieldName, "int", "1"),
			});

			Assert.That(errors, Has.Some.Contains("valid C# identifier"));
		}

		[TestCase("value")]
		[TestCase("var")]
		[TestCase("from")]
		[TestCase("_private")]
		public void parse_accepts_contextual_keywords_as_field_names(string fieldName)
		{
			bool ok = SheetXConfigSheet.TryParse(new List<string[]>
			{
				Header(),
				Row("group", fieldName, "int", "1"),
			}, "GameDataConfig", Fail, out _);

			Assert.That(ok, Is.True);
		}

		[Test]
		public void parse_rejects_invalid_group_identifier()
		{
			var errors = ParseErrors(new List<string[]>
			{
				Header(),
				Row("2group", "count", "int", "1"),
			});

			Assert.That(errors, Has.Some.Contains("Sub Class '2group'"));
		}

		[Test]
		public void parse_rejects_unknown_type()
		{
			var errors = ParseErrors(new List<string[]>
			{
				Header(),
				Row("group", "count", "unknown", "1"),
			});

			Assert.That(errors, Has.Some.Contains("type 'unknown' is not supported"));
		}

		[TestCase("int", "abc")]
		[TestCase("float", "abc")]
		[TestCase("boolean", "yes")]
		[TestCase("int-array", "1|x")]
		[TestCase("float", "NaN")]
		[TestCase("float", "Infinity")]
		[TestCase("float-array", "1|NaN")]
		public void parse_rejects_unparseable_value(string type, string value)
		{
			var errors = ParseErrors(new List<string[]>
			{
				Header(),
				Row("group", "value", type, value),
			});

			Assert.That(errors, Has.Some.Contains("value"));
		}

		[TestCase("vector2", "1|2|3")]
		[TestCase("vector3", "1|2")]
		public void parse_rejects_wrong_vector_arity(string type, string value)
		{
			var errors = ParseErrors(new List<string[]>
			{
				Header(),
				Row("group", "value", type, value),
			});

			Assert.That(errors, Has.Some.Contains("must contain exactly"));
		}

		[Test]
		public void parse_rejects_orphan_field_before_first_group()
		{
			var errors = ParseErrors(new List<string[]>
			{
				Header(),
				Row("", "count", "int", "1"),
			});

			Assert.That(errors, Has.Some.Contains("no Sub Class before a group"));
		}

		[Test]
		public void parse_rejects_duplicate_group_key()
		{
			var errors = ParseErrors(new List<string[]>
			{
				Header(),
				Row("economy", "a", "int", "1"),
				Row("economy", "b", "int", "2"),
			});

			Assert.That(errors, Has.Some.Contains("duplicate Sub Class 'economy'"));
		}

		[Test]
		public void parse_rejects_group_class_name_collision()
		{
			var errors = ParseErrors(new List<string[]>
			{
				Header(),
				Row("economy", "a", "int", "1"),
				Row("Economy", "b", "int", "2"),
			});

			Assert.That(errors, Has.Some.Contains("generated class 'Economy'"));
		}

		[Test]
		public void parse_rejects_group_class_colliding_with_root_type()
		{
			var errors = ParseErrors(new List<string[]>
			{
				Header(),
				Row("gameDataConfig", "a", "int", "1"),
			});

			Assert.That(errors, Has.Some.Contains("generated type 'GameDataConfig'"));
		}

		[Test]
		public void parse_rejects_duplicate_group_field()
		{
			var errors = ParseErrors(new List<string[]>
			{
				Header(),
				Row("economy", "a", "int", "1"),
				Row("", "a", "int", "2"),
			});

			Assert.That(errors, Has.Some.Contains("duplicate field 'a'"));
		}

		[Test]
		public void parse_rejects_duplicate_root_field()
		{
			var errors = ParseErrors(new List<string[]>
			{
				Header(),
				Row("economy", "a", "int", "1"),
				Row("", "", "", ""),
				Row("", "b", "int", "1"),
				Row("", "b", "int", "2"),
			});

			Assert.That(errors, Has.Some.Contains("duplicate root field 'b'"));
		}

		[Test]
		public void parse_rejects_root_field_colliding_with_group()
		{
			var errors = ParseErrors(new List<string[]>
			{
				Header(),
				Row("economy", "a", "int", "1"),
				Row("", "", "", ""),
				Row("", "economy", "int", "2"),
			});

			Assert.That(errors, Has.Some.Contains("conflicts with a Sub Class"));
		}

		[Test]
		public void parse_rejects_missing_field_name_or_type()
		{
			var errors = ParseErrors(new List<string[]>
			{
				Header(),
				Row("economy", "", "int", "1"),
				Row("economy2", "a", "", "1"),
			});

			Assert.That(errors, Has.Some.Contains("Field Name is required"));
			Assert.That(errors, Has.Some.Contains("Type is required"));
		}

		[Test]
		public void emit_json_writes_nested_object_in_source_order()
		{
			// Values are asserted by the JsonUtility round-trip test; this one owns nesting and key order.
			// Float spelling ("2.0" vs "2") is Newtonsoft's business, so it is not asserted.
			string json = SheetXConfigSheet.EmitJson(ParseValidConfig());

			Assert.That(json, Does.StartWith(
				"{\"economy\":{\"startingCoins\":1000,\"rates\":[0.5,0.25],\"enabled\":true},\"position2\":{\"x\":"));
			Assert.That(json.IndexOf("\"position3\"", StringComparison.Ordinal),
				Is.GreaterThan(json.IndexOf("\"position2\"", StringComparison.Ordinal)));
			Assert.That(json, Does.Not.Contain("\"rates[]\""));
		}

		[Test]
		public void emit_csharp_writes_nested_types_load_method_and_namespace()
		{
			string source = SheetXConfigSheet.EmitCSharp(ParseValidConfig(), "GameDataConfig", "Game.Data");

			Assert.That(source, Does.Contain("namespace Game.Data"));
			Assert.That(source, Does.Contain("public class GameDataConfig : ScriptableObject"));
			Assert.That(source, Does.Contain("public class Economy"));
			Assert.That(source, Does.Contain("public int startingCoins;"));
			Assert.That(source, Does.Contain("public float[] rates;"));
			Assert.That(source, Does.Contain("public bool enabled;"));
			Assert.That(source, Does.Contain("public Economy economy;"));
			Assert.That(source, Does.Contain("public Vector2 position2;"));
			Assert.That(source, Does.Contain("public Vector3 position3;"));
			Assert.That(source, Does.Contain("[SerializeField] private TextAsset configJson;"));
			Assert.That(source, Does.Contain("[SerializeField] private bool autoLoad = true;"));
			Assert.That(source, Does.Contain("[ContextMenu(\"Load\")]"));
			Assert.That(source, Does.Contain("JsonUtility.FromJsonOverwrite(configJson.text, this);"));
			Assert.That(source, Does.Contain("UnityEditor.AssetDatabase.SaveAssetIfDirty(this);"));
			Assert.That(source, Does.Contain("[CreateAssetMenu(fileName = \"GameDataConfig\", menuName = \"SheetX/GameDataConfig\")]"));
			Assert.That(source, Does.Not.Contain("Newtonsoft"));
		}

		[Test]
		public void emit_csharp_uses_crlf_only()
		{
			string source = SheetXConfigSheet.EmitCSharp(ParseValidConfig(), "GameDataConfig", "");

			Assert.That(source.Replace("\r\n", ""), Does.Not.Contain("\n"));
		}

		[Test]
		public void emitted_config_json_loads_with_json_utility()
		{
			string json = SheetXConfigSheet.EmitJson(ParseValidConfig());
			var asset = ScriptableObject.CreateInstance<ConfigShape>();
			try
			{
				JsonUtility.FromJsonOverwrite(json, asset);

				Assert.That(asset.economy.startingCoins, Is.EqualTo(1000));
				Assert.That(asset.economy.rates, Is.EqualTo(new[] { 0.5f, 0.25f }));
				Assert.That(asset.economy.enabled, Is.True);
				Assert.That(asset.position2, Is.EqualTo(new Vector2(1f, 2f)));
				Assert.That(asset.position3, Is.EqualTo(new Vector3(1f, 2f, 3f)));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(asset);
			}
		}

		// The detached exporter has no Config route at all: SheetXExportRequest never carries the setting,
		// so an exact "Config" sheet must keep producing the same row-array artifact it always did.
		[TestCase("Config")]
		[TestCase("RemoteConfig")]
		[TestCase("config")]
		public void detached_config_sheet_stays_row_array_json(string sheetName)
		{
			string path = CreateConfigWorkbook(sheetName);
			try
			{
				var output = new MemoryOutput();
				var result = SheetXExporter.ExportExcel(new SheetXExportRequest
				{
					SpreadsheetPath = path,
					JsonOutputPath = "Generated",
				}, output);

				Assert.That(result.Success, Is.True);
				Assert.That(output.Writes[$"Generated/{sheetName}.txt"], Does.StartWith("["));
				Assert.That(output.Writes.Keys, Has.None.EndsWith(".cs"));
			}
			finally
			{
				File.Delete(path);
			}
		}

		[Test]
		public void detached_combine_json_still_merges_config_sheet()
		{
			string path = CreateConfigWorkbook("Config");
			try
			{
				var output = new MemoryOutput();
				var result = SheetXExporter.ExportExcel(new SheetXExportRequest
				{
					SpreadsheetPath = path,
					JsonOutputPath = "Generated",
					CombineJson = true,
				}, output);

				Assert.That(result.Success, Is.True);
				Assert.That(output.Writes, Has.Count.EqualTo(1));
				Assert.That(output.Writes.Values.Single(), Does.Contain("\"Config\":["));
			}
			finally
			{
				File.Delete(path);
			}
		}

		private static string CreateConfigWorkbook(string sheetName)
		{
			string path = Path.Combine(Path.GetTempPath(), $"sheetx-{Guid.NewGuid():N}.xlsx");
			var workbook = new XSSFWorkbook();
			var sheet = workbook.CreateSheet(sheetName);
			var header = sheet.CreateRow(0);
			var row = sheet.CreateRow(1);
			string[] headers = { "Sub Class", "Field Name", "Type", "Value" };
			string[] values = { "economy", "startingCoins", "int", "1000" };
			for (int i = 0; i < headers.Length; i++)
			{
				header.CreateCell(i).SetCellValue(headers[i]);
				row.CreateCell(i).SetCellValue(values[i]);
			}
			using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
			workbook.Write(stream);
			return path;
		}

		private sealed class MemoryOutput : ISheetXOutput
		{
			public readonly Dictionary<string, string> Writes = new Dictionary<string, string>();

			public void Write(string relativePath, string content)
			{
				Writes.Add(relativePath, content);
			}
		}

		private static ConfigSheetData ParseValidConfig()
		{
			bool ok = SheetXConfigSheet.TryParse(new List<string[]>
			{
				Header(),
				Row("economy", "startingCoins", "int", "1000"),
				Row("", "rates[]", "float-array", "0.5|0.25"),
				Row("", "enabled", "boolean", "true"),
				Row("", "", "", ""),
				Row("", "position2", "vector2", "1|2"),
				Row("", "position3", "vector3", "1|2|3"),
			}, "GameDataConfig", Fail, out var data);

			Assert.That(ok, Is.True);
			return data;
		}

		private static List<string> ParseErrors(List<string[]> table, string rootTypeName = "GameDataConfig")
		{
			var errors = new List<string>();
			bool ok = SheetXConfigSheet.TryParse(table, rootTypeName, errors.Add, out var data);

			Assert.That(ok, Is.False);
			Assert.That(data, Is.Null);
			Assert.That(errors, Is.Not.Empty);
			return errors;
		}

		private static string[] Header() => Row("Sub Class", "Field Name", "Type", "Value");

		private static string[] Row(string subClass, string fieldName, string type, string value)
			=> new[] { subClass, fieldName, type, value };

		private static void Fail(string message) => Assert.Fail(message);

		[Serializable]
		private class Economy
		{
			public int startingCoins;
			public float[] rates;
			public bool enabled;
		}

		private class ConfigShape : ScriptableObject
		{
			public Economy economy;
			public Vector2 position2;
			public Vector3 position3;
		}
	}
}
