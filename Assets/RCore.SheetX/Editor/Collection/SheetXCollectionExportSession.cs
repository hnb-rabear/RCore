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
	/// One interactive collection export. Bad sheets are reported and skipped; accepted JSON and generated
	/// source stay in memory until <see cref="Flush"/> can write their internally consistent set atomically.
	/// </summary>
	internal sealed class SheetXCollectionExportSession
	{
		private sealed class Candidate
		{
			internal SheetXSheetBinding Binding;
			internal SheetXCollectionGeneratedTable Table;
			internal string Json;
		}

		private sealed class FileSnapshot
		{
			internal string Path;
			internal byte[] Content;
			internal bool Existed;
		}

		private sealed class RollbackFileOutput : ISheetXOutput
		{
			private readonly ISheetXOutput m_output;
			private readonly Dictionary<string, FileSnapshot> m_snapshots;

			internal RollbackFileOutput(ISheetXOutput output, IEnumerable<FileSnapshot> snapshots)
			{
				m_output = output;
				m_snapshots = snapshots.ToDictionary(snapshot => snapshot.Path, StringComparer.Ordinal);
			}

			public void Write(string relativePath, string content)
			{
				try
				{
					m_output.Write(relativePath, content);
				}
				catch
				{
					if (m_snapshots.TryGetValue(relativePath, out var snapshot))
						RestoreSnapshot(snapshot);
					throw;
				}
			}
		}

		private readonly SheetXSettings m_settings;
		private readonly List<Candidate> m_candidates = new List<Candidate>();
		private readonly List<Candidate> m_accepted = new List<Candidate>();
		private readonly List<SheetXConfigurationSource> m_configurationSources = new List<SheetXConfigurationSource>();
		private readonly HashSet<string> m_processed = new HashSet<string>(StringComparer.Ordinal);
		private readonly HashSet<string> m_processedSources = new HashSet<string>(StringComparer.Ordinal);
		private readonly HashSet<string> m_skipped = new HashSet<string>(StringComparer.Ordinal);
		private readonly Action<string> m_warn;
		private readonly Action<string> m_error;
		private readonly ISheetXOutput m_output;
		private readonly IReadOnlyDictionary<string, int> m_ids;

		internal SheetXCollectionExportSession(
			SheetXSettings settings,
			Action<string> warn = null,
			Action<string> error = null,
			ISheetXOutput output = null,
			IReadOnlyDictionary<string, int> ids = null)
		{
			m_settings = settings;
			m_warn = warn;
			m_error = error;
			m_output = output ?? new SheetXFileOutput();
			m_ids = ids;
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
		/// Adds one exact Configuration sheet from a physical source. Parsed strictly during <see cref="Flush"/>.
		/// </summary>
		internal bool TryAddConfiguration(
			string sourceId,
			IReadOnlyList<string[]> table,
			out string error)
		{
			error = null;
			m_configurationSources.Add(new SheetXConfigurationSource
			{
				SourceId = sourceId,
				Table = table,
			});
			return true;
		}

		/// <summary>Parses one Generated Data Class sheet into typed JSON without writing files.</summary>
		internal bool TryAddGeneratedTable(
			string sourceId,
			string sheetName,
			IReadOnlyList<string> headers,
			IReadOnlyList<IReadOnlyList<string>> rows,
			out string error)
		{
			if (!TryBind(sourceId, sheetName, SheetXSheetOutputMode.GeneratedDataClass, out var binding, out error))
				return false;

			var resolvedRows = ResolveIds(rows);
			if (!SheetXCollectionSchemaParser.TryParse(
				headers, resolvedRows, SheetXCollectionNaming.RowTypeName(sheetName),
				out var schema, out var warnings, out error))
			{
				return SkipSheet(binding, error, out error);
			}
			foreach (string warning in warnings)
				m_warn?.Invoke(Context(sourceId, sheetName, warning));
			if (!SheetXCollectionSchemaParser.TryBuildRows(
				schema, resolvedRows, m_settings.GetPersistentFields(), out string json, out error))
			{
				return SkipSheet(binding, error, out error);
			}

			Add(binding, json, table => table.Schema = schema);
			return true;
		}

		private IReadOnlyList<IReadOnlyList<string>> ResolveIds(
			IReadOnlyList<IReadOnlyList<string>> rows)
		{
			if (m_ids == null || m_ids.Count == 0 || rows == null)
				return rows;

			return rows.Select(row => (IReadOnlyList<string>)(row == null
				? null
				: row.Select(ResolveIds).ToArray())).ToArray();
		}

		private string ResolveIds(string value)
		{
			string[] parts = SheetXHelper.SplitValueToArray(value ?? "", false);
			bool resolved = false;
			for (int i = 0; i < parts.Length; i++)
			{
				if (!m_ids.TryGetValue(parts[i], out int id))
					continue;
				parts[i] = SheetXHelper.FormatInt(id);
				resolved = true;
			}
			return resolved ? string.Join("|", parts) : value;
		}

		/// <summary>
		/// Adds one Existing Data Class sheet, keeping legacy JSON byte for byte. Row type and JSON mapping
		/// are checked now, before any output can be written.
		/// </summary>
		internal bool TryAddExistingTable(
			string sourceId,
			string sheetName,
			string legacyJson,
			out string error)
		{
			if (!TryBind(sourceId, sheetName, SheetXSheetOutputMode.ExistingDataClass, out var binding, out error))
				return false;

			if (!TryResolveRowType(binding.rowTypeName, out var rowType, out error))
				return SkipSheet(binding, error + " Fix: set Existing Data Class to a loaded concrete [Serializable] row type.", out error);

			string json = string.IsNullOrEmpty(legacyJson) ? "[]" : legacyJson;
			try
			{
				JsonConvert.DeserializeObject(json, rowType.MakeArrayType());
			}
			catch (Exception ex)
			{
				return SkipSheet(binding,
					$"JSON does not map onto '{rowType.FullName}': {ex.Message} Fix: correct sheet values or select the matching row type.",
					out error);
			}

			string typeName = rowType.FullName?.Replace('+', '.') ?? rowType.Name;
			Add(binding, json, table => table.ExistingRowTypeName = typeName);
			return true;
		}

		/// <summary>True once <see cref="Flush"/> has written collection artifacts.</summary>
		internal bool WroteArtifacts { get; private set; }

		/// <summary>True when generated collection source changed and needs script reload.</summary>
		internal bool RequiresScriptReload { get; private set; }

		/// <summary>Number of collection sheets rejected during add or candidate admission.</summary>
		internal int SkippedSheetCount => m_skipped.Count;

		/// <summary>True after latest flush completed, including a successful no-op.</summary>
		internal bool FlushSucceeded { get; private set; }

		/// <summary>
		/// Admits valid candidates in processing order, then writes all accepted JSON plus generated source
		/// through one atomic export context.
		/// </summary>
		internal bool Flush(out string error)
		{
			error = null;
			WroteArtifacts = false;
			RequiresScriptReload = false;
			FlushSucceeded = false;
			m_accepted.Clear();

			ConfigSheetData configData = null;
			string configJson = null;
			string configJsonPath = null;
			if (m_configurationSources.Count > 0)
			{
				var configErrors = new List<string>();
				if (!SheetXConfigSheet.TryParseCollection(m_configurationSources, configErrors.Add, out configData))
				{
					error = string.Join("\n", configErrors);
					return false;
				}
				configJson = SheetXConfigSheet.EmitJson(configData);
				configJsonPath = SheetXCollectionGenerator.JsonPathFor(m_settings, SheetXConstants.CONFIGURATION_SHEET);
			}

			var globalIssues = SheetXCollectionSettings.Validate(m_settings, Array.Empty<SheetXSheetBinding>());
			if (globalIssues.Count > 0)
			{
				error = string.Join("\n", globalIssues.Select(issue => issue.Message));
				return false;
			}
			if (!IncludesEveryCollectionBindingFromProcessedSources(out error))
				return false;
			try
			{
				SheetXCollectionGenerator.Emit(m_settings, Array.Empty<SheetXCollectionGeneratedTable>(), configData);
			}
			catch (InvalidOperationException ex)
			{
				error = ex.Message;
				return false;
			}

			foreach (var candidate in m_candidates)
			{
				var trial = m_accepted.Append(candidate).ToList();
				var issues = SheetXCollectionSettings.Validate(m_settings, trial.Select(item => item.Binding));
				if (issues.Count > 0)
				{
					SkipSheet(candidate.Binding,
						string.Join(" ", issues.Select(issue => issue.Message))
						+ " Fix: correct this sheet binding in Collection Management.", out _);
					continue;
				}
				try
				{
					SheetXCollectionGenerator.Emit(m_settings, trial.Select(item => item.Table).ToList(), configData);
					m_accepted.Add(candidate);
				}
				catch (InvalidOperationException ex)
				{
					if (configData != null)
					{
						try
						{
							SheetXCollectionGenerator.Emit(
								m_settings, trial.Select(item => item.Table).ToList(), configuration: null);
							error = ex.Message;
							return false;
						}
						catch (InvalidOperationException)
						{
							// Candidate is invalid without Configuration too; keep existing sheet-local skip policy.
						}
					}
					SkipSheet(candidate.Binding,
						ex.Message + " Fix: rename this sheet, generated field, nested object, or collection to make generated names unique.",
						out _);
				}
			}

			IReadOnlyDictionary<string, string> sources;
			try
			{
				sources = SheetXCollectionGenerator.EmitFiles(
					m_settings, m_accepted.Select(item => item.Table).ToList(), configData);
			}
			catch (InvalidOperationException ex)
			{
				error = ex.Message;
				return false;
			}

			if (m_accepted.Count == 0 && configData == null && !HasGeneratedConfigurationMarker())
			{
				FlushSucceeded = true;
				return true;
			}

			RequiresScriptReload = SourcesChanged(sources);
			List<FileSnapshot> snapshots;
			try
			{
				snapshots = CaptureSnapshots(sources.Keys, configJsonPath);
			}
			catch (Exception ex)
			{
				error = $"Could not prepare Collection output rollback: {ex.Message}";
				return false;
			}
			var context = new SheetXExportContext(
				new RollbackFileOutput(m_output, snapshots), discardStagedOnError: true);
			var writer = new SheetXWriter(m_settings, context);
			if (configData != null && !string.IsNullOrEmpty(configJson))
			{
				writer.Write(
					Path.GetDirectoryName(configJsonPath) ?? "", Path.GetFileName(configJsonPath), configJson,
					SheetXExportFileType.Json);
			}
			foreach (var candidate in m_accepted)
			{
				string path = candidate.Table.JsonPath;
				writer.Write(
					Path.GetDirectoryName(path) ?? "", Path.GetFileName(path), candidate.Json,
					SheetXExportFileType.Json);
			}
			foreach (var source in sources)
			{
				writer.Write(
					m_settings.ResolveCollectionCodeFolder(), source.Key, source.Value,
					SheetXExportFileType.ConfigScript);
			}
			context.Flush();

			var result = context.ToResult();
			if (!result.Success)
			{
				error = string.Join("\n", result.Errors);
				try
				{
					RestoreSnapshots(snapshots);
				}
				catch (Exception ex)
				{
					error += $"\nRestoring previous Collection output failed: {ex.Message}";
				}
				return false;
			}
			try
			{
				DeleteLegacyGeneratedSource();
			}
			catch (Exception ex)
			{
				error = $"Could not remove legacy Collection source: {ex.Message}";
				try
				{
					RestoreSnapshots(snapshots);
				}
				catch (Exception restoreException)
				{
					error += $"\nRestoring previous Collection output failed: {restoreException.Message}";
				}
				return false;
			}

			if (RequiresScriptReload)
			{
				SheetXCollectionBaker.RegisterPendingBake(
					m_settings, m_settings.autoLoadAfterExport, AcceptedBindingIdentities());
			}
			WroteArtifacts = true;
			FlushSucceeded = true;
			return true;
		}

		/// <summary>Completes accepted Existing Data Class output after callers refresh imported JSON.</summary>
		internal bool TryBakeAfterRefresh(out string error)
		{
			if (RequiresScriptReload)
			{
				error = null;
				return true;
			}
			return m_settings.autoLoadAfterExport
				? SheetXCollectionBaker.TryLoadData(
					m_settings, autoLoadOnly: true, AcceptedBindingIdentities(), out error)
				: SheetXCollectionBaker.TryFinishPendingBake(
					m_settings, autoLoadAfterExport: false, AcceptedBindingIdentities(), out error);
		}

		private List<FileSnapshot> CaptureSnapshots(
			IEnumerable<string> sourceFileNames,
			string configJsonPath)
		{
			string codeFolder = SheetXCollectionSettings.NormalizePath(
				m_settings.ResolveCollectionCodeFolder());
			string legacyPath = codeFolder + "/" + SheetXCollectionGenerator.LegacyFileName;
			var paths = m_accepted.Select(candidate => candidate.Table.JsonPath)
				.Concat(sourceFileNames.Select(fileName => codeFolder + "/" + fileName))
				.Append(legacyPath)
				.Append(legacyPath + ".meta");
			if (!string.IsNullOrEmpty(configJsonPath))
				paths = paths.Append(configJsonPath);
			return paths.Distinct(StringComparer.Ordinal).Select(path => new FileSnapshot
			{
				Path = path,
				Existed = File.Exists(path),
				Content = File.Exists(path) ? File.ReadAllBytes(path) : null,
			}).ToList();
		}

		private void DeleteLegacyGeneratedSource()
		{
			string path = Path.Combine(
				m_settings.ResolveCollectionCodeFolder(), SheetXCollectionGenerator.LegacyFileName);
			if (File.Exists(path))
				File.Delete(path);
			string metaPath = path + ".meta";
			if (File.Exists(metaPath))
				File.Delete(metaPath);
		}

		private static void RestoreSnapshots(IEnumerable<FileSnapshot> snapshots)
		{
			List<Exception> errors = null;
			foreach (var snapshot in snapshots)
			{
				try
				{
					RestoreSnapshot(snapshot);
				}
				catch (Exception ex)
				{
					errors ??= new List<Exception>();
					errors.Add(ex);
				}
			}
			if (errors != null)
				throw new AggregateException(errors);
		}

		private static void RestoreSnapshot(FileSnapshot snapshot)
		{
			if (snapshot.Existed)
			{
				string folder = Path.GetDirectoryName(snapshot.Path);
				if (!string.IsNullOrEmpty(folder))
					Directory.CreateDirectory(folder);
				File.WriteAllBytes(snapshot.Path, snapshot.Content);
			}
			else if (File.Exists(snapshot.Path))
			{
				File.Delete(snapshot.Path);
			}
		}

		private IReadOnlyList<PendingCollectionBakeBinding> AcceptedBindingIdentities()
		{
			return m_accepted.Select(candidate => new PendingCollectionBakeBinding
			{
				SourceId = candidate.Binding.sourceId,
				SheetName = candidate.Binding.sheetName,
			}).ToList();
		}

		private bool SourcesChanged(IReadOnlyDictionary<string, string> sources)
		{
			string folder = m_settings.ResolveCollectionCodeFolder();
			return sources.Any(source =>
			{
				string path = Path.Combine(folder, source.Key);
				return !File.Exists(path)
					|| !string.Equals(File.ReadAllText(path), source.Value, StringComparison.Ordinal);
			});
		}

		private bool HasGeneratedConfigurationMarker()
		{
			string path = Path.Combine(
				m_settings.ResolveCollectionCodeFolder(), SheetXCollectionGenerator.FileName);
			return File.Exists(path)
				&& File.ReadAllText(path).Contains(
					"const string Configuration", StringComparison.Ordinal);
		}

		private bool IncludesEveryCollectionBindingFromProcessedSources(out string error)
		{
			foreach (var binding in SheetXCollectionSettings.FilterActiveBindings(
				m_settings, m_settings.sheetBindings))
			{
				if (binding == null || binding.outputMode == SheetXSheetOutputMode.JsonOnly
					|| !m_processedSources.Contains(binding.sourceId)
					|| m_processed.Contains(BindingKey(binding)))
				{
					continue;
				}

				error = Context(binding.sourceId, binding.sheetName,
					"Collection source is shared, but this sheet from an exported source was not processed. Fix: select every Collection sheet in this source before export.");
				return false;
			}
			error = null;
			return true;
		}

		private static string BindingKey(SheetXSheetBinding binding)
			=> binding.sourceId + "\0" + binding.sheetName;

		private bool TryBind(
			string sourceId, string sheetName, SheetXSheetOutputMode expected,
			out SheetXSheetBinding binding, out string error)
		{
			error = null;
			binding = SheetXCollectionSettings.GetOrCreateBinding(m_settings, sourceId, sheetName);
			if (binding == null)
			{
				error = Context(sourceId, sheetName,
					"No settings asset exists for this binding. Fix: assign a SheetXSettings asset and retry.");
				m_error?.Invoke(error);
				return false;
			}
			if (binding.outputMode == SheetXSheetOutputMode.JsonOnly)
				return false;

			m_processed.Add(BindingKey(binding));
			m_processedSources.Add(binding.sourceId);
			if (binding.outputMode != expected)
			{
				return SkipSheet(binding,
					$"Binding mode is {binding.outputMode}, not {expected}. Fix: select the matching output mode.", out error);
			}
			return true;
		}

		private void Add(SheetXSheetBinding binding, string json, Action<SheetXCollectionGeneratedTable> fill)
		{
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
			m_candidates.Add(new Candidate { Binding = binding, Table = table, Json = json });
		}

		private bool SkipSheet(SheetXSheetBinding binding, string cause, out string error)
		{
			string key = BindingKey(binding);
			bool firstError = m_skipped.Add(key);
			error = Context(binding.sourceId, binding.sheetName,
				cause + " Sheet skipped; previous JSON was not updated.");
			if (firstError)
				m_error?.Invoke(error);
			return false;
		}

		private static string Context(string sourceId, string sheetName, string message)
			=> $"[SheetX Collections] Source '{sourceId}', sheet '{sheetName}': {message}";

		private static bool TryResolveRowType(string rowTypeName, out Type type, out string error)
		{
			type = null;
			error = null;
			if (string.IsNullOrWhiteSpace(rowTypeName))
			{
				error = "Existing Data Class needs a row type name.";
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
					{
						return type;
					}
				}
			}
			return null;
		}
	}
}
