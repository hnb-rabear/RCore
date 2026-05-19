using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using RevCore;
using UnityEngine.TestTools;

namespace RevCore.Tests
{
	public class AddressableSceneTests
	{
		[UnityTest]
		public IEnumerator load_scene_async_with_unknown_key_throws() => UniTask.ToCoroutine(async () =>
		{
			try
			{
				await AddressableScene.LoadSceneAsync("does-not-exist");
				Assert.Fail("Expected AddressableLoadException");
			}
			catch (AddressableLoadException)
			{
				Assert.Pass();
			}
		});
	}
}
