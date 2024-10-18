using UnityEngine;
using RCore.Common;

namespace RCore.UI
{
    public abstract class MainMenuScrollItem : MonoBehaviour
    {
        [SerializeField] protected RectTransform m_Main;
        [SerializeField] protected Canvas m_MainCanvas;

        protected RectTransform m_RectTransform;
        protected bool m_Staged;

        public bool Showed => m_Main.gameObject.activeSelf;
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
            if (m_Staged)
            {
                Commit();
                m_Staged = false;
                enabled = false;
            }
        }

        public virtual void Hide()
        {
            m_Main.SetActive(false);
        }

        public virtual void Show()
        {
            m_Main.SetActive(true);
        }

        public abstract void Init();

        public void Stage()
        {
            m_Staged = true;
            enabled = true;
        }

        public abstract void Commit();
    }
}