using UnityEngine;
using UnityEngine.UI;
using NeonSerpent.Core;
using NeonSerpent.Utilities;
using NeonSerpent.Ads;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Main menu screen controller. Handles mode selection and scene navigation.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _classicButton;
        [SerializeField] private Button _timeAttackButton;
        [SerializeField] private Button _campaignButton;
        [SerializeField] private Button _leaderboardButton;
        [SerializeField] private Button _shopButton;
        [SerializeField] private Button _settingsButton;

        private void Awake()
        {
            _classicButton.onClick.AddListener(   () => StartMode(GameMode.ClassicEndless));
            _timeAttackButton.onClick.AddListener(() => StartMode(GameMode.TimeAttack));
            _campaignButton.onClick.AddListener(  () => SceneLoader.Instance.LoadScene(Constants.SCENE_GAME));
            _leaderboardButton.onClick.AddListener(() => SceneLoader.Instance.LoadScene(Constants.SCENE_LEADERBOARD));
            _shopButton.onClick.AddListener(      () => SceneLoader.Instance.LoadScene(Constants.SCENE_SHOP));
            _settingsButton.onClick.AddListener(  () => SceneLoader.Instance.LoadScene(Constants.SCENE_SETTINGS));
        }

        private void OnEnable()
        {
            AdManager.Instance?.ShowBanner();
            AdManager.Instance?.OnReturnToMenu();
        }

        private void OnDisable()
        {
            AdManager.Instance?.HideBanner();
        }

        private void StartMode(GameMode mode)
        {
            GameManager.Instance.CurrentMode.Equals(mode); // just for reference pre-load
            SceneLoader.Instance.LoadScene(Constants.SCENE_GAME,
                () => GameManager.Instance.StartGame(mode));
        }
    }
}
