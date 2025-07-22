using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RCore.UI
{
	/// <summary>
	/// An extended version of the standard Unity ScrollRect.
	/// This class is designed to handle nested scroll rects more effectively. For example, a vertical scroll rect
	/// placed inside a horizontal one. It intelligently decides whether to handle a drag event itself or to pass it 
	/// to a parent scroll rect based on the direction of the drag gesture. If the user drags in a direction
	/// that this scroll rect doesn't handle, the drag event is routed to its parents.
	/// </summary>
	public class ScrollRectEx : ScrollRect
	{
		/// <summary>
		/// A flag that determines if the current drag event should be passed up to parent event handlers.
		/// This is set to true in OnBeginDrag if the drag direction is perpendicular to the scroll direction.
		/// </summary>
		private bool m_routeToParent;

		/// <summary>
		/// Traverses up the transform hierarchy and executes a given action on all components
		/// of type T that are found on the parent GameObjects.
		/// </summary>
		/// <typeparam name="T">The type of the event handler interface to look for (e.g., IDragHandler).</typeparam>
		/// <param name="action">The action to execute on each found component.</param>
		private void DoForParents<T>(Action<T> action) where T : IEventSystemHandler
		{
			var parent = transform.parent;
			while (parent != null)
			{
				foreach (var component in parent.GetComponents<Component>())
				{
					if (component is T)
						action((T)(IEventSystemHandler)component);
				}
				parent = parent.parent;
			}
		}

		/// <summary>
		/// Overrides the base method to always route the OnInitializePotentialDrag event to parents.
		/// This ensures that parent scroll rects are prepared to handle the drag if needed.
		/// </summary>
		/// <param name="eventData">The pointer event data.</param>
		public override void OnInitializePotentialDrag(PointerEventData eventData)
		{
			DoForParents<IInitializePotentialDragHandler>((parent) =>
			{
				parent.OnInitializePotentialDrag(eventData);
			});
			base.OnInitializePotentialDrag(eventData);
		}

		/// <summary>
		/// Overrides the base OnDrag method.
		/// If m_routeToParent is true, it passes the drag event to parent handlers.
		/// Otherwise, it performs the standard drag behavior.
		/// </summary>
		/// <param name="eventData">The pointer event data.</param>
		public override void OnDrag(PointerEventData eventData)
		{
			if (m_routeToParent)
				DoForParents<IDragHandler>((parent) =>
				{
					parent.OnDrag(eventData);
				});
			else
				base.OnDrag(eventData);
		}

		/// <summary>
		/// Overrides the base OnBeginDrag method.
		/// This is the core logic that determines whether to handle the drag or pass it to a parent.
		/// It compares the absolute delta of the drag on the X and Y axes. If the drag is primarily
		/// in a direction that this scroll rect does not support, it sets m_routeToParent to true.
		/// </summary>
		/// <param name="eventData">The pointer event data.</param>
		public override void OnBeginDrag(PointerEventData eventData)
		{
			// If this scroll rect is vertical-only and the drag is more horizontal
			if (!horizontal && Math.Abs(eventData.delta.x) > Math.Abs(eventData.delta.y))
				m_routeToParent = true;
			// If this scroll rect is horizontal-only and the drag is more vertical
			else if (!vertical && Math.Abs(eventData.delta.x) < Math.Abs(eventData.delta.y))
				m_routeToParent = true;
			// Otherwise, this scroll rect should handle the drag
			else
				m_routeToParent = false;

			if (m_routeToParent)
				DoForParents<IBeginDragHandler>((parent) =>
				{
					parent.OnBeginDrag(eventData);
				});
			else
				base.OnBeginDrag(eventData);
		}

		/// <summary>
		/// Overrides the base OnEndDrag method.
		/// If m_routeToParent is true, it passes the end drag event to parent handlers.
		/// Otherwise, it performs the standard end drag behavior. Finally, it resets the m_routeToParent flag.
		/// </summary>
		/// <param name="eventData">The pointer event data.</param>
		public override void OnEndDrag(PointerEventData eventData)
		{
			if (m_routeToParent)
				DoForParents<IEndDragHandler>((parent) =>
				{
					parent.OnEndDrag(eventData);
				});
			else
				base.OnEndDrag(eventData);
			
			// Reset the flag for the next drag event
			m_routeToParent = false;
		}
	}
}