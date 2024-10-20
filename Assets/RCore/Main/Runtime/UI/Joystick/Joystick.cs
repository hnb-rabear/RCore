using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RCore.UI
{
    public class Joystick : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Action onDragBegin = delegate { };
        public Action<Vector3> onDrag = delegate { };
        public Action onDragEnd = delegate { };
        /// <summary>
        /// The target object i.e. jostick thumb being dragged by the user.
        /// </summary>
        public RectTransform thumb;
        /// <summary>
        /// Maximum radius for the target object to be moved in distance from the center.
        /// </summary>
        public float radius = 50f;
        /// <summary>
        /// Current position of the target object on the x and y axis in 2D space.
        /// Values are calculated in the range of [-1, 1] translated to left/down right/up.
        /// </summary>
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
            //get RectTransforms of involved components
            var draggingPlane = transform as RectTransform;
            Vector3 mousePos;

            //check whether the dragged position is inside the dragging rect,
            //then set global mouse position and assign it to the joystick thumb
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(draggingPlane, data.position, data.pressEventCamera, out mousePos))
                thumb.position = mousePos;

            //length of the touch vector (magnitude)
            //calculated from the relative position of the joystick thumb
            float length = thumb.localPosition.magnitude;

            //if the thumb leaves the joystick's boundaries,
            //clamp it to the max radius
            if (length > radius)
                thumb.localPosition = Vector3.ClampMagnitude(thumb.localPosition, radius);

            //set the Vector2 thumb position based on the actual sprite position
            position = thumb.localPosition;
            //smoothly lerps the Vector2 thumb position based on the old positions
            position = position / radius * Mathf.InverseLerp(radius, 2, 1);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            //we aren't dragging anymore, reset to default position
            position = Vector2.zero;
            thumb.position = transform.position;

            //set dragging to false and fire callback
            onDragEnd();
        }

        private void FixedUpdate()
        {
            //in the editor the joystick position does not move, we have to simulate it
            //mirror player input to joystick position and calculate thumb position from that
#if UNITY_EDITOR
            thumb.localPosition = position * radius;
            thumb.localPosition = Vector3.ClampMagnitude(thumb.localPosition, radius);
#endif

            //check for actual drag state and fire callback. We are doing this in Update(),
            //not OnDrag, because OnDrag is only called when the joystick is moving. But we
            //actually want to keep moving the player even though the jostick is being hold down
            if (position != Vector2.zero)
                onDrag(position);
        }
    }
}