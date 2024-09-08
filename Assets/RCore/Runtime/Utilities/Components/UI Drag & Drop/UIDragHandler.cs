/***
 * Author RadBear - nbhung71711@gmail.com - 2017
 **/

using UnityEngine;
using UnityEngine.EventSystems;
using RCore.Common;

namespace RCore.Components
{
    public class UIDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        internal Vector3 preLocalPosition;
        internal UIDragableItem owner;
        internal UIDragableItem clone;
        internal Camera renderCamera;
        internal bool initialzied;
        internal Transform preParent;
        internal UIDragController dragController;
        internal bool dontShowItem;

        public void SetUp(UIDragableItem pOwner, Camera pCamera)
        {
            renderCamera = pCamera;
            owner = pOwner;
            dragController = GetComponentInParent<UIDragController>();
            initialzied = true;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!owner.enableDrag || !initialzied)
                return;

            var item = GetDragableItem();
            preLocalPosition = item.transform.localPosition;
            preParent = item.transform.parent;
            item.transform.SetParent(dragController.dragContainer);
            item.BeginDrag(eventData);
            item.SetActive(!dontShowItem);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!owner.enableDrag || !initialzied)
                return;

            var item = GetDragableItem();
            item.transform.position = renderCamera.MousePointToWorldPoint();
            item.Drag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!owner.enableDrag || !initialzied)
                return;

            var item = GetDragableItem();
            item.EndDrag(eventData);
            item.transform.SetParent(preParent);
            item.transform.localPosition = preLocalPosition;

            if (!owner.dragOriginal)
                item.SetActive(false);
        }

        public UIDragableItem GetDragableItem()
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