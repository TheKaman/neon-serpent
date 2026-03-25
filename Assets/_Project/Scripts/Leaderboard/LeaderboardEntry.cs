using System;

namespace NeonSerpent.Leaderboard
{
    /// <summary>Data transfer object for a single leaderboard entry.</summary>
    [Serializable]
    public class LeaderboardEntry
    {
        public string PlayerName;
        public long   Score;
        public int    Rank;
        public bool   IsLocalPlayer;
    }
}
