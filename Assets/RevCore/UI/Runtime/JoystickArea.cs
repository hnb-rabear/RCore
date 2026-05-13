using UnityEngine;
using UnityEngine.EventSystems;

namespace RevCore.UI
{
	public class JoystickArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		public Canvas canvas;
		public Joystick joystick;
		public bool autoHideJoystick = true;

		private void Start()
		{
			if (autoHideJoystick)
				joystick.gameObject.SetActive(false);
		}

		private void OnDisable()
		{
			if (autoHideJoystick)
				joystick.gameObject.SetActive(false);
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			if (eventData.pointerId != 0 && eventData.pointerId != -1)
				return;

			joystick.OnBeginDrag(eventData);
		}

		public void SetCanvas(Canvas targetCanvas)
		{
			canvas = targetCanvas;
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (eventData.pointerId != 0 && eventData.pointerId != -1)
				return;

			joystick.OnDrag(eventData);
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			if (eventData.pointerId != 0 && eventData.pointerId != -1)
				return;

			joystick.OnEndDrag(eventData);
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			if (eventData.pointerId != 0 && eventData.pointerId != -1)
				return;

			if (autoHideJoystick)
				joystick.gameObject.SetActive(true);

			if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
				joystick.transform.position = eventData.position;
			else if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
				joystick.transform.position = eventData.pointerPressRaycast.worldPosition;
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			if (eventData.pointerId != 0 && eventData.pointerId != -1)
				return;

			if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
				joystick.transform.position = eventData.position;
			else if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
				joystick.transform.position = eventData.pointerPressRaycast.worldPosition;

			if (autoHideJoystick)
				joystick.gameObject.SetActive(false);
		}
	}
}
