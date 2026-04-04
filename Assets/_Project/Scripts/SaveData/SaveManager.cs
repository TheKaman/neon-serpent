using System;
using System.IO;
using UnityEngine;
using NeonSerpent.Utilities;

namespace NeonSerpent.SaveData
{
    /// <summary>
    /// Handles all save/load operations via JSON serialization to Application.persistentDataPath.
    /// All other systems read/write through SaveManager.Data — never directly to disk.
    /// Lives in Bootstrap scene as a Singleton.
    /// </summary>
    public class SaveManager : Singleton<SaveManager>
    {
        private string SavePath => Path.Combine(Application.persistentDataPath, Constants.SAVE_FILE_NAME);

        public PlayerData Data { get; private set; } = new PlayerData();

        /// <summary>Fired with the new coin total whenever coins are awarded or spent.</summary>
        public event Action<int> OnCoinsChanged;

        protected override void Awake()
        {
            base.Awake();
            Load();
        }

        /// <summary>Persist the current PlayerData to disk.</summary>
        public void Save()
        {
            string json = JsonUtility.ToJson(Data, prettyPrint: true);
            File.WriteAllText(SavePath, json);
        }

        /// <summary>Load PlayerData from disk, or create fresh data if no save exists.</summary>
        public void Load()
        {
            if (File.Exists(SavePath))
            {
                string json = File.ReadAllText(SavePath);
                var loaded = JsonUtility.FromJson<PlayerData>(json);
                if (loaded == null)
                {
                    // Corrupt or empty save file — log so it surfaces in crash reports.
                    Debug.LogWarning($"[SaveManager] Save file at '{SavePath}' could not be parsed. " +
                        "Starting with fresh player data. The corrupt file will be overwritten on next Save().");
                }
                Data = loaded ?? new PlayerData();
            }
            else
            {
                Data = new PlayerData();
            }
        }

        /// <summary>
        /// Add coins to the player's wallet and fire OnCoinsChanged.
        /// Does NOT write to disk — the caller is responsible for calling Save() at a
        /// suitable checkpoint (e.g. game over or level complete) so we avoid synchronous
        /// File.WriteAllText on every single food pick-up.
        /// Safe to call with amount = 0 (no-op).
        /// </summary>
        public void AwardCoins(int amount)
        {
            if (amount <= 0) return;
            Data.coins += amount;
            OnCoinsChanged?.Invoke(Data.coins);
        }

        /// <summary>
        /// Deduct coins from the player's wallet, persist the change, and fire OnCoinsChanged.
        /// Clamps the result to zero — will not produce a negative balance.
        /// Safe to call with amount = 0 (no-op).
        /// </summary>
        public void DeductCoins(int amount)
        {
            if (amount <= 0) return;
            Data.coins = Mathf.Max(0, Data.coins - amount);
            Save();
            OnCoinsChanged?.Invoke(Data.coins);
        }

        /// <summary>
        /// Delete the save file, reset to fresh PlayerData, and clear all one-time hint
        /// PlayerPrefs keys so tutorials and tooltips replay from scratch.
        /// Volume and vibration settings are deliberately left intact.
        /// </summary>
        public void DeleteSave()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
            Data = new PlayerData();

            // Clear one-time hint flags so SwipeHintUI and PowerUpTooltipUI replay
            // for the player as if it were a fresh install.
            PlayerPrefs.DeleteKey("ns_swipe_hint_shown");

            // PowerUpTooltipUI stores one key per PowerUpType using the prefix "ns_tooltip_seen_"
            // followed by the integer value of the enum. Clear all known type indices.
            // PowerUpType enum values: SpeedBoost=0, Shield=1, ScoreMultiplier=2,
            //                          GhostMode=3, ShrinkPill=4, Poison=5
            for (int i = 0; i <= 5; i++)
                PlayerPrefs.DeleteKey($"ns_tooltip_seen_{i}");

            PlayerPrefs.Save();

            // Notify subscribers so displays (e.g. HUD coin counter) update immediately.
            OnCoinsChanged?.Invoke(0);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) Save(); // Auto-save when app backgrounds
        }

        private void OnApplicationQuit()
        {
            Save();
        }
    }
}
