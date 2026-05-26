using System.Collections;
using UnityEngine;
using NeonSerpent.Core;
using NeonSerpent.Grid;
using NeonSerpent.Snake;
using NeonSerpent.Scoring;

namespace NeonSerpent.Levels
{
    /// <summary>
    /// Applies a LevelData configuration to the running game scene.
    /// Monitors time limit and score target, notifying GameManager of win/lose conditions.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridSystem       _grid;
        [SerializeField] private SnakeController  _snake;
        [SerializeField] private ScoreManager     _score;
        /// <summary>
        /// Optional: assign the NeonGridRenderer so it rebuilds its border mesh when
        /// a level changes the grid dimensions (e.g. Campaign World 2+ has larger grids).
        /// Without this, the neon border stays the wrong size after the first level.
        /// </summary>
        [SerializeField] private NeonGridRenderer _gridRenderer;

        [Header("Level to Load")]
        [SerializeField] private LevelData _levelData;

        private bool _active;

        private Coroutine _timerCoroutine;

        public LevelData CurrentLevel => _levelData;

        public event System.Action<float>     OnTimerUpdated;  // remaining seconds
        public event System.Action<LevelData> OnLevelLoaded;   // fired after grid/walls are ready

        /// <summary>Apply the given level config and start the session.</summary>
        public void LoadLevel(LevelData data)
        {
            _levelData = data;

            if (_grid != null)
            {
                _grid.InitializeGrid(data.GridWidth, data.GridHeight);
                Camera.main?.GetComponent<CameraFit>()?.FitToGrid(data.GridWidth, data.GridHeight);
                // WallPositions is nullable when no walls are assigned in the Inspector
                if (data.WallPositions != null && data.WallPositions.Length > 0)
                    _grid.SetWalls(data.WallPositions);

                // Bug B fix: rebuild the neon border mesh to match the new grid dimensions.
                // Without this the border stays the wrong size when advancing to a level with
                // a different grid size (e.g. World1 16×16 → World2 18×18) without a
                // full scene reload.
                _gridRenderer?.BuildGridMesh();
            }

            _active = true;

            if (_score != null)
                _score.ResetScore();

            if (data.TimeLimit > 0)
            {
                if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
                _timerCoroutine = StartCoroutine(TimerRoutine(data.TimeLimit));
            }

            // Unsubscribe first to prevent double-registration on restart
            if (_score != null)
            {
                _score.OnScoreChanged -= CheckScoreTarget;
                _score.OnScoreChanged += CheckScoreTarget;
            }

            OnLevelLoaded?.Invoke(data);
        }

        /// <summary>
        /// Stop the active timer and unsubscribe score tracking. Call on game over or
        /// level complete so the timer does not keep running between sessions.
        /// </summary>
        public void StopLevel()
        {
            _active = false;
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }
            if (_score != null)
                _score.OnScoreChanged -= CheckScoreTarget;
        }

        private void OnDisable()
        {
            StopLevel();
        }

        private void CheckScoreTarget(long newScore)
        {
            if (!_active) return;
            if (_levelData.ScoreTarget > 0 && newScore >= _levelData.ScoreTarget)
            {
                _active = false;
                if (GameManager.Instance != null)
                    GameManager.Instance.TriggerLevelComplete();
            }
        }

        private IEnumerator TimerRoutine(float timeLimit)
        {
            float remaining = timeLimit;
            while (remaining > 0f && _active)
            {
                remaining -= Mathf.Min(Time.deltaTime, 0.1f); // cap spike so Android resume cannot expire the timer instantly
                OnTimerUpdated?.Invoke(Mathf.Max(0f, remaining));
                yield return null;
            }
            if (_active)
            {
                _active = false;
                if (GameManager.Instance != null)
                    GameManager.Instance.TriggerGameOver();
            }
        }
    }
}
