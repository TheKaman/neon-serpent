using System.IO;
using UnityEditor;
using UnityEngine;

namespace NeonSerpent.Editor
{
    /// <summary>
    /// Generates Google Play Store screenshots by entering Play mode and
    /// letting StoreScreenshotCapture navigate scenes automatically.
    /// Menu: NeonSerpent → Store Screenshots
    /// </summary>
    public static class StoreScreenshotTool
    {
        private const string PREF_ACTIVE = "NeonSerpent_ScreenshotMode";
        private const string PREF_SIZES  = "NeonSerpent_Screenshot_Sizes";

        [MenuItem("NeonSerpent/Store Screenshots/Phone (1080×1920)")]
        static void CapturePhone() => StartCapture("Phone");

        [MenuItem("NeonSerpent/Store Screenshots/Tablet 7\" (1200×1920)")]
        static void CaptureTablet7() => StartCapture("Tablet7");

        [MenuItem("NeonSerpent/Store Screenshots/Tablet 10\" (1600×2560)")]
        static void CaptureTablet10() => StartCapture("Tablet10");

        [MenuItem("NeonSerpent/Store Screenshots/All Sizes")]
        static void CaptureAll() => StartCapture("Phone,Tablet7,Tablet10");

        [MenuItem("NeonSerpent/Store Screenshots/Open Output Folder")]
        static void OpenFolder()
        {
            string path = OutputDir();
            Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(path);
        }

        private static void StartCapture(string sizes)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Screenshots] Stop Play mode before generating screenshots.");
                return;
            }

            Directory.CreateDirectory(OutputDir());
            EditorPrefs.SetBool(PREF_ACTIVE, true);
            EditorPrefs.SetString(PREF_SIZES, sizes);
            Debug.Log($"[Screenshots] Sizes: {sizes}  →  {OutputDir()}");
            EditorApplication.EnterPlaymode();
        }

        private static string OutputDir() =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Screenshots", "Store"));
    }
}
