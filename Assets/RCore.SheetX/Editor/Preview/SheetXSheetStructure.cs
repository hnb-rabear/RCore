/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RCore.SheetX.Editor
{
	/// <summary>
	/// One inferred top-level field, kept so the window's table and the draft's comments read the same
	/// record instead of re-deriving it. Built once during inference; never mutated afterwards.
	/// </summary>
	internal sealed class SheetXSheetField
	{
		/// <summary>The exact key as it appears in the exported JSON.</summary>
		internal string JsonName;

		/// <summary>The C# identifier the draft declares for it.</summary>
		internal string FieldName;

		/// <summary>The type the draft declares, including any fallback.</summary>
		internal string FieldType;

		/// <summary>
		/// Why inference could not name a real type, in the words inference itself used, or null when the
		/// type is a plain read of the observed values.
		/// </summary>
		internal string FallbackReason;

		/// <summary>True when the JSON key is not usable as a C# identifier, so the draft renames it.</summary>
		internal bool IsRenamed => !string.Equals(JsonName, FieldName, StringComparison.Ordinal);

		/// <summary>
		/// The declaration the draft emits for this field, attribute included. A renamed field carries its
		/// <c>JsonProperty</c> mapping, because the bare declaration would not receive the JSON key.
		/// </summary>
		internal string Declaration
		{
			get
			{
				string declaration = $"public {FieldType} {FieldName};";
				if (!IsRenamed)
					return declaration;
				string quoted = JsonConvert.ToString(JsonName, '"', StringEscapeHandling.EscapeNonAscii);
				return $"[global::Newtonsoft.Json.JsonProperty({quoted})]\r\n{declaration}";
			}
		}
	}

	internal sealed class SheetXSheetStructure
	{
		// A reader who copies this out of the window must not mistake it for a recovered schema.
		private const string DRAFT_HEADER =
			"// Inferred from this sheet's exported JSON, so it is a draft and not a recovered sheet schema.\r\n"
			+ "// A blank or omitted cell is absent from the exported data, so it cannot appear below.\r\n"
			+ "// Types are read from the values this sheet happens to hold, not from a declaration.\r\n";

		private readonly string m_rootTypeName;
		private readonly InferredClass m_rootClass;
		private readonly IReadOnlyList<InferredClass> m_supportingClasses;
		private readonly IReadOnlyList<string> m_rootMemberNames;
		private readonly IReadOnlyList<string> m_diagnostics;

		private SheetXSheetStructure(
			string rootTypeName,
			InferredClass rootClass,
			IReadOnlyList<InferredClass> supportingClasses,
			IReadOnlyList<string> rootMemberNames,
			IReadOnlyList<string> diagnostics)
		{
			m_rootTypeName = rootTypeName;
			m_rootClass = rootClass;
			m_supportingClasses = supportingClasses;
			m_rootMemberNames = rootMemberNames;
			m_diagnostics = diagnostics;
		}

		internal IReadOnlyList<string> RootMemberNames => m_rootMemberNames;
		internal IReadOnlyList<string> Diagnostics => m_diagnostics;

		/// <summary>Top-level fields only, in first-seen key order. Supporting classes stay in the code.</summary>
		internal IReadOnlyList<SheetXSheetField> Fields => m_rootClass.Fields;

		internal static bool TryFromJson(string json, string rootTypeName, out SheetXSheetStructure structure, out string error)
		{
			structure = null;

			if (string.IsNullOrWhiteSpace(rootTypeName) || !SheetXCollectionNaming.IsValidIdentifier(rootTypeName))
			{
				error = $"Root type name '{rootTypeName}' is not a valid C# identifier.";
				return false;
			}

			JArray rows;
			try
			{
				using var input = new StringReader(json ?? "");
				using var reader = new JsonTextReader(input)
				{
					DateParseHandling = DateParseHandling.None,
					MaxDepth = 64,
				};
				var root = JToken.ReadFrom(reader, new JsonLoadSettings
				{
					DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
				});

				if (!(root is JArray rowArray) || rowArray.Any(row => !(row is JObject)))
				{
					error = "JSON root must be an array of objects.";
					return false;
				}

				while (reader.Read())
				{
					if (reader.TokenType == JsonToken.Comment)
						continue;
					error = "JSON contains more than one root value.";
					return false;
				}

				rows = rowArray;
			}
			catch (Exception ex)
			{
				error = ex.Message;
				return false;
			}

			if (rows.Count == 0 || rows.All(row => !((JObject)row).Properties().Any()))
			{
				error = "Cannot infer structure from empty exported data";
				return false;
			}

			var context = new InferenceContext(rootTypeName);
			InferredClass rootClass = context.InferClass(rootTypeName, rows.Cast<JObject>(), "$");

			structure = new SheetXSheetStructure(
				rootTypeName,
				rootClass,
				context.SupportingClasses,
				rootClass.OriginalJsonKeys,
				context.Diagnostics);
			error = null;
			return true;
		}

		internal string ToClassCode(string namespaceName)
		{
			bool hasNamespace = !string.IsNullOrEmpty(namespaceName);
			if (hasNamespace)
			{
				string[] segments = namespaceName.Split('.');
				foreach (string seg in segments)
				{
					if (!SheetXCollectionNaming.IsValidIdentifier(seg))
						throw new ArgumentException($"Invalid namespace segment '{seg}' in '{namespaceName}'.", nameof(namespaceName));
				}
			}

			var sb = new StringBuilder();
			string indent = hasNamespace ? "\t" : "";

			sb.Append(DRAFT_HEADER);

			if (hasNamespace)
			{
				sb.Append("namespace ").Append(namespaceName).Append("\r\n{\r\n");
			}

			EmitClass(sb, m_rootClass, indent);

			foreach (var supportingClass in m_supportingClasses)
			{
				sb.Append("\r\n");
				EmitClass(sb, supportingClass, indent);
			}

			if (hasNamespace)
			{
				sb.Append("}\r\n");
			}

			return sb.ToString();
		}

		private static void EmitClass(StringBuilder sb, InferredClass cls, string indent)
		{
			sb.Append(indent).Append("[global::System.Serializable]\r\n");
			sb.Append(indent).Append("public class ").Append(cls.TypeName).Append("\r\n");
			sb.Append(indent).Append("{\r\n");

			foreach (var field in cls.Fields)
			{
				string comment = FieldComment(field);
				if (comment != null)
					sb.Append(indent).Append("\t// ").Append(comment).Append("\r\n");

				if (field.IsRenamed)
				{
					string quotedName = JsonConvert.ToString(field.JsonName, '"', StringEscapeHandling.EscapeNonAscii);
					sb.Append(indent).Append("\t[global::Newtonsoft.Json.JsonProperty(").Append(quotedName).Append(")]\r\n");
				}
				sb.Append(indent).Append("\tpublic ").Append(field.FieldType).Append(" ").Append(field.FieldName).Append(";\r\n");
			}

			sb.Append(indent).Append("}\r\n");
		}

		// Only a rename or a fallback earns a line: restating every ordinary declaration is noise a reader
		// learns to skip, and a comment that is skipped protects nobody.
		private static string FieldComment(SheetXSheetField field)
		{
			bool renamed = field.IsRenamed;
			bool fallback = field.FallbackReason != null;
			if (!renamed && !fallback)
				return null;

			var parts = new List<string>(2);
			if (renamed)
			{
				// The exported key, not a spreadsheet header: the legacy Attribute System can fold several
				// columns into one key, so the original header is not recoverable from the JSON.
				parts.Add("JSON key " + ToCommentLiteral(field.JsonName));
			}
			if (fallback)
				parts.Add(field.FallbackReason);

			return string.Join("; ", parts);
		}

		// A key is user-authored text. Newtonsoft's escaping already handles quotes and CR/LF; U+2028 and
		// U+2029 also end a C# single-line comment, so they are escaped too, and a stray '*/' cannot matter
		// because the comment is '//'. The result is one line, so it can never open an executable one.
		private static string ToCommentLiteral(string jsonName)
		{
			string quoted = JsonConvert.ToString(jsonName ?? "", '"', StringEscapeHandling.EscapeNonAscii);
			return quoted.Replace("\u2028", "\\u2028").Replace("\u2029", "\\u2029");
		}

		private sealed class InferredClass
		{
			internal string TypeName;
			internal IReadOnlyList<string> OriginalJsonKeys;
			internal IReadOnlyList<SheetXSheetField> Fields;
		}

		private sealed class InferenceContext
		{
			private readonly HashSet<string> m_usedTypeNames = new HashSet<string>(StringComparer.Ordinal);
			private readonly List<InferredClass> m_supportingClasses = new List<InferredClass>();
			private readonly List<string> m_diagnostics = new List<string>();

			internal InferenceContext(string rootTypeName)
			{
				m_usedTypeNames.Add(rootTypeName);
			}

			internal IReadOnlyList<InferredClass> SupportingClasses => m_supportingClasses;
			internal IReadOnlyList<string> Diagnostics => m_diagnostics;

			internal void AddDiagnostic(string message)
			{
				m_diagnostics.Add(message);
			}

			internal string AllocateTypeName(string candidate)
			{
				string pascal = SheetXCollectionNaming.ToPascalIdentifier(candidate);
				if (string.IsNullOrEmpty(pascal))
					pascal = "_Type";
				else if (!SheetXCollectionNaming.IsValidIdentifier(pascal))
					pascal = "_" + pascal;

				string chosen = pascal;
				int counter = 2;
				while (m_usedTypeNames.Contains(chosen))
				{
					chosen = pascal + counter;
					counter++;
				}
				m_usedTypeNames.Add(chosen);
				return chosen;
			}

			internal InferredClass InferClass(string className, IEnumerable<JObject> objects, string path)
			{
				var orderedKeys = new List<string>();
				var keyObservations = new Dictionary<string, List<JToken>>(StringComparer.Ordinal);

				foreach (var obj in objects)
				{
					foreach (var prop in obj.Properties())
					{
						if (!keyObservations.TryGetValue(prop.Name, out var list))
						{
							list = new List<JToken>();
							keyObservations[prop.Name] = list;
							orderedKeys.Add(prop.Name);
						}
						list.Add(prop.Value);
					}
				}

				var usedMemberNames = new HashSet<string>(StringComparer.Ordinal);
				usedMemberNames.Add(className);

				var assignedNames = new Dictionary<string, string>(StringComparer.Ordinal);

				foreach (string key in orderedKeys)
				{
					if (SheetXCollectionNaming.IsValidIdentifier(key) && !string.Equals(key, className, StringComparison.Ordinal))
					{
						usedMemberNames.Add(key);
						assignedNames[key] = key;
					}
				}

				foreach (string key in orderedKeys)
				{
					if (assignedNames.ContainsKey(key))
						continue;

					string candidate = SheetXCollectionNaming.ToPascalIdentifier(key);
					if (string.IsNullOrEmpty(candidate))
						candidate = "_Field";
					else if (!SheetXCollectionNaming.IsValidIdentifier(candidate))
						candidate = "_" + candidate;

					string finalName = candidate;
					int counter = 2;
					while (usedMemberNames.Contains(finalName))
					{
						finalName = candidate + counter;
						counter++;
					}

					usedMemberNames.Add(finalName);
					assignedNames[key] = finalName;
				}

				var fields = new List<SheetXSheetField>(orderedKeys.Count);
				foreach (string key in orderedKeys)
				{
					string fieldPath = path + "." + key;
					var tokens = keyObservations[key];
					string fieldType = InferTypeFromTokens(tokens, key, fieldPath, out string fallbackReason);
					fields.Add(new SheetXSheetField
					{
						JsonName = key,
						FieldName = assignedNames[key],
						FieldType = fieldType,
						FallbackReason = fallbackReason,
					});
				}

				return new InferredClass
				{
					TypeName = className,
					OriginalJsonKeys = orderedKeys,
					Fields = fields,
				};
			}

			// The reason travels beside the type so the draft's comment restates what inference decided here,
			// rather than guessing backwards from the word 'object'. It is a fixed phrase, never user text:
			// a key can contain quotes and newlines, and these phrases are rendered into '//' comments.
			private string InferTypeFromTokens(List<JToken> tokens, string keyHint, string path, out string fallbackReason)
			{
				fallbackReason = null;

				int nullCount = tokens.Count(t => t.Type == JTokenType.Null);
				var nonNullTokens = tokens.Where(t => t.Type != JTokenType.Null).ToList();

				if (nonNullTokens.Count == 0)
				{
					fallbackReason = "fallback to object: only null was observed, so no type could be read";
					return "object";
				}

				if (nonNullTokens.All(t => t.Type == JTokenType.Boolean))
				{
					if (nullCount > 0)
					{
						AddDiagnostic($"Field '{path}' mixes null with boolean; falling back to object.");
						fallbackReason = "fallback to object: mixes null with boolean";
						// ponytail: fallback to object for mixed types, null-with-scalar, oversized integers, and nested arrays; upgrade to nullable value types, polymorphic unions, or multidimensional arrays if consumers require typed schemas.
						return "object";
					}
					return "bool";
				}

				if (nonNullTokens.All(t => t.Type == JTokenType.String))
					return "string";

				if (nonNullTokens.All(t => t.Type == JTokenType.Integer || t.Type == JTokenType.Float))
				{
					if (nullCount > 0)
					{
						AddDiagnostic($"Field '{path}' mixes null with scalar number; falling back to object.");
						fallbackReason = "fallback to object: mixes null with a number";
						// ponytail: fallback to object for mixed types, null-with-scalar, oversized integers, and nested arrays; upgrade to nullable value types, polymorphic unions, or multidimensional arrays if consumers require typed schemas.
						return "object";
					}

					bool hasFloat = false;
					bool hasInt64 = false;
					bool hasBeyondInt64 = false;

					foreach (var token in nonNullTokens)
					{
						if (token.Type == JTokenType.Float)
						{
							hasFloat = true;
						}
						else if (token is JValue jv && jv.Type == JTokenType.Integer)
						{
							if (jv.Value is long l)
							{
								if (l < int.MinValue || l > int.MaxValue)
									hasInt64 = true;
							}
							else if (jv.Value is int)
							{
								// fits Int32
							}
							else
							{
								hasBeyondInt64 = true;
							}
						}
					}

					if (hasBeyondInt64)
					{
						AddDiagnostic($"Field '{path}' has integer beyond Int64 range; falling back to object.");
						fallbackReason = "fallback to object: an integer is beyond Int64 range";
						// ponytail: fallback to object for mixed types, null-with-scalar, oversized integers, and nested arrays; upgrade to nullable value types, polymorphic unions, or multidimensional arrays if consumers require typed schemas.
						return "object";
					}
					if (hasFloat)
						return "double";
					if (hasInt64)
						return "long";
					return "int";
				}

				if (nonNullTokens.All(t => t.Type == JTokenType.Object))
				{
					string supportingTypeName = AllocateTypeName(keyHint);
					var supportingObjects = nonNullTokens.Cast<JObject>();
					var supportingClass = InferClass(supportingTypeName, supportingObjects, path);
					m_supportingClasses.Add(supportingClass);
					return supportingTypeName;
				}

				if (nonNullTokens.All(t => t.Type == JTokenType.Array))
				{
					var allElements = nonNullTokens.Cast<JArray>().SelectMany(a => a).ToList();
					if (allElements.Count == 0)
					{
						AddDiagnostic($"Field '{path}' contains only empty arrays; falling back to object[].");
						fallbackReason = "fallback to object[]: only empty arrays were observed";
						// ponytail: fallback to object for mixed types, null-with-scalar, oversized integers, and nested arrays; upgrade to nullable value types, polymorphic unions, or multidimensional arrays if consumers require typed schemas.
						return "object[]";
					}

					if (allElements.Any(e => e.Type == JTokenType.Array))
					{
						AddDiagnostic($"Field '{path}' contains nested arrays; falling back to object[].");
						fallbackReason = "fallback to object[]: contains nested arrays";
						// ponytail: fallback to object for mixed types, null-with-scalar, oversized integers, and nested arrays; upgrade to nullable value types, polymorphic unions, or multidimensional arrays if consumers require typed schemas.
						return "object[]";
					}

					var nonNullElements = allElements.Where(e => e.Type != JTokenType.Null).ToList();
					if (nonNullElements.Count > 0 && nonNullElements.All(e => e.Type == JTokenType.Object))
					{
						string supportingTypeName = AllocateTypeName(keyHint);
						var supportingObjects = nonNullElements.Cast<JObject>();
						var supportingClass = InferClass(supportingTypeName, supportingObjects, path + "[]");
						m_supportingClasses.Add(supportingClass);
						return supportingTypeName + "[]";
					}

					// The element reason describes the elements, and the array type is built from it, so it
					// carries over unchanged rather than being restated about the array.
					string elemType = InferTypeFromTokens(allElements, keyHint, path + "[]", out fallbackReason);
					return elemType + "[]";
				}

				AddDiagnostic($"Field '{path}' has incompatible observed kinds; falling back to object.");
				fallbackReason = "fallback to object: incompatible kinds were observed across rows";
				// ponytail: fallback to object for mixed types, null-with-scalar, oversized integers, and nested arrays; upgrade to nullable value types, polymorphic unions, or multidimensional arrays if consumers require typed schemas.
				return "object";
			}
		}
	}
}
