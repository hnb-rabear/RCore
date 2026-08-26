/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RCore.SheetX.Editor
{
	internal enum SheetXCollectionScalarType
	{
		Int,
		Float,
		Bool,
		String,
	}

	internal sealed class SheetXCollectionColumn
	{
		internal string Header;
		internal int SourceColumnIndex;
		internal IReadOnlyList<string> Path;
		internal SheetXCollectionScalarType ScalarType;
		internal bool IsArray;
		internal string FieldName => Path[Path.Count - 1];
		internal string TypeName => ScalarType.ToString().ToLowerInvariant() + (IsArray ? "[]" : "");
	}

	internal sealed class SheetXCollectionObject
	{
		internal IReadOnlyList<string> Path;
		internal string FieldName => Path[Path.Count - 1];
		internal string TypeName => SheetXCollectionNaming.ToPascalIdentifier(FieldName);
	}

	internal sealed class SheetXCollectionSchema
	{
		internal string RowTypeName;
		internal IReadOnlyList<SheetXCollectionColumn> Columns;
		internal IReadOnlyList<SheetXCollectionObject> Objects;
	}

	internal static class SheetXCollectionNaming
	{
		private static readonly HashSet<string> s_reservedKeywords = new HashSet<string>(StringComparer.Ordinal)
		{
			"abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
			"continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
			"false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
			"internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
			"private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc",
			"static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
			"unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
		};

		internal static bool IsReservedKeyword(string value)
			=> !string.IsNullOrEmpty(value) && s_reservedKeywords.Contains(value);

		internal static bool IsValidIdentifier(string value)
		{
			if (string.IsNullOrEmpty(value) || IsReservedKeyword(value))
				return false;
			if (value[0] != '_' && !char.IsLetter(value[0]))
				return false;
			for (int i = 1; i < value.Length; i++)
			{
				if (value[i] != '_' && !char.IsLetterOrDigit(value[i]))
					return false;
			}
			return true;
		}

		internal static string ToPascalIdentifier(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return "";

			var words = new List<string>();
			int start = -1;
			for (int i = 0; i <= value.Length; i++)
			{
				bool separator = i == value.Length || !char.IsLetterOrDigit(value[i]);
				if (!separator && start < 0)
					start = i;
				if (!separator || start < 0)
					continue;
				words.Add(value.Substring(start, i - start));
				start = -1;
			}
			if (words.Count == 0)
				return "";

			string result = string.Concat(words.Select(UppercaseFirst));
			if (char.IsDigit(result[0]))
				result = "_" + result;
			return s_reservedKeywords.Contains(result) ? "_" + result : result;
		}

		internal static string ToCamelIdentifier(string value)
		{
			string result = ToPascalIdentifier(value);
			if (string.IsNullOrEmpty(result) || result[0] == '_')
				return result;
			return char.ToLowerInvariant(result[0]) + result.Substring(1);
		}

		internal static string RowTypeName(string sheetName) => ToPascalIdentifier(sheetName) + "SX";

		internal static string CollectionTypeName(string collectionName)
		{
			return string.Equals(collectionName, SheetXCollectionSettings.GlobalName, StringComparison.Ordinal)
				? "GlobalConfigCollection"
				: ToPascalIdentifier(collectionName) + "ConfigCollection";
		}

		internal static string NormalizeFileName(string sheetName)
		{
			return (sheetName ?? "").Trim().Replace(" ", "_");
		}

		private static string UppercaseFirst(string value)
		{
			return char.ToUpperInvariant(value[0]) + value.Substring(1);
		}
	}

	internal static class SheetXCollectionSchemaParser
	{
		internal static bool TryParse(
			IReadOnlyList<string> headers,
			string rowTypeName,
			out SheetXCollectionSchema schema,
			out string error)
		{
			if (headers != null && headers.Any(RequiresRowsForInference))
			{
				schema = null;
				error = "Rows are required to infer types for unannotated headers.";
				return false;
			}
			return TryParse(headers, null, rowTypeName, out schema, out _, out error);
		}

		internal static bool TryParse(
			IReadOnlyList<string> headers,
			IReadOnlyList<IReadOnlyList<string>> rows,
			string rowTypeName,
			out SheetXCollectionSchema schema,
			out string error)
		{
			return TryParse(headers, rows, rowTypeName, out schema, out _, out error);
		}

		internal static bool TryParse(
			IReadOnlyList<string> headers,
			IReadOnlyList<IReadOnlyList<string>> rows,
			string rowTypeName,
			out SheetXCollectionSchema schema,
			out IReadOnlyList<string> warnings,
			out string error)
		{
			schema = null;
			error = null;
			var warningList = new List<string>();
			warnings = warningList;
			if (!SheetXCollectionNaming.IsValidIdentifier(rowTypeName))
			{
				error = $"Row type '{rowTypeName}' is not a valid C# identifier. Fix: rename the sheet so its generated Data Class name is valid.";
				return false;
			}
			if (headers == null || headers.Count == 0)
			{
				error = "Generated Data Class requires at least one header. Fix: add a field header to row 1.";
				return false;
			}

			var columns = new List<SheetXCollectionColumn>();
			var normalizedPaths = new List<IReadOnlyList<string>>();
			for (int i = 0; i < headers.Count; i++)
			{
				string header = headers[i]?.Trim() ?? "";
				if (header.IndexOf("[x]", StringComparison.Ordinal) >= 0)
					continue;
				if (!TryParseHeader(header, rows, i, out SheetXCollectionColumn column,
					out string keyword, out error))
				{
					error = $"Header '{header}' (column {i + 1}): {error}";
					return false;
				}
				if (keyword != null)
				{
					warningList.Add(
						$"Header '{header}' (column {i + 1}) was skipped because '{keyword}' is a C# keyword. "
						+ "Fix: rename the keyword path segment to a valid C# field name, or add [x] to mark the column ignored.");
					continue;
				}

				foreach (var existing in normalizedPaths)
				{
					int shared = Math.Min(existing.Count, column.Path.Count);
					bool samePrefix = true;
					for (int p = 0; p < shared; p++)
					{
						if (!string.Equals(existing[p], column.Path[p], StringComparison.Ordinal))
						{
							samePrefix = false;
							break;
						}
					}
					if (!samePrefix)
						continue;
					if (existing.Count == column.Path.Count)
					{
						error = $"Header '{header}' (column {i + 1}): duplicate normalized field path '{string.Join(".", column.Path)}'. Fix: rename or mark one duplicate column with [x].";
						return false;
					}
					if (shared == existing.Count || shared == column.Path.Count)
					{
						error = $"Header '{header}' (column {i + 1}): object/leaf conflict at '{string.Join(".", column.Path.Take(shared))}'. Fix: rename one path so a field is not both a value and an object.";
						return false;
					}
				}

				columns.Add(column);
				normalizedPaths.Add(column.Path);
			}

			if (columns.Count == 0)
			{
				error = "Generated Data Class has no usable generated fields. Fix: add a valid non-keyword header without [x].";
				return false;
			}
			var objects = BuildObjects(columns, rowTypeName, out error);
			if (objects == null)
				return false;
			schema = new SheetXCollectionSchema
			{
				RowTypeName = rowTypeName,
				Columns = columns,
				Objects = objects,
			};
			return true;
		}

		internal static bool TryBuildRows(
			SheetXCollectionSchema schema,
			IReadOnlyList<IReadOnlyList<string>> rows,
			out string json,
			out string error)
		{
			return TryBuildRows(schema, rows, new[] { "id", "key" }, out json, out error);
		}

		internal static bool TryBuildRows(
			SheetXCollectionSchema schema,
			IReadOnlyList<IReadOnlyList<string>> rows,
			IEnumerable<string> persistentFields,
			out string json,
			out string error)
		{
			json = null;
			error = null;
			if (schema == null)
			{
				error = "Schema is missing.";
				return false;
			}

			var persistent = new HashSet<string>(persistentFields ?? Array.Empty<string>(), StringComparer.Ordinal);
			var output = new JArray();
			for (int rowIndex = 0; rowIndex < (rows?.Count ?? 0); rowIndex++)
			{
				var source = rows[rowIndex];
				var row = new JObject();
				for (int columnIndex = 0; columnIndex < schema.Columns.Count; columnIndex++)
				{
					SheetXCollectionColumn column = schema.Columns[columnIndex];
					string value = column.SourceColumnIndex < (source?.Count ?? 0)
						? source[column.SourceColumnIndex] ?? ""
						: "";
					string fieldPath = string.Join(".", column.Path);
					if (string.IsNullOrEmpty(value))
					{
						if (column.IsArray || !persistent.Contains(fieldPath))
							continue;
						SetToken(row, column.Path, DefaultToken(column.ScalarType));
						continue;
					}

					if (!TryParseToken(column, value, out JToken token, out string parseError))
					{
						error = $"Row {rowIndex + 1}, header '{column.Header}' (column {column.SourceColumnIndex + 1}): {parseError} Fix: correct the cell value or override the header type.";
						return false;
					}
					SetToken(row, column.Path, token);
				}
				output.Add(row);
			}
			json = output.ToString(Formatting.None);
			return true;
		}

		private static bool RequiresRowsForInference(string value)
		{
			string header = value?.Trim() ?? "";
			if (header.IndexOf("[x]", StringComparison.Ordinal) >= 0 || header.IndexOf(':') >= 0)
				return false;
			string rawPath = header.EndsWith("[]", StringComparison.Ordinal)
				? header.Substring(0, header.Length - 2)
				: header;
			return !rawPath.Split('.').Any(SheetXCollectionNaming.IsReservedKeyword);
		}

		private static bool TryParseHeader(
			string header,
			IReadOnlyList<IReadOnlyList<string>> rows,
			int columnIndex,
			out SheetXCollectionColumn column,
			out string keyword,
			out string error)
		{
			column = null;
			keyword = null;
			error = null;
			int colon = header.IndexOf(':');
			if (colon != header.LastIndexOf(':') || colon == 0 || colon == header.Length - 1)
			{
				error = "expected '<path>' or '<path>:<type>'. Fix: remove extra ':' characters or provide both path and type.";
				return false;
			}

			string rawPath = colon < 0 ? header : header.Substring(0, colon);
			string typeName = colon < 0 ? null : header.Substring(colon + 1);
			bool isArray = rawPath.EndsWith("[]", StringComparison.Ordinal);
			if (isArray)
				rawPath = rawPath.Substring(0, rawPath.Length - 2);
			if (rawPath.IndexOfAny(new[] { '[', ']' }) >= 0)
			{
				error = "'[]' is allowed only after the leaf name. Fix: move [] to the end of the path or add [x] to ignore the column.";
				return false;
			}

			string[] rawSegments = rawPath.Split('.');
			keyword = rawSegments.FirstOrDefault(SheetXCollectionNaming.IsReservedKeyword);
			if (keyword != null)
				return true;
			if (rawSegments.Length == 0 || rawSegments.Any(s => !SheetXCollectionNaming.IsValidIdentifier(s)))
			{
				error = $"path '{rawPath}' contains an invalid C# identifier. Fix: use letters, digits, or underscores, starting with a letter or underscore; or add [x] to ignore the column.";
				return false;
			}
			var path = rawSegments.Select(SheetXCollectionNaming.ToCamelIdentifier).ToArray();

			SheetXCollectionScalarType scalarType;
			if (typeName == null)
			{
				scalarType = InferType(rows, columnIndex, isArray);
			}
			else if (!TryParseType(typeName, out scalarType))
			{
				error = $"type '{typeName}' is not supported. Fix: use int, float, bool, or string; or remove the annotation to infer the type.";
				return false;
			}
			column = new SheetXCollectionColumn
			{
				Header = header,
				SourceColumnIndex = columnIndex,
				Path = path,
				ScalarType = scalarType,
				IsArray = isArray,
			};
			return true;
		}

		private static SheetXCollectionScalarType InferType(
			IReadOnlyList<IReadOnlyList<string>> rows, int columnIndex, bool isArray)
		{
			string longest = "";
			for (int rowIndex = 0; rowIndex < (rows?.Count ?? 0); rowIndex++)
			{
				var row = rows[rowIndex];
				string value = columnIndex < (row?.Count ?? 0) ? row[columnIndex]?.Trim() ?? "" : "";
				if (value.Length > longest.Length)
					longest = value;
			}
			if (isArray)
			{
				string longestItem = "";
				foreach (string value in SheetXHelper.SplitValueToArray(longest, false))
				{
					if (value.Length > longestItem.Length)
						longestItem = value;
				}
				longest = longestItem;
			}
			if (SheetXHelper.TryParseInt(longest, out _))
				return SheetXCollectionScalarType.Int;
			if (SheetXHelper.TryParseFloat(longest, out float floatValue)
				&& !float.IsNaN(floatValue) && !float.IsInfinity(floatValue))
			{
				return SheetXCollectionScalarType.Float;
			}
			return bool.TryParse(longest, out _)
				? SheetXCollectionScalarType.Bool
				: SheetXCollectionScalarType.String;
		}

		private static IReadOnlyList<SheetXCollectionObject> BuildObjects(
			IReadOnlyList<SheetXCollectionColumn> columns, string rowTypeName, out string error)
		{
			error = null;
			var objects = new List<SheetXCollectionObject>();
			var paths = new HashSet<string>(StringComparer.Ordinal);
			var types = new Dictionary<string, string>(StringComparer.Ordinal)
			{
				[rowTypeName] = "<row>",
			};
			foreach (var column in columns)
			{
				for (int length = 1; length < column.Path.Count; length++)
				{
					string key = string.Join(".", column.Path.Take(length));
					if (!paths.Add(key))
						continue;
					var path = column.Path.Take(length).ToArray();
					string typeName = SheetXCollectionNaming.ToPascalIdentifier(path[path.Length - 1]);
					if (types.TryGetValue(typeName, out string existing) && !string.Equals(existing, key, StringComparison.Ordinal))
					{
						error = $"Header '{column.Header}' (column {column.SourceColumnIndex + 1}): nested object '{key}' collides with generated type '{typeName}'. Fix: rename the nested path or sheet so generated type names are unique.";
						return null;
					}
					types[typeName] = key;
					objects.Add(new SheetXCollectionObject { Path = path });
				}
			}
			return objects;
		}

		private static bool TryParseType(string value, out SheetXCollectionScalarType type)
		{
			switch (value)
			{
				case "int": type = SheetXCollectionScalarType.Int; return true;
				case "float": type = SheetXCollectionScalarType.Float; return true;
				case "bool": type = SheetXCollectionScalarType.Bool; return true;
				case "string": type = SheetXCollectionScalarType.String; return true;
				default: type = default; return false;
			}
		}

		private static bool TryParseToken(
			SheetXCollectionColumn column, string value, out JToken token, out string error)
		{
			if (!column.IsArray)
				return TryParseScalar(column.ScalarType, value, out token, out error);

			var array = new JArray();
			foreach (string part in SheetXHelper.SplitValueToArray(value, false))
			{
				if (!TryParseScalar(column.ScalarType, part, out JToken item, out error))
				{
					token = null;
					return false;
				}
				array.Add(item);
			}
			token = array;
			error = null;
			return true;
		}

		private static bool TryParseScalar(
			SheetXCollectionScalarType type, string value, out JToken token, out string error)
		{
			token = null;
			error = null;
			switch (type)
			{
				case SheetXCollectionScalarType.Int:
					if (SheetXHelper.TryParseInt(value, out int intValue))
					{
						token = new JValue(intValue);
						return true;
					}
					break;
				case SheetXCollectionScalarType.Float:
					if (SheetXHelper.TryParseFloat(value, out float floatValue)
						&& !float.IsNaN(floatValue) && !float.IsInfinity(floatValue))
					{
						token = new JValue(floatValue);
						return true;
					}
					break;
				case SheetXCollectionScalarType.Bool:
					if (bool.TryParse(value, out bool boolValue))
					{
						token = new JValue(boolValue);
						return true;
					}
					break;
				case SheetXCollectionScalarType.String:
					token = new JValue(value);
					return true;
				default:
					throw new ArgumentOutOfRangeException(nameof(type));
			}
			error = $"invalid {type.ToString().ToLowerInvariant()} value '{value}'.";
			return false;
		}

		private static JToken DefaultToken(SheetXCollectionScalarType type)
		{
			switch (type)
			{
				case SheetXCollectionScalarType.Int: return new JValue(0);
				case SheetXCollectionScalarType.Float: return new JValue(0f);
				case SheetXCollectionScalarType.Bool: return new JValue(false);
				case SheetXCollectionScalarType.String: return new JValue("");
				default: throw new ArgumentOutOfRangeException(nameof(type));
			}
		}

		private static void SetToken(JObject root, IReadOnlyList<string> path, JToken value)
		{
			JObject current = root;
			for (int i = 0; i < path.Count - 1; i++)
			{
				if (!(current[path[i]] is JObject child))
				{
					child = new JObject();
					current[path[i]] = child;
				}
				current = child;
			}
			current[path[path.Count - 1]] = value;
		}
	}
}
