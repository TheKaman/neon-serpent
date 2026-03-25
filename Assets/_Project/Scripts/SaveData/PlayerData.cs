using System;
using System.Collections.Generic;
using UnityEngine;
using NeonSerpent.Leaderboard;

namespace NeonSerpent.SaveData
{
    /// <summary>
    /// All persistent player data. Serialized to JSON by SaveManager.
    /// Do not access PlayerPrefs anywhere else — everything goes through this class.
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        public bool   isAdFree          = false;
        public int    coins             = 0;
        public string equippedSkinId    = "default";
        public List<string> unlockedSkinIds = new List<string> { "default" };

        // Campaign progress: key = level index, value = stars earned (0–3)
        public SerializableDictionary<int, int> campaignProgress = new SerializableDictionary<int, int>();

        // Local high scores: key = leaderboard ID, value = sorted list of (name, score)
        public SerializableDictionary<string, List<LocalScoreEntry>> localScores =
            new SerializableDictionary<string, List<LocalScoreEntry>>();

        public void RecordLocalHighScore(string leaderboardId, long score)
        {
            if (!localScores.ContainsKey(leaderboardId))
                localScores[leaderboardId] = new List<LocalScoreEntry>();

            localScores[leaderboardId].Add(new LocalScoreEntry { score = score, timestamp = DateTime.UtcNow.Ticks });
            localScores[leaderboardId].Sort((a, b) => b.score.CompareTo(a.score));

            // Keep only top 100
            if (localScores[leaderboardId].Count > 100)
                localScores[leaderboardId].RemoveRange(100, localScores[leaderboardId].Count - 100);
        }

        public List<LeaderboardEntry> GetLocalTopScores(string leaderboardId, int count)
        {
            var result = new List<LeaderboardEntry>();
            if (!localScores.ContainsKey(leaderboardId)) return result;

            var entries = localScores[leaderboardId];
            for (int i = 0; i < Mathf.Min(count, entries.Count); i++)
            {
                result.Add(new LeaderboardEntry
                {
                    Rank        = i + 1,
                    Score       = entries[i].score,
                    PlayerName  = "You",
                    IsLocalPlayer = true
                });
            }
            return result;
        }
    }

    [Serializable]
    public class LocalScoreEntry
    {
        public long score;
        public long timestamp;
    }

    /// <summary>Unity-serializable dictionary wrapper.</summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey>   _keys   = new List<TKey>();
        [SerializeField] private List<TValue> _values = new List<TValue>();

        public void OnBeforeSerialize()
        {
            _keys.Clear();
            _values.Clear();
            foreach (var pair in this)
            {
                _keys.Add(pair.Key);
                _values.Add(pair.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            Clear();
            for (int i = 0; i < Mathf.Min(_keys.Count, _values.Count); i++)
                this[_keys[i]] = _values[i];
        }
    }
}
