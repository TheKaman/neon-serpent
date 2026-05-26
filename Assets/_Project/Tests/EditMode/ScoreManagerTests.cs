using NUnit.Framework;
using UnityEngine;
using NeonSerpent.Scoring;
using NeonSerpent.Utilities;

namespace NeonSerpent.Tests
{
    /// <summary>
    /// EditMode unit tests for ScoreManager — score accumulation, multipliers,
    /// combo bonuses, poison effect, frenzy mode, and reset behaviour.
    /// </summary>
    [TestFixture]
    [Category("Campaign")]
    [Category("Score")]
    public class ScoreManagerTests
    {
        private GameObject  _go;
        private ScoreManager _score;

        [SetUp]
        public void Setup()
        {
            _go    = new GameObject("ScoreTest");
            _score = _go.AddComponent<ScoreManager>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_go);
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void ResetScore_SetsCurrentScoreToZero()
        {
            _score.AddScore(500);
            _score.ResetScore();
            Assert.AreEqual(0L, _score.CurrentScore);
        }

        [Test]
        public void ResetScore_ResetsMultiplierToOne()
        {
            _score.SetMultiplier(3);
            _score.ResetScore();
            Assert.AreEqual(1, _score.Multiplier);
        }

        [Test]
        public void ResetScore_ResetsComboCountToZero()
        {
            for (int i = 0; i < 10; i++) _score.AddScore(10);
            _score.ResetScore();
            Assert.AreEqual(0, _score.ComboCount);
        }

        [Test]
        public void ResetScore_FiresOnScoreChangedWithZero()
        {
            long received = -1L;
            _score.OnScoreChanged += s => received = s;
            _score.AddScore(100);
            _score.ResetScore();
            Assert.AreEqual(0L, received, "OnScoreChanged must fire with 0 on ResetScore.");
        }

        [Test]
        public void ResetScore_TurnsFrenzyModeOff()
        {
            _score.SetFrenzyMode(true);
            _score.ResetScore();
            Assert.IsFalse(_score.FrenzyMode, "FrenzyMode must be cleared by ResetScore.");
        }

        // ── AddScore ─────────────────────────────────────────────────────────

        [Test]
        public void AddScore_BasicPoints_AccumulatesCorrectly()
        {
            _score.AddScore(10);
            _score.AddScore(10);
            Assert.AreEqual(20L, _score.CurrentScore);
        }

        [Test]
        public void AddScore_WithMultiplier_ScalesCorrectly()
        {
            _score.SetMultiplier(3);
            _score.AddScore(10);
            Assert.AreEqual(30L, _score.CurrentScore);
        }

        [Test]
        public void AddScore_FiresOnScoreChangedWithNewTotal()
        {
            long received = 0L;
            _score.OnScoreChanged += s => received = s;
            _score.AddScore(50);
            Assert.AreEqual(50L, received);
        }

        [Test]
        public void AddScore_FiresOnScoreAddedWithPointsJustAdded()
        {
            long added = 0L;
            _score.OnScoreAdded += pts => added = pts;
            _score.AddScore(50);
            Assert.AreEqual(50L, added);
        }

        // ── Combo ─────────────────────────────────────────────────────────────

        [Test]
        public void AddScore_AtComboThreshold_FiresOnComboAchieved()
        {
            int comboFired = 0;
            _score.OnComboAchieved += _ => comboFired++;

            // COMBO_THRESHOLD is 5 — eat 5 foods in a row to trigger one combo event.
            for (int i = 0; i < Constants.COMBO_THRESHOLD; i++)
                _score.AddScore(10);

            Assert.AreEqual(1, comboFired,
                $"Exactly one OnComboAchieved event expected after {Constants.COMBO_THRESHOLD} consecutive eats.");
        }

        [Test]
        public void AddScore_AtDoubleComboThreshold_FiresTwice()
        {
            int comboFired = 0;
            _score.OnComboAchieved += _ => comboFired++;

            for (int i = 0; i < Constants.COMBO_THRESHOLD * 2; i++)
                _score.AddScore(10);

            Assert.AreEqual(2, comboFired);
        }

        // ── Poison ────────────────────────────────────────────────────────────

        [Test]
        public void PoisonEffect_SetPoisonEatsRemaining_BlocksScoreForThatManyEats()
        {
            _score.SetPoisonEatsRemaining(2);
            _score.AddScore(10); // poisoned — no score
            _score.AddScore(10); // poisoned — no score
            Assert.AreEqual(0L, _score.CurrentScore,
                "Score must remain 0 while poison eats are remaining.");
        }

        [Test]
        public void PoisonEffect_ScoreResumesAfterPoisonExpires()
        {
            _score.SetPoisonEatsRemaining(1);
            _score.AddScore(10); // poisoned
            _score.AddScore(10); // clean — should score
            Assert.AreEqual(10L, _score.CurrentScore,
                "Score should resume normally once poison eats are exhausted.");
        }

        [Test]
        public void PoisonEffect_ResetsComboOnPoisonedEat()
        {
            // Eat 4 non-poisoned items to build combo, then hit poison — combo resets
            for (int i = 0; i < 4; i++) _score.AddScore(10);
            _score.SetPoisonEatsRemaining(1);
            _score.AddScore(10); // poisoned — combo reset

            // Eat 4 more clean items — combo should restart from 0 (was reset)
            // so no combo event at eat 5 from now (it would have to reach COMBO_THRESHOLD again)
            int comboFired = 0;
            _score.OnComboAchieved += _ => comboFired++;
            for (int i = 0; i < 4; i++) _score.AddScore(10);
            // Total after reset: 4 clean eats — combo at 4, still below threshold (5)
            Assert.AreEqual(0, comboFired,
                "Combo should have reset after a poisoned eat, so 4 subsequent eats should not fire a combo event.");
        }

        // ── Frenzy Mode ───────────────────────────────────────────────────────

        [Test]
        public void FrenzyMode_WhenActive_DoublesScore()
        {
            _score.SetFrenzyMode(true);
            _score.AddScore(10);
            Assert.AreEqual(20L, _score.CurrentScore,
                "Frenzy mode must double scored points.");
        }

        [Test]
        public void FrenzyMode_Deactivate_FiresOnFrenzyChangedWithFalse()
        {
            bool? received = null;
            _score.OnFrenzyChanged += v => received = v;
            _score.SetFrenzyMode(true);
            _score.SetFrenzyMode(false);
            Assert.IsFalse(received, "OnFrenzyChanged must fire with false when frenzy deactivates.");
        }

        [Test]
        public void FrenzyMode_CallingSetFrenzyModeTwiceWithSameValue_DoesNotFireEvent()
        {
            int fired = 0;
            _score.OnFrenzyChanged += _ => fired++;
            _score.SetFrenzyMode(true);  // fires once
            _score.SetFrenzyMode(true);  // should be a no-op
            Assert.AreEqual(1, fired, "OnFrenzyChanged should not fire when value is unchanged.");
        }

        // ── SpeedBoost bonus ──────────────────────────────────────────────────

        [Test]
        public void SpeedBoostBonus_IsAddedToEachEat()
        {
            _score.SetSpeedBoostBonus(5);
            _score.AddScore(10);
            // 10 (base) × 1 (multiplier) + 5 (speed bonus) = 15
            Assert.AreEqual(15L, _score.CurrentScore);
        }

        [Test]
        public void SpeedBoostBonus_SetToZero_NoEffect()
        {
            _score.SetSpeedBoostBonus(0);
            _score.AddScore(10);
            Assert.AreEqual(10L, _score.CurrentScore);
        }

        // ── Level completion condition via ScoreTarget ────────────────────────

        [Test]
        public void ScoreTarget_WhenReached_OnScoreChangedFiresWithTargetValue()
        {
            // Simulate a LevelManager subscribing to OnScoreChanged and checking the target.
            bool levelCompleteTriggered = false;
            int  scoreTarget            = 90; // Level_1_1's ScoreTarget

            _score.OnScoreChanged += score =>
            {
                if (score >= scoreTarget) levelCompleteTriggered = true;
            };

            // Add score until we hit the target
            for (int i = 0; i < 9; i++)
                _score.AddScore(Constants.SCORE_NORMAL_FOOD); // 10 per eat × 9 = 90

            Assert.IsTrue(levelCompleteTriggered,
                "Level complete condition must trigger when score reaches the ScoreTarget.");
        }
    }
}
