using UnityEngine;
using NeonSerpent.Snake;
using NeonSerpent.Food;
using NeonSerpent.PowerUps;
using NeonSerpent.Grid;
using NeonSerpent.Leaderboard;
using NeonSerpent.Levels;
using NeonSerpent.Audio;
using NeonSerpent.Scoring;
using NeonSerpent.SaveData;
using NeonSerpent.Utilities;
using NeonSerpent.VFX;
using NeonSerpent.UI;

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

        [Header("Scoring")]
        [SerializeField] private ScoreManager _scoreManager;

        [Header("Time Attack")]
        [SerializeField] private LevelData _timeAttackLevel;

        [Header("Leaderboard (optional — assign LeaderboardServiceProvider from Bootstrap)")]
        [SerializeField] private LeaderboardServiceProvider _leaderboardProvider;

        [Header("Countdown (optional — skip countdown if null)")]
        [SerializeField] private CountdownUI _countdownUI;

        [Header("Start Position (grid coords)")]
        [SerializeField] private Vector2Int _snakeStartPos = new Vector2Int(5, 10);

        private bool     _sessionStarted;
        // Tracks whether a countdown coroutine is currently running.
        // Prevents a second BeginSession() call during the countdown from launching
        // a second overlapping countdown (Bug 7 — rapid restart double-countdown).
        private bool     _countdownRunning;
        private FoodType _lastEatenFoodType = FoodType.Normal;

        // Used only when GameManager is absent (direct Editor play) so the mode picker
        // can communicate the chosen mode to StartSessionInternal.
        private GameMode _editorSelectedMode = GameMode.ClassicEndless;

        // Fired when the session ends — GameOverUI subscribes to this
        public event System.Action OnSessionGameOver;

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
            gm.OnGameStarted    += HandleGameStarted;
            gm.OnGameOver       += HandleGameOver;
            gm.OnLevelComplete  += HandleLevelComplete;
            gm.OnPaused         += HandlePaused;
            gm.OnResumed        += HandleResumed;

            // Subscribe to snake events
            SubscribeSnakeEvents();

            // If GameManager is already in Playing state (scene loaded mid-game), begin now
            if (gm.CurrentState == GameState.Playing)
                BeginSession();
        }

        private void OnDestroy()
        {
            // Unsubscribe countdown so it doesn't fire after this object is destroyed
            if (_countdownUI != null)
                _countdownUI.OnCountdownComplete -= OnCountdownComplete;

            var gm = GameManager.Instance;
            if (gm == null) return;

            gm.OnGameStarted   -= HandleGameStarted;
            gm.OnGameOver      -= HandleGameOver;
            gm.OnLevelComplete -= HandleLevelComplete;
            gm.OnPaused        -= HandlePaused;
            gm.OnResumed       -= HandleResumed;

            UnsubscribeSnakeEvents();
        }

        private void SubscribeSnakeEvents()
        {
            if (_snake == null) return;
            // Unsubscribe first to prevent double-registration on restart
            _snake.OnAteFood          -= HandleAteFood;
            _snake.OnAtePowerUp       -= HandleAtePowerUp;
            _snake.OnHitWall          -= HandleFatalHit;
            _snake.OnHitSelf          -= HandleFatalHit;
            _snake.OnShieldAbsorbed   -= HandleShieldAbsorbed;
            _snake.OnMaxSpeedReached  -= HandleMaxSpeedReached;
            _snake.OnAteFood          += HandleAteFood;
            _snake.OnAtePowerUp       += HandleAtePowerUp;
            _snake.OnHitWall          += HandleFatalHit;
            _snake.OnHitSelf          += HandleFatalHit;
            _snake.OnShieldAbsorbed   += HandleShieldAbsorbed;
            _snake.OnMaxSpeedReached  += HandleMaxSpeedReached;

            if (_foodSpawner != null)
            {
                _foodSpawner.OnFoodEaten -= HandleFoodTypeEaten;
                _foodSpawner.OnFoodEaten += HandleFoodTypeEaten;
            }

            if (_scoreManager != null)
            {
                _scoreManager.OnComboAchieved -= HandleComboAchieved;
                _scoreManager.OnComboAchieved += HandleComboAchieved;
            }

            if (_powerUpManager != null)
            {
                _powerUpManager.OnEffectDeactivated -= HandleEffectDeactivated;
                _powerUpManager.OnEffectDeactivated += HandleEffectDeactivated;
            }
        }

        private void UnsubscribeSnakeEvents()
        {
            if (_snake == null) return;
            _snake.OnAteFood         -= HandleAteFood;
            _snake.OnAtePowerUp      -= HandleAtePowerUp;
            _snake.OnHitWall         -= HandleFatalHit;
            _snake.OnHitSelf         -= HandleFatalHit;
            _snake.OnShieldAbsorbed  -= HandleShieldAbsorbed;
            _snake.OnMaxSpeedReached -= HandleMaxSpeedReached;

            if (_foodSpawner != null)
                _foodSpawner.OnFoodEaten -= HandleFoodTypeEaten;

            if (_scoreManager != null)
                _scoreManager.OnComboAchieved -= HandleComboAchieved;

            if (_powerUpManager != null)
                _powerUpManager.OnEffectDeactivated -= HandleEffectDeactivated;
        }

        private void HandleGameStarted(GameMode mode) => BeginSession();

        /// <summary>
        /// Editor-only entry point called by EditorModePicker when no GameManager is present.
        /// Stores the chosen mode so StartSessionInternal uses the correct level config.
        /// </summary>
        public void BeginEditorSession(GameMode mode)
        {
            _editorSelectedMode = mode;
            BeginSession();
        }

        /// <summary>
        /// Start or restart the game session.
        /// If a CountdownUI is assigned and the session has not yet begun, the countdown
        /// runs first and BeginSession() is re-invoked automatically when it finishes.
        /// Without a CountdownUI (e.g. direct Editor play) the session starts immediately.
        /// </summary>
        public void BeginSession()
        {
            // Guard both the fully-started state and an in-progress countdown.
            // Without the _countdownRunning check, a second call during the countdown
            // would launch a second overlapping coroutine (Bug 7 — rapid restart).
            if (_sessionStarted || _countdownRunning) return;

            // Delegate to countdown if available and not yet started
            if (_countdownUI != null)
            {
                _countdownRunning = true;
                _countdownUI.OnCountdownComplete -= OnCountdownComplete;
                _countdownUI.OnCountdownComplete += OnCountdownComplete;
                _countdownUI.StartCountdown();
                return; // actual start happens in OnCountdownComplete callback
            }

            StartSessionInternal();
        }

        private void OnCountdownComplete()
        {
            _countdownRunning = false;

            if (_countdownUI != null)
                _countdownUI.OnCountdownComplete -= OnCountdownComplete;

            StartSessionInternal();
        }

        /// <summary>Core session initialisation — called after countdown (or immediately if no countdown).</summary>
        private void StartSessionInternal()
        {
            if (_sessionStarted) return;
            _sessionStarted = true;

            // Wire snake events (safe to call multiple times — unsubscribes first)
            SubscribeSnakeEvents();

            // Load the appropriate level config based on game mode
            if (_levelManager != null)
            {
                GameMode mode = GameManager.Instance != null
                    ? GameManager.Instance.CurrentMode
                    : _editorSelectedMode;

                LevelData levelToLoad = mode switch
                {
                    GameMode.Campaign   => GameManager.Instance?.SelectedLevel ?? _levelManager.CurrentLevel,
                    GameMode.TimeAttack => _timeAttackLevel ?? _levelManager.CurrentLevel,
                    _                   => null   // Classic — no level config needed
                };

                // Bug 3: warn when Time Attack has no level data so the timer and score
                // reset never runs and the broken state is surfaced immediately in logs.
                if (mode == GameMode.TimeAttack && levelToLoad == null)
                {
                    Debug.LogError("[GameSession] Time Attack mode has no LevelData. " +
                        "Assign _timeAttackLevel on GameSession in the Inspector, or re-run NeonSerpent → Setup Project.");
                }

                if (levelToLoad != null)
                    _levelManager.LoadLevel(levelToLoad);
                else
                {
                    // Bug C fix: Classic mode skips LoadLevel so the score never gets
                    // reset by LevelManager. Reset it here before the snake spawns.
                    _scoreManager?.ResetScore();
                }
            }
            else
            {
                // No LevelManager at all (stripped-down test scene) — reset defensively.
                _scoreManager?.ResetScore();
            }

            _snake?.Initialize(_snakeStartPos);

            // Apply per-level initial speed now that Initialize() has reset it to DEFAULT_SPEED.
            // Must happen after Initialize so the coroutine restart uses the correct value (Bug 4).
            if (_levelManager?.CurrentLevel != null && _snake != null)
                _snake.SetSpeed(_levelManager.CurrentLevel.InitialSpeed);

            _foodSpawner?.StartSpawning();

            var allowed = _levelManager?.CurrentLevel?.AllowedPowerUps;
            _powerUpSpawner?.StartSpawning(allowed);
        }

        private void HandleGameOver()
        {
            _sessionStarted   = false;
            _countdownRunning = false;
            // Stop the countdown coroutine so it cannot call OnCountdownComplete
            // and start a new session while in the GameOver state (Bug 8).
            _countdownUI?.CancelCountdown();
            _snake?.StopSnake();
            _levelManager?.StopLevel();
            _foodSpawner?.StopSpawning();
            _powerUpSpawner?.StopSpawning();
            _powerUpManager?.ClearAll();
        }

        /// <summary>
        /// Fired by GameManager when a campaign level win condition is met.
        /// Pauses spawners and resets the session-started flag so BeginSession()
        /// can be called again when the player proceeds to the next level.
        /// Persists coins that were accumulated during the level (AwardCoins no longer
        /// auto-saves on every food eat — we flush once here instead).
        /// </summary>
        private void HandleLevelComplete()
        {
            _sessionStarted   = false;
            _countdownRunning = false;   // cancel any in-progress countdown
            _snake?.StopSnake();
            _levelManager?.StopLevel();
            _foodSpawner?.StopSpawning();
            _powerUpSpawner?.StopSpawning();
            _powerUpManager?.ClearAll();

            // Flush coins accumulated during the level to disk.
            SaveManager.Instance?.Save();
        }

        private void HandlePaused()  => Time.timeScale = 0f;
        private void HandleResumed() => Time.timeScale = 1f;

        private void HandleAteFood(Vector2Int pos)
        {
            // HandleFoodEaten fires OnFoodEaten synchronously, which sets _lastEatenFoodType
            // before we need it below — so call it first.
            _foodSpawner?.HandleFoodEaten(pos);

            // Use the loaded level's SpeedIncrement if available, otherwise fall back to
            // the global constant so Classic mode (no LevelData) still accelerates (Bug 4).
            float speedInc = _levelManager?.CurrentLevel?.SpeedIncrement ?? Constants.SPEED_INCREMENT;
            _snake?.IncrementSpeed(speedInc);
            // Play distinct audio for poison food so the player has clear feedback.
            // _lastEatenFoodType is set by HandleFoodTypeEaten which fires synchronously before this.
            SoundEvent eatSound = _lastEatenFoodType == FoodType.Poison
                ? SoundEvent.EatPoison
                : SoundEvent.EatFood;
            AudioManager.Instance?.PlaySFX(eatSound);
            CameraShake.Instance?.Shake(0.1f, 0.05f);

            // Short haptic pulse on food collection — respects the player's vibration setting.
            if (SaveManager.Instance?.Data?.vibrationEnabled == true)
                VibrateShort();

            Vector3 worldPos = _grid != null ? _grid.GridToWorld(pos) : Vector3.zero;
            if (_lastEatenFoodType == FoodType.Bonus)
                VFXManager.Instance?.PlayBonusEatBurst(worldPos);
            else
                VFXManager.Instance?.PlayEatBurst(worldPos, Color.cyan);
        }

        private void HandleAtePowerUp(Vector2Int pos)
        {
            Vector3 worldPos = _grid != null ? _grid.GridToWorld(pos) : Vector3.zero;
            VFXManager.Instance?.PlayPowerUpBurst(worldPos);

            // Trigger collection on the spawner first so the effect is applied before the
            // audio cue plays — keeps cause and effect in the same frame.
            _powerUpSpawner?.CollectAt(pos);
            AudioManager.Instance?.PlaySFX(SoundEvent.PowerUpCollect);
        }

        private void HandleFatalHit()
        {
            _sessionStarted   = false;
            // Clear countdown flags so BeginSession() is not permanently blocked if
            // the snake dies during the countdown window (e.g. Editor direct-play, H4).
            _countdownRunning = false;
            _countdownUI?.CancelCountdown();
            _levelManager?.StopLevel();
            _foodSpawner?.StopSpawning();
            _powerUpSpawner?.StopSpawning();
            _powerUpManager?.ClearAll();
            // Clear Frenzy Mode immediately on death so the HUD indicator does not
            // stay lit during the game-over screen. ResetScore() is only called at the
            // start of the NEXT session, which is too late for the indicator.
            _scoreManager?.SetFrenzyMode(false);
            AudioManager.Instance?.PlaySFX(SoundEvent.Death);
            CameraShake.Instance?.Shake(0.3f, 0.15f);

            // Haptic feedback on death — respects the player's vibration setting.
            if (SaveManager.Instance?.Data?.vibrationEnabled == true)
                Handheld.Vibrate();

            // VFX — death explosion at head position
            if (_snake != null && _grid != null)
            {
                Vector3 headWorld = _grid.GridToWorld(_snake.HeadPosition);
                VFXManager.Instance?.PlayDeathExplosion(headWorld, Color.cyan);
            }

            ScreenFlash.Instance?.Flash(Color.white);

            RecordHighScore();

            // Always fire OnSessionGameOver so GameOverUI shows regardless of Bootstrap state
            OnSessionGameOver?.Invoke();

            // Also tell GameManager if it exists (full Bootstrap flow)
            GameManager.Instance?.TriggerGameOver();
        }

        /// <summary>Awards coins based on the type of food just eaten.</summary>
        private void HandleFoodTypeEaten(FoodType foodType)
        {
            // Cache the type so HandleAteFood's VFX branch can read it
            _lastEatenFoodType = foodType;

            if (SaveManager.Instance == null) return;

            int coins = foodType switch
            {
                FoodType.Bonus  => Constants.COINS_BONUS_FOOD,
                FoodType.Poison => 0,
                _               => Constants.COINS_NORMAL_FOOD
            };

            SaveManager.Instance.AwardCoins(coins);
        }

        /// <summary>Awards bonus coins when a combo milestone is reached.</summary>
        private void HandleComboAchieved(int comboCount)
        {
            SaveManager.Instance?.AwardCoins(Constants.COINS_COMBO_BONUS);
        }

        /// <summary>
        /// Called when the snake's shield absorbs a fatal collision.
        /// Triggers a cyan screen flash and shield-break sound so the player
        /// receives clear feedback that the shield activated.
        /// </summary>
        private void HandleShieldAbsorbed()
        {
            ScreenFlash.Instance?.Flash(new Color(0f, 1f, 1f, 0.5f));
            AudioManager.Instance?.PlaySFX(SoundEvent.ShieldAbsorb);
        }

        /// <summary>
        /// Called when the snake first reaches MAX_SPEED. Activates Frenzy Mode in
        /// Classic Endless — doubles all scored points to reward surviving long enough
        /// to cap out the speed. Has no effect in Campaign or Time Attack modes.
        /// </summary>
        private void HandleMaxSpeedReached()
        {
            GameMode mode = GameManager.Instance != null
                ? GameManager.Instance.CurrentMode
                : _editorSelectedMode;

            if (mode == GameMode.ClassicEndless)
            {
                _scoreManager?.SetFrenzyMode(true);
                AudioManager.Instance?.PlaySFX(SoundEvent.FrenzyActivated);
            }
        }

        /// <summary>
        /// Called when any power-up effect expires. Checks for Poison specifically
        /// and fires a green flash + sound to signal that scoring has resumed.
        /// </summary>
        private void HandleEffectDeactivated(PowerUpType type)
        {
            if (type == PowerUpType.Poison)
            {
                ScreenFlash.Instance?.Flash(new Color(0.4f, 1f, 0.2f, 0.4f));
                AudioManager.Instance?.PlaySFX(SoundEvent.PoisonExpired);
            }
        }

        /// <summary>
        /// Fires a 30 ms Android vibration using the platform Java API.
        /// Produces a brief tap-like pulse rather than the ~400 ms buzz from
        /// <see cref="Handheld.Vibrate"/>. No-op in the Editor.
        /// Call only after checking the player's vibration preference.
        /// </summary>
        private void VibrateShort()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity    = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var vibrator    = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
            {
                vibrator.Call("vibrate", 30L);
            }
#endif
        }

        private void RecordHighScore()
        {
            if (_scoreManager == null || SaveManager.Instance == null) return;

            GameMode mode = GameManager.Instance != null
                ? GameManager.Instance.CurrentMode
                : _editorSelectedMode;

            string leaderboardId = mode switch
            {
                GameMode.TimeAttack => Constants.LEADERBOARD_TIME_ATTACK,
                GameMode.Campaign   => Constants.LEADERBOARD_CAMPAIGN,
                _                   => Constants.LEADERBOARD_CLASSIC
            };

            long score = _scoreManager.CurrentScore;
            SaveManager.Instance.Data.RecordLocalHighScore(leaderboardId, score);
            SaveManager.Instance.Save();

            // Submit to online leaderboard only when authenticated to avoid a silent
            // no-op or error when GPGS has not signed in yet (Bug 11).
            if (_leaderboardProvider != null && _leaderboardProvider.IsAuthenticated)
            {
                _leaderboardProvider.SubmitScore(score, leaderboardId, success =>
                {
                    if (!success)
                        Debug.Log("[GameSession] Online score submission failed — saved locally only.");
                });
            }
        }
    }
}
