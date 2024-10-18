using UnityEngine;
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
		private static UnityPayment m_Instance;
		public static UnityPayment Instance => m_Instance;

		private void Awake()
		{
			if (m_Instance == null)
				m_Instance = this;
			else if (m_Instance != this)
				Destroy(gameObject);
		}

		/// <summary>
		/// [Apple store only] Occurs when the (non-consumable and subscription) 
		/// purchases were restored successfully.
		/// On non-Apple platforms this event will never fire.
		/// </summary>
		public Action onRestoreCompleted;
		/// <summary>
		/// [Apple store only] Occurs when the purchase restoration failed.
		/// On non-Apple platforms this event will never fire.
		/// </summary>
		public Action onRestoreFailed;
#if UNITY_IAP
		private const string PROCESSING_PURCHASE_ABORT = "PROCESSING_PURCHASE_ABORT";
		private const string PROCESSING_PURCHASE_INVALID_RECEIPT = "PROCESSING_PURCHASE_INVALID_RECEIPT";
		private const string CONFIRM_PENDING_PURCHASE_FAILED = "CONFIRM_PENDING_PURCHASE_FAILED";
		private const string K_ENVIRONMENT = "production";

		public List<Product> products;
		public bool validateAppleReceipt = true;
		public AndroidStore targetAndroidStore;
		public bool validateGooglePlayReceipt = true;
		public bool interceptApplePromotionalPurchases;
		public bool simulateAppleAskToBuy;

		public Action<Product> onPurchaseCompleted;
		public Action<Product, string> onPurchaseFailed;
		public Action<Product> onPurchaseDeferred;

		/// <summary>
		/// [Apple store only] Occurs when a promotional purchase that comes directly
		/// from the App Store is intercepted. This event only fires if the
		/// <see cref="IAPSettings.InterceptApplePromotionalPurchases"/> setting is <c>true</c>.
		/// On non-Apple platforms this event will never fire.
		/// </summary>
		public Action<Product> onPromotionalPurchaseIntercepted;

		private Action<bool> m_onInitialized;
		private Action<bool, string> m_onPurchased;
		private ConfigurationBuilder m_builder;
		private IStoreController m_storeController;
		private IExtensionProvider m_storeExtensionProvider;
		private IAppleExtensions m_appleExtensions;
		private IGooglePlayStoreExtensions m_googlePlayStoreExtensions;

		public void OnInitialized(IStoreController p_controller, IExtensionProvider p_extensions)
		{
			// Purchasing has succeeded initializing.
			Debug.Log("In-App Purchasing OnInitialized: PASS");

			// Overall Purchasing system, configured with products for this application.
			m_storeController = p_controller;

			// Store specific subsystem, for accessing device-specific store features.
			m_storeExtensionProvider = p_extensions;

			// Get the store extensions for later use.
			m_appleExtensions = m_storeExtensionProvider.GetExtension<IAppleExtensions>();
			m_googlePlayStoreExtensions = m_storeExtensionProvider.GetExtension<IGooglePlayStoreExtensions>();

			// Apple store specific setup.
			if (m_appleExtensions != null && Application.platform == RuntimePlatform.IPhonePlayer)
			{
				// Enable Ask To Buy simulation in sandbox if needed.
				m_appleExtensions.simulateAskToBuy = simulateAppleAskToBuy;

				// Register a handler for Ask To Buy's deferred purchases.
				m_appleExtensions.RegisterPurchaseDeferredListener(OnApplePurchaseDeferred);
			}

			m_onInitialized?.Invoke(true);
		}

		public void OnInitializeFailed(InitializationFailureReason p_error)
		{
			// Purchasing set-up has not succeeded. Check error for reason. Consider sharing this reason with the user.
			Debug.Log("In-App Purchasing OnInitializeFailed. InitializationFailureReason:" + p_error);

			m_onInitialized?.Invoke(false);
		}

		public void OnInitializeFailed(InitializationFailureReason p_error, string p_message)
		{
			m_onInitialized?.Invoke(false);
		}

		public void OnPurchaseFailed(UnityEngine.Purchasing.Product p_product, PurchaseFailureReason p_failureReason)
		{
			// A product purchase attempt did not succeed. Check failureReason for more detail. Consider sharing 
			// this reason with the user to guide their troubleshooting actions.
			Debug.Log($"Couldn't purchase product: '{p_product.definition.storeSpecificId}', PurchaseFailureReason: {p_failureReason}");

			// Fire purchase failure event
			onPurchaseFailed?.Invoke(GetIAPProductById(p_product.definition.id), p_failureReason.ToString());
			m_onPurchased?.Invoke(false, p_failureReason.ToString());
		}

		public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs p_args)
		{
			Debug.Log("Processing purchase of product: " + p_args.purchasedProduct.transactionID);

			var pd = GetIAPProductById(p_args.purchasedProduct.definition.id);

			if (m_sPrePurchaseProcessDel != null)
			{
				var nextStep = m_sPrePurchaseProcessDel(p_args);

				if (nextStep == PrePurchaseProcessResult.Abort)
				{
					Debug.Log("Purchase aborted.");

					// Fire purchase failure event
					onPurchaseFailed?.Invoke(pd, PROCESSING_PURCHASE_ABORT);
					m_onPurchased?.Invoke(false, PROCESSING_PURCHASE_ABORT);

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
				validPurchase = ValidateReceipt(p_args.purchasedProduct.receipt, out _);
			}

			if (validPurchase)
			{
				Debug.Log("Product purchase completed.");

				// Fire purchase success event
				onPurchaseCompleted?.Invoke(pd);
				m_onPurchased?.Invoke(true, null);
			}
			else
			{
				Debug.Log("Couldn't purchase product: Invalid receipt.");

				// Fire purchase failure event
				onPurchaseFailed?.Invoke(pd, PROCESSING_PURCHASE_INVALID_RECEIPT);
				m_onPurchased?.Invoke(false, PROCESSING_PURCHASE_INVALID_RECEIPT);
			}

			return PurchaseProcessingResult.Complete;
		}

		public void Init(List<Product> p_pProducts, Action<bool> p_pCallback)
		{
			products = p_pProducts;
			Init(p_pCallback);
		}

		public async void Init(Action<bool> p_pCallback)
		{

			if (IsInitialized())
			{
				p_pCallback?.Invoke(true);
				return;
			}

			m_onInitialized = p_pCallback;

			var options = new InitializationOptions().SetEnvironmentName(K_ENVIRONMENT);
			var task = UnityServices.InitializeAsync(options);
			await task;

			if (!task.IsFaulted && !task.IsCanceled)
				Debug.Log("Congratulations!\nUnity Gaming Services has been successfully initialized.");

			// Create a builder, first passing in a suite of Unity provided stores.
			m_builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

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
					m_builder.AddProduct(pd.id, GetProductType(pd.type), storeIDs);
				}
				else
				{
					// Add product using store-independent id
					m_builder.AddProduct(pd.id, GetProductType(pd.type));
				}
			}

			// Intercepting Apple promotional purchases if needed.
			if (Application.platform == RuntimePlatform.IPhonePlayer)
			{
				if (interceptApplePromotionalPurchases)
					m_builder.Configure<IAppleConfiguration>().SetApplePromotionalPurchaseInterceptorCallback(OnApplePromotionalPurchase);
			}

			// Kick off the remainder of the set-up with an asynchrounous call, passing the configuration 
			// and this class'  Expect a response either in OnInitialized or OnInitializeFailed.
			UnityPurchasing.Initialize(this, m_builder);
		}

		/// <summary>
		/// Determines whether UnityIAP is initialized. All further actions like purchasing
		/// or restoring can only be done if UnityIAP is initialized.
		/// </summary>
		/// <returns><c>true</c> if initialized; otherwise, <c>false</c>.</returns>
		public bool IsInitialized()
		{
			// Only say we are initialized if both the Purchasing references are set.
			return m_storeController != null && m_storeExtensionProvider != null;
		}

		/// <summary>
		/// Purchases the product with specified ID.
		/// </summary>
		/// <param name="p_productId">Product identifier.</param>
		public void Purchase(string p_productId, Action<bool, string> p_pCallback)
		{
#if UNITY_EDITOR
			p_pCallback?.Invoke(true, null);
			return;
#endif

			if (IsInitialized())
			{
				m_onPurchased = p_pCallback;
				var product = m_storeController.products.WithID(p_productId);

				if (product != null && product.availableToPurchase)
				{
					Debug.Log("Purchasing product asychronously: " + product.definition.id);

					// Buy the product, expect a response either through ProcessPurchase or OnPurchaseFailed asynchronously.
					m_storeController.InitiatePurchase(product);
				}
				else
				{
					Debug.Log("IAP purchasing failed: product not found or not available for purchase.");
				}
			}
			else
			{
				string log = "IAP purchasing failed: In-App Purchasing is not initialized.";
				p_pCallback?.Invoke(false, log);
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

			if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.OSXPlayer)
			{
				// Fetch the Apple store-specific subsystem.
				var apple = m_storeExtensionProvider.GetExtension<IAppleExtensions>();

				// Begin the asynchronous process of restoring purchases. Expect a confirmation response in 
				// the Action<bool> below, and ProcessPurchase if there are previously purchased products to restore.
				apple.RestoreTransactions((p_result) =>
				{
					// The first phase of restoration. If no more responses are received on ProcessPurchase then 
					// no purchases are available to be restored.
					Debug.Log("Restoring IAP purchases result: " + p_result);

					if (p_result)
					{
						// Fire restore complete event.
						onRestoreCompleted?.Invoke();
					}
					else
					{
						// Fire event failed event.
						onRestoreFailed?.Invoke();
					}
				});
			}
			else
			{
				// We are not running on an Apple device. No work is necessary to restore purchases.
				Debug.Log("Couldn't restore IAP purchases: not supported on platform " + Application.platform);
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
		/// <param name="p_productId">Product id.</param>
		public bool IsProductWithIdOwned(string p_productId)
		{
			var iapProduct = GetIAPProductById(p_productId);
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
		/// <param name="p_product">the product.</param>
		public bool IsProductOwned(Product p_product)
		{

			if (!IsInitialized())
				return false;

			if (p_product == null)
				return false;

			var pd = m_storeController.products.WithID(p_product.id);

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
		/// <param name="p_successCallback">Success callback.</param>
		/// <param name="p_errorCallback">Error callback.</param>
		public void RefreshAppleAppReceipt(Action<string> p_successCallback, Action p_errorCallback)
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

			m_storeExtensionProvider.GetExtension<IAppleExtensions>().RefreshAppReceipt(
				(p_receipt) =>
				{
					// This handler is invoked if the request is successful.
					// Receipt will be the latest app receipt.
					p_successCallback?.Invoke(p_receipt);
				},
				() =>
				{
					// This handler will be invoked if the request fails,
					// such as if the network is unavailable or the user
					// enters the wrong password.
					p_errorCallback?.Invoke();
				});
		}

		/// <summary>
		/// Enables or disables the Apple's Ask-To-Buy simulation in the sandbox app store.
		/// Call this after the module has been initialized to toggle the simulation, regardless
		/// of the <see cref="IAPSettings.SimulateAppleAskToBuy"/> setting.
		/// </summary>
		/// <param name="p_shouldSimulate">If set to <c>true</c> should simulate.</param>
		public void SetSimulateAppleAskToBuy(bool p_shouldSimulate)
		{
			if (m_appleExtensions != null)
				m_appleExtensions.simulateAskToBuy = p_shouldSimulate;
		}

		/// <summary>
		/// Continues the Apple promotional purchases. Call this inside the handler of
		/// the <see cref="onPromotionalPurchaseIntercepted"/> event to continue the
		/// intercepted purchase.
		/// </summary>
		public void ContinueApplePromotionalPurchases()
		{
			m_appleExtensions?.ContinuePromotionalPurchases(); // iOS and tvOS only; does nothing on Mac
		}

		/// <summary>
		/// Sets the Apple store promotion visibility for the specified product on the current device.
		/// Call this inside the handler of the <see cref="InitializeSucceeded"/> event to set
		/// the visibility for a promotional product on Apple App Store.
		/// On non-Apple platforms this method is a no-op.
		/// </summary>
		/// <param name="p_productId">Product id.</param>
		/// <param name="p_visible">If set to <c>true</c> the product is shown, otherwise it is hidden.</param>
		public void SetAppleStorePromotionVisibilityWithId(string p_productId, bool p_visible)
		{
			var iapProduct = GetIAPProductById(p_productId);

			if (iapProduct == null)
			{
				Debug.Log("Couldn't set promotion visibility: not found product with id: " + p_productId);
				return;
			}

			SetAppleStorePromotionVisibility(iapProduct, p_visible);
		}

		/// <summary>
		/// Sets the Apple store promotion visibility for the specified product on the current device.
		/// Call this inside the handler of the <see cref="InitializeSucceeded"/> event to set
		/// the visibility for a promotional product on Apple App Store.
		/// On non-Apple platforms this method is a no-op.
		/// </summary>
		/// <param name="p_product">the product.</param>
		/// <param name="p_visible">If set to <c>true</c> the product is shown, otherwise it is hidden.</param>
		public void SetAppleStorePromotionVisibility(Product p_product, bool p_visible)
		{

			if (!IsInitialized())
			{
				Debug.Log("Couldn't set promotion visibility: In-App Purchasing is not initialized.");
				return;
			}

			if (p_product == null)
				return;

			var prod = m_storeController.products.WithID(p_product.id);

			m_appleExtensions?.SetStorePromotionVisibility(prod,
				p_visible ? AppleStorePromotionVisibility.Show : AppleStorePromotionVisibility.Hide);
		}

		/// <summary>
		/// Sets the Apple store promotion order on the current device.
		/// Call this inside the handler of the <see cref="InitializeSucceeded"/> event to set
		/// the order for your promotional products on Apple App Store.
		/// On non-Apple platforms this method is a no-op.
		/// </summary>
		/// <param name="p_pProducts">Products.</param>
		public void SetAppleStorePromotionOrder(List<Product> p_pProducts)
		{

			if (!IsInitialized())
			{
				Debug.Log("Couldn't set promotion order: In-App Purchasing is not initialized.");
				return;
			}

			var items = new List<UnityEngine.Purchasing.Product>();

			for (int i = 0; i < p_pProducts.Count; i++)
			{
				if (p_pProducts[i] != null)
					items.Add(m_storeController.products.WithID(p_pProducts[i].id));
			}

			m_appleExtensions?.SetStorePromotionOrder(items);
		}

		/// <summary>
		/// Gets the IAP product declared in module settings with the specified identifier.
		/// </summary>
		/// <returns>The IAP product.</returns>
		/// <param name="p_productId">Product identifier.</param>
		public Product GetIAPProductById(string p_productId)
		{
			foreach (var pd in products)
			{
				if (pd.id.Equals(p_productId))
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
		/// <param name="p_productId">Product id.</param>
		public UnityEngine.Purchasing.Product GetProductWithId(string p_productId)
		{
			var iapProduct = GetIAPProductById(p_productId);

			if (iapProduct == null)
			{
				Debug.Log("Couldn't get product: not found product with id: " + p_productId);
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
		/// <param name="p_product">the product.</param>
		public UnityEngine.Purchasing.Product GetProduct(Product p_product)
		{
			if (!IsInitialized())
			{
				Debug.Log("Couldn't get product: In-App Purchasing is not initialized.");
				return null;
			}

			if (p_product == null)
			{
				return null;
			}

			return m_storeController.products.WithID(p_product.id);
		}

		/// <summary>
		/// Gets the product localized data provided by the stores.
		/// </summary>
		/// <returns>The product localized data.</returns>
		/// <param name="product">the product.</param>
		public ProductMetadata GetProductLocalizedData(string p_productId)
		{
			if (!IsInitialized())
			{
				Debug.Log("Couldn't get product localized data: In-App Purchasing is not initialized.");
				return null;
			}

			return m_storeController.products.WithID(p_productId).metadata;
		}

		public string GetProductLocalizedPrice(string p_productId)
		{
			var metadata = m_storeController?.products.WithID(p_productId).metadata;
			return metadata?.localizedPriceString;
		}

		public decimal GetProductPrice(string p_productId, out char p_pFirstSymbol, out char p_pLastSymbol)
		{
			p_pFirstSymbol = ' ';
			p_pLastSymbol = ' ';
			var metadata = m_storeController?.products.WithID(p_productId).metadata;
			if (metadata != null)
			{
				var str = metadata.localizedPriceString;
				char firstCharacter = str[0];
				char lastCharacter = str[str.Length - 1];
				if (!char.IsNumber(firstCharacter))
					p_pFirstSymbol = firstCharacter;
				if (!char.IsNumber(lastCharacter))
					p_pLastSymbol = lastCharacter;
				return metadata.localizedPrice;
			}
			return 0;
		}

		/// <summary>
		/// Gets the parsed Apple InAppPurchase receipt for the specified product.
		/// This method only works if receipt validation is enabled.
		/// </summary>
		/// <returns>The Apple In App Purchase receipt.</returns>
		/// <param name="p_productId">Product id.</param>
		public AppleInAppPurchaseReceipt GetAppleIAPReceiptWithId(string p_productId)
		{
			if (Application.platform == RuntimePlatform.IPhonePlayer)
			{
				return GetPurchaseReceiptWithId(p_productId) as AppleInAppPurchaseReceipt;
			}
			Debug.Log("Getting Apple IAP receipt is only available on iOS.");
			return null;
		}

		/// <summary>
		/// Gets the parsed Apple InAppPurchase receipt for the specified product.
		/// This method only works if receipt validation is enabled.
		/// </summary>
		/// <returns>The Apple In App Purchase receipt.</returns>
		/// <param name="p_product">The product.</param>
		public AppleInAppPurchaseReceipt GetAppleIAPReceipt(Product p_product)
		{
			if (Application.platform == RuntimePlatform.IPhonePlayer)
			{
				return GetPurchaseReceipt(p_product) as AppleInAppPurchaseReceipt;
			}
			Debug.Log("Getting Apple IAP receipt is only available on iOS.");
			return null;
		}

		/// <summary>
		/// Gets the parsed Google Play receipt for the specified product.
		/// This method only works if receipt validation is enabled.
		/// </summary>
		/// <returns>The Google Play receipt.</returns>
		/// <param name="p_productId">Product id.</param>
		public GooglePlayReceipt GetGooglePlayReceiptWithId(string p_productId)
		{
			if (Application.platform == RuntimePlatform.Android)
			{
				return GetPurchaseReceiptWithId(p_productId) as GooglePlayReceipt;
			}
			Debug.Log("Getting Google Play receipt is only available on Android.");
			return null;
		}

		/// <summary>
		/// Gets the parsed Google Play receipt for the specified product.
		/// This method only works if receipt validation is enabled.
		/// </summary>
		/// <returns>The Google Play receipt.</returns>
		/// <param name="p_product">The product.</param>
		public GooglePlayReceipt GetGooglePlayReceipt(Product p_product)
		{
			if (Application.platform == RuntimePlatform.Android)
			{
				return GetPurchaseReceipt(p_product) as GooglePlayReceipt;
			}
			Debug.Log("Getting Google Play receipt is only available on Android.");
			return null;
		}

		/// <summary>
		/// Gets the parsed purchase receipt for the product.
		/// This method only works if receipt validation is enabled.
		/// </summary>
		/// <returns>The purchase receipt.</returns>
		/// <param name="p_produtId">Product id.</param>
		public IPurchaseReceipt GetPurchaseReceiptWithId(string p_produtId)
		{
			var iapProduct = GetIAPProductById(p_produtId);

			if (iapProduct == null)
			{
				Debug.Log("Couldn't get purchase receipt: not found product with id: " + p_produtId);
				return null;
			}

			return GetPurchaseReceipt(iapProduct);
		}

		/// <summary>
		/// Gets the parsed purchase receipt for the product.
		/// This method only works if receipt validation is enabled.
		/// </summary>
		/// <returns>The purchase receipt.</returns>
		/// <param name="p_product">the product.</param>
		public IPurchaseReceipt GetPurchaseReceipt(Product p_product)
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

			if (p_product == null)
			{
				Debug.Log("Couldn't get purchase receipt: product is null");
				return null;
			}

			var pd = m_storeController.products.WithID(p_product.id);

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
		/// <param name="p_productId">Product name.</param>
		public SubscriptionInfo GetSubscriptionInfoWithId(string p_productId)
		{
			var iapProduct = GetIAPProductById(p_productId);

			if (iapProduct == null)
			{
				Debug.Log("Couldn't get subscription info: not found product with id: " + p_productId);
				return null;
			}
			return GetSubscriptionInfo(iapProduct);
		}

		/// <summary>
		/// Gets the subscription info of the product using the SubscriptionManager class,
		/// which currently supports the Apple store and Google Play store.
		/// </summary>
		/// <returns>The subscription info.</returns>
		/// <param name="p_product">The product.</param>
		public SubscriptionInfo GetSubscriptionInfo(Product p_product)
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

			if (p_product == null)
			{
				Debug.Log("Couldn't get subscription info: product is null");
				return null;
			}

			var prod = m_storeController.products.WithID(p_product.id);

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

			if (m_appleExtensions != null)
				introPriceDict = m_appleExtensions.GetIntroductoryPriceDictionary();

			string introJson = (introPriceDict == null || !introPriceDict.ContainsKey(prod.definition.storeSpecificId)) ? null : introPriceDict[prod.definition.storeSpecificId];

			var p = new SubscriptionManager(prod, introJson);
			return p.getSubscriptionInfo();
		}

		/// <summary>
		/// Gets the name of the store.
		/// </summary>
		/// <returns>The store name.</returns>
		/// <param name="p_store">Store.</param>
		public string GetStoreName(Store p_store)
		{
			switch (p_store)
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
		/// <param name="p_pType">P type.</param>
		public ProductType GetProductType(Product.Type p_pType)
		{
			switch (p_pType)
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
		/// <param name="p_store">Store.</param>
		public UnityEngine.Purchasing.AndroidStore GetAndroidStore(AndroidStore p_store)
		{
			switch (p_store)
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
		/// <param name="p_store">Store.</param>
		public AppStore GetAppStore(AndroidStore p_store)
		{
			switch (p_store)
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
		/// won't inform the app of the purchase again. After confirming the purchase, either <see cref="onPurchaseCompleted"/>
		/// or <see cref="onPurchaseFailed"/> event will be called according to the input given by the caller.
		/// </summary>
		/// <param name="p_product">The pending product to confirm.</param>
		/// <param name="p_purchaseSuccess">If true, <see cref="onPurchaseCompleted"/> event will be called, otherwise <see cref="onPurchaseFailed"/> event will be called.</param>
		public void ConfirmPendingPurchase(UnityEngine.Purchasing.Product p_product, bool p_purchaseSuccess)
		{
			m_storeController?.ConfirmPendingPurchase(p_product);

			if (p_purchaseSuccess)
			{
				onPurchaseCompleted?.Invoke(GetIAPProductById(p_product.definition.id));
			}
			else
			{
				onPurchaseFailed?.Invoke(GetIAPProductById(p_product.definition.id), CONFIRM_PENDING_PURCHASE_FAILED);
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
		/// <param name="p_args"></param>
		/// <returns></returns>
		public delegate PrePurchaseProcessResult PrePurchaseProcessing(PurchaseEventArgs p_args);

		private PrePurchaseProcessing m_sPrePurchaseProcessDel;

		/// <summary>
		/// Registers a <see cref="PrePurchaseProcessing"/> delegate.
		/// </summary>
		/// <param name="p_del"></param>
		public void RegisterPrePurchaseProcessDelegate(PrePurchaseProcessing p_del)
		{
			m_sPrePurchaseProcessDel = p_del;
		}

#endregion

#region Private Stuff

		/// <summary>
		/// Raises the purchase deferred event.
		/// Apple store only.
		/// </summary>
		/// <param name="p_product">Product.</param>
		private void OnApplePurchaseDeferred(UnityEngine.Purchasing.Product p_product)
		{
			Debug.Log("Purchase deferred: " + p_product.definition.id);

			onPurchaseDeferred?.Invoke(GetIAPProductById(p_product.definition.id));
		}

		/// <summary>
		/// Raises the Apple promotional purchase event.
		/// Apple store only.
		/// </summary>
		/// <param name="p_product">Product.</param>
		private void OnApplePromotionalPurchase(UnityEngine.Purchasing.Product p_product)
		{
			Debug.Log("Attempted promotional purchase: " + p_product.definition.id);

			onPromotionalPurchaseIntercepted?.Invoke(GetIAPProductById(p_product.definition.id));
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
				canValidateReceipt &= (GetAndroidStore(targetAndroidStore) == UnityEngine.Purchasing.AndroidStore.GooglePlay);
			}
			else if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.tvOS)
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
		/// <returns><c>true</c>, if the receipt is valid, <c>false</c> otherwise.</returns>
		/// <param name="p_receipt">Receipt.</param>
		/// <param name="p_logReceiptContent">If set to <c>true</c> log receipt content.</param>
		private bool ValidateReceipt(string p_receipt, out IPurchaseReceipt[] p_purchaseReceipts, bool p_logReceiptContent = false)
		{
			p_purchaseReceipts = new IPurchaseReceipt[0]; // default the out parameter to an empty array   

			// Does the receipt has some content?
			if (string.IsNullOrEmpty(p_receipt))
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
				var result = validator.Validate(p_receipt);

				// If the validation is successful, the result won't be null.
				if (result == null)
				{
					isValidReceipt = false;
				}
				else
				{
					p_purchaseReceipts = result;

					// For informational purposes, we list the receipt(s)
					if (p_logReceiptContent)
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
        public bool IsProductWithIdOwned(string sku) => false;
        public decimal GetProductPrice(string p_productId, out char p_pFirstSymbol, out char p_pLastSymbol)
        {
            p_pFirstSymbol = default(char);
            p_pLastSymbol = default(char);
            return 0;
        }
        public void RestorePurchases() => onRestoreFailed?.Invoke();
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