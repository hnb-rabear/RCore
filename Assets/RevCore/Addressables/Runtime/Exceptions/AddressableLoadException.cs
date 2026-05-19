using System;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RevCore
{
	/// <summary>
	/// Thrown when an Addressables operation fails. Wraps the underlying operation exception and carries the originating key.
	/// </summary>
	public sealed class AddressableLoadException : Exception
	{
		/// <summary>The key (address, <see cref="UnityEngine.AddressableAssets.AssetReference"/>, or location) that failed to load.</summary>
		public object Key { get; }

		/// <summary>The terminal status reported by the failed operation.</summary>
		public AsyncOperationStatus Status { get; }

		/// <summary>Initialises a new <see cref="AddressableLoadException"/>.</summary>
		/// <param name="key">The originating key for the failed load.</param>
		/// <param name="status">The operation's final status.</param>
		/// <param name="inner">The underlying exception reported by Addressables, if any.</param>
		public AddressableLoadException(object key, AsyncOperationStatus status, Exception inner)
			: base($"Addressable load failed: {key} (status={status})", inner)
		{
			Key = key;
			Status = status;
		}
	}
}