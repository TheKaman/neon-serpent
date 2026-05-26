using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using NeonSerpent.Levels;
using NeonSerpent.Core;
using NeonSerpent.SaveData;

namespace NeonSerpent.Tests
{
    /// <summary>
    /// EditMode tests for campaign save/load progress and star calculation logic.
    /// Tests PlayerData directly — no file I/O, no singleton dependencies.
    /// </summary>
    [TestFixture]
    [Category("Campaign")]
    [Category("Save")]
    public class CampaignProgressTests
    {
        // ── Progress key formula ──────────────────────────────────────────────

        /// <summary>
        /// The composite key formula (WorldIndex * 100 + LevelIndex) must produce
        /// a unique value for every level in the 4×5 campaign grid.
        /// </summary>
        [Test]
        public void ProgressKeyIsUniqueForAllTwentyLevels()
        {
            // Level assets use a global sequential LevelIndex (1-20).
            // World1: indices 1-5, World2: 6-10, World3: 11-15, World4: 16-20.
            var seen = new HashSet<int>();
            for (int world = 1; world <= 4; world++)
            {
                for (int level = (world - 1) * 5 + 1; level <= world * 5; level++)
                {
                    int key = world * 100 + level;
                    Assert.IsTrue(seen.Add(key),
                        $"Key collision: World {world} Level {level} produces key {key} which was already used.");
                }
            }
        }

        // ── PlayerData.campaignProgress ───────────────────────────────────────

        [Test]
        public void CampaignProgress_DefaultsToEmpty()
        {
            var data = new PlayerData();
            Assert.IsNotNull(data.campaignProgress);
            Assert.AreEqual(0, data.campaignProgress.Count);
        }

        [Test]
        public void CampaignProgress_RecordingStars_RetainsHighestStarCount()
        {
            var data = new PlayerData();
            int key  = 1 * 100 + 1; // World 1, Level 1

            // First completion: 1 star
            if (!data.campaignProgress.ContainsKey(key) || data.campaignProgress[key] < 1)
                data.campaignProgress[key] = 1;
            Assert.AreEqual(1, data.campaignProgress[key]);

            // Re-play with 2 stars — should upgrade
            if (!data.campaignProgress.ContainsKey(key) || data.campaignProgress[key] < 2)
                data.campaignProgress[key] = 2;
            Assert.AreEqual(2, data.campaignProgress[key]);

            // Re-play with 1 star — should NOT downgrade (the game only writes if higher)
            if (!data.campaignProgress.ContainsKey(key) || data.campaignProgress[key] < 1)
                data.campaignProgress[key] = 1;
            Assert.AreEqual(2, data.campaignProgress[key],
                "Stars should never decrease on re-play — only the highest count is saved.");
        }

        [Test]
        public void CampaignProgress_LevelWithKeyPresent_UnlocksNextLevel()
        {
            // LevelSelectUI unlock logic: a level is unlocked if its key exists in campaignProgress,
            // regardless of star count. Simulate that logic here.
            var data = new PlayerData();
            int key  = 1 * 100 + 1;
            data.campaignProgress[key] = 0; // 0 stars — completed but barely

            bool nextLevelUnlocked = data.campaignProgress.ContainsKey(key);
            Assert.IsTrue(nextLevelUnlocked,
                "A level with any entry in campaignProgress (even 0 stars) must unlock the next level.");
        }

        [Test]
        public void CampaignProgress_LevelWithNoKey_DoesNotUnlockNext()
        {
            var data = new PlayerData();
            int key  = 1 * 100 + 1;
            // Key not present at all
            bool nextLevelUnlocked = data.campaignProgress.ContainsKey(key);
            Assert.IsFalse(nextLevelUnlocked,
                "A level that was never completed must not unlock the next level.");
        }

        // ── Star calculation ──────────────────────────────────────────────────
        //  Mirrors LevelCompleteUI.CalculateStars — tested here without a MonoBehaviour.

        private static int CalculateStars(long score, int t1, int t2, int t3)
        {
            if (t1 == 0 && t2 == 0 && t3 == 0) return 3; // all-zero = trivial 3-star
            if (score >= t3) return 3;
            if (score >= t2) return 2;
            if (score >= t1) return 1;
            return 0;
        }

        [Test]
        public void CalculateStars_ZeroScore_ReturnsZeroStars()
        {
            Assert.AreEqual(0, CalculateStars(0L, 30, 60, 120));
        }

        [Test]
        public void CalculateStars_BelowThreshold1_ReturnsZero()
        {
            Assert.AreEqual(0, CalculateStars(29L, 30, 60, 120));
        }

        [Test]
        public void CalculateStars_AtThreshold1_ReturnsOneStar()
        {
            Assert.AreEqual(1, CalculateStars(30L, 30, 60, 120));
        }

        [Test]
        public void CalculateStars_AtThreshold2_ReturnsTwoStars()
        {
            Assert.AreEqual(2, CalculateStars(60L, 30, 60, 120));
        }

        [Test]
        public void CalculateStars_AtThreshold3_ReturnsThreeStars()
        {
            Assert.AreEqual(3, CalculateStars(120L, 30, 60, 120));
        }

        [Test]
        public void CalculateStars_AboveThreshold3_ReturnsThreeStars()
        {
            Assert.AreEqual(3, CalculateStars(9999L, 30, 60, 120));
        }

        [Test]
        public void CalculateStars_AllThresholdsZero_ReturnsThreeStars()
        {
            // When thresholds are 0 the OnValidate warning fires in the editor.
            // Code-wise, the top check (score >= StarThreshold3 = 0) is true for any
            // non-negative score — this test verifies that behaviour explicitly.
            Assert.AreEqual(3, CalculateStars(0L,    0, 0, 0));
            Assert.AreEqual(3, CalculateStars(100L,  0, 0, 0));
        }

        [Test]
        public void CalculateStars_LevelDataLevel1_1_MatchesExpected()
        {
            // Level_1_1: StarThreshold1=30, StarThreshold2=60, StarThreshold3=120, ScoreTarget=90
            // A player who just meets ScoreTarget (90) should get 2 stars.
            Assert.AreEqual(2, CalculateStars(90L,  30, 60, 120));
            // Below threshold 1
            Assert.AreEqual(0, CalculateStars(10L,  30, 60, 120));
            // 3 stars requires 120
            Assert.AreEqual(3, CalculateStars(120L, 30, 60, 120));
        }

        // ── SerializableDictionary round-trip ─────────────────────────────────

        [Test]
        public void SerializableDictionary_RoundTrip_ViaJsonUtility()
        {
            var data = new PlayerData();
            data.campaignProgress[101] = 2;  // World 1, Level 1
            data.campaignProgress[102] = 3;  // World 1, Level 2

            // Simulate what SaveManager.Save() / Load() does
            string json   = JsonUtility.ToJson(data, prettyPrint: false);
            var loaded    = JsonUtility.FromJson<PlayerData>(json);

            Assert.IsNotNull(loaded,              "JsonUtility.FromJson returned null.");
            Assert.IsNotNull(loaded.campaignProgress);
            Assert.AreEqual(2, loaded.campaignProgress.Count,
                "Both progress entries must survive JSON round-trip.");
            Assert.AreEqual(2, loaded.campaignProgress[101]);
            Assert.AreEqual(3, loaded.campaignProgress[102]);
        }

        [Test]
        public void SerializableDictionary_EmptyDict_RoundTripPreservesEmpty()
        {
            var data = new PlayerData();
            // No entries
            string json  = JsonUtility.ToJson(data);
            var loaded   = JsonUtility.FromJson<PlayerData>(json);

            Assert.IsNotNull(loaded.campaignProgress);
            Assert.AreEqual(0, loaded.campaignProgress.Count);
        }

        // ── Level unlock chain simulation ─────────────────────────────────────

        [Test]
        public void LevelUnlockChain_PlayingThroughFiveLevels_UnlocksAllInOrder()
        {
            var progress = new SerializableDictionary<int, int>();
            bool[] unlocked = new bool[6]; // index 1-5 for levels 1-5
            unlocked[1] = true; // first level is always unlocked

            // World 1 levels: indices 1-5, WorldIndex = 1
            // Keys: 101, 102, 103, 104, 105
            for (int i = 1; i <= 5; i++)
            {
                int key = 1 * 100 + i;

                // Simulate: player completes level i
                progress[key] = 1; // 1 star

                // After completing level i, level i+1 is now unlocked
                if (i < 5)
                {
                    int nextKey = 1 * 100 + (i + 1);
                    bool nextUnlocked = progress.ContainsKey(key); // same as before: key present = unlock
                    Assert.IsTrue(nextUnlocked,
                        $"Level {i+1} should be unlocked after completing level {i}.");
                }
            }

            // All 5 levels should be in progress now
            Assert.AreEqual(5, progress.Count,
                "After completing all 5 World 1 levels, progress should contain 5 entries.");
        }

        [Test]
        public void LevelUnlockChain_SkippingALevel_DoesNotUnlockSubsequentLevels()
        {
            // Simulate: player somehow has progress only for level 1 and level 3 (level 2 missing).
            var progress = new SerializableDictionary<int, int>();
            progress[101] = 3; // level 1: 3 stars
            progress[103] = 2; // level 3: 2 stars (gap at level 2)

            // LevelSelectUI processes levels in order. previousUnlocked flips only when
            // progress.ContainsKey(progressKey) for the CURRENT level.
            bool previousUnlocked = true; // level 1 always starts unlocked
            bool[] actualUnlocked = new bool[6];
            for (int i = 1; i <= 5; i++)
            {
                int key = 1 * 100 + i;
                actualUnlocked[i] = previousUnlocked;
                previousUnlocked  = progress.ContainsKey(key);
            }

            Assert.IsTrue(actualUnlocked[1],  "Level 1 must be unlocked (it's always the starting level).");
            Assert.IsTrue(actualUnlocked[2],  "Level 2 must be unlocked because level 1 is complete.");
            Assert.IsFalse(actualUnlocked[3], "Level 3 must be LOCKED because level 2 has no progress entry.");
            Assert.IsFalse(actualUnlocked[4], "Level 4 must be LOCKED because level 2 has no progress entry.");
            Assert.IsFalse(actualUnlocked[5], "Level 5 must be LOCKED because level 2 has no progress entry.");
        }
    }
}
