// ============================================================
// WireInspectorRefs.cs
// One-shot editor utility:
//   • Wires GridSystem into FloatingTextSpawner in the Game scene
//   • Adds a FrenzyActivated entry to the SoundLibrary asset
//
// HOW TO USE:
//   NeonSerpent → Wire Inspector References
//   Safe to run multiple times — skips steps that are already done.
// ============================================================

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using NeonSerpent.Audio;
using NeonSerpent.Grid;
using NeonSerpent.UI;

namespace NeonSerpent.Editor
{
    public static class WireInspectorRefs
    {
        private const string GAME_SCENE_PATH    = "Assets/_Project/Scenes/Game.unity";
        private const string SOUND_LIBRARY_PATH = "Assets/_Project/ScriptableObjects/SoundLibrary.asset";

        [MenuItem("NeonSerpent/Wire Inspector References")]
        public static void Wire()
        {
            int stepsDone = 0;

            stepsDone += WireSoundLibrary();
            stepsDone += WireFloatingTextSpawner();

            if (stepsDone == 0)
                EditorUtility.DisplayDialog("Wire Inspector References",
                    "Nothing to do — all references are already wired.", "OK");
            else
                EditorUtility.DisplayDialog("Wire Inspector References",
                    $"{stepsDone} step(s) completed successfully.\n\n" +
                    "Remember to assign an AudioClip to the FrenzyActivated entry\n" +
                    "in the SoundLibrary asset (Assets/_Project/ScriptableObjects/).",
                    "Done");
        }

        // ─────────────────────────────────────────────────────────────────────
        // 1. SoundLibrary — add FrenzyActivated entry if missing
        // ─────────────────────────────────────────────────────────────────────

        private static int WireSoundLibrary()
        {
            var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(SOUND_LIBRARY_PATH);
            if (library == null)
            {
                Debug.LogError($"[WireInspectorRefs] SoundLibrary not found at {SOUND_LIBRARY_PATH}");
                return 0;
            }

            var so = new SerializedObject(library);
            var entries = so.FindProperty("_entries");

            // Check if FrenzyActivated already exists.
            int frenzyEnumValue = (int)SoundEvent.FrenzyActivated;
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("Event").enumValueIndex == frenzyEnumValue)
                {
                    Debug.Log("[WireInspectorRefs] SoundLibrary already has FrenzyActivated entry — skipping.");
                    return 0;
                }
            }

            // Append new entry.
            entries.arraySize++;
            var newEntry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            newEntry.FindPropertyRelative("Event").enumValueIndex = frenzyEnumValue;
            newEntry.FindPropertyRelative("Clip").objectReferenceValue = null; // clip assigned manually

            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            Debug.Log("[WireInspectorRefs] Added FrenzyActivated entry to SoundLibrary (clip slot is empty — assign in Inspector).");
            return 1;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 2. Game scene — wire GridSystem into FloatingTextSpawner._grid
        // ─────────────────────────────────────────────────────────────────────

        private static int WireFloatingTextSpawner()
        {
            // Remember what scene is currently open so we can restore it.
            string originalScenePath = SceneManager.GetActiveScene().path;
            bool sceneWasAlreadyOpen = originalScenePath == GAME_SCENE_PATH;

            Scene gameScene;
            if (sceneWasAlreadyOpen)
            {
                gameScene = SceneManager.GetActiveScene();
            }
            else
            {
                gameScene = EditorSceneManager.OpenScene(GAME_SCENE_PATH, OpenSceneMode.Additive);
            }

            FloatingTextSpawner spawner = null;
            GridSystem grid = null;

            foreach (var root in gameScene.GetRootGameObjects())
            {
                if (spawner == null) spawner = root.GetComponentInChildren<FloatingTextSpawner>(true);
                if (grid    == null) grid    = root.GetComponentInChildren<GridSystem>(true);
            }

            if (spawner == null)
            {
                Debug.LogError("[WireInspectorRefs] FloatingTextSpawner not found in Game scene.");
                CloseIfAdditive(gameScene, sceneWasAlreadyOpen);
                return 0;
            }

            if (grid == null)
            {
                Debug.LogError("[WireInspectorRefs] GridSystem not found in Game scene.");
                CloseIfAdditive(gameScene, sceneWasAlreadyOpen);
                return 0;
            }

            // Check if already wired.
            var so = new SerializedObject(spawner);
            var gridProp = so.FindProperty("_grid");

            if (gridProp.objectReferenceValue != null)
            {
                Debug.Log("[WireInspectorRefs] FloatingTextSpawner._grid already wired — skipping.");
                CloseIfAdditive(gameScene, sceneWasAlreadyOpen);
                return 0;
            }

            gridProp.objectReferenceValue = grid;
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);

            Debug.Log("[WireInspectorRefs] Wired GridSystem into FloatingTextSpawner._grid in Game scene.");

            CloseIfAdditive(gameScene, sceneWasAlreadyOpen);
            return 1;
        }

        private static void CloseIfAdditive(Scene scene, bool wasAlreadyOpen)
        {
            if (!wasAlreadyOpen)
                EditorSceneManager.CloseScene(scene, true);
        }
    }
}
