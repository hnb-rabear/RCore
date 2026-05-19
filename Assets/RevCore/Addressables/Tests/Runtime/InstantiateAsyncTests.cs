using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using RevCore;
using UnityEngine;
using UnityEngine.TestTools;

namespace RevCore.Tests
{
	public class InstantiateAsyncTests
	{
		[UnityTest]
		public IEnumerator instantiate_async_with_unknown_address_throws_AddressableLoadException() => UniTask.ToCoroutine(async () =>
		{
			try
			{
				await AddressableLoader.InstantiateAsync("does-not-exist", null);
				Assert.Fail("Expected AddressableLoadException");
			}
			catch (AddressableLoadException)
			{
				Assert.Pass();
			}
		});
	}
}
