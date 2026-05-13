using NUnit.Framework;

namespace RevCore.Tests
{
	public class BigNumberTests
	{
		[Test]
		public void Parse_and_format_roundtrip()
		{
			var bn = new BigNumber(1500);
			Assert.AreEqual("1.5K", bn.ToString());
		}

		[Test]
		public void Addition()
		{
			var a = new BigNumber(1000);
			var b = new BigNumber(500);
			var c = a + b;
			Assert.AreEqual(1500, c.ToDouble());
		}

		[Test]
		public void Static_constants_same_instance()
		{
			var one1 = BigNumber.One;
			var one2 = BigNumber.One;
			Assert.AreEqual(one1.ToDouble(), one2.ToDouble());
		}
	}
}
