using UnityEngine;

namespace RCore.SheetX
{
	public abstract class GlobalConfigCollectionBase : SheetXConfigCollectionBase
	{
		private static class InstanceHolder<T> where T : GlobalConfigCollectionBase
		{
			public static T InjectedInstance;
		}

		public static T Instance<T>() where T : GlobalConfigCollectionBase
		{
			if (InstanceHolder<T>.InjectedInstance != null)
				return InstanceHolder<T>.InjectedInstance;

			return Resources.Load<T>(typeof(T).Name);
		}

		public static void SetInstance<T>(T collection) where T : GlobalConfigCollectionBase
		{
			InstanceHolder<T>.InjectedInstance = collection;
		}
	}
}
