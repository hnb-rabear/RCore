using System;
using UnityEngine;

namespace RCore.Service
{
    public class IronSourceProvider : MonoBehaviour
    {
        private static IronSourceProvider m_Instance;
        public static IronSourceProvider Instance => m_Instance;
        public static string ANDROID_APP_KEY => Configuration.KeyValues["IS_APP_KEY_ANDROID"];
        public static string IOS_APP_KEY => Configuration.KeyValues["IS_APP_KEY_IOS"];
#if IRONSOURCE

        private Action<bool> m_OnRewardedAdCompleted;
        private Action m_OnInterstitialAdCompleted;
        private bool m_IsBannerLoaded;

        private void Awake()
        {
            if (m_Instance == null)
                m_Instance = this;
            else if (m_Instance != this)
                Destroy(gameObject);
        }

        public void Init()
        {
            // Create a ConsentRequestParameters object.        
            var request = new ConsentRequestParameters();
            // Check the current consent information status.        
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
#if UNITY_ANDROID
                string appKey = ANDROID_APP_KEY
#elif UNITY_IPHONE
				string appKey = IOS_APP_KEY
#endif
                IronSourceConfig.Instance.setClientSideCallbacks(true);

                string id = IronSource.Agent.getAdvertiserId();
                Debug.Log($"IronSource.Agent.getAdvertiserId : {id}");

                Debug.Log("IronSource.Agent.validateIntegration");
                IronSource.Agent.validateIntegration();

                Debug.Log($"unity version{IronSource.unityVersion()}");

                InitBanner();
                InitInterstitial();
                InitRewarded();

                IronSource.Agent.init(appKey);
            }
        }

        private void InitBanner()
        {
            IronSourceBannerEvents.onAdLoadedEvent += adInfo =>
            {
                m_IsBannerLoaded = true;
                Debug.Log($"IronSourceBannerEvents.onAdLoadedEvent AdInfo {adInfo}");
            };
            IronSourceBannerEvents.onAdLoadFailedEvent += error =>
            {
                m_IsBannerLoaded = false;
                Debug.Log($"IronSourceBannerEvents.onAdLoadFailedEvent Error {error}");
            };
            IronSourceBannerEvents.onAdClickedEvent += adInfo =>
            {
                Debug.Log($"IronSourceBannerEvents.onAdClickedEvent AdInfo {adInfo}");
            };
            IronSourceBannerEvents.onAdScreenPresentedEvent += adInfo =>
            {
                Debug.Log($"IronSourceBannerEvents.onAdScreenPresentedEvent AdInfo {adInfo}");
            };
            IronSourceBannerEvents.onAdScreenDismissedEvent += adInfo =>
            {
                Debug.Log($"IronSourceBannerEvents.onAdScreenDismissedEvent AdInfo {adInfo}");
            };
            IronSourceBannerEvents.onAdLeftApplicationEvent += adInfo =>
            {
                Debug.Log($"IronSourceBannerEvents.onAdLeftApplicationEvent AdInfo {adInfo}");
            };
            IronSource.Agent.loadBanner(IronSourceBannerSize.BANNER, IronSourceBannerPosition.TOP);
        }

        public void DisplayBanner()
        {
            IronSource.Agent.displayBanner();
        }

        public bool IsBannerLoaded()
        {
            return m_IsBannerLoaded;
        }

        public void HideBanner()
        {
            IronSource.Agent.hideBanner();
        }

        private void InitInterstitial()
        {
            IronSourceInterstitialEvents.onAdReadyEvent += adInfo =>
            {
                Debug.Log($"IronSourceInterstitialEvents.onAdReadyEvent AdInfo {adInfo}");
            };
            IronSourceInterstitialEvents.onAdLoadFailedEvent += ironSourceError =>
            {
                Debug.Log($"IronSourceInterstitialEvents.onAdLoadFailedEvent Error {ironSourceError}");
            };
            IronSourceInterstitialEvents.onAdOpenedEvent += adInfo =>
            {
                Debug.Log($"IronSourceInterstitialEvents.onAdOpenedEvent AdInfo {adInfo}");
            };
            IronSourceInterstitialEvents.onAdClickedEvent += adInfo =>
            {
                Debug.Log($"IronSourceInterstitialEvents.onAdClickedEvent AdInfo {adInfo}");
            };
            IronSourceInterstitialEvents.onAdShowSucceededEvent += adInfo =>
            {
                m_OnInterstitialAdCompleted?.Invoke();
                Debug.Log($"IronSourceInterstitialEvents.onAdShowSucceededEvent AdInfo {adInfo}");
            };
            IronSourceInterstitialEvents.onAdShowFailedEvent += (ironSourceError, adInfo) =>
            {
                Debug.Log($"IronSourceInterstitialEvents.onAdShowFailedEvent Error {ironSourceError} AdInfo {adInfo}");
            };
            IronSourceInterstitialEvents.onAdClosedEvent += adInfo =>
            {
                Debug.Log($"IronSourceInterstitialEvents.onAdClosedEvent AdInfo {adInfo}");
            };
        }

        public bool IsInterstitialReady()
        {
            return IronSource.Agent.isInterstitialReady();
        }

        public void ShowInterstitial(Action onAdCompleted)
        {
            m_OnInterstitialAdCompleted = onAdCompleted;
            IronSource.Agent.showInterstitial();
        }

        private void InitRewarded()
        {
            IronSourceRewardedVideoEvents.onAdOpenedEvent += adInfo =>
            {
                Debug.Log($"IronSourceRewardedVideoEvents.onAdOpenedEvent AdInfo {adInfo}");
            };
            IronSourceRewardedVideoEvents.onAdClosedEvent += adInfo =>
            {
                Debug.Log($"IronSourceRewardedVideoEvents.onAdClosedEvent AdInfo {adInfo}");
            };
            IronSourceRewardedVideoEvents.onAdAvailableEvent += adInfo =>
            {
                Debug.Log($"IronSourceRewardedVideoEvents.onAdAvailableEvent AdInfo {adInfo}");
            };
            IronSourceRewardedVideoEvents.onAdUnavailableEvent += () =>
            {
                Debug.Log("IronSourceRewardedVideoEvents.onAdUnavailableEvent");
            };
            IronSourceRewardedVideoEvents.onAdShowFailedEvent += (ironSourceError, adInfo) =>
            {
                m_OnRewardedAdCompleted?.Invoke(false);
                Debug.Log($"IronSourceRewardedVideoEvents.onAdShowFailedEvent Error{ironSourceError} AdInfo {adInfo}");
            };
            IronSourceRewardedVideoEvents.onAdRewardedEvent += (ironSourcePlacement, adInfo) =>
            {
                m_OnRewardedAdCompleted?.Invoke(true);
                Debug.Log($"IronSourceRewardedVideoEvents.onAdRewardedEvent Placement{ironSourcePlacement} AdInfo {adInfo}");
            };
            IronSourceRewardedVideoEvents.onAdClickedEvent += (ironSourcePlacement, adInfo) =>
            {
                Debug.Log($"IronSourceRewardedVideoEvents.onAdClickedEvent Placement{ironSourcePlacement} AdInfo {adInfo}");
            };
        }

        public bool IsRewardedVideoAvailable()
        {
            return IronSource.Agent.isRewardedVideoAvailable();
        }

        public void ShowRewardedAd(Action<bool> onAdCompleted)
        {
            m_OnRewardedAdCompleted = onAdCompleted;
            IronSource.Agent.showRewardedVideo();
        }
#else
    public void Init() { }
#endif
    }
}