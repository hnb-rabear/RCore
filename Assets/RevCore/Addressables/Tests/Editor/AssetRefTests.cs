using NUnit.Framework;
using RevCore;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RevCore.Tests
{
	public class AssetRefTests
	{
		[Test]
		public void newly_constructed_ref_is_not_loaded_and_not_loading()
		{
			var assetRef = new AssetRef<Texture2D>();

			Assert.IsNull(assetRef.Reference);
			Assert.IsNull(assetRef.Asset);
			Assert.IsFalse(assetRef.IsLoading);
			Assert.IsFalse(assetRef.IsLoaded);
		}

		[Test]
		public void load_async_with_invalid_reference_throws_AddressableLoadException()
		{
			var assetRef = new AssetRef<Texture2D>();

			var ex = Assert.ThrowsAsync<AddressableLoadException>(async () =>
			{
				await assetRef.LoadAsync();
			});

			Assert.AreEqual("<null AssetReference>", ex.Key);
			Assert.AreEqual(AsyncOperationStatus.Failed, ex.Status);
			Assert.IsInstanceOf<System.InvalidOperationException>(ex.InnerException);
		}
	}
}
