using UnityEngine;

namespace RevCore.UI
{
	/// <summary>
	/// Base class for items in an <see cref="OptimizedScrollView"/> (or its horizontal/vertical
	/// variants). The scroll view recycles a small pool of instances and reassigns each one's
	/// <see cref="Index"/> as the user scrolls; <see cref="OnUpdateContent"/> is the hook where
	/// each subclass rebinds its visuals to the data at the new index.
	/// </summary>
	public abstract class OptimizedScrollItem : MonoBehaviour
	{
		protected int m_index = -1;
		private bool m_refresh;

		/// <summary>The data index this item currently represents. -1 before the first bind.</summary>
		public int Index => m_index;

		/// <summary>Visibility flag used by the scroll view's culling pass.</summary>
		public bool visible { get; set; }

		/// <summary>
		/// Updates the item's index. Returns <c>true</c> when the index actually changed (or
		/// <paramref name="forced"/> is set). Subsequent <see cref="ManualUpdate"/> will invoke
		/// <see cref="OnUpdateContent"/>.
		/// </summary>
		public bool UpdateContent(int index, bool forced = false)
		{
			if (m_index.Equals(index) && !forced)
				return false;

			m_index = index;
			m_refresh = true;
			return true;
		}

		/// <summary>Alternative bind path that takes an arbitrary data object. Default implementation is a no-op; override when your scroll view passes data directly.</summary>
		public virtual void UpdateContent(object data) { }

		/// <summary>Called once per frame by the scroll view's LateUpdate. Invokes <see cref="OnUpdateContent"/> if a refresh is pending and the item is active.</summary>
		public void ManualUpdate()
		{
			if (m_refresh && gameObject.activeInHierarchy)
			{
				OnUpdateContent();
				m_refresh = false;
			}
		}

		/// <summary>Marks the item as needing a content refresh on the next <see cref="ManualUpdate"/>.</summary>
		public void Refresh()
		{
			m_refresh = true;
		}

		/// <summary>Subclass-implemented bind that reads from <see cref="Index"/> and updates the item's child graphics.</summary>
		protected abstract void OnUpdateContent();
	}
}
