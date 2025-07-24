/***
 * Author HNB-RaBear - 2017
 **/

using System;
using UnityEngine;

namespace RCore
{
    public abstract class CFX_Component : MonoBehaviour
    {
        public bool initialized;
        public Action onHidden;
        public Renderer[] renderers;
        public float lifeTime;

        private bool m_autoDeactivate;
        private float m_elapsedTime;
        private float m_customLifeTime;

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (m_autoDeactivate && m_customLifeTime > 0)
            {
                m_elapsedTime += Time.deltaTime;
                if (m_elapsedTime >= m_customLifeTime)
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

        public virtual void Play(bool p_pAutoDeactivate, float p_pCustomLifeTime = 0)
        {
            m_elapsedTime = 0;
            m_customLifeTime = p_pCustomLifeTime > 0 ? p_pCustomLifeTime : GetLifeTime();
            m_autoDeactivate = p_pAutoDeactivate;
            enabled = p_pAutoDeactivate;
        }

        public abstract void Stop();

        public abstract void Clear();

        public virtual void SetSortingOrder(int p_pValue)
        {
            if (!initialized)
                Initialize();

            if (renderers != null)
                for (int i = 0; i < renderers.Length; i++)
                    renderers[i].sortingOrder = p_pValue;
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