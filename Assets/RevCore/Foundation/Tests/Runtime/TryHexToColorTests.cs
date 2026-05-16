using NUnit.Framework;
using UnityEngine;

namespace RevCore.Tests
{
	public class TryHexToColorTests
	{
		[Test]
		public void Valid_hex_returns_true_and_parses()
		{
			Assert.IsTrue(ColorHelper.TryHexToColor("#FF0000", out var c));
			Assert.AreEqual(1f, c.r, 0.01f);
			Assert.AreEqual(0f, c.g, 0.01f);
			Assert.AreEqual(0f, c.b, 0.01f);
			Assert.AreEqual(1f, c.a, 0.01f);
		}

		[Test]
		public void Invalid_hex_returns_false_and_clear()
		{
			Assert.IsFalse(ColorHelper.TryHexToColor("not-a-hex", out var c));
			Assert.AreEqual(Color.clear, c);
		}

		[Test]
		public void Null_input_returns_false_and_clear()
		{
			Assert.IsFalse(ColorHelper.TryHexToColor(null, out var c));
			Assert.AreEqual(Color.clear, c);
		}

		[Test]
		public void Empty_input_returns_false_and_clear()
		{
			Assert.IsFalse(ColorHelper.TryHexToColor(string.Empty, out var c));
			Assert.AreEqual(Color.clear, c);
		}

		[Test]
		public void RGBA_hex_parses_alpha()
		{
			Assert.IsTrue(ColorHelper.TryHexToColor("#00FF0080", out var c));
			Assert.AreEqual(128f / 255f, c.a, 0.01f);
		}
	}
}
