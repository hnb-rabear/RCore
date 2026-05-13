using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RevCore.UI
{
	public class Joystick : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		public Action onDragBegin = delegate { };
		public Action<Vector3> onDrag = delegate { };
		public Action onDragEnd = delegate { };
		public RectTransform thumb;
		public float radius = 50f;
		public Vector2 position;

		public void OnBeginDrag(PointerEventData eventData)
		{
			onDragBegin?.Invoke();
		}

		private void OnDisable()
		{
			OnEndDrag(null);
		}

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
