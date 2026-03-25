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

        // Active effects: type → (effect, remaining time, coroutine)
        private readonly Dictionary<PowerUpType, (PowerUpEffect effect, Coroutine coroutine)> _active =
            new Dictionary<PowerUpType, (PowerUpEffect, Coroutine)>();

        public event Action<PowerUpType, float> OnEffectApplied;  // type, duration
        public event Action<PowerUpType>        OnEffectExpired;

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
                var coroutine = StartCoroutine(EffectTimer(type, effect));
                _active[type] = (effect, coroutine);
                OnEffectApplied?.Invoke(type, effect.Duration);
            }
            else
            {
                // Instant effect — apply and done, no timer needed
                OnEffectApplied?.Invoke(type, 0f);
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
        }

        private IEnumerator EffectTimer(PowerUpType type, PowerUpEffect effect)
        {
            yield return new WaitForSeconds(effect.Duration);
            RemoveEffect(type, effect);
        }

        private void RemoveEffect(PowerUpType type, PowerUpEffect effect)
        {
            effect.Remove(_snake, _score);

            if (type == PowerUpType.GhostMode)
                _snakeVisuals?.SetGhostMode(false);

            _active.Remove(type);
            OnEffectExpired?.Invoke(type);
        }

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
