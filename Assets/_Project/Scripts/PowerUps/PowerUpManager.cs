using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeonSerpent.Snake;
using NeonSerpent.Scoring;

namespace NeonSerpent.PowerUps
{
    /// <summary>
    /// Tracks all currently active power-up effects, runs their timers,
    /// and calls Remove() when they expire. Also handles GhostMode visual toggle.
    /// Place on the Game scene's PowerUpManager GameObject.
    /// </summary>
    public class PowerUpManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SnakeController _snake;
        [SerializeField] private ScoreManager    _score;
        [SerializeField] private SnakeVisuals    _snakeVisuals;

        // Active timed effects: type → (effect, coroutine)
        private readonly Dictionary<PowerUpType, (PowerUpEffect effect, Coroutine coroutine)> _active =
            new Dictionary<PowerUpType, (PowerUpEffect, Coroutine)>();

        // Bug G fix: instant effects (Duration == 0) are not tracked in _active because
        // they never enter the coroutine branch. Track them separately so ClearAll() can
        // call Remove() on them — otherwise ShieldEffect.Remove() would never be called
        // and IsShielded could remain true across game sessions.
        private readonly Dictionary<PowerUpType, PowerUpEffect> _instantEffects =
            new Dictionary<PowerUpType, PowerUpEffect>();

        /// <summary>Fired when a timed effect becomes active. Args: type, total duration in seconds.</summary>
        public event Action<PowerUpType, float> OnEffectActivated;

        /// <summary>Fired when an active effect expires or is force-removed.</summary>
        public event Action<PowerUpType>        OnEffectDeactivated;

        /// <summary>Apply a power-up effect by type. Replaces any existing effect of the same type.</summary>
        public void ApplyEffect(PowerUpType type)
        {
            PowerUpEffect effect = CreateEffect(type);
            if (effect == null) return;

            // Remove existing effect of same type first
            if (_active.ContainsKey(type))
                ForceExpire(type);

            effect.Apply(_snake, _score);

            // Handle ghost mode visual
            if (type == PowerUpType.GhostMode)
                _snakeVisuals?.SetGhostMode(true);

            if (effect.Duration > 0f)
            {
                effect.SetRemainingTime(effect.Duration);
                var coroutine = StartCoroutine(EffectTimer(type, effect));
                _active[type] = (effect, coroutine);
                OnEffectActivated?.Invoke(type, effect.Duration);
            }
            else
            {
                // Bug G fix: track instant effects so ClearAll() can call Remove() on them.
                // Replacing an existing instant effect of the same type calls Remove() first.
                if (_instantEffects.TryGetValue(type, out var existing))
                {
                    existing.Remove(_snake, _score);
                    _instantEffects.Remove(type);
                }
                _instantEffects[type] = effect;
                OnEffectActivated?.Invoke(type, 0f);
            }
        }

        /// <summary>Force-expire an active effect immediately (e.g., on game over).</summary>
        public void ForceExpire(PowerUpType type)
        {
            if (!_active.TryGetValue(type, out var entry)) return;
            StopCoroutine(entry.coroutine);
            RemoveEffect(type, entry.effect);
        }

        /// <summary>Clear all active effects (call on game over / restart).</summary>
        public void ClearAll()
        {
            foreach (var type in new List<PowerUpType>(_active.Keys))
                ForceExpire(type);

            // Bug G fix: also remove instant effects (e.g. ShieldEffect) that were never
            // added to _active and therefore never reached by the loop above.
            foreach (var kvp in _instantEffects)
            {
                kvp.Value.Remove(_snake, _score);
                OnEffectDeactivated?.Invoke(kvp.Key);
            }
            _instantEffects.Clear();
        }

        private IEnumerator EffectTimer(PowerUpType type, PowerUpEffect effect)
        {
            float elapsed = 0f;
            while (elapsed < effect.Duration)
            {
                // Clamp to 100 ms per frame — prevents a large delta-time spike that can
                // occur on the first Update() after an Android app resume from background
                // from instantly expiring all active effects in a single frame.
                elapsed += Mathf.Min(Time.deltaTime, 0.1f);
                effect.SetRemainingTime(Mathf.Max(0f, effect.Duration - elapsed));
                yield return null;
            }
            RemoveEffect(type, effect);
        }

        private void RemoveEffect(PowerUpType type, PowerUpEffect effect)
        {
            effect.Remove(_snake, _score);
            effect.SetRemainingTime(0f);

            if (type == PowerUpType.GhostMode)
                _snakeVisuals?.SetGhostMode(false);

            _active.Remove(type);
            OnEffectDeactivated?.Invoke(type);
        }

        /// <summary>Returns remaining time for an active effect, or 0 if not active.</summary>
        public float GetRemainingTime(PowerUpType type)
        {
            return _active.TryGetValue(type, out var entry) ? entry.effect.RemainingTime : 0f;
        }

        /// <summary>Returns true if the given power-up type is currently active.</summary>
        public bool IsActive(PowerUpType type) => _active.ContainsKey(type);

        private PowerUpEffect CreateEffect(PowerUpType type) => type switch
        {
            PowerUpType.SpeedBoost      => new Effects.SpeedBoostEffect(),
            PowerUpType.Shield          => new Effects.ShieldEffect(),
            PowerUpType.ScoreMultiplier => new Effects.ScoreMultiplierEffect(),
            PowerUpType.GhostMode       => new Effects.GhostModeEffect(),
            PowerUpType.ShrinkPill      => new Effects.ShrinkEffect(),
            PowerUpType.Poison          => new Effects.PoisonEffect(),
            _                           => null
        };
    }
}
