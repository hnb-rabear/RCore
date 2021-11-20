using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RCore.Pattern.UI;

namespace RCore.Demo
{
    public class Panel1A : PanelController
    {
        [SerializeField] public Animator mAnimator;

        [ContextMenu("Validate")]
        private void Validate()
        {
            mAnimator = GetComponentInChildren<Animator>();
        }

        protected override IEnumerator IE_HideFX()
        {
            mAnimator.SetTrigger("Close");
            yield return new WaitForSeconds(0.3f);
        }

        protected override IEnumerator IE_ShowFX()
        {
            mAnimator.SetTrigger("Open");
            yield return new WaitForSeconds(0.3f);
        }
    }
}