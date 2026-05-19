using System;
using NUnit.Framework;
using RevCore;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RevCore.Tests
{
	public class AddressableLoadExceptionTests
	{
		[Test]
		public void constructor_populates_key_status_and_inner_exception()
		{
			var inner = new InvalidOperationException("boom");
			var ex = new AddressableLoadException("MyKey", AsyncOperationStatus.Failed, inner);

			Assert.AreEqual("MyKey", ex.Key);
			Assert.AreEqual(AsyncOperationStatus.Failed, ex.Status);
			Assert.AreSame(inner, ex.InnerException);
			StringAssert.Contains("MyKey", ex.Message);
			StringAssert.Contains("Failed", ex.Message);
		}
	}
}