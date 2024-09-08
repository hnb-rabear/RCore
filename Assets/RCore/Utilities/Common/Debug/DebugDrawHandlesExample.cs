/***
 * Author RadBear - nbhung71711 @gmail.com - 2018
 **/

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Common
{
    public class DebugDrawHandlesExample : MonoBehaviour
    {
        public Rect rect;
        public Bounds bounds;
        public CustomProgressBar bar;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(DebugDrawHandlesExample))]
    public class DebugDrawHandlesExampleEditor : UnityEditor.Editor
    {
        DebugDrawHandlesExample mTarget;

        private void OnEnable()
        {
            mTarget = (DebugDrawHandlesExample)target;
        }

        void OnSceneGUI()
        {
            if (mTarget == null)
                return;

            mTarget.rect = DebugDrawHandles.DrawHandlesRectangleXY(mTarget.transform.position * 2, mTarget.rect, Color.red);
            mTarget.bounds = DebugDrawHandles.DrawHandlesRectangleXY(mTarget.transform.position * -2, mTarget.bounds, Color.red);
        }
    }
#endif
}