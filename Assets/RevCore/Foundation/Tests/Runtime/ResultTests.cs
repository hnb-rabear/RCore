using NUnit.Framework;

namespace RevCore.Tests
{
	public class ResultTests
	{
		[Test]
		public void Ok_result_has_value()
		{
			var result = Result<int>.Ok(42);
			Assert.IsTrue(result.IsOk);
			Assert.IsFalse(result.IsError);
#pragma warning disable CS0618 // Value is [Obsolete] Stage 1 — exercised here on purpose for the pin.
			Assert.AreEqual(42, result.Value);
#pragma warning restore CS0618
		}

		[Test]
		public void Error_result_has_message()
		{
			var result = Result<int>.Fail("not found");
			Assert.IsFalse(result.IsOk);
			Assert.IsTrue(result.IsError);
			Assert.AreEqual("not found", result.ErrorMessage);
		}

		[Test]
		public void Value_on_error_throws()
		{
			var result = Result<int>.Fail("bad");
#pragma warning disable CS0618 // Value is [Obsolete] Stage 1 — exercised here on purpose for the pin.
			Assert.Throws<System.InvalidOperationException>(() => { var _ = result.Value; });
#pragma warning restore CS0618
		}

		[Test]
		public void TryGetValue_on_ok_returns_true()
		{
			var result = Result<int>.Ok(10);
			Assert.IsTrue(result.TryGetValue(out var val));
			Assert.AreEqual(10, val);
		}

		[Test]
		public void TryGetValue_on_error_returns_false()
		{
			var result = Result<int>.Fail("err");
			Assert.IsFalse(result.TryGetValue(out _));
		}

		[Test]
		public void Void_result_ok()
		{
			var result = Result.Ok();
			Assert.IsTrue(result.IsOk);
		}

		[Test]
		public void Void_result_fail()
		{
			var result = Result.Fail("oops");
			Assert.IsTrue(result.IsError);
			Assert.AreEqual("oops", result.ErrorMessage);
		}
	}
}
