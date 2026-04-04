// ============================================================
// BuildScript.cs
// Automated AAB build pipeline for NeonSerpent.
//
// INTERACTIVE USE:
//   NeonSerpent → Build AAB
//
// CI / COMMAND LINE:
//   Unity.exe -batchmode -quit -projectPath <path>
//             -executeMethod NeonSerpent.Editor.BuildScript.BuildAAB
//             -logFile build.log
//
// REQUIRED ENVIRONMENT VARIABLES (CI):
//   KEYSTORE_PASS  — password for the keystore file
//   KEY_PASS       — password for the signing key alias
//
// KEYSTORE ALIAS:
//   Alias name is set by KEYSTORE_ALIAS below.
//   Confirm this matches the alias created during `keytool -genkey`.
//   Run:  keytool -list -v -keystore "E:\Keys\neonserpent.keystore"
// ============================================================

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NeonSerpent.Editor
{
    /// <summary>
    /// One-click and CLI-driven AAB build pipeline for NeonSerpent.
    /// Applies all required Android settings via <see cref="AndroidBuildValidator"/>
    /// before invoking the build, then reports success or failure.
    /// </summary>
    public static class BuildScript
    {
        // ─────────────────────────────────────────────────────────────────────
        // CONSTANTS
        // ─────────────────────────────────────────────────────────────────────

        private const string KEYSTORE_PATH  = @"E:\Keys\neonserpent.keystore";
        private const string KEYSTORE_ALIAS = "neonserpent";
        private const string OUTPUT_DIR     = "Builds/Android";
        private const string OUTPUT_FILE    = "NeonSerpent.aab";

        // Environment variable names used in CI pipelines.
        private const string ENV_KEYSTORE_PASS = "KEYSTORE_PASS";
        private const string ENV_KEY_PASS      = "KEY_PASS";

        // ─────────────────────────────────────────────────────────────────────
        // MENU ITEM
        // ─────────────────────────────────────────────────────────────────────

        [MenuItem("NeonSerpent/Build AAB")]
        private static void BuildAABMenu()
        {
            BuildAAB();
        }

        // ─────────────────────────────────────────────────────────────────────
        // MAIN BUILD METHOD  (also the -executeMethod entry point)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Applies Android build settings, configures keystore signing, and
        /// produces a release AAB at <c>Builds/Android/NeonSerpent.aab</c>.
        /// Safe to call from the Unity Editor menu or from CLI via
        /// <c>-executeMethod NeonSerpent.Editor.BuildScript.BuildAAB</c>.
        /// </summary>
        public static void BuildAAB()
        {
            Debug.Log("[BuildScript] ===== NeonSerpent AAB Build Started =====");

            // 1. Apply all required Android player settings.
            AndroidBuildValidator.PrepareAndroidBuild();

            // 2. Resolve keystore credentials.
            if (!TryResolveCredentials(
                    out string keystorePass,
                    out string keyPass))
            {
                // User cancelled the interactive dialog — abort silently.
                Debug.LogWarning("[BuildScript] Build cancelled — keystore credentials not provided.");
                return;
            }

            // 3. Configure keystore signing in PlayerSettings.
            ApplyKeystoreSettings(keystorePass, keyPass);

            // 4. Set output to AAB (not APK).
            EditorUserBuildSettings.buildAppBundle = true;

            // 4a. Lock the active build target group to Android so Unity's build
            //     pipeline does not re-evaluate Player Settings against a different
            //     target group when BuildPlayer is invoked.
            EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Android;

            // 4b. Re-enforce the Input System setting immediately before the build.
            //     Unity 6 can revert 'activeInputHandler' to its cached in-memory
            //     value when BuildPlayerOptions is constructed, undoing the write
            //     that PrepareAndroidBuild() made via AssetDatabase.  Calling this
            //     a second time here guarantees it is the last write before the
            //     build pipeline locks the ProjectSettings asset.
            AndroidBuildValidator.SetInputHandlingToNewSystem();

            // 5. Ensure the output directory exists.
            string outputPath = Path.Combine(OUTPUT_DIR, OUTPUT_FILE);
            Directory.CreateDirectory(OUTPUT_DIR);
            Debug.Log($"[BuildScript] Output path: {Path.GetFullPath(outputPath)}");

            // 6. Collect enabled scenes from EditorBuildSettings.
            string[] scenes = CollectEnabledScenes();
            Debug.Log($"[BuildScript] Including {scenes.Length} scene(s):\n  " +
                      string.Join("\n  ", scenes));

            // 7. Invoke the build.
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes           = scenes,
                locationPathName = outputPath,
                target           = BuildTarget.Android,
                options          = BuildOptions.None,
            };

            Debug.Log("[BuildScript] Invoking BuildPipeline.BuildPlayer...");
            BuildReport  report  = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            // 8. Report result.
            if (summary.result == BuildResult.Succeeded)
            {
                string fullPath = Path.GetFullPath(outputPath);
                string sizeMb   = (summary.totalSize / (1024.0 * 1024.0)).ToString("F1");
                Debug.Log($"[BuildScript] BUILD SUCCEEDED\n" +
                          $"  Output : {fullPath}\n" +
                          $"  Size   : {sizeMb} MB\n" +
                          $"  Time   : {summary.totalTime.TotalSeconds:F1}s");

                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog(
                        "Build Succeeded",
                        $"AAB built successfully.\n\n" +
                        $"Output: {fullPath}\n" +
                        $"Size:   {sizeMb} MB",
                        "Done");
                }
            }
            else
            {
                Debug.LogError($"[BuildScript] BUILD FAILED — result: {summary.result}\n" +
                               $"  Errors: {summary.totalErrors}");

                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog(
                        "Build Failed",
                        $"Build result: {summary.result}\n" +
                        $"Error count: {summary.totalErrors}\n\n" +
                        "Check the Console for details.",
                        "OK");
                }

                // Exit with a non-zero code so CI pipelines detect the failure.
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // CREDENTIALS RESOLUTION
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves keystore and key passwords.
        /// Priority order:
        ///   1. Already set in PlayerSettings (interactive Editor workflow)
        ///   2. Environment variables KEYSTORE_PASS / KEY_PASS (CI / batch mode)
        /// Returns <c>false</c> if credentials cannot be resolved.
        /// </summary>
        private static bool TryResolveCredentials(
            out string keystorePass,
            out string keyPass)
        {
            // 1. Use whatever is already configured in Player Settings.
            keystorePass = PlayerSettings.Android.keystorePass;
            keyPass      = PlayerSettings.Android.keyaliasPass;

            if (!string.IsNullOrEmpty(keystorePass) && !string.IsNullOrEmpty(keyPass))
            {
                Debug.Log("[BuildScript] Keystore credentials loaded from Player Settings.");
                return true;
            }

            // 2. Fall back to environment variables (CI / batch mode).
            string envKeystorePass = Environment.GetEnvironmentVariable(ENV_KEYSTORE_PASS);
            string envKeyPass      = Environment.GetEnvironmentVariable(ENV_KEY_PASS);

            if (!string.IsNullOrEmpty(envKeystorePass) && !string.IsNullOrEmpty(envKeyPass))
            {
                keystorePass = envKeystorePass;
                keyPass      = envKeyPass;
                Debug.Log("[BuildScript] Keystore credentials loaded from environment variables.");
                return true;
            }

            // 3. Neither source has credentials — tell the user what to do.
            if (Application.isBatchMode)
            {
                throw new InvalidOperationException(
                    $"[BuildScript] Keystore credentials not found. " +
                    $"Set '{ENV_KEYSTORE_PASS}' and '{ENV_KEY_PASS}' environment variables before building.");
            }

            EditorUtility.DisplayDialog(
                "Keystore Not Configured",
                "No keystore credentials found.\n\n" +
                "Go to:\n  Edit → Project Settings → Player → Publishing Settings\n\n" +
                "Enable 'Custom Keystore', select E:\\Keys\\neonserpent.keystore, " +
                "and enter the passwords. Then run Build AAB again.",
                "OK");

            return false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // KEYSTORE CONFIGURATION
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Writes keystore path, alias, and passwords into <see cref="PlayerSettings"/>
        /// for the current build. These values are NOT saved to disk — they exist
        /// only for the duration of this Editor session, which avoids accidentally
        /// committing credentials into ProjectSettings.
        /// </summary>
        private static void ApplyKeystoreSettings(string keystorePass, string keyPass)
        {
            if (!File.Exists(KEYSTORE_PATH))
            {
                throw new FileNotFoundException(
                    $"[BuildScript] Keystore not found at '{KEYSTORE_PATH}'. " +
                    "Ensure the keystore is stored at E:\\Keys\\ (outside the repo).",
                    KEYSTORE_PATH);
            }

            PlayerSettings.Android.useCustomKeystore  = true;
            PlayerSettings.Android.keystoreName       = KEYSTORE_PATH;
            PlayerSettings.Android.keystorePass       = keystorePass;
            PlayerSettings.Android.keyaliasName       = KEYSTORE_ALIAS;
            PlayerSettings.Android.keyaliasPass       = keyPass;

            Debug.Log($"[BuildScript] Keystore configured — alias: '{KEYSTORE_ALIAS}', " +
                      $"path: {KEYSTORE_PATH}");
        }

        // ─────────────────────────────────────────────────────────────────────
        // SCENE COLLECTION
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the paths of all enabled scenes listed in
        /// <see cref="EditorBuildSettings.scenes"/>, preserving their order.
        /// Throws if no scenes are configured, because a build with zero scenes
        /// would silently produce a broken AAB.
        /// </summary>
        private static string[] CollectEnabledScenes()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
                throw new InvalidOperationException(
                    "[BuildScript] No enabled scenes found in EditorBuildSettings. " +
                    "Add scenes via File > Build Settings before building.");

            return scenes;
        }
    }
}
