using UnityEngine;
using NeonSerpent.SaveData;

namespace NeonSerpent.Leaderboard
{
    /// <summary>
    /// MonoBehaviour wrapper that instantiates the correct ILeaderboardService implementation
    /// at runtime. Attach to a GameObject in the Bootstrap scene.
    /// When GPGS is available and authenticated, uses GPGSLeaderboardService;
    /// otherwise falls back to LocalLeaderboardFallback.
    /// Assign to LeaderboardUI._leaderboardServiceProvider via Inspector.
    /// </summary>
    public class LeaderboardServiceProvider : MonoBehaviour
    {
        private ILeaderboardService _service;

        /// <summary>The active leaderboard service. Null until Awake completes.</summary>
        public ILeaderboardService Service => _service;

        /// <summary>
        /// Whether the underlying service is currently authenticated with the platform.
        /// Always false for the local fallback. Use this before calling SubmitScore to
        /// avoid a silent no-op when GPGS has not signed in yet.
        /// </summary>
        public bool IsAuthenticated => _service != null && _service.IsAuthenticated;

        private void Start()
        {
            // Moved from Awake to Start so that SaveManager.Awake() is guaranteed to
            // have run before we read SaveManager.Instance (all sibling Awakes complete
            // before any Start runs, even when objects share the same parent).
#if GPGS_ENABLED
            _service = new GPGSLeaderboardService();
#else
            _service = new LocalLeaderboardFallback(SaveManager.Instance);
#endif
            _service.Initialize();
        }

        /// <summary>
        /// Attempt to authenticate with the platform leaderboard service.
        /// Result is delivered via the callback (true = authenticated).
        /// </summary>
        public void Authenticate(System.Action<bool> onComplete)
        {
            _service?.Authenticate(onComplete);
        }

        /// <summary>Submit a score to the specified leaderboard.</summary>
        public void SubmitScore(long score, string leaderboardId, System.Action<bool> onComplete)
        {
            _service?.SubmitScore(score, leaderboardId, onComplete);
        }

        /// <summary>Show the native platform leaderboard overlay for the given leaderboard ID.</summary>
        public void ShowLeaderboard(string leaderboardId)
        {
            _service?.ShowLeaderboard(leaderboardId);
        }
    }
}
