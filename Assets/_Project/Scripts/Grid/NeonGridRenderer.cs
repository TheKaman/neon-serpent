using UnityEngine;
using NeonSerpent.Utilities;

namespace NeonSerpent.Grid
{
    /// <summary>
    /// Renders the neon grid background as a procedural mesh of line quads.
    /// Attach to a child GameObject of the GridSystem with MeshFilter + MeshRenderer.
    /// The material color controls the grid line color.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class NeonGridRenderer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridSystem _grid;

        [Header("Appearance")]
        [SerializeField] private float _lineWidth = 0.04f;
        [SerializeField] private Color _lineColor = new Color(0.1f, 0.25f, 0.12f, 1f);

        private MeshFilter   _meshFilter;
        private MeshRenderer _meshRenderer;

        private void Awake()
        {
            _meshFilter   = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        private void Start()
        {
            BuildGridMesh();
            CenterCamera();
        }

        /// <summary>Rebuild the grid mesh (call after grid dimensions change).</summary>
        public void BuildGridMesh()
        {
            int w = _grid.Width;
            int h = _grid.Height;

            int lineCount = (w + 1) + (h + 1);
            var vertices  = new Vector3[lineCount * 4];
            var triangles = new int[lineCount * 6];
            var uvs       = new Vector2[lineCount * 4];

            int vi = 0, ti = 0;
            float half   = _lineWidth * 0.5f;
            float totalW = w * Constants.CELL_SIZE;
            float totalH = h * Constants.CELL_SIZE;

            for (int x = 0; x <= w; x++)
            {
                float px = x * Constants.CELL_SIZE - half;
                vertices[vi + 0] = new Vector3(px,              -half,         0);
                vertices[vi + 1] = new Vector3(px + _lineWidth, -half,         0);
                vertices[vi + 2] = new Vector3(px,              totalH + half, 0);
                vertices[vi + 3] = new Vector3(px + _lineWidth, totalH + half, 0);
                AddQuadTris(triangles, ref ti, vi);
                vi += 4;
            }

            for (int y = 0; y <= h; y++)
            {
                float py = y * Constants.CELL_SIZE - half;
                vertices[vi + 0] = new Vector3(-half,         py,              0);
                vertices[vi + 1] = new Vector3(totalW + half, py,              0);
                vertices[vi + 2] = new Vector3(-half,         py + _lineWidth, 0);
                vertices[vi + 3] = new Vector3(totalW + half, py + _lineWidth, 0);
                AddQuadTris(triangles, ref ti, vi);
                vi += 4;
            }

            for (int i = 0; i < uvs.Length; i++) uvs[i] = Vector2.one;

            var mesh       = new Mesh { name = "NeonGrid" };
            mesh.vertices  = vertices;
            mesh.triangles = triangles;
            mesh.uv        = uvs;
            mesh.RecalculateBounds();
            _meshFilter.mesh = mesh;

            if (_meshRenderer.sharedMaterial != null)
                _meshRenderer.sharedMaterial.color = _lineColor;
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
