using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeonSerpent.Ads;
using NeonSerpent.Core;
using NeonSerpent.Leaderboard;
using NeonSerpent.SaveData;
using NeonSerpent.Utilities;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Leaderboard scene root UI controller.
    /// Shows the local top-10 scores for Classic Endless, Time Attack, and Campaign modes
    /// via tab buttons. The "View Online" button opens the GPGS native overlay when an
    /// <see cref="ILeaderboardService"/> that is authenticated is available.
    /// </summary>
    public class LeaderboardUI : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        #region Inspector Fields

        [Header("Tab Buttons")]
        [SerializeField] private Button _classicTabBtn;
        [SerializeField] private Button _timeAttackTabBtn;
        [SerializeField] private Button _campaignTabBtn;

        [Header("Title")]
        [SerializeField] private TMP_Text _titleText;

        [Header("Entries")]
        [SerializeField] private Transform       _entriesContainer;
        [SerializeField] private LeaderboardRowUI _rowPrefab;
        [SerializeField] private TMP_Text         _noScoresText;

        [Header("Navigation")]
        [SerializeField] private Button _onlineBtn;
        [SerializeField] private Button _backBtn;

        [Header("Online Service (optional)")]
        [Tooltip("Assign a MonoBehaviour that implements ILeaderboardService, e.g. GPGSLeaderboardService.")]
        [SerializeField] private MonoBehaviour _leaderboardServiceProvider;

        #endregion
        // -------------------------------------------------------------------------
        #region Private State

        private GameMode          _activeTab        = GameMode.ClassicEndless;
        private ILeaderboardService _leaderboardService;

        #endregion
        // -------------------------------------------------------------------------
        #region Unity Lifecycle

        private void Start()
        {
            // Resolve the ILeaderboardService from the Inspector-assigned provider.
            if (_leaderboardServiceProvider != null)
                _leaderboardService = _leaderboardServiceProvider as ILeaderboardService;

            _classicTabBtn?.onClick.AddListener(   () => ShowTab(GameMode.ClassicEndless));
            _timeAttackTabBtn?.onClick.AddListener(() => ShowTab(GameMode.TimeAttack));
            _campaignTabBtn?.onClick.AddListener(  () => ShowTab(GameMode.Campaign));
            _onlineBtn?.onClick.AddListener(OnOnlineBtn);
            _backBtn?.onClick.AddListener(OnBackBtn);

            // Default to the Classic tab on open.
            ShowTab(GameMode.ClassicEndless);
        }

        #endregion
        // -------------------------------------------------------------------------
        #region Public API

        /// <summary>
        /// Switches the visible tab to the given <paramref name="mode"/>, clears existing rows,
        /// fetches the local top-10 scores for that mode's leaderboard, and instantiates a
        /// <see cref="LeaderboardRowUI"/> prefab row for each entry.
        /// Shows "No scores yet!" if the list is empty.
        /// </summary>
        /// <param name="mode">The game mode whose scores should be displayed.</param>
        public void ShowTab(GameMode mode)
        {
            _activeTab = mode;

            UpdateTitle(mode);
            ClearRows();

            string leaderboardId = GetLeaderboardId(mode);

            if (SaveManager.Instance == null)
            {
                ShowNoScores(true);
                return;
            }

            List<LeaderboardEntry> entries =
                SaveManager.Instance.Data.GetLocalTopScores(leaderboardId, 10);

            bool empty = entries == null || entries.Count == 0;
            ShowNoScores(empty);

            if (!empty)
            {
                foreach (LeaderboardEntry entry in entries)
                {
                    if (_rowPrefab == null || _entriesContainer == null) break;

                    LeaderboardRowUI row = Instantiate(_rowPrefab, _entriesContainer);
                    row.Populate(entry);
                }
            }
        }

        #endregion
        // -------------------------------------------------------------------------
        #region Button Handlers

        /// <summary>
        /// Opens the GPGS native leaderboard overlay for the current tab.
        /// Falls back to a log message if no authenticated service is available.
        /// </summary>
        private void OnOnlineBtn()
        {
            if (_leaderboardService != null && _leaderboardService.IsAuthenticated)
            {
                _leaderboardService.ShowLeaderboard(GetLeaderboardId(_activeTab));
            }
            else
            {
                Debug.Log("[LeaderboardUI] Sign in required to view the online leaderboard.");
            }
        }

        /// <summary>Navigates back to the Main Menu scene.</summary>
        private void OnBackBtn()
        {
            AdManager.Instance?.OnReturnToMenu();
            SceneLoader.Instance?.LoadScene(Constants.SCENE_MAIN_MENU);
        }

        #endregion
        // -------------------------------------------------------------------------
        #region Helpers

        /// <summary>Destroy all child GameObjects of the entries container.</summary>
        private void ClearRows()
        {
            if (_entriesContainer == null) return;

            // Iterate backwards so the index remains valid after each Destroy call.
            for (int i = _entriesContainer.childCount - 1; i >= 0; i--)
                Destroy(_entriesContainer.GetChild(i).gameObject);
        }

        private void ShowNoScores(bool show)
        {
            if (_noScoresText != null)
                _noScoresText.gameObject.SetActive(show);
        }

        private void UpdateTitle(GameMode mode)
        {
            if (_titleText == null) return;
            _titleText.text = mode switch
            {
                GameMode.TimeAttack => "TIME ATTACK",
                GameMode.Campaign   => "CAMPAIGN",
                _                   => "CLASSIC ENDLESS"
            };
        }

        private static string GetLeaderboardId(GameMode mode)
        {
            return mode switch
            {
                GameMode.TimeAttack => Constants.LEADERBOARD_TIME_ATTACK,
                GameMode.Campaign   => Constants.LEADERBOARD_CAMPAIGN,
                _                   => Constants.LEADERBOARD_CLASSIC
            };
        }

        #endregion
    }
}
