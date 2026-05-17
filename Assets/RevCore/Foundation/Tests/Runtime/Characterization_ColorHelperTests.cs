// CHARACTERIZATION TESTS — pin current behavior of ColorHelper so that any
// future change is intentional. When Phase 3 introduces TryHexToColor and
// audits HexToColor's silent-fail behavior, these tests will need updating;
// the update is itself the signal that the contract changed.
//
// See docs/contributing/API_DESIGN_GUIDELINES.md → Error model.

using NUnit.Framework;
using UnityEngine;

namespace RevCore.Tests
{
	public class Characterization_ColorHelperTests
	{
		// PIN: invalid input is silently swallowed — HexToColor never throws. The exact RGBA on
		// failure is whatever Unity's ColorUtility.TryParseHtmlString leaves in its out param; on
		// Unity 2022.3 that is Color.white (1,1,1,1), NOT default(Color). Earlier notes assumed
		// default(Color); the run-through against the live engine corrected that. New code wanting
		// a stable failure value should use TryHexToColor, which enforces Color.clear.
		[Test]
		public void HexToColor_returns_Color_white_on_invalid_input()
		{
			var c = ColorHelper.HexToColor("not-a-hex");
			Assert.AreEqual(Color.white, c, "Unity 2022.3 leaves out param at Color.white on parse failure.");
		}

		// PIN: empty string follows the same silent-fail path as invalid input.
		[Test]
		public void HexToColor_returns_Color_white_on_empty_string()
		{
			var c = ColorHelper.HexToColor(string.Empty);
			Assert.AreEqual(Color.white, c);
		}

		// PIN: null input is silently tolerated by ColorUtility.TryParseHtmlString.
		// Phase 3 may choose to throw ArgumentNullException instead — that is a deliberate
		// breaking change and will require updating this test plus a CHANGELOG note.
		[Test]
		public void HexToColor_does_not_throw_on_null()
		{
			Assert.DoesNotThrow(() => ColorHelper.HexToColor(null));
		}

		// PIN: valid hex still parses correctly. (Sanity, not a behavior to remove.)
		[Test]
		public void HexToColor_parses_valid_RGB_hex()
		{
			var c = ColorHelper.HexToColor("#FF0000");
			Assert.AreEqual(1f, c.r, 0.01f);
			Assert.AreEqual(0f, c.g, 0.01f);
			Assert.AreEqual(0f, c.b, 0.01f);
			Assert.AreEqual(1f, c.a, 0.01f);
		}

		// PIN: valid RGBA hex parses with explicit alpha.
		[Test]
		public void HexToColor_parses_valid_RGBA_hex()
		{
			var c = ColorHelper.HexToColor("#00FF0080");
			Assert.AreEqual(0f, c.r, 0.01f);
			Assert.AreEqual(1f, c.g, 0.01f);
			Assert.AreEqual(0f, c.b, 0.01f);
			Assert.AreEqual(128f / 255f, c.a, 0.01f);
		}
	}
}
