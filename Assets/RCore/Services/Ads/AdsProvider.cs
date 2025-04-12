using GoogleMobileAds.Ump.Api;
using System;
using RCore.Inspector;
using UnityEngine;

namespace RCore.Service
{
	public class AdsProvider : MonoBehaviour
	{
		private static AdsProvider m_Instance;
		public static AdsProvider Instance
		{
			get
			{
				if (m_Instance == null)
					m_Instance = FindObjectOfType<AdsProvider>();
				if (m_Instance == null)
				{
					var gameObject = new GameObject("AdsProvider");
					m_Instance = gameObject.AddComponent<AdsProvider>();
					gameObject.hideFlags = HideFlags.DontSave;
				}
				return m_Instance;
			}
		}

		public enum AdPlatform
		{
			Applovin,
			Admob,
		}

		[Serializable]
		public struct AppLovinConfig
		{
			public string adUnitInterstitial;
			public string adUnitRewarded;
			public string adUnitBanner;
		}

		[Serializable]
		public struct AdMobConfig
		{
			public string adUnitInterstitial;
			public string adUnitRewarded;
			public string adUnitBanner;
		}

		public bool autoInit;
		public bool requestConsent = true;
		public AdPlatform adPlatform;

		[ShowIf("IsApplovinSelected")]
		public AppLovinConfig androidAppLovinCfg;
		[ShowIf("IsApplovinSelected")]
		public AppLovinConfig iosAppLovinCfg;
		[ShowIf("IsAdmobSelected")]
		public AdMobConfig androidAdMobCfg;
		[ShowIf("IsAdmobSelected")]
		public AdMobConfig iosAdMobCfg;
		
		public GameObject adEventListener;
		public Action onRewardedShowed;
		public Action<bool> onBannerDisplayed;

		public ApplovinProvider AppLovin => ApplovinProvider.Instance;
		public AdMobProvider AdMob => AdMobProvider.Instance;
		private bool IsApplovinSelected => adPlatform == AdPlatform.Applovin;
		private bool IsAdmobSelected => adPlatform == AdPlatform.Admob;

		private IAdProvider m_provider;
		private bool m_initialized;

		private void Start()
		{
			if (autoInit)
				Init();
		}
		public void Init()
		{
#if UNITY_ANDROID && ADMOB
			if (requestConsent)
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
						InitProvider();
						return;
					}

					ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
					{
						if (formError != null)
						{
							// Consent gathering failed.
							InitProvider();
							return;
						}
						// Consent has been gathered.            
						if (ConsentInformation.CanRequestAds())
							InitProvider();
					});
				});
			}
			else
				InitProvider();
#else
			InitProvider();
#endif
		}
		private void InitProvider()
		{
			if (m_initialized)
				return;
			AppLovinConfig appLovinConfig = default;
			AdMobConfig adMobConfig = default;
#if UNITY_IOS
			appLovinConfig = m_iosAppLovinCfg;
			adMobConfig = m_iosAdMobCfg;
#elif UNITY_ANDROID
			appLovinConfig = androidAppLovinCfg;
			adMobConfig = androidAdMobCfg;
#endif
			switch (adPlatform)
			{
				case AdPlatform.Applovin:
					ApplovinProvider.Instance.adUnitInterstitial = appLovinConfig.adUnitInterstitial;
					ApplovinProvider.Instance.adUnitRewarded = appLovinConfig.adUnitRewarded;
					ApplovinProvider.Instance.adUnitBanner = appLovinConfig.adUnitBanner;
					ApplovinProvider.Instance.SetEventListener(adEventListener);
					m_provider = ApplovinProvider.Instance;
					m_provider.Init();
					break;
				case AdPlatform.Admob:
					AdMobProvider.Instance.adUnitInterstitial = adMobConfig.adUnitInterstitial;
					AdMobProvider.Instance.adUnitRewarded = adMobConfig.adUnitRewarded;
					AdMobProvider.Instance.adUnitBanner = adMobConfig.adUnitBanner;
					AdMobProvider.Instance.SetEventListener(adEventListener);
					m_provider = AdMobProvider.Instance;
					m_provider.Init();
					break;
			}
			m_initialized = true;
		}
		public void ShowInterstitial(string placement, Action pCallback = null) => m_provider.ShowInterstitial(placement, pCallback);
		public bool IsInterstitialReady() => m_provider.IsInterstitialReady();
		public void ShowRewardedAd(string placement, Action<bool> pCallback = null)
		{
			m_provider.ShowRewardedAd(placement, ok =>
			{
				pCallback?.Invoke(ok);
				if (ok)
					onRewardedShowed?.Invoke();
			});
		}
		public bool IsRewardedVideoAvailable() => m_provider.IsRewardedVideoAvailable();
		public bool DisplayBanner()
		{
			bool displayed = m_provider.DisplayBanner();
			if (displayed)
				onBannerDisplayed?.Invoke(true);
			return displayed;
		}
		public void HideBanner()
		{
			m_provider.HideBanner();
		}
		public void DestroyBanner() => m_provider.DestroyBanner();
		public bool IsBannerReady() => m_provider.IsBannerReady();
		public bool IsBannerDisplayed() => m_provider != null && m_provider.IsBannerDisplayed();
	}
}