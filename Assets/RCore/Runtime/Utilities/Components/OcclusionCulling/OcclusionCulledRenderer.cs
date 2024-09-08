/***
 * Author RadBear - nbhung71711 @gmail.com - 2019
 **/

using UnityEngine;
using RCore.Common;

namespace RCore.Components
{
    public class OcclusionCulledRenderer : MonoBehaviour
    {
        #region Members

        public Renderer[] renderers;

        #endregion

        //=====================================

        #region MonoBehaviour

        private void OnEnable()
        {
            MakeVisible();
        }

        private void Awake()
        {
            if (OcclusionCuller.Instance != null)
                OcclusionCuller.Instance.Register(this);
        }

        private void OnDestroy()
        {
            if (OcclusionCuller.Instance != null)
                OcclusionCuller.Instance.UnRegister(this);
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
            {
                renderers[i].enabled = true;
            }
        }

        /// <summary>
        /// Disables all the renderer components
        /// </summary>
        public void MakeInvisible()
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = false;
            }
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
            var planes = GeometryUtility.CalculateFrustumPlanes(pCamera);
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
            var _renderers = gameObject.FindComponentsInChildren<Renderer>();
            if (_renderers.Count > 0)
            {
                renderers = new Renderer[_renderers.Count];
                for (int i = 0; i < _renderers.Count; i++)
                    renderers[i] = _renderers[i];
            }
        }

        #endregion
    }
}