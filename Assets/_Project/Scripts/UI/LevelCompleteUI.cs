using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeonSerpent.Ads;
using NeonSerpent.Core;
using NeonSerpent.Levels;
using NeonSerpent.Scoring;
using NeonSerpent.SaveData;
using NeonSerpent.Utilities;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Shown when a campaign level is completed. Displays the score, stars earned,
    /// and buttons for the next level or returning to the main menu.
    /// Subscribes to GameManager.OnLevelComplete.
    /// </summary>
    public class LevelCompleteUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject _panel;

        [Header("Score & Stars")]
        [SerializeField] private TMP_Text _headerText;      // shows "LEVEL COMPLETE!"
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _starsText;
        [SerializeField] private TMP_Text _levelNameText;
        [SerializeField] private TMP_Text _starHintText;    // optional: "X more for ★★★"

        [Header("Buttons")]
        [SerializeField] private Button _nextLevelBtn;
        [SerializeField] private Button _mainMenuBtn;
        [SerializeField] private Button _retryBtn;          // replay the same level

        [Header("References")]
        [SerializeField] private ScoreManager _scoreManager;
        [SerializeField] private LevelManager _levelManager;
        [SerializeField] private GameSession  _gameSession;

        private void Awake()
        {
            if (_panel != null) _panel.SetActive(false);
            _nextLevelBtn?.onClick.AddListener(OnNextLevel);
            _mainMenuBtn?.onClick.AddListener(OnMainMenu);
            _retryBtn?.onClick.AddListener(OnRetry);
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnLevelComplete += Show;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnLevelComplete -= Show;
        }

        /// <summary>Display the level complete panel with score and stars earned.</summary>
        private void Show()
        {
            if (_scoreManager == null)
                Debug.LogWarning("[LevelCompleteUI] _scoreManager is null. Score will show as 0. " +
                    "Re-run NeonSerpent → Setup Project to rewire the reference.");

            if (_panel != null) _panel.SetActive(true);

            // Always show a clear win banner so zero-star completions don't look like failures
            if (_headerText != null) _headerText.text = "LEVEL COMPLETE!";

            long score = _scoreManager != null ? _scoreManager.CurrentScore : 0L;
            if (_scoreText != null)
                _scoreText.text = $"SCORE\n{score:N0}";

            LevelData level = _levelManager?.CurrentLevel;
            if (_levelNameText != null)
                _levelNameText.text = level != null ? level.LevelName.ToUpper() : string.Empty;

            int stars = CalculateStars(score, level);
            if (_starsText != null)
                _starsText.text = new string('★', stars) + new string('☆', 3 - stars);

            // Show how many points to the next star tier (helps the player improve)
            if (_starHintText != null && level != null)
            {
                if (stars < 3)
                {
                    long nextThreshold = stars == 0 ? level.StarThreshold1
                                       : stars == 1 ? level.StarThreshold2
                                       :              level.StarThreshold3;
                    _starHintText.text = $"{nextThreshold - score:N0} more for {new string('★', stars + 1)}";
                }
                else
                {
                    _starHintText.text = "Perfect run!";
                }
            }

            // Persist campaign star progress — only overwrite if this run scored higher
            if (level != null && SaveManager.Instance != null)
            {
                int progressKey = level.WorldIndex * 100 + level.LevelIndex;
                var progress = SaveManager.Instance.Data.campaignProgress;
                if (!progress.ContainsKey(progressKey) || progress[progressKey] < stars)
                {
                    progress[progressKey] = stars;
                    SaveManager.Instance.Save();
                }
            }

            // Hide Next Level button on the final level
            bool hasNext = level != null && level.NextLevel != null;
            _nextLevelBtn?.gameObject.SetActive(hasNext);
        }

        private static int CalculateStars(long score, LevelData level)
        {
            if (level == null) return 1;
            if (score >= level.StarThreshold3) return 3;
            if (score >= level.StarThreshold2) return 2;
            if (score >= level.StarThreshold1) return 1;
            return 0;
        }

        private void OnNextLevel()
        {
            if (_panel != null) _panel.SetActive(false);

            // Count this completion and potentially show an interstitial ad.
            // Called once per completion — NOT in OnMainMenu to avoid double-counting.
            AdManager.Instance?.OnCampaignLevelComplete();

            LevelData next = _levelManager?.CurrentLevel?.NextLevel;
            if (next == null) { OnMainMenu(); return; }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SelectedLevel = next;
                GameManager.Instance.StartGame(GameMode.Campaign);
            }
            else
            {
                _levelManager?.LoadLevel(next);
                _scoreManager?.ResetScore();
                _gameSession?.BeginEditorSession(GameMode.Campaign);
            }
        }

        private void OnRetry()
        {
            if (_panel != null) _panel.SetActive(false);

            LevelData current = _levelManager?.CurrentLevel;
            if (current == null) { OnMainMenu(); return; }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SelectedLevel = current;
                GameManager.Instance.StartGame(GameMode.Campaign);
            }
            else
            {
                _levelManager?.LoadLevel(current);
                _scoreManager?.ResetScore();
                _gameSession?.BeginEditorSession(GameMode.Campaign);
            }
        }

        private void OnMainMenu()
        {
            if (_panel != null) _panel.SetActive(false);
            // Only call OnReturnToMenu here — OnCampaignLevelComplete is called in OnNextLevel
            // to avoid double-counting completions and double-showing interstitials.
            AdManager.Instance?.OnReturnToMenu();
            SceneLoader.Instance?.LoadScene(
                Constants.SCENE_MAIN_MENU,
                () => GameManager.Instance?.GoToMainMenu());
        }
    }
}
