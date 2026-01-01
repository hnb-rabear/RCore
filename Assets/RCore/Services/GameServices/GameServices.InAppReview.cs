using Cysharp.Threading.Tasks;
#if UNITY_ANDROID && IN_APP_REVIEW
using Google.Play.Review;
#endif
using UnityEngine;

namespace RCore.Service
{
    public static partial class GameServices
    {
#if UNITY_ANDROID && IN_APP_REVIEW
        private static ReviewManager m_ReviewManager;
        /// <summary>
        /// Shows the in-app review dialog.
        /// </summary>
        public static async void ShowInAppReview()
        {
            m_ReviewManager ??= new ReviewManager();
            var requestFlowOperation = m_ReviewManager.RequestReviewFlow();
            await requestFlowOperation;
            if (requestFlowOperation.Error != ReviewErrorCode.NoError)
            {
                Application.OpenURL("market://details?id=" + Application.identifier);
                UnityEngine.Debug.LogError(requestFlowOperation.Error.ToString());
                return;
            }
            var playReviewInfo = requestFlowOperation.GetResult();
            var launchFlowOperation = m_ReviewManager.LaunchReviewFlow(playReviewInfo);
            await launchFlowOperation;
            if (launchFlowOperation.Error != ReviewErrorCode.NoError)
            {
                Application.OpenURL("market://details?id=" + Application.identifier);
                UnityEngine.Debug.LogError(launchFlowOperation.Error.ToString());
            }
        }
#elif  UNITY_IOS
		public static async void ShowInAppReview()
		{
			UnityEngine.iOS.Device.RequestStoreReview();
		}
#else
		public static void ShowInAppReview()
		{
	        Application.OpenURL("market://details?id=" + Application.identifier);
		}
#endif
    }
}