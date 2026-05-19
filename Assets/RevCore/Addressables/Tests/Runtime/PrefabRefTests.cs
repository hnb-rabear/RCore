using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using RevCore;
using UnityEngine;
using UnityEngine.TestTools;

namespace RevCore.Tests
{
	public class PrefabRefTests
	{
		[UnityTest]
		public IEnumerator instantiate_async_with_unknown_key_throws() => UniTask.ToCoroutine(async () =>
		{
			var r = new PrefabRef<Transform>("00000000000000000000000000000000");

			try
			{
				await r.InstantiateAsync(null);
				Assert.Fail("Expected AddressableLoadException");
			}
			catch (AddressableLoadException)
			{
				Assert.Pass();
			}
		});
	}
}
