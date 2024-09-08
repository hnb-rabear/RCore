/***
 * Author RadBear - nbhung71711 @gmail.com - 2017
 **/

using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Components
{
    public class MyAlignment : MonoBehaviour
    {

        public virtual void Initialize()
        {

        }

        public virtual void Align()
        {

        }

        public virtual void AlignByTweener(Action onFinish, AnimationCurve pCurve = null)
        {

        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MyAlignment), true)]
    public class MyAlignmentEditor : UnityEditor.Editor
    {
        private MyAlignment mScript;

        private void OnEnable()
        {
            mScript = (MyAlignment)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Align"))
                mScript.Align();


            if (GUILayout.Button("Align By Tweener"))
            {
                if (!Application.isPlaying)
                {
                    Debug.Log("Can run only in Playing mode");
                    return;
                }
                mScript.AlignByTweener(null);
            }
        }
    }
#endif
}