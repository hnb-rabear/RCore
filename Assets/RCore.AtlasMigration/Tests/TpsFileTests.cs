using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using RCore.Editor.Tool;

namespace RCore.Editor.Tests
{
	public class TpsFileTests
	{
		internal static readonly string Fixture = string.Join("\r\n",
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
			"<data version=\"1.0\">",
			"    <struct type=\"Settings\">",
			"        <key>fileFormatVersion</key>",
			"        <int>5</int>",
			"        <key>dataFormat</key>",
			"        <string>unity-texture2d</string>",
			"        <key>textureFileName</key>",
			"        <filename></filename>",
			"        <key>globalSpriteSettings</key>",
			"        <struct type=\"SpriteSettings\">",
			"            <key>trimMode</key>",
			"            <enum type=\"SpriteSettings::TrimMode\">Polygon</enum>",
			"            <key>defaultPivotPoint</key>",
			"            <point_f>0.5,0.5</point_f>",
			"            <key>writePivotPoints</key>",
			"            <false/>",
			"        </struct>",
			"        <key>individualSpriteSettings</key>",
			"        <map type=\"IndividualSpriteSettingsMap\">",
			"            <key type=\"filename\">cars/car_a.png</key>",
			"            <struct type=\"IndividualSpriteSettings\">",
			"                <key>pivotPoint</key>",
			"                <point_f>0.5,0.5</point_f>",
			"                <key>spriteScale</key>",
			"                <double>1</double>",
			"                <key>scale9Enabled</key>",
			"                <false/>",
			"                <key>scale9Borders</key>",
			"                <rect>25,15,49,29</rect>",
			"                <key>scale9Paddings</key>",
			"                <rect>25,15,49,29</rect>",
			"                <key>scale9FromFile</key>",
			"                <false/>",
			"            </struct>",
			"            <key type=\"filename\">cars/shadow_a.png</key>",
			"            <key type=\"filename\">trucks/shadow_b.png</key>",
			"            <key type=\"filename\">trucks/shadow_c.png</key>",
			"            <struct type=\"IndividualSpriteSettings\">",
			"                <key>pivotPoint</key>",
			"                <point_f>0.5,0.5</point_f>",
			"                <key>spriteScale</key>",
			"                <double>1</double>",
			"                <key>scale9Enabled</key>",
			"                <false/>",
			"                <key>scale9Borders</key>",
			"                <rect>19,9,38,19</rect>",
			"                <key>scale9Paddings</key>",
			"                <rect>19,9,38,19</rect>",
			"                <key>scale9FromFile</key>",
			"                <false/>",
			"            </struct>",
			"        </map>",
			"        <key>fileList</key>",
			"        <array>",
			"            <filename>cars</filename>",
			"            <filename>trucks</filename>",
			"        </array>",
			"        <key>ignoreFileList</key>",
			"        <array/>",
			"    </struct>",
			"</data>",
			"");

		private static readonly string EmptyMapFixture = string.Join("\r\n",
			"<?xml version=\"1.0\" encoding=\"UTF-8\"?>",
			"<data version=\"1.0\">",
			"    <struct type=\"Settings\">",
			"        <key>globalSpriteSettings</key>",
			"        <struct type=\"SpriteSettings\">",
			"            <key>writePivotPoints</key>",
			"            <true/>",
			"        </struct>",
			"        <key>individualSpriteSettings</key>",
			"        <map type=\"IndividualSpriteSettingsMap\"/>",
			"        <key>fileList</key>",
			"        <array>",
			"            <filename>../Loose/ui</filename>",
			"        </array>",
			"    </struct>",
			"</data>",
			"");

		private static readonly string[] AddedEntry =
		{
			"            <key type=\"filename\">../Loose/ui/a.png</key>",
			"            <struct type=\"IndividualSpriteSettings\">",
			"                <key>pivotPoint</key>",
			"                <point_f>0.5,0.5</point_f>",
			"                <key>spriteScale</key>",
			"                <double>1</double>",
			"                <key>scale9Enabled</key>",
			"                <false/>",
			"                <key>scale9Borders</key>",
			"                <rect>16,8,32,16</rect>",
			"                <key>scale9Paddings</key>",
			"                <rect>16,8,32,16</rect>",
			"                <key>scale9FromFile</key>",
			"                <false/>",
			"            </struct>",
		};

		[Test]
		public void packs_a_file_under_a_folder_entry()
		{
			string root = Path.Combine(Path.GetTempPath(), "rcore-tps-packs");

			Assert.IsTrue(TpsFile.Parse(EmptyMapFixture).Packs(Path.Combine(root, "Atlas"), Path.Combine(root, "Loose", "ui", "a.png")));
		}

		[Test]
		public void packs_a_file_in_a_subfolder_of_a_folder_entry()
		{
			string root = Path.Combine(Path.GetTempPath(), "rcore-tps-packs");

			Assert.IsTrue(TpsFile.Parse(EmptyMapFixture).Packs(Path.Combine(root, "Atlas"), Path.Combine(root, "Loose", "ui", "sub", "a.png")));
		}

		[Test]
		public void does_not_pack_a_file_outside_the_file_list()
		{
			string root = Path.Combine(Path.GetTempPath(), "rcore-tps-packs");
			string tpsDir = Path.Combine(root, "Atlas");
			var tps = TpsFile.Parse(EmptyMapFixture);

			Assert.IsFalse(tps.Packs(tpsDir, Path.Combine(root, "Other", "a.png")));
			Assert.IsFalse(tps.Packs(tpsDir, Path.Combine(root, "Loose", "uix", "a.png")));
		}

		[Test]
		public void packs_everything_when_there_is_no_file_list()
		{
			string noList = Fixture.Replace(string.Join("\r\n", "        <key>fileList</key>", "        <array>", "            <filename>cars</filename>", "            <filename>trucks</filename>", "        </array>", ""), "");
			Assert.AreNotEqual(Fixture, noList);
			string root = Path.Combine(Path.GetTempPath(), "rcore-tps-packs");

			Assert.IsTrue(TpsFile.Parse(noList).Packs(Path.Combine(root, "Atlas"), Path.Combine(root, "Other", "a.png")));
		}

		[Test]
		public void find_key_resolves_key_relative_to_tps_folder()
		{
			string root = Path.Combine(Path.GetTempPath(), "rcore-tps-key");
			string tpsDir = Path.Combine(root, "Atlas");
			string asset = Path.Combine(root, "Loose", "ui", "a.png");

			Assert.AreEqual("../Loose/ui/a.png", TpsFile.FindKey(new[] { "../Loose/ui/a.png" }, asset, out var error, tpsDir));
			Assert.IsNull(error);
		}

		[Test]
		public void find_key_with_tps_folder_rejects_a_key_that_points_elsewhere()
		{
			string root = Path.Combine(Path.GetTempPath(), "rcore-tps-key");
			string asset = Path.Combine(root, "Loose", "ui", "a.png");

			Assert.IsNull(TpsFile.FindKey(new[] { "ui/a.png" }, asset, out var error, Path.Combine(root, "Atlas")));
			StringAssert.StartsWith("no key in .tps points to", error);
			StringAssert.Contains("ignored by TexturePacker: ui/a.png", error);
		}

		[Test]
		public void find_key_with_tps_folder_prefers_the_resolving_key()
		{
			string root = Path.Combine(Path.GetTempPath(), "rcore-tps-key");
			string asset = Path.Combine(root, "Loose", "ui", "a.png");

			Assert.AreEqual("../Loose/ui/a.png", TpsFile.FindKey(new[] { "ui/a.png", "../Loose/ui/a.png" }, asset, out var error, Path.Combine(root, "Atlas")));
			Assert.IsNull(error);
		}

		[Test]
		public void find_key_with_tps_folder_reports_two_resolving_keys()
		{
			string root = Path.Combine(Path.GetTempPath(), "rcore-tps-key");
			string asset = Path.Combine(root, "Loose", "a.png");

			Assert.IsNull(TpsFile.FindKey(new[] { "../Loose/a.png", "../loose/a.png" }, asset, out var error, Path.Combine(root, "Atlas")));
			StringAssert.StartsWith("ambiguous", error);
		}

		[Test]
		public void relative_key_walks_up_and_keeps_spaces()
		{
			string root = Path.Combine(Path.GetTempPath(), "rcore tps", "Game Resource");

			Assert.AreEqual("../Loose/a b.png", TpsFile.RelativeKey(Path.Combine(root, "Atlas"), Path.Combine(root, "Loose", "a b.png")));
			Assert.AreEqual("ui/a.png", TpsFile.RelativeKey(root, Path.Combine(root, "ui", "a.png")));
		}

		[Test]
		public void add_key_fills_an_empty_map()
		{
			var tps = TpsFile.Parse(EmptyMapFixture);

			Assert.IsTrue(tps.AddKey("../Loose/ui/a.png", 64, 32));

			var expected = new List<string>(EmptyMapFixture.Split(new[] { "\r\n" }, System.StringSplitOptions.None));
			expected[9] = "        <map type=\"IndividualSpriteSettingsMap\">";
			expected.InsertRange(10, AddedEntry);
			expected.Insert(10 + AddedEntry.Length, "        </map>");
			Assert.AreEqual(string.Join("\r\n", expected), tps.ToText());
			Assert.IsTrue(tps.Changed);
		}

		[Test]
		public void add_key_fills_a_map_with_only_whitespace()
		{
			var lines = new List<string>(EmptyMapFixture.Split(new[] { "\r\n" }, System.StringSplitOptions.None));
			lines[9] = "        <map type=\"IndividualSpriteSettingsMap\">";
			lines.Insert(10, "        </map>");
			var tps = TpsFile.Parse(string.Join("\r\n", lines));

			Assert.IsTrue(tps.AddKey("../Loose/ui/a.png", 64, 32));

			var expected = new List<string>(EmptyMapFixture.Split(new[] { "\r\n" }, System.StringSplitOptions.None));
			expected[9] = "        <map type=\"IndividualSpriteSettingsMap\">";
			expected.InsertRange(10, AddedEntry);
			expected.Insert(10 + AddedEntry.Length, "        </map>");
			Assert.AreEqual(string.Join("\r\n", expected), tps.ToText());
		}

		[Test]
		public void add_key_then_set_border_writes_the_new_entry()
		{
			var tps = TpsFile.Parse(EmptyMapFixture);
			tps.AddKey("../Loose/ui/a.png", 64, 32);

			Assert.IsTrue(tps.SetBorder("../Loose/ui/a.png", 4, 8, 12, 16, 64, 32));
			Assert.AreEqual("4,16,48,8", TpsFile.Parse(tps.ToText()).ValueOf("../Loose/ui/a.png", "scale9Borders"));
		}

		[Test]
		public void add_key_appends_after_the_last_struct_and_keeps_the_rest()
		{
			var tps = TpsFile.Parse(Fixture);

			Assert.IsTrue(tps.AddKey("new/b.png", 99, 59));

			var again = TpsFile.Parse(tps.ToText());
			Assert.AreEqual("new/b.png", again.Keys[again.Keys.Count - 1]);
			Assert.AreEqual("25,15,49,29", again.ValueOf("new/b.png", "scale9Borders")); // TexturePacker's own default for 99x59
			Assert.AreEqual("0.5,0.5", again.ValueOf("trucks/shadow_c.png", "pivotPoint"));
			var before = Fixture.Split(new[] { "\r\n" }, System.StringSplitOptions.None);
			var after = tps.ToText().Split(new[] { "\r\n" }, System.StringSplitOptions.None);
			Assert.AreEqual(before.Length + 15, after.Length);
			CollectionAssert.AreEqual(before.Take(52), after.Take(52));
			CollectionAssert.AreEqual(before.Skip(52), after.Skip(67));
		}

		[Test]
		public void add_key_then_set_pivot_writes_the_new_entry()
		{
			var tps = TpsFile.Parse(EmptyMapFixture);
			tps.AddKey("../Loose/ui/a.png", 64, 32);

			Assert.IsTrue(tps.SetPivot("../Loose/ui/a.png", 0.2f, 0.1f));
			Assert.AreEqual("0.2,0.9", TpsFile.Parse(tps.ToText()).ValueOf("../Loose/ui/a.png", "pivotPoint"));
		}

		[Test]
		public void add_key_refuses_an_existing_key()
		{
			var tps = TpsFile.Parse(Fixture);

			Assert.IsFalse(tps.AddKey("cars/car_a.png", 10, 10));
			Assert.IsFalse(tps.Changed);
		}

		[Test]
		public void default_scale9_rect_matches_texturepacker()
		{
			Assert.AreEqual("25,15,49,29", TpsFile.DefaultScale9Rect(99, 59));
			Assert.AreEqual("15,8,29,17", TpsFile.DefaultScale9Rect(58, 33));
			Assert.AreEqual("19,14,39,29", TpsFile.DefaultScale9Rect(77, 57));
			Assert.AreEqual("16,8,32,16", TpsFile.DefaultScale9Rect(64, 32));
		}

		[Test]
		public void round_trip_without_edits_returns_same_text()
		{
			var tps = TpsFile.Parse(Fixture);

			Assert.AreEqual(Fixture, tps.ToText());
			Assert.IsFalse(tps.Changed);
		}

		[Test]
		public void round_trip_keeps_lf_line_endings()
		{
			string lf = Fixture.Replace("\r\n", "\n");

			Assert.AreEqual(lf, TpsFile.Parse(lf).ToText());
		}

		private static string Line(string pText, int pIndex) => pText.Split(new[] { "\r\n" }, System.StringSplitOptions.None)[pIndex];

		[Test]
		public void keys_lists_every_sprite_key_in_order()
		{
			CollectionAssert.AreEqual(
				new[] { "cars/car_a.png", "cars/shadow_a.png", "trucks/shadow_b.png", "trucks/shadow_c.png" },
				TpsFile.Parse(Fixture).Keys);
		}

		[Test]
		public void find_key_matches_path_suffix()
		{
			var keys = TpsFile.Parse(Fixture).Keys;

			Assert.AreEqual("trucks/shadow_b.png", TpsFile.FindKey(keys, "Assets/Art/Loose/trucks/shadow_b.png", out var error));
			Assert.IsNull(error);
		}

		[Test]
		public void find_key_rejects_partial_folder_name()
		{
			// "bigcars/car_a.png" must not match key "cars/car_a.png"
			Assert.IsNull(TpsFile.FindKey(new[] { "cars/car_a.png" }, "Assets/bigcars/car_a.png", out var error));
			StringAssert.Contains("no key", error);
		}

		[Test]
		public void find_key_reports_two_matches_as_ambiguous()
		{
			Assert.IsNull(TpsFile.FindKey(new[] { "car_a.png", "cars/car_a.png" }, "Assets/cars/car_a.png", out var error));
			StringAssert.Contains("ambiguous", error);
		}

		[Test]
		public void find_key_by_name_matches_file_name()
		{
			var keys = TpsFile.Parse(Fixture).Keys;

			Assert.AreEqual("trucks/shadow_b.png", TpsFile.FindKeyByName(keys, "shadow_b", out var error));
			Assert.IsNull(error);
		}

		[Test]
		public void find_key_by_name_reports_ambiguous_and_missing()
		{
			Assert.IsNull(TpsFile.FindKeyByName(new[] { "a/car.png", "b/car.png" }, "car", out var ambiguous));
			StringAssert.Contains("ambiguous", ambiguous);
			Assert.IsNull(TpsFile.FindKeyByName(TpsFile.Parse(Fixture).Keys, "nope", out var missing));
			StringAssert.Contains("no key", missing);
		}

		[Test]
		public void set_pivot_flips_y_and_uses_invariant_culture()
		{
			var culture = Thread.CurrentThread.CurrentCulture;
			Thread.CurrentThread.CurrentCulture = new CultureInfo("vi-VN");
			try
			{
				var tps = TpsFile.Parse(Fixture);

				Assert.IsTrue(tps.SetPivot("cars/car_a.png", 0.5f, 0.75f));

				string text = tps.ToText();
				Assert.AreEqual("                <point_f>0.5,0.25</point_f>", Line(text, 23));
				Assert.IsTrue(tps.Changed);
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = culture;
			}
		}

		[Test]
		public void set_pivot_changes_only_that_line()
		{
			var tps = TpsFile.Parse(Fixture);
			tps.SetPivot("cars/car_a.png", 0.2f, 0.1f);

			var before = Fixture.Split(new[] { "\r\n" }, System.StringSplitOptions.None);
			var after = tps.ToText().Split(new[] { "\r\n" }, System.StringSplitOptions.None);
			Assert.AreEqual(before.Length, after.Length);
			var diff = Enumerable.Range(0, before.Length).Where(i => before[i] != after[i]).ToList();
			CollectionAssert.AreEqual(new[] { 23 }, diff);
			Assert.AreEqual("                <point_f>0.2,0.9</point_f>", after[23]);
		}

		[Test]
		public void same_pivot_does_not_mark_changed()
		{
			var tps = TpsFile.Parse(Fixture);

			tps.SetPivot("cars/car_a.png", 0.5f, 0.5f);

			Assert.IsFalse(tps.Changed);
		}

		[Test]
		public void scale9_rect_is_center_rect_from_top_left()
		{
			// Unity border (left 10, bottom 20, right 30, top 40) on 100x200
			Assert.AreEqual("10,40,60,140", TpsFile.ToScale9Rect(10, 20, 30, 40, 100, 200));
		}

		[Test]
		public void set_border_writes_borders_paddings_and_enables_scale9()
		{
			var tps = TpsFile.Parse(Fixture);

			Assert.IsTrue(tps.SetBorder("cars/car_a.png", 10, 20, 30, 40, 100, 200));

			string text = tps.ToText();
			Assert.AreEqual("                <true/>", Line(text, 27));
			Assert.AreEqual("                <rect>10,40,60,140</rect>", Line(text, 29));
			Assert.AreEqual("                <rect>10,40,60,140</rect>", Line(text, 31));
		}

		[Test]
		public void set_border_returns_false_when_a_setting_is_missing()
		{
			const string paddings = "                <key>scale9Paddings</key>\r\n                <rect>25,15,49,29</rect>\r\n";
			Assert.AreEqual(1, (Fixture.Length - Fixture.Replace(paddings, "").Length) / paddings.Length);
			string text = Fixture.Replace(paddings, "");
			var tps = TpsFile.Parse(text);

			Assert.IsFalse(tps.SetBorder("cars/car_a.png", 10, 20, 30, 40, 100, 200));

			Assert.IsFalse(tps.Changed);
			Assert.AreEqual(text, tps.ToText());
		}

		[Test]
		public void editing_a_shared_struct_splits_it_first()
		{
			var tps = TpsFile.Parse(Fixture);

			tps.SetPivot("trucks/shadow_b.png", 0f, 0f);

			// Reparse the saved text: in a .tps every key uses the next <struct>.
			var again = TpsFile.Parse(tps.ToText());
			CollectionAssert.AreEquivalent(TpsFile.Parse(Fixture).Keys, again.Keys);
			Assert.AreEqual("0,1", again.ValueOf("trucks/shadow_b.png", "pivotPoint"));
			Assert.AreEqual("0.5,0.5", again.ValueOf("cars/shadow_a.png", "pivotPoint"));
			Assert.AreEqual("0.5,0.5", again.ValueOf("trucks/shadow_c.png", "pivotPoint"));
			Assert.AreEqual("19,9,38,19", again.ValueOf("trucks/shadow_b.png", "scale9Borders"));
			StringAssert.DoesNotContain("\r\n\r\n", tps.ToText());
		}

		[Test]
		public void editing_the_last_key_of_a_shared_run_keeps_the_others()
		{
			var tps = TpsFile.Parse(Fixture);

			tps.SetPivot("trucks/shadow_c.png", 1f, 1f);

			var again = TpsFile.Parse(tps.ToText());
			Assert.AreEqual("1,0", again.ValueOf("trucks/shadow_c.png", "pivotPoint"));
			Assert.AreEqual("0.5,0.5", again.ValueOf("trucks/shadow_b.png", "pivotPoint"));
		}

		[Test]
		public void same_value_on_a_shared_struct_does_not_split_it()
		{
			var tps = TpsFile.Parse(Fixture);

			tps.SetPivot("trucks/shadow_b.png", 0.5f, 0.5f);

			Assert.IsFalse(tps.Changed);
			Assert.AreEqual(Fixture, tps.ToText());
		}

		[Test]
		public void unknown_key_returns_false()
		{
			var tps = TpsFile.Parse(Fixture);

			Assert.IsFalse(tps.SetPivot("nope.png", 0f, 0f));
			Assert.IsFalse(tps.Changed);
		}

		[Test]
		public void ensure_write_pivot_points_turns_false_to_true()
		{
			var tps = TpsFile.Parse(Fixture);

			Assert.IsTrue(tps.EnsureWritePivotPoints());
			Assert.AreEqual("            <true/>", Line(tps.ToText(), 16));
			Assert.IsFalse(tps.EnsureWritePivotPoints());
		}
	}
}
