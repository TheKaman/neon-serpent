using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeonSerpent.Ads;
using NeonSerpent.Core;
using NeonSerpent.IAP;
using NeonSerpent.SaveData;
using NeonSerpent.Utilities;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Shop scene root UI controller.
    /// Displays the player's coin balance, a scrollable grid of skin slots, and a
    /// Remove Ads purchase button. Skins are defined directly in the Inspector via the
    /// <see cref="SkinItem"/> array — no ScriptableObject dependency at this stage.
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        #region SkinItem Definition

        /// <summary>
        /// Lightweight inline skin definition. Configure the array in the Inspector.
        /// A dedicated ScriptableObject catalogue can replace this later without
        /// changing any downstream logic.
        /// </summary>
        [Serializable]
        public struct SkinItem
        {
            public string Id;
            public string DisplayName;
            public int    CoinCost;
            public Sprite PreviewSprite;
        }

        #endregion
        // -------------------------------------------------------------------------
        #region Inspector Fields

        [Header("Coin Display")]
        [SerializeField] private TMP_Text _coinBalanceText;

        [Header("Skin Grid")]
        [SerializeField] private SkinItem[]    _skins;
        [SerializeField] private Transform     _skinGridContainer;
        [SerializeField] private SkinSlotUI    _skinSlotPrefab;

        [Header("Remove Ads")]
        [SerializeField] private Button   _removeAdsBtn;
        [SerializeField] private TMP_Text _removeAdsBtnText;

        [Header("Watch Ad for Coins")]
        [Tooltip("Optional. Create the button in the Shop scene and assign it here. " +
                 "If left null, the watch-ad feature is simply absent — no error.")]
        [SerializeField] private Button   _watchAdBtn;
        [SerializeField] private TMP_Text _watchAdBtnText;

        [Header("Navigation")]
        [SerializeField] private Button _backBtn;

        #endregion
        // -------------------------------------------------------------------------
        #region Private State

        // Tracks the temporary "purchases unavailable" message shown on the Remove Ads
        // button so it can be cancelled cleanly on disable (project rule: every
        // StartCoroutine stores its handle and is stopped on cleanup).
        private Coroutine _buttonMessageCoroutine;

        #endregion
        // -------------------------------------------------------------------------
        #region Unity Lifecycle

        private void OnEnable()
        {
#if UNITY_PURCHASING
            if (IAPManager.Instance != null)
                IAPManager.Instance.OnAdsRemoved += HandleAdsRemoved;
#endif
        }

        private void OnDisable()
        {
#if UNITY_PURCHASING
            if (IAPManager.Instance != null)
                IAPManager.Instance.OnAdsRemoved -= HandleAdsRemoved;
#endif
            if (_buttonMessageCoroutine != null)
            {
                StopCoroutine(_buttonMessageCoroutine);
                _buttonMessageCoroutine = null;
            }
        }

        private void Start()
        {
            _removeAdsBtn?.onClick.AddListener(OnRemoveAdsBtn);
            _watchAdBtn?.onClick.AddListener(OnWatchAdBtn);
            _backBtn?.onClick.AddListener(OnBackBtn);

            RefreshCoins();
            PopulateSkinGrid();
            RefreshRemoveAdsButton();
            RefreshWatchAdButton();
        }

        #endregion
        // -------------------------------------------------------------------------
        #region Public API

        /// <summary>
        /// Updates the coin balance label from the current save data.
        /// Call this after any transaction that changes the coin count.
        /// </summary>
        public void RefreshCoins()
        {
            if (_coinBalanceText == null) return;
            int coins = SaveManager.Instance != null ? SaveManager.Instance.Data.coins : 0;
            _coinBalanceText.text = $"{coins:N0} coins";
        }

        /// <summary>
        /// Handles a skin selection from any <see cref="SkinSlotUI"/> slot.
        /// If the skin is unlocked, equips it immediately. If locked and the player
        /// has enough coins, purchases and equips it. Otherwise logs a "not enough
        /// coins" message.
        /// </summary>
        /// <param name="skinId">The ID of the skin the player tapped.</param>
        public void OnSkinSelected(string skinId)
        {
            if (SaveManager.Instance == null) return;

            PlayerData data = SaveManager.Instance.Data;
            bool isUnlocked = data.unlockedSkinIds.Contains(skinId);

            if (isUnlocked)
            {
                EquipSkin(skinId, data);
                return;
            }

            // Find cost for this skin.
            int cost = GetSkinCost(skinId);

            if (data.coins >= cost)
            {
                // Bug F fix: use DeductCoins() so SaveManager.OnCoinsChanged fires and
                // any HUD listening to that event (including this screen's RefreshCoins
                // subscriber) updates automatically. Direct data.coins mutation bypassed it.
                // DeductCoins already calls Save() internally, so no extra Save() needed here.
                SaveManager.Instance.DeductCoins(cost);
                data.unlockedSkinIds.Add(skinId);
                EquipSkin(skinId, data); // EquipSkin calls Save() for the skin equip
                RefreshCoins();
                // Rebuild the grid so the newly unlocked slot updates its state.
                PopulateSkinGrid();
            }
            else
            {
                Debug.Log($"[ShopUI] Not enough coins to purchase skin '{skinId}'. " +
                          $"Cost: {cost}, Balance: {data.coins}");
            }
        }

        #endregion
        // -------------------------------------------------------------------------
        #region Button Handlers

        /// <summary>Initiates the Remove Ads IAP purchase flow.</summary>
        private void OnRemoveAdsBtn()
        {
#if UNITY_PURCHASING
            if (IAPManager.Instance == null)
            {
                Debug.LogWarning("[ShopUI] IAPManager not available — cannot purchase Remove Ads.");
                return;
            }

            // Do nothing if the player has already removed ads (button should be hidden,
            // but guard here in case visibility state is stale).
            if (SaveManager.Instance != null && SaveManager.Instance.Data.isAdFree)
                return;

            IAPManager.Instance.BuyProduct(Constants.IAP_REMOVE_ADS);
#else
            // Without the Unity IAP package the purchase cannot proceed. The player still
            // tapped a visible button, so we MUST give visible feedback — a silent log is
            // a store-policy and UX failure (player taps, nothing happens). Surface a brief
            // message on the button itself rather than failing silently.
            Debug.LogWarning("[ShopUI] Unity IAP package not installed — Remove Ads purchase unavailable.");
            ShowButtonMessage(_removeAdsBtnText,
                "In-app purchases are currently unavailable. Please try again later.");
#endif
        }

        /// <summary>
        /// Plays a rewarded ad and, on a verified completion, awards coins and refreshes
        /// the balance. No-op for ad-free players (they already paid for an ad-free
        /// experience and should use a coin-pack IAP instead). AdManager re-checks the
        /// ad-free guard internally, so the reward can never be granted to an ad-free player.
        /// </summary>
        private void OnWatchAdBtn()
        {
            bool adFree = SaveManager.Instance != null && SaveManager.Instance.Data.isAdFree;
            if (adFree)
            {
                // Defensive: the button should already be hidden for ad-free players via
                // RefreshWatchAdButton(), but guard here in case visibility state is stale.
                ShowButtonMessage(_watchAdBtnText, "You already have an ad-free experience.");
                return;
            }

            if (AdManager.Instance == null)
            {
                Debug.LogWarning("[ShopUI] AdManager not available — cannot show rewarded ad.");
                ShowButtonMessage(_watchAdBtnText, "Ads are unavailable right now. Please try again later.");
                return;
            }

            AdManager.Instance.ShowRewardedAd(coins =>
            {
                // Fired on the main thread only on a verified earn (not on skip/close).
                SaveManager.Instance?.AwardCoins(coins);
                SaveManager.Instance?.Save();   // flush immediately — a crash before the next auto-save would silently lose the earned coins
                RefreshCoins();
            });
        }

        /// <summary>Navigates back to the Main Menu scene.</summary>
        private void OnBackBtn()
        {
            AdManager.Instance?.OnReturnToMenu();
            SceneLoader.Instance?.LoadScene(Constants.SCENE_MAIN_MENU);
        }

        #endregion
        // -------------------------------------------------------------------------
        #region Event Handlers

#if UNITY_PURCHASING
        /// <summary>
        /// Called when <see cref="IAPManager.OnAdsRemoved"/> fires after a successful purchase.
        /// Hides the Remove Ads button since it is no longer relevant.
        /// </summary>
        private void HandleAdsRemoved()
        {
            _removeAdsBtn?.gameObject.SetActive(false);
        }
#endif

        #endregion
        // -------------------------------------------------------------------------
        #region Helpers

        private void PopulateSkinGrid()
        {
            if (_skinGridContainer == null || _skinSlotPrefab == null) return;

            // Clear existing slots before repopulating.
            for (int i = _skinGridContainer.childCount - 1; i >= 0; i--)
                Destroy(_skinGridContainer.GetChild(i).gameObject);

            if (_skins == null) return;

            PlayerData data = SaveManager.Instance?.Data;

            foreach (SkinItem skin in _skins)
            {
                bool isUnlocked = data != null && data.unlockedSkinIds.Contains(skin.Id);
                bool isEquipped = data != null && data.equippedSkinId == skin.Id;

                SkinSlotUI slot = Instantiate(_skinSlotPrefab, _skinGridContainer);
                slot.Populate(skin, isUnlocked, isEquipped, OnSkinSelected);
            }
        }

        private void RefreshRemoveAdsButton()
        {
            if (_removeAdsBtn == null) return;
            bool adFree = SaveManager.Instance != null && SaveManager.Instance.Data.isAdFree;
            _removeAdsBtn.gameObject.SetActive(!adFree);
        }

        /// <summary>
        /// Hides the Watch Ad button for ad-free players (same visibility rule as Remove Ads).
        /// Ad-free players do not receive the watch-ad coin reward by design.
        /// </summary>
        private void RefreshWatchAdButton()
        {
            if (_watchAdBtn == null) return;
            bool adFree = SaveManager.Instance != null && SaveManager.Instance.Data.isAdFree;
            _watchAdBtn.gameObject.SetActive(!adFree);
        }

        /// <summary>
        /// Briefly replaces a button's label with <paramref name="message"/>, then restores
        /// the original text. Used to surface feedback for actions that cannot complete
        /// (e.g. IAP unavailable) so the player never taps into silence. Safe with a null label.
        /// </summary>
        private void ShowButtonMessage(TMP_Text label, string message)
        {
            if (label == null) return;
            if (_buttonMessageCoroutine != null) StopCoroutine(_buttonMessageCoroutine);
            _buttonMessageCoroutine = StartCoroutine(ButtonMessageRoutine(label, message));
        }

        private IEnumerator ButtonMessageRoutine(TMP_Text label, string message)
        {
            string original = label.text;
            label.text = message;
            yield return new WaitForSecondsRealtime(2.5f);
            label.text = original;
            _buttonMessageCoroutine = null;
        }

        private void EquipSkin(string skinId, PlayerData data)
        {
            data.equippedSkinId = skinId;
            SaveManager.Instance.Save();
            Debug.Log($"[ShopUI] Equipped skin '{skinId}'.");
            PopulateSkinGrid(); // Refresh equipped badge state.
        }

        private int GetSkinCost(string skinId)
        {
            if (_skins == null) return int.MaxValue;
            foreach (SkinItem skin in _skins)
            {
                if (skin.Id == skinId) return skin.CoinCost;
            }
            return int.MaxValue;
        }

        #endregion
    }
}
