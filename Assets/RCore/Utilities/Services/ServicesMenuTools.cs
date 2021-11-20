#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RCore.Service
{
    public class ServicesMenuTools : Editor
    {
        [MenuItem("RUtilities/Services/Add Firebase Manager")]
        private static void AddFirebaseManager()
        {
            var manager = FindObjectOfType<RFirebase.RFirebaseManager>();
            if (manager == null)
            {
                var obj = new GameObject("RFirebaseManager");
                obj.AddComponent<RFirebase.RFirebaseManager>();
            }
        }

        [MenuItem("RUtilities/Services/Add Game Services")]
        private static void AddGameServices()
        {
            var manager = FindObjectOfType<GPGS.GameServices>();
            if (manager == null)
            {
                var obj = new GameObject("GameServices");
                obj.AddComponent<GPGS.GameServices>();
            }
        }

        //[MenuItem("RUtilities/Services/Add Ads Manager")]
        //private static void AddAdsManager()
        //{
        //    var manager = FindObjectOfType<Ads.AdsManager>();
        //    if (manager == null)
        //    {
        //        var obj = new GameObject("AdsManager");
        //        obj.AddComponent<Ads.AdsManager>();
        //    }
        //}

        [MenuItem("RUtilities/Services/Add IAP Helper")]
        private static void AddIAPHelper()
        {
            var manager = FindObjectOfType<IAPHelper>();
            if (manager == null)
            {
                var obj = new GameObject("IAPHelper");
                obj.AddComponent<IAPHelper>();
            }
        }

        //[MenuItem("RUtilities/Services/Add Local Notification Helper")]
        //private static void AddLocalNotificationHelper()
        //{
        //    var manager = FindObjectOfType<Notification.LocalNotificationHelper>();
        //    if (manager == null)
        //    {
        //        var obj = new GameObject("LocalNotificationHelper");
        //        obj.AddComponent<Notification.LocalNotificationHelper>();
        //    }
        //}
    }
}
#endif