using UnityEngine;
using UnityEngine.UI;
using NeonSerpent.Core;
using NeonSerpent.Utilities;
using NeonSerpent.Ads;

namespace NeonSerpent.UI
{
    /// <summary>
    /// Main menu screen. Wires button clicks to game mode selection and scene navigation.
    /// Buttons are found by name if not assigned via Inspector.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        private void Start()
        {
            // Find buttons by name (set up by NeonSerpentSetup editor script)
            WireButton("ClassicBtn",     () => StartMode(GameMode.ClassicEndless));
            WireButton("TimeAttackBtn",  () => StartMode(GameMode.TimeAttack));
            WireButton("CampaignBtn",    () => StartMode(GameMode.Campaign));
            WireButton("LeaderboardBtn", () => SceneLoader.Instance?.LoadScene(Constants.SCENE_LEADERBOARD));
        }

        private void OnEnable()
        {
            AdManager.Instance?.ShowBanner();
        }

        private void OnDisable()
        {
            AdManager.Instance?.HideBanner();
        }

        private void WireButton(string btnName, UnityEngine.Events.UnityAction action)
        {
            // Search in the canvas children
            var btn = GetComponentInParent<Canvas>()?.GetComponentInChildren<Transform>()
                      ?.Find(btnName)?.GetComponent<Button>();

            if (btn == null)
            {
                // Fallback: search entire scene
                var allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
                foreach (var b in allButtons)
                    if (b.gameObject.name == btnName) { btn = b; break; }
            }

            btn?.onClick.AddListener(action);
        }

        private void StartMode(GameMode mode)
        {
            SceneLoader.Instance?.LoadScene(Constants.SCENE_GAME,
                () => GameManager.Instance?.StartGame(mode));
        }
    }
}
