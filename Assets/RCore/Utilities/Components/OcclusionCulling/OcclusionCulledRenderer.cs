/**
 * Author RadBear - nbhung71711 @gmail.com - 2019
 **/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RCore.Components
{
    public class OcclusionCulledRenderer : MonoBehaviour
    {
        #region Members

        public enum RendererCachingScheme
        {
            Self,
            Manual,
            Children
        }

        public bool isStatic = true;
        public Renderer[] renderers;
        public RendererCachingScheme scheme;

        #endregion

        //=====================================

        #region MonoBehaviour

        private void Awake()
        {
            if (OcclusionCuller.Instance != null)
            {
                if (!isStatic)
                    OcclusionCuller.Instance.Register(this);

                if (OcclusionCuller.Instance.initialized
                    && !InsideCamera(OcclusionCuller.Instance.mainCamera))
                    MakeInvisible();
            }
        }

        private void OnDestroy()
        {
            OcclusionCuller.Instance?.UnRegister(this);
        }

#if UNITY_EDITOR
        [InspectorButton]
        private void OnValidate()
        {
            CacheRenderers();
        }
#endif

        #endregion

        //=====================================

        #region Public

        /// <summary>
        /// Enables all the renderer containers
        /// </summary>
        public void MakeVisible()
        {
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].enabled = true;
        }

        /// <summary>
        /// Disables all the renderer components
        /// </summary>
        public void MakeInvisible()
        {
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].enabled = false;
        }

        /// <summary>
        /// Returns if any of the renderers in the container array is currently visible
        /// </summary>
        /// <returns></returns>
        public bool IsVisible()
        {
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i].isVisible)
                    return true;
            return false;
        }

        public bool InsideCamera(Camera pCamera)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(pCamera);
            return InSidePlanes(planes);
        }

        public bool InSidePlanes(Plane[] planes)
        {
            bool insideCamera = false;
            for (int i = 0; i < renderers.Length; i++)
                if (GeometryUtility.TestPlanesAABB(planes, renderers[i].bounds))
                {
                    insideCamera = true;
                    break;
                }
            return insideCamera;
        }

        #endregion

        //=====================================

        #region Private

        private void CacheRenderers()
        {
            switch (scheme)
            {
                case RendererCachingScheme.Self:
                    var _renderer = GetComponent<Renderer>();
                    if (_renderer != null)
                    {
                        renderers = new Renderer[1];
                        renderers[0] = _renderer;
                    }
                    else
                    {
                        string warningMsg = gameObject.name + " has DistanceCulledRenderer component with SELF caching scheme but has no renderer component!";
                        Debug.LogWarning(warningMsg);
                    }
                    break;

                case RendererCachingScheme.Children:
                    var _renderers = GetComponentsInChildren<Renderer>();
                    if (_renderers.Length > 0)
                    {
                        renderers = new Renderer[_renderers.Length];
                        for (int i = 0; i < _renderers.Length; i++)
                            renderers[i] = _renderers[i];
                    }
                    break;

                case RendererCachingScheme.Manual:
                    break;
            }
        }

        #endregion
    }
}