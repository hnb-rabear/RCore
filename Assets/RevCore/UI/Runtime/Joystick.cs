using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RevCore.UI
{
	/// <summary>
	/// Virtual joystick UI control. The thumb is constrained inside a circular radius around the
	/// origin; <see cref="position"/> reports the normalized (-1..1) thumb offset. Drag events fire
	/// callbacks for game code to translate into movement.
	/// </summary>
	public class Joystick : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		/// <summary>Fired when the user begins a drag.</summary>
		public Action onDragBegin = delegate { };
		/// <summary>Fired each frame the joystick is dragged, with the normalized position.</summary>
		public Action<Vector3> onDrag = delegate { };
		/// <summary>Fired when the drag ends.</summary>
		public Action onDragEnd = delegate { };
		/// <summary>The thumb graphic that follows the touch within the joystick radius.</summary>
		public RectTransform thumb;
		/// <summary>Maximum thumb offset from origin, in local units.</summary>
		public float radius = 50f;
		/// <summary>Current normalized joystick position (-1..1 on each axis). Read each frame in <see cref="onDrag"/>.</summary>
		public Vector2 position;

		/// <inheritdoc />
		public void OnBeginDrag(PointerEventData eventData)
		{
			onDragBegin?.Invoke();
		}

		private void OnDisable()
		{
			OnEndDrag(null);
		}

		/// <inheritdoc />
		public void OnDrag(PointerEventData data)
		{
			var draggingPlane = transform as RectTransform;
			if (RectTransformUtility.ScreenPointToWorldPointInRectangle(draggingPlane, data.position, data.pressEventCamera, out Vector3 mousePos))
				thumb.position = mousePos;

			float length = thumb.localPosition.magnitude;
			if (length > radius)
				thumb.localPosition = Vector3.ClampMagnitude(thumb.localPosition, radius);

			position = thumb.localPosition;
			position = position / radius * Mathf.InverseLerp(radius, 2, 1);
		}

		/// <inheritdoc />
		public void OnEndDrag(PointerEventData eventData)
		{
			position = Vector2.zero;
			thumb.position = transform.position;
			onDragEnd();
		}

		private void FixedUpdate()
		{
#if UNITY_EDITOR
			thumb.localPosition = position * radius;
			thumb.localPosition = Vector3.ClampMagnitude(thumb.localPosition, radius);
#endif
			if (position != Vector2.zero)
				onDrag(position);
		}
	}
}
