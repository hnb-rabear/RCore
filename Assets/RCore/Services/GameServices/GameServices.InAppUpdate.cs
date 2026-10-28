using Cysharp.Threading.Tasks;
#if UNITY_ANDROID && IN_APP_UPDATE
using Google.Play.AppUpdate;
#endif

namespace RCore.Service
{
	public static partial class GameServices
	{
		/// <summary>
		/// Checks for and triggers a Google Play in-app update.
		/// forceUpdate true: immediate flow, a full-screen update the user must finish before playing.
		/// forceUpdate false: flexible flow, the update downloads in the background while the user plays,
		/// and installs (restarting the app) on the next call after the download finished.
		/// </summary>
		public static async void ShowInAppUpdate(bool forceUpdate)
		{
#if UNITY_EDITOR
			return;
#endif
#if UNITY_ANDROID && IN_APP_UPDATE
			var updateManager = new AppUpdateManager();
			var updateInfoOp = updateManager.GetAppUpdateInfo();
			await updateInfoOp;
			if (!updateInfoOp.IsSuccessful)
				return;

			var result = updateInfoOp.GetResult();

			// A flexible update downloaded in an earlier session: install it now
			if (result.AppUpdateStatus == AppUpdateStatus.Downloaded)
			{
				updateManager.CompleteUpdate();
				return;
			}

			// An immediate update the user left before it finished must be resumed
			bool resumeImmediate = forceUpdate && result.UpdateAvailability == UpdateAvailability.DeveloperTriggeredUpdateInProgress;
			if (result.UpdateAvailability != UpdateAvailability.UpdateAvailable && !resumeImmediate)
				return;

			var updateOption = forceUpdate
				? AppUpdateOptions.ImmediateAppUpdateOptions()
				: AppUpdateOptions.FlexibleAppUpdateOptions();
			if (!result.IsUpdateTypeAllowed(updateOption))
				return;

			await updateManager.StartUpdate(result, updateOption);
#endif
		}
	}
}