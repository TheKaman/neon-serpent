using System.Collections;
using UnityEngine;
using NeonSerpent.Core;
using NeonSerpent.Utilities;

namespace NeonSerpent.Audio
{
    /// <summary>
    /// Reacts to GameManager state changes and drives background music with smooth
    /// 0.5-second crossfades between the menu and game tracks.
    ///
    /// Architecture notes:
    /// - Owns two AudioSources directly (primary and secondary) so crossfade math
    ///   stays entirely local — no coupling to AudioManager's internal volume state.
    /// - Reads the music volume preference from PlayerPrefs (written by AudioManager
    ///   when the player adjusts the slider) so volume settings are respected.
    /// - Lives in the Bootstrap scene on the [Managers] GameObject.
    /// - Uses Time.unscaledDeltaTime so fades work correctly during pause.
    /// </summary>
    public class MusicManager : Singleton<MusicManager>
    {
        [Header("Music Clips")]
        [SerializeField] private AudioClip _menuMusic;
        [SerializeField] private AudioClip _gameMusic;

        [Header("Crossfade")]
        [SerializeField] private float _crossfadeDuration = 0.5f;

        // ─── AudioSources ──────────────────────────────────────────────────────
        // Source A is the current / outgoing track; Source B is the incoming track.
        // After every crossfade completes, the roles swap.
        private AudioSource _sourceA;
        private AudioSource _sourceB;

        // The source currently considered "primary" (the one fading in, or fully audible).
        private AudioSource _primary   => _usingA ? _sourceA : _sourceB;
        private AudioSource _secondary => _usingA ? _sourceB : _sourceA;
        private bool        _usingA    = true;

        private Coroutine _crossfadeCoroutine;
        private AudioClip _currentClip;

        // ─────────────────────────────────────────────────────────────────────
        // LIFECYCLE
        // ─────────────────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            _sourceA = CreateMusicSource("MusicSource_A");
            _sourceB = CreateMusicSource("MusicSource_B");

            // Ship guard: these clips are assigned in the Bootstrap scene Inspector (the
            // NeonSerpentSetup auto-wiring no longer sets them). If either is null the game
            // runs silently — warn so a missing assignment is caught before release rather
            // than shipping with no music. CrossfadeTo() already no-ops safely on a null clip.
            if (_menuMusic == null)
                Debug.LogWarning("[MusicManager] _menuMusic is not assigned — menu will be silent. " +
                                 "Assign it on the MusicManager component in the Bootstrap scene.");
            if (_gameMusic == null)
                Debug.LogWarning("[MusicManager] _gameMusic is not assigned — gameplay will be silent. " +
                                 "Assign it on the MusicManager component in the Bootstrap scene.");
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            gm.OnStateChanged += HandleStateChanged;

            // Sync immediately to whatever state GameManager is already in.
            HandleStateChanged(gm.CurrentState);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.OnStateChanged -= HandleStateChanged;

            if (_crossfadeCoroutine != null)
                StopCoroutine(_crossfadeCoroutine);
        }

        // ─────────────────────────────────────────────────────────────────────
        // STATE → MUSIC MAPPING
        // ─────────────────────────────────────────────────────────────────────

        private void HandleStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.Boot:
                case GameState.MainMenu:
                    CrossfadeTo(_menuMusic);
                    break;

                case GameState.Playing:
                    CrossfadeTo(_gameMusic);
                    break;

                case GameState.GameOver:
                    // Silence music so the death SFX and game-over sting play cleanly.
                    FadeOutAll();
                    break;

                // Paused and LevelComplete leave the current track untouched.
                case GameState.Paused:
                case GameState.LevelComplete:
                default:
                    break;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // CROSSFADE METHODS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Begins a crossfade to <paramref name="clip"/>. No-ops if that clip is already
        /// the active track. Safe to call with a null clip — plays silence.
        /// </summary>
        private void CrossfadeTo(AudioClip clip)
        {
            if (AudioManager.Instance == null) return;
            if (clip == null)                  return;
            if (clip == _currentClip)          return;

            if (_crossfadeCoroutine != null)
                StopCoroutine(_crossfadeCoroutine);

            _crossfadeCoroutine = StartCoroutine(CrossfadeRoutine(clip));
        }

        /// <summary>Fades out all music without starting a new track.</summary>
        private void FadeOutAll()
        {
            if (_crossfadeCoroutine != null)
                StopCoroutine(_crossfadeCoroutine);

            _crossfadeCoroutine = StartCoroutine(FadeOutRoutine());
        }

        // ─────────────────────────────────────────────────────────────────────
        // COROUTINES
        // ─────────────────────────────────────────────────────────────────────

        private IEnumerator CrossfadeRoutine(AudioClip newClip)
        {
            _currentClip = newClip;

            float targetVolume = PlayerPrefs.GetFloat("music_volume", 0.7f);

            // Start the incoming track on the secondary source, silent.
            _secondary.clip   = newClip;
            _secondary.volume = 0f;
            _secondary.loop   = true;
            _secondary.Play();

            float elapsed       = 0f;
            float startVolume   = _primary.volume;

            while (elapsed < _crossfadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t  = Mathf.Clamp01(elapsed / _crossfadeDuration);

                _primary.volume   = Mathf.Lerp(startVolume,  0f,           t);
                _secondary.volume = Mathf.Lerp(0f,           targetVolume, t);

                yield return null;
            }

            // Ensure volumes land exactly at their targets.
            _primary.volume   = 0f;
            _secondary.volume = targetVolume;
            _primary.Stop();

            // Swap which source is considered primary for the next fade.
            _usingA = !_usingA;

            _crossfadeCoroutine = null;
        }

        private IEnumerator FadeOutRoutine()
        {
            _currentClip = null;

            float elapsed     = 0f;
            float startVolume = _primary.volume;

            while (elapsed < _crossfadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t  = Mathf.Clamp01(elapsed / _crossfadeDuration);
                _primary.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            _primary.volume = 0f;
            _primary.Stop();

            _crossfadeCoroutine = null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private AudioSource CreateMusicSource(string sourceName)
        {
            var go  = new GameObject(sourceName);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop        = true;
            src.volume      = 0f;
            return src;
        }
    }
}
