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

        // --- Events (subscribe to these, never call state-change methods from other systems) ---
        public event Action<GameState> OnStateChanged;
        public event Action<GameMode>  OnGameStarted;
        public event Action            OnGameOver;
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

        /// <summary>Trigger game over sequence. Called by SnakeController on fatal collision.</summary>
        public void TriggerGameOver()
        {
            if (CurrentState != GameState.Playing) return;
            SetState(GameState.GameOver);
            OnGameOver?.Invoke();
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
