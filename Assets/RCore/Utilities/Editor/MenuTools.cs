/**
 * Author RadBear - Nguyen Ba Hung - nbhung71711@gmail.com 
 **/

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RCore.Common;
using RCore.Components;
using Debug = UnityEngine.Debug;

namespace RCore.Editor
{
    public class MenuTools : UnityEditor.Editor
    {
        private const string ALT = "&";
        private const string SHIFT = "#";
        private const string CTRL = "%";

        [MenuItem("RUtilities/Save Assets " + SHIFT + "_1", priority = 1)]
        private static void SaveAssets()
        {
            var objs = Selection.objects;
            if (objs != null)
                foreach (var obj in objs)
                    EditorUtility.SetDirty(obj);

            AssetDatabase.SaveAssets();
        }

        //==========================================================

        [MenuItem("RUtilities/Group Scene Objects " + ALT + "_F1", priority = 31)]
        private static void GroupSceneObjects()
        {
            var objs = Selection.gameObjects;
            if (objs.Length > 1)
            {
                var group = new GameObject();
                for (int i = 0; i < objs.Length; i++)
                {
                    objs[i].transform.SetParent(group.transform);
                }
                Selection.activeObject = group;
            }
        }

        [MenuItem("RUtilities/Ungroup Scene Objects " + ALT + "_F2", priority = 32)]
        private static void UngroupSceneObjects()
        {
            var objs = Selection.gameObjects;
            if (objs.Length > 1)
            {
                for (int i = 0; i < objs.Length; i++)
                    objs[i].transform.SetParent(null);
            }
        }

        //==========================================================

        [MenuItem("RUtilities/Run _F5", priority = 61)]
        private static void Run()
        {
            EditorApplication.isPlaying = true;
        }

        [MenuItem("RUtilities/Stop #_F5", priority = 62)]
        private static void Stop()
        {
            EditorApplication.isPlaying = false;
        }

        //[MenuItem("RUtilities/Just Crash", priority = 63)]
        //private static void JustCrash()
        //{
        //    //It used to test game behaviour if crashing happen
        //    throw new NotImplementedException();
        //}

        //==========================================================

        [MenuItem("CONTEXT/Collider/Create a child object with this collider")]
        public static void Menu_AttachBeam(MenuCommand menuCommand)
        {
            var collider = menuCommand.context as Collider;
            if (collider)
            {
                var obj = Instantiate(collider);
                obj.transform.SetParent(collider.transform);
            }
        }
    }
}