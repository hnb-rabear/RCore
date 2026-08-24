/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace RCore.SheetX.Editor
{
	/// <summary>
	/// One interactive collection export. Every bound sheet is parsed and held in memory until
	/// <see cref="Flush"/>, so a single bad sheet leaves no half-written JSON and no generated source
	/// that disagrees with it.
	/// </summary>
	internal sealed class SheetXCollectionExportSession
	{
		private sealed class Candidate
		{
			internal SheetXCollectionGeneratedTable Table;
			internal string Json;
		}

		private readonly SheetXSettings m_settings;
		private readonly List<Candidate> m_candidates = new List<Candidate>();
		private readonly List<SheetXSheetBinding> m_bindings = new List<SheetXSheetBinding>();
		private readonly List<string> m_errors = new List<string>();

		internal SheetXCollectionExportSession(SheetXSettings settings)
		{
			m_settings = settings;
		}

		/// <summary>
		/// Returns how one source sheet must be exported. Unknown sheets bind to JSON Only on first
		/// sight, so an untouched project keeps its existing output.
		/// </summary>
		internal SheetXSheetOutputMode ModeOf(string sourceId, string sheetName)
		{
			var binding = SheetXCollectionSettings.GetOrCreateBinding(m_settings, sourceId, sheetName);
			return binding?.outputMode ?? SheetXSheetOutputMode.JsonOnly;
		}

		/// <summary>
		/// Parses one Generated Model sheet into typed JSON. Nothing reaches the disk here — a parse
		/// failure is remembered so <see cref="Flush"/> refuses the whole export.
		/// </summary>
		internal bool TryAddGeneratedTable(
			string sourceId,
			string sheetName,
			IReadOnlyList<string> headers,
			IReadOnlyList<IReadOnlyList<string>> rows,
			out string error)
		{
			if (!TryBind(sourceId, sheetName, SheetXSheetOutputMode.CollectionGeneratedModel, out var binding, out error))
				return false;

			if (!SheetXCollectionSchemaParser.TryParse(
				headers, SheetXCollectionNaming.RowTypeName(sheetName), out var schema, out error))
			{
				return Reject($"Sheet '{sheetName}': {error}", out error);
			}
			if (!SheetXCollectionSchemaParser.TryBuildRows(
				schema, rows, m_settings.GetPersistentFields(), out string json, out error))
			{
				return Reject($"Sheet '{sheetName}': {error}", out error);
			}

			Add(binding, json, table => table.Schema = schema);
			return true;
		}

		/// <summary>
		/// Adds one Existing Model sheet, keeping the legacy JSON byte for byte. The row type is resolved
		/// and the JSON deserialized into it now, so a mapping mistake is reported before anything is written.
		/// </summary>
		internal bool TryAddExistingTable(
			string sourceId,
			string sheetName,
			string legacyJson,
			out string error)
		{
			if (!TryBind(sourceId, sheetName, SheetXSheetOutputMode.CollectionExistingModel, out var binding, out error))
				return false;

			if (!TryResolveRowType(binding.rowTypeName, out var rowType, out error))
				return Reject($"Sheet '{sheetName}': {error}", out error);

			string json = string.IsNullOrEmpty(legacyJson) ? "[]" : legacyJson;
			try
			{
				JsonConvert.DeserializeObject(json, rowType.MakeArrayType());
			}
			catch (Exception ex)
			{
				return Reject(
					$"Sheet '{sheetName}': its Json does not map onto '{rowType.FullName}': {ex.Message}",
					out error);
			}

			// Nested types read as 'Outer+Inner' in FullName, which is not C#.
			string typeName = rowType.FullName?.Replace('+', '.') ?? rowType.Name;
			Add(binding, json, table => table.ExistingRowTypeName = typeName);
			return true;
		}

		/// <summary>
		/// True once <see cref="Flush"/> has written collection artifacts. The caller owns the single
		/// AssetDatabase refresh, because the same export may also have written Config artifacts.
		/// </summary>
		internal bool WroteArtifacts { get; private set; }

		/// <summary>True when this flush changed generated collection source and needs script reload.</summary>
		internal bool RequiresScriptReload { get; private set; }

		/// <summary>
		/// Validates the whole batch, then stages every JSON file plus the single generated source through
		/// one atomic export context. Nothing is written unless all of it is valid.
		/// </summary>
		internal bool Flush(out string error)
		{
			error = null;
			WroteArtifacts = false;
			RequiresScriptReload = false;
			if (m_errors.Count > 0)
			{
				error = string.Join("\n", m_errors);
				return false;
			}
			var issues = SheetXCollectionSettings.Validate(m_settings, m_bindings);
			if (issues.Count > 0)
			{
				error = string.Join("\n", issues.Select(i => i.Message));
				return false;
			}
			if (m_candidates.Count == 0)
				return true;
			if (!IncludesEveryCollectionBinding(out error))
				return false;

			string source;
			try
			{
				// Emit first: it rejects every name collision and stamps each table with the JSON path
				// its generated constant names, so the staged file and the constant cannot disagree.
				source = SheetXCollectionGenerator.Emit(m_settings, m_candidates.Select(c => c.Table).ToList());
			}
			catch (InvalidOperationException ex)
			{
				error = ex.Message;
				return false;
			}

			RequiresScriptReload = SourceChanged(source);
			// discardStagedOnError: a collision inside the context must leave the previous export intact.
			var context = new SheetXExportContext(new SheetXFileOutput(), discardStagedOnError: true);
			var writer = new SheetXWriter(m_settings, context);
			foreach (var candidate in m_candidates)
			{
				string path = candidate.Table.JsonPath;
				writer.Write(
					Path.GetDirectoryName(path) ?? "", Path.GetFileName(path), candidate.Json,
					SheetXExportFileType.Json);
			}
			writer.Write(
				m_settings.collectionCodeFolder, SheetXCollectionGenerator.FileName, source,
				SheetXExportFileType.ConfigScript);
			context.Flush();

			var result = context.ToResult();
			if (!result.Success)
			{
				error = string.Join("\n", result.Errors);
				return false;
			}

			if (RequiresScriptReload)
			{
				// Generated types cannot be baked before script reload compiles them.
				SheetXCollectionBaker.RegisterPendingBake(m_settings, m_settings.autoLoadAfterExport);
			}
			WroteArtifacts = true;
			return true;
		}

		/// <summary>Completes an Existing Model export after callers refresh imported JSON files.</summary>
		internal bool TryBakeAfterRefresh(out string error)
		{
			if (RequiresScriptReload)
			{
				error = null;
				return true;
			}
			return m_settings.autoLoadAfterExport
				? SheetXCollectionBaker.TryLoadData(m_settings, autoLoadOnly: true, out error)
				: SheetXCollectionBaker.TryFinishPendingBake(m_settings, autoLoadAfterExport: false, out error);
		}

		private bool SourceChanged(string source)
		{
			string path = Path.Combine(m_settings.collectionCodeFolder, SheetXCollectionGenerator.FileName);
			return !File.Exists(path) || !string.Equals(File.ReadAllText(path), source, StringComparison.Ordinal);
		}

		private bool IncludesEveryCollectionBinding(out string error)
		{
			var active = new HashSet<string>(m_bindings.Select(BindingKey), StringComparer.Ordinal);
			foreach (var binding in m_settings.sheetBindings)
			{
				if (binding == null || binding.outputMode == SheetXSheetOutputMode.JsonOnly)
					continue;
				if (active.Contains(BindingKey(binding)))
					continue;

				error = $"Sheet '{binding.sheetName}': collection source is shared; select every Collection sheet before export.";
				return false;
			}
			error = null;
			return true;
		}

		private static string BindingKey(SheetXSheetBinding binding)
			=> binding.sourceId + "\0" + binding.sheetName;

		// A binding the caller routed as a collection must still say so — the settings asset, not the
		// caller, owns that decision.
		private bool TryBind(
			string sourceId, string sheetName, SheetXSheetOutputMode expected,
			out SheetXSheetBinding binding, out string error)
		{
			error = null;
			binding = SheetXCollectionSettings.GetOrCreateBinding(m_settings, sourceId, sheetName);
			if (binding == null)
			{
				Reject($"Sheet '{sheetName}': no settings asset to bind against.", out error);
				return false;
			}
			if (binding.outputMode == SheetXSheetOutputMode.JsonOnly)
				return false;
			if (binding.outputMode != expected)
			{
				Reject($"Sheet '{sheetName}': bound as {binding.outputMode}, not {expected}.", out error);
				return false;
			}
			return true;
		}

		private void Add(SheetXSheetBinding binding, string json, Action<SheetXCollectionGeneratedTable> fill)
		{
			m_bindings.Add(binding);
			var table = new SheetXCollectionGeneratedTable
			{
				SourceId = binding.sourceId,
				SheetName = binding.sheetName,
				CollectionName = string.IsNullOrEmpty(binding.collectionName)
					? SheetXCollectionSettings.GlobalName
					: binding.collectionName,
				FieldName = SheetXCollectionSettings.ResolveFieldName(binding),
			};
			fill(table);
			m_candidates.Add(new Candidate { Table = table, Json = json });
		}

		private bool Reject(string message, out string error)
		{
			m_errors.Add(message);
			error = message;
			return false;
		}

		// Type.GetType only sees mscorlib and the calling assembly unless the name is assembly-qualified,
		// and a row type usually lives in the consuming project's own assembly.
		private static bool TryResolveRowType(string rowTypeName, out Type type, out string error)
		{
			type = null;
			error = null;
			if (string.IsNullOrWhiteSpace(rowTypeName))
			{
				error = "Existing Model needs a row type name.";
				return false;
			}

			rowTypeName = rowTypeName.Trim();
			type = Type.GetType(rowTypeName, throwOnError: false) ?? SearchLoadedAssemblies(rowTypeName);
			if (type == null)
			{
				error = $"row type '{rowTypeName}' was not found in any loaded assembly.";
				return false;
			}
			if (!type.IsClass || type.IsAbstract || type.IsGenericType || type.IsGenericTypeDefinition)
			{
				error = $"row type '{type.FullName}' must be a concrete, non-generic class.";
				type = null;
				return false;
			}
			if (!type.IsDefined(typeof(SerializableAttribute), inherit: false))
			{
				error = $"row type '{type.FullName}' must be marked [Serializable] so Unity can bake it.";
				type = null;
				return false;
			}
			return true;
		}

		private static Type SearchLoadedAssemblies(string rowTypeName)
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				Type[] types;
				try { types = assembly.GetTypes(); }
				catch (System.Reflection.ReflectionTypeLoadException ex) { types = ex.Types; }

				foreach (var type in types)
				{
					if (type == null)
						continue;
					if (string.Equals(type.FullName, rowTypeName, StringComparison.Ordinal)
						|| string.Equals(type.Name, rowTypeName, StringComparison.Ordinal))
						return type;
				}
			}
			return null;
		}
	}
}
