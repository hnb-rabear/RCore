using Cysharp.Threading.Tasks;
#if UNITY_ANDROID && GOOGLE_UPDATE
using Google.Play.AppUpdate;
#endif
using RCore.Common;
using System;
using UnityEngine;

namespace RCore.Service
{
    public static class InAppUpdate
    {
        private const int CHECK_UPDATE_INTERVAL = 14400;
        private static int LastCheckUpdate { get => PlayerPrefs.GetInt("LastCheckUpdate"); set => PlayerPrefs.SetInt("LastCheckUpdate", value); }

        public static async void CheckUpdateAsync(string newestVersion, int versionDifferent = -2)
        {
#if UNITY_EDITOR
            return;
#endif
#if UNITY_ANDROID && GOOGLE_UPDATE
            var timestampUtcNow = TimeHelper.DateTimeToUnixTimestampInt(DateTime.UtcNow);
            if (timestampUtcNow - LastCheckUpdate < CHECK_UPDATE_INTERVAL)
                return;

            var updateManager = new AppUpdateManager();
            var updateInfoOp = updateManager.GetAppUpdateInfo();
            await updateInfoOp;
            if (!updateInfoOp.IsSuccessful)
                return;

            var result = updateInfoOp.GetResult();
            if (result.UpdateAvailability != UpdateAvailability.UpdateAvailable)
                return;

            LastCheckUpdate = timestampUtcNow;

            //If after syncing remote data, user has converted to new map, but the game is still on old map, then force update
            //This case happens when user plays on multi devices, one have new version and the other have old version
            var updateOption = AppUpdateOptions.ImmediateAppUpdateOptions();

            //Force Update if user hasn't updated for at-least [versionDifferent] versions
            int comparedValue = RUtil.CompareVersion(Application.version, newestVersion);
            if (comparedValue <= versionDifferent)
            {
                updateOption = AppUpdateOptions.ImmediateAppUpdateOptions();
                LastCheckUpdate = 0;
            }

            var startUpdateRequest = updateManager.StartUpdate(result, updateOption);
            await startUpdateRequest;
            updateManager.CompleteUpdate();
#endif
        }
    }
}