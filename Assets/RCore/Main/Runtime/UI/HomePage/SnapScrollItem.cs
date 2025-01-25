using UnityEngine;
using UnityEngine.Serialization;

namespace RCore.UI
{
    public abstract class SnapScrollItem : MonoBehaviour
    {
        [SerializeField] protected RectTransform m_Main;
        [SerializeField] protected bool m_keepVisible;
        [SerializeField] protected bool m_keepActive;

        protected RectTransform m_rectTransform;
        protected bool m_refreshed;

        public bool Showing => m_Main.gameObject.activeSelf;
        public RectTransform RectTransform
        {
            get
            {
                if (m_rectTransform == null)
                    m_rectTransform = transform as RectTransform;
                return m_rectTransform;
            }
        }

        protected virtual void Update()
        {
            if (m_refreshed)
            {
                Refresh();
                m_refreshed = false;
                enabled = m_keepActive;
            }
        }

        public virtual void Hide()
        {
            m_Main.gameObject.SetActive(m_keepVisible);
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