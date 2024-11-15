using UnityEngine;

namespace RCore.UI
{
    public abstract class SnapScrollItem : MonoBehaviour
    {
        [SerializeField] protected RectTransform m_Main;
        [SerializeField] protected Canvas m_MainCanvas;

        protected RectTransform m_RectTransform;
        protected bool m_refreshed;

        public bool Showing => m_Main.gameObject.activeSelf;
        public virtual bool AlwaysEnabled => false;
        public RectTransform RectTransform
        {
            get
            {
                if (m_RectTransform == null)
                    m_RectTransform = transform as RectTransform;
                return m_RectTransform;
            }
        }

        protected virtual void Update()
        {
            if (m_refreshed)
            {
                Refresh();
                m_refreshed = false;
                enabled = AlwaysEnabled;
            }
        }

        public virtual void Hide()
        {
            m_Main.gameObject.SetActive(false);
        }

        public virtual void Show()
        {
            m_Main.gameObject.SetActive(true);
        }

        public abstract void Refresh();
        
        internal void OnBeginDrag()
        {
            m_refreshed = true;
            enabled = true;
        }
    }
}