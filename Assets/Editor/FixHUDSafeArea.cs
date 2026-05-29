using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace NeonSerpent.Editor
{
/// <summary>
/// Reparents gameplay HUD elements into the HUDSafeArea (SafeAreaPanel) so they
/// are automatically inset from notches, rounded corners, and system bars.
/// Full-screen overlay panels (GameOver, Pause, LevelComplete, Countdown, Picker)
/// remain on the canvas so they can cover the entire screen including under the notch.
/// Run via: NeonSerpent > Fix HUD Safe Area
/// </summary>
public static class FixHUDSafeArea
{
    // These are full-screen overlays — must stay on the canvas, NOT in safe area
    private static readonly string[] OverlayPanelNames = new[]
    {
        "GameOverPanel",
        "PausePanel",
        "LevelCompletePanel",
        "CountdownPanel",
        "EditorModePickerPanel",
        "PowerUpSlotContainer",
    };

    // These gameplay HUD elements should live inside the safe area
    private static readonly string[] HudElementNames = new[]
    {
        "ScoreText",
        "MultiplierText",
        "TimerPanel",
        "CoinText",
        "PersonalBestText",
        "PauseBtn",
        "ShieldIndicator",
        "FrenzyIndicator",
        "PowerUpTooltipPanel",
        "SwipeHintPanel",
    };

    [MenuItem("NeonSerpent/Fix HUD Safe Area")]
    public static void Fix()
    {
        const string scenePath = "Assets/_Project/Scenes/Game.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Find HUDCanvas
        var hudCanvas = GameObject.Find("HUDCanvas");
        if (hudCanvas == null)
        {
            Debug.LogError("[FixHUDSafeArea] HUDCanvas not found in Game scene.");
            return;
        }

        // Find HUDSafeArea inside the canvas
        var safeAreaTF = hudCanvas.transform.Find("HUDSafeArea");
        if (safeAreaTF == null)
        {
            Debug.LogError("[FixHUDSafeArea] HUDSafeArea not found under HUDCanvas.");
            return;
        }

        int moved = 0;
        foreach (string name in HudElementNames)
        {
            var tf = hudCanvas.transform.Find(name);
            if (tf == null)
            {
                Debug.LogWarning($"[FixHUDSafeArea] '{name}' not found directly under HUDCanvas — skipping.");
                continue;
            }

            // Reparent into HUDSafeArea, preserving world-space anchors
            tf.SetParent(safeAreaTF, false);
            moved++;
            Debug.Log($"[FixHUDSafeArea] Moved '{name}' into HUDSafeArea.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log($"[FixHUDSafeArea] Done — {moved} elements moved into safe area. Scene saved.");
    }
}
} // namespace NeonSerpent.Editor
