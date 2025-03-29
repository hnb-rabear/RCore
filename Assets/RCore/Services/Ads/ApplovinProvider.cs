#if ADMOB
using GoogleMobileAds.Ump.Api;
#endif
using System;
using UnityEngine;

namespace RCore.Service
{
	public class ApplovinProvider : IAdProvider
	{
		private static ApplovinProvider m_Instance;
		public static ApplovinProvider Instance => m_Instance ??= new ApplovinProvider();
		public string adUnitInterstitial;
		public string adUnitRewarded;
		public string adUnitBanner;
		public string placement;
		private IAdEvent m_adEvent;
#if MAX
		public void Init(IAdEvent adEvent)
		{
			m_adEvent = adEvent;
#if UNITY_ANDROID && ADMOB
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
#else
			InitAds();
#endif
			void InitAds()
			{
				MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
				{
					Debug.Log("AppLovin SDK is initialized");

					InitInterstitialAd();
					InitRewardedAd();
					InitBannerAds();
				};

				MaxSdk.InitializeSdk();
			}
		}

#region Interstitial Ad

		private int m_interstitialRetryAttempt;
		private bool m_interstitialInitialized;
		private Action m_onInterstitialAdCompleted;
		public MaxSdkBase.AdInfo lastInterstitialInfo;
		public MaxSdkBase.ErrorInfo lastInterstitialErrInfo;

		private void InitInterstitialAd()
		{
			if (string.IsNullOrEmpty(adUnitInterstitial)) return;
			// Attach callback
			MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
			MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailedEvent;
			MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayedEvent;
			MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHiddenEvent;
			MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialAdFailedToDisplayEvent;
			MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialAdClickedEvent;
			MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialAdRevenuePaidEvent;

			// Load the first interstitial
			LoadInterstitial();

			m_interstitialInitialized = true;
			m_adEvent.OnInterstitialInit();
		}
		private void LoadInterstitial()
		{
			MaxSdk.LoadInterstitial(adUnitInterstitial);
		}
		private void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			Debug.Log("Interstitial ad is ready for you to show.");
			lastInterstitialInfo = adInfo;
			m_interstitialRetryAttempt = 0;
			m_adEvent?.OnInterstitialLoaded();
		}
		private void OnInterstitialLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
		{
			lastInterstitialErrInfo = errorInfo;
			Debug.Log("Interstitial ad failed to load.");
			//AppLovin recommends that you retry with exponentially higher delays, up to a maximum delay (in this case 64 seconds)

			m_interstitialRetryAttempt++;
			var retryDelay = Mathf.Pow(2, Mathf.Min(6, m_interstitialRetryAttempt));
			TimerEventsGlobal.Instance.WaitForSeconds(retryDelay, (s) => LoadInterstitial());
			m_adEvent?.OnInterstitialLoadFailed();
		}
		private void OnInterstitialDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			lastInterstitialInfo = adInfo;
			m_onInterstitialAdCompleted?.Invoke();
			m_onInterstitialAdCompleted = null;
			m_adEvent?.OnInterstitialCompleted(placement);
		}
		private void OnInterstitialAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
		{
			Debug.Log("Interstitial ad failed to display.");
			//AppLovin recommends that you load the next ad.
			lastInterstitialInfo = adInfo;
			lastInterstitialErrInfo = errorInfo;
			LoadInterstitial();
			m_adEvent.OnInterstitialShow(false, placement);
		}
		private void OnInterstitialHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			// Interstitial ad is hidden. Pre-load the next ad.
			lastInterstitialInfo = adInfo;
			LoadInterstitial();
		}
		private void OnInterstitialAdClickedEvent(string arg1, MaxSdkBase.AdInfo arg2)
		{
			m_adEvent?.OnInterstitialClick(placement);
		}
		public void ShowInterstitial(string pPlacement = null, Action pCallback = null)
		{
			lastInterstitialInfo = null;
			lastInterstitialErrInfo = null;
			placement = pPlacement;
#if UNITY_EDITOR
			pCallback?.Invoke();
			return;
#endif
			if (IsInterstitialReady())
			{
				m_onInterstitialAdCompleted = pCallback;
				MaxSdk.ShowInterstitial(adUnitInterstitial);
				m_adEvent?.OnInterstitialShow(true, placement);
			}
			else
				m_adEvent?.OnInterstitialShow(false, placement);
		}
		public bool IsInterstitialReady()
		{
#if UNITY_EDITOR
			return true;
#endif
			return m_interstitialInitialized && MaxSdk.IsInterstitialReady(adUnitInterstitial);
		}
		private void OnInterstitialAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			lastInterstitialInfo = adInfo;
			m_adEvent?.OnInterstitialPaid(placement);
		}

#endregion

#region Rewarded Ad

		private int m_rewardedRetryAttempt;
		private bool m_rewardedInitialized;
		private Action<bool> m_onRewardedAdCompleted;
		public MaxSdkBase.Reward lastRewarded;
		public MaxSdkBase.AdInfo lastRewardedInfo;
		public MaxSdkBase.ErrorInfo lastRewardedErrInfo;

		private void InitRewardedAd()
		{
			if (string.IsNullOrEmpty(adUnitRewarded)) return;
			// Attach callback
			MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent;
			MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdLoadFailedEvent;
			MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdHiddenEvent;
			MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdFailedToDisplayEvent;
			MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;
			MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClickedEvent;
			MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaidEvent;

			// Load the first rewarded ad
			LoadRewardedAd();

			m_rewardedInitialized = true;
			m_adEvent.OnRewardedInit();
		}
		private void LoadRewardedAd()
		{
			MaxSdk.LoadRewardedAd(adUnitRewarded);
		}
		private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			Debug.Log("Rewarded ad is ready for you to show.");
			lastRewardedInfo = adInfo;
			m_rewardedRetryAttempt = 0;
			m_adEvent?.OnRewardedLoaded();
		}
		private void OnRewardedAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
		{
			lastRewardedErrInfo = errorInfo;
			Debug.Log("Rewarded ad failed to load.");
			//AppLovin recommends that you retry with exponentially higher delays, up to a maximum delay (in this case 64 seconds).

			m_rewardedRetryAttempt++;
			var retryDelay = Mathf.Pow(2, Mathf.Min(6, m_rewardedRetryAttempt));

			TimerEventsGlobal.Instance.WaitForSeconds(retryDelay, _ => LoadRewardedAd());

			m_adEvent?.OnRewardedLoadFailed();
		}
		private void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
		{
			Debug.Log("Rewarded ad failed to display.");
			//AppLovin recommends that you load the next ad.
			lastRewardedErrInfo = errorInfo;
			lastRewardedInfo = adInfo;
			LoadRewardedAd();
			m_adEvent.OnRewardedShow(false, placement);
		}
		private void OnRewardedAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			// Rewarded ad is hidden. Pre-load the next ad
			lastRewardedInfo = adInfo;
			LoadRewardedAd();
		}
		private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
		{
			// The rewarded ad displayed and the user should receive the reward.
			lastRewarded = reward;
			lastRewardedInfo = adInfo;
			m_onRewardedAdCompleted?.Invoke(true);
			m_onRewardedAdCompleted = null;
			m_adEvent?.OnRewardedCompleted(placement);
		}
		private void OnRewardedAdClickedEvent(string arg1, MaxSdkBase.AdInfo arg2)
		{
			m_adEvent?.OnRewardedClicked(placement);
		}
		public void ShowRewardedAd(string pPlacement = null, Action<bool> pCallback = null)
		{
			lastRewardedInfo = null;
			lastRewardedErrInfo = null;
			placement = pPlacement;
#if UNITY_EDITOR
			pCallback?.Invoke(true);
			return;
#endif
			if (IsRewardedVideoAvailable())
			{
				m_onRewardedAdCompleted = pCallback;
				MaxSdk.ShowRewardedAd(adUnitRewarded);
				m_adEvent?.OnRewardedShow(true, placement);
			}
			else
			{
				m_adEvent?.OnRewardedShow(false, placement);
				ShowMessage("Rewarded ads unavailable!");
			}
		}
		public bool IsRewardedVideoAvailable()
		{
#if UNITY_EDITOR
			return true;
#endif
			return m_rewardedInitialized && MaxSdk.IsRewardedAdReady(adUnitRewarded);
		}
		private void OnRewardedAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			lastRewardedInfo = adInfo;
			m_adEvent?.OnRewardedPaid(placement);
		}

#endregion

#region Banner

		private bool m_bannerLoaded;
		public MaxSdkBase.AdInfo lastBannerInfo;
		public MaxSdkBase.ErrorInfo lastBannerErrInfo;
		private bool m_firstLoad = true;
		private bool m_bannerDisplayed;

		private void InitBannerAds()
		{
			if (string.IsNullOrEmpty(adUnitBanner)) return;
			// Banners are automatically sized to 320×50 on phones and 728×90 on tablets
			// You may call the utility method MaxSdkUtils.isTablet() to help with view sizing adjustments
			MaxSdk.CreateBanner(adUnitBanner, MaxSdkBase.BannerPosition.BottomCenter);
			MaxSdk.StartBannerAutoRefresh(adUnitBanner);
			MaxSdk.SetBannerExtraParameter(adUnitBanner, "adaptive_banner", "true");
			// Set background or background color for banners to be fully functional
			MaxSdk.SetBannerBackgroundColor(adUnitBanner, Color.clear);

			MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent;
			MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdLoadFailedEvent;
			MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClickedEvent;
			MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerAdPaidEvent;

			m_adEvent.OnBannerInit();
		}
		private void OnBannerAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			if (m_firstLoad)
				MaxSdk.HideBanner(adUnitBanner);
			m_firstLoad = false;
			lastBannerInfo = adInfo;
			m_bannerLoaded = true;
			m_adEvent?.OnBannerLoaded();
		}
		private void OnBannerAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
		{
			lastBannerErrInfo = errorInfo;
			m_bannerLoaded = false;
			m_adEvent?.OnBannerLoadFailed();
		}
		private void OnBannerAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			lastBannerInfo = adInfo;
			m_adEvent.OnBannerClicked();
		}
		private void OnBannerAdPaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			lastBannerInfo = adInfo;
			m_adEvent?.OnBannerPaid();
		}
		public bool DisplayBanner()
		{
			if (!m_bannerLoaded) return false;
			MaxSdk.ShowBanner(adUnitBanner);
			m_adEvent.OnBannerShowed(true);
			m_bannerDisplayed = true;
			return true;
		}
		public void HideBanner()
		{
			if (!m_bannerLoaded) return;
			MaxSdk.HideBanner(adUnitBanner);
			m_adEvent.OnBannerShowed(false);
			m_bannerDisplayed = false;
		}
		public void DestroyBanner()
		{
			if (!m_bannerLoaded) return;
			MaxSdk.DestroyBanner(adUnitBanner);
			if (m_bannerDisplayed)
				m_adEvent.OnBannerShowed(false);
			lastBannerInfo = null;
			lastBannerErrInfo = null;
			m_bannerDisplayed = false;
		}
		public bool IsBannerReady()
		{
			return m_bannerLoaded;
		}
		public bool IsBannerDisplayed()
		{
			return m_bannerDisplayed;
		}

#endregion

		public void ShowMessage(string msg)
		{
#if UNITY_ANDROID
			// Get the current Android activity.
			var currentActivity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
			// Get the Toast class.
			AndroidJavaObject toastClass = new AndroidJavaClass("android.widget.Toast");
			// Create and show the Toast message.
			toastClass.CallStatic<AndroidJavaObject>("makeText", new object[]
				{
					currentActivity,
					msg,
					toastClass.GetStatic<int>("LENGTH_SHORT")
				}
			).Call("show", Array.Empty<object>());
#elif UNITY_IOS
			IOSControl.instance.ShowMessage(msg);
#else
			Debug.Log("ShowMessage: " + msg);
#endif
		}

#else
		public void Init(IAdEvent adEvent) { }
        public void ShowInterstitial(string pPlacement = null, Action pCallback = null) => pCallback?.Invoke();
        public bool IsInterstitialReady() => Application.platform == RuntimePlatform.WindowsEditor;
		public void ShowRewardedAd(string pPlacement = null, Action<bool> pCallback = null) => pCallback?.Invoke(Application.platform == RuntimePlatform.WindowsEditor);
		public bool IsRewardedVideoAvailable() => Application.platform == RuntimePlatform.WindowsEditor;
		public void DisplayBanner() { }
		public void HideBanner() { }
		public void DestroyBanner() { }
		public bool IsBannerReady() => false;
		public bool IsBannerDisplayed() => false;
#endif
	}
}