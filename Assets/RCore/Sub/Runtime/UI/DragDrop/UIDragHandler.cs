/***
 * Author HNB-RaBear - 2017
 **/

using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace RCore.UI
{
    [Obsolete]
    public class UIDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        internal Vector3 preLocalPosition;
        internal UIDraggableItem owner;
        internal UIDraggableItem clone;
        internal Camera renderCamera;
        internal bool initialized;
        internal Transform preParent;
        internal UIDragController dragController;
        internal bool dontShowItem;

        public void SetUp(UIDraggableItem pOwner, Camera pCamera)
        {
            renderCamera = pCamera;
            owner = pOwner;
            dragController = GetComponentInParent<UIDragController>();
            initialized = true;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!owner.enableDrag || !initialized)
                return;

            var item = GetDraggableItem();
            preLocalPosition = item.transform.localPosition;
            preParent = item.transform.parent;
            item.transform.SetParent(dragController.dragContainer);
            item.BeginDrag(eventData);
            item.SetActive(!dontShowItem);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!owner.enableDrag || !initialized)
                return;

            var item = GetDraggableItem();
            item.transform.position = renderCamera.MousePointToWorldPoint();
            item.Drag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!owner.enableDrag || !initialized)
                return;

            var item = GetDraggableItem();
            item.EndDrag(eventData);
            item.transform.SetParent(preParent);
            item.transform.localPosition = preLocalPosition;

            if (!owner.dragOriginal)
                item.SetActive(false);
        }

        public UIDraggableItem GetDraggableItem()
        {
            if (owner.dragOriginal)
                return owner;
            else
            {
                if (clone == null)
                {
                    clone = Instantiate(owner, dragController.dragContainer);
                    clone.GetComponent<RectTransform>().sizeDelta = owner.GetComponent<RectTransform>().sizeDelta;
                    clone.Cloned();
                }
                return clone;
            }
        }
    }
}