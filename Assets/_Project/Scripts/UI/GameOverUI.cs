using UnityEngine;
using UnityEngine.UI;
using NeonSerpent.Core;
using NeonSerpent.Scoring;
using NeonSerpent.Ads;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Shown after a game over event. Displays final score and navigation buttons.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text       _finalScoreText;
        [SerializeField] private Button     _restartButton;
        [SerializeField] private Button     _mainMenuButton;

        [Header("References")]
        [SerializeField] private ScoreManager _scoreManager;

        private void Awake()
        {
            if (_panel != null) _panel.SetActive(false);
            _restartButton?.onClick.AddListener(OnRestart);
            _mainMenuButton?.onClick.AddListener(OnMainMenu);
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameOver += Show;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameOver -= Show;
        }

        private void Show()
        {
            if (_panel != null) _panel.SetActive(true);
            if (_finalScoreText != null && _scoreManager != null)
                _finalScoreText.text = $"SCORE\n{_scoreManager.CurrentScore:N0}";

            Invoke(nameof(ShowAd), 0.8f);
        }

        private void ShowAd()
        {
            if (AdManager.Instance != null)
                AdManager.Instance.ShowGameOverInterstitial();
        }

        private void OnRestart()
        {
            if (_panel != null) _panel.SetActive(false);
            GameManager.Instance?.StartGame(GameManager.Instance.CurrentMode);
        }

        private void OnMainMenu()
        {
            if (_panel != null) _panel.SetActive(false);
            SceneLoader.Instance?.LoadScene(
                NeonSerpent.Utilities.Constants.SCENE_MAIN_MENU,
                () => GameManager.Instance?.GoToMainMenu());
        }
    }
}
