/***
 * Author HNB-RaBear - 2019
 **/

using UnityEngine;

namespace RCore.Misc
{
    [System.Obsolete]
    public class OcclusionCulledRenderer : MonoBehaviour
    {
        public Renderer[] renderers;

        //=====================================

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
        
        [InspectorButton]
        private void OnValidate()
        {
            CacheRenderers();
        }

        //=====================================

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

        //=====================================

        private void CacheRenderers()
        {
            renderers = gameObject.GetComponentsInChildren<Renderer>(true);
        }
    }
}