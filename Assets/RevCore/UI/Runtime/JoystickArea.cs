using UnityEngine;
using UnityEngine.EventSystems;

namespace RevCore.UI
{
	/// <summary>
	/// Large invisible touch zone that spawns a <see cref="Joystick"/> at the user's first touch
	/// position. Lets the player place the joystick wherever they grab the screen rather than
	/// requiring them to find a fixed origin.
	/// </summary>
	public class JoystickArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		/// <summary>Canvas the joystick is parented to. Used to pick the correct world-position projection mode.</summary>
		public Canvas canvas;
		/// <summary>The joystick instance this area drives.</summary>
		public Joystick joystick;
		/// <summary>When true, hides the joystick on pointer-up so the screen stays clean between touches.</summary>
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

		/// <inheritdoc />
		public void OnBeginDrag(PointerEventData eventData)
		{
			if (eventData.pointerId != 0 && eventData.pointerId != -1)
				return;

			joystick.OnBeginDrag(eventData);
		}

		/// <summary>Updates the canvas reference at runtime — useful when the joystick is reparented across canvases.</summary>
		public void SetCanvas(Canvas targetCanvas)
		{
			canvas = targetCanvas;
		}

		/// <inheritdoc />
		public void OnDrag(PointerEventData eventData)
		{
			if (eventData.pointerId != 0 && eventData.pointerId != -1)
				return;

			joystick.OnDrag(eventData);
		}

		/// <inheritdoc />
		public void OnEndDrag(PointerEventData eventData)
		{
			if (eventData.pointerId != 0 && eventData.pointerId != -1)
				return;

			joystick.OnEndDrag(eventData);
		}

		/// <inheritdoc />
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

		/// <inheritdoc />
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
