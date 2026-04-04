using UnityEngine;
using UnityEngine.UI;
using NeonSerpent.Ads;
using NeonSerpent.Core;
using NeonSerpent.Input;
using NeonSerpent.Utilities;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Pause menu panel. Shown/hidden by the pause button in HUD or the Escape/Back key.
    /// Back-button detection is centralized in SwipeInputHandler — PauseUI subscribes to
    /// SwipeInputHandler.OnBackPressed rather than polling input directly.
    /// Calls GameManager.PauseGame() and ResumeGame() — never changes game state directly.
    /// </summary>
    public class PauseUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject _panel;

        [Header("Buttons")]
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _mainMenuButton;

        [Header("Input (assign SwipeInputHandler from scene)")]
        [SerializeField] private SwipeInputHandler _swipeInput;

        private void Awake()
        {
            if (_panel != null) _panel.SetActive(false);
            _resumeButton?.onClick.AddListener(OnResume);
            _mainMenuButton?.onClick.AddListener(OnMainMenu);
        }

        private void OnEnable()
        {
            if (_swipeInput != null)
                _swipeInput.OnBackPressed += Toggle;
        }

        private void OnDisable()
        {
            if (_swipeInput != null)
                _swipeInput.OnBackPressed -= Toggle;
        }

        /// <summary>Toggle between paused and playing state.</summary>
        public void Toggle()
        {
            var gm = GameManager.Instance;

            // Bug E fix: when there is no GameManager (direct Editor play), use Time.timeScale
            // as the source of truth so Pause/Resume still work in isolation.
            if (gm == null)
            {
                if (Time.timeScale > 0f)
                    Pause();
                else
                    OnResume();
                return;
            }

            if (gm.CurrentState == GameState.Playing)
                Pause();
            else if (gm.CurrentState == GameState.Paused)
                OnResume();
        }

        /// <summary>Show the pause panel and pause the game.</summary>
        public void Pause()
        {
            if (_panel != null) _panel.SetActive(true);

            if (GameManager.Instance != null)
                GameManager.Instance.PauseGame();
            else
                Time.timeScale = 0f; // fallback for direct Editor play
        }

        private void OnResume()
        {
            if (_panel != null) _panel.SetActive(false);

            if (GameManager.Instance != null)
                GameManager.Instance.ResumeGame();
            else
                Time.timeScale = 1f; // fallback for direct Editor play
        }

        private void OnMainMenu()
        {
            if (_panel != null) _panel.SetActive(false);
            Time.timeScale = 1f;
            AdManager.Instance?.OnReturnToMenu();
            SceneLoader.Instance?.LoadScene(
                Constants.SCENE_MAIN_MENU,
                () => GameManager.Instance?.GoToMainMenu());
        }
    }
}
