// ============================================================
// AudioImportSettings.cs
// AssetPostprocessor that automatically applies optimal Android
// audio import settings to any file dropped into:
//   Assets/_Project/Audio/
//
// Rules applied on import:
//   Music files  (name contains "music", "bgm", or "loop"):
//     - Compression  : Vorbis, quality 70 %
//     - Load Type    : Streaming
//     - Force Mono   : false
//     - Sample Rate  : Override to 44100 Hz
//
//   SFX files (everything else):
//     - Compression  : ADPCM
//     - Load Type    : DecompressOnLoad
//     - Force Mono   : true
//     - Sample Rate  : Override to 44100 Hz
//
// No manual steps required — drop audio files and they are
// configured automatically.
// ============================================================

using UnityEngine;
using UnityEditor;

namespace NeonSerpent.Editor
{
    /// <summary>
    /// Automatically applies platform-appropriate audio import settings
    /// to any audio asset placed under <c>Assets/_Project/Audio/</c>.
    /// </summary>
    public class AudioImportSettings : AssetPostprocessor
    {
        private const string AUDIO_FOLDER   = "Assets/_Project/Audio/";
        private const int    TARGET_SAMPLE_RATE = 44100;
        private const float  MUSIC_VORBIS_QUALITY = 0.70f; // 0–1 range; 0.70 = 70 %

        // ─────────────────────────────────────────────────────────
        // AssetPostprocessor callback — fires before the asset is
        // written to disk so our settings take effect on first import.
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Called by Unity before importing an audio asset.
        /// Applies mobile-optimised settings when the asset lives under
        /// <c>Assets/_Project/Audio/</c>.
        /// </summary>
        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(AUDIO_FOLDER)) return;

            var importer = (AudioImporter)assetImporter;
            bool isMusic = IsMusic(assetPath);

            // ── Shared settings ──────────────────────────────────
            importer.forceToMono = !isMusic; // mono for SFX, stereo for music

            // ── Platform-independent default settings ────────────
            var defaultSettings = importer.defaultSampleSettings;
            ApplySettings(ref defaultSettings, isMusic);
            importer.defaultSampleSettings = defaultSettings;

            // ── Android-specific override ─────────────────────────
            var androidSettings = importer.GetOverrideSampleSettings("Android");
            ApplySettings(ref androidSettings, isMusic);
            importer.SetOverrideSampleSettings("Android", androidSettings);

            Debug.Log($"[AudioImport] {(isMusic ? "Music" : "SFX")} settings applied to: {assetPath}");
        }

        // ─────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true when the filename (lowercased) contains "music", "bgm", or "loop".
        /// </summary>
        private static bool IsMusic(string path)
        {
            string lower = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            return lower.Contains("music") || lower.Contains("bgm") || lower.Contains("loop");
        }

        /// <summary>
        /// Fills an <see cref="AudioImporterSampleSettings"/> struct with the correct
        /// compression, load type, and sample rate for the given file category.
        /// </summary>
        private static void ApplySettings(ref AudioImporterSampleSettings settings, bool isMusic)
        {
            if (isMusic)
            {
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality           = MUSIC_VORBIS_QUALITY;
                settings.loadType          = AudioClipLoadType.Streaming;
            }
            else
            {
                settings.compressionFormat = AudioCompressionFormat.ADPCM;
                settings.quality           = 1f; // quality is irrelevant for ADPCM, but set it cleanly
                settings.loadType          = AudioClipLoadType.DecompressOnLoad;
            }

            settings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
            settings.sampleRateOverride = TARGET_SAMPLE_RATE;
        }
    }
}
