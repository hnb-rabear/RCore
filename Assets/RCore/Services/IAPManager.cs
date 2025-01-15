using System;
using System.Collections.Generic;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Security;

namespace RCore.Service
{
	public class IAPManager : MonoBehaviour, IDetailedStoreListener
	{
		private static IAPManager m_Instance;
		public static IAPManager Instance
		{
			get
			{
				if (m_Instance == null)
					m_Instance = FindObjectOfType<IAPManager>();
				if (m_Instance == null)
				{
					var gameObject = new GameObject("IAPManager");
					m_Instance = gameObject.AddComponent<IAPManager>();
					gameObject.hideFlags = HideFlags.DontSave;
				}
				return m_Instance;
			}
		}
		
		[SerializeField] private SerializableDictionary<string, ProductType> m_products;

		public static Action<Product> OnIAPSucceed;
		public static Action<Product, string> OnIAPFailed;
		private Action<bool> m_onInitialized;
		private Action<Product> m_onPurchaseDeferred;
		private Action<Product> m_onPurchaseFailed;
		private Action<Product> m_onPurchaseSucceed;
		private IStoreController m_storeController;
		private IAppleExtensions m_appleExtensions;
		private IGooglePlayStoreExtensions m_googlePlayStoreExtensions;
		private bool m_initialized;
		private CrossPlatformValidator m_validator;
		private RPlayerPrefDict<string, string> m_cacheLocalizedPrices;

#region Init

		public async void Init(Dictionary<string, ProductType> pProducts, Action<bool> pCallback)
		{
			if (m_initialized)
				return;

			try
			{
				await UnityServices.InitializeAsync(new InitializationOptions().SetEnvironmentName("production"));
			}
			catch { }

			m_cacheLocalizedPrices = new RPlayerPrefDict<string, string>("m_cacheLocalizedPrices");
			m_onInitialized = pCallback;

			var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

			foreach (var product in pProducts)
				m_products.TryAdd(product.Key, product.Value);
			
			foreach (var product in m_products)
				builder.AddProduct(product.Key, product.Value);

			if (Application.platform == RuntimePlatform.Android)
			{
				var googlePlayConfiguration = builder.Configure<IGooglePlayConfiguration>();
				googlePlayConfiguration.SetDeferredPurchaseListener(OnDeferredPurchase);
			}
			UnityPurchasing.Initialize(this, builder);

			m_initialized = true;
		}

		public void OnInitializeFailed(InitializationFailureReason error)
		{
			OnInitializeFailed(error, null);
		}

		public void OnInitializeFailed(InitializationFailureReason error, string message)
		{
			m_onInitialized?.Invoke(false);

			var errorMessage = $"Purchasing failed to initialize. Reason: {error}.";
			if (message != null)
				errorMessage += $" More details: {message}";
			Debug.Log(errorMessage);
		}

		public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
		{
			m_storeController = controller;
			if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS)
			{
				m_appleExtensions = extensions.GetExtension<IAppleExtensions>();
				m_appleExtensions.RegisterPurchaseDeferredListener(OnDeferredPurchase);
				m_appleExtensions.simulateAskToBuy = true; // Only applies to Sandbox testing
			}
			else if (Application.platform == RuntimePlatform.Android)
			{
				m_googlePlayStoreExtensions = extensions.GetExtension<IGooglePlayStoreExtensions>();
			}
#if !UNITY_EDITOR
			if (IsCurrentStoreSupportedByValidator())
				m_validator = new CrossPlatformValidator(GooglePlayTangle.Data(), AppleTangle.Data(), Application.identifier);
#endif
			m_onInitialized?.Invoke(true);
		}

#endregion

#region Purchase

		public void Purchase(string productId, Action<Product> pOnPurchaseSucceed, Action<Product> pOnPurchaseFailed, Action<Product> pOnPurchaseDeferred = null)
		{
			m_onPurchaseSucceed = pOnPurchaseSucceed;
			m_onPurchaseFailed = pOnPurchaseFailed;
			m_onPurchaseDeferred = pOnPurchaseDeferred;
			m_storeController.InitiatePurchase(productId);
		}

		public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
		{
			var product = e.purchasedProduct;

			if (m_googlePlayStoreExtensions != null && m_googlePlayStoreExtensions.IsPurchasedProductDeferred(product))
			{
				//The purchase is Deferred.
				//Therefore, we do not unlock the content or complete the transaction.
				//ProcessPurchase will be called again once the purchase is Purchased.
				return PurchaseProcessingResult.Pending;
			}

			var isPurchaseValid = IsPurchaseValid(product);
			if (isPurchaseValid)
			{
				m_onPurchaseSucceed?.Invoke(product);
				OnIAPSucceed?.Invoke(product);
			}
			else
			{
				m_onPurchaseFailed?.Invoke(product);
				OnIAPFailed?.Invoke(product, "invalid");
			}
			return PurchaseProcessingResult.Complete;
		}

		public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
		{
			m_onPurchaseFailed?.Invoke(product);
			OnIAPFailed?.Invoke(product, failureReason.ToString());

			Debug.Log($"Purchase failed - Product: '{product.definition.id}', PurchaseFailureReason: {failureReason}");
		}

		public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
		{
			m_onPurchaseFailed?.Invoke(product);
			OnIAPFailed?.Invoke(product, failureDescription.reason.ToString());

			Debug.Log($"Purchase failed - Product: '{product.definition.id}'," + $" Purchase failure reason: {failureDescription.reason}," + $" Purchase failure details: {failureDescription.message}");
		}

		public bool IsPurchasedProductDeferred(string productId)
		{
			if (m_googlePlayStoreExtensions == null)
				return false;
			var product = m_storeController.products.WithID(productId);
			return m_googlePlayStoreExtensions.IsPurchasedProductDeferred(product);
		}

		private void OnDeferredPurchase(Product product)
		{
			Debug.Log($"Purchase of {product.definition.id} is deferred");
			m_onPurchaseDeferred?.Invoke(product);
		}

		private bool IsPurchaseValid(Product product)
		{
			//If we the validator doesn't support the current store, we assume the purchase is valid
			if (IsCurrentStoreSupportedByValidator())
			{
				try
				{
					var result = m_validator.Validate(product.receipt);
					//The validator returns parsed receipts.
					LogReceipts(result);
				}
				//If the purchase is deemed invalid, the validator throws an IAPSecurityException.
				catch (IAPSecurityException reason)
				{
					Debug.Log($"Invalid receipt: {reason}");
					return false;
				}
			}

			return true;
		}

#endregion

#region Price

		public string GetProductLocalizedPrice(string productId)
		{
			if (m_storeController == null)
				return m_cacheLocalizedPrices.Contain(productId) ? m_cacheLocalizedPrices[productId] : null;
			var metadata = m_storeController.products.WithID(productId).metadata;
			if (metadata == null)
				return null;
			string result = metadata.localizedPriceString;
			m_cacheLocalizedPrices.Add(productId, result);
			return result;
		}

		public decimal GetProductLocalizedPrice(string productId, out char pFirstSymbol, out char pLastSymbol)
		{
			pFirstSymbol = ' ';
			pLastSymbol = ' ';

			var str = GetProductLocalizedPrice(productId);
			if (str == null)
				return 0;

			char firstCharacter = str[0];
			char lastCharacter = str[str.Length - 1];
			if (!char.IsNumber(firstCharacter))
				pFirstSymbol = firstCharacter;
			if (!char.IsNumber(lastCharacter))
				pLastSymbol = lastCharacter;
			return m_storeController.products.WithID(productId).metadata.localizedPrice;
		}

#endregion

		public void Restore(Action<bool, string> onRestore)
		{
			m_googlePlayStoreExtensions?.RestoreTransactions(onRestore);
			m_appleExtensions?.RestoreTransactions(onRestore);
		}

		public bool IsSubscribedTo(Product product)
		{
			// If the product doesn't have a receipt, then it wasn't purchased and the user is therefore not subscribed.
			if (product.receipt == null)
				return false;

			//The intro_json parameter is optional and is only used for the App Store to get introductory information.
			var subscriptionManager = new SubscriptionManager(product, null);

			// The SubscriptionInfo contains all of the information about the subscription.
			// Find out more: https://docs.unity3d.com/Packages/com.unity.purchasing@3.1/manual/UnityIAPSubscriptionProducts.html
			var info = subscriptionManager.getSubscriptionInfo();

			return info.isSubscribed() == Result.True;
		}

		public bool IsSubscribedTo(string productId)
		{
			var product = m_storeController.products.WithStoreSpecificID(productId);
			if (product.receipt == null)
				return false;

			var introductorySubscriptionInfo = GetIntroductoryPriceForProduct(productId);

			var subscriptionManager = new SubscriptionManager(product, introductorySubscriptionInfo);
			var info = subscriptionManager.getSubscriptionInfo();

			return info.isSubscribed() == Result.True;
		}

		public void FetchAdditionalProducts(string productId, ProductType productType)
		{
			var additionalProductsToFetch = new HashSet<ProductDefinition>
			{
				new(productId, productType)
			};

			Debug.Log($"Fetching additional products in progress");

			m_storeController.FetchAdditionalProducts(additionalProductsToFetch,
				() =>
				{
					Debug.Log($"Successfully fetched additional products");
				},
				(reason, message) =>
				{
					var errorMessage = $"Fetching additional products failed: {reason.ToString()}.";
					if (message != null)
						errorMessage += $" More details: {message}";

					Debug.LogError(errorMessage);
				});
		}

		public Product GetProduct(string productId)
		{
			return m_storeController?.products.WithID(productId);
		}

		public void RefreshAppReceipt(Action<string> onRefreshSuccess, Action<string> onRefreshFailure)
		{
			m_appleExtensions?.RefreshAppReceipt(onRefreshSuccess, onRefreshFailure);
		}

		private bool IsCurrentStoreSupportedByValidator()
		{
			//The CrossPlatform validator only supports the GooglePlayStore and Apple's App Stores.
			return IsGooglePlayStoreSelected() || IsAppleAppStoreSelected();
		}

		private bool IsGooglePlayStoreSelected()
		{
			var currentAppStore = StandardPurchasingModule.Instance().appStore;
			return currentAppStore == AppStore.GooglePlay;
		}

		private bool IsAppleAppStoreSelected()
		{
			var currentAppStore = StandardPurchasingModule.Instance().appStore;
			return currentAppStore == AppStore.AppleAppStore || currentAppStore == AppStore.MacAppStore;
		}

		private string GetIntroductoryPriceForProduct(string productId)
		{
			if (m_appleExtensions == null)
				return null;
			var introductoryPrices = m_appleExtensions.GetIntroductoryPriceDictionary();
			var subscriptionIntroductionPriceInfo = introductoryPrices[productId];

			Debug.Log($"Introductory Price Information for {productId}: {subscriptionIntroductionPriceInfo}");

			return subscriptionIntroductionPriceInfo;
		}

		private void LogReceipts(IEnumerable<IPurchaseReceipt> receipts)
		{
			Debug.Log("Receipt is valid. Contents:");
			foreach (var receipt in receipts)
				LogReceipt(receipt);
		}

		private void LogReceipt(IPurchaseReceipt receipt)
		{
			Debug.Log($"Product ID: {receipt.productID}\n" + $"Purchase Date: {receipt.purchaseDate}\n" + $"Transaction ID: {receipt.transactionID}");

			if (receipt is GooglePlayReceipt googleReceipt)
				Debug.Log($"Purchase State: {googleReceipt.purchaseState}\n" + $"Purchase Token: {googleReceipt.purchaseToken}");

			if (receipt is AppleInAppPurchaseReceipt appleReceipt)
				Debug.Log($"Original Transaction ID: {appleReceipt.originalTransactionIdentifier}\n" + $"Subscription Expiration Date: {appleReceipt.subscriptionExpirationDate}\n" + $"Cancellation Date: {appleReceipt.cancellationDate}\n" + $"Quantity: {appleReceipt.quantity}");
		}
	}
}