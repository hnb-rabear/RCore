﻿using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Serialization;

#if UNITY_IAP
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
#endif

namespace RCore.Service
{
#if UNITY_IAP
	public class UnityPayment : MonoBehaviour, IStoreListener
#else
    public class UnityPayment : MonoBehaviour
#endif
	{
		public static UnityPayment m_Instance;
		public static UnityPayment Instance
		{
			get
			{
				if (m_Instance == null)
					m_Instance = FindObjectOfType<UnityPayment>();
				return m_Instance;
			}
		}

		private void Awake()
		{
			if (m_Instance == null)
				m_Instance = this;
			else if (m_Instance != this)
				Destroy(gameObject);
		}

#if UNITY_IAP
		public const string PROCESSING_PURCHASE_ABORT = "PROCESSING_PURCHASE_ABORT";
		public const string PROCESSING_PURCHASE_INVALID_RECEIPT = "PROCESSING_PURCHASE_INVALID_RECEIPT";
		public const string CONFIRM_PENDING_PURCHASE_FAILED = "CONFIRM_PENDING_PURCHASE_FAILED";
		public const string K_ENVIRONMENT = "production";

		public List<Product> products;
		public bool validateAppleReceipt = true;
		public AndroidStore targetAndroidStore;
		public bool validateGooglePlayReceipt = true;
		public bool interceptApplePromotionalPurchases;
		public bool simulateAppleAskToBuy;

		public Action<Product> purchaseCompleted;
		public Action<Product, string> purchaseFailed;
		public Action<Product> purchaseDeferred;

		/// <summary>
		/// [Apple store only] Occurs when a promotional purchase that comes directly
		/// from the App Store is intercepted. This event only fires if the
		/// <see cref="IAPSettings.InterceptApplePromotionalPurchases"/> setting is <c>true</c>.
		/// On non-Apple platforms this event will never fire.
		/// </summary>
		public Action<Product> promotionalPurchaseIntercepted;
		/// <summary>
		/// [Apple store only] Occurs when the (non-consumable and subscription) 
		/// purchases were restored successfully.
		/// On non-Apple platforms this event will never fire.
		/// </summary>
		public Action restoreCompleted;
		/// <summary>
		/// [Apple store only] Occurs when the purchase restoration failed.
		/// On non-Apple platforms this event will never fire.
		/// </summary>
		public Action restoreFailed;

		private Action<bool> m_OnInitialized;
		private Action<bool, string> m_OnPurchased;
		private ConfigurationBuilder m_Builder;
		private IStoreController m_StoreController;
		private IExtensionProvider m_StoreExtensionProvider;
		private IAppleExtensions m_AppleExtensions;
		private IGooglePlayStoreExtensions m_GooglePlayStoreExtensions;

		public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
		{
			// Purchasing has succeeded initializing.
			Debug.Log("In-App Purchasing OnInitialized: PASS");

			// Overall Purchasing system, configured with products for this application.
			m_StoreController = controller;

			// Store specific subsystem, for accessing device-specific store features.
			m_StoreExtensionProvider = extensions;

			// Get the store extensions for later use.
			m_AppleExtensions = m_StoreExtensionProvider.GetExtension<IAppleExtensions>();
			m_GooglePlayStoreExtensions = m_StoreExtensionProvider.GetExtension<IGooglePlayStoreExtensions>();

			// Apple store specific setup.
			if (m_AppleExtensions != null && Application.platform == RuntimePlatform.IPhonePlayer)
			{
				// Enable Ask To Buy simulation in sandbox if needed.
				m_AppleExtensions.simulateAskToBuy = simulateAppleAskToBuy;

				// Register a handler for Ask To Buy's deferred purchases.
				m_AppleExtensions.RegisterPurchaseDeferredListener(OnApplePurchaseDeferred);
			}

			m_OnInitialized?.Invoke(true);
		}

		public void OnInitializeFailed(InitializationFailureReason error)
		{
			Debug.Log("In-App Purchasing OnInitializeFailed. InitializationFailureReason:" + error);

			m_OnInitialized?.Invoke(false);
		}
		
		public void OnInitializeFailed(InitializationFailureReason error, string message)
		{
			Debug.Log($"In-App Purchasing OnInitializeFailed. InitializationFailureReason:{error}\n{message}");
			
			m_OnInitialized?.Invoke(false);
		}

		public void OnPurchaseFailed(UnityEngine.Purchasing.Product product, PurchaseFailureReason failureReason)
		{
			// A product purchase attempt did not succeed. Check failureReason for more detail. Consider sharing 
			// this reason with the user to guide their troubleshooting actions.
			Debug.Log(string.Format("Couldn't purchase product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));

			// Fire purchase failure event
			purchaseFailed?.Invoke(GetIAPProductById(product.definition.id), failureReason.ToString());
			m_OnPurchased?.Invoke(false, failureReason.ToString());
		}

		public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
		{
			Debug.Log("Processing purchase of product: " + args.purchasedProduct.transactionID);

			var pd = GetIAPProductById(args.purchasedProduct.definition.id);

			if (m_SPrePurchaseProcessDel != null)
			{
				var nextStep = m_SPrePurchaseProcessDel(args);

				if (nextStep == PrePurchaseProcessResult.Abort)
				{
					Debug.Log("Purchase aborted.");

					// Fire purchase failure event
					purchaseFailed?.Invoke(pd, PROCESSING_PURCHASE_ABORT);
					m_OnPurchased?.Invoke(false, PROCESSING_PURCHASE_ABORT);

					return PurchaseProcessingResult.Complete;
				}
				if (nextStep == PrePurchaseProcessResult.Suspend)
				{
					Debug.Log("Purchase suspended.");
					return PurchaseProcessingResult.Pending;
				}
				// Proceed.
				Debug.Log("Proceeding with purchase processing.");
			}

			bool validPurchase = true; // presume validity if not validate receipt

			if (IsReceiptValidationEnabled())
			{
				validPurchase = ValidateReceipt(args.purchasedProduct.receipt, out _);
			}

			if (validPurchase)
			{
				Debug.Log("Product purchase completed.");

				// Fire purchase success event
				purchaseCompleted?.Invoke(pd);
				m_OnPurchased?.Invoke(true, null);
			}
			else
			{
				Debug.Log("Couldn't purchase product: Invalid receipt.");

				// Fire purchase failure event
				purchaseFailed?.Invoke(pd, PROCESSING_PURCHASE_INVALID_RECEIPT);
				m_OnPurchased?.Invoke(false, PROCESSING_PURCHASE_INVALID_RECEIPT);
			}

			return PurchaseProcessingResult.Complete;
		}

		public void Init(List<Product> pProducts, Action<bool> pCallback)
		{
			products = pProducts;
			Init(pCallback);
		}

		public async void Init(Action<bool> pCallback)
		{
			if (IsInitialized())
			{
				pCallback?.Invoke(true);
				return;
			}

			m_OnInitialized = pCallback;

			var options = new InitializationOptions().SetEnvironmentName(K_ENVIRONMENT);
			var task = UnityServices.InitializeAsync(options);
			await task;

			// Create a builder, first passing in a suite of Unity provided stores.
			m_Builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

			// Add products
			foreach (var pd in products)
			{
				if (pd.storeSpecificIds != null && pd.storeSpecificIds.Length > 0)
				{
					// Add store-specific id if any
					var storeIDs = new IDs();

					foreach (var sId in pd.storeSpecificIds)
					{
						storeIDs.Add(sId.id, new string[] { GetStoreName(sId.store) });
					}

					// Add product with store-specific ids
					m_Builder.AddProduct(pd.id, GetProductType(pd.type), storeIDs);
				}
				else
				{
					// Add product using store-independent id
					m_Builder.AddProduct(pd.id, GetProductType(pd.type));
				}
			}

			// Intercepting Apple promotional purchases if needed.
			if (Application.platform == RuntimePlatform.IPhonePlayer)
			{
				if (interceptApplePromotionalPurchases)
					m_Builder.Configure<IAppleConfiguration>().SetApplePromotionalPurchaseInterceptorCallback(OnApplePromotionalPurchase);
			}

			// Kick off the remainder of the set-up with an asynchrounous call, passing the configuration 
			// and this class'  Expect a response either in OnInitialized or OnInitializeFailed.
			UnityPurchasing.Initialize(this, m_Builder);
		}

		/// <summary>
		/// Determines whether UnityIAP is initialized. All further actions like purchasing
		/// or restoring can only be done if UnityIAP is initialized.
		/// </summary>
		/// <returns><c>true</c> if initialized; otherwise, <c>false</c>.</returns>
		public bool IsInitialized()
		{
			// Only say we are initialized if both the Purchasing references are set.
			return m_StoreController != null && m_StoreExtensionProvider != null;
		}

		/// <summary>
		/// Purchases the product with specified ID.
		/// </summary>
		public void Purchase(string productId, Action<bool, string> pCallback)
		{
#if UNITY_EDITOR
			pCallback?.Invoke(true, null);
			return;
#endif

			if (IsInitialized())
			{
				m_OnPurchased = pCallback;
				var product = m_StoreController.products.WithID(productId);

				if (product != null && product.availableToPurchase)
				{
					Debug.Log("Purchasing product asychronously: " + product.definition.id);

					// Buy the product, expect a response either through ProcessPurchase or OnPurchaseFailed asynchronously.
					m_StoreController.InitiatePurchase(product);
				}
				else
				{
					Debug.Log("IAP purchasing failed: product not found or not available for purchase.");
				}
			}
			else
			{
				string log = "IAP purchasing failed: In-App Purchasing is not initialized.";
				pCallback?.Invoke(false, log);
				Debug.Log(log);
			}
		}

		/// <summary>
		/// Restore purchases previously made by this customer. Some platforms automatically restore purchases, like Google Play.
		/// Apple currently requires explicit purchase restoration for IAP, conditionally displaying a password prompt.
		/// This method only has effect on iOS and MacOSX apps.
		/// </summary>
		public void RestorePurchases()
		{
			if (!IsInitialized())
			{
				Debug.Log("Couldn't restore IAP purchases: In-App Purchasing is not initialized.");
				return;
			}

			if (Application.platform == RuntimePlatform.IPhonePlayer ||
				Application.platform == RuntimePlatform.OSXPlayer)
			{
				// Fetch the Apple store-specific subsystem.
				var apple = m_StoreExtensionProvider.GetExtension<IAppleExtensions>();

				// Begin the asynchronous process of restoring purchases. Expect a confirmation response in 
				// the Action<bool> below, and ProcessPurchase if there are previously purchased products to restore.
				apple.RestoreTransactions(result =>
				{
					// The first phase of restoration. If no more responses are received on ProcessPurchase then 
					// no purchases are available to be restored.
					Debug.Log("Restoring IAP purchases result: " + result);

					if (result)
					{
						// Fire restore complete event.
						restoreCompleted?.Invoke();
					}
					else
					{
						// Fire event failed event.
						restoreFailed?.Invoke();
					}
				});
			}
			else
			{
				// We are not running on an Apple device. No work is necessary to restore purchases.
				Debug.Log("Couldn't restore IAP purchases: not supported on platform " + Application.platform.ToString());
			}
		}

		/// <summary>
		/// Determines whether the product with the specified id is owned.
		/// A product is consider owned if it has a receipt. If receipt validation
		/// is enabled, it is also required that this receipt passes the validation check.
		/// Note that this method is mostly useful with non-consumable products.
		/// Consumable products' receipts are not persisted between app restarts,
		/// therefore their ownership only pertains in the session they're purchased.
		/// In the case of subscription products, this method only checks if a product has been purchased before,
		/// it doesn't check if the subscription has been expired or canceled. 
		/// </summary>
		/// <returns><c>true</c> if the product has a receipt and that receipt is valid (if receipt validation is enabled); otherwise, <c>false</c>.</returns>
		public bool IsProductWithIdOwned(string productId)
		{
			var iapProduct = GetIAPProductById(productId);
			return IsProductOwned(iapProduct);
		}

		/// <summary>
		/// Determines whether the product is owned.
		/// A product is consider owned if it has a receipt. If receipt validation
		/// is enabled, it is also required that this receipt passes the validation check.
		/// Note that this method is mostly useful with non-consumable products.
		/// Consumable products' receipts are not persisted between app restarts,
		/// therefore their ownership only pertains in the session they're purchased.
		/// In the case of subscription products, this method only checks if a product has been purchased before,
		/// it doesn't check if the subscription has been expired or canceled. 
		/// </summary>
		/// <returns><c>true</c> if the product has a receipt and that receipt is valid (if receipt validation is enabled); otherwise, <c>false</c>.</returns>
		public bool IsProductOwned(Product product)
		{
			if (!IsInitialized())
				return false;

			if (product == null)
				return false;

			var pd = m_StoreController.products.WithID(product.id);

			if (pd.hasReceipt)
			{
				bool isValid = true; // presume validity if not validate receipt.

				if (IsReceiptValidationEnabled())
				{
					isValid = ValidateReceipt(pd.receipt, out _);
				}

				return isValid;
			}
			return false;
		}

		/// <summary>
		/// Fetches a new Apple App receipt from their servers.
		/// Note that this will prompt the user for their password.
		/// </summary>
		public void RefreshAppleAppReceipt(Action<string> successCallback, Action errorCallback)
		{
			if (Application.platform != RuntimePlatform.IPhonePlayer)
			{
				Debug.Log("Refreshing Apple app receipt is only available on iOS.");
				return;
			}

			if (!IsInitialized())
			{
				Debug.Log("Couldn't refresh Apple app receipt: In-App Purchasing is not initialized.");
				return;
			}

			m_StoreExtensionProvider.GetExtension<IAppleExtensions>()
				.RefreshAppReceipt(
					receipt =>
					{
						// This handler is invoked if the request is successful.
						// Receipt will be the latest app receipt.
						successCallback?.Invoke(receipt);
					},
					() =>
					{
						// This handler will be invoked if the request fails,
						// such as if the network is unavailable or the user
						// enters the wrong password.
						errorCallback?.Invoke();
					});
		}

		/// <summary>
		/// Enables or disables the Apple's Ask-To-Buy simulation in the sandbox app store.
		/// Call this after the module has been initialized to toggle the simulation, regardless
		/// of the <see cref="IAPSettings.SimulateAppleAskToBuy"/> setting.
		/// </summary>
		public void SetSimulateAppleAskToBuy(bool shouldSimulate)
		{
			if (m_AppleExtensions != null)
				m_AppleExtensions.simulateAskToBuy = shouldSimulate;
		}

		/// <summary>
		/// Continues the Apple promotional purchases. Call this inside the handler of
		/// the <see cref="promotionalPurchaseIntercepted"/> event to continue the
		/// intercepted purchase.
		/// </summary>
		public void ContinueApplePromotionalPurchases()
		{
			m_AppleExtensions?.ContinuePromotionalPurchases(); // iOS and tvOS only; does nothing on Mac
		}

		/// <summary>
		/// Sets the Apple store promotion visibility for the specified product on the current device.
		/// Call this inside the handler of the <see cref="InitializeSucceeded"/> event to set
		/// the visibility for a promotional product on Apple App Store.
		/// On non-Apple platforms this method is a no-op.
		/// </summary>
		public void SetAppleStorePromotionVisibilityWithId(string pProductId, bool visible)
		{
			var iapProduct = GetIAPProductById(pProductId);

			if (iapProduct == null)
			{
				Debug.Log("Couldn't set promotion visibility: not found product with id: " + pProductId);
				return;
			}

			SetAppleStorePromotionVisibility(iapProduct, visible);
		}

		/// <summary>
		/// Sets the Apple store promotion visibility for the specified product on the current device.
		/// Call this inside the handler of the <see cref="InitializeSucceeded"/> event to set
		/// the visibility for a promotional product on Apple App Store.
		/// On non-Apple platforms this method is a no-op.
		/// </summary>
		public void SetAppleStorePromotionVisibility(Product product, bool visible)
		{
			if (!IsInitialized())
			{
				Debug.Log("Couldn't set promotion visibility: In-App Purchasing is not initialized.");
				return;
			}

			if (product == null)
				return;

			var prod = m_StoreController.products.WithID(product.id);

			m_AppleExtensions?.SetStorePromotionVisibility(prod,
				visible ? AppleStorePromotionVisibility.Show : AppleStorePromotionVisibility.Hide);
		}

		/// <summary>
		/// Sets the Apple store promotion order on the current device.
		/// Call this inside the handler of the <see cref="InitializeSucceeded"/> event to set
		/// the order for your promotional products on Apple App Store.
		/// On non-Apple platforms this method is a no-op.
		/// </summary>
		public void SetAppleStorePromotionOrder(List<Product> pProducts)
		{
			if (!IsInitialized())
			{
				Debug.Log("Couldn't set promotion order: In-App Purchasing is not initialized.");
				return;
			}

			var items = new List<UnityEngine.Purchasing.Product>();

			for (int i = 0; i < pProducts.Count; i++)
			{
				if (pProducts[i] != null)
					items.Add(m_StoreController.products.WithID(pProducts[i].id));
			}

			m_AppleExtensions?.SetStorePromotionOrder(items);
		}

		/// <summary>
		/// Gets the IAP product declared in module settings with the specified identifier.
		/// </summary>
		/// <returns>The IAP product.</returns>
		public Product GetIAPProductById(string productId)
		{
			foreach (var pd in products)
			{
				if (pd.id.Equals(productId))
					return pd;
			}

			return null;
		}

#region Module-Enable-Only Methods

		/// <summary>
		/// Gets the product registered with UnityIAP stores by its id. This method returns
		/// a Product object, which contains more information than an IAPProduct
		/// object, whose main purpose is for displaying.
		/// </summary>
		/// <returns>The product.</returns>
		public UnityEngine.Purchasing.Product GetProductWithId(string productId)
		{
			var iapProduct = GetIAPProductById(productId);

			if (iapProduct == null)
			{
				Debug.Log("Couldn't get product: not found product with id: " + productId);
				return null;
			}

			return GetProduct(iapProduct);
		}

		/// <summary>
		/// Gets the product registered with UnityIAP stores. This method returns
		/// a Product object, which contains more information than an IAPProduct
		/// object, whose main purpose is for displaying.
		/// </summary>
		/// <returns>The product.</returns>
		public UnityEngine.Purchasing.Product GetProduct(Product product)
		{
			if (!IsInitialized())
			{
				Debug.Log("Couldn't get product: In-App Purchasing is not initialized.");
				return null;
			}

			if (product == null)
			{
				return null;
			}

			return m_StoreController.products.WithID(product.id);
		}

		/// <summary>
		/// Gets the product localized data provided by the stores.
		/// </summary>
		/// <returns>The product localized data.</returns>
		public ProductMetadata GetProductLocalizedData(string pProductId)
		{
			if (!IsInitialized())
			{
				Debug.Log("Couldn't get product localized data: In-App Purchasing is not initialized.");
				return null;
			}

			return m_StoreController.products.WithID(pProductId).metadata;
		}

		public string GetProductLocalizedPrice(string productId)
		{
			var metadata = m_StoreController.products.WithID(productId).metadata;
			return metadata?.localizedPriceString;
		}

		/// <summary>
		/// Gets the parsed Apple InAppPurchase receipt for the specified product.
		/// This method only works if receipt validation is enabled.
		/// </summary>
		/// <returns>The Apple In App Purchase receipt.</returns>
		public AppleInAppPurchaseReceipt GetAppleIAPReceiptWithId(string productId)
		{
			if (Application.platform == RuntimePlatform.IPhonePlayer)
			{
				return GetPurchaseReceiptWithId(productId) as AppleInAppPurchaseReceipt;
			}
			Debug.Log("Getting Apple IAP receipt is only available on iOS.");
			return null;
		}

		/// <summary>
		/// Gets the parsed Apple InAppPurchase receipt for the specified product.
		/// This method only works if receipt validation is enabled.
		/// </summary>
		/// <returns>The Apple In App Purchase receipt.</returns>
		public AppleInAppPurchaseReceipt GetAppleIAPReceipt(Product product)
		{
			if (Application.platform == RuntimePlatform.IPhonePlayer)
			{
				return GetPurchaseReceipt(product) as AppleInAppPurchaseReceipt;
			}
			Debug.Log("Getting Apple IAP receipt is only available on iOS.");
			return null;
		}

		/// <summary>
		/// Gets the parsed Google Play receipt for the specified product.
		/// This method only works if receipt validation is enabled.
		/// </summary>
		/// <returns>The Google Play receipt.</returns>
		public GooglePlayReceipt GetGooglePlayReceiptWithId(string productId)
		{
			if (Application.platform == RuntimePlatform.Android)
			{
				return GetPurchaseReceiptWithId(productId) as GooglePlayReceipt;
			}
			Debug.Log("Getting Google Play receipt is only available on Android.");
			return null;
		}

		/// <summary>
		/// Gets the parsed Google Play receipt for the specified product.
		/// This method only works if receipt validation is enabled.
		/// </summary>
		/// <returns>The Google Play receipt.</returns>
		public GooglePlayReceipt GetGooglePlayReceipt(Product product)
		{
			if (Application.platform == RuntimePlatform.Android)
			{
				return GetPurchaseReceipt(product) as GooglePlayReceipt;
			}
			Debug.Log("Getting Google Play receipt is only available on Android.");
			return null;
		}

		/// <summary>
		/// Gets the parsed purchase receipt for the product.
		/// This method only works if receipt validation is enabled.
		/// </summary>
		/// <returns>The purchase receipt.</returns>
		public IPurchaseReceipt GetPurchaseReceiptWithId(string produtId)
		{
			var iapProduct = GetIAPProductById(produtId);

			if (iapProduct == null)
			{
				Debug.Log("Couldn't get purchase receipt: not found product with id: " + produtId);
				return null;
			}

			return GetPurchaseReceipt(iapProduct);
		}

		/// <summary>
		/// Gets the parsed purchase receipt for the product.
		/// This method only works if receipt validation is enabled.
		/// </summary>
		/// <returns>The purchase receipt.</returns>
		public IPurchaseReceipt GetPurchaseReceipt(Product product)
		{
			if (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer)
			{
				Debug.Log("Getting purchase receipt is only available on Android and iOS.");
				return null;
			}

			if (!IsInitialized())
			{
				Debug.Log("Couldn't get purchase receipt: In-App Purchasing is not initialized.");
				return null;
			}

			if (product == null)
			{
				Debug.Log("Couldn't get purchase receipt: product is null");
				return null;
			}

			var pd = m_StoreController.products.WithID(product.id);

			if (!pd.hasReceipt)
			{
				Debug.Log("Couldn't get purchase receipt: this product doesn't have a receipt.");
				return null;
			}

			if (!IsReceiptValidationEnabled())
			{
				Debug.Log("Couldn't get purchase receipt: please enable receipt validation.");
				return null;
			}

			IPurchaseReceipt[] purchaseReceipts;
			if (!ValidateReceipt(pd.receipt, out purchaseReceipts))
			{
				Debug.Log("Couldn't get purchase receipt: the receipt of this product is invalid.");
				return null;
			}

			foreach (var r in purchaseReceipts)
			{
				if (r.productID.Equals(pd.definition.storeSpecificId))
					return r;
			}

			// If we reach here, there's no receipt with the matching productID
			return null;
		}

		/// <summary>
		/// Gets the Apple App receipt. This method only works if receipt validation is enabled.
		/// </summary>
		/// <returns>The Apple App receipt.</returns>
		public AppleReceipt GetAppleAppReceipt()
		{
			if (!IsInitialized())
			{
				Debug.Log("Couldn't get Apple app receipt: In-App Purchasing is not initialized.");
				return null;
			}

			if (!validateAppleReceipt)
			{
				Debug.Log("Couldn't get Apple app receipt: Please enable Apple receipt validation.");
				return null;
			}

			// Note that the code is disabled in the editor for it to not stop the EM editor code (due to ClassNotFound error)
			// from recreating the dummy AppleTangle class if they were inadvertently removed.
#if UNITY_IOS && !UNITY_EDITOR
            // Get a reference to IAppleConfiguration during IAP initialization.
            var appleConfig = sBuilder.Configure<IAppleConfiguration>();
            var receiptData = System.Convert.FromBase64String(appleConfig.appReceipt);
            AppleReceipt receipt = new AppleValidator(AppleTangle.Data()).Validate(receiptData);
            return receipt;
#else
			Debug.Log("Getting Apple app receipt is only available on iOS.");
			return null;
#endif
		}

		/// <summary>
		/// Gets the subscription info of the product using the SubscriptionManager class,
		/// which currently supports the Apple store and Google Play store.
		/// </summary>
		/// <returns>The subscription info.</returns>
		public SubscriptionInfo GetSubscriptionInfoWithId(string productId)
		{
			var iapProduct = GetIAPProductById(productId);

			if (iapProduct == null)
			{
				Debug.Log("Couldn't get subscription info: not found product with id: " + productId);
				return null;
			}
			return GetSubscriptionInfo(iapProduct);
		}

		/// <summary>
		/// Gets the subscription info of the product using the SubscriptionManager class,
		/// which currently supports the Apple store and Google Play store.
		/// </summary>
		/// <returns>The subscription info.</returns>
		public SubscriptionInfo GetSubscriptionInfo(Product product)
		{
			if (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer)
			{
				Debug.Log("Getting subscription info is only available on Android and iOS.");
				return null;
			}

			if (!IsInitialized())
			{
				Debug.Log("Couldn't get subscripton info: In-App Purchasing is not initialized.");
				return null;
			}

			if (product == null)
			{
				Debug.Log("Couldn't get subscription info: product is null");
				return null;
			}

			var prod = m_StoreController.products.WithID(product.id);

			if (prod.definition.type != ProductType.Subscription)
			{
				Debug.Log("Couldn't get subscription info: this product is not a subscription product.");
				return null;
			}

			if (string.IsNullOrEmpty(prod.receipt))
			{
				Debug.Log("Couldn't get subscription info: this product doesn't have a valid receipt.");
				return null;
			}

			// Now actually get the subscription info using SubscriptionManager class.
			Dictionary<string, string> introPriceDict = null;

			if (m_AppleExtensions != null)
				introPriceDict = m_AppleExtensions.GetIntroductoryPriceDictionary();

			string introJson = introPriceDict == null || !introPriceDict.ContainsKey(prod.definition.storeSpecificId) ?
				null : introPriceDict[prod.definition.storeSpecificId];

			var p = new SubscriptionManager(prod, introJson);
			return p.getSubscriptionInfo();
		}

		/// <summary>
		/// Gets the name of the store.
		/// </summary>
		/// <returns>The store name.</returns>
		public string GetStoreName(Store store)
		{
			switch (store)
			{
				case Store.GooglePlay:
					return GooglePlay.Name;
				case Store.AmazonAppStore:
					return AmazonApps.Name;
				case Store.MacAppStore:
					return MacAppStore.Name;
				case Store.AppleAppStore:
					return AppleAppStore.Name;
				case Store.WinRT:
					return WindowsStore.Name;
				default:
					return string.Empty;
			}
		}

		/// <summary>
		/// Gets the type of the product.
		/// </summary>
		/// <returns>The product type.</returns>
		public ProductType GetProductType(Product.Type pType)
		{
			switch (pType)
			{
				case Product.Type.Consumable:
					return ProductType.Consumable;
				case Product.Type.NonConsumable:
					return ProductType.NonConsumable;
				case Product.Type.Subscription:
					return ProductType.Subscription;
				default:
					return ProductType.Consumable;
			}
		}

		/// <summary>
		/// Converts to UnityIAP AndroidStore.
		/// </summary>
		/// <returns>The android store.</returns>
		public UnityEngine.Purchasing.AndroidStore GetAndroidStore(AndroidStore store)
		{
			switch (store)
			{
				case AndroidStore.AmazonAppStore:
					return UnityEngine.Purchasing.AndroidStore.AmazonAppStore;
				case AndroidStore.GooglePlay:
					return UnityEngine.Purchasing.AndroidStore.GooglePlay;
				case AndroidStore.NotSpecified:
					return UnityEngine.Purchasing.AndroidStore.NotSpecified;
				default:
					return UnityEngine.Purchasing.AndroidStore.NotSpecified;
			}
		}

		/// <summary>
		/// Converts to UnityIAP AppStore.
		/// </summary>
		/// <returns>The app store.</returns>
		public AppStore GetAppStore(AndroidStore store)
		{
			switch (store)
			{
				case AndroidStore.AmazonAppStore:
					return AppStore.AmazonAppStore;
				case AndroidStore.GooglePlay:
					return AppStore.GooglePlay;
				case AndroidStore.NotSpecified:
					return AppStore.NotSpecified;
				default:
					return AppStore.NotSpecified;
			}
		}

		/// <summary>
		/// Confirms a pending purchase. Use this if you register a <see cref="PrePurchaseProcessing"/>
		/// delegate and return a <see cref="PrePurchaseProcessResult.Suspend"/> in it so that UnityIAP
		/// won't inform the app of the purchase again. After confirming the purchase, either <see cref="purchaseCompleted"/>
		/// or <see cref="purchaseFailed"/> event will be called according to the input given by the caller.
		/// </summary>
		public void ConfirmPendingPurchase(UnityEngine.Purchasing.Product product, bool purchaseSuccess)
		{
			m_StoreController?.ConfirmPendingPurchase(product);

			if (purchaseSuccess)
			{
				purchaseCompleted?.Invoke(GetIAPProductById(product.definition.id));
			}
			else
			{
				purchaseFailed?.Invoke(GetIAPProductById(product.definition.id), CONFIRM_PENDING_PURCHASE_FAILED);
			}
		}

#endregion

#region PrePurchaseProcessing

		/// <summary>
		/// Available results for the <see cref="PrePurchaseProcessing"/> delegate.
		/// </summary>
		public enum PrePurchaseProcessResult
		{
			/// <summary>
			/// Continue the normal purchase processing.
			/// </summary>
			Proceed,
			/// <summary>
			/// Suspend the purchase, PurchaseProcessingResult.Pending will be returned to UnityIAP.
			/// </summary>
			Suspend,
			/// <summary>
			/// Abort the purchase, PurchaseFailed event will be called.
			/// </summary>
			Abort
		}

		/// <summary>
		/// Once registered, this delegate will be invoked before the normal purchase processing method.
		/// The return value of this delegate determines how the purchase processing will be done.
		/// If you want to intervene in the purchase processing step, e.g. adding custom receipt validation,
		/// this delegate is the place to go.
		/// </summary>
		public delegate PrePurchaseProcessResult PrePurchaseProcessing(PurchaseEventArgs args);

		private PrePurchaseProcessing m_SPrePurchaseProcessDel;

		/// <summary>
		/// Registers a <see cref="PrePurchaseProcessing"/> delegate.
		/// </summary>
		public void RegisterPrePurchaseProcessDelegate(PrePurchaseProcessing del)
		{
			m_SPrePurchaseProcessDel = del;
		}

#endregion

#region Private Stuff

		/// <summary>
		/// Raises the purchase deferred event.
		/// Apple store only.
		/// </summary>
		private void OnApplePurchaseDeferred(UnityEngine.Purchasing.Product product)
		{
			Debug.Log("Purchase deferred: " + product.definition.id);

			purchaseDeferred?.Invoke(GetIAPProductById(product.definition.id));
		}

		/// <summary>
		/// Raises the Apple promotional purchase event.
		/// Apple store only.
		/// </summary>
		private void OnApplePromotionalPurchase(UnityEngine.Purchasing.Product product)
		{
			Debug.Log("Attempted promotional purchase: " + product.definition.id);

			promotionalPurchaseIntercepted?.Invoke(GetIAPProductById(product.definition.id));
		}

		/// <summary>
		/// Determines if receipt validation is enabled.
		/// </summary>
		/// <returns><c>true</c> if is receipt validation enabled; otherwise, <c>false</c>.</returns>
		private bool IsReceiptValidationEnabled()
		{
			bool canValidateReceipt = false; // disable receipt validation by default

			if (Application.platform == RuntimePlatform.Android)
			{
				// On Android, receipt validation is only available for Google Play store
				canValidateReceipt = validateGooglePlayReceipt;
				canValidateReceipt &= GetAndroidStore(targetAndroidStore) == UnityEngine.Purchasing.AndroidStore.GooglePlay;
			}
			else if (Application.platform == RuntimePlatform.IPhonePlayer ||
					Application.platform == RuntimePlatform.OSXPlayer ||
					Application.platform == RuntimePlatform.tvOS)
			{
				// Receipt validation is also available for Apple app stores
				canValidateReceipt = validateAppleReceipt;
			}

			return canValidateReceipt;
		}

		/// <summary>
		/// Validates the receipt. Works with receipts from Apple stores and Google Play store only.
		/// Always returns true for other stores.
		/// </summary>
		private bool ValidateReceipt(string receipt, out IPurchaseReceipt[] pPurchaseReceipts, bool logReceiptContent = false)
		{
			pPurchaseReceipts = new IPurchaseReceipt[0]; // default the out parameter to an empty array   

			// Does the receipt has some content?
			if (string.IsNullOrEmpty(receipt))
			{
				Debug.Log("Receipt Validation: receipt is null or empty.");
				return false;
			}

			bool isValidReceipt = true; // presume validity for platforms with no receipt validation.
			// Unity IAP's receipt validation is only available for Apple app stores and Google Play store.   
#if UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_TVOS

			byte[] googlePlayTangleData = null;
			byte[] appleTangleData = null;

			// Here we populate the secret keys for each platform.
			// Note that the code is disabled in the editor for it to not stop the EM editor code (due to ClassNotFound error)
			// from recreating the dummy AppleTangle and GoogleTangle classes if they were inadvertently removed.

#if UNITY_ANDROID && !UNITY_EDITOR
            googlePlayTangleData = GooglePlayTangle.Data();
#endif

#if (UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_TVOS) && !UNITY_EDITOR
            appleTangleData = AppleTangle.Data();
#endif

			// Prepare the validator with the secrets we prepared in the Editor obfuscation window.
#if UNITY_5_6_OR_NEWER
			var validator = new CrossPlatformValidator(googlePlayTangleData, appleTangleData, Application.identifier);
#else
            var validator = new CrossPlatformValidator(googlePlayTangleData, appleTangleData, Application.bundleIdentifier);
#endif

			try
			{
				// On Google Play, result has a single product ID.
				// On Apple stores, receipts contain multiple products.
				var result = validator.Validate(receipt);

				// If the validation is successful, the result won't be null.
				if (result == null)
				{
					isValidReceipt = false;
				}
				else
				{
					pPurchaseReceipts = result;

					// For informational purposes, we list the receipt(s)
					if (logReceiptContent)
					{
						Debug.Log("Receipt contents:");
						foreach (var productReceipt in result)
						{
							if (productReceipt != null)
							{
								Debug.Log(productReceipt.productID);
								Debug.Log(productReceipt.purchaseDate);
								Debug.Log(productReceipt.transactionID);
							}
						}
					}
				}
			}
			catch (IAPSecurityException)
			{
				isValidReceipt = false;
			}
#endif
			return isValidReceipt;
		}

#endregion

#else
        public string GetProductLocalizedPrice(string sku)
        {
            return null;
        }
        public void Purchase(string sku, Action<bool, string> p)
        {
            p?.Invoke(false, null);
        }
        public void Init(List<Product> pProducts, Action<bool> pCallback)
        {
            pCallback?.Invoke(false);
        }
#endif
		[Serializable]
		public class Product
		{
			[Serializable]
			public class StoreSpecificId
			{
				public Store store;
				public string id;
			}

			public enum Type
			{
				Consumable,
				NonConsumable,
				Subscription
			}

			public string id;
			public Type type;
			/// <summary>
			/// Store-specific product Ids, these Ids if given will override the unified Id for the corresponding stores.
			/// </summary>
			[FormerlySerializedAs("StoreSpecificIds")] public StoreSpecificId[] storeSpecificIds;
		}

		public enum Store
		{
			GooglePlay,
			AmazonAppStore,
			MacAppStore,
			AppleAppStore,
			WinRT
		}

		public enum AndroidStore
		{
			GooglePlay,
			AmazonAppStore,
			NotSpecified
		}
	}
}