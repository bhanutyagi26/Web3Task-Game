using UnityEngine;
using CurveShot.Physics;

namespace CurveShot.UI
{
    /// <summary>
    /// Computes and renders a real-time kinematic trajectory preview line.
    /// Synchronized with Unity's 2D gravity and projectile initial velocity.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class TrajectoryPreview : MonoBehaviour
    {
        [Header("Trajectory Settings")]
        [SerializeField] private int pointCount = 55;
        [SerializeField] private float timeStep = 0.040f;
        [SerializeField] private float startWidth = 0.12f;
        [SerializeField] private float endWidth = 0.04f;

        [Header("Colors")]
        [SerializeField] private Color startColor = new Color(1f, 0.85f, 0.2f, 0.9f); // Bright Gold
        [SerializeField] private Color endColor = new Color(1f, 0.5f, 0.1f, 0.15f); // Faded Orange

        private LineRenderer _lineRenderer;
        private Vector3[] _trajectoryPoints;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _trajectoryPoints = new Vector3[pointCount];

            _lineRenderer.positionCount = pointCount;
            _lineRenderer.startWidth = startWidth;
            _lineRenderer.endWidth = endWidth;
            _lineRenderer.startColor = startColor;
            _lineRenderer.endColor = endColor;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.enabled = false;
        }

        /// <summary>
        /// Recalculates and renders the trajectory prediction.
        /// </summary>
        public void ShowTrajectory(Vector2 startPosition, Vector2 launchVelocity, float gravityScale)
        {
            if (_lineRenderer == null) return;

            Vector2 effectiveGravity = UnityEngine.Physics2D.gravity * gravityScale;

            BezierMath.CalculateTrajectoryPoints(
                startPosition,
                launchVelocity,
                effectiveGravity,
                timeStep,
                pointCount,
                _trajectoryPoints);

            _lineRenderer.enabled = true;
            _lineRenderer.SetPositions(_trajectoryPoints);
        }

        /// <summary>
        /// Hides the preview line once launched or aim is cancelled.
        /// </summary>
        public void Hide()
        {
            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = false;
            }
        }
    }
}
