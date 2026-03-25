using System;
using UnityEngine;
using NeonSerpent.SaveData;
using NeonSerpent.Utilities;

namespace NeonSerpent.IAP
{
    /// <summary>
    /// Manages in-app purchases via Unity IAP (com.unity.purchasing).
    /// Handles remove_ads (non-consumable) and coin pack purchases (consumable).
    /// TODO: Initialize Unity IAP and implement IStoreListener after adding the package.
    /// Lives in Bootstrap scene as a Singleton.
    /// </summary>
    public class IAPManager : Singleton<IAPManager>
    {
        public event Action    OnAdsRemoved;
        public event Action<int> OnCoinsGranted;

        // Called by Unity IAP after a successful purchase
        public void ProcessPurchase(string productId, int coinAmount = 0)
        {
            switch (productId)
            {
                case Constants.IAP_REMOVE_ADS:
                    SaveManager.Instance.Data.isAdFree = true;
                    SaveManager.Instance.Save();
                    OnAdsRemoved?.Invoke();
                    Debug.Log("[IAP] Ads removed.");
                    break;

                case Constants.IAP_COINS_SMALL:
                case Constants.IAP_COINS_MEDIUM:
                case Constants.IAP_COINS_LARGE:
                    SaveManager.Instance.Data.coins += coinAmount;
                    SaveManager.Instance.Save();
                    OnCoinsGranted?.Invoke(coinAmount);
                    Debug.Log($"[IAP] Granted {coinAmount} coins.");
                    break;

                default:
                    Debug.LogWarning($"[IAP] Unknown product: {productId}");
                    break;
            }
        }

        /// <summary>Restore non-consumable purchases (required by app stores).</summary>
        public void RestorePurchases()
        {
            // TODO: Call Unity IAP's IExtensionProvider.GetExtension<IAppleExtensions>().RestoreTransactions()
            Debug.Log("[IAP] RestorePurchases placeholder — wire up Unity IAP.");
        }
    }
}
