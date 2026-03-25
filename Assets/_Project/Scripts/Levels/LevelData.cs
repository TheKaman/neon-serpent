using UnityEngine;
using NeonSerpent.Core;
using NeonSerpent.PowerUps;

namespace NeonSerpent.Levels
{
    /// <summary>
    /// ScriptableObject defining all parameters for a single level or game mode session.
    /// Create via: Assets > Create > NeonSerpent > Level Data
    /// </summary>
    [CreateAssetMenu(fileName = "LevelData", menuName = "NeonSerpent/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Identity")]
        public int    LevelIndex;
        public string LevelName;
        public string WorldName;

        [Header("Grid")]
        public int GridWidth  = 20;
        public int GridHeight = 20;

        [Header("Snake")]
        public float InitialSpeed    = 8f;
        public float SpeedIncrement  = 0.3f; // added per food eaten

        [Header("Win Conditions")]
        public float TimeLimit   = 0f;   // seconds; 0 = no limit
        public int   ScoreTarget = 0;    // 0 = no target (endless); Campaign must reach this

        [Header("Obstacles")]
        public Vector2Int[] WallPositions;

        [Header("Power-Ups")]
        public PowerUpType[] AllowedPowerUps;

        [Header("Mode")]
        public GameMode Mode = GameMode.ClassicEndless;

        [Header("Star Thresholds")]
        public int StarThreshold1;
        public int StarThreshold2;
        public int StarThreshold3;

        [Header("Campaign Progression")]
        public LevelData NextLevel; // null = final level
    }
}
