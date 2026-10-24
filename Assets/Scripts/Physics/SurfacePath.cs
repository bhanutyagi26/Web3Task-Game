using UnityEngine;

namespace CurveShot.Physics
{
    /// <summary>
    /// Represents the curved ramp surface generated via Cubic Bezier mathematics.
    /// Synchronizes physics collision geometry (EdgeCollider2D) with visual line/mesh rendering.
    /// </summary>
    [RequireComponent(typeof(EdgeCollider2D))]
    public class SurfacePath : MonoBehaviour
    {
        [Header("Bezier Control Points (Local Space)")]
        [SerializeField] private Vector2 p0 = new Vector2(1.0f, 2.5f);
        [SerializeField] private Vector2 p1 = new Vector2(2.5f, 2.2f);
        [SerializeField] private Vector2 p2 = new Vector2(4.5f, -0.5f);
        [SerializeField] private Vector2 p3 = new Vector2(6.0f, -3.2f);

        [Header("Generation Settings")]
        [Range(10, 100)]
        [SerializeField] private int sampleResolution = 50;
        [SerializeField] private float visualLineWidth = 0.22f;
        [SerializeField] private Color normalColor = new Color(0.18f, 0.75f, 0.95f, 1f); // Vibrant Cyan
        [SerializeField] private Color highlightColor = new Color(0.35f, 1.0f, 0.65f, 1f); // Neon Greenish Cyan

        [Header("Components")]
        [SerializeField] private EdgeCollider2D edgeCollider;
        [SerializeField] private LineRenderer lineRenderer;

        // Cached sampled points in world space
        private Vector2[] _sampledWorldPoints;
        private Vector2[] _sampledLocalPoints;

        public Vector2 P0 => transform.TransformPoint(p0);
        public Vector2 P1 => transform.TransformPoint(p1);
        public Vector2 P2 => transform.TransformPoint(p2);
        public Vector2 P3 => transform.TransformPoint(p3);

        public Vector2[] SampledWorldPoints => _sampledWorldPoints;
        public Vector2 RampEntryTangent => BezierMath.EvaluateCubicTangent(P0, P1, P2, P3, 0.05f);

        private void Awake()
        {
            if (edgeCollider == null) edgeCollider = GetComponent<EdgeCollider2D>();
            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();

            RebuildPath();
        }

        private void OnValidate()
        {
            if (edgeCollider == null) edgeCollider = GetComponent<EdgeCollider2D>();
            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();

            RebuildPath();
        }

        /// <summary>
        /// Configures new Bezier control points (e.g. from LevelController) and regenerates geometry.
        /// </summary>
        public void ConfigurePoints(Vector2 newP0, Vector2 newP1, Vector2 newP2, Vector2 newP3)
        {
            p0 = newP0;
            p1 = newP1;
            p2 = newP2;
            p3 = newP3;
            RebuildPath();
        }

        /// <summary>
        /// Generates the collider points and visual LineRenderer points from the Bezier formula.
        /// </summary>
        public void RebuildPath()
        {
            if (sampleResolution < 2) sampleResolution = 2;

            _sampledLocalPoints = BezierMath.SamplePoints(p0, p1, p2, p3, sampleResolution);
            _sampledWorldPoints = new Vector2[sampleResolution];

            for (int i = 0; i < sampleResolution; i++)
            {
                _sampledWorldPoints[i] = transform.TransformPoint(_sampledLocalPoints[i]);
            }

            // Configure EdgeCollider2D
            if (edgeCollider != null)
            {
                edgeCollider.points = _sampledLocalPoints;
                edgeCollider.edgeRadius = 0.04f; // subtle round bevel for ultra-smooth rolling
            }

            // Configure LineRenderer
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = sampleResolution;
                lineRenderer.useWorldSpace = true;
                lineRenderer.startWidth = visualLineWidth;
                lineRenderer.endWidth = visualLineWidth;

                for (int i = 0; i < sampleResolution; i++)
                {
                    lineRenderer.SetPosition(i, new Vector3(_sampledWorldPoints[i].x, _sampledWorldPoints[i].y, 0f));
                }

                SetHighlight(false);
            }
        }

        /// <summary>
        /// Evaluates the normalized tangent direction vector along the ramp at parameter t [0, 1].
        /// </summary>
        public Vector2 GetTangentAt(float t)
        {
            return BezierMath.EvaluateCubicTangent(P0, P1, P2, P3, t);
        }

        /// <summary>
        /// Evaluates the surface normal at parameter t [0, 1].
        /// </summary>
        public Vector2 GetNormalAt(float t)
        {
            return BezierMath.EvaluateCubicNormal(P0, P1, P2, P3, t);
        }

        /// <summary>
        /// Projects a world position onto the curve and returns the closest parameter t [0, 1].
        /// </summary>
        public float GetClosestT(Vector2 worldPosition, out Vector2 closestPoint)
        {
            if (_sampledWorldPoints == null || _sampledWorldPoints.Length == 0)
            {
                RebuildPath();
            }
            return BezierMath.FindClosestT(_sampledWorldPoints, worldPosition, out closestPoint);
        }

        /// <summary>
        /// Toggles visual highlight glow on the ramp (e.g. during successful traversal).
        /// </summary>
        public void SetHighlight(bool active)
        {
            if (lineRenderer == null) return;

            Color targetColor = active ? highlightColor : normalColor;
            lineRenderer.startColor = targetColor;
            lineRenderer.endColor = targetColor;
        }

        private void OnDrawGizmos()
        {
            // Visualize Bezier control points and curve in Editor
            Gizmos.color = Color.gray;
            Gizmos.DrawLine(P0, P1);
            Gizmos.DrawLine(P2, P3);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(P0, 0.18f); // Ramp Entry
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(P1, 0.12f);
            Gizmos.DrawWireSphere(P2, 0.12f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(P3, 0.18f); // Ramp Exit
        }
    }
}
