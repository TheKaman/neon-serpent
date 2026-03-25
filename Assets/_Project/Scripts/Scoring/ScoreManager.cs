using System;
using UnityEngine;
using NeonSerpent.Utilities;

namespace NeonSerpent.Scoring
{
    /// <summary>
    /// Tracks current score, multiplier, and high scores per game mode.
    /// All score changes go through AddScore() so multipliers and poison flags apply correctly.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        private int  _currentScore;
        private int  _multiplier = 1;
        private bool _nextEatScoreZero;

        public int CurrentScore => _currentScore;
        public int Multiplier   => _multiplier;

        public event Action<int> OnScoreChanged;    // fires with new total
        public event Action<int> OnScoreAdded;      // fires with points just added (for floating text)

        /// <summary>Reset score for a new game session.</summary>
        public void ResetScore()
        {
            _currentScore    = 0;
            _multiplier      = 1;
            _nextEatScoreZero = false;
            OnScoreChanged?.Invoke(0);
        }

        /// <summary>Add points, applying the current multiplier and any poison flag.</summary>
        public void AddScore(int basePoints)
        {
            if (_nextEatScoreZero)
            {
                _nextEatScoreZero = false;
                return;
            }
            int points = basePoints * _multiplier;
            _currentScore += points;
            OnScoreAdded?.Invoke(points);
            OnScoreChanged?.Invoke(_currentScore);
        }

        /// <summary>Set the score multiplier (used by ScoreMultiplierEffect).</summary>
        public void SetMultiplier(int multiplier) => _multiplier = Mathf.Max(1, multiplier);

        /// <summary>When true, the next food eat adds zero score (used by PoisonEffect).</summary>
        public void SetNextEatScoreZero(bool value) => _nextEatScoreZero = value;
    }
}
