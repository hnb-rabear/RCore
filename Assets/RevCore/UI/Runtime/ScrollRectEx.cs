using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RevCore.UI
{
	/// <summary>
	/// <see cref="ScrollRect"/> that re-routes drags along an orthogonal axis to a parent drag
	/// handler. The standard ScrollRect captures drags even when the user clearly meant to drag
	/// the parent — this subclass detects that case (drag axis disagrees with the local axis
	/// configuration) and forwards drag events up the hierarchy.
	/// </summary>
	public class ScrollRectEx : ScrollRect
	{
		private bool m_routeToParent;

		private void DoForParents<T>(Action<T> action) where T : IEventSystemHandler
		{
			var parent = transform.parent;
			while (parent != null)
			{
				foreach (var component in parent.GetComponents<Component>())
				{
					if (component is T handler)
						action(handler);
				}
				parent = parent.parent;
			}
		}

		/// <inheritdoc />
		public override void OnInitializePotentialDrag(PointerEventData eventData)
		{
			DoForParents<IInitializePotentialDragHandler>(parent => parent.OnInitializePotentialDrag(eventData));
			base.OnInitializePotentialDrag(eventData);
		}

		/// <inheritdoc />
		public override void OnDrag(PointerEventData eventData)
		{
			if (m_routeToParent)
				DoForParents<IDragHandler>(parent => parent.OnDrag(eventData));
			else
				base.OnDrag(eventData);
		}

		/// <inheritdoc />
		public override void OnBeginDrag(PointerEventData eventData)
		{
			if (!horizontal && Math.Abs(eventData.delta.x) > Math.Abs(eventData.delta.y))
				m_routeToParent = true;
			else if (!vertical && Math.Abs(eventData.delta.x) < Math.Abs(eventData.delta.y))
				m_routeToParent = true;
			else
				m_routeToParent = false;

			if (m_routeToParent)
				DoForParents<IBeginDragHandler>(parent => parent.OnBeginDrag(eventData));
			else
				base.OnBeginDrag(eventData);
		}

		/// <inheritdoc />
		public override void OnEndDrag(PointerEventData eventData)
		{
			if (m_routeToParent)
				DoForParents<IEndDragHandler>(parent => parent.OnEndDrag(eventData));
			else
				base.OnEndDrag(eventData);

			m_routeToParent = false;
		}
	}
}
