/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RCore.SheetX.Editor
{
	internal enum ConfigFieldType
	{
		Int,
		Float,
		Boolean,
		String,
		IntArray,
		FloatArray,
		StringArray,
		Vector2,
		Vector3,
	}

	internal sealed class ConfigField
	{
		public string Name;
		public ConfigFieldType Type;
		public object Value;
	}

	internal sealed class ConfigGroup
	{
		public string Key;
		public string ClassName;
		public List<ConfigField> Fields = new List<ConfigField>();
	}

	internal sealed class ConfigSheetData
	{
		public List<ConfigGroup> Groups = new List<ConfigGroup>();
		public List<ConfigField> RootFields = new List<ConfigField>();
	}

	internal static class SheetXConfigSheet
	{
		private static readonly string[] s_expectedHeader = { "Sub Class", "Field Name", "Type", "Value" };
		// Reserved keywords only. Contextual keywords (value, group, var, from, ...) are legal identifiers
		// and a Config sheet is expected to use words like 'value' and 'group' as field names.
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

		internal static bool TryParse(
			IReadOnlyList<string[]> table,
			string rootTypeName,
			Action<string> onError,
			out ConfigSheetData data)
		{
			data = null;
			int errorCount = 0;
			Action<string> error = message =>
			{
				errorCount++;
				onError?.Invoke(message);
			};

			if (table == null || table.Count == 0)
			{
				error("Config sheet is missing its header row.");
				return false;
			}

			if (!IsExpectedHeader(table[0]))
			{
				error("Config header must be: Sub Class, Field Name, Type, Value.");
				return false;
			}

			if (!IsValidIdentifier(rootTypeName))
				error($"Config generated type name '{rootTypeName}' is not a valid C# identifier.");

			var result = new ConfigSheetData();
			var groupKeys = new HashSet<string>(StringComparer.Ordinal);
			var groupClasses = new HashSet<string>(StringComparer.Ordinal);
			var rootNames = new HashSet<string>(StringComparer.Ordinal);
			var groupFieldNames = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
			ConfigGroup currentGroup = null;
			bool rootFieldMode = false;

			for (int rowIndex = 1; rowIndex < table.Count; rowIndex++)
			{
				string[] row = ReadRow(table[rowIndex]);
				if (row.All(string.IsNullOrEmpty))
				{
					currentGroup = null;
					rootFieldMode = true;
					continue;
				}

				int spreadsheetRow = rowIndex + 1;
				string groupKey = row[0];
				string fieldName = StripArraySuffix(row[1]);
				string typeName = row[2];
				string value = row[3];

				if (string.IsNullOrEmpty(fieldName))
				{
					error($"Config row {spreadsheetRow}: Field Name is required.");
					continue;
				}
				if (string.IsNullOrEmpty(typeName))
				{
					error($"Config row {spreadsheetRow}: Type is required for field '{fieldName}'.");
					continue;
				}
				if (!IsValidIdentifier(fieldName))
				{
					error($"Config row {spreadsheetRow}: field '{fieldName}' is not a valid C# identifier.");
					continue;
				}
				if (!TryParseFieldType(typeName, out ConfigFieldType fieldType))
				{
					error($"Config row {spreadsheetRow}: type '{typeName}' is not supported.");
					continue;
				}
				if (!TryParseValue(fieldType, value, out object parsedValue, out string parseError))
				{
					error($"Config row {spreadsheetRow}: field '{fieldName}' {parseError}.");
					continue;
				}

				if (!string.IsNullOrEmpty(groupKey))
				{
					if (!IsValidIdentifier(groupKey))
					{
						error($"Config row {spreadsheetRow}: Sub Class '{groupKey}' is not a valid C# identifier.");
						continue;
					}

					string className = UppercaseFirst(groupKey);
					// A rejected group must not open: its fields would otherwise land in the previous
					// group and produce a second, misleading error per row.
					if (!groupKeys.Add(groupKey))
					{
						error($"Config row {spreadsheetRow}: duplicate Sub Class '{groupKey}'.");
						currentGroup = null;
						rootFieldMode = false;
						continue;
					}
					if (!groupClasses.Add(className))
					{
						error($"Config row {spreadsheetRow}: Sub Class '{groupKey}' conflicts with generated class '{className}'.");
						currentGroup = null;
						rootFieldMode = false;
						continue;
					}
					if (string.Equals(className, rootTypeName, StringComparison.Ordinal))
					{
						error($"Config row {spreadsheetRow}: Sub Class '{groupKey}' conflicts with generated type '{rootTypeName}'.");
						currentGroup = null;
						rootFieldMode = false;
						continue;
					}
					if (rootNames.Contains(groupKey))
					{
						error($"Config row {spreadsheetRow}: Sub Class '{groupKey}' conflicts with a root field.");
						currentGroup = null;
						rootFieldMode = false;
						continue;
					}

					currentGroup = new ConfigGroup { Key = groupKey, ClassName = className };
					result.Groups.Add(currentGroup);
					groupFieldNames[groupKey] = new HashSet<string>(StringComparer.Ordinal);
					rootFieldMode = false;
				}

				var field = new ConfigField { Name = fieldName, Type = fieldType, Value = parsedValue };
				if (currentGroup != null && !rootFieldMode)
				{
					if (!groupFieldNames[currentGroup.Key].Add(fieldName))
						error($"Config row {spreadsheetRow}: duplicate field '{fieldName}' in Sub Class '{currentGroup.Key}'.");
					else
						currentGroup.Fields.Add(field);
				}
				else if (rootFieldMode)
				{
					if (!rootNames.Add(fieldName))
						error($"Config row {spreadsheetRow}: duplicate root field '{fieldName}'.");
					else if (groupKeys.Contains(fieldName))
						error($"Config row {spreadsheetRow}: root field '{fieldName}' conflicts with a Sub Class.");
					else
						result.RootFields.Add(field);
				}
				else
				{
					error($"Config row {spreadsheetRow}: field '{fieldName}' has no Sub Class before a group.");
				}
			}

			if (errorCount != 0)
				return false;

			data = result;
			return true;
		}

		internal static string EmitJson(ConfigSheetData data)
		{
			var root = new JObject();
			foreach (ConfigGroup group in data.Groups)
			{
				var groupJson = new JObject();
				foreach (ConfigField field in group.Fields)
					groupJson.Add(field.Name, ToJsonToken(field.Value));
				root.Add(group.Key, groupJson);
			}
			foreach (ConfigField field in data.RootFields)
				root.Add(field.Name, ToJsonToken(field.Value));
			return root.ToString(Formatting.None);
		}

		internal static string EmitCSharp(ConfigSheetData data, string typeName, string @namespace)
		{
			const string indent = "\t";
			string nl = "\r\n";
			var source = new StringBuilder();
			source.Append("using System;").Append(nl);
			source.Append("using UnityEngine;").Append(nl).Append(nl);
			source.Append("[CreateAssetMenu(fileName = \"").Append(typeName).Append("\", menuName = \"SheetX/").Append(typeName).Append("\")]").Append(nl);
			source.Append("public class ").Append(typeName).Append(" : ScriptableObject").Append(nl);
			source.Append('{').Append(nl);
			foreach (ConfigGroup group in data.Groups)
			{
				source.Append(indent).Append("[Serializable]").Append(nl);
				source.Append(indent).Append("public class ").Append(group.ClassName).Append(nl);
				source.Append(indent).Append('{').Append(nl);
				foreach (ConfigField field in group.Fields)
					source.Append(indent).Append(indent).Append("public ").Append(ToCSharpType(field.Type)).Append(' ').Append(field.Name).Append(';').Append(nl);
				source.Append(indent).Append('}').Append(nl).Append(nl);
			}
			foreach (ConfigGroup group in data.Groups)
				source.Append(indent).Append("public ").Append(group.ClassName).Append(' ').Append(group.Key).Append(';').Append(nl);
			foreach (ConfigField field in data.RootFields)
				source.Append(indent).Append("public ").Append(ToCSharpType(field.Type)).Append(' ').Append(field.Name).Append(';').Append(nl);
			if (data.Groups.Count != 0 || data.RootFields.Count != 0)
				source.Append(nl);
			source.Append(indent).Append("[SerializeField] private TextAsset configJson;").Append(nl);
			source.Append(indent).Append("[SerializeField] private bool autoLoad = true;").Append(nl).Append(nl);
			source.Append(indent).Append("[ContextMenu(\"Load\")]").Append(nl);
			source.Append(indent).Append("public void Load()").Append(nl);
			source.Append(indent).Append('{').Append(nl);
			source.Append(indent).Append(indent).Append("if (configJson == null)").Append(nl);
			source.Append(indent).Append(indent).Append('{').Append(nl);
			source.Append(indent).Append(indent).Append(indent).Append("Debug.LogError(\"Config JSON is not assigned.\", this);").Append(nl);
			source.Append(indent).Append(indent).Append(indent).Append("return;").Append(nl);
			source.Append(indent).Append(indent).Append('}').Append(nl).Append(nl);
			source.Append(indent).Append(indent).Append("JsonUtility.FromJsonOverwrite(configJson.text, this);").Append(nl).Append(nl);
			source.Append("#if UNITY_EDITOR").Append(nl);
			source.Append(indent).Append(indent).Append("UnityEditor.EditorUtility.SetDirty(this);").Append(nl);
			source.Append(indent).Append(indent).Append("UnityEditor.AssetDatabase.SaveAssetIfDirty(this);").Append(nl);
			source.Append("#endif").Append(nl);
			source.Append(indent).Append('}').Append(nl);
			source.Append('}').Append(nl);
			return SheetXHelper.AddNamespace(source.ToString(), @namespace).Replace("\r\n", "\n").Replace("\n", "\r\n");
		}

		private static bool IsExpectedHeader(string[] header)
		{
			if (header == null || header.Length < s_expectedHeader.Length)
				return false;
			for (int i = 0; i < s_expectedHeader.Length; i++)
			{
				if (!string.Equals(header[i]?.Trim(), s_expectedHeader[i], StringComparison.Ordinal))
					return false;
			}
			return true;
		}

		private static string[] ReadRow(string[] row)
		{
			var values = new string[4];
			for (int i = 0; i < values.Length; i++)
				values[i] = i < (row?.Length ?? 0) ? row[i]?.Trim() ?? "" : "";
			return values;
		}

		private static string StripArraySuffix(string value)
		{
			return value.EndsWith("[]", StringComparison.Ordinal) ? value.Substring(0, value.Length - 2) : value;
		}

		private static string UppercaseFirst(string value)
		{
			return char.ToUpper(value[0], CultureInfo.InvariantCulture) + value.Substring(1);
		}

		private static bool TryParseFieldType(string typeName, out ConfigFieldType fieldType)
		{
			switch (typeName.ToLowerInvariant())
			{
				case "int": fieldType = ConfigFieldType.Int; return true;
				case "float": fieldType = ConfigFieldType.Float; return true;
				case "boolean": fieldType = ConfigFieldType.Boolean; return true;
				case "string": fieldType = ConfigFieldType.String; return true;
				case "int-array": fieldType = ConfigFieldType.IntArray; return true;
				case "float-array": fieldType = ConfigFieldType.FloatArray; return true;
				case "string-array": fieldType = ConfigFieldType.StringArray; return true;
				case "vector2": fieldType = ConfigFieldType.Vector2; return true;
				case "vector3": fieldType = ConfigFieldType.Vector3; return true;
				default: fieldType = default; return false;
			}
		}

		private static bool TryParseValue(ConfigFieldType type, string value, out object parsed, out string error)
		{
			parsed = null;
			error = null;
			switch (type)
			{
				case ConfigFieldType.Int:
					if (SheetXHelper.TryParseInt(value, out int intValue)) { parsed = intValue; return true; }
					break;
				case ConfigFieldType.Float:
					if (TryParseFiniteFloat(value, out float floatValue)) { parsed = floatValue; return true; }
					break;
				case ConfigFieldType.Boolean:
					if (bool.TryParse(value, out bool boolValue)) { parsed = boolValue; return true; }
					break;
				case ConfigFieldType.String:
					parsed = value;
					return true;
				case ConfigFieldType.IntArray:
					if (TryParseArray(value, SheetXHelper.TryParseInt, out int[] intValues)) { parsed = intValues; return true; }
					break;
				case ConfigFieldType.FloatArray:
					if (TryParseArray(value, TryParseFiniteFloat, out float[] floatValues)) { parsed = floatValues; return true; }
					break;
				case ConfigFieldType.StringArray:
					parsed = SheetXHelper.SplitValueToArray(value, false);
					return true;
				case ConfigFieldType.Vector2:
					if (TryParseVector(value, 2, out float[] vector2)) { parsed = new ConfigVector2(vector2[0], vector2[1]); return true; }
					error = "must contain exactly 2 finite float values";
					return false;
				case ConfigFieldType.Vector3:
					if (TryParseVector(value, 3, out float[] vector3)) { parsed = new ConfigVector3(vector3[0], vector3[1], vector3[2]); return true; }
					error = "must contain exactly 3 finite float values";
					return false;
			}
			error = $"has invalid {ToCSharpType(type)} value '{value}'";
			return false;
		}

		private static bool TryParseArray<T>(string value, TryParser<T> parser, out T[] values)
		{
			string[] parts = SheetXHelper.SplitValueToArray(value, false);
			values = new T[parts.Length];
			for (int i = 0; i < parts.Length; i++)
			{
				if (!parser(parts[i], out values[i]))
					return false;
			}
			return true;
		}

		private static bool TryParseVector(string value, int count, out float[] values)
		{
			return TryParseArray(value, TryParseFiniteFloat, out values) && values.Length == count;
		}

		private static bool TryParseFiniteFloat(string value, out float result)
		{
			return SheetXHelper.TryParseFloat(value, out result) && !float.IsNaN(result) && !float.IsInfinity(result);
		}

		private static JToken ToJsonToken(object value)
		{
			if (value is ConfigVector2 vector2)
				return new JObject { ["x"] = vector2.X, ["y"] = vector2.Y };
			if (value is ConfigVector3 vector3)
				return new JObject { ["x"] = vector3.X, ["y"] = vector3.Y, ["z"] = vector3.Z };
			if (value is Array array)
			{
				var result = new JArray();
				foreach (object item in array)
					result.Add(new JValue(item));
				return result;
			}
			return new JValue(value);
		}

		private static string ToCSharpType(ConfigFieldType type)
		{
			switch (type)
			{
				case ConfigFieldType.Int: return "int";
				case ConfigFieldType.Float: return "float";
				case ConfigFieldType.Boolean: return "bool";
				case ConfigFieldType.String: return "string";
				case ConfigFieldType.IntArray: return "int[]";
				case ConfigFieldType.FloatArray: return "float[]";
				case ConfigFieldType.StringArray: return "string[]";
				case ConfigFieldType.Vector2: return "Vector2";
				case ConfigFieldType.Vector3: return "Vector3";
				default: throw new ArgumentOutOfRangeException(nameof(type));
			}
		}

		private static bool IsValidIdentifier(string value)
		{
			if (string.IsNullOrEmpty(value) || s_reservedKeywords.Contains(value))
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

		private delegate bool TryParser<T>(string value, out T result);

		private readonly struct ConfigVector2
		{
			public readonly float X;
			public readonly float Y;

			public ConfigVector2(float x, float y)
			{
				X = x;
				Y = y;
			}
		}

		private readonly struct ConfigVector3
		{
			public readonly float X;
			public readonly float Y;
			public readonly float Z;

			public ConfigVector3(float x, float y, float z)
			{
				X = x;
				Y = y;
				Z = z;
			}
		}
	}
}
