/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace RCore.SheetX.Editor
{
	/// <summary>
	/// Holds what the preview knows between repaints: the last snapshot, whether it is still current, and
	/// how it maps onto a row type. Deliberately free of any <see cref="EditorWindow"/> reference so the
	/// staleness and revalidation rules are testable without IMGUI.
	/// </summary>
	internal sealed class SheetXStructurePreviewState
	{
		/// <summary>Most characters the JSON view will render. Past this a repaint costs more than it tells.</summary>
		internal const int JSON_CHAR_LIMIT = 50000;

		// Export resolves and validates the same row type, and skips the sheet when either fails, so these
		// states are blocking rather than advisory.
		private const string EXPORT_SKIPS = "Export skips this sheet until this is fixed.";

		private SheetXSheetPreviewData m_snapshot;
		private string m_error;
		private bool m_stale;
		private bool m_loading;
		private SheetXRowTypeMatch m_match;
		private string m_conversionError;
		private string m_prettyJson = "";
		private bool m_jsonTruncated;
		private string m_jsonNote;
		private int m_selectedTab;
		private string m_rowTypeError;

		/// <summary>The last successfully loaded snapshot, kept across a failed refresh.</summary>
		internal SheetXSheetPreviewData Snapshot => m_snapshot;

		/// <summary>True while a load is in flight, so the window paints a loading label instead of a snapshot.</summary>
		internal bool Loading => m_loading;

		/// <summary>
		/// Diagnostics worth rendering: the snapshot's generic ones only. Every row-type-dependent finding
		/// lives on <c>RowTypeDiagnostics</c> instead and is deliberately not rendered here, because the
		/// Data Class block re-derives all of it against the binding selected right now. The snapshot's copy
		/// names whichever class was bound at load time, so it is stale the moment that changes. Structural
		/// ownership, not phrase matching: a generic warning that happens to contain the same words as a
		/// row-type message is still shown.
		/// </summary>
		internal IReadOnlyList<string> Diagnostics
			=> m_snapshot?.Diagnostics ?? Array.Empty<string>();

		/// <summary>Why the last load failed, or null when it succeeded.</summary>
		internal string Error => m_error;

		/// <summary>True when <see cref="Snapshot"/> survived a later failed refresh and no longer reflects the source.</summary>
		internal bool Stale => m_stale;

		/// <summary>How the snapshot's top-level members map onto the selected row type, or null when not compared.</summary>
		internal SheetXRowTypeMatch Match => m_match;

		/// <summary>Why the snapshot's Json does not deserialize into the selected row type, or null.</summary>
		internal string ConversionError => m_conversionError;

		/// <summary>
		/// Why the bound row type blocks this sheet, or null. Separate from the unverifiable arm because
		/// these two states call for opposite advice: an unmodellable contract may still export fine,
		/// whereas an unresolvable or ineligible type makes the export skip the sheet outright.
		/// </summary>
		internal string RowTypeError => m_rowTypeError;

		/// <summary>True when there is class code worth putting on the clipboard.</summary>
		internal bool CanCopy => m_snapshot != null && !string.IsNullOrEmpty(m_snapshot.ClassCode);

		/// <summary>Top-level inferred fields the table renders. Empty in Generated Data Class mode.</summary>
		internal IReadOnlyList<SheetXSheetField> Fields
			=> m_snapshot?.Fields ?? (IReadOnlyList<SheetXSheetField>)Array.Empty<SheetXSheetField>();

		/// <summary>
		/// When the snapshot in hand was fetched, in local time, or an em dash before the first load. Carries
		/// the date as well as the clock: a preview window left open overnight would otherwise show a time
		/// that reads as minutes old. A failed refresh keeps the snapshot it kept, so this keeps that
		/// snapshot's timestamp, and the stale banner supplies the rest of the story.
		/// </summary>
		internal string FetchedLabel
			=> m_snapshot == null ? "—" : m_snapshot.FetchedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

		/// <summary>True when this snapshot carries exported Json, so the JSON tab is worth offering.</summary>
		internal bool HasJson => !string.IsNullOrEmpty(m_prettyJson);

		/// <summary>The Json formatted for reading, built once per snapshot and capped.</summary>
		internal string PrettyJson => m_prettyJson;

		/// <summary>True when <see cref="PrettyJson"/> is an excerpt rather than the whole document.</summary>
		internal bool JsonTruncated => m_jsonTruncated;

		/// <summary>What was cut from the JSON view, or null when it is complete.</summary>
		internal string JsonNote => m_jsonNote;

		/// <summary>Which tab is showing: 0 is the code, 1 the JSON. Never lands on a tab that has nothing.</summary>
		internal int SelectedTab
		{
			get => m_selectedTab;
			set => m_selectedTab = value == 1 && HasJson ? 1 : 0;
		}

		/// <summary>The declaration the draft would use for one JSON key, or null when it inferred none.</summary>
		/// <param name="jsonName">Exported JSON key to look up.</param>
		internal string DeclarationFor(string jsonName)
		{
			var fields = Fields;
			for (int i = 0; i < fields.Count; i++)
			{
				if (string.Equals(fields[i].JsonName, jsonName, StringComparison.Ordinal))
					return fields[i].Declaration;
			}
			return null;
		}

		/// <summary>
		/// Records one load outcome. A failure after a success keeps the old snapshot and marks it stale
		/// rather than blanking the window: the last known structure is still worth reading, as long as it
		/// is never presented as fresh.
		/// </summary>
		/// <param name="ok">Whether the load produced a snapshot.</param>
		/// <param name="data">The new snapshot, when <paramref name="ok"/> is true.</param>
		/// <param name="error">Why the load failed, when <paramref name="ok"/> is false.</param>
		internal void Apply(bool ok, SheetXSheetPreviewData data, string error)
		{
			if (ok)
			{
				m_snapshot = data;
				m_error = null;
				m_stale = false;
			}
			else
			{
				m_error = error;
				m_stale = m_snapshot != null;
			}

			// The comparison belonged to whatever snapshot was current when it ran, so it never carries over.
			m_match = null;
			m_conversionError = null;
			m_rowTypeError = null;

			// Formatting happens here, once, for the same reason ConversionError does: a 5000-row sheet
			// reformatted on every repaint locks the editor. A failed refresh keeps the snapshot it kept, so
			// it keeps that snapshot's JSON view too.
			if (ok)
			{
				BuildJsonView(m_snapshot?.Json);
				SelectedTab = m_selectedTab;
			}
		}

		// Never mutates Snapshot.Json: Revalidate deserializes the stored document, so a shortened copy would
		// turn a large sheet's validation into a lie.
		private void BuildJsonView(string json)
		{
			m_prettyJson = "";
			m_jsonTruncated = false;
			m_jsonNote = null;

			if (string.IsNullOrEmpty(json))
				return;

			if (json.Length > JSON_CHAR_LIMIT)
			{
				// Pretty-printing a document only to discard most of it costs the time the cap exists to save,
				// so an oversized export is excerpted raw instead.
				m_prettyJson = json.Substring(0, JSON_CHAR_LIMIT);
				m_jsonTruncated = true;
				m_jsonNote = $"Large JSON: showing a raw excerpt of the first {JSON_CHAR_LIMIT:N0} of "
					+ $"{json.Length:N0} characters. It is incomplete and will not parse as JSON on its own.";
				return;
			}

			string formatted;
			try
			{
				// DateParseHandling.None for the same reason inference uses it: a date-like string is a string,
				// and reformatting it here would show the reader something the export never wrote.
				using var input = new StringReader(json);
				using var reader = new JsonTextReader(input) { DateParseHandling = DateParseHandling.None };
				var token = JToken.ReadFrom(reader);
				formatted = token.ToString(Formatting.Indented).Replace("\r\n", "\n").Replace("\n", "\r\n");
			}
			catch (Exception)
			{
				// Json that will not parse is exactly what a reader opened this tab to see.
				formatted = json;
			}

			if (formatted.Length > JSON_CHAR_LIMIT)
			{
				m_prettyJson = formatted.Substring(0, JSON_CHAR_LIMIT);
				m_jsonTruncated = true;
				m_jsonNote = $"Formatting expanded this past {JSON_CHAR_LIMIT:N0} characters, so this is a raw "
					+ "excerpt: it is incomplete and will not parse as JSON on its own.";
				return;
			}

			m_prettyJson = formatted;
		}

		/// <summary>Explains one unmatched JSON member: what is lost, and the two ways to stop losing it.</summary>
		/// <param name="jsonName">The exported key with no destination in the selected class.</param>
		/// <param name="declaration">The draft's declaration for it, or null when none was inferred.</param>
		internal static string UnmatchedMemberGuidance(string jsonName, string declaration)
		{
			string text = $"Unmatched JSON member '{jsonName}': the selected Data Class has no member that "
				+ "accepts it, so this column's values are discarded when the exported JSON is loaded. "
				+ "Add a matching member to the class, or rename the sheet column so it matches an existing one.";

			if (string.IsNullOrEmpty(declaration))
				return text;

			// Inferred from the values this sheet happens to hold, so it is offered as a starting point. An
			// object or object[] fallback in particular is not something Unity serializes as-is.
			return text + "\n\nSuggested declaration, inferred from this sheet's values — a suggestion, not a "
				+ "guaranteed fix. Check it before pasting; an object or object[] fallback in particular is not "
				+ "something Unity serializes as-is.\n" + declaration;
		}

		/// <summary>Explains class members the export did not write, without claiming what they become.</summary>
		/// <param name="memberNames">Accepted destinations that never appeared in the JSON.</param>
		internal static string UnobservedMemberGuidance(IReadOnlyList<string> memberNames)
		{
			string names = memberNames == null || memberNames.Count == 0 ? "" : string.Join(", ", memberNames);
			return "Not present in exported data: " + names
				+ ".\nThis is informational. A blank cell is omitted from the export, so these members may simply "
				+ "have no values in this sheet right now. What each one holds after loading depends on the class.";
		}

		/// <summary>Keeps the specific reason coverage could not be established, and says what to do instead.</summary>
		/// <param name="note">The reason the comparison reported, or null.</param>
		internal static string UnverifiableGuidance(string note)
		{
			string reason = string.IsNullOrEmpty(note) ? "Member coverage cannot be verified." : note;
			return reason
				+ "\nThis check cannot model that contract, so it can neither confirm nor deny coverage here. "
				+ "Nothing is necessarily wrong with the data; confirm the mapping by inspecting the class.";
		}

		/// <summary>Keeps the deserializer's own message and points at the value it refused.</summary>
		/// <param name="message">The exception message from the trial deserialize.</param>
		internal static string ConversionFailureGuidance(string message)
		{
			return message
				+ "\nExport performs this same conversion, so it would refuse this sheet. Inspect the named value "
				+ "in the sheet and the declared type of the matching member, and correct whichever is wrong.";
		}

		/// <summary>Marks a load as started, so the window paints its loading label before the load runs.</summary>
		internal void BeginLoad()
		{
			m_loading = true;
		}

		/// <summary>
		/// Runs one load and records its outcome. A throw becomes an ordinary error: this tool reads
		/// user-authored spreadsheets, where the conversion path indexes columns and reads raw cells without
		/// guarding every case, and a window stuck on a loading label forever is the same lie as unlabelled
		/// staleness. The loading flag clears in a finally, so Refresh is never permanently disabled.
		/// </summary>
		/// <param name="load">Performs the load, reporting failure the same way the source does.</param>
		internal void Load(SheetXPreviewLoad load)
		{
			try
			{
				bool ok = load(out var data, out string error);
				Apply(ok, data, error);
			}
			catch (Exception ex)
			{
				Apply(false, null, $"Could not read this sheet: {ex.Message}");
			}
			finally
			{
				m_loading = false;
			}
		}

		/// <summary>
		/// Recompares the snapshot already in hand against <paramref name="rowType"/>. Never fetches: picking
		/// a different Data Class changes how the same exported data is read, not what the sheet holds.
		/// </summary>
		/// <param name="rowType">Row type to compare against, or null to clear the comparison.</param>
		internal void Revalidate(Type rowType) => Revalidate(rowType, rowType?.FullName);

		/// <summary>
		/// Recompares the snapshot against <paramref name="rowType"/>, and records why the binding blocks
		/// this sheet when the type is unresolvable or ineligible.
		/// </summary>
		/// <param name="rowType">Resolved row type, or null when the bound name did not resolve.</param>
		/// <param name="boundTypeName">The name the binding holds, so an unresolved type can still be named.</param>
		internal void Revalidate(Type rowType, string boundTypeName)
		{
			m_match = null;
			m_conversionError = null;
			m_rowTypeError = null;
			if (m_snapshot == null)
				return;

			if (rowType == null)
			{
				// No name bound at all is not a failure — the user simply has not picked a class. A name that
				// will not resolve is, and the header is already showing 'Missing: <name>', so saying
				// 'no row type is selected' here would contradict it and drop the actionable reason.
				if (!string.IsNullOrWhiteSpace(boundTypeName))
				{
					m_rowTypeError = $"Row type '{boundTypeName.Trim()}' was not found in any loaded assembly. "
						+ "Fix: re-pick the type in the Data Class column of the Edit Spreadsheets window; it may "
						+ "have been renamed, moved, or deleted. " + EXPORT_SKIPS;
				}
				return;
			}

			// Eligibility next. A type the export would refuse — no [SheetXBindable] or [Serializable], not
			// public, abstract, generic — can still have every member name line up, so comparing first would
			// report a clean match for a sheet the next export skips outright.
			if (!SheetXRowType.Validate(rowType, out string validationError))
			{
				m_rowTypeError = validationError + " " + EXPORT_SKIPS;
				return;
			}

			m_match = SheetXRowTypeMatch.Compare(m_snapshot.RootMemberNames, rowType);

			// Matching member names still deserialize badly when a value's type is wrong, and export checks
			// exactly this before it writes. Without it the preview would show a clean match for a sheet the
			// next export refuses.
			try
			{
				JsonConvert.DeserializeObject(
					string.IsNullOrEmpty(m_snapshot.Json) ? "[]" : m_snapshot.Json, rowType.MakeArrayType());
			}
			catch (Exception ex)
			{
				m_conversionError = ex.Message;
			}
		}

		/// <summary>Drops everything, so a window pointed at a different sheet starts empty.</summary>
		internal void Reset()
		{
			m_snapshot = null;
			m_error = null;
			m_stale = false;
			m_loading = false;
			m_match = null;
			m_conversionError = null;
			m_rowTypeError = null;
			m_prettyJson = "";
			m_jsonTruncated = false;
			m_jsonNote = null;
			m_selectedTab = 0;
		}
	}

	/// <summary>Performs one preview load, reporting failure the way <see cref="SheetXSheetJsonSource"/> does.</summary>
	/// <param name="data">The snapshot, when this returns true.</param>
	/// <param name="error">Why no snapshot could be taken, when this returns false.</param>
	/// <returns>True when a snapshot was produced.</returns>
	internal delegate bool SheetXPreviewLoad(out SheetXSheetPreviewData data, out string error);

	/// <summary>
	/// Shows what one sheet would export, without exporting it. Holds no serialized state, so a domain
	/// reload leaves it empty and it closes rather than showing a snapshot it can no longer attribute
	/// to a source.
	/// </summary>
	internal sealed class SheetXSheetStructureWindow : EditorWindow
	{
		private const string TITLE = "Sheet Structure";
		private const string NESTED_NOTE = "Top-level member check only; nested members are not compared.";
		private const string LEGACY_HELP =
			"Inferred from this sheet's exported JSON, so it is a draft rather than a contract:\n"
			+ "• A blank or omitted field is absent from the data, so it cannot appear here.\n"
			+ "• Inferred types are suggestions read from the values present, not declarations.\n";

		// The ID line depends on which namespace the load actually used, so claiming either unconditionally
		// would be false half the time.
		private const string IDS_SOURCE_LOCAL =
			"• IDs are resolved against this source only, as this tab's own export does, so the Export Multi "
			+ "Files list may resolve them differently.";
		private const string IDS_MULTI_FILE =
			"• IDs are resolved against every source in the Export Multi Files list, as that export does.";

		private SheetXSettings m_settings;
		private SheetXSheetSource m_source;
		private string m_sheetName;
		private readonly SheetXStructurePreviewState m_state = new SheetXStructurePreviewState();
		private Vector2 m_scroll;
		private SheetXSheetPreviewData m_validatedSnapshot;
		private string m_validatedRowTypeName;

		/// <summary>
		/// Opens the preview on one sheet and starts the first load.
		/// </summary>
		/// <param name="settings">Settings read for mode, bindings, and namespace. Never written.</param>
		/// <param name="source">Which spreadsheet the sheet belongs to.</param>
		/// <param name="sheetName">Sheet to preview.</param>
		internal static void Open(SheetXSettings settings, SheetXSheetSource source, string sheetName)
		{
			var window = GetWindow<SheetXSheetStructureWindow>(true, TITLE, true);
			window.titleContent = new GUIContent(TITLE);
			window.minSize = new Vector2(520, 420);
			window.m_settings = settings;
			window.m_source = source;
			window.m_sheetName = sheetName;
			window.m_state.Reset();
			window.m_validatedSnapshot = null;
			window.m_validatedRowTypeName = null;
			window.BeginLoad();
		}

		// Never called from OnGUI: a Google fetch blocks the editor for seconds, and a repaint that fetched
		// would do it on every mouse move. delayCall lets 'Loading...' paint first.
		private void BeginLoad()
		{
			if (m_state.Loading)
				return;
			m_state.BeginLoad();
			Repaint();
			EditorApplication.delayCall += () =>
			{
				// The window can be closed while the fetch is queued; Unity's overloaded null covers that.
				if (this == null)
					return;
				m_state.Load((out SheetXSheetPreviewData data, out string error) =>
					SheetXSheetJsonSource.TryLoad(m_settings, m_source, m_sheetName, out data, out error));
				m_validatedSnapshot = null;
				m_validatedRowTypeName = null;
				Repaint();
			};
		}

		private void OnGUI()
		{
			// Nothing here survives a domain reload, so a reloaded window has no sheet to describe.
			if (m_settings == null || m_source == null || string.IsNullOrEmpty(m_sheetName))
			{
				Close();
				return;
			}

			DrawToolbar();
			DrawHeader();

			if (m_state.Loading)
			{
				EditorGUILayout.LabelField("Loading…");
				return;
			}

			SyncValidation();

			if (m_state.Stale)
				EditorGUILayout.HelpBox("Stale — last refresh failed: " + m_state.Error, MessageType.Warning);
			else if (!string.IsNullOrEmpty(m_state.Error))
				EditorGUILayout.HelpBox(m_state.Error, MessageType.Error);

			var snapshot = m_state.Snapshot;
			if (snapshot == null)
				return;

			if (CurrentMode() != snapshot.Mode)
			{
				EditorGUILayout.HelpBox(
					"Output Mode changed since this preview was taken. Press Refresh to reload it.",
					MessageType.Info);
			}

			m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
			DrawDiagnostics();
			if (snapshot.Mode == SheetXSheetOutputMode.ExistingDataClass)
				DrawDataClassBlock();
			DrawFieldTable(snapshot);
			DrawTabs();
			if (m_state.SelectedTab == 1)
				DrawJson();
			else
				DrawCode(snapshot);
			EditorGUILayout.EndScrollView();
		}

		private void DrawToolbar()
		{
			GUILayout.BeginHorizontal(EditorStyles.toolbar);
			using (new EditorGUI.DisabledScope(m_state.Loading))
			{
				if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
				{
					// Refresh means 're-read the source'. The Google cross-spreadsheet ID map is kept for the
					// session, so serving it here would answer an explicit re-read with cached data — exactly
					// the staleness this button exists to clear.
					SheetXSheetJsonSource.InvalidateGoogleIdCache();
					BeginLoad();
				}
			}
			using (new EditorGUI.DisabledScope(!m_state.CanCopy))
			{
				if (GUILayout.Button("Copy Code", EditorStyles.toolbarButton, GUILayout.Width(80)))
					EditorGUIUtility.systemCopyBuffer = m_state.Snapshot.ClassCode;
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}

		private void DrawHeader()
		{
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUILayout.LabelField("Source", m_source.Id ?? "");
			EditorGUILayout.LabelField("Sheet", m_sheetName);
			EditorGUILayout.LabelField("Output Mode", CurrentMode().ToString());
			EditorGUILayout.LabelField("Fetched", m_state.FetchedLabel);
			EditorGUILayout.LabelField("Data Class", DataClassLabel());
			EditorGUILayout.EndVertical();
		}

		// Reads the state's filtered list, not the snapshot's raw one: the row-type mapping entry is owned by
		// the Data Class block, which reports it against the Data Class selected right now.
		private void DrawDiagnostics()
		{
			var diagnostics = m_state.Diagnostics;
			for (int i = 0; i < diagnostics.Count; i++)
				EditorGUILayout.HelpBox(diagnostics[i], MessageType.Warning);
		}

		private void DrawDataClassBlock()
		{
			EditorGUILayout.LabelField("Existing Data Class", EditorStyles.boldLabel);

			// A blocking binding problem outranks everything else: export would skip this sheet, so the
			// reassuring 'nothing is necessarily wrong' wording of the unverifiable arm would be untrue here.
			if (!string.IsNullOrEmpty(m_state.RowTypeError))
			{
				EditorGUILayout.HelpBox(m_state.RowTypeError, MessageType.Error);
				EditorGUILayout.LabelField(NESTED_NOTE, EditorStyles.miniLabel);
				return;
			}

			var match = m_state.Match;
			if (match == null)
			{
				EditorGUILayout.HelpBox(
					SheetXStructurePreviewState.UnverifiableGuidance(
						"Member coverage cannot be verified: no row type is selected."),
					MessageType.Warning);
				EditorGUILayout.LabelField(NESTED_NOTE, EditorStyles.miniLabel);
				return;
			}

			switch (match.State)
			{
				case SheetXRowTypeMatchState.EmptyData:
					EditorGUILayout.HelpBox("Cannot infer structure from empty exported data", MessageType.Info);
					break;

				case SheetXRowTypeMatchState.Unverifiable:
					EditorGUILayout.HelpBox(
						SheetXStructurePreviewState.UnverifiableGuidance(match.Note), MessageType.Warning);
					break;

				default:
					DrawComparedMembers(match);
					break;
			}

			EditorGUILayout.LabelField(NESTED_NOTE, EditorStyles.miniLabel);
		}

		private void DrawComparedMembers(SheetXRowTypeMatch match)
		{
			for (int i = 0; i < match.UnmatchedJsonMembers.Count; i++)
			{
				string jsonName = match.UnmatchedJsonMembers[i];
				EditorGUILayout.HelpBox(
					SheetXStructurePreviewState.UnmatchedMemberGuidance(jsonName, m_state.DeclarationFor(jsonName)),
					MessageType.Warning);
			}

			if (match.UnobservedClassMembers.Count > 0)
			{
				EditorGUILayout.HelpBox(
					SheetXStructurePreviewState.UnobservedMemberGuidance(match.UnobservedClassMembers),
					MessageType.Info);
			}

			// A conversion failure replaces the success line rather than sitting beside it: export would
			// refuse this sheet, so 'No unmatched top-level JSON members' would read as an all-clear it is not.
			if (!string.IsNullOrEmpty(m_state.ConversionError))
			{
				EditorGUILayout.HelpBox(
					SheetXStructurePreviewState.ConversionFailureGuidance(m_state.ConversionError), MessageType.Error);
			}
			else if (!match.HasUnmatched)
				EditorGUILayout.HelpBox("No unmatched top-level JSON members", MessageType.Info);
		}

		// Reads the metadata the snapshot already carries, so a repaint neither parses Json nor re-infers.
		// Above the code because it is the summary: a reader scans it, then drops into the code for detail.
		private void DrawFieldTable(SheetXSheetPreviewData snapshot)
		{
			EditorGUILayout.LabelField("Top-level fields", EditorStyles.boldLabel);

			if (snapshot.Mode == SheetXSheetOutputMode.GeneratedDataClass)
			{
				EditorGUILayout.LabelField(
					"Generated from the sheet's schema — see the code below.", EditorStyles.miniLabel);
				return;
			}

			var fields = m_state.Fields;
			if (fields.Count == 0)
			{
				EditorGUILayout.LabelField("No top-level fields were inferred.", EditorStyles.miniLabel);
				return;
			}

			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("JSON key", EditorStyles.miniBoldLabel, GUILayout.MinWidth(120));
			EditorGUILayout.LabelField("C# field", EditorStyles.miniBoldLabel, GUILayout.MinWidth(120));
			EditorGUILayout.LabelField("Type", EditorStyles.miniBoldLabel, GUILayout.MinWidth(100));
			EditorGUILayout.EndHorizontal();

			for (int i = 0; i < fields.Count; i++)
			{
				var field = fields[i];
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(new GUIContent(field.JsonName, field.JsonName),
					EditorStyles.miniLabel, GUILayout.MinWidth(120));
				EditorGUILayout.LabelField(field.FieldName, EditorStyles.miniLabel, GUILayout.MinWidth(120));
				// The fallback reason is the tooltip rather than a fourth column: it is the same sentence the
				// draft comment carries, read from the same record, so the two cannot drift.
				EditorGUILayout.LabelField(
					new GUIContent(field.FallbackReason == null ? field.FieldType : field.FieldType + " *",
						field.FallbackReason ?? field.FieldType),
					EditorStyles.miniLabel, GUILayout.MinWidth(100));
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndVertical();

			EditorGUILayout.LabelField(
				"Top-level fields only; * marks an inferred fallback, explained in the code comments. "
				+ "Nested types appear in the code below.",
				EditorStyles.miniLabel);
		}

		private void DrawTabs()
		{
			if (!m_state.HasJson)
				return;

			m_state.SelectedTab = GUILayout.Toolbar(
				m_state.SelectedTab, new[] { "Class Code", "JSON" }, GUILayout.Width(200));
		}

		private void DrawJson()
		{
			if (m_state.JsonTruncated)
				EditorGUILayout.HelpBox(m_state.JsonNote, MessageType.Warning);

			// Formatted once when the snapshot was applied; this only paints it.
			EditorGUILayout.TextArea(m_state.PrettyJson, GUILayout.ExpandHeight(true));
		}

		private void DrawCode(SheetXSheetPreviewData snapshot)
		{
			bool generated = snapshot.Mode == SheetXSheetOutputMode.GeneratedDataClass;
			EditorGUILayout.LabelField(
				generated ? "Generated class preview" : "Inferred draft from exported JSON",
				EditorStyles.boldLabel);
			if (!generated)
			{
				EditorGUILayout.HelpBox(
					LEGACY_HELP + (snapshot.MultiFileIds ? IDS_MULTI_FILE : IDS_SOURCE_LOCAL),
					MessageType.Info);
			}

			// Assigned to nothing, so edits are discarded on the next repaint; a TextArea is still what lets
			// the reader select and copy a fragment rather than the whole file.
			EditorGUILayout.TextArea(snapshot.ClassCode ?? "", GUILayout.ExpandHeight(true));
		}

		// Compares against the snapshot instance as well as the type name, so a fresh load revalidates even
		// when the Data Class never changed.
		private void SyncValidation()
		{
			var snapshot = m_state.Snapshot;
			string rowTypeName = snapshot != null && snapshot.Mode == SheetXSheetOutputMode.ExistingDataClass
				? CurrentBinding()?.rowTypeName
				: null;

			if (ReferenceEquals(m_validatedSnapshot, snapshot)
				&& string.Equals(m_validatedRowTypeName, rowTypeName, StringComparison.Ordinal))
			{
				return;
			}

			m_validatedSnapshot = snapshot;
			m_validatedRowTypeName = rowTypeName;
			// The bound name travels with the resolved type so a name that no longer resolves can still be
			// named on screen, matching the 'Missing: <name>' the header shows for the same binding.
			m_state.Revalidate(ResolveRowType(rowTypeName), rowTypeName);
		}

		private string DataClassLabel()
		{
			switch (CurrentMode())
			{
				case SheetXSheetOutputMode.GeneratedDataClass:
					return SheetXCollectionNaming.RowTypeName(m_sheetName);

				case SheetXSheetOutputMode.ExistingDataClass:
					string rowTypeName = CurrentBinding()?.rowTypeName;
					if (string.IsNullOrWhiteSpace(rowTypeName))
						return "<none>";
					var rowType = ResolveRowType(rowTypeName);
					return rowType == null ? "Missing: " + rowTypeName : rowType.FullName;

				default:
					return "—";
			}
		}

		private SheetXSheetOutputMode CurrentMode()
			=> CurrentBinding()?.outputMode ?? SheetXSheetOutputMode.JsonOnly;

		// Read-only on purpose: GetOrCreateBinding appends a binding, so merely looking at a sheet would
		// change the settings asset.
		private SheetXSheetBinding CurrentBinding()
		{
			return m_settings?.sheetBindings?.FirstOrDefault(binding =>
				binding != null
				&& string.Equals(binding.sourceId, m_source?.Id, StringComparison.Ordinal)
				&& string.Equals(binding.sheetName, m_sheetName, StringComparison.Ordinal));
		}

		// binding.rowTypeName stores an AssemblyQualifiedName, so Type.GetType resolves it directly; the
		// TypeCache pass covers a type whose assembly identity has since moved.
		private static Type ResolveRowType(string rowTypeName)
		{
			if (string.IsNullOrWhiteSpace(rowTypeName))
				return null;
			string trimmed = rowTypeName.Trim();
			return Type.GetType(trimmed, throwOnError: false)
				?? TypeCache.GetTypesWithAttribute<SheetXBindableAttribute>().FirstOrDefault(type =>
					string.Equals(type.AssemblyQualifiedName, trimmed, StringComparison.Ordinal)
					|| string.Equals(type.FullName, trimmed, StringComparison.Ordinal)
					|| string.Equals(type.Name, trimmed, StringComparison.Ordinal));
		}
	}
}
