using System;
using UnityEngine;

namespace RCore.Service
{
    public class IronSourceProvider : MonoBehaviour
    {
        private static IronSourceProvider m_Instance;
        public static IronSourceProvider Instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = FindObjectOfType<IronSourceProvider>();
                return m_Instance;
            }
        }
#if IRONSOURCE
        public bool activeConsole;

        private bool m_Initialized;
        private IronSourceBannerPosition m_CurrentBannerAdPos;
        private IronSourceBannerSize m_CurrentBannerAdSize;
        private Action<bool> m_OnRewardedAdCompleted;
        private Action m_OnInterstitialAdCompleted;
        private bool m_RewardedVideoIsCompleted;
        private bool m_IsBannerLoaded;

        private void Awake()
        {
            if (m_Instance == null)
                m_Instance = this;
            else if (m_Instance != this)
                Destroy(gameObject);
        }

        public void Init(string isAppId, Action pOnCompleted = null)
        {
            m_OnInitializationCompleted = pOnCompleted;

            IronSource.Agent.validateIntegration();
            IronSource.Agent.init(isAppId);

            //Add Init Event
            IronSourceEvents.onSdkInitializationCompletedEvent += SdkInitializationCompletedEvent;

            //Add Rewarded Video Events
            IronSourceEvents.onRewardedVideoAdOpenedEvent += RewardedVideoAdOpenedEvent;
            IronSourceEvents.onRewardedVideoAdClosedEvent += RewardedVideoAdClosedEvent;
            IronSourceEvents.onRewardedVideoAvailabilityChangedEvent += RewardedVideoAvailabilityChangedEvent;
            IronSourceEvents.onRewardedVideoAdStartedEvent += RewardedVideoAdStartedEvent;
            IronSourceEvents.onRewardedVideoAdEndedEvent += RewardedVideoAdEndedEvent;
            IronSourceEvents.onRewardedVideoAdRewardedEvent += RewardedVideoAdRewardedEvent;
            IronSourceEvents.onRewardedVideoAdShowFailedEvent += RewardedVideoAdShowFailedEvent;
            IronSourceEvents.onRewardedVideoAdClickedEvent += RewardedVideoAdClickedEvent;

            //Add Rewarded Video DemandOnly Events
            IronSourceEvents.onRewardedVideoAdOpenedDemandOnlyEvent += RewardedVideoAdOpenedDemandOnlyEvent;
            IronSourceEvents.onRewardedVideoAdClosedDemandOnlyEvent += RewardedVideoAdClosedDemandOnlyEvent;
            IronSourceEvents.onRewardedVideoAdLoadedDemandOnlyEvent += RewardedVideoAdLoadedDemandOnlyEvent;
            IronSourceEvents.onRewardedVideoAdRewardedDemandOnlyEvent += RewardedVideoAdRewardedDemandOnlyEvent;
            IronSourceEvents.onRewardedVideoAdShowFailedDemandOnlyEvent += RewardedVideoAdShowFailedDemandOnlyEvent;
            IronSourceEvents.onRewardedVideoAdClickedDemandOnlyEvent += RewardedVideoAdClickedDemandOnlyEvent;
            IronSourceEvents.onRewardedVideoAdLoadFailedDemandOnlyEvent += RewardedVideoAdLoadFailedDemandOnlyEvent;

            // Add Offerwall Events
            IronSourceEvents.onOfferwallClosedEvent += OfferwallClosedEvent;
            IronSourceEvents.onOfferwallOpenedEvent += OfferwallOpenedEvent;
            IronSourceEvents.onOfferwallShowFailedEvent += OfferwallShowFailedEvent;
            IronSourceEvents.onOfferwallAdCreditedEvent += OfferwallAdCreditedEvent;
            IronSourceEvents.onGetOfferwallCreditsFailedEvent += GetOfferwallCreditsFailedEvent;
            IronSourceEvents.onOfferwallAvailableEvent += OfferwallAvailableEvent;

            // Add Interstitial Events
            IronSourceEvents.onInterstitialAdReadyEvent += InterstitialAdReadyEvent;
            IronSourceEvents.onInterstitialAdLoadFailedEvent += InterstitialAdLoadFailedEvent;
            IronSourceEvents.onInterstitialAdShowSucceededEvent += InterstitialAdShowSucceededEvent;
            IronSourceEvents.onInterstitialAdShowFailedEvent += InterstitialAdShowFailedEvent;
            IronSourceEvents.onInterstitialAdClickedEvent += InterstitialAdClickedEvent;
            IronSourceEvents.onInterstitialAdOpenedEvent += InterstitialAdOpenedEvent;
            IronSourceEvents.onInterstitialAdClosedEvent += InterstitialAdClosedEvent;

            // Add Interstitial DemandOnly Events
            IronSourceEvents.onInterstitialAdReadyDemandOnlyEvent += InterstitialAdReadyDemandOnlyEvent;
            IronSourceEvents.onInterstitialAdLoadFailedDemandOnlyEvent += InterstitialAdLoadFailedDemandOnlyEvent;
            IronSourceEvents.onInterstitialAdShowFailedDemandOnlyEvent += InterstitialAdShowFailedDemandOnlyEvent;
            IronSourceEvents.onInterstitialAdClickedDemandOnlyEvent += InterstitialAdClickedDemandOnlyEvent;
            IronSourceEvents.onInterstitialAdOpenedDemandOnlyEvent += InterstitialAdOpenedDemandOnlyEvent;
            IronSourceEvents.onInterstitialAdClosedDemandOnlyEvent += InterstitialAdClosedDemandOnlyEvent;

            // Add Banner Events
            IronSourceEvents.onBannerAdLoadedEvent += BannerAdLoadedEvent;
            IronSourceEvents.onBannerAdLoadFailedEvent += BannerAdLoadFailedEvent;
            IronSourceEvents.onBannerAdClickedEvent += BannerAdClickedEvent;
            IronSourceEvents.onBannerAdScreenPresentedEvent += BannerAdScreenPresentedEvent;
            IronSourceEvents.onBannerAdScreenDismissedEvent += BannerAdScreenDismissedEvent;
            IronSourceEvents.onBannerAdLeftApplicationEvent += BannerAdLeftApplicationEvent;

            //Add ImpressionSuccess Event
            IronSourceEvents.onImpressionSuccessEvent += ImpressionSuccessEvent;
            IronSourceEvents.onImpressionDataReadyEvent += ImpressionDataReadyEvent;
        }

        private void OnApplicationPause(bool isPaused)
        {
            IronSource.Agent.onApplicationPause(isPaused);
        }

#if DEVELOPMENT || UNITY_EDITOR
        public void OnGUI()
        {
            if (!activeConsole)
                return;

            GUI.backgroundColor = Color.blue;
            GUI.skin.button.fontSize = (int)(0.035f * Screen.width);

            Rect showRewardedVideoButton = new Rect(0.10f * Screen.width, 0.15f * Screen.height, 0.80f * Screen.width, 0.08f * Screen.height);
            if (GUI.Button(showRewardedVideoButton, "Show Rewarded Video"))
            {
                if (IronSource.Agent.isRewardedVideoAvailable())
                    IronSource.Agent.showRewardedVideo();
                else
                    Debug.Log("ISHelper: IronSource.Agent.isRewardedVideoAvailable - False");
            }

            Rect showOfferwallButton = new Rect(0.10f * Screen.width, 0.25f * Screen.height, 0.80f * Screen.width, 0.08f * Screen.height);
            if (GUI.Button(showOfferwallButton, "Show Offerwall"))
            {
                if (IronSource.Agent.isOfferwallAvailable())
                    IronSource.Agent.showOfferwall();
                else
                    Debug.Log("IronSource.Agent.isOfferwallAvailable - False");
            }

            Rect loadInterstitialButton = new Rect(0.10f * Screen.width, 0.35f * Screen.height, 0.35f * Screen.width, 0.08f * Screen.height);
            if (GUI.Button(loadInterstitialButton, "Load Interstitial"))
                IronSource.Agent.loadInterstitial();

            Rect showInterstitialButton = new Rect(0.55f * Screen.width, 0.35f * Screen.height, 0.35f * Screen.width, 0.08f * Screen.height);
            if (GUI.Button(showInterstitialButton, "Show Interstitial"))
            {
                if (IronSource.Agent.isInterstitialReady())
                    IronSource.Agent.showInterstitial();
                else
                    Debug.Log("ISHelper: IronSource.Agent.isInterstitialReady - False");
            }

            Rect loadBannerButton = new Rect(0.10f * Screen.width, 0.45f * Screen.height, 0.35f * Screen.width, 0.08f * Screen.height);
            if (GUI.Button(loadBannerButton, "Load Banner"))
                IronSource.Agent.loadBanner(IronSourceBannerSize.BANNER, IronSourceBannerPosition.BOTTOM);

            Rect destroyBannerButton = new Rect(0.55f * Screen.width, 0.45f * Screen.height, 0.35f * Screen.width, 0.08f * Screen.height);
            if (GUI.Button(destroyBannerButton, "Destroy Banner"))
                IronSource.Agent.destroyBanner();
        }
#endif

        #region Ultitlies

        public bool IsOfferwallAvailable()
        {
            return IronSource.Agent.isOfferwallAvailable();
        }

        public void DisplayBanner(string placementName, IronSourceBannerPosition position, IronSourceBannerSize size)
        {
            IronSource.Agent.displayBanner();
        }

        public void LoadBanner(string placementName, IronSourceBannerPosition position, IronSourceBannerSize size)
        {
            // If player requests a banner with different position or size,
            // we have to load a new banner.
            if (m_CurrentBannerAdPos != position)
            {
                m_CurrentBannerAdPos = position;
                m_IsBannerLoaded = false;
            }

            if (m_CurrentBannerAdSize != size)
            {
                m_CurrentBannerAdSize = size;
                m_IsBannerLoaded = false;
            }

            if (!m_IsBannerLoaded)
            {
                if (string.IsNullOrEmpty(placementName))
                    IronSource.Agent.loadBanner(size, position);
                else
                    IronSource.Agent.loadBanner(size, position, placementName);
            }
        }

        public bool IsBannerLoaded() => m_IsBannerLoaded;

        public void HideBanner()
        {
            IronSource.Agent.hideBanner();
        }

        public void DestroyBanner()
        {
            IronSource.Agent.destroyBanner();
            m_IsBannerLoaded = false;
        }

        public bool IsInterstitialReady()
        {
            return IronSource.Agent.isInterstitialReady();
        }

        public void LoadInterstitial()
        {
            IronSource.Agent.loadInterstitial();
        }

        public void ShowInterstitial(string placementName = null, Action pCallback = null)
        {
            if (!IronSource.Agent.isInterstitialReady())
                return;

            m_OnInterstitialAdCompleted = pCallback;
            if (string.IsNullOrEmpty(placementName))
                IronSource.Agent.showInterstitial();
            else
                IronSource.Agent.showInterstitial(placementName);
        }

        public bool IsRewardedVideoAvailable()
        {
            return IronSource.Agent.isRewardedVideoAvailable();
        }

        public void ShowRewardedAd(string placementName = null, Action<bool> pCallback = null)
        {
            if (!IronSource.Agent.isRewardedVideoAvailable())
                return;

            m_OnRewardedAdCompleted = pCallback;
            if (string.IsNullOrEmpty(placementName))
                IronSource.Agent.showRewardedVideo();
            else
                IronSource.Agent.showRewardedVideo(placementName);
        }

        #endregion

        #region Init callback handlers

        private Action m_OnInitializationCompleted;
        private void SdkInitializationCompletedEvent()
        {
            m_Initialized = true;

            Debug.Log("ISHelper: I got SdkInitializationCompletedEvent");

            m_OnInitializationCompleted?.Invoke();
        }

        #endregion

        #region RewardedAd callback handlers

        private void RewardedVideoAvailabilityChangedEvent(bool canShowAd)
        {
            Debug.Log("ISHelper: I got RewardedVideoAvailabilityChangedEvent, value = " + canShowAd);
        }

        private void RewardedVideoAdOpenedEvent()
        {
            Debug.Log("ISHelper: I got RewardedVideoAdOpenedEvent");
            m_RewardedVideoIsCompleted = false;
        }

        private void RewardedVideoAdRewardedEvent(IronSourcePlacement ssp)
        {
            Debug.Log("ISHelper: I got RewardedVideoAdRewardedEvent, amount = " + ssp.getRewardAmount() + " name = " + ssp.getRewardName());
            m_RewardedVideoIsCompleted = true;
        }

        private void RewardedVideoAdClosedEvent()
        {
            Debug.Log("ISHelper: I got RewardedVideoAdClosedEvent");
            if (!m_RewardedVideoIsCompleted)
                m_OnRewardedAdCompleted?.Invoke(false);
            else
                m_OnRewardedAdCompleted?.Invoke(true);
            m_OnRewardedAdCompleted = null;
            m_RewardedVideoIsCompleted = false;
        }

        private void RewardedVideoAdStartedEvent()
        {
            Debug.Log("ISHelper: I got RewardedVideoAdStartedEvent");
            m_RewardedVideoIsCompleted = false;
        }

        private void RewardedVideoAdEndedEvent()
        {
            Debug.Log("ISHelper: I got RewardedVideoAdEndedEvent");
        }

        private void RewardedVideoAdShowFailedEvent(IronSourceError error)
        {
            Debug.Log("ISHelper: I got RewardedVideoAdShowFailedEvent, code :  " + error.getCode() + ", description : " + error.getDescription());
            m_RewardedVideoIsCompleted = false;
        }

        private void RewardedVideoAdClickedEvent(IronSourcePlacement ssp)
        {
            Debug.Log("ISHelper: I got RewardedVideoAdClickedEvent, name = " + ssp.getRewardName());
        }

        /************* RewardedVideo DemandOnly Delegates *************/

        private void RewardedVideoAdLoadedDemandOnlyEvent(string instanceId)
        {

            Debug.Log("ISHelper: I got RewardedVideoAdLoadedDemandOnlyEvent for instance: " + instanceId);
        }

        private void RewardedVideoAdLoadFailedDemandOnlyEvent(string instanceId, IronSourceError error)
        {

            Debug.Log("ISHelper: I got RewardedVideoAdLoadFailedDemandOnlyEvent for instance: " + instanceId + ", code :  " + error.getCode() + ", description : " + error.getDescription());
        }

        private void RewardedVideoAdOpenedDemandOnlyEvent(string instanceId)
        {
            Debug.Log("ISHelper: I got RewardedVideoAdOpenedDemandOnlyEvent for instance: " + instanceId);
        }

        private void RewardedVideoAdRewardedDemandOnlyEvent(string instanceId)
        {
            Debug.Log("ISHelper: I got RewardedVideoAdRewardedDemandOnlyEvent for instance: " + instanceId);
        }

        private void RewardedVideoAdClosedDemandOnlyEvent(string instanceId)
        {
            Debug.Log("ISHelper: I got RewardedVideoAdClosedDemandOnlyEvent for instance: " + instanceId);
        }

        private void RewardedVideoAdShowFailedDemandOnlyEvent(string instanceId, IronSourceError error)
        {
            Debug.Log("ISHelper: I got RewardedVideoAdShowFailedDemandOnlyEvent for instance: " + instanceId + ", code :  " + error.getCode() + ", description : " + error.getDescription());
        }

        private void RewardedVideoAdClickedDemandOnlyEvent(string instanceId)
        {
            Debug.Log("ISHelper: I got RewardedVideoAdClickedDemandOnlyEvent for instance: " + instanceId);
        }


        #endregion



        #region Interstitial callback handlers

        private void InterstitialAdReadyEvent()
        {
            Debug.Log("ISHelper: I got InterstitialAdReadyEvent");
        }

        private void InterstitialAdLoadFailedEvent(IronSourceError error)
        {
            Debug.Log("ISHelper: I got InterstitialAdLoadFailedEvent, code: " + error.getCode() + ", description : " + error.getDescription());
        }

        private void InterstitialAdShowSucceededEvent()
        {
            Debug.Log("ISHelper: I got InterstitialAdShowSucceededEvent");
            m_OnInterstitialAdCompleted?.Invoke();
            m_OnInterstitialAdCompleted = null;
        }

        private void InterstitialAdShowFailedEvent(IronSourceError error)
        {
            Debug.Log("ISHelper: I got InterstitialAdShowFailedEvent, code :  " + error.getCode() + ", description : " + error.getDescription());
        }

        private void InterstitialAdClickedEvent()
        {
            Debug.Log("ISHelper: I got InterstitialAdClickedEvent");
        }

        private void InterstitialAdOpenedEvent()
        {
            Debug.Log("ISHelper: I got InterstitialAdOpenedEvent");
        }

        private void InterstitialAdClosedEvent()
        {
            Debug.Log("ISHelper: I got InterstitialAdClosedEvent");
            m_OnInterstitialAdCompleted?.Invoke();
            m_OnInterstitialAdCompleted = null;
        }

        /************* Interstitial DemandOnly Delegates *************/

        private void InterstitialAdReadyDemandOnlyEvent(string instanceId)
        {
            Debug.Log("ISHelper: I got InterstitialAdReadyDemandOnlyEvent for instance: " + instanceId);
        }

        private void InterstitialAdLoadFailedDemandOnlyEvent(string instanceId, IronSourceError error)
        {
            Debug.Log("ISHelper: I got InterstitialAdLoadFailedDemandOnlyEvent for instance: " + instanceId + ", error code: " + error.getCode() + ",error description : " + error.getDescription());
        }

        private void InterstitialAdShowFailedDemandOnlyEvent(string instanceId, IronSourceError error)
        {
            Debug.Log("ISHelper: I got InterstitialAdShowFailedDemandOnlyEvent for instance: " + instanceId + ", error code :  " + error.getCode() + ",error description : " + error.getDescription());
        }

        private void InterstitialAdClickedDemandOnlyEvent(string instanceId)
        {
            Debug.Log("ISHelper: I got InterstitialAdClickedDemandOnlyEvent for instance: " + instanceId);
        }

        private void InterstitialAdOpenedDemandOnlyEvent(string instanceId)
        {
            Debug.Log("ISHelper: I got InterstitialAdOpenedDemandOnlyEvent for instance: " + instanceId);
        }

        private void InterstitialAdClosedDemandOnlyEvent(string instanceId)
        {
            Debug.Log("ISHelper: I got InterstitialAdClosedDemandOnlyEvent for instance: " + instanceId);
        }




        #endregion

        #region Banner callback handlers

        private void BannerAdLoadedEvent()
        {
            Debug.Log("ISHelper: I got BannerAdLoadedEvent");
            m_IsBannerLoaded = true;
        }

        private void BannerAdLoadFailedEvent(IronSourceError error)
        {
            Debug.Log("ISHelper: I got BannerAdLoadFailedEvent, code: " + error.getCode() + ", description : " + error.getDescription());
            m_IsBannerLoaded = false;
        }

        private void BannerAdClickedEvent()
        {
            Debug.Log("ISHelper: I got BannerAdClickedEvent");
        }

        private void BannerAdScreenPresentedEvent()
        {
            Debug.Log("ISHelper: I got BannerAdScreenPresentedEvent");
        }

        private void BannerAdScreenDismissedEvent()
        {
            Debug.Log("ISHelper: I got BannerAdScreenDismissedEvent");
        }

        private void BannerAdLeftApplicationEvent()
        {
            Debug.Log("ISHelper: I got BannerAdLeftApplicationEvent");
        }

        #endregion



        #region Offerwall callback handlers

        private void OfferwallOpenedEvent()
        {
            Debug.Log("I got OfferwallOpenedEvent");
        }

        private void OfferwallClosedEvent()
        {
            Debug.Log("I got OfferwallClosedEvent");
        }

        private void OfferwallShowFailedEvent(IronSourceError error)
        {
            Debug.Log("I got OfferwallShowFailedEvent, code :  " + error.getCode() + ", description : " + error.getDescription());
        }

        private void OfferwallAdCreditedEvent(Dictionary<string, object> dict)
        {
            Debug.Log("I got OfferwallAdCreditedEvent, current credits = " + dict["credits"] + " totalCredits = " + dict["totalCredits"]);

        }

        private void GetOfferwallCreditsFailedEvent(IronSourceError error)
        {
            Debug.Log("I got GetOfferwallCreditsFailedEvent, code :  " + error.getCode() + ", description : " + error.getDescription());
        }

        private void OfferwallAvailableEvent(bool canShowOfferwal)
        {
            Debug.Log("I got OfferwallAvailableEvent, value = " + canShowOfferwal);

        }

        #endregion



        #region ImpressionSuccess callback handler

        private void ImpressionSuccessEvent(IronSourceImpressionData impressionData)
        {
            //com.adjust.sdk.AdjustAdRevenue adjustAdRevenue = new com.adjust.sdk.AdjustAdRevenue(com.adjust.sdk.AdjustConfig.AdjustAdRevenueSourceIronSource);
            //adjustAdRevenue.setRevenue(impressionData.revenue.Value, "USD");
            //// optional fields
            //adjustAdRevenue.setAdRevenueNetwork(impressionData.adNetwork);
            //adjustAdRevenue.setAdRevenueUnit(impressionData.adUnit);
            //adjustAdRevenue.setAdRevenuePlacement(impressionData.placement);
            //// track Adjust ad revenue
            //com.adjust.sdk.Adjust.trackAdRevenue(adjustAdRevenue);
        }

        private void ImpressionDataReadyEvent(IronSourceImpressionData impressionData)
        {
            Debug.Log("unity - script: I got ImpressionDataReadyEvent ToString(): " + impressionData.ToString());
            Debug.Log("unity - script: I got ImpressionDataReadyEvent allData: " + impressionData.allData);
        }

        #endregion
#else
        public void Init(string isAppId, Action pOnCompleted = null) { }
#endif
    }
}