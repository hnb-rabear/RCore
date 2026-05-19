using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace RevCore.Tests
{
	/// <summary>
	/// Test-only <see cref="IResourceLocator"/> that returns canned <see cref="IResourceLocation"/> entries
	/// for a fixed set of keys. Allows EditMode tests to exercise <c>Addressables.LoadAssetAsync</c> without
	/// the real catalog or asset bundles.
	/// </summary>
	internal sealed class FakeResourceLocator : IResourceLocator
	{
		private readonly Dictionary<object, IList<IResourceLocation>> m_map = new();

		public string LocatorId => nameof(FakeResourceLocator);
		public IEnumerable<object> Keys => m_map.Keys;

		public void Register(object key, IResourceLocation location)
		{
			if (!m_map.TryGetValue(key, out var list))
			{
				list = new List<IResourceLocation>();
				m_map[key] = list;
			}
			list.Add(location);
		}

		public bool Locate(object key, Type type, out IList<IResourceLocation> locations)
		{
			return m_map.TryGetValue(key, out locations);
		}

		public sealed class FailingLocation : IResourceLocation
		{
			public FailingLocation(string key, Type type)
			{
				PrimaryKey = key;
				InternalId = key;
				ResourceType = type;
				ProviderId = "RevCore.Tests.FailingProvider";
			}

			public string InternalId { get; }
			public string ProviderId { get; }
			public IList<IResourceLocation> Dependencies => null;
			public int DependencyHashCode => 0;
			public bool HasDependencies => false;
			public object Data => null;
			public string PrimaryKey { get; }
			public Type ResourceType { get; }
			public int Hash(Type resultType) => PrimaryKey.GetHashCode();
		}
	}
}
