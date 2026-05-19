using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RevCore
{
	/// <summary>
	/// Tagged variant of <see cref="AssetRef{T}"/> that pairs the serialized Addressables reference with a user-defined key.
	/// </summary>
	/// <remarks>
	/// Use this wrapper when a serialized collection of asset references needs an identifier (such as an enum, integer, or
	/// string tag) so callers can look up an entry without relying on list ordering. Inherits all loading, caching, and
	/// release semantics from <see cref="AssetRef{T}"/>. Replaces RCore's <c>AssetBundleWithEnumKey</c>,
	/// <c>AssetBundleWith2EnumKeys</c>, and <c>AssetBundleWithIntKey</c>.
	/// </remarks>
	/// <typeparam name="TKey">The key type used to identify this asset reference (for example an enum or composite struct).</typeparam>
	/// <typeparam name="T">The Unity asset type referenced by this wrapper.</typeparam>
	[Serializable]
	public class KeyedAssetRef<TKey, T> : AssetRef<T> where T : Object
	{
		[SerializeField] private TKey m_key;

		/// <summary>
		/// Gets or sets the user-defined key that identifies this asset reference.
		/// </summary>
		/// <value>The key paired with the serialized Addressables reference.</value>
		public TKey Key { get => m_key; set => m_key = value; }
	}
}
