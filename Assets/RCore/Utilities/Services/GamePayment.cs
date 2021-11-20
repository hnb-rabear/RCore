//#define UNITY_IAP

using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Networking;
using RCore.Common;
using Debug = UnityEngine.Debug;
#if UNITY_IAP
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
#endif

namespace RCore.Service
{
#if UNITY_IAP
    [Obsolete("Removed because I no longer work for RedAntz")]
    public class GamePayment : MonoBehaviour, IStoreListener
    {
        public enum ValidationResult
        {
            OK,
            WrongRequest,
            ServerError,
            ResultError,
            InvalidValue,
            PackageNotFound
        }

    #region Members

        private static GamePayment mInstance;
        public static GamePayment Instance => mInstance;

        private Action<bool> mOnPurchased;
        /// <summary>
        /// [Apple store only] Occurs when the (non-consumable and subscription) 
        /// </summary>
        private Action<bool> mOnRestored;
        private List<string> mIAPProducts;
        private Action<bool> mOnInitialized;

        private IStoreController mStoreController;
        private IExtensionProvider mStoreExtensionProvider;
        private bool mIsValidatingProduct;
        private IAppleExtensions mAppleExtensions;
        private IGooglePlayStoreExtensions mGooglePlayStoreExtensions;
        /// <summary>
        /// Set all these products to be visible in the user's App Store according to Apple's Promotional IAP feature
        /// </summary>
        private bool mInterceptPromotionalPurchase = true;
        public bool Initialized => mStoreController != null && mStoreExtensionProvider != null;

    #endregion

        //=============================================

    #region MonoBehaviour

        private void Awake()
        {
            if (mInstance == null)
                mInstance = this;
            else if (mInstance != this)
                Destroy(gameObject);
        }

    #endregion

        //=============================================

    #region Public

        public void Init(List<string> pIapProductIds, bool pInterceptPromotionalPurchase, Action<bool> pOnFinished)
        {
            mIAPProducts = pIapProductIds;
            mInterceptPromotionalPurchase = pInterceptPromotionalPurchase;
            mOnInitialized = pOnFinished;

            var module = StandardPurchasingModule.Instance();
            var builder = ConfigurationBuilder.Instance(module);
            for (int i = 0; i < mIAPProducts.Count; i++)
                builder.AddProduct(mIAPProducts[i], ProductType.Consumable);

            UnityPurchasing.Initialize(this, builder);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            Debug.Log("GamePayment Initialized");

            mStoreController = controller;
            mStoreExtensionProvider = extensions;
            mAppleExtensions = extensions.GetExtension<IAppleExtensions>();
            mGooglePlayStoreExtensions = extensions.GetExtension<IGooglePlayStoreExtensions>();

            if (mAppleExtensions != null)
            {
                // On Apple platforms we need to handle deferred purchases caused by Apple's Ask to Buy feature.
                // On non-Apple platforms this will have no effect; OnDeferred will never be called.
                mAppleExtensions.RegisterPurchaseDeferredListener(OnApplePurchaseDeferred);
                // Enables or disables the Apple's Ask-To-Buy simulation in the sandbox app store.
                mAppleExtensions.simulateAskToBuy = true;
            }

            foreach (var item in controller.products.all)
            {
                if (item.availableToPurchase)
                {
#if UNITY_EDITOR || DEVELOPMENT
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
#endif

                    // Set all these products to be visible in the user's App Store according to Apple's Promotional IAP feature
                    // https://developer.apple.com/library/content/documentation/NetworkingInternet/Conceptual/StoreKitGuide/PromotingIn-AppPurchases/PromotingIn-AppPurchases.html
                    if (mInterceptPromotionalPurchase)
                    {
                        mAppleExtensions.SetStorePromotionVisibility(item, AppleStorePromotionVisibility.Show);
                    }
                }
            }

            mOnInitialized?.Invoke(true);
        }

        /// <summary>
        /// iOS Specific.
        /// This is called as part of Apple's 'Ask to buy' functionality,
        /// when a purchase is requested by a minor and referred to a parent for approval.
        /// When the purchase is approved or rejected, the normal purchase events
        /// will fire.
        /// </summary>
        /// <param name="item">Item.</param>
        private void OnApplePurchaseDeferred(Product pProduct)
        {
            Debug.Log("Purchase deferred: " + pProduct.definition.id);
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.Log($"GamePayment Initialize Failed {error}");
            mOnInitialized?.Invoke(false);
        }

        public void OnPurchaseFailed(Product pProduct, PurchaseFailureReason pReason)
        {
            Debug.Log($"GamePayment Purchase Failed product: {pProduct.definition.storeSpecificId} \n reason {pReason.ToString()}");
        }

        public decimal GetLocalizedPrice(string pPackageId)
        {
            try
            {
                var product = mStoreController.products.WithID(pPackageId);
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
                var product = mStoreController.products.WithID(pPackageId);
                if (product != null)
                    return product.metadata.localizedPriceString;
                return "";
            }
            catch { return ""; }
        }

        //Purchase step 2: Start Validation
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
        {
#if UNITY_EDITOR
            if (mOnPurchased != null)
                mOnPurchased(true);
            return PurchaseProcessingResult.Complete;
#elif UNITY_ANDROID
            Verify(e.purchasedProduct);
            return PurchaseProcessingResult.Pending;
#else
            if (mOnPurchased != null)
                mOnPurchased(true);
            return PurchaseProcessingResult.Complete;
#endif
        }

        //Purchase step 1
        public void Purchase(string pPackageId, Action<bool> pOnPurchased)
        {
            mOnPurchased = pOnPurchased;
#if UNITY_EDITOR
            if (mOnPurchased != null)
                mOnPurchased(true);
#else
            if (mIsValidatingProduct)
            {
                Debug.LogWarning("Server Is Busy");
                if (mOnPurchased != null)
                    mOnPurchased(false);
            }
            else
            {
                if (!Initialized)
                {
                    Debug.LogError("GamePayment is not initialized");
                    if (mOnPurchased != null)
                        mOnPurchased(false);
                    return;
                }

                var product = mStoreController.products.WithID(pPackageId);
                if (product != null && product.availableToPurchase)
                {
                    Debug.Log($"Buying product {product.definition.id}!");
                    mStoreController.InitiatePurchase(product);
                }
                else
                {
                    Debug.LogError($"Product {pPackageId} is not available!");
                    if (mOnPurchased != null)
                        mOnPurchased(false);
                }
            }
#endif
        }

        /// <summary>
        /// Apple normally requires a Restore Purchases button to exist in your game.
        /// On other platforms, e.g.Google Play, the restoration is done automatically during the first initialization after reinstallation
        /// </summary>
        public void Restore(Action<bool> pOnRestored)
        {
            if (!Initialized)
            {
                Debug.Log("GamePayment is not initialized.");
                return;
            }

            mOnRestored = pOnRestored;

            if (Application.platform == RuntimePlatform.IPhonePlayer ||
                Application.platform == RuntimePlatform.OSXPlayer)
            {
                mAppleExtensions.RestoreTransactions((success) =>
                {
                    Debug.Log("Transactions restored." + success);
                    mOnRestored?.Invoke(success);
                });
            }
            else
            {
                // We are not running on an Apple device. No work is necessary to restore purchases.
                Debug.Log("Couldn't restore IAP purchases: not supported on platform " + Application.platform.ToString());
            }
        }

        public bool IsProductOwned(string pProductId)
        {
            if (!Initialized)
                return false;

            bool isValid = false;
            var product = mStoreController.products.WithID(pProductId);
            if (product.hasReceipt)
            {
                IPurchaseReceipt[] purchaseReceipts;
                isValid = ValidateReceipt(product.receipt, out purchaseReceipts);
            }
            return isValid;
        }

        public SubscriptionInfo GetSubscriptionInfo(string pProductId)
        {
            Product prod = mStoreController.products.WithID(pProductId);

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

            if (!IsProductAvailableForSubscriptionManager(prod.receipt))
            {
                Debug.Log("Couldn't get subscription info: this product is not available for SubscriptionManager class, " +
                    "only products that are purchase by 1.19+ SDK can use this class.");
                return null;
            }

            //Get the subscription info using SubscriptionManager class
            Dictionary<string, string> introPriceDict = null;

            if (mAppleExtensions != null)
                introPriceDict = mAppleExtensions.GetIntroductoryPriceDictionary();

            string introJson = (introPriceDict == null || !introPriceDict.ContainsKey(prod.definition.storeSpecificId)) ?
                       null : introPriceDict[prod.definition.storeSpecificId];

            SubscriptionManager p = new SubscriptionManager(prod, introJson);
            return p.getSubscriptionInfo();
        }

    #endregion

        //==============================================

    #region Private

        //Purchase Step 4: Send result
        private void OnIAPVerified(ValidationResult pResult, Product pProduct)
        {
            switch (pResult)
            {
                case ValidationResult.OK:
                    if (mOnPurchased != null) mOnPurchased(true);
                    mStoreController.ConfirmPendingPurchase(pProduct);
                    break;
                case ValidationResult.InvalidValue:
                case ValidationResult.ResultError:
                case ValidationResult.PackageNotFound:
                case ValidationResult.ServerError:
                case ValidationResult.WrongRequest:
                    if (mOnPurchased != null) mOnPurchased(false);
                    break;
            }
        }

        //Purchase Step 3: Remotely Verify Product 
        private void Verify(Product pProduct)
        {
#if UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_TVOS
            var validator = new CrossPlatformValidator(GooglePlayTangle.Data(), AppleTangle.Data(), Application.identifier);
            try
            {
                // On Google Play, result has a single product ID.
                // On Apple stores, receipts contain multiple products.
                var result = validator.Validate(pProduct.receipt);
                // For informational purposes, we list the receipt(s)
                foreach (IPurchaseReceipt productReceipt in result)
                {
                    GooglePlayReceipt google = productReceipt as GooglePlayReceipt;
                    if (null != google)
                    {
                        StartCoroutine(IERequestVerifyingProduct(Application.identifier, google.productID, google.purchaseToken));
                        return;
                    }
                }
            }
            catch (IAPSecurityException e)
            {
                Debug.LogError(e.ToString());
            }
#endif
            OnIAPVerified(ValidationResult.ServerError, pProduct);
        }

        //Purchase Step 3.5: Send request to server
        private IEnumerator IERequestVerifyingProduct(string pAppPackage, string pProductId, string pPurchaseToken)
        {
            mIsValidatingProduct = true;

            WWWForm form = new WWWForm();
            form.AddField("package", Application.identifier, System.Text.Encoding.UTF8);
            form.AddField("sku", pProductId, System.Text.Encoding.UTF8);
            form.AddField("token", pPurchaseToken, System.Text.Encoding.UTF8);
            UnityWebRequest www = UnityWebRequest.Post("http://divmob.com/api/iab/gps/verify.php", form);

            yield return www.SendWebRequest();

            mIsValidatingProduct = false;
            var product = mStoreController.products.WithID(pProductId);

            if (www.isNetworkError)
            {
                OnIAPVerified(ValidationResult.ServerError, product);
            }
            else
            {
                try
                {
                    string data = www.downloadHandler.text;
                    var result = JsonUtility.FromJson<IAPVerifyResult>(data);
                    string sku = result.sku;
                    if (sku != null)
                    {
                        OnIAPVerified(ValidationResult.OK, product);
                    }
                    else
                    {
                        switch (result.error)
                        {
                            case 1:
                            case 2:
                                OnIAPVerified(ValidationResult.ResultError, product);
                                break;
                            case 3:
                                OnIAPVerified(ValidationResult.InvalidValue, product);
                                break;
                            case 4:
                                OnIAPVerified(ValidationResult.PackageNotFound, product);
                                break;
                            case 99:
                                OnIAPVerified(ValidationResult.WrongRequest, product);
                                break;
                            default:
                                OnIAPVerified(ValidationResult.ServerError, product);
                                break;
                        }
                    }
                }
                catch
                {
                    OnIAPVerified(ValidationResult.ServerError, product);
                }
            }
        }

        /// <summary>
        /// Validates the receipt. Works with receipts from Apple stores and Google Play store only.
        /// Always returns true for other stores.
        /// </summary>
        /// <returns><c>true</c>, if the receipt is valid, <c>false</c> otherwise.</returns>
        /// <param name="receipt">Receipt.</param>
        /// <param name="logReceiptContent">If set to <c>true</c> log receipt content.</param>
        private static bool ValidateReceipt(string receipt, out IPurchaseReceipt[] purchaseReceipts, bool logReceiptContent = false)
        {
            purchaseReceipts = new IPurchaseReceipt[0];   // default the out parameter to an empty array   

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
                    purchaseReceipts = result;

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
#endif

            return isValidReceipt;
        }

        private bool IsProductAvailableForSubscriptionManager(string receipt)
        {
            var receipt_wrapper = (Dictionary<string, object>)MiniJson.JsonDecode(receipt);
            if (!receipt_wrapper.ContainsKey("Store") || !receipt_wrapper.ContainsKey("Payload"))
            {
                Debug.Log("The product receipt does not contain enough information, " +
                    "the 'Store' or 'Payload' field is missing.");
                return false;
            }

            var store = (string)receipt_wrapper["Store"];
            var payload = (string)receipt_wrapper["Payload"];

            if (payload != null)
            {
                switch (store)
                {
                    case GooglePlay.Name:
                        {
                            var payload_wrapper = (Dictionary<string, object>)MiniJson.JsonDecode(payload);
                            if (!payload_wrapper.ContainsKey("json"))
                            {
                                Debug.Log("The product receipt does not contain enough information, the 'json' field is missing.");
                                return false;
                            }
                            var original_json_payload_wrapper = (Dictionary<string, object>)MiniJson.JsonDecode((string)payload_wrapper["json"]);
                            if (original_json_payload_wrapper == null || !original_json_payload_wrapper.ContainsKey("developerPayload"))
                            {
                                Debug.Log("The product receipt does not contain enough information, the 'developerPayload' field is missing.");
                                return false;
                            }
                            var developerPayloadJSON = (string)original_json_payload_wrapper["developerPayload"];
                            var developerPayload_wrapper = (Dictionary<string, object>)MiniJson.JsonDecode(developerPayloadJSON);
                            if (developerPayload_wrapper == null || !developerPayload_wrapper.ContainsKey("is_free_trial") || !developerPayload_wrapper.ContainsKey("has_introductory_price_trial"))
                            {
                                Debug.Log("The product receipt does not contain enough information, the product is not purchased using 1.19 or later.");
                                return false;
                            }
                            return true;
                        }
                    case AppleAppStore.Name:
                    case AmazonApps.Name:
                    case MacAppStore.Name:
                        {
                            return true;
                        }
                    default:
                        {
                            return false;
                        }
                }
            }
            return false;
        }

    #endregion

        //==============================================

    #region Internal Class

        [System.Serializable]
        public class IAPVerifyResult
        {
            public string sku = null;
            public int error = -1;
            public string errorMessage;
        }

    #endregion
    }
#else
    public class GamePayment : MonoBehaviour
    {
        public bool Initialized => false;
        private static GamePayment mInstance;
        public static GamePayment Instance => mInstance;
        private void Awake()
        {
            if (mInstance == null)
                mInstance = this;
            else if (mInstance != this)
                Destroy(gameObject);
        }
        public void Init(List<string> pIapProductIds, bool pInterceptPromotionalPurchase, Action<bool> pOnFinished)
        {
        }
        public decimal GetLocalizedPrice(string pPackageId)
        {
            return 0;
        }
        public string GetLocalizedPriceString(string pPackageId)
        {
            return "0";
        }
        public void Purchase(string pPackageId, Action<bool> pOnPurchased)
        {
            if (pOnPurchased != null)
                pOnPurchased(true);
        }
        public void Restore(Action<bool> pOnRestore)
        {
            if (pOnRestore != null)
                pOnRestore(true);
        }
        public bool IsProductOwned(string pProductId)
        {
            return false;
        }
    }
#endif

#if UNITY_EDITOR
    [CustomEditor(typeof(GamePayment))]
    public class GamePaymentEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.BeginVertical("box");
#if UNITY_IAP
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Disable Game Payment"))
                EditorHelper.RemoveDirective("UNITY_IAP");
#else
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Enable Game Payment"))
                EditorHelper.AddDirective("UNITY_IAP");
#endif
            GUILayout.EndVertical();
        }
    }
#endif
}