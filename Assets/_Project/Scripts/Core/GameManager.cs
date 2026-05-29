using System;
using UnityEngine;
using NeonSerpent.Utilities;

namespace NeonSerpent.Core
{
    /// <summary>
    /// Central state machine and game loop coordinator.
    /// Single source of truth for GameState — all other systems read from here.
    /// Lives in Bootstrap scene, persists across all scenes via Singleton base.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        // --- State ---
        public GameState CurrentState { get; private set; } = GameState.Boot;
        public GameMode  CurrentMode  { get; private set; } = GameMode.ClassicEndless;

        /// <summary>The campaign level the player selected on the level select screen.</summary>
        public NeonSerpent.Levels.LevelData SelectedLevel { get; set; }

        // --- Events (subscribe to these, never call state-change methods from other systems) ---
        public event Action<GameState> OnStateChanged;
        public event Action<GameMode>  OnGameStarted;
        /// <summary>
        /// Raised when the game ends. The bool payload is true when the run ended because a
        /// timed mode's clock reached zero (a success/finish), and false for a fatal collision
        /// (a failure). UI uses this to show "TIME'S UP!" vs "GAME OVER".
        /// </summary>
        public event Action<bool>      OnGameOver;
        public event Action            OnPaused;
        public event Action            OnResumed;
        public event Action            OnLevelComplete;

        // --- Public API ---

        /// <summary>Begin a new game session with the given mode.</summary>
        public void StartGame(GameMode mode)
        {
            CurrentMode = mode;
            SetState(GameState.Playing);
            OnGameStarted?.Invoke(mode);
        }

        /// <summary>Pause the current game session.</summary>
        public void PauseGame()
        {
            if (CurrentState != GameState.Playing) return;
            Time.timeScale = 0f;
            SetState(GameState.Paused);
            OnPaused?.Invoke();
        }

        /// <summary>Resume a paused game session.</summary>
        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(GameState.Playing);
            OnResumed?.Invoke();
        }

        /// <summary>
        /// Trigger game over sequence. Called by SnakeController/GameSession on a fatal
        /// collision, and by LevelManager when a timed mode's clock reaches zero.
        /// </summary>
        /// <param name="timerExpired">
        /// True when the run ended because the timer hit zero (a Time Attack finish), false
        /// for a fatal collision. Propagated to <see cref="OnGameOver"/> so UI can distinguish
        /// "TIME'S UP!" from "GAME OVER".
        /// </param>
        public void TriggerGameOver(bool timerExpired = false)
        {
            if (CurrentState != GameState.Playing) return;
            SetState(GameState.GameOver);
            OnGameOver?.Invoke(timerExpired);
        }

        /// <summary>Trigger level complete sequence. Called by LevelManager on win condition.</summary>
        public void TriggerLevelComplete()
        {
            if (CurrentState != GameState.Playing) return;
            SetState(GameState.LevelComplete);
            OnLevelComplete?.Invoke();
        }

        /// <summary>Return to main menu state.</summary>
        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            SetState(GameState.MainMenu);
        }

        // --- Internal ---

        private void SetState(GameState newState)
        {
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
