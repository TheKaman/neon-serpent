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
        private long _currentScore;
        private int  _multiplier           = 1;
        private int  _poisonEatsRemaining;   // counts how many upcoming eats score zero
        private int  _comboCount;
        private int  _speedBoostBonusPerEat; // flat bonus per eat while SpeedBoost is active
        private bool _frenzyMode;            // doubles all points when max speed is reached

        public long CurrentScore => _currentScore;
        public int  Multiplier   => _multiplier;
        public int  ComboCount   => _comboCount;
        public bool FrenzyMode   => _frenzyMode;

        public event Action<long> OnScoreChanged;    // fires with new total
        public event Action<long> OnScoreAdded;      // fires with points just added (for floating text)
        public event Action<int>  OnComboAchieved;   // fires with combo count when threshold is hit
        public event Action<bool> OnFrenzyChanged;   // fires when Frenzy Mode toggles

        /// <summary>Reset score for a new game session.</summary>
        public void ResetScore()
        {
            _currentScore          = 0L;
            _multiplier            = 1;
            _poisonEatsRemaining   = 0;
            _comboCount            = 0;
            _speedBoostBonusPerEat = 0;
            bool wasFrenzy         = _frenzyMode;
            _frenzyMode            = false;
            if (wasFrenzy) OnFrenzyChanged?.Invoke(false);
            OnScoreChanged?.Invoke(0L);
        }

        /// <summary>Add points, applying the current multiplier, combo bonus, and any active effects.</summary>
        public void AddScore(int basePoints)
        {
            if (_poisonEatsRemaining > 0)
            {
                _poisonEatsRemaining--;
                _comboCount = 0;
                OnScoreChanged?.Invoke(_currentScore);
                return;
            }

            _comboCount++;

            // Use long arithmetic to prevent overflow on very long Classic Endless runs.
            long points     = (long)basePoints * _multiplier;
            long comboBonus = 0L;

            if (_comboCount > 0 && _comboCount % Constants.COMBO_THRESHOLD == 0)
            {
                comboBonus = (long)basePoints * _multiplier;
                OnComboAchieved?.Invoke(_comboCount);
            }

            // Frenzy Mode (max speed reached in Classic Endless) doubles all earned points.
            if (_frenzyMode)
            {
                points     *= 2;
                comboBonus *= 2;
            }

            long totalAdded = points + comboBonus + _speedBoostBonusPerEat;
            _currentScore += totalAdded;

            // Single event per eat — FloatingTextSpawner sees exactly one popup.
            OnScoreAdded?.Invoke(totalAdded);
            OnScoreChanged?.Invoke(_currentScore);
        }

        /// <summary>Break the combo streak (call on death or game over).</summary>
        public void ResetCombo() => _comboCount = 0;

        /// <summary>Set the score multiplier (used by ScoreMultiplierEffect).</summary>
        public void SetMultiplier(int multiplier) => _multiplier = Mathf.Max(1, multiplier);

        /// <summary>
        /// Set how many consecutive food eats will score zero (Poison effect).
        /// Each eat decrements the counter — set to 0 to cancel early.
        /// </summary>
        public void SetPoisonEatsRemaining(int count) => _poisonEatsRemaining = Mathf.Max(0, count);

        /// <summary>Flat bonus points added to every eat while SpeedBoost is active.</summary>
        public void SetSpeedBoostBonus(int bonusPerEat) => _speedBoostBonusPerEat = Mathf.Max(0, bonusPerEat);

        /// <summary>
        /// Enable or disable Frenzy Mode, which doubles all scored points.
        /// Activated by GameSession when the snake reaches MAX_SPEED in Classic Endless.
        /// </summary>
        public void SetFrenzyMode(bool active)
        {
            if (_frenzyMode == active) return;
            _frenzyMode = active;
            OnFrenzyChanged?.Invoke(active);
        }
    }
}
