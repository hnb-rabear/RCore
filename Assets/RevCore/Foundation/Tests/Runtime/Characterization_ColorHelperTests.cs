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
		// PIN: invalid input produces default(Color) silently.
		// Phase 3 will add TryHexToColor; HexToColor's silent fallback stays for backward compat.
		[Test]
		public void HexToColor_returns_default_Color_on_invalid_input()
		{
			var c = ColorHelper.HexToColor("not-a-hex");
			Assert.AreEqual(default(Color), c);
			Assert.AreEqual(0f, c.r);
			Assert.AreEqual(0f, c.g);
			Assert.AreEqual(0f, c.b);
			Assert.AreEqual(0f, c.a, "Alpha is 0 — distinguishable from valid black #000000FF, but easy to miss in callers.");
		}

		// PIN: empty string also returns default(Color), not throwing.
		[Test]
		public void HexToColor_returns_default_Color_on_empty_string()
		{
			var c = ColorHelper.HexToColor(string.Empty);
			Assert.AreEqual(default(Color), c);
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
