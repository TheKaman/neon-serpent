using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using NeonSerpent.UI;

namespace NeonSerpent.Editor
{
/// <summary>
/// One-shot editor fix: wires the 6 MainMenuUI button SerializeFields that were
/// not set when the refactor replaced FindObjectsByType with [SerializeField] refs.
/// Run via: NeonSerpent > Fix Main Menu Buttons
/// </summary>
public static class FixMainMenuButtons
{
    [MenuItem("NeonSerpent/Fix Main Menu Buttons")]
    public static void Fix()
    {
        const string scenePath = "Assets/_Project/Scenes/MainMenu.unity";

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var menuUI = Object.FindFirstObjectByType<MainMenuUI>();
        if (menuUI == null)
        {
            Debug.LogError("[Fix] MainMenuUI not found in MainMenu scene.");
            return;
        }

        var so = new SerializedObject(menuUI);

        Wire(so, "_classicBtn",     "ClassicBtn");
        Wire(so, "_timeAttackBtn",  "TimeAttackBtn");
        Wire(so, "_campaignBtn",    "CampaignBtn");
        Wire(so, "_leaderboardBtn", "LeaderboardBtn");
        Wire(so, "_shopBtn",        "ShopBtn");
        Wire(so, "_settingsBtn",    "SettingsBtn");

        so.ApplyModifiedProperties();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[Fix] MainMenu buttons wired successfully. Scene saved.");
    }

    private static void Wire(SerializedObject so, string fieldName, string goName)
    {
        var prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogWarning($"[Fix] Field '{fieldName}' not found on MainMenuUI.");
            return;
        }

        // Search entire scene hierarchy for a GameObject with this name
        var all = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        Button found = null;
        foreach (var btn in all)
        {
            if (btn.gameObject.name == goName)
            {
                found = btn;
                break;
            }
        }

        if (found == null)
        {
            Debug.LogWarning($"[Fix] Button GameObject '{goName}' not found in scene.");
            return;
        }

        prop.objectReferenceValue = found;
        Debug.Log($"[Fix] Wired {fieldName} → {goName}");
    }
}
} // namespace NeonSerpent.Editor
