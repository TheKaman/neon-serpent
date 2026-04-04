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
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _starsText;
        [SerializeField] private TMP_Text _levelNameText;

        [Header("Buttons")]
        [SerializeField] private Button _nextLevelBtn;
        [SerializeField] private Button _mainMenuBtn;

        [Header("References")]
        [SerializeField] private ScoreManager _scoreManager;
        [SerializeField] private LevelManager _levelManager;
        [SerializeField] private GameSession  _gameSession;

        private void Awake()
        {
            if (_panel != null) _panel.SetActive(false);
            _nextLevelBtn?.onClick.AddListener(OnNextLevel);
            _mainMenuBtn?.onClick.AddListener(OnMainMenu);
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
            // Bug 8: if _scoreManager is null, score will be 0, CalculateStars returns 0,
            // and progress is saved as 0 stars — the next level will never unlock.
            // Log clearly so the missing wire-up is caught immediately in the Editor.
            if (_scoreManager == null)
                Debug.LogWarning("[LevelCompleteUI] _scoreManager is null. Score will show as 0 and no stars will be awarded. " +
                    "Re-run NeonSerpent → Setup Project to rewire the reference.");

            if (_panel != null) _panel.SetActive(true);

            long score = _scoreManager != null ? _scoreManager.CurrentScore : 0L;
            if (_scoreText != null)
                _scoreText.text = $"SCORE\n{score:N0}";

            LevelData level = _levelManager?.CurrentLevel;
            if (_levelNameText != null)
                _levelNameText.text = level != null ? level.LevelName.ToUpper() : "LEVEL COMPLETE";

            int stars = CalculateStars(score, level);
            if (_starsText != null)
                _starsText.text = new string('★', stars) + new string('☆', 3 - stars);

            // Persist campaign star progress.
            // Key is WorldIndex * 100 + LevelIndex — same composite used in LevelSelectUI
            // to avoid collisions when multiple worlds share the same LevelIndex value.
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

            // Hide Next Level button if this is the final level
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

            AdManager.Instance?.OnCampaignLevelComplete();

            LevelData next = _levelManager?.CurrentLevel?.NextLevel;
            if (next == null) { OnMainMenu(); return; }

            // Tell GameManager which level to load next, then restart the game loop.
            // StartGame sets CurrentState back to Playing, which re-enables win/lose condition
            // guards in LevelManager. GameSession.HandleGameStarted then calls BeginSession(),
            // which calls StartSessionInternal → LevelManager.LoadLevel(SelectedLevel).
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SelectedLevel = next;
                GameManager.Instance.StartGame(GameMode.Campaign);
            }
            else
            {
                // Fallback for direct Editor play without Bootstrap.
                // Load the level first so StartSessionInternal picks up CurrentLevel,
                // then reset score once, then begin the session in Campaign mode so
                // per-level speed and power-up settings are applied correctly.
                _levelManager?.LoadLevel(next);
                _scoreManager?.ResetScore();
                _gameSession?.BeginEditorSession(GameMode.Campaign);
            }
        }

        private void OnMainMenu()
        {
            if (_panel != null) _panel.SetActive(false);
            AdManager.Instance?.OnCampaignLevelComplete();
            AdManager.Instance?.OnReturnToMenu();
            SceneLoader.Instance?.LoadScene(
                Constants.SCENE_MAIN_MENU,
                () => GameManager.Instance?.GoToMainMenu());
        }
    }
}
