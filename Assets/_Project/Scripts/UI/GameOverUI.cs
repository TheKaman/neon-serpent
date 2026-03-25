using UnityEngine;
using UnityEngine.UI;
using NeonSerpent.Core;
using NeonSerpent.Scoring;
using NeonSerpent.Ads;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Shown after game over. Listens to both GameManager (full flow)
    /// and GameSession (direct-play from Editor) so it works in both cases.
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
        [SerializeField] private GameSession  _gameSession;

        private void Awake()
        {
            if (_panel != null) _panel.SetActive(false);
            _restartButton?.onClick.AddListener(OnRestart);
            _mainMenuButton?.onClick.AddListener(OnMainMenu);
        }

        private void OnEnable()
        {
            // Full flow — GameManager exists (Bootstrap loaded)
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameOver += Show;

            // Direct-play from Editor — no Bootstrap
            if (_gameSession != null)
                _gameSession.OnSessionGameOver += Show;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameOver -= Show;

            if (_gameSession != null)
                _gameSession.OnSessionGameOver -= Show;
        }

        private void Show()
        {
            if (_panel != null) _panel.SetActive(true);

            if (_finalScoreText != null && _scoreManager != null)
                _finalScoreText.text = $"SCORE\n{_scoreManager.CurrentScore:N0}";

            Invoke(nameof(ShowAd), 0.8f);
        }

        private void ShowAd() => AdManager.Instance?.ShowGameOverInterstitial();

        private void OnRestart()
        {
            if (_panel != null) _panel.SetActive(false);

            // Reset score
            _scoreManager?.ResetScore();

            var gm = GameManager.Instance;
            if (gm != null)
                gm.StartGame(gm.CurrentMode);
            else
                _gameSession?.SendMessage("BeginSession"); // direct-play restart
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
