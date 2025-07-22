/***
 * Author HNB-RaBear - 2017
 */

using UnityEngine;
using UnityEngine.Serialization;

namespace RCore.UI
{
	/// <summary>
	/// This is the abstract base class for any UI element intended for use within an
	/// OptimizedScrollView or OptimizedVerticalScrollView. It defines the core contract for how a
	/// recyclable item should behave. Developers must create a new script that inherits from this
	/// class and implement the OnUpdateContent method to define how the item's visuals
	/// (like text and images) are updated based on its assigned data index.
	/// </summary>
	public abstract class OptimizedScrollItem : MonoBehaviour
	{
		/// <summary>The internal virtual index of the data this item currently represents.</summary>
		protected int m_index = -1;
		
		/// <summary>
		/// Gets the virtual index of the data this item is currently displaying.
		/// This is a read-only property.
		/// </summary>
		public int Index => m_index;
		
		/// <summary>A flag used to determine if the item's content needs to be visually updated.</summary>
		private bool m_refresh;
		
		/// <summary>
		/// Gets or sets whether this item is currently visible within the scroll view's viewport.
		/// This property is set by the parent OptimizedScrollView and can be used to optimize
		/// behavior, such as pausing animations on items that are not visible.
		/// </summary>
		public bool visible { get; set; }

		/// <summary>
		/// Called by the parent scroll view to assign a new data index to this item.
		/// It marks the item as "dirty" so its content will be refreshed on the next ManualUpdate call.
		/// </summary>
		/// <param name="pIndex">The new virtual index for this item.</param>
		/// <param name="pForced">If true, the item will be marked for refresh even if the index is the same as the current one.</param>
		/// <returns>True if the item was marked for refresh, otherwise false.</returns>
		public bool UpdateContent(int pIndex, bool pForced = false)
		{
			if (m_index.Equals(pIndex) && !pForced)
				return false;
			m_index = pIndex;
			m_refresh = true;
			return true;
		}

		/// <summary>
		/// An optional, overridable method to update the item's content using a full data object.
		/// This provides an alternative way to pass data to the item beyond just its index.
		/// </summary>
		/// <param name="data">The data object to populate the item with.</param>
		public virtual void UpdateContent(object data) { }

		/// <summary>
		/// This method is called by the parent scroll view every frame. It acts as a controlled
		/// Update() loop. It checks if a refresh is needed and, if so, calls OnUpdateContent.
		/// </summary>
		public void ManualUpdate()
		{
			if (m_refresh && gameObject.activeInHierarchy)
			{
				OnUpdateContent();
				m_refresh = false;
			}
		}

		/// <summary>
		/// Manually flags this item to be refreshed on the next ManualUpdate call.
		/// </summary>
		public void Refresh()
		{
			m_refresh = true;
		}

		/// <summary>
		/// This is the core abstract method that must be implemented by any inheriting class.
		/// Place all the logic for updating the visual elements of this item (e.g., setting text,
		/// loading sprites, changing colors) inside this method. It is called automatically
		/// when the item needs to display new data.
		/// </summary>
		protected abstract void OnUpdateContent();
	}
}