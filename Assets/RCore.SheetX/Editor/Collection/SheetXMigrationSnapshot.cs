/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace RCore.SheetX.Editor
{
	/// <summary>
	/// Preserves what a collection depth change replaces, so a bake that fails on the far side of
	/// the domain reload can be undone. The per-bake rollback in SheetXCollectionBaker cannot reach
	/// that far: its snapshots live in locals, and the only state crossing the reload is the
	/// pending-bake entry, which carries no sources. This store is a file under Library/ so it also
	/// survives an editor restart mid-migration.
	/// </summary>
	internal static class SheetXMigrationSnapshot
	{
		private sealed class Entry
		{
			public string SettingsAssetPath;
			public List<string> ChangedCollections = new List<string>();
			public Dictionary<string, string> PreviousSources = new Dictionary<string, string>();
		}

		// Not Directory.GetCurrentDirectory(): the cwd is process-mutable and native file dialogs are
		// a known source of cwd changes. A cwd move between Capture and TryRestore would make Exists
		// report false and silently lose the rollback route. Application.dataPath is fixed for the
		// process lifetime.
		private static string StorePath => Path.GetFullPath(
			Path.Combine(Application.dataPath, "..", "Library", "SheetX", "migration-snapshot.json"));

		/// <summary>
		/// True while a captured snapshot is waiting to be restored or cleared.
		/// </summary>
		internal static bool Exists => File.Exists(StorePath);

		/// <summary>
		/// Records the sources a depth change is about to overwrite. Call before writing any
		/// generated file; a no-op when nothing changed depth.
		/// </summary>
		internal static void Capture(
			SheetXSettings settings,
			IReadOnlyCollection<string> changedCollections,
			IReadOnlyDictionary<string, string> previousSources)
		{
			if (changedCollections == null || changedCollections.Count == 0)
				return;

			// An un-restored snapshot is the older, true pre-migration state; a second capture
			// would only store the half-migrated sources. Keep the first.
			if (Exists)
				return;

			var entry = new Entry
			{
				SettingsAssetPath = settings == null ? "" : AssetDatabase.GetAssetPath(settings),
				ChangedCollections = changedCollections.ToList(),
				PreviousSources = previousSources?.ToDictionary(pair => pair.Key, pair => pair.Value)
					?? new Dictionary<string, string>(),
			};
			SheetXHelper.WriteFile(
				Path.GetDirectoryName(StorePath) ?? "",
				Path.GetFileName(StorePath),
				JsonConvert.SerializeObject(entry, Formatting.Indented));
		}

		/// <summary>
		/// Reads what the pending snapshot would revert, without touching any file. Capture keeps
		/// the first snapshot, so a snapshot left behind by an abandoned migration is the one a
		/// later restore would apply — surfacing its collection names lets the caller confirm
		/// before reverting sources that belong to a different migration.
		/// </summary>
		/// <param name="collections">Collection names the snapshot was captured for. Never null.</param>
		/// <param name="settingsAssetPath">Asset path of the settings the snapshot came from.</param>
		/// <returns>True when a readable snapshot is pending.</returns>
		internal static bool TryPeek(out IReadOnlyList<string> collections, out string settingsAssetPath)
		{
			collections = Array.Empty<string>();
			settingsAssetPath = "";
			if (!TryLoad(out var entry, out _))
				return false;
			collections = entry.ChangedCollections ?? new List<string>();
			settingsAssetPath = entry.SettingsAssetPath ?? "";
			return true;
		}

		// Shared by TryPeek and TryRestore: a store that cannot be parsed is discarded here, once.
		private static bool TryLoad(out Entry entry, out string error)
		{
			entry = null;
			error = null;
			if (!Exists)
			{
				error = "No migration snapshot to restore.";
				return false;
			}

			try
			{
				entry = JsonConvert.DeserializeObject<Entry>(File.ReadAllText(StorePath));
			}
			catch (Exception ex)
			{
				// An unparseable store (editor killed mid-write) has zero recovery value, and with
				// Capture now refusing to overwrite it, leaving it in place wedges every later
				// restore on the same error forever.
				Clear();
				error = $"Migration snapshot could not be read and was discarded: {ex.Message}";
				return false;
			}
			if (entry == null)
			{
				Clear();
				error = "Migration snapshot was empty and was discarded.";
				return false;
			}
			return true;
		}

		/// <summary>
		/// Puts the previous generated sources back and clears the snapshot. Only sources: asset
		/// data is restored by the bake that follows recompilation, because writing old data into a
		/// type that no longer matches is what corrupts it.
		/// </summary>
		internal static bool TryRestore(out string error)
		{
			if (!TryLoad(out var entry, out error))
				return false;

			try
			{
				foreach (var source in entry.PreviousSources)
				{
					// Write-beside-and-swap, not truncate-in-place: a process death mid-restore would
					// otherwise leave an empty .cs behind, from the one operation meant to un-break the
					// project. WriteFile also creates the folder and omits the BOM.
					SheetXHelper.WriteFile(
						Path.GetDirectoryName(source.Key) ?? "",
						Path.GetFileName(source.Key),
						source.Value);
				}
			}
			catch (Exception ex)
			{
				error = $"Migration snapshot could not be restored: {ex.Message}";
				return false;
			}

			Clear();
			return true;
		}

		/// <summary>
		/// Discards the snapshot. Call once a bake finally succeeds, or after a restore.
		/// </summary>
		internal static void Clear()
		{
			if (Exists)
				File.Delete(StorePath);
		}
	}
}
