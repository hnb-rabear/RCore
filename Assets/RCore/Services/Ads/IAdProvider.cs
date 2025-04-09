using System;
using UnityEngine;

namespace RCore.Service
{
	public interface IAdProvider
	{
		void Init();
		void ShowInterstitial(string pPlacement = null, Action pCallback = null);
		bool IsInterstitialReady();
		void ShowRewardedAd(string pPlacement = null, Action<bool> pCallback = null);
		bool IsRewardedVideoAvailable();
		bool DisplayBanner() => false;
		void HideBanner();
		void DestroyBanner();
		bool IsBannerReady();
		bool IsBannerDisplayed();
	}

	public interface IRewardedAdListener
	{
		void OnRewardedInit();
		void OnRewardedLoaded();
		void OnRewardedLoadFailed();
		void OnRewardedCompleted(string placement);
		void OnRewardedClicked(string placement);
		void OnRewardedShow(bool success, string pPlacement);
		void OnRewardedPaid(string pPlacement);
	}
	
	public interface IInterstitialAdListener
	{
		void OnInterstitialInit();
		void OnInterstitialLoaded();
		void OnInterstitialLoadFailed();
		void OnInterstitialCompleted(string placement);
		void OnInterstitialClick(string placement);
		void OnInterstitialShow(bool success, string pPlacement);
		void OnInterstitialPaid(string pPlacement);
	}

	public interface IBannerAdListener
	{
		void OnBannerInit();
		void OnBannerLoaded();
		void OnBannerLoadFailed();
		void OnBannerClicked();
		void OnBannerPaid();
		void OnBannerDisplayed(bool pSuccess);
	}
}