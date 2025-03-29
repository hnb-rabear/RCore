using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.Serialization;

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
					var gameObject = new GameObject("IAPManager");
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
		public AdPlatform adPlatform;
		[ShowIf("@(adPlatform == AdPlatform.Applovin)")]
		public AppLovinConfig androidAppLovinCfg;
		[ShowIf("@(adPlatform == AdPlatform.Applovin)")]
		public AppLovinConfig iosAppLovinCfg;
		[ShowIf("@(adPlatform == AdPlatform.Admob)")]
		public AdMobConfig androidAdMobCfg;
		[ShowIf("@(adPlatform == AdPlatform.Admob)")]
		public AdMobConfig iosAdMobCfg;
		public GameObject adEventListener;
		public Action onRewardedShowed;

		public ApplovinProvider AppLovin => ApplovinProvider.Instance;
		public AdMobProvider AdMob => AdMobProvider.Instance;
		public bool IsBannerDisplayed => m_provider.IsBannerDisplayed();

		private IAdProvider m_provider;

		private void Start()
		{
			if (autoInit)
				Init();
		}
		public void Init()
		{
			Init(adEventListener != null ? adEventListener.GetComponent<IAdEvent>() : null);
		}
		public void Init(IAdEvent adEvent)
		{
			AppLovinConfig appLovinConfig;
			AdMobConfig adMobConfig;
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
					m_provider = ApplovinProvider.Instance;
					m_provider.Init(adEvent);
					break;
				case AdPlatform.Admob:
					AdMobProvider.Instance.adUnitInterstitial = adMobConfig.adUnitInterstitial;
					AdMobProvider.Instance.adUnitRewarded = adMobConfig.adUnitRewarded;
					AdMobProvider.Instance.adUnitBanner = adMobConfig.adUnitBanner;
					m_provider = AdMobProvider.Instance;
					m_provider.Init(adEvent);
					break;
			}
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
		public bool DisplayBanner() => m_provider.DisplayBanner();
		public void HideBanner() => m_provider.HideBanner();
		public void DestroyBanner() => m_provider.DestroyBanner();
		public bool IsBannerReady() => m_provider.IsBannerReady();
	}
}