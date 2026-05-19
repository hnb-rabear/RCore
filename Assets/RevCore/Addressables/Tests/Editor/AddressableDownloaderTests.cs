using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using RevCore;

namespace RevCore.Tests
{
	public class AddressableDownloaderTests : AddressableTestFixture
	{
		[Test]
		public void get_download_size_async_for_unknown_key_returns_zero()
		{
			Assert.DoesNotThrowAsync(async () =>
			{
				var size = await AddressableDownloader.GetDownloadSizeAsync("definitely-not-a-downloadable-key");
				Assert.AreEqual(0L, size);
			});
		}

		[Test]
		public void clear_dependency_cache_async_for_unknown_key_returns_true_or_false_without_throwing()
		{
			Assert.DoesNotThrowAsync(async () =>
			{
				var cleared = await AddressableDownloader.ClearDependencyCacheAsync("definitely-not-a-cached-key");
				Assert.That(cleared, Is.TypeOf<bool>());
			});
		}

		[Test]
		public void get_download_size_async_honours_cancellation_token()
		{
			using var cts = new CancellationTokenSource();
			cts.Cancel();
			Assert.ThrowsAsync<System.OperationCanceledException>(async () =>
			{
				await AddressableDownloader.GetDownloadSizeAsync("any", cts.Token);
			});
		}
	}
}
