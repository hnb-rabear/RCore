/**
 * Author NBear - nbhung71711 @gmail.com - 2017
 **/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RCore.Common;

namespace RCore.Components
{
    public abstract class CFX_Component : MonoBehaviour
    {
        public bool initialized;
        public Action onHidden;
        public Renderer[] renderers;
        public float lifeTime;

        private bool mAutoDeactive;
        private float mElapsedTime;
        private float mCustomLifeTime;

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (mAutoDeactive && mCustomLifeTime > 0)
            {
                mElapsedTime += Time.deltaTime;
                if (mElapsedTime >= mCustomLifeTime)
                {
                    Clear();
                    Stop();
                    onHidden.Raise();
                    gameObject.SetActive(false);
                }
            }
            else
                enabled = false;
        }

        public virtual void Initialize()
        {
            if (initialized)
                return;

            initialized = true;

            Validate();
        }

        public virtual float GetLifeTime()
        {
            if (!initialized)
                Initialize();

            return lifeTime;
        }

        public virtual void Play(bool pAutoDeactive, float pCustomLifeTime = 0)
        {
            mElapsedTime = 0;
            mCustomLifeTime = pCustomLifeTime > 0 ? pCustomLifeTime : GetLifeTime();
            mAutoDeactive = pAutoDeactive;
            enabled = pAutoDeactive;
        }

        public abstract void Stop();

        public abstract void Clear();

        public virtual void SetSortingOrder(int pValue)
        {
            if (!initialized)
                Initialize();

            if (renderers != null)
                for (int i = 0; i < renderers.Length; i++)
                    renderers[i].sortingOrder = pValue;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            initialized = false;
            Initialize();
        }
#endif

        protected abstract void Validate();
    }
}