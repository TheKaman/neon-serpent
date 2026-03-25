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

        private readonly Queue<AudioSource> _sfxPool = new Queue<AudioSource>();

        private float _sfxVolume   = 1f;
        private float _musicVolume = 0.7f;

        protected override void Awake()
        {
            base.Awake();
            BuildSfxPool();
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
            StartCoroutine(ReturnToPoolWhenDone(source, clip.length));
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

        public void SetSFXVolume(float v)
        {
            _sfxVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat("sfx_volume", _sfxVolume);
        }

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
                var go = new UnityEngine.GameObject($"SFX_Source_{i}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _sfxPool.Enqueue(src);
            }
        }

        private AudioSource GetFromPool()
        {
            if (_sfxPool.Count > 0) return _sfxPool.Dequeue();
            // Pool exhausted — create a temporary source
            var go  = new UnityEngine.GameObject("SFX_Overflow");
            go.transform.SetParent(transform);
            return go.AddComponent<AudioSource>();
        }

        private System.Collections.IEnumerator ReturnToPoolWhenDone(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay + 0.05f);
            source.Stop();
            _sfxPool.Enqueue(source);
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
        Combo
    }
}
