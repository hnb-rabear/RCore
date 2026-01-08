using UnityEngine;
using UnityEngine.Serialization;

namespace RCore.UI
{
    /// <summary>
    /// This is the abstract base class for any UI element intended for use within a HorizontalSnapScrollView.
    /// It provides the basic structure for showing, hiding, and refreshing its content. Developers must create a
    /// new script that inherits from this class and implement the `Refresh` method.
    /// </summary>
    public abstract class SnapScrollItem : MonoBehaviour
    {
        [Tooltip("The main GameObject/RectTransform of the item that will be shown or hidden.")]
        [SerializeField] protected RectTransform m_Main;
        [Tooltip("If true, the main object is never deactivated, even when 'hidden'. The Show/Hide methods will have no effect on its active state.")]
        [SerializeField] protected bool m_keepVisible;
        [Tooltip("If true, this script component remains enabled after its first refresh. If false, it disables itself to save performance.")]
        [SerializeField] protected bool m_keepActive;

        protected RectTransform m_rectTransform;
        protected bool m_refreshed;

        /// <summary>
        /// Gets a value indicating whether the main content of this item is currently active in the hierarchy.
        /// </summary>
        public bool Showing => m_Main.gameObject.activeSelf;
        
        /// <summary>
        /// Gets a cached reference to this item's RectTransform.
        /// </summary>
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
            if (transform.parent != null)
            {
                var scrollView = GetComponentInParent<HorizontalSnapScrollView>();
                if (scrollView != null)
                    scrollView.ValidateNextFrame();
            }
        }

        protected virtual void Update()
        {
            // If the item was marked for refresh (e.g., by OnBeginDrag),
            // call the Refresh method and then disable this component if not needed.
            if (m_refreshed)
            {
                Refresh();
                m_refreshed = false;
                enabled = m_keepActive;
            }
        }

        /// <summary>
        /// Hides the main content of the item by deactivating its GameObject, unless `m_keepVisible` is true.
        /// </summary>
        public virtual void Hide()
        {
            if(m_Main != null)
                m_Main.gameObject.SetActive(m_keepVisible);
        }

        /// <summary>
        /// Shows the main content of the item by activating its GameObject.
        /// </summary>
        public virtual void Show()
        {
            if(m_Main != null)
                m_Main.gameObject.SetActive(true);
        }

        /// <summary>
        /// This abstract method MUST be implemented by any inheriting class.
        /// Place all logic for updating the visual elements of this item (e.g., setting text,
        /// loading images) inside this method. It is called when the item needs to be updated.
        /// </summary>
        public abstract void Refresh();
        
        /// <summary>
        /// Internal method called by the parent scroll view when a drag begins.
        /// It flags the item to be refreshed on the next Update call.
        /// </summary>
        internal void OnBeginDrag()
        {
            m_refreshed = true;
            enabled = true; // Re-enable the component to allow the Update loop to run.
        }
    }
}