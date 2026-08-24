using System;
using UnityEngine;

namespace RCore.SheetX
{
	public abstract class SheetXConfigCollectionBase : ScriptableObject
	{
		[NonSerialized] private bool m_isLoaded;

		public bool IsLoaded => m_isLoaded;

		public void SetLoaded()
		{
			m_isLoaded = true;
		}

		public void ResetLoaded()
		{
			m_isLoaded = false;
		}
	}
}
