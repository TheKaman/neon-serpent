using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSerpent.Audio
{
    /// <summary>
    /// ScriptableObject mapping SoundEvents to AudioClips.
    /// Assign clips in the Inspector on the SoundLibrary asset.
    /// Create via: Assets > Create > NeonSerpent > Sound Library
    /// </summary>
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "NeonSerpent/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        [Serializable]
        private struct SoundEntry
        {
            public SoundEvent Event;
            public AudioClip  Clip;
        }

        [SerializeField] private SoundEntry[] _entries;

        private Dictionary<SoundEvent, AudioClip> _lookup;

        private void OnEnable() => BuildLookup();

        /// <summary>Retrieve the AudioClip for a given sound event. Returns null if not mapped.</summary>
        public AudioClip GetClip(SoundEvent sfx)
        {
            if (_lookup == null) BuildLookup();
            return _lookup.TryGetValue(sfx, out var clip) ? clip : null;
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<SoundEvent, AudioClip>();
            if (_entries == null) return;
            foreach (var entry in _entries)
                _lookup[entry.Event] = entry.Clip;
        }
    }
}
