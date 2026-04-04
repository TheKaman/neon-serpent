// ============================================================================
// SETUP INSTRUCTIONS — Read before enabling this file
// ============================================================================
//
// 1. INSTALL THE PLUGIN
//    Download the Google Mobile Ads Unity Plugin from:
//    https://github.com/googleads/googleads-mobile-unity/releases
//    Import the .unitypackage into this project (Assets > Import Package).
//    The plugin places its files under Assets/GoogleMobileAds/.
//
// 2. ADD THE SCRIPTING DEFINE SYMBOL
//    Project Settings > Player > Other Settings > Scripting Define Symbols
//    Add: GOOGLE_MOBILE_ADS
//    This file compiles only when that symbol is present, so the project
//    builds cleanly before the plugin is installed.
//
// 3. ADD THE APP ID TO ANDROIDMANIFEST.XML
//    The Google Mobile Ads SDK reads the App ID from the Android manifest at
//    runtime — the MobileAds.Initialize() call alone is not enough.
//    In Unity: Edit > Project Settings > Google Mobile Ads > Android App ID
//    Enter: ca-app-pub-7408967267429686~6774004553
//    The plugin writes the <meta-data> element to the manifest automatically.
//    If you are managing AndroidManifest.xml manually, add inside <application>:
//    <meta-data
//        android:name="com.google.android.gms.ads.APPLICATION_ID"
//        android:value="ca-app-pub-7408967267429686~6774004553" />
//
// ============================================================================

using UnityEngine;
using NeonSerpent.SaveData;
using NeonSerpent.Utilities;
using System;

#if GOOGLE_MOBILE_ADS
using GoogleMobileAds.Api;
#endif

namespace NeonSerpent.Ads
{
    /// <summary>
    /// Central ad coordinator using the Google Mobile Ads (AdMob) Unity Plugin.
    /// Handles interstitial and rewarded ad placements with frequency capping.
    /// All public methods guard against ad-free players via <see cref="SaveManager.Instance"/>.
    /// Lives in the Bootstrap scene as a DontDestroyOnLoad Singleton.
    /// </summary>
    /// <remarks>
    /// Compile guard: this file activates only when GOOGLE_MOBILE_ADS is defined in
    /// Scripting Define Symbols. The #else branches provide stub logging so the game
    /// builds and runs cleanly before the plugin is imported.
    /// </remarks>
    public class AdManager : Singleton<AdManager>
    {
        // -------------------------------------------------------------------------
        #region Ad Unit IDs

        // Production ad unit IDs
        private const string PROD_INTERSTITIAL_ID = "ca-app-pub-7408967267429686/3381554455";
        private const string PROD_REWARDED_ID      = "ca-app-pub-7408967267429686/5749734907";

        // Google-provided test ad unit IDs — always safe to use in debug builds
        private const string TEST_INTERSTITIAL_ID  = "ca-app-pub-3940256099942544/1033173712";
        private const string TEST_REWARDED_ID      = "ca-app-pub-3940256099942544/5224354917";

        /// <summary>Returns the interstitial ad unit ID appropriate for this build.</summary>
        private static string InterstitialId => Debug.isDebugBuild ? TEST_INTERSTITIAL_ID : PROD_INTERSTITIAL_ID;

        /// <summary>Returns the rewarded ad unit ID appropriate for this build.</summary>
        private static string RewardedId => Debug.isDebugBuild ? TEST_REWARDED_ID : PROD_REWARDED_ID;

        #endregion
        // -------------------------------------------------------------------------
        #region State

#if GOOGLE_MOBILE_ADS
        private InterstitialAd _interstitialAd;
        private RewardedAd     _rewardedAd;
#endif

        /// <summary>
        /// How many game-over events have occurred since the last interstitial was shown.
        /// An interstitial is shown on every 2nd game over.
        /// </summary>
        private int _gameOverCount;

        /// <summary>Incremented each time the player returns to the main menu.</summary>
        private int _menuReturnCount;

        /// <summary>Incremented each time a campaign level is completed.</summary>
        private int _campaignCompletionCount;

        #endregion
        // -------------------------------------------------------------------------
        #region Public Properties

        /// <summary>
        /// True when the player has purchased ad removal.
        /// GameOverUI and other callers may read this to hide ad-related UI.
        /// </summary>
        public bool IsAdFree => SaveManager.Instance != null && SaveManager.Instance.Data.isAdFree;

        #endregion
        // -------------------------------------------------------------------------
        #region Lifecycle

        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        protected override void OnDestroy()
        {
#if GOOGLE_MOBILE_ADS
            DestroyInterstitial();
            DestroyRewarded();
#endif
        }

        #endregion
        // -------------------------------------------------------------------------
        #region Initialization

        /// <summary>
        /// Initializes the Google Mobile Ads SDK and pre-loads both ad formats.
        /// Uses test ad unit IDs when <c>Debug.isDebugBuild</c> is true so no real
        /// impressions are recorded during development.
        /// </summary>
        private void Initialize()
        {
#if GOOGLE_MOBILE_ADS
            MobileAds.Initialize(status =>
            {
                Debug.Log("[AdManager] Google Mobile Ads SDK initialized.");
                // Callbacks from MobileAds.Initialize are not guaranteed to arrive on
                // the main thread. Use MobileAds.RaiseAdEventsOnUnityMainThread if you
                // need to update UI here; for simple pre-loading it is safe either way.
                LoadInterstitial();
                LoadRewarded();
            });

            Debug.Log($"[AdManager] Initializing. Using {(Debug.isDebugBuild ? "TEST" : "PRODUCTION")} ad units.");
#else
            Debug.Log("[AdManager] GOOGLE_MOBILE_ADS not defined — running in stub mode. " +
                      "Import the Google Mobile Ads Unity Plugin and add GOOGLE_MOBILE_ADS " +
                      "to Project Settings > Player > Scripting Define Symbols.");
#endif
        }

        #endregion
        // -------------------------------------------------------------------------
        #region Interstitial

        /// <summary>
        /// Requests and pre-loads a new interstitial ad.
        /// Called automatically on init and after each interstitial is dismissed.
        /// </summary>
        public void LoadInterstitial()
        {
#if GOOGLE_MOBILE_ADS
            DestroyInterstitial(); // release any existing ad before creating a new request

            var request = new AdRequest();
            InterstitialAd.Load(InterstitialId, request, (ad, error) =>
            {
                if (error != null)
                {
                    Debug.LogWarning($"[AdManager] Interstitial load failed: {error.GetMessage()}");
                    return;
                }

                _interstitialAd = ad;
                RegisterInterstitialCallbacks(_interstitialAd);
                Debug.Log("[AdManager] Interstitial loaded and ready.");
            });
#else
            Debug.Log("[AdManager] LoadInterstitial stub.");
#endif
        }

        /// <summary>
        /// Shows an interstitial ad on every 2nd game-over event.
        /// No-op immediately if the player has purchased ad removal or no ad is loaded.
        /// A new interstitial is requested automatically after the shown ad is dismissed.
        /// </summary>
        public void ShowGameOverInterstitial()
        {
            if (IsAdFree) return;

            _gameOverCount++;
            Debug.Log($"[AdManager] Game-over count: {_gameOverCount}");

            if (_gameOverCount % 2 != 0) return;

#if GOOGLE_MOBILE_ADS
            if (_interstitialAd != null && _interstitialAd.CanShowAd())
            {
                _interstitialAd.Show();
                Debug.Log("[AdManager] Showing game-over interstitial.");
            }
            else
            {
                Debug.LogWarning("[AdManager] Interstitial not ready for game-over — requesting reload.");
                LoadInterstitial();
            }
#else
            Debug.Log("[AdManager] ShowGameOverInterstitial stub.");
#endif
        }

        /// <summary>
        /// Tracks each return to the main menu and shows an interstitial on every 3rd call.
        /// No-op if the player has purchased ad removal.
        /// </summary>
        public void OnReturnToMenu()
        {
            if (IsAdFree) return;

            _menuReturnCount++;
            Debug.Log($"[AdManager] Menu return count: {_menuReturnCount}");

            if (_menuReturnCount % 3 == 0)
                ShowInterstitialIfReady(context: "ReturnToMenu");
        }

        /// <summary>
        /// Tracks each campaign level completion and shows an interstitial on every 2nd call.
        /// No-op if the player has purchased ad removal.
        /// </summary>
        public void OnCampaignLevelComplete()
        {
            if (IsAdFree) return;

            _campaignCompletionCount++;
            Debug.Log($"[AdManager] Campaign completion count: {_campaignCompletionCount}");

            if (_campaignCompletionCount % 2 == 0)
                ShowInterstitialIfReady(context: "CampaignLevelComplete");
        }

        /// <summary>
        /// Shows a pre-loaded interstitial immediately if one is available,
        /// otherwise requests a fresh load.
        /// </summary>
        /// <param name="context">Descriptive label used only in log messages.</param>
        private void ShowInterstitialIfReady(string context)
        {
#if GOOGLE_MOBILE_ADS
            if (_interstitialAd != null && _interstitialAd.CanShowAd())
            {
                _interstitialAd.Show();
                Debug.Log($"[AdManager] Showing interstitial for: {context}");
            }
            else
            {
                Debug.LogWarning($"[AdManager] Interstitial not ready for: {context}. Requesting load.");
                LoadInterstitial();
            }
#else
            Debug.Log($"[AdManager] ShowInterstitialIfReady stub — context={context}");
#endif
        }

        private void DestroyInterstitial()
        {
#if GOOGLE_MOBILE_ADS
            if (_interstitialAd != null)
            {
                _interstitialAd.Destroy();
                _interstitialAd = null;
            }
#endif
        }

        #endregion
        // -------------------------------------------------------------------------
        #region Banner Ads

        /// <summary>
        /// Stub — NeonSerpent does not use banner ads.
        /// Banner placement calls in UI scripts compile against this method but
        /// produce no ad traffic. Implement with a real banner ad unit if banners
        /// are added to the monetisation plan in a future update.
        /// </summary>
        public void ShowBanner()
        {
            Debug.Log("[AdManager] ShowBanner called — banner ads are not implemented in NeonSerpent.");
        }

        /// <summary>
        /// Stub — NeonSerpent does not use banner ads.
        /// Counterpart to <see cref="ShowBanner"/>. Safe to call; does nothing.
        /// </summary>
        public void HideBanner()
        {
            Debug.Log("[AdManager] HideBanner called — banner ads are not implemented in NeonSerpent.");
        }

        #endregion
        // -------------------------------------------------------------------------
        #region Rewarded Ads

        /// <summary>
        /// Requests and pre-loads a new rewarded ad.
        /// Called automatically on init and after each rewarded ad is dismissed.
        /// </summary>
        public void LoadRewarded()
        {
#if GOOGLE_MOBILE_ADS
            DestroyRewarded();

            var request = new AdRequest();
            RewardedAd.Load(RewardedId, request, (ad, error) =>
            {
                if (error != null)
                {
                    Debug.LogWarning($"[AdManager] Rewarded ad load failed: {error.GetMessage()}");
                    return;
                }

                _rewardedAd = ad;
                RegisterRewardedCallbacks(_rewardedAd);
                Debug.Log("[AdManager] Rewarded ad loaded and ready.");
            });
#else
            Debug.Log("[AdManager] LoadRewarded stub.");
#endif
        }

        /// <summary>
        /// Shows a rewarded ad and calls <paramref name="onRewarded"/> with the coin
        /// amount (50) if the player watches it to completion.
        /// If the player is ad-free, the callback is NOT invoked — the reward requires
        /// the player to watch an ad. Use an IAP coin pack for ad-free players instead.
        /// If no rewarded ad is loaded yet, a reload is requested and the callback is skipped.
        /// </summary>
        /// <param name="onRewarded">
        /// Invoked on the main thread with the number of coins to grant (always 50).
        /// Only called on a verified earn — not on skip or close.
        /// </param>
        public void ShowRewardedAd(Action<int> onRewarded)
        {
            if (IsAdFree)
            {
                // Design decision: ad-free players do not receive the watch-ad coin reward.
                // They already paid for an ad-free experience. Direct them to the coin shop IAP instead.
                Debug.Log("[AdManager] Player is ad-free — rewarded ad skipped (no callback invoked).");
                return;
            }

#if GOOGLE_MOBILE_ADS
            if (_rewardedAd != null && _rewardedAd.CanShowAd())
            {
                _rewardedAd.Show(reward =>
                {
                    // This callback fires on the main thread when the player earns the reward.
                    const int COIN_REWARD = 50;
                    Debug.Log($"[AdManager] Rewarded ad earned — granting {COIN_REWARD} coins.");
                    onRewarded?.Invoke(COIN_REWARD);
                });
            }
            else
            {
                Debug.LogWarning("[AdManager] Rewarded ad not ready — requesting reload.");
                LoadRewarded();
            }
#else
            Debug.Log("[AdManager] ShowRewardedAd stub — invoking callback with 50 coins for Editor testing.");
            onRewarded?.Invoke(50);
#endif
        }

        private void DestroyRewarded()
        {
#if GOOGLE_MOBILE_ADS
            if (_rewardedAd != null)
            {
                _rewardedAd.Destroy();
                _rewardedAd = null;
            }
#endif
        }

        #endregion
        // -------------------------------------------------------------------------
        #region AdMob Event Callbacks

#if GOOGLE_MOBILE_ADS

        private void RegisterInterstitialCallbacks(InterstitialAd ad)
        {
            ad.OnAdPaid         += adValue => Debug.Log($"[AdManager] Interstitial paid: {adValue.Value} {adValue.CurrencyCode}");
            ad.OnAdImpressionRecorded += () => Debug.Log("[AdManager] Interstitial impression recorded.");
            ad.OnAdClicked      += () => Debug.Log("[AdManager] Interstitial clicked.");
            ad.OnAdFullScreenContentOpened  += () => Debug.Log("[AdManager] Interstitial opened full-screen.");
            ad.OnAdFullScreenContentClosed  += OnInterstitialClosed;
            ad.OnAdFullScreenContentFailed  += error =>
            {
                Debug.LogWarning($"[AdManager] Interstitial failed to show: {error.GetMessage()}");
                LoadInterstitial(); // keep an ad ready for next time
            };
        }

        private void OnInterstitialClosed()
        {
            Debug.Log("[AdManager] Interstitial closed — pre-loading next.");
            DestroyInterstitial();
            LoadInterstitial();
        }

        private void RegisterRewardedCallbacks(RewardedAd ad)
        {
            ad.OnAdPaid         += adValue => Debug.Log($"[AdManager] Rewarded ad paid: {adValue.Value} {adValue.CurrencyCode}");
            ad.OnAdImpressionRecorded += () => Debug.Log("[AdManager] Rewarded impression recorded.");
            ad.OnAdClicked      += () => Debug.Log("[AdManager] Rewarded ad clicked.");
            ad.OnAdFullScreenContentOpened  += () => Debug.Log("[AdManager] Rewarded ad opened full-screen.");
            ad.OnAdFullScreenContentClosed  += OnRewardedClosed;
            ad.OnAdFullScreenContentFailed  += error =>
            {
                Debug.LogWarning($"[AdManager] Rewarded ad failed to show: {error.GetMessage()}");
                LoadRewarded();
            };
        }

        private void OnRewardedClosed()
        {
            Debug.Log("[AdManager] Rewarded ad closed — pre-loading next.");
            DestroyRewarded();
            LoadRewarded();
        }

#endif // GOOGLE_MOBILE_ADS

        #endregion
    }
}
