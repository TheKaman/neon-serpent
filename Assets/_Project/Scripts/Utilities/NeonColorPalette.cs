using UnityEngine;

namespace NeonSerpent.Utilities
{
    /// <summary>
    /// Centralized neon color constants used by materials, UI, and visual effects.
    /// All colors are HDR-bright to work with URP Bloom post-processing.
    /// </summary>
    public static class NeonColorPalette
    {
        public static readonly Color SnakeHead    = new Color(0.0f, 1.0f, 0.4f, 1f);   // neon green
        public static readonly Color SnakeBody    = new Color(0.0f, 0.8f, 0.3f, 1f);   // slightly dimmer green
        public static readonly Color FoodNormal   = new Color(1.0f, 0.2f, 0.2f, 1f);   // neon red
        public static readonly Color FoodBonus    = new Color(1.0f, 0.8f, 0.0f, 1f);   // neon gold
        public static readonly Color FoodPoison   = new Color(0.6f, 0.0f, 1.0f, 1f);   // neon purple
        public static readonly Color GridLines    = new Color(0.1f, 0.2f, 0.1f, 1f);   // subtle dark grid
        public static readonly Color Background   = new Color(0.02f, 0.03f, 0.05f, 1f); // near-black
        public static readonly Color UIAccent     = new Color(0.0f, 0.9f, 1.0f, 1f);   // neon cyan
        public static readonly Color UIWarning    = new Color(1.0f, 0.4f, 0.0f, 1f);   // neon orange
        public static readonly Color ScoreText    = new Color(1.0f, 1.0f, 1.0f, 1f);   // white

        // Power-up colors
        public static readonly Color PowerUpSpeed      = new Color(1.0f, 0.7f, 0.0f, 1f); // orange
        public static readonly Color PowerUpShield     = new Color(0.0f, 0.6f, 1.0f, 1f); // blue
        public static readonly Color PowerUpMultiplier = new Color(1.0f, 0.2f, 0.8f, 1f); // pink
        public static readonly Color PowerUpGhost      = new Color(0.7f, 0.7f, 1.0f, 0.5f); // translucent blue
        public static readonly Color PowerUpShrink     = new Color(0.4f, 1.0f, 0.0f, 1f); // lime
        public static readonly Color PowerUpPoison     = new Color(0.5f, 0.0f, 0.8f, 1f); // purple
    }
}
