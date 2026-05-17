using UnityEngine;

namespace RevCore.UI
{
    /// <summary>
    /// Item inside a <see cref="HorizontalSnapScrollView"/>. Each item is "snapped" to focus by the
    /// parent scroll view, and is shown/hidden based on its drag-distance from the viewport. Subclass
    /// to implement <see cref="Refresh"/> for per-item visual updates triggered when focus changes.
    /// </summary>
    public abstract class SnapScrollItem : MonoBehaviour
    {
        [SerializeField] protected RectTransform m_Main;
        [SerializeField] protected bool m_keepVisible;
        [SerializeField] protected bool m_keepActive;

        protected RectTransform m_rectTransform;
        protected bool m_refreshed;

        /// <summary>True when the main visual is currently active in the hierarchy.</summary>
        public bool Showing => m_Main != null && m_Main.gameObject.activeSelf;

        /// <summary>This component's <see cref="UnityEngine.RectTransform"/>, cached on first access.</summary>
        public RectTransform RectTransform
        {
            get
            {
                if (m_rectTransform == null)
                    m_rectTransform = transform as RectTransform;
                return m_rectTransform;
            }
        }

        protected virtual void OnEnable()
        {
            NotifyChange();
        }

        protected virtual void OnDisable()
        {
            NotifyChange();
        }

        private void NotifyChange()
        {
            if (transform.parent == null)
                return;

            var scrollView = GetComponentInParent<HorizontalSnapScrollView>();
            if (scrollView != null)
                scrollView.ValidateNextFrame();
        }

        protected virtual void Update()
        {
            if (!m_refreshed)
                return;

            Refresh();
            m_refreshed = false;
            enabled = m_keepActive;
        }

        /// <summary>Hides the main visual. Honors <c>m_keepVisible</c>: when true the main stays active even when "hidden".</summary>
        public virtual void Hide()
        {
            if (m_Main != null)
                m_Main.gameObject.SetActive(m_keepVisible);
        }

        /// <summary>Shows the main visual.</summary>
        public virtual void Show()
        {
            if (m_Main != null)
                m_Main.gameObject.SetActive(true);
        }

        /// <summary>Subclass-implemented redraw — called when the item's drag offset crosses the show/hide threshold.</summary>
        public abstract void Refresh();

        internal void OnBeginDrag()
        {
            m_refreshed = true;
            enabled = true;
        }
    }
}
