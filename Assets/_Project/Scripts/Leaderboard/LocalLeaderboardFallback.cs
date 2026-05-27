using System;
using System.Collections.Generic;
using UnityEngine;
using NeonSerpent.SaveData;

namespace NeonSerpent.Leaderboard
{
    /// <summary>
    /// Offline-only leaderboard backed by PlayerData saved to disk.
    /// Used when GPGS is unavailable or the player is not authenticated.
    /// </summary>
    public class LocalLeaderboardFallback : ILeaderboardService
    {
        private readonly SaveManager _saveManager;

        public LocalLeaderboardFallback(SaveManager saveManager)
        {
            _saveManager = saveManager;
        }

        public bool IsAuthenticated => false;

        public void Initialize() { /* no-op */ }

        public void Authenticate(Action<bool> onComplete) => onComplete?.Invoke(false);

        public void SubmitScore(long score, string leaderboardId, Action<bool> onComplete)
        {
            // GameSession.RecordHighScore already calls SaveManager.Save() after this returns.
            _saveManager.Data.RecordLocalHighScore(leaderboardId, score);
            onComplete?.Invoke(true);
        }

        public void ShowLeaderboard(string leaderboardId)
        {
            // Handled by LeaderboardUI in the Leaderboard scene
            Debug.Log("[LocalLeaderboard] ShowLeaderboard called — UI will display local scores.");
        }

        public void GetTopScores(string leaderboardId, int count, Action<List<LeaderboardEntry>> onComplete)
        {
            var entries = _saveManager.Data.GetLocalTopScores(leaderboardId, count);
            onComplete?.Invoke(entries);
        }
    }
}
