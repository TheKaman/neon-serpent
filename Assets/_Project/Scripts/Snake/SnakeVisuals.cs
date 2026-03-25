using System.Collections.Generic;
using UnityEngine;
using NeonSerpent.Grid;
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

        private bool _ghostMode;

        private void Awake()
        {
            _segmentPool = new ObjectPool<SnakeSegment>(_bodyPrefab, 32, transform);
        }

        private void OnEnable()
        {
            _snake.OnMoved   += HandleMoved;
        }

        private void OnDisable()
        {
            _snake.OnMoved   -= HandleMoved;
        }

        private void Start()
        {
            // Spawn head
            _headSegment = Instantiate(_headPrefab, transform);
            RefreshAll();
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
            RefreshAll();
        }

        private void RefreshAll()
        {
            // Return all body segments to pool
            foreach (var seg in _bodySegments)
                _segmentPool.Return(seg);
            _bodySegments.Clear();

            var positions = new List<Vector2Int>(_snake.Body);

            if (positions.Count == 0) return;

            // Position head at last element (front of queue)
            _headSegment.transform.position = _grid.GridToWorld(positions[positions.Count - 1]);
            _headSegment.gameObject.SetActive(true);

            // Body segments
            for (int i = 0; i < positions.Count - 1; i++)
            {
                var seg = _segmentPool.Get();
                seg.transform.position = _grid.GridToWorld(positions[i]);
                _bodySegments.Add(seg);
            }
        }

        private void SetSegmentAlpha(SnakeSegment seg, float alpha)
        {
            if (seg == null) return;
            var sr = seg.GetComponent<SpriteRenderer>();
            if (sr == null) return;
            var c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }
}
