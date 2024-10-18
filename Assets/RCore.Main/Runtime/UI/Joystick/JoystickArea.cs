﻿using UnityEngine;
using UnityEngine.EventSystems;
using RCore.Common;

namespace RCore.UI
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
            //Only Left Mouse and Single Tap are allowed
            if (eventData.pointerId != 0 && eventData.pointerId != -1)
                return;

            joystick.OnBeginDrag(eventData);
        }

        internal void SetCanvas(Canvas pSanvas)
        {
            canvas = pSanvas;
        }

        public void OnDrag(PointerEventData eventData)
        {
            //Only Left Mouse and Single Tap are allowed
            if (eventData.pointerId != 0 && eventData.pointerId != -1)
                return;

            joystick.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            //Only Left Mouse and Single Tap are allowed
            if (eventData.pointerId != 0 && eventData.pointerId != -1)
                return;

            joystick.OnEndDrag(eventData);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            //Only Left Mouse and Single Tap are allowed
            if (eventData.pointerId != 0 && eventData.pointerId != -1)
                return;

            if (autoHideJoystick)
                joystick.SetActive(true);

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                joystick.transform.position = eventData.position;
            else if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
                joystick.transform.position = eventData.pointerPressRaycast.worldPosition;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            //Only Left Mouse and Single Tap are allowed
            if (eventData.pointerId != 0 && eventData.pointerId != -1)
                return;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                joystick.transform.position = eventData.position;
            else if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
                joystick.transform.position = eventData.pointerPressRaycast.worldPosition;

            if (autoHideJoystick)
                joystick.SetActive(false);
        }
    }
}