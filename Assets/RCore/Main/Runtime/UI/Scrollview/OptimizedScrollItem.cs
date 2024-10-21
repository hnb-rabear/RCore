/***
* Author RadBear - nbhung71711 @gmail.com - 2017
**/

using UnityEngine;

namespace RCore.UI
{
	public abstract class OptimizedScrollItem : MonoBehaviour
	{
		protected int m_Index = -1;
		public int Index => m_Index;
		private bool m_Refresh;

		public void UpdateContent(int pIndex, bool pForced)
		{
			if (m_Index.Equals(pIndex) && !pForced)
				return;
			m_Index = pIndex;
			m_Refresh = true;
		}
        
		public void ManualUpdate()
		{
			if (m_Refresh && gameObject.activeInHierarchy)
			{
				OnUpdateContent();
				m_Refresh = false;
			}
		}

		public void Refresh()
		{
			m_Refresh = true;
		}

		protected abstract void OnUpdateContent();
	}
}