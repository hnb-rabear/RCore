#if UNITY_ANDROID || UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_TVOS
// You must obfuscate your secrets using Window > Unity IAP > Receipt Validation Obfuscator
// before receipt validation will compile in this sample.
#define RECEIPT_VALIDATION
#endif

//#define DELAY_CONFIRMATION // Returns PurchaseProcessingResult.Pending from ProcessPurchase, then calls ConfirmPendingPurchase after a delay
//#define USE_PAYOUTS // Enables use of PayoutDefinitions to specify what the player should receive when a product is purchased
//#define INTERCEPT_PROMOTIONAL_PURCHASES // Enables intercepting promotional purchases that come directly from the Apple App Store

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;

#if RECEIPT_VALIDATION
using UnityEngine.Purchasing.Security;
using RCore.Common;
using Debug = UnityEngine.Debug;
#endif

public class UnityPayment : IStoreListener
{
    public static UnityPayment m_Instance;
    public static UnityPayment Instance
    {
        get
        {
            if (m_Instance == null)
                return m_Instance = new UnityPayment();
            return m_Instance;
        }
    }

    private IStoreController m_Controller;
    private IAppleExtensions m_AppleExtensions;
    private ITransactionHistoryExtensions m_TransactionHistoryExtensions;
    private IGooglePlayStoreExtensions m_GooglePlayStoreExtensions;

#pragma warning disable 0414
    private bool m_IsGooglePlayStoreSelected;
#pragma warning restore 0414

    private bool m_PurchaseInProgress;
    private Action<bool> m_OnInitialized;
    private Action<bool, string> m_OnPurchased;
    private Action<bool, string> m_OnRestored;

#if RECEIPT_VALIDATION
    private CrossPlatformValidator m_Validator;
#endif

    /// <summary>
    /// Purchasing initialized successfully.
    ///
    /// The <c>IStoreController</c> and <c>IExtensionProvider</c> are
    /// available for accessing purchasing functionality.
    /// </summary>
    /// <param name="controller"> The <c>IStoreController</c> created during initialization. </param>
    /// <param name="extensions"> The <c>IExtensionProvider</c> created during initialization. </param>
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        m_Controller = controller;
        m_AppleExtensions = extensions.GetExtension<IAppleExtensions>();
        m_TransactionHistoryExtensions = extensions.GetExtension<ITransactionHistoryExtensions>();
        m_GooglePlayStoreExtensions = extensions.GetExtension<IGooglePlayStoreExtensions>();

        // On Apple platforms we need to handle deferred purchases caused by Apple's Ask to Buy feature.
        // On non-Apple platforms this will have no effect; OnDeferred will never be called.
        m_AppleExtensions.RegisterPurchaseDeferredListener(OnDeferred);

        Debug.Log("Available items:");
        foreach (var item in controller.products.all)
        {
            if (item.availableToPurchase)
            {
                Debug.Log(string.Join(" - ",
                    new[]
                    {
                        item.metadata.localizedTitle,
                        item.metadata.localizedDescription,
                        item.metadata.isoCurrencyCode,
                        item.metadata.localizedPrice.ToString(),
                        item.metadata.localizedPriceString,
                        item.transactionID,
                        item.receipt
                    }));
#if INTERCEPT_PROMOTIONAL_PURCHASES
                // Set all these products to be visible in the user's App Store according to Apple's Promotional IAP feature
                // https://developer.apple.com/library/content/documentation/NetworkingInternet/Conceptual/StoreKitGuide/PromotingIn-AppPurchases/PromotingIn-AppPurchases.html
                m_AppleExtensions.SetStorePromotionVisibility(item, AppleStorePromotionVisibility.Show);
#endif
            }
        }

        m_OnInitialized?.Invoke(true);

        LogProductDefinitions();
    }

    /// <summary>
    /// A purchase succeeded.
    /// </summary>
    /// <param name="e"> The <c>PurchaseEventArgs</c> for the purchase event. </param>
    /// <returns> The result of the successful purchase </returns>
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
    {
        Debug.Log("Purchase OK: " + e.purchasedProduct.definition.id);
        Debug.Log("Receipt: " + e.purchasedProduct.receipt);

        m_PurchaseInProgress = false;

#if RECEIPT_VALIDATION // Local validation is available for GooglePlay, and Apple stores
        if (m_IsGooglePlayStoreSelected ||
            Application.platform == RuntimePlatform.IPhonePlayer ||
            Application.platform == RuntimePlatform.OSXPlayer ||
            Application.platform == RuntimePlatform.tvOS)
        {
            try
            {
                var result = m_Validator.Validate(e.purchasedProduct.receipt);
                Debug.Log("Receipt is valid. Contents:");
                foreach (IPurchaseReceipt productReceipt in result)
                {
                    Debug.Log(productReceipt.productID);
                    Debug.Log(productReceipt.purchaseDate);
                    Debug.Log(productReceipt.transactionID);

                    GooglePlayReceipt google = productReceipt as GooglePlayReceipt;
                    if (null != google)
                    {
                        Debug.Log(google.purchaseState);
                        Debug.Log(google.purchaseToken);
                    }

                    AppleInAppPurchaseReceipt apple = productReceipt as AppleInAppPurchaseReceipt;
                    if (null != apple)
                    {
                        Debug.Log(apple.originalTransactionIdentifier);
                        Debug.Log(apple.subscriptionExpirationDate);
                        Debug.Log(apple.cancellationDate);
                        Debug.Log(apple.quantity);
                    }

                    // For improved security, consider comparing the signed
                    // IPurchaseReceipt.productId, IPurchaseReceipt.transactionID, and other data
                    // embedded in the signed receipt objects to the data which the game is using
                    // to make this purchase.
                }
            }
            catch (IAPSecurityException ex)
            {
                string message = $"Invalid receipt, not unlocking content. {ex}";
                m_OnPurchased?.Invoke(false, message);
                Debug.LogError(message);
                return PurchaseProcessingResult.Complete;
            }
            catch (NotImplementedException exception)
            {
                string message = $"Cross Platform Validator Not Implemented: {exception}";
                Debug.LogError(message);
            }
        }
#endif

        // Unlock content from purchases here.
#if USE_PAYOUTS
        if (e.purchasedProduct.definition.payouts != null)
        {
            // Purchase complete, paying out based on defined payouts
            foreach (var payout in e.purchasedProduct.definition.payouts)
            {
            }
        }
#endif
        // Indicate if we have handled this purchase.
        //   PurchaseProcessingResult.Complete: ProcessPurchase will not be called
        //     with this product again, until next purchase.
        //   PurchaseProcessingResult.Pending: ProcessPurchase will be called
        //     again with this product at next app launch. Later, call
        //     m_Controller.ConfirmPendingPurchase(Product) to complete handling
        //     this purchase. Use to transactionally save purchases to a cloud
        //     game service.
#if DELAY_CONFIRMATION
        StartCoroutine(ConfirmPendingPurchaseAfterDelay(e.purchasedProduct));
        return PurchaseProcessingResult.Pending;
#else
        m_OnPurchased?.Invoke(true, null);
        return PurchaseProcessingResult.Complete;
#endif
    }

#if DELAY_CONFIRMATION
    private HashSet<string> m_PendingProducts = new HashSet<string>();

    private IEnumerator ConfirmPendingPurchaseAfterDelay(Product p)
    {
        m_PendingProducts.Add(p.definition.id);
        Debug.Log($"Delaying confirmation of {p.definition.id} for 5 seconds.");

        var end = Time.time + 5f;
        while (Time.time < end)
        {
            yield return null;
            var remaining = Mathf.CeilToInt(end - Time.time);
        }

        Debug.Log("Confirming purchase of " + p.definition.id);
        m_Controller.ConfirmPendingPurchase(p);
        m_PendingProducts.Remove(p.definition.id);
        m_OnPurchased?.Invoke(true, null);
    }
#endif

    /// <summary>
    /// A purchase failed with specified reason.
    /// </summary>
    /// <param name="item">The product that was attempted to be purchased. </param>
    /// <param name="reason">The failure reason.</param>
    public void OnPurchaseFailed(Product item, PurchaseFailureReason reason)
    {
        string message = "Purchase failed: " + item.definition.id;
        message += "/n" + reason;
        message += "/n" + "Store specific error code: " + m_TransactionHistoryExtensions.GetLastStoreSpecificPurchaseErrorCode();
        if (m_TransactionHistoryExtensions.GetLastPurchaseFailureDescription() != null)
            message += "/n" + "Purchase failure description message: " + m_TransactionHistoryExtensions.GetLastPurchaseFailureDescription().message;

        m_PurchaseInProgress = false;
        m_OnPurchased?.Invoke(false, message);
    }

    public decimal GetLocalizedPrice(string pPackageId)
    {
        try
        {
            var product = m_Controller.products.WithID(pPackageId);
            if (product != null)
                return product.metadata.localizedPrice;
            return 0;
        }
        catch { return 0; }
    }

    public string GetLocalizedPriceString(string pPackageId)
    {
        try
        {
            var product = m_Controller.products.WithID(pPackageId);
            if (product != null)
                return product.metadata.localizedPriceString;
            return "";
        }
        catch { return ""; }
    }

    /// <summary>
    /// Purchasing failed to initialise for a non recoverable reason.
    /// </summary>
    /// <param name="error"> The failure reason. </param>
    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.Log("Billing failed to initialize!");
        switch (error)
        {
            case InitializationFailureReason.AppNotKnown:
                Debug.LogError("Is your App correctly uploaded on the relevant publisher console?");
                break;

            case InitializationFailureReason.PurchasingUnavailable:
                // Ask the user if billing is disabled in device settings.
                Debug.Log("Billing disabled!");
                break;

            case InitializationFailureReason.NoProductsAvailable:
                // Developer configuration error; check product metadata.
                Debug.Log("No products available for purchase!");
                break;
        }

        m_OnInitialized?.Invoke(false);
    }

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    public void Init(List<string> pIapProductIds, Action<bool> pOnInitiliazed)
    {
        m_OnInitialized = pOnInitiliazed;

        var module = StandardPurchasingModule.Instance();
        var builder = ConfigurationBuilder.Instance(module);
        m_IsGooglePlayStoreSelected = Application.platform == RuntimePlatform.Android && module.appStore == AppStore.GooglePlay;

        // Define our products.
        // Either use the Unity IAP Catalog, or manually use the ConfigurationBuilder.AddProduct API.
        // Use IDs from both the Unity IAP Catalog and hardcoded IDs via the ConfigurationBuilder.AddProduct API.
        for (int i = 0; i < pIapProductIds.Count; i++)
            builder.AddProduct(pIapProductIds[i], ProductType.Consumable);

        // Use the products defined in the IAP Catalog GUI.
        // E.g. Menu: "Window" > "Unity IAP" > "IAP Catalog", then add products, then click "App Store Export".
        var catalog = ProductCatalog.LoadDefaultCatalog();
        foreach (var product in catalog.allValidProducts)
        {
            if (product.allStoreIDs.Count > 0)
            {
                var ids = new IDs();
                foreach (var storeID in product.allStoreIDs)
                    ids.Add(storeID.id, storeID.store);
                builder.AddProduct(product.id, product.type, ids);
            }
            else
                builder.AddProduct(product.id, product.type);
        }

#if INTERCEPT_PROMOTIONAL_PURCHASES
        // On iOS and tvOS we can intercept promotional purchases that come directly from the App Store.
        // On other platforms this will have no effect; OnPromotionalPurchase will never be called.
        builder.Configure<IAppleConfiguration>().SetApplePromotionalPurchaseInterceptorCallback(OnPromotionalPurchase);
        Debug.Log("Setting Apple promotional purchase interceptor callback");
#endif

#if RECEIPT_VALIDATION
        string appIdentifier;
#if UNITY_5_6_OR_NEWER
        appIdentifier = Application.identifier;
#else
        appIdentifier = Application.bundleIdentifier;
#endif
        try
        {
            m_Validator = new CrossPlatformValidator(GooglePlayTangle.Data(), AppleTangle.Data(), appIdentifier);
        }
        catch (NotImplementedException exception)
        {
            Debug.Log("Cross Platform Validator Not Implemented: " + exception);
        }
#endif

        // Now we're ready to initialize Unity IAP.
        UnityPurchasing.Initialize(this, builder);
    }

    /// <summary>
    /// This will be called after a call to IAppleExtensions.RestoreTransactions().
    /// </summary>
    private void OnTransactionsRestored(bool success)
    {
        Debug.Log("Transactions restored." + success);
        m_OnRestored?.Invoke(true, null);
    }

    public bool IsProductOwned(string pProductId)
    {
        if (m_Controller == null)
            return false;

        bool isValid = false;
        var product = m_Controller.products.WithID(pProductId);
        if (product.hasReceipt)
            isValid = ValidateReceipt(product.receipt);
        return isValid;
    }

    /// <summary>
    /// Validates the receipt. Works with receipts from Apple stores and Google Play store only.
    /// Always returns true for other stores.
    /// </summary>
    /// <returns><c>true</c>, if the receipt is valid, <c>false</c> otherwise.</returns>
    /// <param name="receipt">Receipt.</param>
    /// <param name="logReceiptContent">If set to <c>true</c> log receipt content.</param>
    private bool ValidateReceipt(string receipt, bool logReceiptContent = false)
    {
        // Does the receipt has some content?
        if (string.IsNullOrEmpty(receipt))
        {
            Debug.Log("Receipt Validation: receipt is null or empty.");
            return false;
        }

        bool isValidReceipt = true; // presume validity for platforms with no receipt validation.
                                    // Unity IAP's receipt validation is only available for Apple app stores and Google Play store.   
                                    // Here we populate the secret keys for each platform.
                                    // Note that the code is disabled in the editor for it to not stop the EM editor code (due to ClassNotFound error)
                                    // from recreating the dummy AppleTangle and GoogleTangle classes if they were inadvertently removed.
#if RECEIPT_VALIDATION
        try
        {
            // On Google Play, result has a single product ID.
            // On Apple stores, receipts contain multiple products.
            var result = m_Validator.Validate(receipt);
            if (result == null)
                isValidReceipt = false;
            else
            {
                // For informational purposes, we list the receipt(s)
                if (logReceiptContent)
                {
                    Debug.Log("Receipt contents:");
                    foreach (IPurchaseReceipt productReceipt in result)
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
#else
        isValidReceipt = false;
#endif
        return isValidReceipt;
    }

    /// <summary>
    /// iOS Specific.
    /// This is called as part of Apple's 'Ask to buy' functionality,
    /// when a purchase is requested by a minor and referred to a parent for approval.
    ///
    /// When the purchase is approved or rejected, the normal purchase events
    /// will fire.
    /// </summary>
    /// <param name="item">Item.</param>
    private void OnDeferred(Product item)
    {
        Debug.Log("Purchase deferred: " + item.definition.id);
    }

#if INTERCEPT_PROMOTIONAL_PURCHASES
    private void OnPromotionalPurchase(Product item)
    {
        Debug.Log("Attempted promotional purchase: " + item.definition.id);

        // Promotional purchase has been detected. Handle this event by, e.g. presenting a parental gate.
        // Here, for demonstration purposes only, we will wait five seconds before continuing the purchase.
        CoroutineUtil.StartCoroutine(ContinuePromotionalPurchases());
    }

    private IEnumerator ContinuePromotionalPurchases()
    {
        Debug.Log("Continuing promotional purchases in 5 seconds");
        yield return new WaitForSeconds(5);
        Debug.Log("Continuing promotional purchases now");
        m_AppleExtensions.ContinuePromotionalPurchases(); // iOS and tvOS only; does nothing on Mac
    }
#endif

    public void Purchase(string productID, Action<bool, string> pOnPurchased)
    {
        string message = "";
#if UNITY_EDITOR
        pOnPurchased?.Invoke(true, message);
        return;
#endif
        if (m_PurchaseInProgress == true)
        {
            message = "Please wait, purchase in progress";
            pOnPurchased?.Invoke(false, message);
            Debug.LogError(message);
            return;
        }

        if (m_Controller == null)
        {
            message = "Purchasing is not initialized";
            pOnPurchased?.Invoke(false, message);
            Debug.LogError(message);
            return;
        }

        if (m_Controller.products.WithID(productID) == null)
        {
            message = "No product has id " + productID;
            pOnPurchased?.Invoke(false, message);
            Debug.LogError(message);
            return;
        }

        m_OnPurchased = pOnPurchased;
        m_PurchaseInProgress = true;

#if DEVELOPMENT
        //Sample code how to add accountId in developerPayload to pass it to getBuyIntentExtraParams
        Dictionary<string, string> payload_dictionary = new Dictionary<string, string>();
        payload_dictionary["accountId"] = "Faked account id";
        payload_dictionary["developerPayload"] = "Faked developer payload";
        m_Controller.InitiatePurchase(m_Controller.products.WithID(productID), MiniJson.JsonEncode(payload_dictionary));
#else
        m_Controller.InitiatePurchase(m_Controller.products.WithID(productID));
#endif
    }

    public void Restore(Action<bool, string> pOnRestored)
    {
        if (m_Controller == null)
        {
            string message = "Purchasing is not initialized";
            pOnRestored?.Invoke(false, message);
            Debug.LogError(message);
            return;
        }

        if (!NeedRestoreButton())
        {
            string message = "Couldn't restore IAP purchases: not supported on platform " + Application.platform.ToString();
            Debug.LogError(message);
            pOnRestored?.Invoke(false, message);
            return;
        }

        m_OnRestored = pOnRestored;

        if (m_IsGooglePlayStoreSelected)
            m_GooglePlayStoreExtensions.RestoreTransactions(OnTransactionsRestored);
        else
            m_AppleExtensions.RestoreTransactions(OnTransactionsRestored);
    }

    public bool NeedRestoreButton()
    {
        return Application.platform == RuntimePlatform.IPhonePlayer ||
               Application.platform == RuntimePlatform.OSXPlayer ||
               Application.platform == RuntimePlatform.tvOS ||
               Application.platform == RuntimePlatform.WSAPlayerX86 ||
               Application.platform == RuntimePlatform.WSAPlayerX64 ||
               Application.platform == RuntimePlatform.WSAPlayerARM;
    }

    private void LogProductDefinitions()
    {
        var products = m_Controller.products.all;
        foreach (var product in products)
        {
#if UNITY_5_6_OR_NEWER
            Debug.Log(string.Format("id: {0}\nstore-specific id: {1}\ntype: {2}\nenabled: {3}\n",
                product.definition.id, product.definition.storeSpecificId, product.definition.type.ToString(), product.definition.enabled ? "enabled" : "disabled"));
#else
            Debug.Log(string.Format("id: {0}\nstore-specific id: {1}\ntype: {2}\n", product.definition.id,
                product.definition.storeSpecificId, product.definition.type.ToString()));
#endif
        }
    }
}
