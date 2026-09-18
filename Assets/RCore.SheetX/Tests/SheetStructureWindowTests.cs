/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Linq;
using NUnit.Framework;
using RCore.SheetX.Editor;
using UnityEngine;

namespace RCore.SheetX.Tests
{
	/// <summary>
	/// Stands in for a consuming project's row type in the preview's validation block: 'id' is an int,
	/// so a sheet holding text there is a conversion failure without being a member mismatch.
	/// </summary>
	[SheetXBindable]
	[Serializable]
	public sealed class StructureWindowRow
	{
		public int id;
		public string name;
	}

	/// <summary>
	/// Same members as <see cref="StructureWindowRow"/> but without the marker, so the export would refuse
	/// it. Exists to prove a name-perfect match cannot silently pass the eligibility check.
	/// </summary>
	[Serializable]
	public sealed class UnmarkedStructureWindowRow
	{
		public int id;
		public string name;
	}

	public class SheetStructureWindowTests
	{
		#region Snapshot state

		[Test]
		public void success_clears_the_error_and_enables_copy()
		{
			var state = new SheetXStructurePreviewState();
			state.Apply(false, null, "boom");

			var data = Snapshot("[{\"id\":1}]", "public class HeroesSX { }");
			state.Apply(true, data, null);

			Assert.That(state.Snapshot, Is.SameAs(data));
			Assert.That(state.Error, Is.Null);
			Assert.That(state.Stale, Is.False);
			Assert.That(state.CanCopy, Is.True);
		}

		[Test]
		public void success_without_class_code_cannot_copy()
		{
			var state = new SheetXStructurePreviewState();

			state.Apply(true, Snapshot("[{\"id\":1}]", ""), null);

			Assert.That(state.CanCopy, Is.False);
		}

		[Test]
		public void failed_refresh_after_a_success_keeps_the_snapshot_and_marks_it_stale()
		{
			var state = new SheetXStructurePreviewState();
			var data = Snapshot("[{\"id\":\"abc\"}]", "public class HeroesSX { }", "id");
			state.Apply(true, data, null);
			state.Revalidate(typeof(StructureWindowRow));
			Assert.That(state.Match, Is.Not.Null, "Fixture check: there must be a comparison to lose.");
			Assert.That(state.ConversionError, Is.Not.Null, "Fixture check: there must be a diagnostic to lose.");

			state.Apply(false, null, "network is down");

			Assert.That(state.Snapshot, Is.SameAs(data), "A failed refresh discarded the last good snapshot.");
			Assert.That(state.Stale, Is.True);
			Assert.That(state.Error, Is.EqualTo("network is down"));
			Assert.That(state.CanCopy, Is.True, "The last good code is still copyable.");
			// A comparison made against a snapshot that is now stale must not survive as if it were current.
			Assert.That(state.Match, Is.Null, "A failed refresh kept the previous comparison.");
			Assert.That(state.ConversionError, Is.Null);
		}

		[Test]
		public void failure_without_a_prior_snapshot_is_an_error_not_a_stale_snapshot()
		{
			var state = new SheetXStructurePreviewState();

			state.Apply(false, null, "Sheet 'Ghosts' was not found in the workbook.");

			Assert.That(state.Snapshot, Is.Null);
			Assert.That(state.Stale, Is.False, "Nothing was ever loaded, so nothing is stale.");
			Assert.That(state.Error, Is.EqualTo("Sheet 'Ghosts' was not found in the workbook."));
			Assert.That(state.CanCopy, Is.False);
		}

		[Test]
		public void a_success_after_a_stale_failure_clears_stale()
		{
			var state = new SheetXStructurePreviewState();
			state.Apply(true, Snapshot("[{\"id\":1}]", "a"), null);
			state.Apply(false, null, "network is down");

			state.Apply(true, Snapshot("[{\"id\":2}]", "b"), null);

			Assert.That(state.Stale, Is.False);
			Assert.That(state.Error, Is.Null);
		}

		#endregion

		#region Revalidation

		[Test]
		public void revalidate_reuses_the_snapshot_and_reports_a_clean_match()
		{
			var state = new SheetXStructurePreviewState();
			var data = Snapshot("[{\"id\":1,\"name\":\"Alpha\"}]", "code", "id", "name");
			state.Apply(true, data, null);

			state.Revalidate(typeof(StructureWindowRow));

			Assert.That(state.Snapshot, Is.SameAs(data), "Revalidate refetched instead of reusing the snapshot.");
			Assert.That(state.Match, Is.Not.Null);
			Assert.That(state.Match.State, Is.EqualTo(SheetXRowTypeMatchState.Compared));
			Assert.That(state.Match.HasUnmatched, Is.False);
			Assert.That(state.ConversionError, Is.Null);
		}

		[Test]
		public void revalidate_reports_an_unmatched_json_member()
		{
			var state = new SheetXStructurePreviewState();
			state.Apply(true, Snapshot("[{\"id\":1,\"rank\":3}]", "code", "id", "rank"), null);

			state.Revalidate(typeof(StructureWindowRow));

			Assert.That(state.Match.HasUnmatched, Is.True);
			Assert.That(state.Match.UnmatchedJsonMembers, Is.EqualTo(new[] { "rank" }));
		}

		[Test]
		public void revalidate_reports_a_conversion_failure_on_a_matching_member_set()
		{
			var state = new SheetXStructurePreviewState();
			// Every member matches by name, so only the deserialize catches that export would refuse it.
			state.Apply(true, Snapshot("[{\"id\":\"abc\"}]", "code", "id"), null);

			state.Revalidate(typeof(StructureWindowRow));

			Assert.That(state.Match.HasUnmatched, Is.False, "Member names match; only the value type is wrong.");
			Assert.That(state.ConversionError, Is.Not.Null.And.Not.Empty,
				"A preview that showed a clean match here would lie about what export does.");
		}

		[Test]
		public void revalidate_with_null_clears_the_match()
		{
			var state = new SheetXStructurePreviewState();
			state.Apply(true, Snapshot("[{\"id\":\"abc\"}]", "code", "id"), null);
			state.Revalidate(typeof(StructureWindowRow));

			state.Revalidate(null);

			Assert.That(state.Match, Is.Null);
			Assert.That(state.ConversionError, Is.Null);
			Assert.That(state.Snapshot, Is.Not.Null, "Clearing the match must not drop the snapshot.");
		}

		[Test]
		public void revalidate_without_a_snapshot_is_a_no_op()
		{
			var state = new SheetXStructurePreviewState();

			state.Revalidate(typeof(StructureWindowRow));

			Assert.That(state.Match, Is.Null);
			Assert.That(state.ConversionError, Is.Null);
		}

		[Test]
		public void a_new_snapshot_drops_the_previous_match()
		{
			var state = new SheetXStructurePreviewState();
			state.Apply(true, Snapshot("[{\"id\":1}]", "code", "id"), null);
			state.Revalidate(typeof(StructureWindowRow));

			state.Apply(true, Snapshot("[{\"id\":2}]", "code", "id"), null);

			Assert.That(state.Match, Is.Null, "A new snapshot must not keep the old snapshot's comparison.");
		}

		[Test]
		public void reset_clears_everything()
		{
			var state = new SheetXStructurePreviewState();
			state.Apply(true, Snapshot("[{\"id\":\"abc\"}]", "code", "id"), null);
			state.Revalidate(typeof(StructureWindowRow));
			state.Apply(false, null, "network is down");

			state.Reset();

			Assert.That(state.Snapshot, Is.Null);
			Assert.That(state.Error, Is.Null);
			Assert.That(state.Stale, Is.False);
			Assert.That(state.Match, Is.Null);
			Assert.That(state.ConversionError, Is.Null);
			Assert.That(state.CanCopy, Is.False);
		}

		#endregion

		#region Loading

		[Test]
		public void a_throwing_load_surfaces_an_error_and_never_sticks_on_loading()
		{
			var state = new SheetXStructurePreviewState();
			state.BeginLoad();
			Assert.That(state.Loading, Is.True, "Fixture check: the load must be in flight.");

			// The conversion path indexes columns and reads raw NPOI cells without guarding every case, so a
			// malformed workbook throws rather than returning false. A window stuck on 'Loading...' forever,
			// with Refresh permanently disabled, would be recoverable only by closing it.
			state.Load((out SheetXSheetPreviewData data, out string error)
				=> throw new IndexOutOfRangeException("Index was outside the bounds of the array."));

			Assert.That(state.Loading, Is.False, "A throwing load left the window stuck on its loading label.");
			Assert.That(state.Error, Does.Contain("Index was outside the bounds of the array."),
				"A throwing load must surface a real error, not a silent permanent loading state.");
			Assert.That(state.Snapshot, Is.Null);
		}

		[Test]
		public void a_throwing_refresh_after_a_success_is_stale_not_stuck()
		{
			var state = new SheetXStructurePreviewState();
			var data = Snapshot("[{\"id\":1}]", "code");
			state.Load((out SheetXSheetPreviewData d, out string e) =>
			{
				d = data;
				e = null;
				return true;
			});

			state.BeginLoad();
			state.Load((out SheetXSheetPreviewData d, out string e)
				=> throw new InvalidOperationException("workbook is corrupt"));

			Assert.That(state.Loading, Is.False);
			Assert.That(state.Stale, Is.True);
			Assert.That(state.Snapshot, Is.SameAs(data), "The last good snapshot must survive a throwing refresh.");
			Assert.That(state.Error, Does.Contain("workbook is corrupt"));
		}

		[Test]
		public void a_successful_load_clears_loading_and_records_the_snapshot()
		{
			var state = new SheetXStructurePreviewState();
			var data = Snapshot("[{\"id\":1}]", "code");
			state.BeginLoad();

			state.Load((out SheetXSheetPreviewData d, out string e) =>
			{
				d = data;
				e = null;
				return true;
			});

			Assert.That(state.Loading, Is.False);
			Assert.That(state.Snapshot, Is.SameAs(data));
			Assert.That(state.Error, Is.Null);
		}

		[Test]
		public void a_load_reporting_failure_clears_loading()
		{
			var state = new SheetXStructurePreviewState();
			state.BeginLoad();

			state.Load((out SheetXSheetPreviewData d, out string e) =>
			{
				d = null;
				e = "Sheet 'Ghosts' was not found in the workbook.";
				return false;
			});

			Assert.That(state.Loading, Is.False);
			Assert.That(state.Error, Is.EqualTo("Sheet 'Ghosts' was not found in the workbook."));
		}

		[Test]
		public void reset_clears_a_load_left_in_flight()
		{
			var state = new SheetXStructurePreviewState();
			state.BeginLoad();

			state.Reset();

			Assert.That(state.Loading, Is.False);
		}

		#endregion

		#region Diagnostics ownership

		[Test]
		public void row_type_diagnostics_are_never_rendered_beside_the_live_result()
		{
			var state = new SheetXStructurePreviewState();
			var data = Snapshot("[{\"id\":\"abc\"}]", "code", "id");
			// All four of the row-type findings the source can bake, on their own member.
			data.RowTypeDiagnostics = new[]
			{
				"Existing Data Class needs a row type name. Fix: pick a type in the Data Class column.",
				"row type 'GoneType' was not found in any loaded assembly. Fix: re-pick the type.",
				"row type 'RCore.SheetX.Tests.UnmarkedRow' is missing [SheetXBindable]. Fix: add it.",
				"JSON does not map onto 'RCore.SheetX.Tests.StaleStructureWindowRow': boom Fix: correct values.",
			};
			data.Diagnostics = new[] { "A warning that has nothing to do with the row type." };
			state.Apply(true, data, null);

			state.Revalidate(typeof(StructureWindowRow));

			Assert.That(state.ConversionError, Is.Not.Null, "The live diagnostic must still be reported.");
			Assert.That(state.Diagnostics, Is.EqualTo(new[] { "A warning that has nothing to do with the row type." }),
				"Row-type findings have one owner: the Data Class block, which tracks the current binding.");
		}

		[Test]
		public void a_data_class_change_drops_every_diagnostic_naming_the_previous_type()
		{
			var state = new SheetXStructurePreviewState();
			var data = Snapshot("[{\"id\":1,\"name\":\"Alpha\"}]", "code", "id", "name");
			// Baked at load time against whichever class was bound then — all four go stale together.
			data.RowTypeDiagnostics = new[]
			{
				"row type 'StaleStructureWindowRow' was not found in any loaded assembly. Fix: re-pick it.",
				"JSON does not map onto 'RCore.SheetX.Tests.StaleStructureWindowRow': boom Fix: correct values.",
			};
			state.Apply(true, data, null);

			// The user picks a Data Class the data does map onto. The stale warnings must not sit above it.
			state.Revalidate(typeof(StructureWindowRow));

			Assert.That(state.ConversionError, Is.Null, "Fixture check: the new type maps cleanly.");
			Assert.That(state.Match.HasUnmatched, Is.False);
			Assert.That(state.Diagnostics.Any(d => d.Contains("StaleStructureWindowRow")), Is.False,
				"A diagnostic naming the previous Data Class contradicts the live result above it.");
			Assert.That(state.Diagnostics, Is.Empty);
		}

		[Test]
		public void a_generic_warning_containing_row_type_wording_is_still_shown()
		{
			var state = new SheetXStructurePreviewState();
			var data = Snapshot("[{\"id\":1}]", "code", "id");
			// Ownership is structural now, so a generic warning quoting a user's sheet text that happens to
			// read like a row-type message must survive. The old substring filter swallowed exactly this.
			const string generic = "Column 'notes' does not map onto anything the converter understands.";
			data.Diagnostics = new[] { generic };
			state.Apply(true, data, null);

			state.Revalidate(typeof(StructureWindowRow));

			Assert.That(state.Diagnostics, Is.EqualTo(new[] { generic }),
				"Filtering by phrase hid generic warnings that merely shared wording.");
		}

		#endregion

		#region Row type eligibility

		[Test]
		public void an_unmarked_row_type_cannot_report_a_clean_match()
		{
			var state = new SheetXStructurePreviewState();
			// Every member name lines up, so only an eligibility check catches that export would refuse it.
			state.Apply(true, Snapshot("[{\"id\":1,\"name\":\"Alpha\"}]", "code", "id", "name"), null);

			state.Revalidate(typeof(UnmarkedStructureWindowRow));

			// An ineligible type is a blocking binding problem, so it is reported through RowTypeError
			// rather than the unverifiable arm, whose wording is reassuring and would be untrue here.
			Assert.That(state.Match, Is.Null,
				"A type the export refuses must never produce a comparison that could read as clean.");
			Assert.That(state.RowTypeError, Does.Contain("SheetXBindable"),
				"The reason must stay actionable, not collapse into a generic failure.");
		}

		[Test]
		public void an_eligible_row_type_still_compares_normally()
		{
			var state = new SheetXStructurePreviewState();
			state.Apply(true, Snapshot("[{\"id\":1,\"name\":\"Alpha\"}]", "code", "id", "name"), null);

			state.Revalidate(typeof(StructureWindowRow));

			Assert.That(state.Match.State, Is.EqualTo(SheetXRowTypeMatchState.Compared),
				"The eligibility guard must not swallow the ordinary comparison path.");
		}

		[Test]
		public void unrelated_diagnostics_survive_and_a_null_diagnostics_list_is_empty()
		{
			var state = new SheetXStructurePreviewState();
			Assert.That(state.Diagnostics, Is.Empty, "No snapshot means nothing to render.");

			var data = Snapshot("[{\"id\":1}]", "code", "id");
			data.Diagnostics = null;
			state.Apply(true, data, null);
			Assert.That(state.Diagnostics, Is.Empty);

			var withWarnings = Snapshot("[{\"id\":1}]", "code", "id");
			withWarnings.Diagnostics = new[] { "Existing Data Class needs a row type name." };
			state.Apply(true, withWarnings, null);
			Assert.That(state.Diagnostics, Is.EqualTo(new[] { "Existing Data Class needs a row type name." }));
		}

		#endregion

		#region Structure column skip predicate

		[Test]
		public void configuration_is_skipped_with_collections_off()
		{
			var settings = Settings();
			try
			{
				// The regression this test exists for: with collections off, IsAutomaticConfiguration is
				// false, yet ExportOrdinaryJson still routes an exact 'Configuration' sheet to the typed
				// config exporter because ConfigurationRouteEnabled is !Detached. A preview would show a
				// legacy row array for a sheet export writes as a typed config class.
				settings.enableCollections = false;

				Assert.That(
					SheetXHelper.ShouldOfferStructurePreview(settings, Sheet(SheetXConstants.CONFIGURATION_SHEET)),
					Is.False,
					"Configuration must be skipped by exact name, not via IsAutomaticConfiguration.");
			}
			finally
			{
				Cleanup(settings);
			}
		}

		[Test]
		public void configuration_is_skipped_with_collections_on()
		{
			var settings = Settings();
			try
			{
				settings.enableCollections = true;

				Assert.That(
					SheetXHelper.ShouldOfferStructurePreview(settings, Sheet(SheetXConstants.CONFIGURATION_SHEET)),
					Is.False);
			}
			finally
			{
				Cleanup(settings);
			}
		}

		[Test]
		public void a_sheet_merely_starting_with_configuration_is_not_skipped()
		{
			var settings = Settings();
			try
			{
				Assert.That(SheetXHelper.ShouldOfferStructurePreview(settings, Sheet("ConfigurationExtra")),
					Is.True, "The skip is an exact-name match, so a longer name is an ordinary sheet.");
			}
			finally
			{
				Cleanup(settings);
			}
		}

		[TestCase("HeroIDs")]
		[TestCase("IDs")]
		[TestCase("Constants")]
		[TestCase("Settings")]
		[TestCase("LocalizationEN")]
		public void non_ordinary_sheets_are_skipped(string sheetName)
		{
			var settings = Settings();
			try
			{
				Assert.That(SheetXHelper.ShouldOfferStructurePreview(settings, Sheet(sheetName)), Is.False);
			}
			finally
			{
				Cleanup(settings);
			}
		}

		[Test]
		public void an_ordinary_sheet_offers_a_structure_preview_regardless_of_collections()
		{
			var settings = Settings();
			try
			{
				settings.enableCollections = false;
				Assert.That(SheetXHelper.ShouldOfferStructurePreview(settings, Sheet("Heroes")), Is.True);

				settings.enableCollections = true;
				Assert.That(SheetXHelper.ShouldOfferStructurePreview(settings, Sheet("Heroes")), Is.True);
			}
			finally
			{
				Cleanup(settings);
			}
		}

		[Test]
		public void a_missing_settings_or_sheet_offers_nothing()
		{
			var settings = Settings();
			try
			{
				Assert.That(SheetXHelper.ShouldOfferStructurePreview(null, Sheet("Heroes")), Is.False);
				Assert.That(SheetXHelper.ShouldOfferStructurePreview(settings, null), Is.False);
				Assert.That(SheetXHelper.ShouldOfferStructurePreview(settings, Sheet("")), Is.False);
				Assert.That(SheetXHelper.ShouldOfferStructurePreview(settings, Sheet(null)), Is.False);
			}
			finally
			{
				Cleanup(settings);
			}
		}

		#endregion

		#region JSON view

		[Test]
		public void a_snapshot_with_json_offers_a_pretty_printed_json_view()
		{
			var state = new SheetXStructurePreviewState();

			state.Apply(true, Snapshot("[{\"id\":1,\"name\":\"Alpha\"}]", "code", "id", "name"), null);

			Assert.That(state.HasJson, Is.True);
			Assert.That(state.JsonTruncated, Is.False);
			Assert.That(state.PrettyJson, Does.Contain("\r\n"), "The JSON view exists to be read, so it is formatted.");
			Assert.That(state.PrettyJson, Does.Contain("\"id\": 1"));
		}

		[Test]
		public void generated_mode_has_no_json_view()
		{
			var state = new SheetXStructurePreviewState();
			var data = Snapshot(null, "public class HeroesSX { }");
			data.Mode = SheetXSheetOutputMode.GeneratedDataClass;

			state.Apply(true, data, null);

			Assert.That(state.HasJson, Is.False, "Generated mode has no legacy JSON, so it must not offer an empty tab.");
			Assert.That(state.PrettyJson, Is.Empty);
			Assert.That(state.JsonTruncated, Is.False);
		}

		[Test]
		public void date_like_strings_survive_pretty_printing_as_strings()
		{
			var state = new SheetXStructurePreviewState();

			state.Apply(true, Snapshot("[{\"created_at\":\"2026-09-16T12:00:00Z\"}]", "code", "created_at"), null);

			Assert.That(state.PrettyJson, Does.Contain("\"2026-09-16T12:00:00Z\""),
				"Newtonsoft reformats dates unless date parsing is off; the view must show what was exported.");
		}

		[Test]
		public void oversized_json_is_truncated_and_says_so_without_touching_the_stored_json()
		{
			var state = new SheetXStructurePreviewState();
			string json = "[" + string.Join(",",
				Enumerable.Range(0, 4000).Select(i => "{\"id\":" + i + ",\"name\":\"row_" + i + "\"}")) + "]";
			Assert.That(json.Length, Is.GreaterThan(50000), "Fixture check: the raw input must exceed the cap.");
			var data = Snapshot(json, "code", "id", "name");

			state.Apply(true, data, null);

			Assert.That(state.JsonTruncated, Is.True);
			Assert.That(state.PrettyJson.Length, Is.LessThanOrEqualTo(50000),
				"An unbounded document would lock the window.");
			Assert.That(state.JsonNote, Does.Contain("excerpt"),
				"A silent prefix would read as the whole document.");
			Assert.That(state.Snapshot.Json, Is.EqualTo(json),
				"Validation deserializes the stored Json, so the view must never shorten it.");
		}

		[Test]
		public void small_json_keeps_its_whole_document_and_carries_no_truncation_note()
		{
			var state = new SheetXStructurePreviewState();

			state.Apply(true, Snapshot("[{\"id\":1}]", "code", "id"), null);

			Assert.That(state.JsonTruncated, Is.False);
			Assert.That(state.JsonNote, Is.Null);
		}

		[Test]
		public void malformed_json_is_shown_raw_rather_than_losing_the_view()
		{
			var state = new SheetXStructurePreviewState();

			state.Apply(true, Snapshot("[{\"id\":", "code"), null);

			Assert.That(state.HasJson, Is.True, "Unparseable JSON is exactly what a reader needs to see.");
			Assert.That(state.PrettyJson, Does.Contain("\"id\""));
		}

		[Test]
		public void an_invalid_selected_tab_resets_when_a_snapshot_has_no_json()
		{
			var state = new SheetXStructurePreviewState();
			state.Apply(true, Snapshot("[{\"id\":1}]", "code", "id"), null);
			state.SelectedTab = 1;
			Assert.That(state.SelectedTab, Is.EqualTo(1), "Fixture check: the JSON tab must be selectable first.");

			var generated = Snapshot(null, "code");
			generated.Mode = SheetXSheetOutputMode.GeneratedDataClass;
			state.Apply(true, generated, null);

			Assert.That(state.SelectedTab, Is.EqualTo(0),
				"A JSON tab left selected in a mode without JSON would paint an empty view.");
		}

		[Test]
		public void the_json_tab_cannot_be_selected_without_json()
		{
			var state = new SheetXStructurePreviewState();
			var generated = Snapshot(null, "code");
			generated.Mode = SheetXSheetOutputMode.GeneratedDataClass;
			state.Apply(true, generated, null);

			state.SelectedTab = 1;

			Assert.That(state.SelectedTab, Is.EqualTo(0));
		}

		[Test]
		public void reset_clears_the_json_view_and_the_selected_tab()
		{
			var state = new SheetXStructurePreviewState();
			state.Apply(true, Snapshot("[{\"id\":1}]", "code", "id"), null);
			state.SelectedTab = 1;

			state.Reset();

			Assert.That(state.HasJson, Is.False);
			Assert.That(state.PrettyJson, Is.Empty);
			Assert.That(state.SelectedTab, Is.EqualTo(0));
		}

		[Test]
		public void a_failed_refresh_keeps_the_json_view_of_the_snapshot_it_kept()
		{
			var state = new SheetXStructurePreviewState();
			state.Apply(true, Snapshot("[{\"id\":1}]", "code", "id"), null);

			state.Apply(false, null, "network is down");

			Assert.That(state.HasJson, Is.True, "The stale snapshot is still shown, so its JSON is still shown.");
			Assert.That(state.PrettyJson, Does.Contain("\"id\""));
		}

		#endregion

		#region Validation guidance

		[Test]
		public void unmatched_member_guidance_names_the_consequence_and_both_fixes()
		{
			string text = SheetXStructurePreviewState.UnmatchedMemberGuidance("rank", null);

			Assert.That(text, Does.Contain("rank"));
			Assert.That(text, Does.Contain("discarded"), "The user must learn the value is lost, not just that it differs.");
			Assert.That(text, Does.Contain("Add").Or.Contain("add"));
			Assert.That(text, Does.Contain("rename").Or.Contain("Rename"));
		}

		[Test]
		public void unmatched_member_guidance_offers_the_draft_declaration_as_a_suggestion()
		{
			string text = SheetXStructurePreviewState.UnmatchedMemberGuidance("rank", "public int rank;");

			Assert.That(text, Does.Contain("public int rank;"));
			Assert.That(text, Does.Contain("suggestion").Or.Contain("Suggested"),
				"The draft declaration is inferred, so it must not be presented as a guaranteed fix.");
		}

		[Test]
		public void unobserved_member_guidance_stays_informational_and_claims_no_default_value()
		{
			string text = SheetXStructurePreviewState.UnobservedMemberGuidance(new[] { "icon", "tier" });

			Assert.That(text, Does.Contain("icon"));
			Assert.That(text, Does.Contain("tier"));
			Assert.That(text, Does.Contain("blank").Or.Contain("omitted"),
				"Absence from the export is not proof the class member is wrong.");
			Assert.That(text, Does.Not.Contain("will be zero").And.Not.Contain("will be null"),
				"A constructor default may exist, so no value may be asserted.");
		}

		[Test]
		public void unverifiable_guidance_keeps_its_reason_and_asks_for_inspection_not_repair()
		{
			string text = SheetXStructurePreviewState.UnverifiableGuidance(
				"Member coverage cannot be verified: contract has a custom converter.");

			Assert.That(text, Does.Contain("custom converter"), "The specific reason must survive.");
			Assert.That(text, Does.Contain("manual").Or.Contain("inspect"));
			Assert.That(text, Does.Not.Contain("is wrong"),
				"An unverifiable contract is not evidence the data is wrong.");
		}

		[Test]
		public void conversion_failure_guidance_keeps_the_message_and_points_at_the_value()
		{
			string text = SheetXStructurePreviewState.ConversionFailureGuidance("Could not convert string to int: abc.");

			Assert.That(text, Does.Contain("Could not convert string to int: abc."));
			Assert.That(text, Does.Contain("value").Or.Contain("type"));
		}

		[Test]
		public void a_null_or_empty_unverifiable_reason_still_produces_guidance()
		{
			Assert.That(SheetXStructurePreviewState.UnverifiableGuidance(null), Is.Not.Null.And.Not.Empty);
			Assert.That(SheetXStructurePreviewState.UnobservedMemberGuidance(null), Is.Not.Null.And.Not.Empty);
		}

		#endregion

		#region Row type resolution and eligibility messages

		[Test]
		public void a_bound_type_that_no_longer_resolves_names_it_instead_of_claiming_none_is_selected()
		{
			var state = new SheetXStructurePreviewState();
			state.Apply(true, Snapshot("[{\"id\":1}]", "code", "id"), null);

			// The binding still names a class; it just cannot be found. Saying "no row type is selected"
			// here contradicts the header, which shows 'Missing: MyOldRow' three lines above.
			state.Revalidate(null, "MyOldRow");

			Assert.That(state.RowTypeError, Is.Not.Null.And.Not.Empty);
			Assert.That(state.RowTypeError, Does.Contain("MyOldRow"),
				"The message must name the bound type the header is reporting as missing.");
			Assert.That(state.RowTypeError, Does.Contain("re-pick"),
				"The actionable fix is to re-pick the type in the Data Class column.");
			Assert.That(state.RowTypeError, Does.Not.Contain("no row type is selected"),
				"A type IS selected; it cannot be resolved.");
		}

		[Test]
		public void an_unresolvable_bound_type_says_export_skips_the_sheet()
		{
			var state = new SheetXStructurePreviewState();
			state.Apply(true, Snapshot("[{\"id\":1}]", "code", "id"), null);

			state.Revalidate(null, "MyOldRow");

			Assert.That(state.RowTypeError, Does.Contain("Export skips this sheet"),
				"Export resolves the same type and skips the sheet, so this is blocking, not advisory.");
		}

		[Test]
		public void no_bound_type_at_all_still_reports_that_none_is_selected()
		{
			var state = new SheetXStructurePreviewState();
			state.Apply(true, Snapshot("[{\"id\":1}]", "code", "id"), null);

			state.Revalidate(null, null);

			Assert.That(state.RowTypeError, Is.Null,
				"Nothing is broken here — the user simply has not picked a class yet.");
			Assert.That(state.Match, Is.Null);
		}

		[Test]
		public void an_ineligible_type_renders_a_blocking_error_with_the_real_validate_reason()
		{
			var state = new SheetXStructurePreviewState();
			state.Apply(true, Snapshot("[{\"id\":1,\"name\":\"Alpha\"}]", "code", "id", "name"), null);

			state.Revalidate(typeof(UnmarkedStructureWindowRow), "UnmarkedStructureWindowRow");

			Assert.That(state.RowTypeError, Is.Not.Null.And.Not.Empty);
			Assert.That(state.RowTypeError, Does.Contain("SheetXBindable"),
				"Validate's own actionable reason must survive.");
			Assert.That(state.RowTypeError, Does.Contain("Export skips this sheet"),
				"Export refuses this type, so the window must not imply the sheet still exports.");
			Assert.That(state.RowTypeError, Does.Not.Contain("Nothing is necessarily wrong"),
				"Something IS wrong: the next export will not update this sheet's JSON.");
			Assert.That(state.RowTypeError, Does.Not.Contain("cannot model that contract"),
				"The contract is modellable; the type is ineligible.");
		}

		[Test]
		public void picking_a_valid_type_clears_a_previous_row_type_error()
		{
			var state = new SheetXStructurePreviewState();
			state.Apply(true, Snapshot("[{\"id\":1,\"name\":\"Alpha\"}]", "code", "id", "name"), null);
			state.Revalidate(typeof(UnmarkedStructureWindowRow), "UnmarkedStructureWindowRow");
			Assert.That(state.RowTypeError, Is.Not.Null, "Fixture check: there must be an error to clear.");

			state.Revalidate(typeof(StructureWindowRow), "StructureWindowRow");

			Assert.That(state.RowTypeError, Is.Null, "A stale blocking error would contradict the clean result.");
			Assert.That(state.Match.State, Is.EqualTo(SheetXRowTypeMatchState.Compared));
			Assert.That(state.Match.HasUnmatched, Is.False);
		}

		[Test]
		public void an_ordinary_unverifiable_contract_keeps_its_reassuring_wording()
		{
			// A genuine unmodellable contract is NOT blocking: export may still handle it fine. This is the
			// case UnverifiableGuidance was written for, and its wording must not be collateral damage.
			string text = SheetXStructurePreviewState.UnverifiableGuidance(
				"Member coverage cannot be verified: contract has a custom converter.");

			Assert.That(text, Does.Contain("custom converter"));
			Assert.That(text, Does.Contain("Nothing is necessarily wrong"));
			Assert.That(text, Does.Not.Contain("Export skips this sheet"));
		}

		[Test]
		public void a_new_snapshot_clears_a_row_type_error()
		{
			var state = new SheetXStructurePreviewState();
			state.Apply(true, Snapshot("[{\"id\":1}]", "code", "id"), null);
			state.Revalidate(null, "MyOldRow");

			state.Apply(true, Snapshot("[{\"id\":2}]", "code", "id"), null);

			Assert.That(state.RowTypeError, Is.Null,
				"An error belonging to the previous snapshot must not survive into a new one.");
		}

		#endregion

		#region Fetched timestamp

		[Test]
		public void the_fetched_timestamp_shows_the_date_and_local_time_of_the_snapshot()
		{
			var state = new SheetXStructurePreviewState();
			var data = Snapshot("[{\"id\":1}]", "code", "id");
			// Fixed, never DateTime.UtcNow: a wall-clock assertion would be flaky by construction.
			data.FetchedAtUtc = new DateTime(2026, 9, 16, 22, 30, 15, DateTimeKind.Utc);

			state.Apply(true, data, null);

			string expected = data.FetchedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
			Assert.That(state.FetchedLabel, Is.EqualTo(expected));
			Assert.That(state.FetchedLabel, Does.Contain("2026-"),
				"A snapshot can outlive the day it was taken, so the date must be shown, not only the clock.");
		}

		[Test]
		public void there_is_no_fetched_timestamp_before_the_first_load()
		{
			var state = new SheetXStructurePreviewState();

			Assert.That(state.FetchedLabel, Is.EqualTo("—"), "Nothing was fetched, so no time may be implied.");
		}

		[Test]
		public void a_failed_refresh_keeps_the_timestamp_of_the_snapshot_it_kept()
		{
			var state = new SheetXStructurePreviewState();
			var data = Snapshot("[{\"id\":1}]", "code", "id");
			data.FetchedAtUtc = new DateTime(2026, 9, 16, 8, 0, 0, DateTimeKind.Utc);
			state.Apply(true, data, null);
			string before = state.FetchedLabel;

			state.Apply(false, null, "network is down");

			Assert.That(state.Stale, Is.True);
			Assert.That(state.FetchedLabel, Is.EqualTo(before),
				"The kept snapshot is still the one on screen, so its age must not appear to advance.");
		}

		[Test]
		public void a_successful_refresh_updates_the_timestamp()
		{
			var state = new SheetXStructurePreviewState();
			var first = Snapshot("[{\"id\":1}]", "code", "id");
			first.FetchedAtUtc = new DateTime(2026, 9, 16, 8, 0, 0, DateTimeKind.Utc);
			state.Apply(true, first, null);

			var second = Snapshot("[{\"id\":2}]", "code", "id");
			second.FetchedAtUtc = new DateTime(2026, 9, 17, 9, 45, 0, DateTimeKind.Utc);
			state.Apply(true, second, null);

			Assert.That(state.FetchedLabel,
				Is.EqualTo(second.FetchedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")));
		}

		#endregion

		#region Field table

		[Test]
		public void the_field_table_reads_the_metadata_the_snapshot_carries()
		{
			var state = new SheetXStructurePreviewState();
			var data = Snapshot("[{\"id\":1}]", "code", "id");
			Assert.That(SheetXSheetStructure.TryFromJson("[{\"id\":1,\"a-b\":3}]", "RowSX", out var draft, out _), Is.True);
			data.Fields = draft.Fields;

			state.Apply(true, data, null);

			Assert.That(state.Fields.Select(f => f.JsonName), Is.EqualTo(new[] { "id", "a-b" }));
			Assert.That(state.Fields.Select(f => f.FieldName), Is.EqualTo(new[] { "id", "AB" }));
		}

		[Test]
		public void a_snapshot_without_field_metadata_has_an_empty_table_rather_than_a_throw()
		{
			var state = new SheetXStructurePreviewState();
			var data = Snapshot("[{\"id\":1}]", "code", "id");
			data.Fields = null;

			state.Apply(true, data, null);

			Assert.That(state.Fields, Is.Empty);
		}

		[Test]
		public void the_declaration_hint_for_an_unmatched_member_comes_from_the_snapshot_metadata()
		{
			var state = new SheetXStructurePreviewState();
			var data = Snapshot("[{\"id\":1,\"rank\":3}]", "code", "id", "rank");
			Assert.That(SheetXSheetStructure.TryFromJson("[{\"id\":1,\"rank\":3}]", "RowSX", out var draft, out _), Is.True);
			data.Fields = draft.Fields;
			state.Apply(true, data, null);

			Assert.That(state.DeclarationFor("rank"), Is.EqualTo("public int rank;"));
			Assert.That(state.DeclarationFor("nope"), Is.Null, "A member with no metadata gets no invented hint.");
		}

		#endregion

		private static SheetPath Sheet(string name) => new SheetPath { name = name, selected = true };

		private static SheetXSettings Settings()
		{
			var settings = ScriptableObject.CreateInstance<SheetXSettings>();
			settings.ResetToDefault();
			settings.silent = true;
			return settings;
		}

		private static void Cleanup(SheetXSettings settings) => ScriptableObject.DestroyImmediate(settings);

		private static SheetXSheetPreviewData Snapshot(string json, string classCode, params string[] rootMembers)
		{
			return new SheetXSheetPreviewData
			{
				Mode = SheetXSheetOutputMode.ExistingDataClass,
				Json = json,
				ClassCode = classCode,
				RootMemberNames = rootMembers ?? Array.Empty<string>(),
				Diagnostics = Array.Empty<string>(),
				FetchedAtUtc = DateTime.UtcNow,
			};
		}
	}
}
