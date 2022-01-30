/**
 * Author RadBear - nbhung71711 @gmail.com - 2019
 **/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RCore.Components
{
    public class OcclusionCuller : MonoBehaviour
    {
        #region Members

        private static OcclusionCuller mInstance;
        public static OcclusionCuller Instance
        {
            get
            {
                if (mInstance == null)
                    mInstance = FindObjectOfType<OcclusionCuller>();
                return mInstance;
            }
        }

        public Camera mainCamera;
        public List<OcclusionCulledRenderer> culledRenderers = new List<OcclusionCulledRenderer>();
        public int visibleCount;
        public bool initialized;

        private Vector3 mLastCamPos;
        private Quaternion mLastCamRot;

        #endregion

        //=====================================

        #region MonoBehaviour

        private IEnumerator Start()
        {
            while (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera != null)
                    break;
                else
                    yield return null;
            }
            initialized = true;
        }

        private void OnDisable()
        {
            for (int i = 0; i < culledRenderers.Count; i++)
                culledRenderers[i].MakeVisible();
        }

#if UNITY_EDITOR
        [InspectorButton]
        private void OnValidate()
        {
            culledRenderers = new List<OcclusionCulledRenderer>();
            var targets = FindObjectsOfType<OcclusionCulledRenderer>();
            for (int i = 0; i < targets.Length; i++)
                if (targets[i].isStatic)
                    culledRenderers.Add(targets[i]);
        }
#endif

        private void LateUpdate()
        {
            if (!initialized)
                return;

            Check();
        }

        #endregion

        //=====================================

        #region Public

        public void Register(OcclusionCulledRenderer pObj)
        {
            if (!culledRenderers.Contains(pObj))
                culledRenderers.Add(pObj);
        }

        public void UnRegister(OcclusionCulledRenderer pObj)
        {
            if (culledRenderers.Contains(pObj))
                culledRenderers.Remove(pObj);
        }

        #endregion

        //=====================================

        #region Private

        private void Check()
        {
            if (mLastCamPos != mainCamera.transform.position || mLastCamRot != mainCamera.transform.rotation)
            {
                mLastCamPos = mainCamera.transform.position;
                mLastCamRot = mainCamera.transform.rotation;

                Plane[] planes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
                visibleCount = 0;
                for (int i = 0; i < culledRenderers.Count; i++)
                {
                    var obj = culledRenderers[i];
                    if (obj.InSidePlanes(planes))
                    {
                        obj.MakeVisible();
                        visibleCount++;
                    }
                    else
                        obj.MakeInvisible();
                }
            }
        }

        #endregion
    }
}