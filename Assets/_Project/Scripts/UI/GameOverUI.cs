using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeonSerpent.Core;
using NeonSerpent.Scoring;
using NeonSerpent.Ads;
using NeonSerpent.SaveData;
using NeonSerpent.Leaderboard;
using NeonSerpent.Utilities;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Shown after game over. Displays the current run score and the all-time best
    /// score for the active mode. Listens to both GameManager (full Bootstrap flow)
    /// and GameSession (direct Editor play) so it works in both contexts.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text   _finalScoreText;
        [SerializeField] private TMP_Text   _bestScoreText;
        [SerializeField] private TMP_Text   _newBestText;
        [SerializeField] private Button     _restartButton;
        [SerializeField] private Button     _mainMenuButton;
        [SerializeField] private Button     _viewLeaderboardBtn;

        [Header("References")]
        [SerializeField] private ScoreManager _scoreManager;
        [SerializeField] private GameSession  _gameSession;

        private void Awake()
        {
            if (_panel != null) _panel.SetActive(false);
            if (_viewLeaderboardBtn != null) _viewLeaderboardBtn.gameObject.SetActive(false);
            _restartButton?.onClick.AddListener(OnRestart);
            _mainMenuButton?.onClick.AddListener(OnMainMenu);
            _viewLeaderboardBtn?.onClick.AddListener(OnViewLeaderboard);
        }

        private void OnEnable()
        {
            // Subscribe to exactly one source — GameManager takes priority when Bootstrap
            // is loaded. Subscribing to both caused two ad requests per death (H2).
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameOver += Show;
            else if (_gameSession != null)
                _gameSession.OnSessionGameOver += Show;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameOver -= Show;
            else if (_gameSession != null)
                _gameSession.OnSessionGameOver -= Show;
        }

        private void Show()
        {
            // Do not show the game-over panel when a campaign level has already been
            // completed — LevelCompleteUI owns that state.
            if (GameManager.Instance != null &&
                GameManager.Instance.CurrentState == GameState.LevelComplete)
                return;

            // Guard against duplicate calls (e.g. timer expiry followed by wall hit
            // in Time Attack both routing here before the panel is dismissed).
            if (_panel != null && _panel.activeSelf) return;

            if (_panel != null) _panel.SetActive(true);

            // Hide "NEW BEST!" by default; show it below only if the player beat their record.
            if (_newBestText != null) _newBestText.gameObject.SetActive(false);

            long currentScore = _scoreManager != null ? _scoreManager.CurrentScore : 0L;

            if (_finalScoreText != null)
                _finalScoreText.text = $"SCORE\n{currentScore:N0}";

            // SaveManager has already persisted the score (HandleFatalHit saves before
            // raising OnSessionGameOver / OnGameOver), so the top entry IS the new score.
            // To detect a new best we compare against the second entry (previous best).
            if (_bestScoreText != null)
                _bestScoreText.text = BuildBestScoreText();

            bool isNewBest = IsNewPersonalBest(currentScore);
            if (_newBestText != null)
            {
                _newBestText.gameObject.SetActive(isNewBest);
                if (isNewBest)
                    _newBestText.color = new Color(1f, 0.84f, 0f, 1f); // gold
            }
            if (_viewLeaderboardBtn != null)
                _viewLeaderboardBtn.gameObject.SetActive(isNewBest);

            CancelInvoke(nameof(ShowAd));
            Invoke(nameof(ShowAd), 2.5f); // give the player time to read their score before the ad appears
        }

        /// <summary>
        /// Returns true when the current score is strictly greater than the previous
        /// personal best for this mode. Because RecordHighScore() has already run before
        /// Show() is called, the list already contains the new score at index 0.
        /// The previous best is therefore the entry at index 1 (if it exists).
        /// If there is only one entry the player has never scored before — always a new best.
        /// </summary>
        private bool IsNewPersonalBest(long currentScore)
        {
            if (SaveManager.Instance == null || SaveManager.Instance.Data == null) return false;

            string leaderboardId = GetLeaderboardIdForCurrentMode();
            var data             = SaveManager.Instance.Data;

            // Top score list — index 0 is the current run (just recorded)
            var topOne = data.GetLocalTopScores(leaderboardId, 1);
            if (topOne == null || topOne.Count == 0) return false;

            // If the list has only one entry, this is the player's very first run — always a new best.
            // To get the second entry we need to peek the raw list.
            if (!data.localScores.ContainsKey(leaderboardId)) return false;
            var raw = data.localScores[leaderboardId];

            // Also require a non-zero score — a score of 0 is never a meaningful personal best.
            if (raw.Count <= 1) return currentScore > 0;

            // raw[0] = all-time best (may be this run). raw[1] = previous best.
            // New best only if this run strictly exceeds the previous best (ties don't count).
            return currentScore == raw[0].score && currentScore > raw[1].score;
        }

        /// <summary>
        /// Reads the all-time top score for the current game mode from SaveManager.
        /// Returns a formatted string ready to assign to a TMP_Text element.
        /// </summary>
        private string BuildBestScoreText()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.Data == null)
                return "BEST\n---";

            string leaderboardId = GetLeaderboardIdForCurrentMode();
            var entries = SaveManager.Instance.Data.GetLocalTopScores(leaderboardId, 1);

            if (entries == null || entries.Count == 0)
                return "BEST\n---";

            return $"BEST\n{entries[0].Score:N0}";
        }

        /// <summary>
        /// Returns the leaderboard ID constant for whichever game mode is active.
        /// Falls back to Classic when running without a GameManager (Editor direct-play).
        /// </summary>
        private string GetLeaderboardIdForCurrentMode()
        {
            GameMode mode = GameManager.Instance != null
                ? GameManager.Instance.CurrentMode
                : GameMode.ClassicEndless;

            return mode switch
            {
                GameMode.TimeAttack => Constants.LEADERBOARD_TIME_ATTACK,
                GameMode.Campaign   => Constants.LEADERBOARD_CAMPAIGN,
                _                   => Constants.LEADERBOARD_CLASSIC
            };
        }

        private void ShowAd() => AdManager.Instance?.ShowGameOverInterstitial();

        private void OnViewLeaderboard()
        {
            if (_panel != null) _panel.SetActive(false);
            GameManager.Instance?.GoToMainMenu();
            AdManager.Instance?.OnReturnToMenu();
            SceneLoader.Instance?.LoadScene(Constants.SCENE_LEADERBOARD);
        }

        private void OnRestart()
        {
            CancelInvoke(nameof(ShowAd));
            if (_panel != null) _panel.SetActive(false);

            var gm = GameManager.Instance;
            if (gm != null)
                gm.StartGame(gm.CurrentMode);
            else
                _gameSession?.BeginSession();
        }

        private void OnMainMenu()
        {
            CancelInvoke(nameof(ShowAd));
            if (_panel != null) _panel.SetActive(false);
            // Reset state machine immediately — don't defer inside the scene-load callback,
            // because if SceneLoader.Instance is null (Editor direct-play) the state would
            // stay stuck in GameOver and the next session would never start correctly (L4).
            GameManager.Instance?.GoToMainMenu();
            AdManager.Instance?.OnReturnToMenu();
            SceneLoader.Instance?.LoadScene(Constants.SCENE_MAIN_MENU);
        }
    }
}
