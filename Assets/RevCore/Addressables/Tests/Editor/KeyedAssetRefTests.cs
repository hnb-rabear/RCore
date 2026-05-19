using NUnit.Framework;
using UnityEngine;

namespace RevCore.Tests
{
	public class KeyedAssetRefTests
	{
		private enum SampleKey
		{
			None = 0,
			Hero = 1,
			Villain = 2,
		}

		[Test]
		public void key_property_round_trips()
		{
			var keyedRef = new KeyedAssetRef<SampleKey, Texture2D>();

			keyedRef.Key = SampleKey.Villain;

			Assert.AreEqual(SampleKey.Villain, keyedRef.Key);
		}

		[Test]
		public void inherits_AssetRef_lifecycle_flags()
		{
			var keyedRef = new KeyedAssetRef<SampleKey, Texture2D>();

			Assert.IsFalse(keyedRef.IsLoaded);
			Assert.IsFalse(keyedRef.IsLoading);
			Assert.IsNull(keyedRef.Asset);
			Assert.IsNull(keyedRef.Reference);
		}
	}
}
