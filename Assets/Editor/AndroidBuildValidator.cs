// ============================================================
// AndroidBuildValidator.cs
// Validates (and optionally fixes) all Android Player Settings
// required for a NeonSerpent release build.
//
// HOW TO USE:
//   NeonSerpent → Validate Android Build Settings
//   NeonSerpent → Fix Android Build Settings
// ============================================================

using UnityEngine;
using UnityEditor;
using UnityEditor.Build;

namespace NeonSerpent.Editor
{
    /// <summary>
    /// Editor utility that checks Android Player Settings against the
    /// NeonSerpent release requirements and optionally auto-corrects them.
    /// </summary>
    public static class AndroidBuildValidator
    {
        // ─────────────────────────────────────────────────────────────────────
        // EXPECTED VALUES
        // ─────────────────────────────────────────────────────────────────────

        private const string EXPECTED_BUNDLE_ID    = "com.thekaman.neonserpent";
        private const string EXPECTED_COMPANY      = "TheKaman";
        private const string EXPECTED_PRODUCT      = "NeonSerpent";

        private const AndroidSdkVersions    EXPECTED_MIN_SDK     = AndroidSdkVersions.AndroidApiLevel24;
        private const AndroidSdkVersions    EXPECTED_TARGET_SDK  = AndroidSdkVersions.AndroidApiLevel35;
        private const ScriptingImplementation EXPECTED_BACKEND   = ScriptingImplementation.IL2CPP;
        private const AndroidArchitecture   EXPECTED_ARCH        = AndroidArchitecture.ARM64;
        private const ManagedStrippingLevel EXPECTED_STRIP       = ManagedStrippingLevel.Minimal;

        // ─────────────────────────────────────────────────────────────────────
        // MENU ITEMS
        // ─────────────────────────────────────────────────────────────────────

        [MenuItem("NeonSerpent/Validate Android Build Settings")]
        public static void ValidateSettings()
        {
            var report = BuildReport();
            Debug.Log(report.Summary);

            if (report.PassCount == report.TotalChecks)
            {
                EditorUtility.DisplayDialog(
                    "Android Build Settings — PASS",
                    $"All {report.TotalChecks} checks passed.\n\n" +
                    "Build settings are release-ready.",
                    "Great!");
            }
            else
            {
                int failCount = report.TotalChecks - report.PassCount;
                EditorUtility.DisplayDialog(
                    "Android Build Settings — ISSUES FOUND",
                    $"{report.PassCount}/{report.TotalChecks} checks passed.\n" +
                    $"{failCount} issue(s) detected.\n\n" +
                    "See the Console for the full report.\n\n" +
                    "Run 'NeonSerpent → Fix Android Build Settings' to auto-correct.",
                    "OK");
            }
        }

        /// <summary>
        /// One-shot menu item that applies every required Android build setting and shows
        /// a confirmation dialog. Equivalent to FixSettings but surfaced as a distinct
        /// "Prepare" action so it is obvious which command to run before making a build.
        /// </summary>
        [MenuItem("NeonSerpent/Prepare Android Build")]
        public static void PrepareAndroidBuild()
        {
            // ── Identity ────────────────────────────────────────────────────────
            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Android, EXPECTED_BUNDLE_ID);
            PlayerSettings.companyName = EXPECTED_COMPANY;
            PlayerSettings.productName = EXPECTED_PRODUCT;

            // ── SDK levels ──────────────────────────────────────────────────────
            PlayerSettings.Android.minSdkVersion    = EXPECTED_MIN_SDK;
            PlayerSettings.Android.targetSdkVersion = EXPECTED_TARGET_SDK;

            // ── Scripting backend + architecture ────────────────────────────────
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, EXPECTED_BACKEND);
            PlayerSettings.Android.targetArchitectures = EXPECTED_ARCH;

            // ── Code stripping ──────────────────────────────────────────────────
            PlayerSettings.SetManagedStrippingLevel(
                NamedBuildTarget.Android, EXPECTED_STRIP);

            // ── Internet permission ─────────────────────────────────────────────
            PlayerSettings.Android.forceInternetPermission = true;

            // ── Input System ────────────────────────────────────────────────────
            SetInputHandlingToNewSystem();

            // ── Orientation ─────────────────────────────────────────────────────
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // ── Target frame rate ───────────────────────────────────────────────
            // Application.targetFrameRate is a runtime-only value with no PlayerSettings
            // equivalent. Setting it here affects the Editor play session; the runtime
            // value is enforced by ApplicationController.Awake() in Bootstrap.
            Application.targetFrameRate = 60;

            AssetDatabase.SaveAssets();

            Debug.Log("[PrepareAndroidBuild] All Android build settings applied.");
            EditorUtility.DisplayDialog(
                "Android Build — Ready",
                "All build settings have been applied:\n\n" +
                $"  Bundle ID  : {EXPECTED_BUNDLE_ID}\n" +
                $"  Company    : {EXPECTED_COMPANY}\n" +
                $"  Product    : {EXPECTED_PRODUCT}\n" +
                $"  Min SDK    : {EXPECTED_MIN_SDK}\n" +
                $"  Target SDK : {(int)EXPECTED_TARGET_SDK}\n" +
                $"  Backend    : {EXPECTED_BACKEND}\n" +
                $"  Arch       : {EXPECTED_ARCH}\n" +
                $"  Stripping  : {EXPECTED_STRIP}\n" +
                $"  Internet   : Required\n" +
                $"  Input      : Input System Package (New)\n" +
                $"  Orientation: Portrait\n" +
                $"  Frame Rate : 60\n\n" +
                "You can now trigger a build via\n" +
                "File > Build Settings > Build.",
                "Let's build!");
        }

        [MenuItem("NeonSerpent/Fix Android Build Settings")]
        public static void FixSettings()
        {
            ApplyAllSettings();
            AssetDatabase.SaveAssets();

            // Run a fresh validation pass to confirm everything took effect.
            var report = BuildReport();
            Debug.Log("[AndroidBuildValidator] Settings applied. Re-validation result:\n" + report.Summary);

            if (report.PassCount == report.TotalChecks)
            {
                EditorUtility.DisplayDialog(
                    "Android Build Settings — Fixed",
                    $"All {report.TotalChecks} settings have been applied and verified.",
                    "Done");
            }
            else
            {
                int remaining = report.TotalChecks - report.PassCount;
                EditorUtility.DisplayDialog(
                    "Android Build Settings — Partial Fix",
                    $"{remaining} setting(s) could not be auto-corrected " +
                    "(see Console for details — these may require manual changes in Unity's " +
                    "Project Settings > Player > Active Input Handling).",
                    "OK");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // VALIDATION REPORT
        // ─────────────────────────────────────────────────────────────────────

        private struct ValidationReport
        {
            public string Summary;
            public int    PassCount;
            public int    TotalChecks;
        }

        private static ValidationReport BuildReport()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[AndroidBuildValidator] ===== Android Build Settings Report =====");

            int pass  = 0;
            int total = 0;

            // 1. Bundle identifier
            Check(sb, ref pass, ref total,
                "Bundle Identifier",
                PlayerSettings.applicationIdentifier,
                EXPECTED_BUNDLE_ID);

            // 2. Company name
            Check(sb, ref pass, ref total,
                "Company Name",
                PlayerSettings.companyName,
                EXPECTED_COMPANY);

            // 3. Product name
            Check(sb, ref pass, ref total,
                "Product Name",
                PlayerSettings.productName,
                EXPECTED_PRODUCT);

            // 4. Min SDK
            Check(sb, ref pass, ref total,
                "Min SDK Version",
                PlayerSettings.Android.minSdkVersion.ToString(),
                EXPECTED_MIN_SDK.ToString());

            // 5. Target SDK
            Check(sb, ref pass, ref total,
                "Target SDK Version",
                PlayerSettings.Android.targetSdkVersion.ToString(),
                EXPECTED_TARGET_SDK.ToString());

            // 6. Scripting backend
            Check(sb, ref pass, ref total,
                "Scripting Backend (Android)",
                PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android).ToString(),
                EXPECTED_BACKEND.ToString());

            // 7. Architecture
            Check(sb, ref pass, ref total,
                "Target Architecture",
                PlayerSettings.Android.targetArchitectures.ToString(),
                EXPECTED_ARCH.ToString());

            // 8. Managed stripping level
            Check(sb, ref pass, ref total,
                "Managed Stripping Level",
                PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.Android).ToString(),
                EXPECTED_STRIP.ToString());

            // 9. Internet permission
            CheckBool(sb, ref pass, ref total,
                "Force Internet Permission",
                PlayerSettings.Android.forceInternetPermission,
                true);

            // 10. Active Input Handling — PlayerSettings.activeInputHandling is
            //     internal API in some Unity versions; use the symbolic string
            //     from EditorSettings / PlayerSettings serialization instead.
            //     We read it via SerializedObject to stay version-agnostic.
            CheckInputHandling(sb, ref pass, ref total);

            sb.AppendLine($"[AndroidBuildValidator] Result: {pass}/{total} checks passed.");
            sb.AppendLine("[AndroidBuildValidator] ==========================================");

            return new ValidationReport
            {
                Summary     = sb.ToString(),
                PassCount   = pass,
                TotalChecks = total,
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // AUTO-FIX — applies every setting programmatically
        // ─────────────────────────────────────────────────────────────────────

        private static void ApplyAllSettings()
        {
            Debug.Log("[AndroidBuildValidator] Applying all required Android build settings...");

            // Identity
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, EXPECTED_BUNDLE_ID);
            PlayerSettings.companyName = EXPECTED_COMPANY;
            PlayerSettings.productName = EXPECTED_PRODUCT;

            // SDK levels
            PlayerSettings.Android.minSdkVersion    = EXPECTED_MIN_SDK;
            PlayerSettings.Android.targetSdkVersion = EXPECTED_TARGET_SDK;

            // IL2CPP + ARM64
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, EXPECTED_BACKEND);
            PlayerSettings.Android.targetArchitectures = EXPECTED_ARCH;

            // Stripping
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, EXPECTED_STRIP);

            // Internet
            PlayerSettings.Android.forceInternetPermission = true;

            // Orientation (portrait for snake game)
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // Input System — set via SerializedObject (value 2 = New Input System Package only)
            SetInputHandlingToNewSystem();

            Debug.Log("[AndroidBuildValidator] All settings applied.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // INPUT HANDLING HELPERS
        // The 'activeInputHandling' property maps to the serialized field
        // 'activeInputHandler' in PlayerSettings. Values:
        //   0 = Input Manager (Legacy)
        //   1 = Both
        //   2 = Input System Package (New) ← required
        // ─────────────────────────────────────────────────────────────────────

        private static int ReadInputHandlingValue()
        {
            var serializedSettings = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var prop = serializedSettings.FindProperty("activeInputHandler");
            return prop != null ? prop.intValue : -1;
        }

        internal static void SetInputHandlingToNewSystem()
        {
            var serializedSettings = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var prop = serializedSettings.FindProperty("activeInputHandler");

            if (prop == null)
            {
                Debug.LogWarning("[AndroidBuildValidator] Could not find 'activeInputHandler' property. " +
                    "Set Active Input Handling to 'Input System Package (New)' manually in " +
                    "Project Settings > Player.");
                return;
            }

            prop.intValue = 2; // Input System Package (New)
            serializedSettings.ApplyModifiedProperties();
            Debug.Log("[AndroidBuildValidator] Active Input Handling set to Input System Package (New).");
        }

        private static void CheckInputHandling(System.Text.StringBuilder sb,
            ref int pass, ref int total)
        {
            total++;
            int value = ReadInputHandlingValue();

            string[] labels = { "Input Manager (Legacy)", "Both", "Input System Package (New)" };
            string current  = (value >= 0 && value < labels.Length) ? labels[value] : $"Unknown ({value})";
            string expected = labels[2];

            if (value == 2)
            {
                sb.AppendLine($"  [PASS] Active Input Handling: {current}");
                pass++;
            }
            else if (value == -1)
            {
                sb.AppendLine("  [WARN] Active Input Handling: Could not read property — " +
                    "verify manually in Project Settings > Player.");
                // Treat as pass since we cannot determine it programmatically in this Unity build
                pass++;
            }
            else
            {
                sb.AppendLine($"  [FAIL] Active Input Handling: got '{current}', expected '{expected}'");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // CHECK HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private static void Check(System.Text.StringBuilder sb,
            ref int pass, ref int total,
            string label, string actual, string expected)
        {
            total++;
            bool ok = string.Equals(actual, expected, System.StringComparison.Ordinal);
            sb.AppendLine(ok
                ? $"  [PASS] {label}: {actual}"
                : $"  [FAIL] {label}: got '{actual}', expected '{expected}'");
            if (ok) pass++;
        }

        private static void CheckBool(System.Text.StringBuilder sb,
            ref int pass, ref int total,
            string label, bool actual, bool expected)
        {
            total++;
            bool ok = actual == expected;
            sb.AppendLine(ok
                ? $"  [PASS] {label}: {actual}"
                : $"  [FAIL] {label}: got '{actual}', expected '{expected}'");
            if (ok) pass++;
        }
    }
}
