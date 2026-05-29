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
        [Tooltip("Optional. A prefab with a TMP_Text used as a per-world section header " +
                 "(e.g. \"WORLD 1 - NEON CITY\"). If assigned, one is inserted before the " +
                 "first level of each world. If left null, levels render as a flat list as " +
                 "before — no header is shown. For correct layout in a GridLayoutGroup the " +
                 "header should span a full row (e.g. via LayoutElement / a row-spanning cell).")]
        [SerializeField] private GameObject _worldHeaderPrefab;

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
            int  previousWorldIndex = int.MinValue; // guarantees a header before the first world

            foreach (var level in levels)
            {
                // Insert a world header whenever we cross into a new world. Levels are already
                // sorted by WorldIndex then LevelIndex above, so a change in WorldIndex marks
                // the first level of a world (M-7: previously all 20 levels were an
                // undifferentiated list with no world separation).
                if (level.WorldIndex != previousWorldIndex)
                {
                    InsertWorldHeader(level);
                    previousWorldIndex = level.WorldIndex;
                }

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

        /// <summary>
        /// Instantiates a world section header before the first level of a world. No-op when
        /// no header prefab is assigned, so the level list still renders without it.
        /// Prefers a child named "WorldNameText", then any TMP_Text on the prefab.
        /// </summary>
        private void InsertWorldHeader(LevelData level)
        {
            if (_worldHeaderPrefab == null || _levelGridContainer == null) return;

            var headerGO = Instantiate(_worldHeaderPrefab, _levelGridContainer);

            var label = headerGO.transform.Find("WorldNameText")?.GetComponent<TMP_Text>();
            if (label == null)
            {
                label = headerGO.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    Debug.LogWarning($"[LevelSelectUI] World header prefab has no child named 'WorldNameText' — " +
                                     $"falling back to first TMP_Text found ('{label.name}'). Rename the child to avoid this.");
            }

            if (label != null)
            {
                // Fall back to "WORLD N" when the asset has no WorldName so the header is never blank.
                label.text = !string.IsNullOrEmpty(level.WorldName)
                    ? level.WorldName
                    : $"WORLD {level.WorldIndex + 1}";
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
