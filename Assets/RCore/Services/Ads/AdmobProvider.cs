using System;
using UnityEngine;
#if ADMOB
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
#endif

namespace RCore.Service
{
	public class AdMobProvider : IAdProvider
	{
		private static AdMobProvider m_Instance;
		public static AdMobProvider Instance => m_Instance ??= new AdMobProvider();
		public string adUnitInterstitial;
		public string adUnitRewarded;
		public string adUnitBanner;
		public string placement;
		private IInterstitialAdListener m_interstitialAdListener;
		private IBannerAdListener m_bannerAdListener;
		private IRewardedAdListener m_rewardedAdListener;
#if ADMOB
		public void Init()
		{
			MobileAds.Initialize(_ =>
			{
				InitInterstitialAds();
				InitRewardedAds();
				InitBannerAds();
			});
		}
		public void SetEventListener(GameObject listener)
		{
			m_interstitialAdListener = listener?.GetComponent<IInterstitialAdListener>();
			m_bannerAdListener = listener?.GetComponent<IBannerAdListener>();
			m_rewardedAdListener = listener?.GetComponent<IRewardedAdListener>();
		}
		
#region Interstitial Ads

		private InterstitialAd m_interstitialAd;
		private int m_interstitialRetryAttempt;
		private bool m_interstitialInitialized;
		private Action m_onInterstitialAdCompleted;
		public AdValue interstitialValue;

		private void InitInterstitialAds()
		{
			if (string.IsNullOrEmpty(adUnitInterstitial)) return;
			m_interstitialInitialized = true;
			LoadInterstitial();
			m_interstitialAdListener?.OnInterstitialInit();
		}
		private void LoadInterstitial()
		{
			var request = new AdRequest();
			InterstitialAd.Load(adUnitInterstitial, request, (ad, error) =>
			{
				if (error != null || ad == null)
				{
					Debug.Log($"Interstitial ad failed to load: {error}");
					m_interstitialRetryAttempt++;
					// Use exponential delay with a cap (here, the delay is capped by 2^6 seconds).
					float retryDelay = (float)Math.Pow(2, Mathf.Min(6, m_interstitialRetryAttempt));
					TimerEventsGlobal.Instance.WaitForSeconds(retryDelay, (s) => LoadInterstitial());
					m_interstitialAdListener?.OnInterstitialLoadFailed();
					return;
				}
				m_interstitialRetryAttempt = 0;
				m_interstitialAd = ad;
				Debug.Log("Interstitial ad loaded.");

				m_interstitialAd.OnAdFullScreenContentOpened += InterstitialAd_OnAdFullScreenContentOpened;
				m_interstitialAd.OnAdFullScreenContentClosed += InterstitialAd_OnAdFullScreenContentClosed;
				m_interstitialAd.OnAdFullScreenContentFailed += InterstitialAd_OnAdFullScreenContentFailed;
				m_interstitialAd.OnAdClicked += InterstitialAd_OnAdClicked;
				m_interstitialAd.OnAdPaid += InterstitialAd_OnAdPaid;

				m_interstitialAdListener?.OnInterstitialLoaded();
			});
		}
		private void InterstitialAd_OnAdFullScreenContentClosed()
		{
			m_onInterstitialAdCompleted?.Invoke();
			m_onInterstitialAdCompleted = null;
			// Load the next interstitial ad.
			LoadInterstitial();
			m_interstitialAdListener?.OnInterstitialCompleted(placement);
		}
		private void InterstitialAd_OnAdFullScreenContentFailed(AdError error)
		{
			Debug.Log($"Interstitial ad failed to show: {error}");
			LoadInterstitial();
		}
		private void InterstitialAd_OnAdFullScreenContentOpened()
		{
			Debug.Log(nameof(InterstitialAd_OnAdFullScreenContentOpened));
		}
		private void InterstitialAd_OnAdClicked()
		{
			m_interstitialAdListener?.OnInterstitialClick(placement);
		}
		public void ShowInterstitial(string pPlacement, Action callback = null)
		{
			interstitialValue = null;
			placement = pPlacement;
#if UNITY_EDITOR
			callback?.Invoke();
			return;
#endif
			if (IsInterstitialReady())
			{
				m_onInterstitialAdCompleted = callback;
				m_interstitialAd.Show();
				m_interstitialAdListener?.OnInterstitialShow(true, pPlacement);
			}
			else
				m_interstitialAdListener?.OnInterstitialShow(false, pPlacement);
		}
		public bool IsInterstitialReady()
		{
#if UNITY_EDITOR
			return true;
#endif
			return m_interstitialInitialized && m_interstitialAd != null;
		}
		private void InterstitialAd_OnAdPaid(AdValue pAdValue)
		{
			interstitialValue = pAdValue;
			m_interstitialAdListener?.OnInterstitialPaid(placement);
		}

#endregion

#region Rewarded Ads

		private RewardedAd m_rewardedAd;
		private int m_rewardedRetryAttempt;
		private bool m_rewardedInitialized;
		private Action<bool> m_onRewardedAdCompleted;
		public AdValue rewardedValue;
		
		private void InitRewardedAds()
		{
			if (string.IsNullOrEmpty(adUnitRewarded)) return;
			m_rewardedInitialized = true;
			LoadRewardedAd();
			m_rewardedAdListener?.OnRewardedInit();
		}
		private void LoadRewardedAd()
		{
			var request = new AdRequest();
			RewardedAd.Load(adUnitRewarded, request, (ad, error) =>
			{
				if (error != null || ad == null)
				{
					Debug.Log($"Rewarded ad failed to load: {error}");
					m_rewardedRetryAttempt++;
					float retryDelay = (float)Math.Pow(2, Mathf.Min(6, m_rewardedRetryAttempt));
					TimerEventsGlobal.Instance.WaitForSeconds(retryDelay, (s) => LoadRewardedAd());
					m_rewardedAdListener?.OnRewardedLoadFailed();
					return;
				}
				m_rewardedRetryAttempt = 0;
				m_rewardedAd = ad;
				Debug.Log("Rewarded ad loaded.");

				m_rewardedAd.OnAdFullScreenContentOpened += RewardedAd_OnAdFullScreenContentOpened;
				m_rewardedAd.OnAdFullScreenContentClosed += RewardedAd_OnAdFullScreenContentClosed;
				m_rewardedAd.OnAdFullScreenContentFailed += RewardedAd_OnAdFullScreenContentFailed;
				m_rewardedAd.OnAdClicked += RewardedAd_OnAdClicked;
				m_rewardedAd.OnAdPaid += RewardedAd_OnAdPaid;
				m_rewardedAdListener?.OnRewardedLoaded();
			});
		}
		private void RewardedAd_OnAdFullScreenContentOpened()
		{
			Debug.Log(nameof(RewardedAd_OnAdFullScreenContentOpened));
		}
		private void RewardedAd_OnAdFullScreenContentClosed()
		{
			m_onRewardedAdCompleted?.Invoke(true);
			m_onRewardedAdCompleted = null;
			// Preload the next rewarded ad.
			LoadRewardedAd();
			m_rewardedAdListener?.OnRewardedCompleted(placement);
		}
		private void RewardedAd_OnAdFullScreenContentFailed(AdError error)
		{
			Debug.Log($"Rewarded ad failed to show: {error}");
			LoadRewardedAd();
			m_onRewardedAdCompleted?.Invoke(false);
			m_onRewardedAdCompleted = null;
		}
		private void RewardedAd_OnAdClicked()
		{
			m_rewardedAdListener?.OnRewardedClicked(placement);
		}
		public void ShowRewardedAd(string pPlacement, Action<bool> callback = null)
		{
			rewardedValue = null;
			placement = pPlacement;
#if UNITY_EDITOR
			callback?.Invoke(true);
			return;
#endif
			if (IsRewardedVideoAvailable())
			{
				m_onRewardedAdCompleted = callback;
				m_rewardedAd.Show(null);
				m_rewardedAdListener?.OnRewardedShow(true, pPlacement);
			}
			else
			{
				m_onRewardedAdCompleted?.Invoke(false);
				ShowMessage("Rewarded ads unavailable!");
				m_rewardedAdListener?.OnRewardedShow(false, pPlacement);
			}
		}
		public bool IsRewardedVideoAvailable()
		{
#if UNITY_EDITOR
			return true;
#endif
			return m_rewardedInitialized && m_rewardedAd != null;
		}
		private void RewardedAd_OnAdPaid(AdValue pAdValue)
		{
			rewardedValue = pAdValue;
			m_rewardedAdListener?.OnRewardedPaid(placement);
		}

#endregion

#region Banner Ads

		private BannerView m_bannerView;
		private bool m_bannerLoaded;
		private bool m_bannerDisplayed;
		public AdValue bannerValue;

		private void InitBannerAds()
		{
			if (string.IsNullOrEmpty(adUnitBanner)) return;
			// Create a banner. Here we use the standard Banner size and position it at the bottom.
			var adSize = AdSize.Banner;
			m_bannerView = new BannerView(adUnitBanner, adSize, AdPosition.Bottom);
			m_bannerView.OnBannerAdLoaded += Banner_OnBannerAdLoaded;
			m_bannerView.OnBannerAdLoadFailed += Banner_OnBannerAdLoadFailed;
			m_bannerView.OnAdClicked += Banner_OnAdClicked;
			m_bannerView.OnAdPaid += Banner_OnAdPaid;

			var request = new AdRequest();
			m_bannerView.LoadAd(request);
			m_bannerAdListener?.OnBannerInit();
		}
		private void Banner_OnBannerAdLoaded()
		{
			m_bannerLoaded = true;
			m_bannerAdListener?.OnBannerLoaded();
		}
		private void Banner_OnBannerAdLoadFailed(LoadAdError obj)
		{
			m_bannerLoaded = false;
			m_bannerAdListener?.OnBannerLoadFailed();
		}
		private void Banner_OnAdClicked()
		{
			m_bannerAdListener?.OnBannerClicked();
		}
		private void Banner_OnAdPaid(AdValue obj)
		{
			bannerValue = obj;
			m_bannerAdListener?.OnBannerPaid();
		}
		public bool DisplayBanner()
		{
			if (m_bannerLoaded && m_bannerView != null)
			{
				m_bannerView.Show();
				m_bannerDisplayed = true;
				m_bannerAdListener?.OnBannerDisplayed(true);
				return true;
			}
			return false;
		}
		public void HideBanner()
		{
			if (m_bannerLoaded && m_bannerView != null)
			{
				m_bannerView.Hide();
				m_bannerDisplayed = false;
				m_bannerAdListener?.OnBannerDisplayed(false);
			}
		}
		public void DestroyBanner()
		{
			if (m_bannerLoaded && m_bannerView != null)
			{
				m_bannerView.Destroy();
				m_bannerView = null;
				bannerValue = null;
				m_bannerDisplayed = false;
				m_bannerAdListener?.OnBannerDisplayed(false);
			}
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