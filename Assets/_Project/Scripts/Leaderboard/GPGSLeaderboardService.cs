// To enable GPGS, add the Google Play Games Unity Plugin v11+ via the Unity Package Manager
// or by importing the .unitypackage, then add GPGS_ENABLED to:
//   Project Settings > Player > Other Settings > Scripting Define Symbols

using System;
using System.Collections.Generic;
using UnityEngine;

#if GPGS_ENABLED
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

namespace NeonSerpent.Leaderboard
{
    /// <summary>
    /// Implements <see cref="ILeaderboardService"/> using the Google Play Games Services
    /// Unity Plugin v11+. Compile-guarded behind GPGS_ENABLED so the project builds
    /// cleanly before the plugin is imported.
    /// </summary>
    public class GPGSLeaderboardService : ILeaderboardService
    {
        private bool _isAuthenticated;

        /// <inheritdoc/>
        public bool IsAuthenticated => _isAuthenticated;

        // -------------------------------------------------------------------------
        // Initialization
        // -------------------------------------------------------------------------

        /// <summary>
        /// Activates the PlayGamesPlatform and sets it as the active Social platform.
        /// Must be called once during Bootstrap before any other GPGS calls.
        /// </summary>
        public void Initialize()
        {
#if GPGS_ENABLED
            // v11+ API: PlayGamesClientConfiguration was removed. Activate directly.
            PlayGamesPlatform.DebugLogEnabled = Debug.isDebugBuild;
            PlayGamesPlatform.Activate();
            Debug.Log("[GPGS] PlayGamesPlatform activated.");
#else
            Debug.Log("[GPGS] GPGS_ENABLED not defined — running in stub mode. " +
                      "Import the GPGS plugin and add GPGS_ENABLED to Scripting Define Symbols.");
#endif
        }

        // -------------------------------------------------------------------------
        // Authentication
        // -------------------------------------------------------------------------

        /// <summary>
        /// Attempts to sign in via Google Play Games Services.
        /// Calls <paramref name="onComplete"/> with <c>true</c> on success, <c>false</c> on failure.
        /// On first run this shows the consent UI; subsequent calls are silent.
        /// </summary>
        /// <param name="onComplete">Callback receiving the authentication result.</param>
        public void Authenticate(Action<bool> onComplete)
        {
#if GPGS_ENABLED
            Social.localUser.Authenticate(success =>
            {
                _isAuthenticated = success;
                if (success)
                    Debug.Log($"[GPGS] Signed in as: {Social.localUser.userName}");
                else
                    Debug.LogWarning("[GPGS] Authentication failed.");
                onComplete?.Invoke(success);
            });
#else
            Debug.Log("[GPGS] Authenticate stub — GPGS_ENABLED not defined.");
            _isAuthenticated = false;
            onComplete?.Invoke(false);
#endif
        }

        // -------------------------------------------------------------------------
        // Score Submission
        // -------------------------------------------------------------------------

        /// <summary>
        /// Reports <paramref name="score"/> to the specified leaderboard.
        /// No-ops if the player is not authenticated.
        /// </summary>
        /// <param name="score">The score value to report.</param>
        /// <param name="leaderboardId">The Play Console leaderboard ID string.</param>
        /// <param name="onComplete">Callback receiving <c>true</c> on successful submission.</param>
        public void SubmitScore(long score, string leaderboardId, Action<bool> onComplete)
        {
#if GPGS_ENABLED
            if (!_isAuthenticated)
            {
                Debug.LogWarning("[GPGS] SubmitScore called while not authenticated.");
                onComplete?.Invoke(false);
                return;
            }

            Social.Active.ReportScore(score, leaderboardId, success =>
            {
                if (!success)
                    Debug.LogWarning($"[GPGS] ReportScore failed for leaderboard: {leaderboardId}");
                onComplete?.Invoke(success);
            });
#else
            Debug.Log($"[GPGS] SubmitScore stub — score={score}, leaderboard={leaderboardId}");
            onComplete?.Invoke(false);
#endif
        }

        // -------------------------------------------------------------------------
        // Native UI
        // -------------------------------------------------------------------------

        /// <summary>
        /// Opens the native Google Play Games leaderboard overlay for the specified leaderboard.
        /// No-ops if the player is not authenticated.
        /// </summary>
        /// <param name="leaderboardId">The Play Console leaderboard ID string.</param>
        public void ShowLeaderboard(string leaderboardId)
        {
#if GPGS_ENABLED
            if (!_isAuthenticated)
            {
                Debug.LogWarning("[GPGS] ShowLeaderboard called while not authenticated.");
                return;
            }

            PlayGamesPlatform.Instance.ShowLeaderboardUI(leaderboardId);
#else
            Debug.Log($"[GPGS] ShowLeaderboard stub — leaderboardId={leaderboardId}");
#endif
        }

        // -------------------------------------------------------------------------
        // Score Fetching
        // -------------------------------------------------------------------------

        /// <summary>
        /// Asynchronously fetches the top <paramref name="count"/> scores from the given leaderboard
        /// and returns them as a list of <see cref="LeaderboardEntry"/> objects.
        /// Invokes <paramref name="onComplete"/> with an empty list if unauthenticated or on failure.
        /// </summary>
        /// <param name="leaderboardId">The Play Console leaderboard ID string.</param>
        /// <param name="count">Maximum number of entries to retrieve (1–25 recommended).</param>
        /// <param name="onComplete">Callback receiving the ordered list of entries.</param>
        public void GetTopScores(string leaderboardId, int count, Action<List<LeaderboardEntry>> onComplete)
        {
#if GPGS_ENABLED
            if (!_isAuthenticated)
            {
                Debug.LogWarning("[GPGS] GetTopScores called while not authenticated.");
                onComplete?.Invoke(new List<LeaderboardEntry>());
                return;
            }

            PlayGamesPlatform.Instance.LoadScores(
                leaderboardId,
                LeaderboardStart.TopScores,
                count,
                LeaderboardCollection.Public,
                LeaderboardTimeSpan.AllTime,
                data =>
                {
                    var entries = new List<LeaderboardEntry>();

                    if (data.Valid)
                    {
                        string localPlayerId = Social.localUser.id;
                        int rank = 1;
                        foreach (IScore score in data.Scores)
                        {
                            entries.Add(new LeaderboardEntry
                            {
                                PlayerName    = score.userID == localPlayerId
                                                    ? Social.localUser.userName
                                                    : score.userID,
                                Score         = score.value,
                                Rank          = rank++,
                                IsLocalPlayer = score.userID == localPlayerId
                            });
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[GPGS] LoadScores returned invalid data for: {leaderboardId}");
                    }

                    onComplete?.Invoke(entries);
                });
#else
            Debug.Log($"[GPGS] GetTopScores stub — leaderboardId={leaderboardId}, count={count}");
            onComplete?.Invoke(new List<LeaderboardEntry>());
#endif
        }
    }
}
