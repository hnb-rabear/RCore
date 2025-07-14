#if ADMOB
using GoogleMobileAds.Ump.Api;
#endif
using System;
using System.Threading.Tasks;
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
		private IInterstitialAdListener m_interstitialAdListener;
		private IBannerAdListener m_bannerAdListener;
		private IRewardedAdListener m_rewardedAdListener;
#if MAX
		public void Init()
		{
			MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
			{
				Debug.Log("AppLovin SDK is initialized");

				InitRewardedAd();
				InitInterstitialAd();
				InitBannerAds();
			};

			MaxSdk.InitializeSdk();
		}
		public void SetEventListener(GameObject listener)
		{
			m_interstitialAdListener = listener?.GetComponent<IInterstitialAdListener>();
			m_bannerAdListener = listener?.GetComponent<IBannerAdListener>();
			m_rewardedAdListener = listener?.GetComponent<IRewardedAdListener>();
		}

#region Interstitial Ad

		private int m_interstitialRetryAttempt;
		private Action m_onInterstitialAdCompleted;
		public MaxSdkBase.AdInfo lastInterstitialInfo;
		public MaxSdkBase.ErrorInfo lastInterstitialErrInfo;

		private void InitInterstitialAd()
		{
			if (string.IsNullOrEmpty(adUnitInterstitial)) return;
			// Attach callback
			MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += InterstitialAd_OnLoaded;
			MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += InterstitialAd_OnLoadFailed;
			MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += InterstitialAd_OnDisplayed;
			MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += InterstitialAd_OnHidden;
			MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += InterstitialAd_OnFailedToDisplay;
			MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialAd_OnClicked;
			MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += InterstitialAd_OnRevenuePaid;

			// Load the first interstitial
			WaitForSeconds(1, LoadInterstitial);

			m_interstitialAdListener.OnInterstitialInit();
		}
		private void LoadInterstitial()
		{
			MaxSdk.LoadInterstitial(adUnitInterstitial);
		}
		private void InterstitialAd_OnLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			Debug.Log("Interstitial ad is ready for you to show.");
			lastInterstitialInfo = adInfo;
			m_interstitialRetryAttempt = 0;
			m_interstitialAdListener?.OnInterstitialLoaded();
		}
		private void InterstitialAd_OnLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
		{
			lastInterstitialErrInfo = errorInfo;
			Debug.Log("Interstitial ad failed to load.");
			//AppLovin recommends that you retry with exponentially higher delays, up to a maximum delay (in this case 64 seconds)

			m_interstitialRetryAttempt++;
			var retryDelay = Mathf.Pow(2, Mathf.Min(6, m_interstitialRetryAttempt));
			WaitForSeconds(retryDelay, LoadInterstitial);
			m_interstitialAdListener?.OnInterstitialLoadFailed();
		}
		private void InterstitialAd_OnDisplayed(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			lastInterstitialInfo = adInfo;
			m_onInterstitialAdCompleted?.Invoke();
			m_onInterstitialAdCompleted = null;
			m_interstitialAdListener?.OnInterstitialCompleted(placement);
		}
		private void InterstitialAd_OnFailedToDisplay(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
		{
			Debug.Log("Interstitial ad failed to display.");
			//AppLovin recommends that you load the next ad.
			lastInterstitialInfo = adInfo;
			lastInterstitialErrInfo = errorInfo;
			LoadInterstitial();
			m_interstitialAdListener.OnInterstitialShow(false, placement);
		}
		private void InterstitialAd_OnHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			// Interstitial ad is hidden. Pre-load the next ad.
			lastInterstitialInfo = adInfo;
			LoadInterstitial();
		}
		private void OnInterstitialAd_OnClicked(string arg1, MaxSdkBase.AdInfo arg2)
		{
			m_interstitialAdListener?.OnInterstitialClick(placement);
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

			}
			m_interstitialAdListener?.OnInterstitialShow(IsInterstitialReady(), placement);
		}
		public bool IsInterstitialReady()
		{
#if UNITY_EDITOR
			return true;
#endif
			return MaxSdk.IsInterstitialReady(adUnitInterstitial);
		}
		private void InterstitialAd_OnRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			lastInterstitialInfo = adInfo;
			m_interstitialAdListener?.OnInterstitialPaid(placement);
		}
		private static async void WaitForSeconds(float pSecond, Action pAction)
		{
			await Task.Delay(Mathf.RoundToInt(pSecond * 1000));
			pAction?.Invoke();
		}

#endregion

#region Rewarded Ad

		private int m_rewardedRetryAttempt;
		private Action<bool> m_onRewardedAdCompleted;
		public MaxSdkBase.Reward lastRewarded;
		public MaxSdkBase.AdInfo lastRewardedInfo;
		public MaxSdkBase.ErrorInfo lastRewardedErrInfo;

		private void InitRewardedAd()
		{
			if (string.IsNullOrEmpty(adUnitRewarded)) return;
			// Attach callback
			MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += RewardedAd_OnLoaded;
			MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += RewardedAd_OnLoadFailed;
			MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += RewardedAd_OnHidden;
			MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += RewardedAd_OnFailedToDisplay;
			MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += RewardedAd_OnReceivedReward;
			MaxSdkCallbacks.Rewarded.OnAdClickedEvent += RewardedAd_OnClicked;
			MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += RewardedAd_OnRevenuePaid;

			// Load the first rewarded ad
			WaitForSeconds(1, LoadRewardedAd);

			m_rewardedAdListener?.OnRewardedInit();
		}
		private void LoadRewardedAd()
		{
			MaxSdk.LoadRewardedAd(adUnitRewarded);
		}
		private void RewardedAd_OnLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			Debug.Log("Rewarded ad is ready for you to show.");
			lastRewardedInfo = adInfo;
			m_rewardedRetryAttempt = 0;
			m_rewardedAdListener?.OnRewardedLoaded();
		}
		private void RewardedAd_OnLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
		{
			lastRewardedErrInfo = errorInfo;
			Debug.Log("Rewarded ad failed to load.");
			//AppLovin recommends that you retry with exponentially higher delays, up to a maximum delay (in this case 64 seconds).

			m_rewardedRetryAttempt++;
			var retryDelay = Mathf.Pow(2, Mathf.Min(6, m_rewardedRetryAttempt));

			WaitForSeconds(retryDelay, LoadRewardedAd);

			m_rewardedAdListener?.OnRewardedLoadFailed();
		}
		private void RewardedAd_OnFailedToDisplay(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
		{
			Debug.Log("Rewarded ad failed to display.");
			//AppLovin recommends that you load the next ad.
			lastRewardedErrInfo = errorInfo;
			lastRewardedInfo = adInfo;
			LoadRewardedAd();
			m_rewardedAdListener?.OnRewardedShow(false, placement);
		}
		private void RewardedAd_OnHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			// Rewarded ad is hidden. Pre-load the next ad
			lastRewardedInfo = adInfo;
			LoadRewardedAd();
		}
		private void RewardedAd_OnReceivedReward(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
		{
			// The rewarded ad displayed and the user should receive the reward.
			lastRewarded = reward;
			lastRewardedInfo = adInfo;
			m_onRewardedAdCompleted?.Invoke(true);
			m_onRewardedAdCompleted = null;
			m_rewardedAdListener?.OnRewardedCompleted(placement);
		}
		private void RewardedAd_OnClicked(string arg1, MaxSdkBase.AdInfo arg2)
		{
			m_rewardedAdListener?.OnRewardedClicked(placement);
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
				m_rewardedAdListener?.OnRewardedShow(true, placement);
			}
			else
			{
				m_rewardedAdListener?.OnRewardedShow(false, placement);
				ShowMessage("Rewarded ads unavailable!");
			}
		}
		public bool IsRewardedVideoAvailable()
		{
#if UNITY_EDITOR
			return true;
#endif
			return MaxSdk.IsRewardedAdReady(adUnitRewarded);
		}
		private void RewardedAd_OnRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			lastRewardedInfo = adInfo;
			m_rewardedAdListener?.OnRewardedPaid(placement);
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

			MaxSdk.CreateBanner(adUnitBanner, MaxSdkBase.BannerPosition.BottomCenter);
			MaxSdk.StartBannerAutoRefresh(adUnitBanner);
			MaxSdk.SetBannerExtraParameter(adUnitBanner, "adaptive_banner", "true");
			MaxSdk.SetBannerBackgroundColor(adUnitBanner, Color.clear); // Set background or background color for banners to be fully functional

			MaxSdkCallbacks.Banner.OnAdLoadedEvent += BannerAd_OnLoaded;
			MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += BannerAd_OnLoadFailed;
			MaxSdkCallbacks.Banner.OnAdClickedEvent += BannerAd_OnClicked;
			MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += BannerAd_OnPaid;

			m_bannerAdListener?.OnBannerInit();
		}
		private void BannerAd_OnLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			if (m_firstLoad)
				MaxSdk.HideBanner(adUnitBanner);
			m_firstLoad = false;
			lastBannerInfo = adInfo;
			m_bannerLoaded = true;
			m_bannerAdListener?.OnBannerLoaded();
		}
		private void BannerAd_OnLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
		{
			lastBannerErrInfo = errorInfo;
			m_bannerLoaded = false;
			m_bannerAdListener?.OnBannerLoadFailed();
		}
		private void BannerAd_OnClicked(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			lastBannerInfo = adInfo;
			m_bannerAdListener?.OnBannerClicked();
		}
		private void BannerAd_OnPaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
		{
			lastBannerInfo = adInfo;
			m_bannerAdListener?.OnBannerPaid();
		}
		public bool DisplayBanner()
		{
			if (!m_bannerLoaded) return false;
			MaxSdk.ShowBanner(adUnitBanner);
			m_bannerAdListener.OnBannerDisplayed(true);
			m_bannerDisplayed = true;
			return true;
		}
		public void HideBanner()
		{
			if (!m_bannerLoaded) return;
			MaxSdk.HideBanner(adUnitBanner);
			m_bannerAdListener.OnBannerDisplayed(false);
			m_bannerDisplayed = false;
		}
		public void DestroyBanner()
		{
			if (!m_bannerLoaded) return;
			MaxSdk.DestroyBanner(adUnitBanner);
			if (m_bannerDisplayed)
				m_bannerAdListener.OnBannerDisplayed(false);
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
#else
			Debug.Log("ShowMessage: " + msg);
#endif
		}

#else
		public void Init() { }
		public void SetEventListener(GameObject listener) { }
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