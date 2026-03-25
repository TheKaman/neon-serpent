using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeonSerpent.Core;
using NeonSerpent.Scoring;
using NeonSerpent.Ads;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Shown after a game over event. Displays final score and provides restart/menu navigation.
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject      _panel;
        [SerializeField] private TextMeshProUGUI _finalScoreText;
        [SerializeField] private TextMeshProUGUI _highScoreText;
        [SerializeField] private Button          _restartButton;
        [SerializeField] private Button          _mainMenuButton;

        [Header("References")]
        [SerializeField] private ScoreManager _scoreManager;

        private void Awake()
        {
            _panel.SetActive(false);
            _restartButton.onClick.AddListener(OnRestart);
            _mainMenuButton.onClick.AddListener(OnMainMenu);
        }

        private void OnEnable()
        {
            GameManager.Instance.OnGameOver += Show;
        }

        private void OnDisable()
        {
            GameManager.Instance.OnGameOver -= Show;
        }

        private void Show()
        {
            _panel.SetActive(true);
            _finalScoreText.text = $"SCORE\n{_scoreManager.CurrentScore:N0}";

            // Show interstitial after a short delay so stats are visible first
            Invoke(nameof(ShowAd), 0.8f);
        }

        private void ShowAd() => AdManager.Instance?.ShowGameOverInterstitial();

        private void OnRestart()
        {
            _panel.SetActive(false);
            GameManager.Instance.StartGame(GameManager.Instance.CurrentMode);
        }

        private void OnMainMenu()
        {
            _panel.SetActive(false);
            SceneLoader.Instance.LoadScene(NeonSerpent.Utilities.Constants.SCENE_MAIN_MENU,
                () => GameManager.Instance.GoToMainMenu());
        }
    }
}
