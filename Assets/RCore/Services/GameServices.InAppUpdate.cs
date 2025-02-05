using Cysharp.Threading.Tasks;
#if UNITY_ANDROID && IN_APP_UPDATE
using Google.Play.AppUpdate;
#endif

namespace RCore.Service
{
    public static partial class GameServices
    {
        public static async void ShowInAppUpdate(bool forceUpdate)
        {
#if UNITY_ANDROID && IN_APP_UPDATE
            var updateManager = new AppUpdateManager();
            var updateInfoOp = updateManager.GetAppUpdateInfo();
            await updateInfoOp;
            if (!updateInfoOp.IsSuccessful)
                return;

            var result = updateInfoOp.GetResult();
            if (result.UpdateAvailability != UpdateAvailability.UpdateAvailable)
                return;

            //If after syncing remote data, user has converted to new map, but the game is still on old map, then force update
            //This case happens when user plays on multi devices, one have new version and the other have old version
            var updateOption = AppUpdateOptions.ImmediateAppUpdateOptions();

            if (forceUpdate)
                updateOption = AppUpdateOptions.ImmediateAppUpdateOptions();

            var startUpdateRequest = updateManager.StartUpdate(result, updateOption);
            await startUpdateRequest;
            updateManager.CompleteUpdate();
#endif
        }
    }
}