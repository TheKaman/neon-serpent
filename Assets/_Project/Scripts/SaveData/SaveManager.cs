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
                Data = JsonUtility.FromJson<PlayerData>(json) ?? new PlayerData();
            }
            else
            {
                Data = new PlayerData();
            }
        }

        /// <summary>Delete save file and reset to fresh PlayerData (for testing / "delete save" option).</summary>
        public void DeleteSave()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
            Data = new PlayerData();
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
