// SoundLibrarySetup.cs
// Runs automatically after every Unity compilation (no user action needed).
// Also exposed as a menu item for manual triggering.
//
// Finds the SoundLibrary ScriptableObject asset and populates all 14 entries
// with the correct SoundEvent int values and AudioClips from Assets/_Project/Audio/.
//
// Uses raw int values (0-13) for the Event field to avoid any dependency on the
// NeonSerpent.Audio namespace, which prevents the circular-resolve compile issue.

using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility that assigns AudioClip references to the SoundLibrary ScriptableObject.
/// Runs automatically on every domain reload via [InitializeOnLoad]. The static constructor
/// calls AssignSoundLibraryClips() directly rather than via EditorApplication.delayCall,
/// which avoids delegate-drop issues when Unity recompiles mid-frame.
/// Also exposed via NeonSerpent > Assign Audio Clips for manual triggering.
/// </summary>
[InitializeOnLoad]
public static class SoundLibrarySetup
{
    private const string LIBRARY_PATH = "Assets/_Project/ScriptableObjects/SoundLibrary.asset";

    // Static constructor — called by [InitializeOnLoad] after every compile/domain reload.
    // We call directly here instead of via delayCall so the work happens on the same reload
    // cycle and cannot be dropped by a subsequent recompile.
    static SoundLibrarySetup()
    {
        AssignSoundLibraryClips();
    }

    /// <summary>
    /// Manually re-run the clip assignment. Useful if audio files were imported after the
    /// last compile, or if the automatic run silently failed because clips weren't yet on disk.
    /// </summary>
    [MenuItem("NeonSerpent/Assign Audio Clips")]
    public static void AssignSoundLibraryClipsMenu()
    {
        AssignSoundLibraryClips();
        Debug.Log("[SoundLibrary] Manual assign complete — check above for any missing-clip warnings.");
    }

    /// <summary>
    /// Loads the SoundLibrary asset (creating it if absent) and assigns all 14 AudioClip
    /// entries via SerializedObject so changes are recorded by Unity's undo/asset system.
    /// Safe to call multiple times — subsequent calls overwrite the same array indices.
    /// </summary>
    public static void AssignSoundLibraryClips()
    {
        // Ensure the ScriptableObjects folder exists before trying to load or create the asset.
        if (!AssetDatabase.IsValidFolder("Assets/_Project/ScriptableObjects"))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "ScriptableObjects");
            AssetDatabase.SaveAssets();
        }

        // Load or create the SoundLibrary asset.
        // We use the string-form of CreateInstance so this editor script compiles cleanly
        // even if SoundLibrary's namespace changes — the type lookup is done at runtime.
        var lib = AssetDatabase.LoadAssetAtPath<ScriptableObject>(LIBRARY_PATH);
        if (lib == null)
        {
            // Asset doesn't exist yet — create it so NeonSerpentSetup doesn't need to run first.
            // Try fully-qualified name first, then the short name as a fallback.
            lib = ScriptableObject.CreateInstance("NeonSerpent.Audio.SoundLibrary") as ScriptableObject
               ?? ScriptableObject.CreateInstance("SoundLibrary") as ScriptableObject;

            if (lib == null)
            {
                // SoundLibrary type is not yet compiled (first-ever compile on a fresh project).
                // Skip silently — the static constructor will fire again on the next compile.
                return;
            }
            AssetDatabase.CreateAsset(lib, LIBRARY_PATH);
            AssetDatabase.SaveAssets();
            Debug.Log("[SoundLibrary] SoundLibrary.asset created at " + LIBRARY_PATH);
        }

        // Each entry: (eventIntValue, audioFileName)
        // Order must match the SoundEvent enum in AudioManager.cs exactly:
        //   0=EatFood, 1=EatBonus, 2=EatPoison, 3=PowerUpCollect, 4=PowerUpExpire,
        //   5=Death, 6=LevelComplete, 7=UIClick, 8=UIBack, 9=ShieldAbsorb, 10=Combo,
        //   11=PoisonExpired, 12=FrenzyActivated, 13=TimerTick
        var entries = new (int eventValue, string fileName)[]
        {
            (0,  "EatFood.wav"),
            (1,  "EatBonus.wav"),
            (2,  "EatPoison.wav"),
            (3,  "PowerUpCollect.wav"),
            (4,  "PowerUpExpire.wav"),
            (5,  "Death.wav"),
            (6,  "LevelComplete.wav"),
            (7,  "UIClick.wav"),
            (8,  "UIBack.wav"),
            (9,  "ShieldAbsorb.wav"),
            (10, "Combo.wav"),
            (11, "PoisonExpired.ogg"),
            (12, "FrenzyActivated.ogg"),
            (13, "TimerTick.ogg"),
        };

        var so   = new SerializedObject(lib);
        var prop = so.FindProperty("_entries");
        if (prop == null)
        {
            // '_entries' not found — can happen transiently on the first domain reload after
            // SoundLibrary.cs moved to a new assembly (e.g. after NeonSerpent.Runtime.asmdef
            // was added). The asset's serialized data is intact; the property path resolves
            // itself on the next reload once all assemblies have fully compiled.
            // Downgraded from LogError to LogWarning so it does not appear as a red error.
            Debug.LogWarning("[SoundLibrary] '_entries' property not found on SoundLibrary asset. " +
                             "This usually resolves automatically on the next domain reload. " +
                             "If the warning persists, ensure the backing field in SoundLibrary.cs " +
                             "is [SerializeField] private SoundEntry[] _entries;");
            return;
        }

        prop.arraySize = entries.Length;

        int assignedCount = 0;
        for (int i = 0; i < entries.Length; i++)
        {
            var element   = prop.GetArrayElementAtIndex(i);
            var eventProp = element.FindPropertyRelative("Event");
            var clipProp  = element.FindPropertyRelative("Clip");

            if (eventProp == null || clipProp == null)
            {
                // Field names 'Event' or 'Clip' not found — same transient assembly-change
                // situation as above. Silently skip this run; next domain reload will fix it.
                Debug.LogWarning($"[SoundLibrary] SoundEntry at index {i} is missing 'Event' or 'Clip' " +
                               $"property. This resolves automatically on the next domain reload.");
                return;
            }

            eventProp.intValue = entries[i].eventValue;

            string clipPath = $"Assets/_Project/Audio/{entries[i].fileName}";
            var    clip     = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);

            clipProp.objectReferenceValue = clip;

            if (clip != null)
                assignedCount++;
            else
                Debug.LogWarning($"[SoundLibrary] AudioClip not found at '{clipPath}'. " +
                                 $"Import the file then run NeonSerpent > Assign Audio Clips.");
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        // ── Music clips (optional — no warning if missing, music is not required for gameplay) ──
        AssignMusicClip(so, "MenuMusic", "MenuMusic.wav");
        AssignMusicClip(so, "GameMusic", "GameMusic.wav");

        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();

        if (assignedCount > 0)
            Debug.Log($"[SoundLibrary] {assignedCount}/{entries.Length} SFX clips assigned.");
    }

    private static void AssignMusicClip(SerializedObject so, string propertyName, string fileName)
    {
        var prop = so.FindProperty(propertyName);
        if (prop == null) return;   // Property not found — SoundLibrary may not have recompiled yet

        string clipPath = $"Assets/_Project/Audio/{fileName}";
        var    clip     = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);

        // Only assign if the clip is found — leave the field alone if it's missing so a
        // previously-assigned custom file is not cleared by a re-run.
        if (clip != null)
        {
            prop.objectReferenceValue = clip;
            Debug.Log($"[SoundLibrary] Music clip '{fileName}' assigned.");
        }
        // Silently skip if missing — music files are optional.
    }
}
