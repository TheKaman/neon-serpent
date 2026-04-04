using UnityEngine;
using NeonSerpent.Utilities;

namespace NeonSerpent.Grid
{
    /// <summary>
    /// Renders a neon border around the play area. No internal grid lines —
    /// just the four edges forming the snake's arena boundary.
    /// The border color pulses slowly between two cyan shades for a neon glow effect.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class NeonGridRenderer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridSystem _grid;

        [Header("Appearance")]
        [SerializeField] private float _lineWidth = 0.08f;

        private static readonly Color BORDER_COLOR_A = new Color(0f, 1f,  1f,  1f);   // bright cyan
        private static readonly Color BORDER_COLOR_B = new Color(0f, 0.5f, 0.8f, 0.7f); // dim cyan

        private MeshFilter   _meshFilter;
        private MeshRenderer _meshRenderer;
        private Material     _material;

        private void Awake()
        {
            _meshFilter   = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }

        private void Start()
        {
            BuildBorderMesh();
            CenterCamera();
        }

        private void Update()
        {
            if (_material == null) return;

            float t = (Mathf.Sin(Time.time * 1.2f) + 1f) * 0.5f;
            _material.color = Color.Lerp(BORDER_COLOR_B, BORDER_COLOR_A, t);
        }

        /// <summary>Rebuild the border mesh (call after grid dimensions change).</summary>
        public void BuildGridMesh()
        {
            BuildBorderMesh();
        }

        private void BuildBorderMesh()
        {
            if (_grid == null) return;

            int   w      = _grid.Width;
            int   h      = _grid.Height;
            float cell   = Constants.CELL_SIZE;
            float half   = _lineWidth * 0.5f;
            float halfCell = cell * 0.5f;

            // Cell centers run from 0 to (w-1)*cell and 0 to (h-1)*cell.
            // Each sprite is cell-sized, so the play area spans:
            //   x: [-halfCell, (w-1)*cell + halfCell]  →  [-0.5, 19.5] for a 20-wide grid
            //   y: [-halfCell, (h-1)*cell + halfCell]
            // Border edges are centered on these outer faces so all 4 sides are equidistant
            // from the snake cells.
            float xMin = -halfCell;               // center of left border
            float xMax =  (w - 1) * cell + halfCell; // center of right border
            float yMin = -halfCell;               // center of bottom border
            float yMax =  (h - 1) * cell + halfCell; // center of top border

            // 4 quads, each 4 verts + 6 tris
            var verts = new Vector3[16];
            var tris  = new int[24];
            var uvs   = new Vector2[16];

            int vi = 0, ti = 0;

            // Bottom edge — centered at y=yMin
            AddHorizontalQuad(verts, tris, uvs, ref vi, ref ti,
                xMin - half, xMax + half, yMin - half, _lineWidth);

            // Top edge — centered at y=yMax
            AddHorizontalQuad(verts, tris, uvs, ref vi, ref ti,
                xMin - half, xMax + half, yMax - half, _lineWidth);

            // Left edge — centered at x=xMin
            AddVerticalQuad(verts, tris, uvs, ref vi, ref ti,
                xMin - half, _lineWidth, yMin - half, yMax + half);

            // Right edge — centered at x=xMax
            AddVerticalQuad(verts, tris, uvs, ref vi, ref ti,
                xMax - half, _lineWidth, yMin - half, yMax + half);

            var mesh       = new Mesh { name = "NeonBorder" };
            mesh.vertices  = verts;
            mesh.triangles = tris;
            mesh.uv        = uvs;
            mesh.RecalculateBounds();
            _meshFilter.mesh = mesh;

            if (_material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit")
                          ?? Shader.Find("Sprites/Default")
                          ?? Shader.Find("Hidden/InternalErrorShader");
                _material = new Material(shader);
            }

            _material.color         = BORDER_COLOR_A;
            _meshRenderer.material  = _material;
        }

        private void AddHorizontalQuad(Vector3[] v, int[] t, Vector2[] uv,
            ref int vi, ref int ti, float x0, float x1, float y0, float lineW)
        {
            v[vi]   = new Vector3(x0, y0,         0);
            v[vi+1] = new Vector3(x1, y0,         0);
            v[vi+2] = new Vector3(x0, y0 + lineW, 0);
            v[vi+3] = new Vector3(x1, y0 + lineW, 0);
            AddQuadTris(t, ref ti, vi);
            for (int i = vi; i < vi + 4; i++) uv[i] = Vector2.one;
            vi += 4;
        }

        private void AddVerticalQuad(Vector3[] v, int[] t, Vector2[] uv,
            ref int vi, ref int ti, float x0, float lineW, float y0, float y1)
        {
            v[vi]   = new Vector3(x0,         y0, 0);
            v[vi+1] = new Vector3(x0 + lineW, y0, 0);
            v[vi+2] = new Vector3(x0,         y1, 0);
            v[vi+3] = new Vector3(x0 + lineW, y1, 0);
            AddQuadTris(t, ref ti, vi);
            for (int i = vi; i < vi + 4; i++) uv[i] = Vector2.one;
            vi += 4;
        }

        private void AddQuadTris(int[] tris, ref int ti, int vi)
        {
            tris[ti++] = vi;     tris[ti++] = vi + 2; tris[ti++] = vi + 1;
            tris[ti++] = vi + 1; tris[ti++] = vi + 2; tris[ti++] = vi + 3;
        }

        /// <summary>Center the main camera on the grid.</summary>
        private void CenterCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            float gridW = _grid.Width  * Constants.CELL_SIZE;
            float gridH = _grid.Height * Constants.CELL_SIZE;

            cam.transform.position = new Vector3(
                gridW * 0.5f - Constants.CELL_SIZE * 0.5f,
                gridH * 0.5f - Constants.CELL_SIZE * 0.5f,
                -10f);

            float margin = 1.5f;
            float aspect = (float)Screen.width / Screen.height;
            cam.orthographicSize = Mathf.Max(
                gridH * 0.5f + margin,
                gridW * 0.5f / aspect + margin);
        }
    }
}
