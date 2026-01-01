using System;
using UnityEngine;

namespace RCore.Service
{
	/// <summary>
	/// Interface for implementing an Advertisement Provider (e.g., AdMob, AppLovin, IronSource).
	/// </summary>
	public interface IAdProvider
	{
		/// <summary>
		/// Initializes the ad provider SDK.
		/// </summary>
		void Init();
		
		/// <summary>
		/// Shows an interstitial ad.
		/// </summary>
		/// <param name="pPlacement">The placement ID for the ad.</param>
		/// <param name="pCallback">Callback executed when the ad is closed or fails to show.</param>
		void ShowInterstitial(string pPlacement = null, Action pCallback = null);
		
		/// <summary>
		/// Checks if an interstitial ad is loaded and ready to be shown.
		/// </summary>
		bool IsInterstitialReady();
		
		/// <summary>
		/// Shows a rewarded video ad.
		/// </summary>
		/// <param name="pPlacement">The placement ID for the ad.</param>
		/// <param name="pCallback">Callback executed with true if the user should be rewarded, false otherwise.</param>
		void ShowRewardedAd(string pPlacement = null, Action<bool> pCallback = null);
		
		/// <summary>
		/// Checks if a rewarded video ad is available.
		/// </summary>
		bool IsRewardedVideoAvailable();
		
		/// <summary>
		/// Displays a banner ad.
		/// </summary>
		/// <returns>True if the banner was successfully displayed.</returns>
		bool DisplayBanner() => false;
		
		/// <summary>
		/// Hides the banner ad.
		/// </summary>
		void HideBanner();
		
		/// <summary>
		/// Destroys the banner ad to free up resources.
		/// </summary>
		void DestroyBanner();
		
		/// <summary>
		/// Checks if a banner ad is loaded and ready.
		/// </summary>
		bool IsBannerReady();
		
		/// <summary>
		/// Checks if the banner ad is currently being displayed.
		/// </summary>
		bool IsBannerDisplayed();
	}

	/// <summary>
	/// Listener interface for Rewarded Ad events.
	/// </summary>
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
	
	/// <summary>
	/// Listener interface for Interstitial Ad events.
	/// </summary>
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

	/// <summary>
	/// Listener interface for Banner Ad events.
	/// </summary>
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