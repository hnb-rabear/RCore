/***
 * Author HNB-RaBear - 2017
 */

using UnityEngine;
using UnityEngine.Serialization;

namespace RCore.UI
{
	public abstract class OptimizedScrollItem : MonoBehaviour
	{
		protected int m_index = -1;
		public int Index => m_index;
		private bool m_refresh;
		public bool visible { get; set; }

		public bool UpdateContent(int pIndex, bool pForced = false)
		{
			if (m_index.Equals(pIndex) && !pForced)
				return false;
			m_index = pIndex;
			m_refresh = true;
			return true;
		}

		public virtual void UpdateContent(object data) { }

		public void ManualUpdate()
		{
			if (m_refresh && gameObject.activeInHierarchy)
			{
				OnUpdateContent();
				m_refresh = false;
			}
		}

		public void Refresh()
		{
			m_refresh = true;
		}

		protected abstract void OnUpdateContent();
	}
}