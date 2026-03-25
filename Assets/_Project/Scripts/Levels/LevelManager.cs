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
        [SerializeField] private GridSystem      _grid;
        [SerializeField] private SnakeController _snake;
        [SerializeField] private ScoreManager    _score;

        [Header("Level to Load")]
        [SerializeField] private LevelData _levelData;

        private float _elapsedTime;
        private bool  _active;

        private Coroutine _timerCoroutine;

        public LevelData CurrentLevel => _levelData;

        public event System.Action<float> OnTimerUpdated;   // remaining seconds

        /// <summary>Apply the given level config and start the session.</summary>
        public void LoadLevel(LevelData data)
        {
            _levelData = data;
            _grid.InitializeGrid(data.GridWidth, data.GridHeight);
            _grid.SetWalls(data.WallPositions);

            // TODO: pass speed values to SnakeController once exposed via Initialize()
            _elapsedTime = 0f;
            _active      = true;

            _score.ResetScore();

            if (data.TimeLimit > 0)
            {
                if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
                _timerCoroutine = StartCoroutine(TimerRoutine(data.TimeLimit));
            }

            // Subscribe to score for score-target win condition
            _score.OnScoreChanged += CheckScoreTarget;
        }

        private void OnDisable()
        {
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }
            _score.OnScoreChanged -= CheckScoreTarget;
        }

        private void CheckScoreTarget(int newScore)
        {
            if (!_active) return;
            if (_levelData.ScoreTarget > 0 && newScore >= _levelData.ScoreTarget)
            {
                _active = false;
                GameManager.Instance.TriggerLevelComplete();
            }
        }

        private IEnumerator TimerRoutine(float timeLimit)
        {
            float remaining = timeLimit;
            while (remaining > 0f && _active)
            {
                remaining -= Time.deltaTime;
                OnTimerUpdated?.Invoke(Mathf.Max(0f, remaining));
                yield return null;
            }
            if (_active)
            {
                _active = false;
                GameManager.Instance.TriggerGameOver();
            }
        }
    }
}
