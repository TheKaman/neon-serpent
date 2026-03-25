using UnityEngine;
using NeonSerpent.SaveData;
using NeonSerpent.Utilities;

namespace NeonSerpent.Ads
{
    /// <summary>
    /// Central ad coordinator using Unity LevelPlay (IronSource) SDK.
    /// All ad calls no-op if PlayerData.isAdFree is true.
    /// Lives in Bootstrap scene as a Singleton.
    /// TODO: Add IronSource SDK via Unity Package Manager before wiring up ad unit IDs.
    /// </summary>
    public class AdManager : Singleton<AdManager>
    {
        [Header("Ad Unit IDs (fill in after LevelPlay setup)")]
        [SerializeField] private string _androidAppKey   = "YOUR_IRONSOURCE_APP_KEY";
        [SerializeField] private string _bannerAdUnitId  = "";
        [SerializeField] private string _interstitialAdUnitId = "";

        private int _sessionCount;
        private int _campaignCompletionCount;

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        private void Initialize()
        {
            // TODO: IronSource.Agent.init(_androidAppKey); after SDK is imported
            Debug.Log("[AdManager] LevelPlay SDK init placeholder. Import SDK before enabling.");
        }

        /// <summary>Show a banner ad. No-op if player has removed ads.</summary>
        public void ShowBanner()
        {
            if (SaveManager.Instance.Data.isAdFree) return;
            // TODO: IronSource.Agent.displayBanner();
        }

        /// <summary>Hide the banner ad.</summary>
        public void HideBanner()
        {
            // TODO: IronSource.Agent.hideBanner();
        }

        /// <summary>Show an interstitial ad after game over. No-op if ad-free.</summary>
        public void ShowGameOverInterstitial()
        {
            if (SaveManager.Instance.Data.isAdFree) return;
            // TODO: if (IronSource.Agent.isInterstitialReady()) IronSource.Agent.showInterstitial();
            Debug.Log("[AdManager] Game over interstitial placeholder.");
        }

        /// <summary>Increment session count and show interstitial on every 3rd session return to menu.</summary>
        public void OnReturnToMenu()
        {
            if (SaveManager.Instance.Data.isAdFree) return;
            _sessionCount++;
            if (_sessionCount % 3 == 0)
                ShowGameOverInterstitial();
        }

        /// <summary>Show interstitial on every 2nd campaign level completion.</summary>
        public void OnCampaignLevelComplete()
        {
            if (SaveManager.Instance.Data.isAdFree) return;
            _campaignCompletionCount++;
            if (_campaignCompletionCount % 2 == 0)
                ShowGameOverInterstitial();
        }
    }
}
