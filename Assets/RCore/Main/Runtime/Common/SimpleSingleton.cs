using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RCore
{
	public class SimpleSingleton<T> : MonoBehaviour where T : MonoBehaviour
	{
		private static T m_Instance;
		public static T Instance => m_Instance;

		protected virtual void Awake()
		{
			if (m_Instance == null)
				m_Instance = this as T;
			else if (m_Instance != this)
				Destroy(gameObject);
		}
	}
}