using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeonSerpent.Utilities;

namespace NeonSerpent.Audio
{
    /// <summary>
    /// Centralized audio manager. Plays SFX from an AudioSource pool and manages music tracks.
    /// Lives in Bootstrap scene. Other systems call AudioManager.Instance.PlaySFX().
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Configuration")]
        [SerializeField] private SoundLibrary _soundLibrary;
        [SerializeField] private int          _sfxPoolSize = 8;

        [Header("Music")]
        [SerializeField] private AudioSource  _musicSource;

        private readonly Queue<AudioSource>   _sfxPool        = new Queue<AudioSource>();
        private readonly HashSet<AudioSource> _overflowSources = new HashSet<AudioSource>();
        private readonly List<Coroutine>      _activeCoroutines = new List<Coroutine>();

        private float _sfxVolume   = 1f;
        private float _musicVolume = 0.7f;

        protected override void Awake()
        {
            base.Awake();
            BuildSfxPool();
        }

        private void OnDisable()
        {
            for (int i = _activeCoroutines.Count - 1; i >= 0; i--)
            {
                if (_activeCoroutines[i] != null)
                    StopCoroutine(_activeCoroutines[i]);
            }
            _activeCoroutines.Clear();
        }

        /// <summary>Play a sound effect by event type.</summary>
        public void PlaySFX(SoundEvent sfx)
        {
            AudioClip clip = _soundLibrary?.GetClip(sfx);
            if (clip == null) return;

            AudioSource source = GetFromPool();
            source.clip   = clip;
            source.volume = _sfxVolume;
            source.Play();

            Coroutine cr = StartCoroutine(ReturnToPoolWhenDone(source, clip.length));
            _activeCoroutines.Add(cr);
        }

        /// <summary>Play a music track, looping it.</summary>
        public void PlayMusic(AudioClip track)
        {
            if (_musicSource == null) return;
            _musicSource.clip   = track;
            _musicSource.volume = _musicVolume;
            _musicSource.loop   = true;
            _musicSource.Play();
        }

        /// <summary>Stop the current music track.</summary>
        public void StopMusic() => _musicSource?.Stop();

        /// <summary>Set the SFX volume and persist to PlayerPrefs.</summary>
        public void SetSFXVolume(float v)
        {
            _sfxVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat("sfx_volume", _sfxVolume);
        }

        /// <summary>Set the music volume and persist to PlayerPrefs.</summary>
        public void SetMusicVolume(float v)
        {
            _musicVolume = Mathf.Clamp01(v);
            if (_musicSource != null) _musicSource.volume = _musicVolume;
            PlayerPrefs.SetFloat("music_volume", _musicVolume);
        }

        private void BuildSfxPool()
        {
            _sfxVolume   = PlayerPrefs.GetFloat("sfx_volume",   1f);
            _musicVolume = PlayerPrefs.GetFloat("music_volume", 0.7f);

            for (int i = 0; i < _sfxPoolSize; i++)
            {
                var go = new GameObject($"SFX_Source_{i}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _sfxPool.Enqueue(src);
            }
        }

        private AudioSource GetFromPool()
        {
            if (_sfxPool.Count > 0) return _sfxPool.Dequeue();

            // Pool exhausted — create a temporary overflow source tracked for cleanup.
            var go  = new GameObject("SFX_Overflow");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            _overflowSources.Add(src);
            return src;
        }

        private IEnumerator ReturnToPoolWhenDone(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay + 0.05f);
            source.Stop();

            _activeCoroutines.RemoveAll(c => c == null);

            if (_overflowSources.Contains(source))
            {
                // Overflow source — destroy its GameObject rather than growing the pool.
                _overflowSources.Remove(source);
                Destroy(source.gameObject);
            }
            else
            {
                _sfxPool.Enqueue(source);
            }
        }
    }

    /// <summary>Sound event enum mapping to clips in SoundLibrary.</summary>
    public enum SoundEvent
    {
        EatFood,
        EatBonus,
        EatPoison,
        PowerUpCollect,
        PowerUpExpire,
        Death,
        LevelComplete,
        UIClick,
        UIBack,
        ShieldAbsorb,
        Combo,
        PoisonExpired,
        FrenzyActivated  // distinct cue for entering Frenzy Mode in Classic Endless
    }
}
