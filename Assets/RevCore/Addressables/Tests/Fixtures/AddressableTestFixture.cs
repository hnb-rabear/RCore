using NUnit.Framework;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace RevCore.Tests
{
	/// <summary>
	/// NUnit base fixture that registers a <see cref="FakeResourceLocator"/> with the global
	/// Addressables system in <see cref="SetUp"/> and removes it in <see cref="TearDown"/>.
	/// Tests derive from this and call <see cref="Locator"/>.Register(...) inside the test body.
	/// </summary>
	public abstract class AddressableTestFixture
	{
		internal FakeResourceLocator Locator { get; private set; }

		[SetUp]
		public void BaseSetUp()
		{
			Locator = new FakeResourceLocator();
			Addressables.AddResourceLocator(Locator);
		}

		[TearDown]
		public void BaseTearDown()
		{
			if (Locator != null)
			{
				Addressables.RemoveResourceLocator(Locator);
				Locator = null;
			}
		}
	}
}
