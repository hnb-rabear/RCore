using System.Collections;
using UnityEngine;
using RCore.UI;

namespace RCore.Example.UI
{
    public class Panel3 : PanelController
    {
        [SerializeField] public Animator m_animator;

        [ContextMenu("Validate")]
        private void Validate()
        {
            m_animator = GetComponentInChildren<Animator>();
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