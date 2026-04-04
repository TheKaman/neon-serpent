using UnityEngine;
using NeonSerpent.Grid;
using NeonSerpent.Scoring;
using NeonSerpent.Snake;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Listens to ScoreManager events and spawns FloatingScoreText popups
    /// at the snake's current head position (world space).
    /// Attach to the Game scene's UI root. Assign references via Inspector.
    /// </summary>
    public class FloatingTextSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ScoreManager    _scoreManager;
        [SerializeField] private SnakeController _snake;
        [SerializeField] private GridSystem      _grid;

        [Header("Prefab")]
        [SerializeField] private FloatingScoreText _floatingTextPrefab;

        [Header("Settings")]
        [Tooltip("World-space Y offset above the snake head where text spawns.")]
        [SerializeField] private float _spawnYOffset = 0.5f;

        private void OnEnable()
        {
            if (_scoreManager == null) return;
            _scoreManager.OnScoreAdded    += HandleScoreAdded;
            _scoreManager.OnComboAchieved += HandleComboAchieved;
        }

        private void OnDisable()
        {
            if (_scoreManager == null) return;
            _scoreManager.OnScoreAdded    -= HandleScoreAdded;
            _scoreManager.OnComboAchieved -= HandleComboAchieved;
        }

        private void HandleScoreAdded(long points)
        {
            if (points <= 0 || _floatingTextPrefab == null) return;
            Spawn(points, isCombo: false);
        }

        private void HandleComboAchieved(int comboCount)
        {
            // The combo bonus points were already sent via OnScoreAdded;
            // we only need the special "COMBO" label popup here.
            if (_floatingTextPrefab == null) return;
            Spawn(comboCount, isCombo: true);
        }

        private void Spawn(long value, bool isCombo)
        {
            Vector3 headWorld = _snake != null && _grid != null
                ? _grid.GridToWorld(_snake.HeadPosition)
                : (_snake != null
                    ? new Vector3(_snake.HeadPosition.x, _snake.HeadPosition.y, 0f)
                    : transform.position);

            Vector3 spawnPos = headWorld + Vector3.up * _spawnYOffset;

            FloatingScoreText popup = Instantiate(_floatingTextPrefab, spawnPos, Quaternion.identity);
            popup.Populate(value, isCombo);  // FloatingScoreText.Populate accepts long
        }
    }
}
