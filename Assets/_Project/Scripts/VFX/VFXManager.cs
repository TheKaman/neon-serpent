using UnityEngine;

namespace NeonSerpent.VFX
{
    /// <summary>
    /// Singleton that owns and plays all in-game particle effects.
    /// All ParticleSystems are built procedurally in Awake — no art assets required.
    /// Scene-local singleton: not DontDestroyOnLoad.
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        private ParticleSystem _eatBurst;
        private ParticleSystem _bonusEatBurst;
        private ParticleSystem _deathExplosion;
        private ParticleSystem _powerUpBurst;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _eatBurst       = BuildEatBurst();
            _bonusEatBurst  = BuildBonusEatBurst();
            _deathExplosion = BuildDeathExplosion();
            _powerUpBurst   = BuildPowerUpBurst();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ─────────────────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────────────────

        /// <summary>Play a small burst at the given world position when normal food is eaten.</summary>
        public void PlayEatBurst(Vector3 pos, Color color)
        {
            if (_eatBurst == null) return;
            TintSystem(_eatBurst, color);
            PlayAt(_eatBurst, pos);
        }

        /// <summary>Play a larger gold burst at the given world position when bonus food is eaten.</summary>
        public void PlayBonusEatBurst(Vector3 pos)
        {
            if (_bonusEatBurst == null) return;
            PlayAt(_bonusEatBurst, pos);
        }

        /// <summary>Play a large explosion at the given world position on snake death.</summary>
        public void PlayDeathExplosion(Vector3 pos, Color snakeColor)
        {
            if (_deathExplosion == null) return;
            TintSystem(_deathExplosion, snakeColor);
            PlayAt(_deathExplosion, pos);
        }

        /// <summary>Play a white burst at the given world position when a power-up is collected.</summary>
        public void PlayPowerUpBurst(Vector3 pos)
        {
            if (_powerUpBurst == null) return;
            PlayAt(_powerUpBurst, pos);
        }

        // ─────────────────────────────────────────────────────────
        // INTERNAL HELPERS
        // ─────────────────────────────────────────────────────────

        private void PlayAt(ParticleSystem ps, Vector3 worldPos)
        {
            ps.transform.position = worldPos;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play();
        }

        private void TintSystem(ParticleSystem ps, Color color)
        {
            var main = ps.main;
            // Fade the start color from the supplied color to transparent over particle lifetime
            var startColor = new ParticleSystem.MinMaxGradient(
                color,
                new Color(color.r, color.g, color.b, 0f));
            main.startColor = startColor;
        }

        private static Material MakeDefaultMaterial()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            return new Material(shader != null ? shader : Shader.Find("Hidden/InternalErrorShader"));
        }

        // ─────────────────────────────────────────────────────────
        // PARTICLE SYSTEM BUILDERS
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// EatBurst — 8 cyan particles, 0.4s, outward circle burst.
        /// </summary>
        private ParticleSystem BuildEatBurst()
        {
            var go = new GameObject("[VFX_EatBurst]");
            go.transform.SetParent(transform);

            var ps   = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop              = false;
            main.playOnAwake       = false;
            main.maxParticles      = 8;
            main.startLifetime     = 0.4f;
            main.startSpeed        = 3f;
            main.startSize         = 0.08f;
            main.startColor        = new ParticleSystem.MinMaxGradient(
                new Color(0f, 1f, 0.8f, 1f),
                new Color(0f, 1f, 0.8f, 0f));
            main.gravityModifier   = 0f;
            main.simulationSpace   = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled     = true;
            emission.rateOverTime = 0;
            var burst = new ParticleSystem.Burst(0f, 8);
            emission.SetBursts(new[] { burst });

            var shape = ps.shape;
            shape.enabled      = true;
            shape.shapeType    = ParticleSystemShapeType.Circle;
            shape.radius       = 0.1f;
            shape.radiusThickness = 0f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material       = MakeDefaultMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return ps;
        }

        /// <summary>
        /// BonusEatBurst — 16 gold particles, 0.6s, larger and faster.
        /// </summary>
        private ParticleSystem BuildBonusEatBurst()
        {
            var go = new GameObject("[VFX_BonusEatBurst]");
            go.transform.SetParent(transform);

            var ps   = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop              = false;
            main.playOnAwake       = false;
            main.maxParticles      = 16;
            main.startLifetime     = 0.6f;
            main.startSpeed        = 5f;
            main.startSize         = 0.15f;
            main.startColor        = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.84f, 0f, 1f),
                new Color(1f, 0.84f, 0f, 0f));
            main.gravityModifier   = 0f;
            main.simulationSpace   = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled     = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) });

            var shape = ps.shape;
            shape.enabled         = true;
            shape.shapeType       = ParticleSystemShapeType.Circle;
            shape.radius          = 0.1f;
            shape.radiusThickness = 0f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material       = MakeDefaultMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return ps;
        }

        /// <summary>
        /// DeathExplosion — 30 particles, outward sphere, slight gravity, snake color.
        /// </summary>
        private ParticleSystem BuildDeathExplosion()
        {
            var go = new GameObject("[VFX_DeathExplosion]");
            go.transform.SetParent(transform);

            var ps   = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop              = false;
            main.playOnAwake       = false;
            main.maxParticles      = 30;
            main.startLifetime     = 0.8f;
            main.startSpeed        = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSize         = 0.2f;
            main.startColor        = new ParticleSystem.MinMaxGradient(
                new Color(0f, 1f, 0.8f, 1f),
                new Color(0f, 1f, 0.8f, 0f));
            main.gravityModifier   = 0.3f;
            main.simulationSpace   = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled     = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });

            var shape = ps.shape;
            shape.enabled         = true;
            shape.shapeType       = ParticleSystemShapeType.Sphere;
            shape.radius          = 0.2f;
            shape.radiusThickness = 1f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material       = MakeDefaultMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return ps;
        }

        /// <summary>
        /// PowerUpCollectBurst — 12 white particles, circle burst.
        /// </summary>
        private ParticleSystem BuildPowerUpBurst()
        {
            var go = new GameObject("[VFX_PowerUpBurst]");
            go.transform.SetParent(transform);

            var ps   = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop              = false;
            main.playOnAwake       = false;
            main.maxParticles      = 12;
            main.startLifetime     = 0.5f;
            main.startSpeed        = 2f;
            main.startSize         = 0.1f;
            main.startColor        = new ParticleSystem.MinMaxGradient(
                new Color(1f, 1f, 1f, 1f),
                new Color(1f, 1f, 1f, 0f));
            main.gravityModifier   = 0f;
            main.simulationSpace   = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled     = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

            var shape = ps.shape;
            shape.enabled         = true;
            shape.shapeType       = ParticleSystemShapeType.Circle;
            shape.radius          = 0.1f;
            shape.radiusThickness = 0f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material       = MakeDefaultMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return ps;
        }
    }
}
