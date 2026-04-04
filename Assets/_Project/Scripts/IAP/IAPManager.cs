// Requires the Unity IAP package: com.unity.purchasing
// Install via Window > Package Manager, then Unity adds UNITY_PURCHASING to Scripting Define Symbols automatically.

using System;
using UnityEngine;
using NeonSerpent.SaveData;
using NeonSerpent.Utilities;

#if UNITY_PURCHASING
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

namespace NeonSerpent.IAP
{
    /// <summary>
    /// Manages in-app purchases via Unity IAP (com.unity.purchasing).
    /// Handles one non-consumable (remove ads) and three consumable coin packs.
    /// Compile-guarded behind UNITY_PURCHASING which is defined automatically
    /// by Unity when the IAP package is installed.
    /// Lives in the Bootstrap scene as a Singleton.
    /// </summary>
#if UNITY_PURCHASING
    public class IAPManager : Singleton<IAPManager>, IStoreListener
#else
    public class IAPManager : Singleton<IAPManager>
#endif
    {
        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------

#if UNITY_PURCHASING
        /// <summary>Fired after the remove_ads purchase is successfully processed.</summary>
        public event Action OnAdsRemoved;
#endif

        /// <summary>Fired after a coin pack purchase is processed. Argument is the coin amount granted.</summary>
        public event Action<int> OnCoinsGranted;

        // -------------------------------------------------------------------------
        // Coin amounts — kept here rather than spread across the switch statement
        // -------------------------------------------------------------------------

        private const int COINS_SMALL  = 100;
        private const int COINS_MEDIUM = 500;
        private const int COINS_LARGE  = 2000;

        // -------------------------------------------------------------------------
        // Private state
        // -------------------------------------------------------------------------

#if UNITY_PURCHASING
        private IStoreController   _storeController;
        private IExtensionProvider _extensionProvider;
#endif

        private bool IsInitialized
        {
            get
            {
#if UNITY_PURCHASING
                return _storeController != null && _extensionProvider != null;
#else
                return false;
#endif
            }
        }

        // -------------------------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------------------------

        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();
            InitializePurchasing();
        }

        // -------------------------------------------------------------------------
        // Initialization
        // -------------------------------------------------------------------------

        /// <summary>
        /// Registers all product IDs with the Unity IAP ConfigurationBuilder and
        /// starts the initialization process. Called once during Awake.
        /// </summary>
        private void InitializePurchasing()
        {
#if UNITY_PURCHASING
            if (IsInitialized)
            {
                Debug.Log("[IAP] Already initialized.");
                return;
            }

            var module  = StandardPurchasingModule.Instance();
            var builder = ConfigurationBuilder.Instance(module);

            builder.AddProduct(Constants.IAP_REMOVE_ADS,    ProductType.NonConsumable);
            builder.AddProduct(Constants.IAP_COINS_SMALL,   ProductType.Consumable);
            builder.AddProduct(Constants.IAP_COINS_MEDIUM,  ProductType.Consumable);
            builder.AddProduct(Constants.IAP_COINS_LARGE,   ProductType.Consumable);

            UnityPurchasing.Initialize(this, builder);
            Debug.Log("[IAP] Initialization started.");
#else
            Debug.Log("[IAP] UNITY_PURCHASING not defined — IAP running in stub mode. " +
                      "Install com.unity.purchasing via Package Manager.");
#endif
        }

        // -------------------------------------------------------------------------
        // Public Purchase API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Initiates a purchase for the given product ID. Call this from UI button handlers.
        /// No-op if the store is not initialized.
        /// </summary>
        /// <param name="productId">One of the Constants.IAP_* string values.</param>
        public void BuyProduct(string productId)
        {
#if UNITY_PURCHASING
            if (!IsInitialized)
            {
                Debug.LogWarning("[IAP] BuyProduct called before store initialized.");
                return;
            }

            var product = _storeController.products.WithID(productId);
            if (product != null && product.availableToPurchase)
            {
                Debug.Log($"[IAP] Initiating purchase: {productId}");
                _storeController.InitiatePurchase(product);
            }
            else
            {
                Debug.LogWarning($"[IAP] Product not available: {productId}");
            }
#else
            Debug.Log($"[IAP] BuyProduct stub — productId={productId}");
#endif
        }

        /// <summary>
        /// Restores non-consumable purchases. Required on iOS; logs unsupported on Android
        /// since Google Play restores automatically on reinstall.
        /// </summary>
        public void RestorePurchases()
        {
#if UNITY_PURCHASING
            if (!IsInitialized)
            {
                Debug.LogWarning("[IAP] RestorePurchases called before store initialized.");
                return;
            }

#if UNITY_IOS
            var appleExtensions = _extensionProvider.GetExtension<IAppleExtensions>();
            appleExtensions.RestoreTransactions(success =>
            {
                Debug.Log(success
                    ? "[IAP] iOS restore transactions succeeded."
                    : "[IAP] iOS restore transactions failed.");
            });
#else
            Debug.Log("[IAP] RestorePurchases: Android restores automatically via Google Play. No action required.");
#endif
#else
            Debug.Log("[IAP] RestorePurchases stub — UNITY_PURCHASING not defined.");
#endif
        }

        // -------------------------------------------------------------------------
        // IStoreListener Implementation
        // -------------------------------------------------------------------------

#if UNITY_PURCHASING

        /// <summary>
        /// Called by Unity IAP when the store initializes successfully.
        /// Stores the controller and extension provider for later use.
        /// </summary>
        /// <param name="controller">Provides access to available products and purchase flow.</param>
        /// <param name="extensions">Provides platform-specific extensions (e.g. IAppleExtensions).</param>
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _storeController   = controller;
            _extensionProvider = extensions;
            Debug.Log("[IAP] Store initialized successfully.");
        }

        /// <summary>
        /// Called by Unity IAP when the store fails to initialize.
        /// </summary>
        /// <param name="error">The reason initialization failed.</param>
        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogWarning($"[IAP] Store initialization failed: {error}");
        }

        /// <summary>
        /// Called by Unity IAP when the store fails to initialize, with a descriptive message.
        /// </summary>
        /// <param name="error">The reason initialization failed.</param>
        /// <param name="message">Additional context provided by the platform.</param>
        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogWarning($"[IAP] Store initialization failed: {error} — {message}");
        }

        /// <summary>
        /// Called by Unity IAP to process a completed purchase. Applies the purchase effect,
        /// updates persistent data, and fires the appropriate event.
        /// </summary>
        /// <param name="args">Contains the purchased product details.</param>
        /// <returns><see cref="PurchaseProcessingResult.Complete"/> after processing.</returns>
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            string productId = args.purchasedProduct.definition.id;
            Debug.Log($"[IAP] Processing purchase: {productId}");

            switch (productId)
            {
                case Constants.IAP_REMOVE_ADS:
                    SaveManager.Instance.Data.isAdFree = true;
                    SaveManager.Instance.Save();
                    OnAdsRemoved?.Invoke();
                    Debug.Log("[IAP] Ads removed — isAdFree set to true.");
                    break;

                case Constants.IAP_COINS_SMALL:
                    GrantCoins(COINS_SMALL);
                    break;

                case Constants.IAP_COINS_MEDIUM:
                    GrantCoins(COINS_MEDIUM);
                    break;

                case Constants.IAP_COINS_LARGE:
                    GrantCoins(COINS_LARGE);
                    break;

                default:
                    Debug.LogWarning($"[IAP] Unrecognized product ID in ProcessPurchase: {productId}");
                    break;
            }

            return PurchaseProcessingResult.Complete;
        }

        /// <summary>
        /// Called by Unity IAP when a purchase fails.
        /// </summary>
        /// <param name="product">The product that failed to purchase.</param>
        /// <param name="failureReason">The reason the purchase failed.</param>
        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.LogWarning($"[IAP] Purchase failed — product={product.definition.id}, reason={failureReason}");
        }

#endif // UNITY_PURCHASING

        // -------------------------------------------------------------------------
        // Private Helpers
        // -------------------------------------------------------------------------

        /// <summary>
        /// Adds coins to the player's account via SaveManager.AwardCoins so that
        /// OnCoinsChanged fires and the HUD updates automatically.
        /// </summary>
        /// <param name="amount">Number of coins to grant.</param>
        private void GrantCoins(int amount)
        {
            // Bug F fix: use AwardCoins() instead of direct Data.coins mutation so that
            // SaveManager.OnCoinsChanged fires and any listening HUD refreshes automatically.
            SaveManager.Instance.AwardCoins(amount);
            // Persist immediately — the player paid real money and a crash must not lose their coins.
            SaveManager.Instance?.Save();
            OnCoinsGranted?.Invoke(amount);
            Debug.Log($"[IAP] Granted {amount} coins. Total: {SaveManager.Instance.Data.coins}");
        }
    }
}
