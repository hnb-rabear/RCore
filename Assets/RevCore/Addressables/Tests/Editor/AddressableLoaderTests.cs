using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using RevCore;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RevCore.Tests
{
	public class AddressableLoaderTests : AddressableTestFixture
	{
		[Test]
		public void load_asset_async_with_unknown_address_throws_AddressableLoadException()
		{
			Assert.ThrowsAsync<AddressableLoadException>(async () =>
			{
				await AddressableLoader.LoadAssetAsync<Texture2D>("does-not-exist");
			});
		}

		[Test]
		public void load_asset_async_carries_address_as_key_on_failure()
		{
			var ex = Assert.ThrowsAsync<AddressableLoadException>(async () =>
			{
				await AddressableLoader.LoadAssetAsync<Texture2D>("missing-key");
			});
			Assert.AreEqual("missing-key", ex.Key);
			Assert.AreEqual(AsyncOperationStatus.Failed, ex.Status);
		}

		[Test]
		public void load_asset_async_honours_cancellation_token()
		{
			using var cts = new CancellationTokenSource();
			cts.Cancel();
			Assert.ThrowsAsync<OperationCanceledException>(async () =>
			{
				await AddressableLoader.LoadAssetAsync<Texture2D>("any", ct: cts.Token);
			});
		}

		[Test]
		public void load_asset_async_with_invalid_AssetReference_throws()
		{
			var reference = new UnityEngine.AddressableAssets.AssetReference("00000000000000000000000000000000");
			Assert.ThrowsAsync<AddressableLoadException>(async () =>
			{
				await AddressableLoader.LoadAssetAsync<Texture2D>(reference);
			});
		}

		[Test]
		public void load_asset_with_handle_async_returns_handle_for_caller_release()
		{
			Assert.ThrowsAsync<AddressableLoadException>(async () =>
			{
				var handle = await AddressableLoader.LoadAssetWithHandleAsync<Texture2D>("missing");
				Assert.Fail("Expected exception, got " + handle);
			});
		}
	}
}