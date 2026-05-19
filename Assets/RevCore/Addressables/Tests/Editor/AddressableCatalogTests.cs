using NUnit.Framework;
using RevCore;

namespace RevCore.Tests
{
	public class AddressableCatalogTests : AddressableTestFixture
	{
		[Test]
		public void check_for_catalog_updates_async_completes_without_throwing()
		{
			Assert.DoesNotThrowAsync(async () =>
			{
				var updates = await AddressableCatalog.CheckForCatalogUpdatesAsync();
				Assert.NotNull(updates);
			});
		}
	}
}
