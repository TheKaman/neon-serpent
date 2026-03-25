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
    /// The Game scene's coordinator. Wires together all in-scene systems by subscribing
    /// to SnakeController events and dispatching to the appropriate handlers.
    /// This is the only script that knows about all Game scene components simultaneously.
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
        [SerializeField] private Vector2Int _snakeStartPos = new Vector2Int(10, 10);

        private void OnEnable()
        {
            GameManager.Instance.OnGameStarted += HandleGameStarted;
            GameManager.Instance.OnGameOver    += HandleGameOver;
            GameManager.Instance.OnPaused      += HandlePaused;
            GameManager.Instance.OnResumed     += HandleResumed;

            _snake.OnAteFood   += HandleAteFood;
            _snake.OnAtePowerUp += HandleAtePowerUp;
            _snake.OnHitWall   += HandleFatalHit;
            _snake.OnHitSelf   += HandleFatalHit;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStarted -= HandleGameStarted;
                GameManager.Instance.OnGameOver    -= HandleGameOver;
                GameManager.Instance.OnPaused      -= HandlePaused;
                GameManager.Instance.OnResumed     -= HandleResumed;
            }

            _snake.OnAteFood    -= HandleAteFood;
            _snake.OnAtePowerUp -= HandleAtePowerUp;
            _snake.OnHitWall    -= HandleFatalHit;
            _snake.OnHitSelf    -= HandleFatalHit;
        }

        private void Start()
        {
            // If GameManager already set a mode before this scene loaded, begin immediately.
            // Otherwise wait for OnGameStarted event.
            if (GameManager.Instance.CurrentState == GameState.Playing)
                BeginSession();
        }

        private void HandleGameStarted(GameMode mode)
        {
            BeginSession();
        }

        private void BeginSession()
        {
            // Apply level config (uses defaults if no level set)
            if (_levelManager != null && _levelManager.CurrentLevel != null)
                _levelManager.LoadLevel(_levelManager.CurrentLevel);

            _snake.Initialize(_snakeStartPos);
            _foodSpawner.StartSpawning();

            var allowed = _levelManager?.CurrentLevel?.AllowedPowerUps;
            _powerUpSpawner?.StartSpawning(allowed);
        }

        private void HandleGameOver()
        {
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
            // PowerUpSpawner handles the pickup via its own OnCollected subscription.
            // Nothing extra needed here — just play sound.
            AudioManager.Instance?.PlaySFX(SoundEvent.PowerUpCollect);
        }

        private void HandleFatalHit()
        {
            GameManager.Instance.TriggerGameOver();
        }
    }
}
