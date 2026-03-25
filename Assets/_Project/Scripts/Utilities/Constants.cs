namespace NeonSerpent.Utilities
{
    /// <summary>
    /// Project-wide constants. Leaderboard IDs are filled in after Google Play Console setup.
    /// </summary>
    public static class Constants
    {
        // --- Grid ---
        public const int DEFAULT_GRID_WIDTH  = 20;
        public const int DEFAULT_GRID_HEIGHT = 20;
        public const float CELL_SIZE         = 1f;

        // --- Snake ---
        public const float DEFAULT_SPEED       = 8f;   // cells per second
        public const float SPEED_INCREMENT     = 0.3f; // added per food eaten
        public const float MAX_SPEED           = 20f;

        // --- Power-Ups ---
        public const float POWERUP_DESPAWN_TIME = 10f;
        public const int   MAX_ACTIVE_POWERUPS  = 2;

        // --- Scoring ---
        public const int SCORE_NORMAL_FOOD   = 10;
        public const int SCORE_BONUS_FOOD    = 50;
        public const int COMBO_THRESHOLD     = 5;   // eats in a row for combo bonus

        // --- Google Play Games Services leaderboard IDs ---
        // TODO: Replace with real IDs from Google Play Console after app is registered
        public const string LEADERBOARD_CLASSIC    = "LEADERBOARD_CLASSIC_PLACEHOLDER";
        public const string LEADERBOARD_TIME_ATTACK = "LEADERBOARD_TIMEATTACK_PLACEHOLDER";
        public const string LEADERBOARD_CAMPAIGN   = "LEADERBOARD_CAMPAIGN_PLACEHOLDER";

        // --- IAP Product IDs ---
        public const string IAP_REMOVE_ADS   = "remove_ads";
        public const string IAP_COINS_SMALL  = "coins_small";
        public const string IAP_COINS_MEDIUM = "coins_medium";
        public const string IAP_COINS_LARGE  = "coins_large";

        // --- Save ---
        public const string SAVE_FILE_NAME = "player_data.json";

        // --- Scenes ---
        public const string SCENE_BOOTSTRAP  = "Bootstrap";
        public const string SCENE_MAIN_MENU  = "MainMenu";
        public const string SCENE_GAME       = "Game";
        public const string SCENE_LEADERBOARD = "Leaderboard";
        public const string SCENE_SHOP       = "Shop";
        public const string SCENE_SETTINGS   = "Settings";
    }
}
