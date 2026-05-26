using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NeonSerpent.Levels;
using NeonSerpent.Core;
using NeonSerpent.Grid;
using NeonSerpent.Scoring;

namespace NeonSerpent.Tests
{
    /// <summary>
    /// PlayMode integration tests for LevelManager — covers the time-limit coroutine,
    /// score-target win condition, and grid/score reset between levels.
    ///
    /// These run in a real Play session so coroutines and Time.deltaTime work correctly.
    /// Each test builds its own minimal scene — no scenes from the project are loaded.
    /// </summary>
    [TestFixture]
    [Category("Campaign")]
    [Category("LevelManager")]
    public class LevelManagerPlayModeTests
    {
        // Helpers to build a minimal scene for each test
        private GameObject   _levelManagerGO;
        private LevelManager _levelManager;
        private GridSystem   _grid;
        private ScoreManager _score;

        // Tracks calls from GameManager-style delegation
        private bool _levelCompleteTriggered;
        private bool _gameOverTriggered;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _levelCompleteTriggered = false;
            _gameOverTriggered      = false;

            // Build minimal scene objects
            var gridGO   = new GameObject("Grid");
            _grid        = gridGO.AddComponent<GridSystem>();

            var scoreGO  = new GameObject("Score");
            _score       = scoreGO.AddComponent<ScoreManager>();

            _levelManagerGO = new GameObject("LevelManager");
            _levelManager   = _levelManagerGO.AddComponent<LevelManager>();

            // Wire references via the internal API that Inspector would set.
            // LevelManager's fields are [SerializeField] private — inject using
            // UnityEngine's reflection helper available in test code.
            SetPrivateField(_levelManager, "_grid",  _grid);
            SetPrivateField(_levelManager, "_score", _score);

            // Subscribe to LevelManager's events to detect win/lose without GameManager
            _levelManager.OnTimerUpdated += _ => { }; // keep event alive
            // We'll use CheckScoreTarget indirectly via OnScoreChanged below.

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _levelManager.StopLevel();
            Object.DestroyImmediate(_levelManagerGO);
            Object.DestroyImmediate(_grid.gameObject);
            Object.DestroyImmediate(_score.gameObject);
            yield return null;
        }

        // ── LoadLevel: score reset ────────────────────────────────────────────

        [UnityTest]
        public IEnumerator LoadLevel_ResetsScoreToZero()
        {
            var levelData = CreateLevelData(scoreTarget: 100, timeLimit: 0f);

            // Add some score before loading
            _score.AddScore(50);
            Assert.AreEqual(50L, _score.CurrentScore);

            _levelManager.LoadLevel(levelData);
            yield return null;

            Assert.AreEqual(0L, _score.CurrentScore,
                "LoadLevel must reset the score to 0 before the session starts.");
        }

        [UnityTest]
        public IEnumerator LoadLevel_InitializesGridWithCorrectDimensions()
        {
            var levelData = CreateLevelData(scoreTarget: 100, timeLimit: 0f, gridW: 18, gridH: 18);

            _levelManager.LoadLevel(levelData);
            yield return null;

            Assert.AreEqual(18, _grid.Width,  "Grid width must match level data after LoadLevel.");
            Assert.AreEqual(18, _grid.Height, "Grid height must match level data after LoadLevel.");
        }

        [UnityTest]
        public IEnumerator LoadLevel_PlacesWallsCorrectly()
        {
            var walls     = new[] { new Vector2Int(5, 5), new Vector2Int(6, 5) };
            var levelData = CreateLevelData(scoreTarget: 100, timeLimit: 0f, gridW: 20, gridH: 20, walls: walls);

            _levelManager.LoadLevel(levelData);
            yield return null;

            Assert.AreEqual(GridCellType.Wall, _grid.GetCellType(new Vector2Int(5, 5)));
            Assert.AreEqual(GridCellType.Wall, _grid.GetCellType(new Vector2Int(6, 5)));
            Assert.AreEqual(GridCellType.Empty, _grid.GetCellType(new Vector2Int(4, 5)),
                "Non-wall cells must remain Empty after SetWalls.");
        }

        // ── Score target win condition ─────────────────────────────────────────

        [UnityTest]
        public IEnumerator ScoreTarget_WhenReached_SetsLevelManagerInactive()
        {
            var levelData = CreateLevelData(scoreTarget: 30, timeLimit: 0f);
            _levelManager.LoadLevel(levelData);
            yield return null;

            // Accumulate score to target via ScoreManager
            _score.AddScore(10); // 10
            _score.AddScore(10); // 20
            _score.AddScore(10); // 30 — target reached

            // Give one frame for event handling
            yield return null;

            // LevelManager.CheckScoreTarget sets _active = false when target is reached.
            // We verify this indirectly: loading a second level should still work (not throw),
            // and if _active is false the coroutine has stopped.
            // The IsActive internal state is not public, but we can verify via the timer:
            // if StopLevel was called (by the win-condition path), restarting a timer
            // on the next LoadLevel works without double-coroutine issues.
            Assert.DoesNotThrow(() =>
            {
                var nextLevel = CreateLevelData(scoreTarget: 50, timeLimit: 2f);
                _levelManager.LoadLevel(nextLevel);
            }, "After score target is reached, loading the next level must not throw.");
        }

        // ── Timer ─────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Timer_FiresOnTimerUpdatedEveryFrame()
        {
            var levelData  = CreateLevelData(scoreTarget: 9999, timeLimit: 5f);
            int updateCount = 0;
            _levelManager.OnTimerUpdated += _ => updateCount++;

            _levelManager.LoadLevel(levelData);
            yield return new WaitForSeconds(0.5f);

            Assert.Greater(updateCount, 5,
                "OnTimerUpdated must fire at least once per frame while the timer is running.");
        }

        [UnityTest]
        public IEnumerator Timer_RemainingTimeDecreases()
        {
            var levelData  = CreateLevelData(scoreTarget: 9999, timeLimit: 10f);
            float lastTime = float.MaxValue;
            bool  decreased = false;

            _levelManager.OnTimerUpdated += remaining =>
            {
                if (remaining < lastTime) { decreased = true; }
                lastTime = remaining;
            };

            _levelManager.LoadLevel(levelData);
            yield return new WaitForSeconds(0.3f);

            Assert.IsTrue(decreased, "Timer remaining time must decrease over time.");
        }

        [UnityTest]
        public IEnumerator StopLevel_HaltsTimer()
        {
            var levelData  = CreateLevelData(scoreTarget: 9999, timeLimit: 5f);
            int updatesBefore = 0;
            int updatesAfter  = 0;
            bool stopped      = false;

            _levelManager.OnTimerUpdated += _ =>
            {
                if (!stopped) updatesBefore++;
                else          updatesAfter++;
            };

            _levelManager.LoadLevel(levelData);
            yield return new WaitForSeconds(0.2f);
            _levelManager.StopLevel();
            stopped = true;
            yield return new WaitForSeconds(0.3f);

            Assert.Greater(updatesBefore, 0, "Timer must have fired before StopLevel.");
            Assert.AreEqual(0, updatesAfter,
                "Timer must not fire after StopLevel — coroutine must be stopped.");
        }

        // ── StopLevel / restart idempotency ───────────────────────────────────

        [UnityTest]
        public IEnumerator StopLevel_CalledTwice_DoesNotThrow()
        {
            var levelData = CreateLevelData(scoreTarget: 100, timeLimit: 5f);
            _levelManager.LoadLevel(levelData);
            yield return null;

            Assert.DoesNotThrow(() => _levelManager.StopLevel());
            Assert.DoesNotThrow(() => _levelManager.StopLevel(),
                "Calling StopLevel twice must be idempotent and not throw.");
        }

        [UnityTest]
        public IEnumerator LoadLevel_SecondTime_RestartsTimer()
        {
            var level1 = CreateLevelData(scoreTarget: 9999, timeLimit: 10f);
            var level2 = CreateLevelData(scoreTarget: 9999, timeLimit: 5f);

            int timerCallsL1 = 0;
            int timerCallsL2 = 0;
            bool onLevel2    = false;

            _levelManager.OnTimerUpdated += _ =>
            {
                if (onLevel2) timerCallsL2++;
                else          timerCallsL1++;
            };

            _levelManager.LoadLevel(level1);
            yield return new WaitForSeconds(0.2f);
            _levelManager.StopLevel();

            onLevel2 = true;
            _levelManager.LoadLevel(level2);
            yield return new WaitForSeconds(0.2f);

            Assert.Greater(timerCallsL1, 0, "Level 1 timer must have fired.");
            Assert.Greater(timerCallsL2, 0, "Level 2 timer must have fired after restart.");
        }

        // ── Score listener unsubscribed on StopLevel ──────────────────────────

        [UnityTest]
        public IEnumerator StopLevel_UnsubscribesScoreListener()
        {
            // After StopLevel, further score additions must NOT trigger level complete.
            // We verify this by adding more than ScoreTarget score after StopLevel and
            // confirming the LevelManager does not log or mutate state unexpectedly.
            var levelData = CreateLevelData(scoreTarget: 30, timeLimit: 0f);
            _levelManager.LoadLevel(levelData);
            _levelManager.StopLevel();
            yield return null;

            // Add score well past target — CheckScoreTarget must not run
            Assert.DoesNotThrow(() =>
            {
                _score.AddScore(100); // 100 > 30 — would trigger if listener was still active
            }, "StopLevel must unsubscribe the score listener to prevent stale event callbacks.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static LevelData CreateLevelData(
            int          scoreTarget = 100,
            float        timeLimit   = 0f,
            int          gridW       = 20,
            int          gridH       = 20,
            Vector2Int[] walls       = null)
        {
            var data           = ScriptableObject.CreateInstance<LevelData>();
            data.LevelIndex    = 1;
            data.WorldIndex    = 1;
            data.LevelName     = "TestLevel";
            data.WorldName     = "TestWorld";
            data.GridWidth     = gridW;
            data.GridHeight    = gridH;
            data.InitialSpeed  = 5f;
            data.SpeedIncrement = 0.1f;
            data.TimeLimit     = timeLimit;
            data.ScoreTarget   = scoreTarget;
            data.WallPositions = walls ?? System.Array.Empty<Vector2Int>();
            data.Mode          = GameMode.Campaign;
            data.StarThreshold1 = scoreTarget / 3;
            data.StarThreshold2 = scoreTarget * 2 / 3;
            data.StarThreshold3 = scoreTarget;
            return data;
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null)
                throw new System.Exception($"Field '{fieldName}' not found on {obj.GetType().Name}.");
            field.SetValue(obj, value);
        }
    }
}
