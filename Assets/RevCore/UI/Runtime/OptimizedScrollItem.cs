using UnityEngine;

namespace RevCore.UI
{
	public abstract class OptimizedScrollItem : MonoBehaviour
	{
		protected int m_index = -1;
		private bool m_refresh;

		public int Index => m_index;
		public bool visible { get; set; }

		public bool UpdateContent(int index, bool forced = false)
		{
			if (m_index.Equals(index) && !forced)
				return false;

			m_index = index;
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
