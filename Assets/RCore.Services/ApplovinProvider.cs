#if MAX
using GoogleMobileAds.Ump.Api;
#endif
using System;
using UnityEngine;

namespace RCore.Service
{
    public class ApplovinProvider
    {
        private static ApplovinProvider instance;
        public static ApplovinProvider Instance => instance ??= new ApplovinProvider();
        private static string SDK_KEY => Configuration.KeyValues["MAX_SDK_KEY"];
        private static string AD_UNIT_INTERSTITIAL => Configuration.KeyValues["MAX_INTERSTITIAL"];
        private static string AD_UNIT_REWARDED => Configuration.KeyValues["MAX_REWARDED"];
        private static string AD_UNIT_BANNER => Configuration.KeyValues["MAX_BANNER"];
#if MAX
        public void Init()
        {
            // Create a ConsentRequestParameters object     
            var request = new ConsentRequestParameters();
            // Check the current consent information status
            ConsentInformation.Update(request, error =>
            {
                if (error != null)
                {
                    // Handle the error.            
                    Debug.LogError(error);
                    InitAds();
                    return;
                }

                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    if (formError != null)
                    {
                        // Consent gathering failed.
                        InitAds();
                        return;
                    }

                    // Consent has been gathered.            
                    if (ConsentInformation.CanRequestAds())
                        InitAds();
                });
            });

            void InitAds()
            {
                MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
                {
                    Debug.Log("AppLovin SDK is initialized");

                    InitializeInterstitialAds();
                    InitializeRewardedAds();
                    InitializeBannerAds();
                };

                MaxSdk.SetSdkKey(SDK_KEY);
                //MaxSdk.SetUserId("USER_ID");
                MaxSdk.InitializeSdk();
            }
        }

#region Interstitial Ads

        private int m_InterstitialRetryAttempt;
        private bool m_InterstitialInitialized;
        private Action m_OnInterstitialAdCompleted;

        private void InitializeInterstitialAds()
        {
            // Attach callback
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayedEvent;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialClickedEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHiddenEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialAdFailedToDisplayEvent;

            // Load the first interstitial
            LoadInterstitial();

            m_InterstitialInitialized = true;
        }

        private void LoadInterstitial()
        {
            MaxSdk.LoadInterstitial(AD_UNIT_INTERSTITIAL);
        }

        private void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("Interstitial ad is ready for you to show.");
            m_InterstitialRetryAttempt = 0;
        }

        private void OnInterstitialLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            Debug.Log("Interstitial ad failed to load.");
            //AppLovin recommends that you retry with exponentially higher delays, up to a maximum delay (in this case 64 seconds)

            m_InterstitialRetryAttempt++;
            var retryDelay = Mathf.Pow(2, Mathf.Min(6, m_InterstitialRetryAttempt));

            TimerEventsGlobal.Instance.WaitForSeconds(retryDelay, (s) => LoadInterstitial());
        }

        private void OnInterstitialDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            m_OnInterstitialAdCompleted?.Invoke();
        }

        private void OnInterstitialAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("Interstitial ad failed to display.");
            //AppLovin recommends that you load the next ad.
            LoadInterstitial();
        }

        private void OnInterstitialClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

        private void OnInterstitialHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Interstitial ad is hidden. Pre-load the next ad.
            LoadInterstitial();
        }

        public void ShowInterstitial(Action pCallback = null)
        {
#if UNITY_EDITOR
            pCallback?.Invoke();
#endif
            if (IsInterstitialReady())
            {
                m_OnInterstitialAdCompleted = pCallback;
                MaxSdk.ShowInterstitial(AD_UNIT_INTERSTITIAL);
            }
        }

        public bool IsInterstitialReady()
        {
#if UNITY_EDITOR
            return true;
#endif
            return m_InterstitialInitialized && MaxSdk.IsInterstitialReady(AD_UNIT_INTERSTITIAL);
        }

#endregion

#region Rewarded Ad

        private int m_RewardedRetryAttempt;
        private bool m_RewardedInitialized;
        private Action<bool> m_OnRewardedAdCompleted;

        public void InitializeRewardedAds()
        {
            // Attach callback
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdLoadFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaidEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdHiddenEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdFailedToDisplayEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;

            // Load the first rewarded ad
            LoadRewardedAd();

            m_RewardedInitialized = true;
        }

        private void LoadRewardedAd()
        {
            MaxSdk.LoadRewardedAd(AD_UNIT_REWARDED);
        }

        private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("Rewarded ad is ready for you to show.");
            m_RewardedRetryAttempt = 0;
        }

        private void OnRewardedAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            Debug.Log("Rewarded ad failed to load.");
            //AppLovin recommends that you retry with exponentially higher delays, up to a maximum delay (in this case 64 seconds).

            m_RewardedRetryAttempt++;
            var retryDelay = Mathf.Pow(2, Mathf.Min(6, m_RewardedRetryAttempt));

            TimerEventsGlobal.Instance.WaitForSeconds(retryDelay, (s) => LoadRewardedAd());
        }

        private void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

        private void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("Rewarded ad failed to display.");
            //AppLovin recommends that you load the next ad.
            LoadRewardedAd();
        }

        private void OnRewardedAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

        private void OnRewardedAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Rewarded ad is hidden. Pre-load the next ad
            LoadRewardedAd();
        }

        private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            // The rewarded ad displayed and the user should receive the reward.
            m_OnRewardedAdCompleted?.Invoke(true);
            m_OnRewardedAdCompleted = null;
        }

        private void OnRewardedAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Ad revenue paid. Use this callback to track user revenue.
        }

        public void ShowRewardedAd(Action<bool> pCallback = null)
        {
#if UNITY_EDITOR
            pCallback?.Invoke(true);
#endif
            if (IsRewardedVideoAvailable())
            {
                m_OnRewardedAdCompleted = pCallback;
                MaxSdk.ShowRewardedAd(AD_UNIT_REWARDED);
            }
        }

        public bool IsRewardedVideoAvailable()
        {
#if UNITY_EDITOR
            return true;
#endif
            return m_RewardedInitialized && MaxSdk.IsRewardedAdReady(AD_UNIT_REWARDED);
        }

#endregion

#region Banner

        private bool m_BannerInitialized;
        private bool m_BannerLoaded;

        public void InitializeBannerAds()
        {
            // Banners are automatically sized to 320×50 on phones and 728×90 on tablets
            // You may call the utility method MaxSdkUtils.isTablet() to help with view sizing adjustments
            MaxSdk.CreateBanner(AD_UNIT_BANNER, MaxSdkBase.BannerPosition.TopCenter);

            // Set background or background color for banners to be fully functional
            MaxSdk.SetBannerBackgroundColor(AD_UNIT_BANNER, Color.clear);

            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdLoadFailedEvent;
            MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClickedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerAdRevenuePaidEvent;
            MaxSdkCallbacks.Banner.OnAdExpandedEvent += OnBannerAdExpandedEvent;
            MaxSdkCallbacks.Banner.OnAdCollapsedEvent += OnBannerAdCollapsedEvent;

            m_BannerInitialized = true;
        }

        private void OnBannerAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            m_BannerLoaded = true;
        }

        private void OnBannerAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            m_BannerLoaded = false;
        }

        private void OnBannerAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

        private void OnBannerAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

        private void OnBannerAdExpandedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

        private void OnBannerAdCollapsedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

        public void DisplayBanner()
        {
            if (!m_BannerInitialized) return;
            MaxSdk.ShowBanner(AD_UNIT_BANNER);
        }

        public void HideBanner()
        {
            if (!m_BannerInitialized) return;
            MaxSdk.HideBanner(AD_UNIT_BANNER);
        }

        public void DestroyBanner()
        {
            if (!m_BannerInitialized) return;
            MaxSdk.DestroyBanner(AD_UNIT_BANNER);
        }

        public bool IsBannerReady()
        {
            return m_BannerInitialized && m_BannerLoaded;
        }

#endregion

#else
        public void Init() { }
        public void ShowInterstitial(Action pCallback = null) => pCallback?.Invoke();
        public bool IsInterstitialReady() => Application.platform == RuntimePlatform.WindowsEditor;
		public void ShowRewardedAd(Action<bool> pCallback = null) => pCallback?.Invoke(Application.platform == RuntimePlatform.WindowsEditor);
		public bool IsRewardedVideoAvailable() => Application.platform == RuntimePlatform.WindowsEditor;
		public void DisplayBanner() { }
		public void HideBanner() { }
		public void DestroyBanner() { }
		public bool IsBannerReady() => Application.platform == RuntimePlatform.WindowsEditor;
#endif
    }
}