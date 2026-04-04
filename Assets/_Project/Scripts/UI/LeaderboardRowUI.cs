using TMPro;
using UnityEngine;
using NeonSerpent.Leaderboard;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Displays a single leaderboard entry row: rank, player name, and score.
    /// Attach to the row prefab that LeaderboardUI instantiates into its entries container.
    /// </summary>
    public class LeaderboardRowUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _rankText;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _scoreText;

        /// <summary>
        /// Fills all three text fields from the given <see cref="LeaderboardEntry"/>.
        /// Rank is displayed as "#1", score uses the "N0" format (thousands separator).
        /// </summary>
        /// <param name="entry">The leaderboard entry data to display.</param>
        public void Populate(LeaderboardEntry entry)
        {
            if (entry == null)
            {
                Debug.LogWarning("[LeaderboardRowUI] Populate called with null entry.");
                return;
            }

            if (_rankText  != null) _rankText.text  = $"#{entry.Rank}";
            if (_nameText  != null) _nameText.text  = entry.PlayerName;
            if (_scoreText != null) _scoreText.text = entry.Score.ToString("N0");
        }
    }
}
