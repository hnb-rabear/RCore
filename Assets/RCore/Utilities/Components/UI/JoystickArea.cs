using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RCore.Common;

namespace RCore.Components
{
    public class JoystickArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Canvas canvas;
        public Joystick joystick;
        public bool autoHideJoystick = true;

        private void Start()
        {
            if (autoHideJoystick)
                joystick.SetActive(false);
        }

        private void OnDisable()
        {
            if (autoHideJoystick)
                joystick.SetActive(false);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            joystick.OnBeginDrag(eventData);
        }

        internal void SetCanvas(Canvas pSanvas)
        {
            canvas = pSanvas;
        }

        public void OnDrag(PointerEventData eventData)
        {
            joystick.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            joystick.OnEndDrag(eventData);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (autoHideJoystick)
                joystick.SetActive(true);

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                joystick.transform.position = eventData.position;
            else if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
                joystick.transform.position = eventData.pointerPressRaycast.worldPosition;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                joystick.transform.position = eventData.position;
            else if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
                joystick.transform.position = eventData.pointerPressRaycast.worldPosition;

            if (autoHideJoystick)
                joystick.SetActive(false);
        }
    }
}