using NUnit.Framework;
using RCore.SheetX.Editor;

namespace RCore.SheetX.Tests
{
	public class SheetXBatchStateTests
	{
		[Test]
		public void try_add_id_records_the_first_origin()
		{
			var state = new SheetXBatchState();

			bool added = state.TryAddId("HERO_1", 1, "a.xlsx", "HeroIDs", out string error);

			Assert.That(added, Is.True);
			Assert.That(error, Is.Null);
			Assert.That(state.AllIds["HERO_1"], Is.EqualTo(1));
			Assert.That(state.IdOrigins["HERO_1"].Source, Is.EqualTo("a.xlsx"));
			Assert.That(state.IdOrigins["HERO_1"].Sheet, Is.EqualTo("HeroIDs"));
			Assert.That(state.IdOrigins["HERO_1"].Value, Is.EqualTo(1));
		}

		[Test]
		public void strict_duplicate_with_a_different_value_reports_both_origins()
		{
			var state = new SheetXBatchState();
			state.TryAddId("HERO_1", 1, "a.xlsx", "HeroIDs", out _);

			bool added = state.TryAddId("HERO_1", 2, "b.xlsx", "HeroIDs", out string error);

			Assert.That(added, Is.False);
			Assert.That(
				error,
				Is.EqualTo(
					"Duplicate ID 'HERO_1': first 'a.xlsx' sheet 'HeroIDs' value '1'; "
					+ "second 'b.xlsx' sheet 'HeroIDs' value '2'."));
			Assert.That(state.AllIds["HERO_1"], Is.EqualTo(1));
		}

		[Test]
		public void strict_duplicate_with_the_same_value_is_still_an_error()
		{
			var state = new SheetXBatchState();
			state.TryAddId("HERO_1", 1, "a.xlsx", "HeroIDs", out _);

			bool added = state.TryAddId("HERO_1", 1, "b.xlsx", "OtherIDs", out string error);

			Assert.That(added, Is.False);
			Assert.That(
				error,
				Is.EqualTo(
					"Duplicate ID 'HERO_1': first 'a.xlsx' sheet 'HeroIDs' value '1'; "
					+ "second 'b.xlsx' sheet 'OtherIDs' value '1'."));
		}

		[Test]
		public void non_strict_duplicate_is_silent_and_keeps_the_first_value()
		{
			var state = new SheetXBatchState { StrictDuplicateIds = false };
			state.TryAddId("HERO_1", 1, "a.xlsx", "HeroIDs", out _);

			bool added = state.TryAddId("HERO_1", 2, "a.xlsx", "HeroIDs", out string error);

			Assert.That(added, Is.False);
			Assert.That(error, Is.Null);
			Assert.That(state.AllIds["HERO_1"], Is.EqualTo(1));
		}

		[Test]
		public void ids_are_ordinal_so_case_differs()
		{
			var state = new SheetXBatchState();

			Assert.That(state.TryAddId("hero", 1, "a.xlsx", "S", out _), Is.True);
			Assert.That(state.TryAddId("HERO", 2, "a.xlsx", "S", out _), Is.True);
			Assert.That(state.AllIds.Count, Is.EqualTo(2));
		}

		[Test]
		public void declared_ids_gate_emission_independently_of_all_ids()
		{
			var state = new SheetXBatchState();
			state.TryAddId("HERO_1", 1, "a.xlsx", "HeroIDs", out _);

			Assert.That(state.DeclaredIds.Add("HERO_1"), Is.True);
			Assert.That(state.DeclaredIds.Add("HERO_1"), Is.False);
		}

		[Test]
		public void combined_jsons_keep_same_named_sheets_isolated_by_source_index()
		{
			var state = new SheetXBatchState();
			state.CombinedJsons.Add(
				0,
				new System.Collections.Generic.Dictionary<string, string>(
					System.StringComparer.Ordinal)
				{
					["Data"] = "{\"source\":0}",
				});
			state.CombinedJsons.Add(
				1,
				new System.Collections.Generic.Dictionary<string, string>(
					System.StringComparer.Ordinal)
				{
					["Data"] = "{\"source\":1}",
				});

			Assert.That(state.CombinedJsons, Has.Count.EqualTo(2));
			Assert.That(state.CombinedJsons[0]["Data"], Is.EqualTo("{\"source\":0}"));
			Assert.That(state.CombinedJsons[1]["Data"], Is.EqualTo("{\"source\":1}"));
			Assert.That(state.CombinedJsons[0], Is.Not.SameAs(state.CombinedJsons[1]));
		}

		[Test]
		public void all_ids_stays_the_same_instance_across_adds()
		{
			var state = new SheetXBatchState();
			var reference = state.AllIds;

			state.TryAddId("HERO_1", 1, "a.xlsx", "HeroIDs", out _);

			Assert.That(state.AllIds, Is.SameAs(reference));
		}

		[Test]
		public void sheet_keys_from_different_sources_do_not_collide()
		{
			var a = new SheetXBatchSheetKey(0, "Data");
			var b = new SheetXBatchSheetKey(1, "Data");

			Assert.That(a, Is.Not.EqualTo(b));
			Assert.That(a, Is.EqualTo(new SheetXBatchSheetKey(0, "Data")));
			Assert.That(a, Is.Not.EqualTo(new SheetXBatchSheetKey(0, "data")));
		}
	}
}
