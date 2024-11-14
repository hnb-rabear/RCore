/***
 * Author HNB-RaBear - 2017
 **/

using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RCore.UI
{
    /// <summary>
    /// Drag an item internal UI-Canvas
    /// </summary>
    [Obsolete]
    public class UIDraggableItem : MonoBehaviour
    {
        public Camera renderCamera;
        public bool dragOriginal;

        protected CanvasGroup mCanvasGroup;
        protected UIDragHandler mDragHandler;

        internal bool enableDrag = true;
        internal bool dragging = false;

        protected virtual void Awake()
        {
            if (renderCamera == null)
                renderCamera = Camera.main;

            GetDragHandler().SetUp(this, renderCamera);
        }

        public virtual void Cloned()
        {

        }

        public virtual void BeginDrag(PointerEventData eventData)
        {
            GetCanvasGroup().blocksRaycasts = false;
            dragging = true;
        }

        public virtual void EndDrag(PointerEventData eventData)
        {
            GetCanvasGroup().blocksRaycasts = true;
            dragging = false;
        }

        public virtual void Drag(PointerEventData eventData)
        {
            
        }

        public CanvasGroup GetCanvasGroup()
        {
            if (mCanvasGroup != null)
                return mCanvasGroup;

            mCanvasGroup = GetComponent<CanvasGroup>();
            if (mCanvasGroup == null)
                mCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            return mCanvasGroup;
        }

        public UIDragHandler GetDragHandler()
        {
            if (mDragHandler != null)
                return mDragHandler;

            mDragHandler = GetComponent<UIDragHandler>();
            if (mDragHandler == null)
                mDragHandler = gameObject.AddComponent<UIDragHandler>();
            return mDragHandler;
        }
    }
}