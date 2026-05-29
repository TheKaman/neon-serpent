using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeonSerpent.Audio;
using NeonSerpent.Core;
using NeonSerpent.PowerUps;
using NeonSerpent.Scoring;
using NeonSerpent.Levels;
using NeonSerpent.SaveData;
using NeonSerpent.Snake;
using NeonSerpent.Utilities;

namespace NeonSerpent.UI
{
    /// <summary>
    /// In-game HUD: score, personal best, coin display, timer, shield indicator, and frenzy indicator.
    /// Subscribes to ScoreManager, SaveManager, LevelManager, PowerUpManager, and GameManager events.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        [Header("Score")]
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _multiplierText;
        [SerializeField] private TMP_Text _personalBestText;

        [Header("Coins")]
        [SerializeField] private TMP_Text _coinText;

        [Header("Timer (Time Attack / Campaign only)")]
        [SerializeField] private GameObject _timerPanel;
        [SerializeField] private TMP_Text   _timerText;

        [Header("Status Indicators")]
        [SerializeField] private GameObject _shieldIndicator;  // shown while Shield power-up is active
        [SerializeField] private GameObject _frenzyIndicator;  // shown when Frenzy Mode is active (Classic max speed)

        [Header("Pause")]
        [SerializeField] private Button  _pauseButton;
        [SerializeField] private PauseUI _pauseUI;

        [Header("References")]
        [SerializeField] private ScoreManager  _scoreManager;
        [SerializeField] private LevelManager  _levelManager;
        [SerializeField] private PowerUpManager _powerUpManager;
        [SerializeField] private SnakeController _snake;

        // Campaign score-target tracking (no new UI element needed — reuses _personalBestText)
        private long _campaignScoreTarget;

        private const float TIMER_URGENCY_THRESHOLD = 10f;
        private int  _lastUrgencyTickSecond = -1;
        private bool _urgencyActive;

        private void Awake()
        {
            _pauseButton?.onClick.AddListener(() => _pauseUI?.Toggle());
        }

        private void OnEnable()
        {
            if (_scoreManager != null)
            {
                _scoreManager.OnScoreChanged  += UpdateScore;
                _scoreManager.OnFrenzyChanged += HandleFrenzyChanged;
            }
            if (_levelManager != null)
            {
                _levelManager.OnTimerUpdated += UpdateTimer;
                _levelManager.OnLevelLoaded  += HandleLevelLoaded;
            }
            if (SaveManager.Instance != null)
                SaveManager.Instance.OnCoinsChanged += UpdateCoins;
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStarted += HandleGameStarted;
            if (_powerUpManager != null)
            {
                _powerUpManager.OnEffectActivated   += HandleEffectActivated;
                _powerUpManager.OnEffectDeactivated += HandleEffectDeactivated;
            }
            if (_snake != null)
                _snake.OnShieldAbsorbed += HandleShieldAbsorbed;
        }

        private void OnDisable()
        {
            if (_scoreManager != null)
            {
                _scoreManager.OnScoreChanged  -= UpdateScore;
                _scoreManager.OnFrenzyChanged -= HandleFrenzyChanged;
            }
            if (_levelManager != null)
            {
                _levelManager.OnTimerUpdated -= UpdateTimer;
                _levelManager.OnLevelLoaded  -= HandleLevelLoaded;
            }
            if (SaveManager.Instance != null)
                SaveManager.Instance.OnCoinsChanged -= UpdateCoins;
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStarted -= HandleGameStarted;
            if (_powerUpManager != null)
            {
                _powerUpManager.OnEffectActivated   -= HandleEffectActivated;
                _powerUpManager.OnEffectDeactivated -= HandleEffectDeactivated;
            }
            if (_snake != null)
                _snake.OnShieldAbsorbed -= HandleShieldAbsorbed;
        }

        private void Start()
        {
            UpdateScore(0);

            // LevelManager.LoadLevel has not been called yet at this point — CurrentLevel is null.
            // Hide the timer panel by default; UpdateTimer() will reveal it when the first tick fires.
            if (_timerPanel != null) _timerPanel.SetActive(false);

            // Status indicators off by default
            if (_shieldIndicator != null) _shieldIndicator.SetActive(false);
            if (_frenzyIndicator  != null) _frenzyIndicator.SetActive(false);

            // Show current coin total on scene load
            int startingCoins = SaveManager.Instance != null ? SaveManager.Instance.Data.coins : 0;
            UpdateCoins(startingCoins);

            // Show personal best for the active mode
            GameMode mode = GameManager.Instance != null
                ? GameManager.Instance.CurrentMode
                : GameMode.ClassicEndless;
            UpdatePersonalBest(mode);
        }

        // ── GameManager events ────────────────────────────────────────────────

        /// <summary>
        /// Resets the timer panel and status indicators at the start of each new game,
        /// and refreshes the personal best for the new mode.
        /// </summary>
        private void HandleGameStarted(GameMode mode)
        {
            if (_timerPanel      != null) _timerPanel.SetActive(false);
            if (_shieldIndicator != null) _shieldIndicator.SetActive(false);
            if (_frenzyIndicator != null) _frenzyIndicator.SetActive(false);
            if (_timerText       != null) _timerText.color = Color.white;
            _urgencyActive         = false;
            _lastUrgencyTickSecond = -1;

            if (mode != GameMode.Campaign)
            {
                _campaignScoreTarget = 0;
                UpdatePersonalBest(mode);
            }
            else
            {
                // Campaign: the real "TARGET: X" value arrives shortly via HandleLevelLoaded.
                // Until then, clear the label so the previous level's / previous session's
                // stale "NEED: X" value is never visible during the 3-2-1 countdown (M-4).
                _campaignScoreTarget = 0;
                if (_personalBestText != null) _personalBestText.text = string.Empty;
            }
        }

        // ── Score events ──────────────────────────────────────────────────────

        private void UpdateScore(long score)
        {
            if (_scoreText != null) _scoreText.text = score.ToString("N0");
            if (_multiplierText != null && _scoreManager != null)
            {
                int mult = _scoreManager.Multiplier;
                _multiplierText.text    = mult > 1 ? $"x{mult}" : "";
                _multiplierText.enabled = mult > 1;
            }

            // In Campaign with a score target, repurpose the personal best label to show
            // how many points the player still needs — the most useful real-time info.
            bool isCampaign = GameManager.Instance?.CurrentMode == GameMode.Campaign;
            if (isCampaign && _campaignScoreTarget > 0 && _personalBestText != null)
            {
                long remaining = _campaignScoreTarget - score;
                _personalBestText.text = remaining > 0
                    ? $"NEED: {remaining:N0}"
                    : "TARGET HIT!";
            }
        }

        private void HandleFrenzyChanged(bool active)
        {
            if (_frenzyIndicator != null) _frenzyIndicator.SetActive(active);
        }

        // ── Level loaded (Campaign) ───────────────────────────────────────────

        /// <summary>
        /// Called when LevelManager loads a new level. In Campaign mode the personal best
        /// label is repurposed to show the score target so the player always knows their goal.
        /// </summary>
        private void HandleLevelLoaded(LevelData data)
        {
            _campaignScoreTarget = data != null ? data.ScoreTarget : 0;
            if (_personalBestText == null) return;

            bool isCampaign = GameManager.Instance?.CurrentMode == GameMode.Campaign;
            if (isCampaign && _campaignScoreTarget > 0)
                _personalBestText.text = $"TARGET: {_campaignScoreTarget:N0}";
            else if (!isCampaign)
                UpdatePersonalBest(GameManager.Instance?.CurrentMode ?? GameMode.ClassicEndless);
        }

        // ── Personal best ─────────────────────────────────────────────────────

        /// <summary>
        /// Reads the player's all-time best score for the current mode from SaveManager
        /// and updates the personal best label. Displayed during play to create
        /// run-to-run pressure — the most impactful retention hook for a score-based game.
        /// </summary>
        private void UpdatePersonalBest(GameMode mode)
        {
            if (_personalBestText == null) return;

            string leaderboardId = mode switch
            {
                GameMode.TimeAttack => Constants.LEADERBOARD_TIME_ATTACK,
                GameMode.Campaign   => Constants.LEADERBOARD_CAMPAIGN,
                _                   => Constants.LEADERBOARD_CLASSIC
            };

            var top = SaveManager.Instance?.Data?.GetLocalTopScores(leaderboardId, 1);
            _personalBestText.text = (top != null && top.Count > 0)
                ? $"BEST: {top[0].Score:N0}"
                : "BEST: ---";
        }

        // ── Timer ─────────────────────────────────────────────────────────────

        private void UpdateTimer(float remaining)
        {
            // Reveal the timer panel on the first tick — LevelManager only fires this
            // event when a level with a time limit is active, so this is always correct.
            if (_timerPanel != null && !_timerPanel.activeSelf)
            {
                _timerPanel.SetActive(true);
                _urgencyActive = false;
                _lastUrgencyTickSecond = -1;
            }

            if (_timerText == null) return;

            bool urgent = remaining <= TIMER_URGENCY_THRESHOLD && remaining > 0f;
            _timerText.color = urgent ? Color.red : Color.white;

            if (urgent)
            {
                int currentSec = Mathf.CeilToInt(remaining);
                if (currentSec != _lastUrgencyTickSecond)
                {
                    _lastUrgencyTickSecond = currentSec;
                    AudioManager.Instance?.PlaySFX(SoundEvent.TimerTick);
                }
            }
            else if (_urgencyActive)
            {
                // Timer was reset (new level) — restore white
                _timerText.color = Color.white;
            }
            _urgencyActive = urgent;

            int mins = (int)(remaining / 60);
            int secs = (int)(remaining % 60);
            _timerText.text = mins > 0 ? $"{mins}:{secs:00}" : $"{secs}";
        }

        // ── Coins ─────────────────────────────────────────────────────────────

        /// <summary>Update the coin counter display. Called by SaveManager.OnCoinsChanged.</summary>
        private void UpdateCoins(int totalCoins)
        {
            if (_coinText != null)
                _coinText.text = $"{totalCoins:N0} coins";
        }

        // ── Power-up indicators ───────────────────────────────────────────────

        private void HandleEffectActivated(PowerUpType type, float duration)
        {
            if (type == PowerUpType.Shield && _shieldIndicator != null)
                _shieldIndicator.SetActive(true);
        }

        private void HandleEffectDeactivated(PowerUpType type)
        {
            if (type == PowerUpType.Shield && _shieldIndicator != null)
                _shieldIndicator.SetActive(false);
        }

        /// <summary>Shield was consumed by a collision — hide the indicator immediately.</summary>
        private void HandleShieldAbsorbed()
        {
            if (_shieldIndicator != null) _shieldIndicator.SetActive(false);
        }
    }
}
