using System;
using TMPro;
#if ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif
using UnityEngine.U2D;

namespace RCore
{
#if ADDRESSABLES
	/// <summary>
	/// A specialized <see cref="AssetReferenceT{TObject}"/> for loading a <see cref="SpriteAtlas"/>.
	/// </summary>
	[Serializable]
	public class AssetRef_SpriteAtlas : AssetReferenceT<SpriteAtlas>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AssetRef_SpriteAtlas"/> class.
		/// </summary>
		/// <param name="guid">The GUID of the addressable asset.</param>
		public AssetRef_SpriteAtlas(string guid) : base(guid) { }
	}

	/// <summary>
	/// A specialized <see cref="AssetReferenceT{TObject}"/> for loading a <see cref="TMP_FontAsset"/>.
	/// </summary>
	[Serializable]
	public class AssetRef_FontAsset : AssetReferenceT<TMP_FontAsset>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AssetRef_FontAsset"/> class.
		/// </summary>
		/// <param name="guid">The GUID of the addressable asset.</param>
		public AssetRef_FontAsset(string guid) : base(guid) { }
	}
#endif
}