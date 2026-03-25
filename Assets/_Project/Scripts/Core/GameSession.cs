using UnityEngine;
using NeonSerpent.Snake;
using NeonSerpent.Food;
using NeonSerpent.PowerUps;
using NeonSerpent.Grid;
using NeonSerpent.Levels;
using NeonSerpent.Audio;

namespace NeonSerpent.Core
{
    /// <summary>
    /// Coordinates all in-scene game systems. Handles both:
    /// - Normal flow (loaded via Bootstrap → GameManager exists)
    /// - Direct play from Editor (Bootstrap not loaded → starts immediately)
    /// </summary>
    public class GameSession : MonoBehaviour
    {
        [Header("Core Systems")]
        [SerializeField] private GridSystem      _grid;
        [SerializeField] private SnakeController _snake;
        [SerializeField] private FoodSpawner     _foodSpawner;
        [SerializeField] private PowerUpSpawner  _powerUpSpawner;
        [SerializeField] private PowerUpManager  _powerUpManager;
        [SerializeField] private LevelManager    _levelManager;

        [Header("Start Position (grid coords)")]
        [SerializeField] private Vector2Int _snakeStartPos = new Vector2Int(5, 10);

        private bool _sessionStarted;

        private void Start()
        {
            var gm = GameManager.Instance;

            if (gm == null)
            {
                // No Bootstrap loaded — playing directly from Editor. Start immediately.
                BeginSession();
                return;
            }

            // Subscribe to GameManager events
            gm.OnGameStarted += HandleGameStarted;
            gm.OnGameOver    += HandleGameOver;
            gm.OnPaused      += HandlePaused;
            gm.OnResumed     += HandleResumed;

            // Subscribe to snake events
            SubscribeSnakeEvents();

            // If GameManager is already in Playing state (scene loaded mid-game), begin now
            if (gm.CurrentState == GameState.Playing)
                BeginSession();
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            gm.OnGameStarted -= HandleGameStarted;
            gm.OnGameOver    -= HandleGameOver;
            gm.OnPaused      -= HandlePaused;
            gm.OnResumed     -= HandleResumed;

            UnsubscribeSnakeEvents();
        }

        private void SubscribeSnakeEvents()
        {
            if (_snake == null) return;
            _snake.OnAteFood    += HandleAteFood;
            _snake.OnAtePowerUp += HandleAtePowerUp;
            _snake.OnHitWall    += HandleFatalHit;
            _snake.OnHitSelf    += HandleFatalHit;
        }

        private void UnsubscribeSnakeEvents()
        {
            if (_snake == null) return;
            _snake.OnAteFood    -= HandleAteFood;
            _snake.OnAtePowerUp -= HandleAtePowerUp;
            _snake.OnHitWall    -= HandleFatalHit;
            _snake.OnHitSelf    -= HandleFatalHit;
        }

        private void HandleGameStarted(GameMode mode) => BeginSession();

        private void BeginSession()
        {
            if (_sessionStarted) return;
            _sessionStarted = true;

            // Also wire snake events if not done yet (direct-play path)
            SubscribeSnakeEvents();

            if (_levelManager != null && _levelManager.CurrentLevel != null)
                _levelManager.LoadLevel(_levelManager.CurrentLevel);

            _snake.Initialize(_snakeStartPos);
            _foodSpawner.StartSpawning();

            var allowed = _levelManager?.CurrentLevel?.AllowedPowerUps;
            _powerUpSpawner?.StartSpawning(allowed);
        }

        private void HandleGameOver()
        {
            _sessionStarted = false;
            _foodSpawner.StopSpawning();
            _powerUpSpawner?.StopSpawning();
            _powerUpManager?.ClearAll();
            AudioManager.Instance?.PlaySFX(SoundEvent.Death);
        }

        private void HandlePaused()  => Time.timeScale = 0f;
        private void HandleResumed() => Time.timeScale = 1f;

        private void HandleAteFood(Vector2Int pos)
        {
            _foodSpawner.HandleFoodEaten(pos);
            AudioManager.Instance?.PlaySFX(SoundEvent.EatFood);
        }

        private void HandleAtePowerUp(Vector2Int pos)
        {
            AudioManager.Instance?.PlaySFX(SoundEvent.PowerUpCollect);
        }

        private void HandleFatalHit()
        {
            var gm = GameManager.Instance;
            if (gm != null)
                gm.TriggerGameOver();
            else
            {
                // Direct play — just reset
                _sessionStarted = false;
                _snake.Initialize(_snakeStartPos);
                _foodSpawner.StopSpawning();
                _foodSpawner.StartSpawning();
            }
        }
    }
}
