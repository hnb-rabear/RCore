/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using RCore.SheetX.Editor;

namespace RCore.SheetX.Tests
{
	public class SheetXHelperTests
	{
		[Test]
		public void sort_ids_by_length_replaces_long_key_first()
		{
			var ids = SheetXHelper.SortIDsByLength(new Dictionary<string, int>
			{
				["HERO_1"] = 5,
				["HERO_10"] = 9,
			});

			string value = "HERO_10";
			foreach (var id in ids)
				value = value.Replace(id.Key, SheetXHelper.FormatInt(id.Value));

			Assert.That(value, Is.EqualTo("9"));
		}

		[Test]
		public void sort_ids_by_length_orders_equal_keys_ordinally()
		{
			var ids = SheetXHelper.SortIDsByLength(new Dictionary<string, int>
			{
				["B"] = 1,
				["A"] = 2,
			});

			CollectionAssert.AreEqual(new[] { "A", "B" }, ids.Keys.ToArray());
		}

		[Test]
		public void close_combined_column_emits_empty_array_when_no_value_collected()
		{
			string closed = SheetXHelper.CloseCombinedColumn("\"drops\":[");

			Assert.That(closed, Is.EqualTo("\"drops\":[]"));
			Assert.That(SheetXHelper.IsValidJson("{" + closed + "}"), Is.True,
				"A duplicate-name column group with no rows must still produce a parseable member.");
		}

		[Test]
		public void close_combined_column_drops_only_the_trailing_separator()
		{
			Assert.That(SheetXHelper.CloseCombinedColumn("\"drops\":[1,2,"), Is.EqualTo("\"drops\":[1,2]"));
		}

		[Test]
		public void merge_json_contents_does_not_depend_on_sheet_order()
		{
			string forward = SheetXHelper.MergeJsonContents(new Dictionary<string, string>
			{
				["Heroes"] = "[]",
				["Items"] = "[]",
			});
			string reversed = SheetXHelper.MergeJsonContents(new Dictionary<string, string>
			{
				["Items"] = "[]",
				["Heroes"] = "[]",
			});

			Assert.That(reversed, Is.EqualTo(forward));
			StringAssert.StartsWith("{\"Heroes\"", forward);
		}

		[Test]
		public void try_parse_decimal_rejects_comma_decimal_in_de_de()
		{
			var previous = CultureInfo.CurrentCulture;
			try
			{
				CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
				// "1,5" typed as number would be classified as a Number column and then emitted verbatim,
				// producing "value":1,5 — invalid JSON.
				Assert.That(SheetXHelper.TryParseDecimal("1,5", out decimal _), Is.False);
				Assert.That(SheetXHelper.TryParseDecimal("1.5", out decimal parsed), Is.True);
				Assert.That(parsed, Is.EqualTo(1.5m));
			}
			finally
			{
				CultureInfo.CurrentCulture = previous;
			}
		}

		[Test]
		public void format_float_uses_dot_in_de_de()
		{
			var previous = CultureInfo.CurrentCulture;
			try
			{
				CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
				Assert.That(SheetXHelper.FormatFloat(1.5f), Is.EqualTo("1.5"));
				Assert.That(SheetXHelper.FormatFloatLiteral(" 1.5 "), Is.EqualTo("1.5f"));
				Assert.That(SheetXHelper.TryParseFloat("1.5", out float parsed), Is.True);
				Assert.That(parsed, Is.EqualTo(1.5f));
			}
			finally
			{
				CultureInfo.CurrentCulture = previous;
			}
		}
	}
}
