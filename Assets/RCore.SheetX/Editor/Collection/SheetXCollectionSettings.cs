/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace RCore.SheetX.Editor
{
	/// <summary>
	/// Describes one collection configuration problem found during preflight.
	/// </summary>
	internal sealed class SheetXCollectionDiagnostic
	{
		/// <summary>Gets or sets the formatted, user-facing description.</summary>
		internal string Message;
		/// <summary>Gets or sets the workbook path or Google spreadsheet ID, when the issue has one.</summary>
		internal string SourceId;
		/// <summary>Gets or sets the source sheet name, when the issue has one.</summary>
		internal string SheetName;
		/// <summary>Gets or sets the offending path, when the issue has one.</summary>
		internal string Path;
	}

	/// <summary>
	/// Owns collection CRUD, sheet-binding lookup, and preflight validation over <see cref="SheetXSettings"/>.
	/// Pure settings surgery: never touches the AssetDatabase and never deletes generated files.
	/// </summary>
	internal static class SheetXCollectionSettings
	{
		/// <summary>The immutable built-in collection every unassigned sheet falls back to.</summary>
		internal const string GlobalName = "Global";

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

		#region Paths

		/// <summary>
		/// Rewrites separators to '/' and drops trailing slashes. Deliberately does not resolve, absolutize,
		/// or case-fold the path — a stored path must round-trip to the same string the user typed.
		/// </summary>
		internal static string NormalizePath(string path)
		{
			if (string.IsNullOrEmpty(path))
				return "";
			string normalized = path.Replace('\\', '/');
			while (normalized.Length > 1 && normalized[normalized.Length - 1] == '/')
				normalized = normalized.Substring(0, normalized.Length - 1);
			return normalized;
		}

		private static string[] Segments(string path)
		{
			return NormalizePath(path).Split('/').Where(s => s.Length > 0).ToArray();
		}

		/// <summary>Determines whether the path is project-relative, i.e. its first segment is exactly "Assets".</summary>
		internal static bool IsProjectPath(string path)
		{
			var segments = Segments(path);
			return segments.Length > 0 && string.Equals(segments[0], "Assets", StringComparison.Ordinal);
		}

		/// <summary>
		/// Determines whether an "Editor" segment follows "Assets". Segment membership, not substring:
		/// "Assets/GameEditor/Data" is a shipping folder, "Assets/Game/Editor/Data" is not.
		/// </summary>
		internal static bool HasEditorSegment(string path)
		{
			var segments = Segments(path);
			for (int i = 1; i < segments.Length; i++)
			{
				// Unity's special-folder rule is case-sensitive, so the comparison is too.
				if (string.Equals(segments[i], "Editor", StringComparison.Ordinal))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Determines whether the final segment is exactly "Resources". Resources.Load resolves relative to
		/// such a folder, so the Global root asset must sit directly inside one.
		/// </summary>
		internal static bool EndsWithResources(string path)
		{
			var segments = Segments(path);
			return segments.Length > 0 && string.Equals(segments[segments.Length - 1], "Resources", StringComparison.Ordinal);
		}

		/// <summary>
		/// Determines whether either path contains the other, comparing whole segments. Case-insensitive on
		/// purpose: the check is conservative, and a Windows filesystem would treat the two as one folder.
		/// </summary>
		internal static bool PathsOverlap(string a, string b)
		{
			var left = Segments(a);
			var right = Segments(b);
			if (left.Length == 0 || right.Length == 0)
				return false;
			int shared = Math.Min(left.Length, right.Length);
			for (int i = 0; i < shared; i++)
			{
				if (!string.Equals(left[i], right[i], StringComparison.OrdinalIgnoreCase))
					return false;
			}
			return true;
		}

		#endregion

		#region CRUD

		/// <summary>
		/// Restores exactly one immutable Global definition and re-homes every binding whose collection is
		/// missing, blank, or deleted. Safe to call repeatedly.
		/// </summary>
		internal static void EnsureGlobal(SheetXSettings settings)
		{
			if (settings == null)
				return;

			settings.collections ??= new List<SheetXCollectionDefinition>();
			settings.sheetBindings ??= new List<SheetXSheetBinding>();
			settings.collections.RemoveAll(c => c == null);
			settings.sheetBindings.RemoveAll(b => b == null);

			var globals = settings.collections
				.Where(c => string.Equals(c.name, GlobalName, StringComparison.Ordinal))
				.ToList();

			if (globals.Count == 0)
			{
				settings.collections.Insert(
					0,
					new SheetXCollectionDefinition { name = GlobalName, autoLoad = true, builtInGlobal = true });
			}
			else
			{
				globals[0].builtInGlobal = true;
				for (int i = 1; i < globals.Count; i++)
					settings.collections.Remove(globals[i]);
			}

			// The flag is what the UI locks against, so it must not survive on a renamed or hand-edited entry.
			foreach (var collection in settings.collections)
			{
				if (!string.Equals(collection.name, GlobalName, StringComparison.Ordinal))
					collection.builtInGlobal = false;
			}

			var known = new HashSet<string>(settings.collections.Select(c => c.name), StringComparer.Ordinal);
			foreach (var binding in settings.sheetBindings)
			{
				if (string.IsNullOrEmpty(binding.collectionName) || !known.Contains(binding.collectionName))
					binding.collectionName = GlobalName;
			}
		}

		/// <summary>
		/// Returns the binding for one source sheet, creating a JSON-only, Global-assigned one on first use.
		/// Identity is the ordinal pair of <paramref name="sourceId"/> and <paramref name="sheetName"/>.
		/// </summary>
		internal static SheetXSheetBinding GetOrCreateBinding(SheetXSettings settings, string sourceId, string sheetName)
		{
			if (settings == null)
				return null;

			EnsureGlobal(settings);
			var existing = settings.sheetBindings.FirstOrDefault(b =>
				string.Equals(b.sourceId, sourceId, StringComparison.Ordinal)
				&& string.Equals(b.sheetName, sheetName, StringComparison.Ordinal));
			if (existing != null)
				return existing;

			var binding = new SheetXSheetBinding
			{
				sourceId = sourceId ?? "",
				sheetName = sheetName ?? "",
				outputMode = SheetXSheetOutputMode.JsonOnly,
				collectionName = GlobalName,
			};
			settings.sheetBindings.Add(binding);
			return binding;
		}

		/// <summary>
		/// Renames a collection and migrates every binding that pointed at it. Rejects Global, unknown names,
		/// duplicates, and anything that is not a valid C# identifier.
		/// </summary>
		internal static bool RenameCollection(SheetXSettings settings, string oldName, string newName, out string error)
		{
			error = null;
			if (settings == null)
			{
				error = "No settings asset.";
				return false;
			}

			EnsureGlobal(settings);
			if (string.Equals(oldName, GlobalName, StringComparison.Ordinal))
			{
				error = "The Global collection cannot be renamed.";
				return false;
			}

			var definition = settings.collections
				.FirstOrDefault(c => string.Equals(c.name, oldName, StringComparison.Ordinal));
			if (definition == null)
			{
				error = $"Collection '{oldName}' does not exist.";
				return false;
			}

			newName = newName?.Trim();
			if (string.Equals(newName, oldName, StringComparison.Ordinal))
				return true;
			if (string.Equals(newName, GlobalName, StringComparison.Ordinal))
			{
				error = "The name 'Global' is reserved.";
				return false;
			}
			if (!IsValidIdentifier(newName))
			{
				error = $"'{newName}' is not a valid C# identifier.";
				return false;
			}
			if (settings.collections.Any(c => string.Equals(c.name, newName, StringComparison.Ordinal)))
			{
				error = $"A collection named '{newName}' already exists.";
				return false;
			}

			definition.name = newName;
			foreach (var binding in settings.sheetBindings)
			{
				if (string.Equals(binding.collectionName, oldName, StringComparison.Ordinal))
					binding.collectionName = newName;
			}
			return true;
		}

		/// <summary>
		/// Removes a collection definition and re-homes its bindings to Global. Never deletes generated code
		/// or baked assets — that stays a human decision.
		/// </summary>
		internal static bool DeleteCollection(SheetXSettings settings, string name, out string error)
		{
			error = null;
			if (settings == null)
			{
				error = "No settings asset.";
				return false;
			}

			EnsureGlobal(settings);
			if (string.Equals(name, GlobalName, StringComparison.Ordinal))
			{
				error = "The Global collection cannot be deleted.";
				return false;
			}

			var definition = settings.collections
				.FirstOrDefault(c => string.Equals(c.name, name, StringComparison.Ordinal));
			if (definition == null)
			{
				error = $"Collection '{name}' does not exist.";
				return false;
			}

			settings.collections.Remove(definition);
			foreach (var binding in settings.sheetBindings)
			{
				if (string.Equals(binding.collectionName, name, StringComparison.Ordinal))
					binding.collectionName = GlobalName;
			}
			return true;
		}

		/// <summary>
		/// Resolves the collection field a binding writes into: the explicit override when set, otherwise the
		/// sheet name reduced to a legal identifier.
		/// </summary>
		internal static string ResolveFieldName(SheetXSheetBinding binding)
		{
			if (binding == null)
				return "";
			string raw = string.IsNullOrWhiteSpace(binding.fieldName) ? binding.sheetName : binding.fieldName;
			return Sanitize(raw);
		}

		#endregion

		#region Validation

		/// <summary>
		/// Reports every collection configuration problem at once — a preflight, not a fail-fast guard, so one
		/// export attempt surfaces the whole list. Returns an empty list when the feature is off.
		/// </summary>
		/// <param name="settings">Settings to inspect. Not mutated.</param>
		/// <param name="activeBindings">Bindings the caller is about to export, or null.</param>
		internal static List<SheetXCollectionDiagnostic> Validate(
			SheetXSettings settings, IEnumerable<SheetXSheetBinding> activeBindings)
		{
			var issues = new List<SheetXCollectionDiagnostic>();
			if (settings == null || !settings.enableCollections)
				return issues;

			// Read-only on purpose: a missing Global is a diagnostic here, not something to silently repair.
			var collections = settings.collections?.Where(c => c != null).ToList() ?? new List<SheetXCollectionDefinition>();
			var bindings = settings.sheetBindings?.Where(b => b != null).ToList() ?? new List<SheetXSheetBinding>();

			ValidateNamespace(settings, issues);
			ValidateFolders(settings, issues);
			ValidateDefinitions(collections, issues);

			var known = new HashSet<string>(collections.Select(c => c.name), StringComparer.Ordinal);
			foreach (var binding in bindings)
			{
				if (!known.Contains(binding.collectionName ?? ""))
				{
					issues.Add(Issue(
						binding.collectionName, binding.sourceId, binding.sheetName,
						$"Binding points at collection '{Or(binding.collectionName)}', which is not defined.", null));
				}
			}

			var all = new List<SheetXSheetBinding>(bindings);
			if (activeBindings != null)
			{
				foreach (var binding in activeBindings)
				{
					if (binding == null)
						continue;
					bool registered = bindings.Any(b =>
						string.Equals(b.sourceId, binding.sourceId, StringComparison.Ordinal)
						&& string.Equals(b.sheetName, binding.sheetName, StringComparison.Ordinal));
					if (registered)
						continue;
					all.Add(binding);
					issues.Add(Issue(
						binding.collectionName, binding.sourceId, binding.sheetName,
						"Binding was supplied for export but is not saved in the settings asset.", null));
				}
			}

			ValidateFieldNames(all, issues);
			return issues;
		}

		private static void ValidateNamespace(SheetXSettings settings, List<SheetXCollectionDiagnostic> issues)
		{
			string ns = settings.collectionNamespace;
			if (string.IsNullOrWhiteSpace(ns))
			{
				issues.Add(Issue(null, null, null, "Collection namespace is empty.", null));
				return;
			}
			if (!IsValidNamespace(ns))
				issues.Add(Issue(null, null, null, $"'{ns}' is not a valid namespace.", null));
		}

		private static void ValidateFolders(SheetXSettings settings, List<SheetXCollectionDiagnostic> issues)
		{
			ValidateFolder("Generated code folder", settings.collectionCodeFolder, issues);
			ValidateFolder("Collection asset folder", settings.collectionAssetFolder, issues);
			ValidateFolder("Collection JSON folder", settings.collectionJsonFolder, issues);
			ValidateFolder("Global Resources folder", settings.globalResourcesFolder, issues);

			string json = settings.collectionJsonFolder;
			if (!string.IsNullOrWhiteSpace(json) && !HasEditorSegment(json))
			{
				issues.Add(Issue(null, null, null,
					"Collection JSON folder must sit under an 'Editor' folder so the source JSON never ships in a build.",
					json));
			}

			string global = settings.globalResourcesFolder;
			if (!string.IsNullOrWhiteSpace(global) && !EndsWithResources(global))
			{
				issues.Add(Issue(null, null, null,
					"Global Resources folder must end with a 'Resources' folder so Resources.Load can find the root asset.",
					global));
			}

			CheckOverlap("Generated code folder", settings.collectionCodeFolder,
				"Collection asset folder", settings.collectionAssetFolder, issues);
			CheckOverlap("Generated code folder", settings.collectionCodeFolder,
				"Collection JSON folder", settings.collectionJsonFolder, issues);
			CheckOverlap("Collection asset folder", settings.collectionAssetFolder,
				"Collection JSON folder", settings.collectionJsonFolder, issues);
		}

		private static void ValidateFolder(string label, string path, List<SheetXCollectionDiagnostic> issues)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				issues.Add(Issue(null, null, null, $"{label} is empty.", path));
				return;
			}
			if (!IsProjectPath(path))
			{
				issues.Add(Issue(null, null, null,
					$"{label} must be a project-relative path starting with 'Assets/'.", path));
			}
		}

		private static void CheckOverlap(
			string labelA, string a, string labelB, string b, List<SheetXCollectionDiagnostic> issues)
		{
			if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b) || !PathsOverlap(a, b))
				return;
			issues.Add(Issue(null, null, null,
				$"{labelA} and {labelB} must not contain each other.",
				$"{NormalizePath(a)} | {NormalizePath(b)}"));
		}

		private static void ValidateDefinitions(
			List<SheetXCollectionDefinition> collections, List<SheetXCollectionDiagnostic> issues)
		{
			if (!collections.Any(c => string.Equals(c.name, GlobalName, StringComparison.Ordinal)))
				issues.Add(Issue(GlobalName, null, null, "The built-in Global collection is missing.", null));

			var seen = new HashSet<string>(StringComparer.Ordinal);
			foreach (var collection in collections)
			{
				if (!IsValidIdentifier(collection.name))
				{
					issues.Add(Issue(collection.name, null, null,
						$"'{Or(collection.name)}' is not a valid C# identifier, so no type can be generated for it.", null));
				}
				if (!seen.Add(collection.name ?? ""))
					issues.Add(Issue(collection.name, null, null, "Duplicate collection definition.", null));
			}
		}

		private static void ValidateFieldNames(
			List<SheetXSheetBinding> bindings, List<SheetXCollectionDiagnostic> issues)
		{
			var seen = new HashSet<string>(StringComparer.Ordinal);
			foreach (var binding in bindings)
			{
				if (binding.outputMode == SheetXSheetOutputMode.JsonOnly)
					continue;
				string field = ResolveFieldName(binding);
				if (string.IsNullOrEmpty(field))
				{
					issues.Add(Issue(binding.collectionName, binding.sourceId, binding.sheetName,
						"Sheet resolves to an empty collection field name. Set an explicit field name.", null));
					continue;
				}
				if (!seen.Add(binding.collectionName + "\0" + field))
				{
					issues.Add(Issue(binding.collectionName, binding.sourceId, binding.sheetName,
						$"Two sheets both write collection field '{field}'. Set an explicit field name on one of them.",
						null));
				}
			}
		}

		private static SheetXCollectionDiagnostic Issue(
			string collection, string sourceId, string sheetName, string cause, string path)
		{
			return new SheetXCollectionDiagnostic
			{
				SourceId = sourceId ?? "",
				SheetName = sheetName ?? "",
				Path = path ?? "",
				Message = $"[SheetX Collections] {Or(collection)} / {Or(sourceId)} / {Or(sheetName)}:\n{cause}\nPath: {Or(path)}",
			};
		}

		private static string Or(string value) => string.IsNullOrEmpty(value) ? "-" : value;

		#endregion

		#region Identifiers

		private static bool IsValidNamespace(string value)
		{
			if (string.IsNullOrEmpty(value))
				return false;
			var parts = value.Split('.');
			return parts.All(IsValidIdentifier);
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

		private static string Sanitize(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return "";
			var chars = value.Trim().ToCharArray();
			for (int i = 0; i < chars.Length; i++)
			{
				if (chars[i] != '_' && !char.IsLetterOrDigit(chars[i]))
					chars[i] = '_';
			}
			string result = new string(chars);
			if (char.IsDigit(result[0]))
				result = "_" + result;
			return s_reservedKeywords.Contains(result) ? "_" + result : result;
		}

		#endregion
	}
}
