using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using NeonSerpent.Levels;
using NeonSerpent.Core;
using NeonSerpent.PowerUps;
using NeonSerpent.SaveData;

namespace NeonSerpent.Tests
{
    /// <summary>
    /// EditMode tests for LevelData asset integrity and campaign data validation.
    /// These run without entering Play mode, so they are fast and CI-friendly.
    /// Run via: Window > General > Test Runner > EditMode tab.
    /// </summary>
    [TestFixture]
    [Category("Campaign")]
    public class CampaignLevelDataTests
    {
        private LevelData[] _allLevels;

        [OneTimeSetUp]
        public void LoadLevels()
        {
            // Resources.LoadAll searches recursively through subfolders (World1, World2, etc.)
            _allLevels = Resources.LoadAll<LevelData>("Levels");
        }

        // ── Asset presence ────────────────────────────────────────────────────

        /// <summary>Verify that 20 LevelData assets are present in Resources/Levels/.</summary>
        [Test]
        public void AllTwentyLevelsExist()
        {
            Assert.AreEqual(20, _allLevels.Length,
                "Expected 20 LevelData assets under Resources/Levels/ (including subfolders). " +
                "Run NeonSerpent > Generate Campaign Levels if assets are missing.");
        }

        /// <summary>Every level must have a non-empty LevelName.</summary>
        [Test]
        public void AllLevelsHaveNonEmptyLevelName()
        {
            foreach (var level in _allLevels)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(level.LevelName),
                    $"LevelData '{level.name}' (W{level.WorldIndex} L{level.LevelIndex}) has an empty LevelName.");
            }
        }

        /// <summary>Every level must have a non-empty WorldName.</summary>
        [Test]
        public void AllLevelsHaveNonEmptyWorldName()
        {
            foreach (var level in _allLevels)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(level.WorldName),
                    $"LevelData '{level.name}' has an empty WorldName.");
            }
        }

        /// <summary>Each level must be assigned GameMode.Campaign.</summary>
        [Test]
        public void AllLevelsHaveCampaignMode()
        {
            foreach (var level in _allLevels)
            {
                Assert.AreEqual(GameMode.Campaign, level.Mode,
                    $"LevelData '{level.name}' Mode is {level.Mode}, expected Campaign.");
            }
        }

        // ── World / level index integrity ─────────────────────────────────────

        /// <summary>LevelIndex values must be positive and unique across all levels.</summary>
        [Test]
        public void AllLevelIndexesAreUniqueAndPositive()
        {
            var seen = new HashSet<int>();
            foreach (var level in _allLevels)
            {
                Assert.Greater(level.LevelIndex, 0,
                    $"LevelData '{level.name}' has LevelIndex <= 0.");
                Assert.IsTrue(seen.Add(level.LevelIndex),
                    $"LevelData '{level.name}' has duplicate LevelIndex {level.LevelIndex}.");
            }
        }

        /// <summary>WorldIndex must be in the valid range 1–4.</summary>
        [Test]
        public void AllWorldIndexesAreInRange()
        {
            foreach (var level in _allLevels)
            {
                Assert.IsTrue(level.WorldIndex >= 1 && level.WorldIndex <= 4,
                    $"LevelData '{level.name}' WorldIndex {level.WorldIndex} is outside 1–4.");
            }
        }

        /// <summary>Each world must contain exactly 5 levels.</summary>
        [Test]
        public void EachWorldContainsFiveLevels()
        {
            var countPerWorld = new Dictionary<int, int>();
            foreach (var level in _allLevels)
            {
                if (!countPerWorld.ContainsKey(level.WorldIndex))
                    countPerWorld[level.WorldIndex] = 0;
                countPerWorld[level.WorldIndex]++;
            }

            for (int w = 1; w <= 4; w++)
            {
                int count = countPerWorld.TryGetValue(w, out int c) ? c : 0;
                Assert.AreEqual(5, count,
                    $"World {w} has {count} levels, expected 5.");
            }
        }

        // ── Win condition integrity ───────────────────────────────────────────

        /// <summary>Campaign levels must have a positive ScoreTarget (otherwise they never complete).</summary>
        [Test]
        public void AllCampaignLevelsHavePositiveScoreTarget()
        {
            foreach (var level in _allLevels)
            {
                Assert.Greater(level.ScoreTarget, 0,
                    $"LevelData '{level.name}' ScoreTarget is 0 — the level can never complete by score.");
            }
        }

        /// <summary>
        /// Star thresholds must be ascending: StarThreshold1 &lt; StarThreshold2 &lt; StarThreshold3.
        /// A level where thresholds are all zero means every player gets 3 stars trivially.
        /// </summary>
        [Test]
        public void StarThresholdsAreAscending()
        {
            foreach (var level in _allLevels)
            {
                // Warn about all-zero thresholds (every score earns 3 stars)
                if (level.StarThreshold1 == 0 && level.StarThreshold2 == 0 && level.StarThreshold3 == 0)
                {
                    Debug.LogWarning($"[Test] '{level.name}': all star thresholds are zero — every run earns 3 stars.");
                    continue;
                }

                Assert.IsTrue(level.StarThreshold1 < level.StarThreshold2,
                    $"'{level.name}': StarThreshold1 ({level.StarThreshold1}) >= StarThreshold2 ({level.StarThreshold2}).");
                Assert.IsTrue(level.StarThreshold2 < level.StarThreshold3,
                    $"'{level.name}': StarThreshold2 ({level.StarThreshold2}) >= StarThreshold3 ({level.StarThreshold3}).");
            }
        }

        /// <summary>
        /// StarThreshold1 must be reachable given the level's ScoreTarget.
        /// At minimum the player needs to reach ScoreTarget — if StarThreshold1 is above ScoreTarget
        /// then it is unreachable in a single session.
        /// </summary>
        [Test]
        public void StarThreshold1IsReachableWithinScoreTarget()
        {
            foreach (var level in _allLevels)
            {
                if (level.StarThreshold1 == 0) continue;
                Assert.LessOrEqual(level.StarThreshold1, level.ScoreTarget * 2,
                    $"'{level.name}': StarThreshold1 ({level.StarThreshold1}) appears unreachably high " +
                    $"vs ScoreTarget ({level.ScoreTarget}).");
            }
        }

        // ── Grid integrity ────────────────────────────────────────────────────

        /// <summary>Grid dimensions must be at least 8×8 (minimum viable play area).</summary>
        [Test]
        public void AllLevelsHaveMinimumGridSize()
        {
            foreach (var level in _allLevels)
            {
                Assert.GreaterOrEqual(level.GridWidth, 8,
                    $"'{level.name}': GridWidth {level.GridWidth} is below minimum 8.");
                Assert.GreaterOrEqual(level.GridHeight, 8,
                    $"'{level.name}': GridHeight {level.GridHeight} is below minimum 8.");
            }
        }

        /// <summary>Wall positions must be within the declared grid bounds.</summary>
        [Test]
        public void AllWallPositionsAreWithinGridBounds()
        {
            foreach (var level in _allLevels)
            {
                if (level.WallPositions == null) continue;
                foreach (var wall in level.WallPositions)
                {
                    Assert.IsTrue(wall.x >= 0 && wall.x < level.GridWidth,
                        $"'{level.name}': wall X={wall.x} is outside grid width {level.GridWidth}.");
                    Assert.IsTrue(wall.y >= 0 && wall.y < level.GridHeight,
                        $"'{level.name}': wall Y={wall.y} is outside grid height {level.GridHeight}.");
                }
            }
        }

        // ── Progression chain ─────────────────────────────────────────────────

        /// <summary>
        /// Only the last level (Level_4_5, LevelIndex 20) should have NextLevel = null.
        /// All other levels must chain to a valid next level.
        /// </summary>
        [Test]
        public void AllNonFinalLevelsHaveNextLevelSet()
        {
            // Find the level with the highest LevelIndex — that is the final level.
            int maxIndex = 0;
            foreach (var level in _allLevels)
                if (level.LevelIndex > maxIndex) maxIndex = level.LevelIndex;

            foreach (var level in _allLevels)
            {
                if (level.LevelIndex == maxIndex)
                {
                    // Final level: NextLevel should be null
                    Assert.IsNull(level.NextLevel,
                        $"Final level '{level.name}' (LevelIndex {maxIndex}) has NextLevel set — " +
                        "it should be null to signal end of campaign.");
                }
                else
                {
                    Assert.IsNotNull(level.NextLevel,
                        $"Non-final level '{level.name}' (LevelIndex {level.LevelIndex}) has no NextLevel. " +
                        "Chain will break at this level.");
                }
            }
        }

        /// <summary>NextLevel references must not form a cycle (would cause an infinite progression loop).</summary>
        [Test]
        public void NextLevelChainHasNoCycles()
        {
            // Build a set of all known LevelData references
            var allLevelSet = new HashSet<LevelData>(_allLevels);

            foreach (var startLevel in _allLevels)
            {
                var visited = new HashSet<LevelData>();
                var current = startLevel.NextLevel;

                while (current != null)
                {
                    if (!visited.Add(current))
                    {
                        Assert.Fail($"Cycle detected in NextLevel chain starting from '{startLevel.name}'. " +
                            $"'{current.name}' appears twice.");
                        break;
                    }
                    current = current.NextLevel;
                }
            }
        }

        /// <summary>NextLevel references must point to a level that exists in Resources/Levels/.</summary>
        [Test]
        public void NextLevelReferencesAreLoadable()
        {
            var allLevelSet = new HashSet<LevelData>(_allLevels);
            foreach (var level in _allLevels)
            {
                if (level.NextLevel == null) continue;
                Assert.IsTrue(allLevelSet.Contains(level.NextLevel),
                    $"'{level.name}'.NextLevel ('{level.NextLevel.name}') is not in the " +
                    "Resources/Levels/ pool. It may have been deleted or moved.");
            }
        }

        // ── Speed settings ────────────────────────────────────────────────────

        /// <summary>InitialSpeed must be between 1 and Constants.MAX_SPEED (20).</summary>
        [Test]
        public void AllLevelsHaveValidInitialSpeed()
        {
            foreach (var level in _allLevels)
            {
                Assert.IsTrue(level.InitialSpeed >= 1f && level.InitialSpeed <= 20f,
                    $"'{level.name}': InitialSpeed {level.InitialSpeed} is outside [1, 20].");
            }
        }

        /// <summary>SpeedIncrement must be non-negative.</summary>
        [Test]
        public void AllLevelsHaveNonNegativeSpeedIncrement()
        {
            foreach (var level in _allLevels)
            {
                Assert.GreaterOrEqual(level.SpeedIncrement, 0f,
                    $"'{level.name}': SpeedIncrement {level.SpeedIncrement} is negative.");
            }
        }

        /// <summary>Time limit must be non-negative (0 = no limit).</summary>
        [Test]
        public void AllLevelsHaveNonNegativeTimeLimit()
        {
            foreach (var level in _allLevels)
            {
                Assert.GreaterOrEqual(level.TimeLimit, 0f,
                    $"'{level.name}': TimeLimit {level.TimeLimit} is negative.");
            }
        }
    }
}
