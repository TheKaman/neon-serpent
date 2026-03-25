using UnityEngine;
using TMPro;
using NeonSerpent.Scoring;
using NeonSerpent.Levels;

namespace NeonSerpent.UI
{
    /// <summary>
    /// In-game HUD: score display, timer, and power-up icon area.
    /// Subscribes to ScoreManager and LevelManager events.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        [Header("Score")]
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _multiplierText;

        [Header("Timer (Time Attack / Campaign)")]
        [SerializeField] private GameObject      _timerPanel;
        [SerializeField] private TextMeshProUGUI _timerText;

        [Header("References")]
        [SerializeField] private ScoreManager _scoreManager;
        [SerializeField] private LevelManager _levelManager;

        private void OnEnable()
        {
            _scoreManager.OnScoreChanged += UpdateScore;
            if (_levelManager != null)
                _levelManager.OnTimerUpdated += UpdateTimer;
        }

        private void OnDisable()
        {
            _scoreManager.OnScoreChanged -= UpdateScore;
            if (_levelManager != null)
                _levelManager.OnTimerUpdated -= UpdateTimer;
        }

        private void Start()
        {
            UpdateScore(0);
            bool hasTimer = _levelManager?.CurrentLevel?.TimeLimit > 0;
            if (_timerPanel != null) _timerPanel.SetActive(hasTimer);
        }

        private void UpdateScore(int score)
        {
            if (_scoreText      != null) _scoreText.text      = score.ToString("N0");
            if (_multiplierText != null)
            {
                int mult = _scoreManager.Multiplier;
                _multiplierText.text    = mult > 1 ? $"x{mult}" : "";
                _multiplierText.enabled = mult > 1;
            }
        }

        private void UpdateTimer(float remaining)
        {
            if (_timerText == null) return;
            int mins = (int)(remaining / 60);
            int secs = (int)(remaining % 60);
            _timerText.text = mins > 0 ? $"{mins}:{secs:00}" : $"{secs}";
        }
    }
}
