using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NeonSerpent.Core;
using NeonSerpent.Levels;
using NeonSerpent.SaveData;
using NeonSerpent.Utilities;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Campaign level select screen. Loads all LevelData assets from Resources,
    /// builds a button grid showing unlock state and star progress per level,
    /// and passes the selected level to GameManager before loading the Game scene.
    /// </summary>
    public class LevelSelectUI : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private Transform  _levelGridContainer;
        [SerializeField] private GameObject _levelButtonPrefab;

        [Header("Error State")]
        [SerializeField] private TMPro.TMP_Text _noLevelsErrorText;

        [Header("Navigation")]
        [SerializeField] private Button _backButton;

        private void Awake()
        {
            _backButton?.onClick.AddListener(OnBack);
        }

        private void Start()
        {
            BuildGrid();
        }

        private void BuildGrid()
        {
            if (_levelGridContainer == null || _levelButtonPrefab == null) return;

            // Load all LevelData assets from Resources/Levels/
            var levels = Resources.LoadAll<LevelData>("Levels");
            if (levels.Length == 0)
            {
                Debug.LogError("[LevelSelectUI] No LevelData assets found in Resources/Levels/. " +
                    "Run NeonSerpent > Generate Campaign Levels, then move the generated assets " +
                    "into Assets/Resources/Levels/ and re-enter Play mode.");

                // Show an in-scene error message so this failure is visible at runtime,
                // not just in the console. Assign a TMP_Text to _noLevelsErrorText in the Inspector.
                if (_noLevelsErrorText != null)
                {
                    _noLevelsErrorText.gameObject.SetActive(true);
                    _noLevelsErrorText.text =
                        "No campaign levels found.\n" +
                        "Run  NeonSerpent > Generate Campaign Levels\n" +
                        "then move assets to  Resources/Levels/";
                }
                return;
            }

            // Hide the error label if levels were found (it defaults to inactive in the scene).
            if (_noLevelsErrorText != null)
                _noLevelsErrorText.gameObject.SetActive(false);

            // Sort by world then level index
            System.Array.Sort(levels, (a, b) =>
            {
                int worldCmp = a.WorldIndex.CompareTo(b.WorldIndex);
                return worldCmp != 0 ? worldCmp : a.LevelIndex.CompareTo(b.LevelIndex);
            });

            var progress = SaveManager.Instance?.Data?.campaignProgress;
            bool previousUnlocked = true;

            foreach (var level in levels)
            {
                bool isUnlocked = previousUnlocked;
                // Use a composite key to avoid collisions when multiple worlds share the same LevelIndex.
                int progressKey = level.WorldIndex * 100 + level.LevelIndex;
                int stars = (progress != null && progress.ContainsKey(progressKey))
                    ? progress[progressKey]
                    : 0;

                var btnGO = Instantiate(_levelButtonPrefab, _levelGridContainer);
                PopulateButton(btnGO, level, isUnlocked, stars);

                // A level unlocks the next one as soon as it has been completed
                // (key present in progress dict), regardless of star count.
                // Stars are a rating only — not a progression gate.
                previousUnlocked = progress != null && progress.ContainsKey(progressKey);
            }
        }

        private void PopulateButton(GameObject btnGO, LevelData level, bool isUnlocked, int stars)
        {
            var btn = btnGO.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = isUnlocked;
                btn.onClick.AddListener(() => OnLevelSelected(level));
            }

            var nameText = btnGO.transform.Find("LevelNameText")?.GetComponent<TMP_Text>();
            if (nameText != null)
                nameText.text = isUnlocked ? level.LevelName : "LOCKED";

            var starsText = btnGO.transform.Find("StarsText")?.GetComponent<TMP_Text>();
            if (starsText != null)
                starsText.text = new string('★', stars) + new string('☆', 3 - stars);
        }

        private void OnLevelSelected(LevelData level)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SelectedLevel = level;

            SceneLoader.Instance?.LoadScene(
                Constants.SCENE_GAME,
                () => GameManager.Instance?.StartGame(GameMode.Campaign));
        }

        private void OnBack()
        {
            SceneLoader.Instance?.LoadScene(Constants.SCENE_MAIN_MENU);
        }
    }
}
