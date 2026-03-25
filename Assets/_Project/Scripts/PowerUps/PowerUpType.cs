namespace NeonSerpent.PowerUps
{
    /// <summary>All available power-up types in the game.</summary>
    public enum PowerUpType
    {
        SpeedBoost,        // Move 2x faster for 5s
        Shield,            // Absorb one fatal collision
        ScoreMultiplier,   // 2x score for 8s
        GhostMode,         // Pass through own body for 6s
        ShrinkPill,        // Remove 3 tail segments instantly
        Poison             // Slow + zero score on next food for 4s (applied by poison food)
    }
}
