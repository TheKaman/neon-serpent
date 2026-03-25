using System;
using System.Collections.Generic;

namespace NeonSerpent.Leaderboard
{
    /// <summary>
    /// Interface for leaderboard operations.
    /// Implemented by GPGSLeaderboardService (online) and LocalLeaderboardFallback (offline).
    /// Use the factory in LeaderboardService to get the correct implementation.
    /// </summary>
    public interface ILeaderboardService
    {
        /// <summary>Initialize the leaderboard system (call once on Bootstrap).</summary>
        void Initialize();

        bool IsAuthenticated { get; }

        /// <summary>Attempt silent or explicit sign-in.</summary>
        void Authenticate(Action<bool> onComplete);

        /// <summary>Submit a score to the given leaderboard ID.</summary>
        void SubmitScore(long score, string leaderboardId, Action<bool> onComplete);

        /// <summary>Show the platform's native leaderboard UI overlay.</summary>
        void ShowLeaderboard(string leaderboardId);

        /// <summary>Fetch the top N scores for display in custom UI.</summary>
        void GetTopScores(string leaderboardId, int count, Action<List<LeaderboardEntry>> onComplete);
    }
}
