using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RCore
{
	[System.Serializable]
	public class UnityObjectWrapper<T> where T : class
	{
		[SerializeField] private Object value;

		public T Value => value as T;

		public UnityObjectWrapper(Object value)
		{
			this.value = value;
		}
	}
#if ADDRESSABLES
	[System.Serializable]
	public class AssetReferenceWrapper<T> where T : class
	{
		[SerializeField] private AssetReference value;

		public T Value => value as T;

		public AssetReferenceWrapper(AssetReference value)
		{
			this.value = value;
		}
	}
#endif
}