using System.Collections.Generic;
using UnityEngine;
using NeonSerpent.Grid;
using NeonSerpent.SaveData;
using NeonSerpent.Utilities;

namespace NeonSerpent.Snake
{
    /// <summary>
    /// Renders the snake by positioning a pool of segment GameObjects.
    /// Contains ZERO game logic — only reads from SnakeController and GridSystem.
    /// </summary>
    public class SnakeVisuals : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SnakeController _snake;
        [SerializeField] private GridSystem      _grid;

        [Header("Prefabs")]
        [SerializeField] private SnakeSegment _headPrefab;
        [SerializeField] private SnakeSegment _bodyPrefab;

        [Header("Ghost Mode")]
        [SerializeField] private float _ghostAlpha = 0.35f;

        private SnakeSegment _headSegment;
        private readonly List<SnakeSegment> _bodySegments = new List<SnakeSegment>();
        private ObjectPool<SnakeSegment> _segmentPool;

        private bool          _ghostMode;
        private Color         _skinColor = Color.white;
        private TrailRenderer _headTrail;
        // Set to true by OnEnable so the next HandleMoved (fired by Initialize()) always
        // re-reads the equipped skin — ensures a skin bought between sessions is applied.
        private bool          _pendingSkinRefresh = true;

        private void OnEnable()
        {
            if (_snake != null)
                _snake.OnMoved += HandleMoved;
            // Arm skin refresh so the next Initialize() call picks up any skin change.
            _pendingSkinRefresh = true;
        }

        private void OnDisable()
        {
            if (_snake != null)
                _snake.OnMoved -= HandleMoved;
        }

        private void Awake()
        {
            // Bug D fix: the pool and head segment MUST exist before Initialize() fires
            // OnMoved. If creation were deferred to Start(), any OnMoved raised between
            // Awake and the first Start (e.g. when the scene loads with GameManager already
            // in Playing state) would find _segmentPool null and silently skip the render.
            if (_snake == null)
            {
                Debug.LogError("[SnakeVisuals] _snake is not assigned. Assign it in the Inspector.", this);
                return;
            }
            if (_headPrefab == null)
            {
                Debug.LogError("[SnakeVisuals] _headPrefab is not assigned. Assign it in the Inspector.", this);
                return;
            }
            if (_bodyPrefab == null)
            {
                Debug.LogError("[SnakeVisuals] _bodyPrefab is not assigned. Assign it in the Inspector.", this);
                return;
            }
            if (_grid == null)
            {
                Debug.LogError("[SnakeVisuals] _grid is not assigned. Assign it in the Inspector.", this);
                return;
            }

            _segmentPool = new ObjectPool<SnakeSegment>(_bodyPrefab, 32, transform);
            _skinColor   = ResolveSkinColor();
            _headSegment = Instantiate(_headPrefab, transform);
            AttachHeadTrail();
        }

        private void Start()
        {
            // Awake handles pool/head creation. Start only does the initial visual refresh
            // so that everything is ready after all other Awake() calls have completed.
            RefreshAll();
        }

        /// <summary>
        /// Creates and configures a TrailRenderer on the head segment for the glow trail effect.
        /// </summary>
        private void AttachHeadTrail()
        {
            if (_headSegment == null) return;

            _headTrail = _headSegment.gameObject.AddComponent<TrailRenderer>();
            _headTrail.time             = 0.15f;
            _headTrail.minVertexDistance = 0.1f;
            _headTrail.startWidth       = 0.3f;
            _headTrail.endWidth         = 0f;
            _headTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _headTrail.receiveShadows   = false;
            _headTrail.generateLightingData = false;

            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null) _headTrail.material = new Material(shader);

            UpdateTrailColor();
        }

        /// <summary>Synchronises the trail gradient with the current skin color.</summary>
        private void UpdateTrailColor()
        {
            if (_headTrail == null) return;

            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(_skinColor, 0f),
                    new GradientColorKey(_skinColor, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            _headTrail.colorGradient = gradient;
        }

        /// <summary>Toggle ghost mode transparency on the whole snake.</summary>
        public void SetGhostMode(bool enabled)
        {
            _ghostMode = enabled;
            float alpha = enabled ? _ghostAlpha : 1f;
            SetSegmentAlpha(_headSegment, alpha);
            foreach (var seg in _bodySegments)
                SetSegmentAlpha(seg, alpha);
        }

        private void HandleMoved(Vector2Int headPos)
        {
            // Group 2 fix: on the first OnMoved of a new session (fired by Initialize()),
            // re-read the skin in case the player equipped a different one since last play.
            if (_pendingSkinRefresh)
            {
                _pendingSkinRefresh = false;
                RefreshSkin();
            }

            RefreshAll();
        }

        private void RefreshAll()
        {
            if (_headSegment == null || _grid == null || _snake == null || _segmentPool == null) return;

            // Return all body segments to pool
            foreach (var seg in _bodySegments)
                _segmentPool.Return(seg);
            _bodySegments.Clear();

            var positions = new List<Vector2Int>(_snake.Body);
            if (positions.Count == 0) return;

            // Position and rotate head — sprite is drawn facing right (0°) by convention
            _headSegment.transform.position = _grid.GridToWorld(positions[positions.Count - 1]);

            var dir = _snake.CurrentDirection;
            float headAngle = 0f;
            if      (dir.x ==  1) headAngle =   0f;
            else if (dir.y ==  1) headAngle =  90f;
            else if (dir.x == -1) headAngle = 180f;
            else if (dir.y == -1) headAngle = 270f;
            _headSegment.transform.eulerAngles = new Vector3(0f, 0f, headAngle);

            _headSegment.gameObject.SetActive(true);
            ApplySkinColor(_headSegment, _ghostMode ? _ghostAlpha : 1f);

            // Body segments
            for (int i = 0; i < positions.Count - 1; i++)
            {
                var seg = _segmentPool.Get();
                seg.transform.position = _grid.GridToWorld(positions[i]);
                ApplySkinColor(seg, _ghostMode ? _ghostAlpha : 1f);
                _bodySegments.Add(seg);
            }
        }

        /// <summary>
        /// Refresh the skin color and trail gradient — call this when the equipped skin changes.
        /// </summary>
        public void RefreshSkin()
        {
            _skinColor = ResolveSkinColor();
            UpdateTrailColor();
        }

        private static Color ResolveSkinColor()
        {
            string id = SaveManager.Instance?.Data?.equippedSkinId ?? "default";
            return id switch
            {
                "neon_pink"   => new Color(1f,   0.2f, 0.8f),
                "cyber_blue"  => new Color(0.1f, 0.6f, 1f),
                "toxic_green" => new Color(0.2f, 1f,   0.3f),
                "gold"        => new Color(1f,   0.84f, 0f),
                "void_purple" => new Color(0.6f, 0.1f, 1f),
                _             => new Color(0f,   1f,   0.8f), // default cyan
            };
        }

        private void ApplySkinColor(SnakeSegment seg, float alpha)
        {
            if (seg == null || seg.Renderer == null) return;
            seg.Renderer.color = new Color(_skinColor.r, _skinColor.g, _skinColor.b, alpha);
        }

        private void SetSegmentAlpha(SnakeSegment seg, float alpha)
        {
            if (seg == null || seg.Renderer == null) return;
            var c = seg.Renderer.color;
            c.a = alpha;
            seg.Renderer.color = c;
        }
    }
}
