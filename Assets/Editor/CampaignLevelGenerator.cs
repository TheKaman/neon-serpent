// ============================================================
// CampaignLevelGenerator.cs
// Generates all 20 Campaign LevelData ScriptableObject assets
// (4 worlds × 5 levels) under:
//   Assets/Resources/Levels/World{w}/Level_{w}_{l}.asset
// Saved to Resources/ so LevelSelectUI can load them at runtime via Resources.LoadAll.
//
// HOW TO USE:
//   NeonSerpent → Generate Campaign Levels
//   NeonSerpent → Clear Campaign Levels  (deletes and regenerates)
// ============================================================

using UnityEngine;
using UnityEditor;
using NeonSerpent.Levels;
using NeonSerpent.PowerUps;
using NeonSerpent.Core;

namespace NeonSerpent.Editor
{
    /// <summary>
    /// Editor utility that generates and wires all 20 campaign LevelData
    /// ScriptableObject assets from a single menu click.
    /// </summary>
    public static class CampaignLevelGenerator
    {
        // ─────────────────────────────────────────────────────────────────────
        // CONSTANTS
        // ─────────────────────────────────────────────────────────────────────

        private const string ROOT_FOLDER   = "Assets/Resources";
        private const string LEVELS_FOLDER = "Assets/Resources/Levels";

        // ─────────────────────────────────────────────────────────────────────
        // MENU ITEMS
        // ─────────────────────────────────────────────────────────────────────

        [MenuItem("NeonSerpent/Generate Campaign Levels")]
        public static void GenerateLevels()
        {
            EnsureFolderHierarchy();
            var allLevels = CreateAllLevelAssets();
            WireNextLevelReferences(allLevels);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CampaignLevelGenerator] Done — generated {allLevels.Length} LevelData assets.");
        }

        [MenuItem("NeonSerpent/Clear Campaign Levels")]
        public static void ClearAndRegenerate()
        {
            if (AssetDatabase.IsValidFolder(LEVELS_FOLDER))
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Clear Campaign Levels",
                    $"This will delete all assets under:\n{LEVELS_FOLDER}\nand regenerate them fresh. Continue?",
                    "Yes, clear and regenerate", "Cancel");

                if (!confirmed) return;

                AssetDatabase.DeleteAsset(LEVELS_FOLDER);
                AssetDatabase.Refresh();
                Debug.Log("[CampaignLevelGenerator] Cleared existing campaign levels.");
            }

            GenerateLevels();
        }

        // ─────────────────────────────────────────────────────────────────────
        // FOLDER SETUP
        // ─────────────────────────────────────────────────────────────────────

        private static void EnsureFolderHierarchy()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(ROOT_FOLDER);
            EnsureFolder(LEVELS_FOLDER);

            for (int w = 1; w <= 4; w++)
                EnsureFolder($"{LEVELS_FOLDER}/World{w}");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            int lastSlash = path.LastIndexOf('/');
            string parent = path.Substring(0, lastSlash);
            string child  = path.Substring(lastSlash + 1);
            AssetDatabase.CreateFolder(parent, child);
        }

        // ─────────────────────────────────────────────────────────────────────
        // ASSET CREATION — iterates all 20 combinations
        // Returns a 2D array: [world 0-3][level 0-4]
        // ─────────────────────────────────────────────────────────────────────

        private static LevelData[,] CreateAllLevelAssets()
        {
            var grid = new LevelData[4, 5];

            for (int w = 1; w <= 4; w++)
            {
                for (int l = 1; l <= 5; l++)
                {
                    string assetPath = $"{LEVELS_FOLDER}/World{w}/Level_{w}_{l}.asset";
                    var data = ScriptableObject.CreateInstance<LevelData>();
                    PopulateLevel(data, w, l);
                    AssetDatabase.CreateAsset(data, assetPath);
                    Debug.Log($"[CampaignLevelGenerator] Generated Level_{w}_{l}: {data.LevelName}");
                    grid[w - 1, l - 1] = data;
                }
            }

            return grid;
        }

        // ─────────────────────────────────────────────────────────────────────
        // POPULATE — dispatches to world-specific builders
        // ─────────────────────────────────────────────────────────────────────

        private static void PopulateLevel(LevelData d, int world, int level)
        {
            d.Mode       = GameMode.Campaign;
            d.WorldIndex = world;
            d.LevelIndex = (world - 1) * 5 + level; // 1–20

            switch (world)
            {
                case 1: PopulateWorld1(d, level); break;
                case 2: PopulateWorld2(d, level); break;
                case 3: PopulateWorld3(d, level); break;
                case 4: PopulateWorld4(d, level); break;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // WORLD 1 — "Neon Streets"  (Tutorial / Easy, 16×16)
        // ─────────────────────────────────────────────────────────────────────

        private static void PopulateWorld1(LevelData d, int level)
        {
            d.WorldName  = "Neon Streets";
            d.GridWidth  = 16;
            d.GridHeight = 16;

            // Score targets and thresholds scale with level within the world.
            // Baseline: 50/100/200 star thresholds, 150 score target.
            // Level multipliers: 0.6 / 0.75 / 1.0 / 1.25 / 1.5
            float[] mult = { 0.6f, 0.75f, 1.0f, 1.25f, 1.5f };
            float m = mult[level - 1];

            d.StarThreshold1 = Mathf.RoundToInt(50  * m);
            d.StarThreshold2 = Mathf.RoundToInt(100 * m);
            d.StarThreshold3 = Mathf.RoundToInt(200 * m);
            d.ScoreTarget    = Mathf.RoundToInt(150 * m);

            switch (level)
            {
                case 1:
                    d.LevelName      = "First Steps";
                    d.InitialSpeed   = 5f;
                    d.SpeedIncrement = 0.1f;
                    d.TimeLimit      = 0f;
                    d.WallPositions  = System.Array.Empty<Vector2Int>();
                    d.AllowedPowerUps = new[] { PowerUpType.SpeedBoost };
                    break;

                case 2:
                    d.LevelName      = "Getting Started";
                    d.InitialSpeed   = 5.5f;
                    d.SpeedIncrement = 0.15f;
                    d.TimeLimit      = 0f;
                    d.WallPositions  = System.Array.Empty<Vector2Int>();
                    d.AllowedPowerUps = new[] { PowerUpType.SpeedBoost, PowerUpType.Shield };
                    break;

                case 3:
                    // Simple border notches — four inward protrusions on each wall edge
                    d.LevelName      = "Street Corners";
                    d.InitialSpeed   = 6f;
                    d.SpeedIncrement = 0.2f;
                    d.TimeLimit      = 90f;
                    d.WallPositions  = new[]
                    {
                        // Top edge notches (y=14)
                        new Vector2Int(7,  14), new Vector2Int(8,  14),
                        // Bottom edge notches (y=1)
                        new Vector2Int(7,  1),  new Vector2Int(8,  1),
                        // Left edge notches (x=1)
                        new Vector2Int(1,  7),  new Vector2Int(1,  8),
                        // Right edge notches (x=14)
                        new Vector2Int(14, 7),  new Vector2Int(14, 8),
                    };
                    d.AllowedPowerUps = new[]
                    {
                        PowerUpType.SpeedBoost,
                        PowerUpType.Shield,
                        PowerUpType.ScoreMultiplier,
                    };
                    break;

                case 4:
                    // Inward notches + short central pillars
                    d.LevelName      = "Neon Alleys";
                    d.InitialSpeed   = 6.5f;
                    d.SpeedIncrement = 0.25f;
                    d.TimeLimit      = 75f;
                    d.WallPositions  = new[]
                    {
                        // Top/bottom edge notches
                        new Vector2Int(5,  14), new Vector2Int(6,  14),
                        new Vector2Int(9,  14), new Vector2Int(10, 14),
                        new Vector2Int(5,  1),  new Vector2Int(6,  1),
                        new Vector2Int(9,  1),  new Vector2Int(10, 1),
                        // Left/right edge notches
                        new Vector2Int(1,  5),  new Vector2Int(1,  6),
                        new Vector2Int(1,  9),  new Vector2Int(1,  10),
                        new Vector2Int(14, 5),  new Vector2Int(14, 6),
                        new Vector2Int(14, 9),  new Vector2Int(14, 10),
                        // Small central pillars
                        new Vector2Int(5,  5),
                        new Vector2Int(10, 5),
                        new Vector2Int(5,  10),
                        new Vector2Int(10, 10),
                    };
                    d.AllowedPowerUps = new[]
                    {
                        PowerUpType.SpeedBoost,
                        PowerUpType.Shield,
                        PowerUpType.ScoreMultiplier,
                        PowerUpType.GhostMode,
                    };
                    break;

                case 5:
                    // Boss: wider notches + 2×1 pillars at quad-points
                    d.LevelName      = "Street King";
                    d.InitialSpeed   = 7f;
                    d.SpeedIncrement = 0.3f;
                    d.TimeLimit      = 60f;
                    d.WallPositions  = new[]
                    {
                        // Top wall notches
                        new Vector2Int(4,  14), new Vector2Int(5,  14),
                        new Vector2Int(10, 14), new Vector2Int(11, 14),
                        // Bottom wall notches
                        new Vector2Int(4,  1),  new Vector2Int(5,  1),
                        new Vector2Int(10, 1),  new Vector2Int(11, 1),
                        // Left wall notches
                        new Vector2Int(1,  4),  new Vector2Int(1,  5),
                        new Vector2Int(1,  10), new Vector2Int(1,  11),
                        // Right wall notches
                        new Vector2Int(14, 4),  new Vector2Int(14, 5),
                        new Vector2Int(14, 10), new Vector2Int(14, 11),
                        // 2×1 pillars at quadrant centres
                        new Vector2Int(4,  4),  new Vector2Int(5,  4),
                        new Vector2Int(10, 4),  new Vector2Int(11, 4),
                        new Vector2Int(4,  11), new Vector2Int(5,  11),
                        new Vector2Int(10, 11), new Vector2Int(11, 11),
                    };
                    d.AllowedPowerUps = new[]
                    {
                        PowerUpType.SpeedBoost,
                        PowerUpType.Shield,
                        PowerUpType.ScoreMultiplier,
                        PowerUpType.GhostMode,
                        PowerUpType.ShrinkPill,
                    };
                    break;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // WORLD 2 — "Circuit Board"  (Medium, 18×18)
        // Horizontal/vertical barriers creating corridors.
        // Grid coords: valid play area is 0–17 on both axes.
        // ─────────────────────────────────────────────────────────────────────

        private static void PopulateWorld2(LevelData d, int level)
        {
            d.WorldName  = "Circuit Board";
            d.GridWidth  = 18;
            d.GridHeight = 18;

            float[] mult = { 0.6f, 0.75f, 1.0f, 1.25f, 1.5f };
            float m = mult[level - 1];

            d.StarThreshold1 = Mathf.RoundToInt(150 * m);
            d.StarThreshold2 = Mathf.RoundToInt(300 * m);
            d.StarThreshold3 = Mathf.RoundToInt(500 * m);
            d.ScoreTarget    = Mathf.RoundToInt(400 * m);

            // All power-ups except Poison (Circuit board introduces most, not Poison)
            var basicPowerUps = new[]
            {
                PowerUpType.SpeedBoost,
                PowerUpType.Shield,
                PowerUpType.ScoreMultiplier,
                PowerUpType.GhostMode,
                PowerUpType.ShrinkPill,
            };

            switch (level)
            {
                case 1:
                    // Single horizontal barrier across the middle with a gap
                    d.LevelName      = "Trace Route";
                    d.InitialSpeed   = 7f;
                    d.SpeedIncrement = 0.2f;
                    d.TimeLimit      = 0f;
                    d.WallPositions  = new[]
                    {
                        // Horizontal bar at y=9, x=2..7 (gap at x=8,9) then x=10..15
                        new Vector2Int(2,  9), new Vector2Int(3,  9), new Vector2Int(4,  9),
                        new Vector2Int(5,  9), new Vector2Int(6,  9), new Vector2Int(7,  9),
                        new Vector2Int(10, 9), new Vector2Int(11, 9), new Vector2Int(12, 9),
                        new Vector2Int(13, 9), new Vector2Int(14, 9), new Vector2Int(15, 9),
                    };
                    d.AllowedPowerUps = new[]
                    {
                        PowerUpType.SpeedBoost,
                        PowerUpType.Shield,
                        PowerUpType.ScoreMultiplier,
                    };
                    break;

                case 2:
                    // Vertical barrier down the middle with a gap
                    d.LevelName      = "Data Bus";
                    d.InitialSpeed   = 7.5f;
                    d.SpeedIncrement = 0.25f;
                    d.TimeLimit      = 120f;
                    d.WallPositions  = new[]
                    {
                        // Vertical bar at x=9, y=2..7 and y=10..15
                        new Vector2Int(9, 2),  new Vector2Int(9, 3),  new Vector2Int(9, 4),
                        new Vector2Int(9, 5),  new Vector2Int(9, 6),  new Vector2Int(9, 7),
                        new Vector2Int(9, 10), new Vector2Int(9, 11), new Vector2Int(9, 12),
                        new Vector2Int(9, 13), new Vector2Int(9, 14), new Vector2Int(9, 15),
                    };
                    d.AllowedPowerUps = new[]
                    {
                        PowerUpType.SpeedBoost,
                        PowerUpType.Shield,
                        PowerUpType.ScoreMultiplier,
                        PowerUpType.GhostMode,
                    };
                    break;

                case 3:
                    // Cross pattern (+) — horizontal + vertical barrier crossing at centre (gap each)
                    d.LevelName      = "Cross Circuit";
                    d.InitialSpeed   = 8f;
                    d.SpeedIncrement = 0.3f;
                    d.TimeLimit      = 100f;
                    d.WallPositions  = new[]
                    {
                        // Horizontal bar at y=9: x=2..6, x=11..15
                        new Vector2Int(2,  9), new Vector2Int(3,  9), new Vector2Int(4,  9),
                        new Vector2Int(5,  9), new Vector2Int(6,  9),
                        new Vector2Int(11, 9), new Vector2Int(12, 9), new Vector2Int(13, 9),
                        new Vector2Int(14, 9), new Vector2Int(15, 9),
                        // Vertical bar at x=9: y=2..6, y=11..15
                        new Vector2Int(9, 2),  new Vector2Int(9, 3),  new Vector2Int(9, 4),
                        new Vector2Int(9, 5),  new Vector2Int(9, 6),
                        new Vector2Int(9, 11), new Vector2Int(9, 12), new Vector2Int(9, 13),
                        new Vector2Int(9, 14), new Vector2Int(9, 15),
                    };
                    d.AllowedPowerUps = basicPowerUps;
                    break;

                case 4:
                    // Quadrant barriers — each quadrant has its own short wall stub
                    d.LevelName      = "Quad Sectors";
                    d.InitialSpeed   = 8.5f;
                    d.SpeedIncrement = 0.3f;
                    d.TimeLimit      = 85f;
                    d.WallPositions  = new[]
                    {
                        // Top-left quadrant: horizontal stub
                        new Vector2Int(2, 13), new Vector2Int(3, 13), new Vector2Int(4, 13),
                        new Vector2Int(5, 13),
                        // Top-right quadrant: horizontal stub
                        new Vector2Int(12, 13), new Vector2Int(13, 13), new Vector2Int(14, 13),
                        new Vector2Int(15, 13),
                        // Bottom-left quadrant: vertical stub
                        new Vector2Int(4, 2), new Vector2Int(4, 3), new Vector2Int(4, 4),
                        new Vector2Int(4, 5),
                        // Bottom-right quadrant: vertical stub
                        new Vector2Int(13, 2), new Vector2Int(13, 3), new Vector2Int(13, 4),
                        new Vector2Int(13, 5),
                        // Centre cross (partial, leaving gaps for navigation)
                        new Vector2Int(8,  9), new Vector2Int(9,  9),
                        new Vector2Int(9,  8), new Vector2Int(9,  10),
                    };
                    d.AllowedPowerUps = basicPowerUps;
                    break;

                case 5:
                    // Boss: dual horizontal + dual vertical = grid of corridors
                    d.LevelName      = "Circuit Overload";
                    d.InitialSpeed   = 9f;
                    d.SpeedIncrement = 0.35f;
                    d.TimeLimit      = 70f;
                    d.WallPositions  = new[]
                    {
                        // First horizontal barrier y=6: x=1..5 and x=7..11
                        new Vector2Int(1,  6), new Vector2Int(2,  6), new Vector2Int(3,  6),
                        new Vector2Int(4,  6), new Vector2Int(5,  6),
                        new Vector2Int(7,  6), new Vector2Int(8,  6), new Vector2Int(9,  6),
                        new Vector2Int(10, 6), new Vector2Int(11, 6),
                        // Second horizontal barrier y=12: x=6..10 and x=12..16
                        new Vector2Int(6,  12), new Vector2Int(7,  12), new Vector2Int(8,  12),
                        new Vector2Int(9,  12), new Vector2Int(10, 12),
                        new Vector2Int(12, 12), new Vector2Int(13, 12), new Vector2Int(14, 12),
                        new Vector2Int(15, 12), new Vector2Int(16, 12),
                        // First vertical barrier x=6: y=1..5
                        new Vector2Int(6, 1), new Vector2Int(6, 2), new Vector2Int(6, 3),
                        new Vector2Int(6, 4), new Vector2Int(6, 5),
                        // Second vertical barrier x=12: y=7..11
                        new Vector2Int(12, 7),  new Vector2Int(12, 8),  new Vector2Int(12, 9),
                        new Vector2Int(12, 10), new Vector2Int(12, 11),
                    };
                    d.AllowedPowerUps = basicPowerUps;
                    break;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // WORLD 3 — "Data Storm"  (Hard, 20×20)
        // L-shapes and T-junction mazes. All power-ups including Poison.
        // ─────────────────────────────────────────────────────────────────────

        private static void PopulateWorld3(LevelData d, int level)
        {
            d.WorldName  = "Data Storm";
            d.GridWidth  = 20;
            d.GridHeight = 20;

            float[] mult = { 0.6f, 0.75f, 1.0f, 1.25f, 1.5f };
            float m = mult[level - 1];

            d.StarThreshold1 = Mathf.RoundToInt(400  * m);
            d.StarThreshold2 = Mathf.RoundToInt(700  * m);
            d.StarThreshold3 = Mathf.RoundToInt(1000 * m);
            d.ScoreTarget    = Mathf.RoundToInt(800  * m);

            var allPowerUps = new[]
            {
                PowerUpType.SpeedBoost,
                PowerUpType.Shield,
                PowerUpType.ScoreMultiplier,
                PowerUpType.GhostMode,
                PowerUpType.ShrinkPill,
                PowerUpType.Poison,
            };

            switch (level)
            {
                case 1:
                    // Four L-shapes, one in each quadrant corner, opening inward
                    d.LevelName      = "Static Fields";
                    d.InitialSpeed   = 9f;
                    d.SpeedIncrement = 0.3f;
                    d.TimeLimit      = 0f;
                    d.WallPositions  = new[]
                    {
                        // Top-left L (opens right and down)
                        new Vector2Int(2, 17), new Vector2Int(3, 17), new Vector2Int(4, 17),
                        new Vector2Int(2, 16), new Vector2Int(2, 15),
                        // Top-right L (opens left and down)
                        new Vector2Int(15, 17), new Vector2Int(16, 17), new Vector2Int(17, 17),
                        new Vector2Int(17, 16), new Vector2Int(17, 15),
                        // Bottom-left L (opens right and up)
                        new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(4, 2),
                        new Vector2Int(2, 3), new Vector2Int(2, 4),
                        // Bottom-right L (opens left and up)
                        new Vector2Int(15, 2), new Vector2Int(16, 2), new Vector2Int(17, 2),
                        new Vector2Int(17, 3), new Vector2Int(17, 4),
                    };
                    d.AllowedPowerUps = new[]
                    {
                        PowerUpType.SpeedBoost,
                        PowerUpType.Shield,
                        PowerUpType.ScoreMultiplier,
                        PowerUpType.GhostMode,
                        PowerUpType.ShrinkPill,
                    };
                    break;

                case 2:
                    // Two T-junctions facing each other across the centre
                    d.LevelName      = "Signal Noise";
                    d.InitialSpeed   = 9.5f;
                    d.SpeedIncrement = 0.3f;
                    d.TimeLimit      = 130f;
                    d.WallPositions  = new[]
                    {
                        // Left T (facing right): vertical stem + left cap
                        new Vector2Int(3, 7),  new Vector2Int(3, 8),  new Vector2Int(3, 9),
                        new Vector2Int(3, 10), new Vector2Int(3, 11), new Vector2Int(3, 12),
                        new Vector2Int(2, 9),  new Vector2Int(2, 10),
                        // Right T (facing left): vertical stem + right cap
                        new Vector2Int(16, 7),  new Vector2Int(16, 8),  new Vector2Int(16, 9),
                        new Vector2Int(16, 10), new Vector2Int(16, 11), new Vector2Int(16, 12),
                        new Vector2Int(17, 9),  new Vector2Int(17, 10),
                        // Horizontal centre divider with gap at y=10
                        new Vector2Int(7,  10), new Vector2Int(8,  10), new Vector2Int(9,  10),
                        new Vector2Int(11, 10), new Vector2Int(12, 10),
                    };
                    d.AllowedPowerUps = allPowerUps;
                    break;

                case 3:
                    // S-curve channel: two offset horizontal bars creating a zigzag corridor
                    d.LevelName      = "Data Surge";
                    d.InitialSpeed   = 10f;
                    d.SpeedIncrement = 0.35f;
                    d.TimeLimit      = 110f;
                    d.WallPositions  = new[]
                    {
                        // Upper bar: y=13, x=1..9 (right half open)
                        new Vector2Int(1,  13), new Vector2Int(2,  13), new Vector2Int(3,  13),
                        new Vector2Int(4,  13), new Vector2Int(5,  13), new Vector2Int(6,  13),
                        new Vector2Int(7,  13), new Vector2Int(8,  13), new Vector2Int(9,  13),
                        // Lower bar: y=7, x=10..18 (left half open)
                        new Vector2Int(10, 7), new Vector2Int(11, 7), new Vector2Int(12, 7),
                        new Vector2Int(13, 7), new Vector2Int(14, 7), new Vector2Int(15, 7),
                        new Vector2Int(16, 7), new Vector2Int(17, 7), new Vector2Int(18, 7),
                        // Left vertical connector at x=1, y=7..13
                        new Vector2Int(1, 7),  new Vector2Int(1, 8),  new Vector2Int(1, 9),
                        new Vector2Int(1, 10), new Vector2Int(1, 11), new Vector2Int(1, 12),
                        // Right vertical connector at x=18, y=7..13
                        new Vector2Int(18, 8),  new Vector2Int(18, 9),  new Vector2Int(18, 10),
                        new Vector2Int(18, 11), new Vector2Int(18, 12), new Vector2Int(18, 13),
                    };
                    d.AllowedPowerUps = allPowerUps;
                    break;

                case 4:
                    // Maze: nested L corridors + T-junctions forming a partial grid maze
                    d.LevelName      = "Storm Core";
                    d.InitialSpeed   = 10.5f;
                    d.SpeedIncrement = 0.35f;
                    d.TimeLimit      = 90f;
                    d.WallPositions  = new[]
                    {
                        // Outer ring stubs (not full border — just indentations)
                        new Vector2Int(5,  2),  new Vector2Int(6,  2),  new Vector2Int(7,  2),
                        new Vector2Int(12, 2),  new Vector2Int(13, 2),  new Vector2Int(14, 2),
                        new Vector2Int(5,  17), new Vector2Int(6,  17), new Vector2Int(7,  17),
                        new Vector2Int(12, 17), new Vector2Int(13, 17), new Vector2Int(14, 17),
                        new Vector2Int(2,  5),  new Vector2Int(2,  6),  new Vector2Int(2,  7),
                        new Vector2Int(2,  12), new Vector2Int(2,  13), new Vector2Int(2,  14),
                        new Vector2Int(17, 5),  new Vector2Int(17, 6),  new Vector2Int(17, 7),
                        new Vector2Int(17, 12), new Vector2Int(17, 13), new Vector2Int(17, 14),
                        // Inner L-shapes at quadrant midpoints
                        new Vector2Int(6,  6),  new Vector2Int(7,  6),  new Vector2Int(6,  7),
                        new Vector2Int(12, 6),  new Vector2Int(13, 6),  new Vector2Int(13, 7),
                        new Vector2Int(6,  12), new Vector2Int(6,  13), new Vector2Int(7,  13),
                        new Vector2Int(13, 12), new Vector2Int(13, 13), new Vector2Int(12, 13),
                        // Centre block (2×2) forcing navigation around it
                        new Vector2Int(9, 9),   new Vector2Int(10, 9),
                        new Vector2Int(9, 10),  new Vector2Int(10, 10),
                    };
                    d.AllowedPowerUps = allPowerUps;
                    break;

                case 5:
                    // Boss: double-ring maze — outer ring stubs + inner ring stubs + centre block
                    d.LevelName      = "Data Meltdown";
                    d.InitialSpeed   = 11f;
                    d.SpeedIncrement = 0.4f;
                    d.TimeLimit      = 75f;
                    d.WallPositions  = new[]
                    {
                        // Outer ring stubs (denser)
                        new Vector2Int(4,  2),  new Vector2Int(5,  2),  new Vector2Int(6,  2),
                        new Vector2Int(13, 2),  new Vector2Int(14, 2),  new Vector2Int(15, 2),
                        new Vector2Int(4,  17), new Vector2Int(5,  17), new Vector2Int(6,  17),
                        new Vector2Int(13, 17), new Vector2Int(14, 17), new Vector2Int(15, 17),
                        new Vector2Int(2,  4),  new Vector2Int(2,  5),  new Vector2Int(2,  6),
                        new Vector2Int(2,  13), new Vector2Int(2,  14), new Vector2Int(2,  15),
                        new Vector2Int(17, 4),  new Vector2Int(17, 5),  new Vector2Int(17, 6),
                        new Vector2Int(17, 13), new Vector2Int(17, 14), new Vector2Int(17, 15),
                        // Inner ring (at roughly ±4 from edge inward)
                        new Vector2Int(5,  5),  new Vector2Int(6,  5),  new Vector2Int(7,  5),
                        new Vector2Int(12, 5),  new Vector2Int(13, 5),  new Vector2Int(14, 5),
                        new Vector2Int(5,  14), new Vector2Int(6,  14), new Vector2Int(7,  14),
                        new Vector2Int(12, 14), new Vector2Int(13, 14), new Vector2Int(14, 14),
                        new Vector2Int(5,  6),  new Vector2Int(5,  7),  new Vector2Int(5,  12),
                        new Vector2Int(5,  13),
                        new Vector2Int(14, 6),  new Vector2Int(14, 7),  new Vector2Int(14, 12),
                        new Vector2Int(14, 13),
                        // Centre 3×3 obstacle with single-cell entry gaps
                        new Vector2Int(8,  8),  new Vector2Int(9,  8),  new Vector2Int(10, 8),
                        new Vector2Int(8,  9),                           new Vector2Int(10, 9),
                        new Vector2Int(8,  10), new Vector2Int(9,  10), new Vector2Int(10, 10),
                    };
                    d.AllowedPowerUps = allPowerUps;
                    break;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // WORLD 4 — "Singularity"  (Expert, 22×22)
        // Dense interlocked obstacle courses. Tight time limits. All power-ups.
        // Grid valid play area: 0–21 on both axes.
        // ─────────────────────────────────────────────────────────────────────

        private static void PopulateWorld4(LevelData d, int level)
        {
            d.WorldName  = "Singularity";
            d.GridWidth  = 22;
            d.GridHeight = 22;

            float[] mult = { 0.6f, 0.75f, 1.0f, 1.25f, 1.5f };
            float m = mult[level - 1];

            d.StarThreshold1 = Mathf.RoundToInt(800  * m);
            d.StarThreshold2 = Mathf.RoundToInt(1200 * m);
            d.StarThreshold3 = Mathf.RoundToInt(1800 * m);
            d.ScoreTarget    = Mathf.RoundToInt(1500 * m);

            var allPowerUps = new[]
            {
                PowerUpType.SpeedBoost,
                PowerUpType.Shield,
                PowerUpType.ScoreMultiplier,
                PowerUpType.GhostMode,
                PowerUpType.ShrinkPill,
                PowerUpType.Poison,
            };

            switch (level)
            {
                case 1:
                    // Four symmetric L-arms radiating from the centre
                    d.LevelName      = "Event Horizon";
                    d.InitialSpeed   = 11f;
                    d.SpeedIncrement = 0.35f;
                    d.TimeLimit      = 120f;
                    d.WallPositions  = new[]
                    {
                        // North arm: x=10..11, y=12..17
                        new Vector2Int(10, 12), new Vector2Int(10, 13), new Vector2Int(10, 14),
                        new Vector2Int(10, 15), new Vector2Int(10, 16), new Vector2Int(10, 17),
                        new Vector2Int(11, 12), new Vector2Int(11, 13), new Vector2Int(11, 14),
                        new Vector2Int(11, 15), new Vector2Int(11, 16), new Vector2Int(11, 17),
                        // South arm: x=10..11, y=4..9
                        new Vector2Int(10, 4),  new Vector2Int(10, 5),  new Vector2Int(10, 6),
                        new Vector2Int(10, 7),  new Vector2Int(10, 8),  new Vector2Int(10, 9),
                        new Vector2Int(11, 4),  new Vector2Int(11, 5),  new Vector2Int(11, 6),
                        new Vector2Int(11, 7),  new Vector2Int(11, 8),  new Vector2Int(11, 9),
                        // West arm: x=4..9, y=10..11
                        new Vector2Int(4,  10), new Vector2Int(5,  10), new Vector2Int(6,  10),
                        new Vector2Int(7,  10), new Vector2Int(8,  10), new Vector2Int(9,  10),
                        new Vector2Int(4,  11), new Vector2Int(5,  11), new Vector2Int(6,  11),
                        new Vector2Int(7,  11), new Vector2Int(8,  11), new Vector2Int(9,  11),
                        // East arm: x=12..17, y=10..11
                        new Vector2Int(12, 10), new Vector2Int(13, 10), new Vector2Int(14, 10),
                        new Vector2Int(15, 10), new Vector2Int(16, 10), new Vector2Int(17, 10),
                        new Vector2Int(12, 11), new Vector2Int(13, 11), new Vector2Int(14, 11),
                        new Vector2Int(15, 11), new Vector2Int(16, 11), new Vector2Int(17, 11),
                    };
                    d.AllowedPowerUps = allPowerUps;
                    break;

                case 2:
                    // Concentric partial rings with narrow passage gaps
                    d.LevelName      = "Gravity Well";
                    d.InitialSpeed   = 11.5f;
                    d.SpeedIncrement = 0.35f;
                    d.TimeLimit      = 105f;
                    d.WallPositions  = new[]
                    {
                        // Outer partial ring segments (3-cell gap at each cardinal)
                        // Top segment: y=17, x=4..8 and x=13..17
                        new Vector2Int(4,  17), new Vector2Int(5,  17), new Vector2Int(6,  17),
                        new Vector2Int(7,  17), new Vector2Int(8,  17),
                        new Vector2Int(13, 17), new Vector2Int(14, 17), new Vector2Int(15, 17),
                        new Vector2Int(16, 17), new Vector2Int(17, 17),
                        // Bottom segment: y=4, x=4..8 and x=13..17
                        new Vector2Int(4,  4),  new Vector2Int(5,  4),  new Vector2Int(6,  4),
                        new Vector2Int(7,  4),  new Vector2Int(8,  4),
                        new Vector2Int(13, 4),  new Vector2Int(14, 4),  new Vector2Int(15, 4),
                        new Vector2Int(16, 4),  new Vector2Int(17, 4),
                        // Left segment: x=4, y=5..8 and y=13..16
                        new Vector2Int(4, 5),   new Vector2Int(4, 6),   new Vector2Int(4, 7),
                        new Vector2Int(4, 8),
                        new Vector2Int(4, 13),  new Vector2Int(4, 14),  new Vector2Int(4, 15),
                        new Vector2Int(4, 16),
                        // Right segment: x=17, y=5..8 and y=13..16
                        new Vector2Int(17, 5),  new Vector2Int(17, 6),  new Vector2Int(17, 7),
                        new Vector2Int(17, 8),
                        new Vector2Int(17, 13), new Vector2Int(17, 14), new Vector2Int(17, 15),
                        new Vector2Int(17, 16),
                        // Inner 3×3 centre
                        new Vector2Int(9,  9),  new Vector2Int(10, 9),  new Vector2Int(11, 9),
                        new Vector2Int(9,  10), new Vector2Int(11, 10),
                        new Vector2Int(9,  11), new Vector2Int(10, 11), new Vector2Int(11, 11),
                    };
                    d.AllowedPowerUps = allPowerUps;
                    break;

                case 3:
                    // Interlocked staggered rows — alternating-direction horizontal bars
                    d.LevelName      = "Phase Collapse";
                    d.InitialSpeed   = 12f;
                    d.SpeedIncrement = 0.4f;
                    d.TimeLimit      = 90f;
                    d.WallPositions  = new[]
                    {
                        // Row 1 at y=16: x=1..8 (right side open)
                        new Vector2Int(1,  16), new Vector2Int(2,  16), new Vector2Int(3,  16),
                        new Vector2Int(4,  16), new Vector2Int(5,  16), new Vector2Int(6,  16),
                        new Vector2Int(7,  16), new Vector2Int(8,  16),
                        // Row 2 at y=13: x=13..20 (left side open)
                        new Vector2Int(13, 13), new Vector2Int(14, 13), new Vector2Int(15, 13),
                        new Vector2Int(16, 13), new Vector2Int(17, 13), new Vector2Int(18, 13),
                        new Vector2Int(19, 13), new Vector2Int(20, 13),
                        // Row 3 at y=10: x=1..8
                        new Vector2Int(1,  10), new Vector2Int(2,  10), new Vector2Int(3,  10),
                        new Vector2Int(4,  10), new Vector2Int(5,  10), new Vector2Int(6,  10),
                        new Vector2Int(7,  10), new Vector2Int(8,  10),
                        // Row 4 at y=7: x=13..20
                        new Vector2Int(13, 7),  new Vector2Int(14, 7),  new Vector2Int(15, 7),
                        new Vector2Int(16, 7),  new Vector2Int(17, 7),  new Vector2Int(18, 7),
                        new Vector2Int(19, 7),  new Vector2Int(20, 7),
                        // Row 5 at y=4: x=1..8
                        new Vector2Int(1,  4),  new Vector2Int(2,  4),  new Vector2Int(3,  4),
                        new Vector2Int(4,  4),  new Vector2Int(5,  4),  new Vector2Int(6,  4),
                        new Vector2Int(7,  4),  new Vector2Int(8,  4),
                        // Vertical connectors joining rows on the right side (x=20)
                        new Vector2Int(20, 14), new Vector2Int(20, 15), new Vector2Int(20, 16),
                        new Vector2Int(20, 4),  new Vector2Int(20, 5),  new Vector2Int(20, 6),
                        // Vertical connectors on the left side (x=1)
                        new Vector2Int(1,  11), new Vector2Int(1,  12), new Vector2Int(1,  13),
                        new Vector2Int(1,  5),  new Vector2Int(1,  6),  new Vector2Int(1,  7),
                    };
                    d.AllowedPowerUps = allPowerUps;
                    break;

                case 4:
                    // Radial spokes from two off-centre hubs
                    d.LevelName      = "Null Pointer";
                    d.InitialSpeed   = 12.5f;
                    d.SpeedIncrement = 0.4f;
                    d.TimeLimit      = 75f;
                    d.WallPositions  = new[]
                    {
                        // Left hub at (6,11): 4 spokes of length 3
                        // North spoke
                        new Vector2Int(6, 12), new Vector2Int(6, 13), new Vector2Int(6, 14),
                        // South spoke
                        new Vector2Int(6, 8),  new Vector2Int(6, 9),  new Vector2Int(6, 10),
                        // East spoke
                        new Vector2Int(7, 11), new Vector2Int(8, 11), new Vector2Int(9, 11),
                        // West spoke
                        new Vector2Int(3, 11), new Vector2Int(4, 11), new Vector2Int(5, 11),
                        // Right hub at (16,11): 4 spokes of length 3
                        // North spoke
                        new Vector2Int(16, 12), new Vector2Int(16, 13), new Vector2Int(16, 14),
                        // South spoke
                        new Vector2Int(16, 8),  new Vector2Int(16, 9),  new Vector2Int(16, 10),
                        // West spoke
                        new Vector2Int(13, 11), new Vector2Int(14, 11), new Vector2Int(15, 11),
                        // East spoke
                        new Vector2Int(17, 11), new Vector2Int(18, 11), new Vector2Int(19, 11),
                        // Connecting bar between hubs (y=11, x=10..12 — the gap between them)
                        new Vector2Int(10, 11), new Vector2Int(11, 11), new Vector2Int(12, 11),
                        // Upper diagonal L-wings
                        new Vector2Int(4,  17), new Vector2Int(5,  17), new Vector2Int(6,  17),
                        new Vector2Int(4,  16), new Vector2Int(4,  15),
                        new Vector2Int(15, 17), new Vector2Int(16, 17), new Vector2Int(17, 17),
                        new Vector2Int(17, 16), new Vector2Int(17, 15),
                        // Lower diagonal L-wings
                        new Vector2Int(4,  4),  new Vector2Int(5,  4),  new Vector2Int(6,  4),
                        new Vector2Int(4,  5),  new Vector2Int(4,  6),
                        new Vector2Int(15, 4),  new Vector2Int(16, 4),  new Vector2Int(17, 4),
                        new Vector2Int(17, 5),  new Vector2Int(17, 6),
                    };
                    d.AllowedPowerUps = allPowerUps;
                    break;

                case 5:
                    // Boss: full labyrinth — outer shell + inner cross + 8 island blocks
                    d.LevelName      = "Singularity Core";
                    d.InitialSpeed   = 13f;
                    d.SpeedIncrement = 0.45f;
                    d.TimeLimit      = 60f;
                    d.WallPositions  = new[]
                    {
                        // Outer shell stubs (tight border with deliberate single-cell passage gaps)
                        // Top wall: y=19, x=2..7 gap@8 x=9..12 gap@13 x=14..19
                        new Vector2Int(2,  19), new Vector2Int(3,  19), new Vector2Int(4,  19),
                        new Vector2Int(5,  19), new Vector2Int(6,  19), new Vector2Int(7,  19),
                        new Vector2Int(9,  19), new Vector2Int(10, 19), new Vector2Int(11, 19),
                        new Vector2Int(12, 19), new Vector2Int(14, 19), new Vector2Int(15, 19),
                        new Vector2Int(16, 19), new Vector2Int(17, 19), new Vector2Int(18, 19),
                        new Vector2Int(19, 19),
                        // Bottom wall: y=2, x=2..7 gap@8 x=9..12 gap@13 x=14..19
                        new Vector2Int(2,  2),  new Vector2Int(3,  2),  new Vector2Int(4,  2),
                        new Vector2Int(5,  2),  new Vector2Int(6,  2),  new Vector2Int(7,  2),
                        new Vector2Int(9,  2),  new Vector2Int(10, 2),  new Vector2Int(11, 2),
                        new Vector2Int(12, 2),  new Vector2Int(14, 2),  new Vector2Int(15, 2),
                        new Vector2Int(16, 2),  new Vector2Int(17, 2),  new Vector2Int(18, 2),
                        new Vector2Int(19, 2),
                        // Left wall: x=2, y=3..7 gap@8 y=9..12 gap@13 y=14..18
                        new Vector2Int(2, 3),   new Vector2Int(2, 4),   new Vector2Int(2, 5),
                        new Vector2Int(2, 6),   new Vector2Int(2, 7),
                        new Vector2Int(2, 9),   new Vector2Int(2, 10),  new Vector2Int(2, 11),
                        new Vector2Int(2, 12),  new Vector2Int(2, 14),  new Vector2Int(2, 15),
                        new Vector2Int(2, 16),  new Vector2Int(2, 17),  new Vector2Int(2, 18),
                        // Right wall: x=19, y=3..7 gap@8 y=9..12 gap@13 y=14..18
                        new Vector2Int(19, 3),  new Vector2Int(19, 4),  new Vector2Int(19, 5),
                        new Vector2Int(19, 6),  new Vector2Int(19, 7),
                        new Vector2Int(19, 9),  new Vector2Int(19, 10), new Vector2Int(19, 11),
                        new Vector2Int(19, 12), new Vector2Int(19, 14), new Vector2Int(19, 15),
                        new Vector2Int(19, 16), new Vector2Int(19, 17), new Vector2Int(19, 18),
                        // Inner cross — horizontal arm y=11, x=5..7 and x=14..16
                        new Vector2Int(5,  11), new Vector2Int(6,  11), new Vector2Int(7,  11),
                        new Vector2Int(14, 11), new Vector2Int(15, 11), new Vector2Int(16, 11),
                        // Inner cross — vertical arm x=11, y=5..7 and y=14..16
                        new Vector2Int(11, 5),  new Vector2Int(11, 6),  new Vector2Int(11, 7),
                        new Vector2Int(11, 14), new Vector2Int(11, 15), new Vector2Int(11, 16),
                        // Eight 2×2 island blocks in each octant
                        // NW island
                        new Vector2Int(5,  15), new Vector2Int(6,  15),
                        new Vector2Int(5,  16), new Vector2Int(6,  16),
                        // NE island
                        new Vector2Int(15, 15), new Vector2Int(16, 15),
                        new Vector2Int(15, 16), new Vector2Int(16, 16),
                        // SW island
                        new Vector2Int(5,  5),  new Vector2Int(6,  5),
                        new Vector2Int(5,  6),  new Vector2Int(6,  6),
                        // SE island
                        new Vector2Int(15, 5),  new Vector2Int(16, 5),
                        new Vector2Int(15, 6),  new Vector2Int(16, 6),
                        // Centre 2×2 core
                        new Vector2Int(10, 10), new Vector2Int(11, 10),
                        new Vector2Int(10, 11),
                        // (11,11 intentionally omitted to leave one-cell gap in centre)
                    };
                    d.AllowedPowerUps = allPowerUps;
                    break;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // NEXT LEVEL WIRING
        // Chains: W1L1→W1L2→...→W1L5→W2L1→...→W4L5→null
        // ─────────────────────────────────────────────────────────────────────

        private static void WireNextLevelReferences(LevelData[,] grid)
        {
            for (int w = 0; w < 4; w++)
            {
                for (int l = 0; l < 5; l++)
                {
                    LevelData current = grid[w, l];

                    // Advance to next level
                    if (l < 4)
                    {
                        // Next level in the same world
                        current.NextLevel = grid[w, l + 1];
                    }
                    else if (w < 3)
                    {
                        // First level of the next world
                        current.NextLevel = grid[w + 1, 0];
                    }
                    else
                    {
                        // World 4 Level 5 — final level, no next
                        current.NextLevel = null;
                    }

                    // Mark the SO dirty so the reference persists
                    EditorUtility.SetDirty(current);
                }
            }

            Debug.Log("[CampaignLevelGenerator] NextLevel references wired across all 20 levels.");
        }
    }
}
