using System.Collections;
using UnityEngine;
using RCore.UI;

namespace RCore.Example.UI
{
    public class Panel1 : PanelController
    {
        [SerializeField] private SimpleButton m_btnPanel1A;
        [SerializeField] private SimpleButton m_btnPanel1B;
        [SerializeField] private Panel1A m_panel1A;
        [SerializeField] private Panel1B m_panel1B;
        [SerializeField] public Animator m_animator;

        public Panel1A Panel1A => GetCachedPanel(m_panel1A);
        public Panel1B Panel1B => GetCachedPanel(m_panel1B);

        private void Start()
        {
            m_btnPanel1A.onClick.AddListener(ShowPanel1A);
            m_btnPanel1B.onClick.AddListener(ShowPanel1B);
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if (m_animator != null)
                m_animator = GetComponentInChildren<Animator>();
        }

        [ContextMenu("ShowPanel1A")]
        public void ShowPanel1A()
        {
            PushPanel(ref m_panel1A, true);
        }

        [ContextMenu("ShowPanel1B")]
        public void ShowPanel1B()
        {
            PushPanel(ref m_panel1B, true);
        }

        protected override IEnumerator IE_HideFX()
        {
            m_animator.SetTrigger("Close");
            yield return new WaitForSeconds(0.3f);
        }

        protected override IEnumerator IE_ShowFX()
        {
            m_animator.SetTrigger("Open");
            yield return new WaitForSeconds(0.3f);
        }
    }
}