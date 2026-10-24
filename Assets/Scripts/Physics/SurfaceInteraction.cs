using System;
using UnityEngine;
using CurveShot.Core;

namespace CurveShot.Physics
{
    /// <summary>
    /// Evaluates projectile contact on the curved ramp against angle and velocity thresholds.
    /// Determines whether the shot is accepted for smooth traversal or rejected as a crash/failure.
    /// </summary>
    [RequireComponent(typeof(SurfacePath))]
    public class SurfaceInteraction : MonoBehaviour
    {
        [Header("Surface Reference")]
        [SerializeField] private SurfacePath surfacePath;

        [Header("Entry Validation Thresholds")]
        [Tooltip("Maximum allowed angular deviation between incoming velocity and ramp tangent (in degrees).")]
        [Range(5f, 45f)]
        [SerializeField] private float maxAngleToleranceDegrees = 26f;

        [Tooltip("Minimum incoming velocity magnitude required to enter ramp smoothly.")]
        [SerializeField] private float minEntrySpeed = 3.2f;

        [Tooltip("Maximum incoming velocity magnitude before excessive force causes a crash.")]
        [SerializeField] private float maxEntrySpeed = 17.5f;

        [Tooltip("Maximum parameter t along ramp considered the 'entry zone'. Contacts beyond this are mid-ramp hits.")]
        [Range(0.1f, 0.7f)]
        [SerializeField] private float entryZoneMaxT = 0.55f;

        [Header("Traversal Assist Settings")]
        [Tooltip("Slight downward adherence force to prevent micro-bounce while preserving natural physics.")]
        [SerializeField] private float surfaceAdherenceForce = 4.5f;

        // Events for decoupled listeners (GameManager, FeedbackController, UI)
        public event Action<Vector2, float, float> OnSuccessfulEntry; // contactPoint, incomingSpeed, angleDiff
        public event Action<Vector2, FailureReason, string> OnRejectedEntry; // contactPoint, reason, message

        private bool _isTraversing = false;
        private ProjectileController _activeProjectile;

        public float MaxAngleTolerance => maxAngleToleranceDegrees;
        public float MinEntrySpeed => minEntrySpeed;
        public float MaxEntrySpeed => maxEntrySpeed;

        private void Awake()
        {
            if (surfacePath == null) surfacePath = GetComponent<SurfacePath>();
        }

        /// <summary>
        /// Updates the tolerance thresholds for level difficulty tuning.
        /// </summary>
        public void ConfigureThresholds(float angleTolerance, float minSpeed, float maxSpeed)
        {
            maxAngleToleranceDegrees = angleTolerance;
            minEntrySpeed = minSpeed;
            maxEntrySpeed = maxSpeed;
        }

        /// <summary>
        /// Resets the interaction state for a new shot attempt.
        /// </summary>
        public void ResetInteraction()
        {
            _isTraversing = false;
            _activeProjectile = null;
            if (surfacePath != null)
            {
                surfacePath.SetHighlight(false);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Only process projectile collisions
            ProjectileController projectile = collision.collider.GetComponent<ProjectileController>();
            if (projectile == null) return;

            // Prevent duplicate validation once traversal is locked
            if (_isTraversing) return;

            ContactPoint2D contact = collision.GetContact(0);
            Vector2 contactPoint = contact.point;

            // Determine closest parameter t and local ramp tangent
            float contactT = surfacePath.GetClosestT(contactPoint, out Vector2 closestCurvePoint);
            Vector2 rampTangent = surfacePath.GetTangentAt(contactT);

            // Retrieve the incoming velocity right before collision impact
            Vector2 incomingVelocity = projectile.PreCollisionVelocity;
            float incomingSpeed = incomingVelocity.magnitude;

            // If incoming velocity is somehow zero or negligible, fallback to current velocity
            if (incomingSpeed < 0.1f)
            {
                incomingVelocity = projectile.CurrentVelocity;
                incomingSpeed = incomingVelocity.magnitude;
            }

            // Calculate directional angle between projectile approach vector and ramp tangent
            // Ramp tangent points in the intended direction of travel (down the slope)
            float angleDeviation = Vector2.Angle(incomingVelocity.normalized, rampTangent.normalized);

            // Detailed validation logic:
            // 1. Check entry region: Was the contact in the initial landing zone of the ramp?
            if (contactT > entryZoneMaxT)
            {
                // Ball slammed directly into the middle/lower slope without entering the crest
                RejectContact(projectile, contactPoint, FailureReason.IncorrectAngle,
                    $"Mid-Ramp Impact (t={contactT:F2})! Missed the entry crest.");
                return;
            }

            // 2. Check velocity magnitude: Insufficient speed
            if (incomingSpeed < minEntrySpeed)
            {
                RejectContact(projectile, contactPoint, FailureReason.InsufficientSpeed,
                    $"Too Slow ({incomingSpeed:F1} m/s)! Min required: {minEntrySpeed:F1} m/s.");
                return;
            }

            // 3. Check velocity magnitude: Excessive speed
            if (incomingSpeed > maxEntrySpeed)
            {
                RejectContact(projectile, contactPoint, FailureReason.OverpoweredSpeed,
                    $"Overpowered ({incomingSpeed:F1} m/s)! Max allowed: {maxEntrySpeed:F1} m/s.");
                return;
            }

            // 4. Check approach angle deviation vs ramp tangent tolerance
            if (angleDeviation > maxAngleToleranceDegrees)
            {
                RejectContact(projectile, contactPoint, FailureReason.IncorrectAngle,
                    $"Steep Entry ({angleDeviation:F1}°)! Allowed tolerance: ±{maxAngleToleranceDegrees:F1}°.");
                return;
            }

            // --- ALL THRESHOLDS SATISFIED: SUCCESSFUL RAMP ENTRY ---
            AcceptContact(projectile, contactPoint, rampTangent, incomingSpeed, angleDeviation);
        }

        private void AcceptContact(
            ProjectileController projectile,
            Vector2 contactPoint,
            Vector2 rampTangent,
            float incomingSpeed,
            float angleDeviation)
        {
            _isTraversing = true;
            _activeProjectile = projectile;

            // Highlight the ramp visual to signal valid interaction
            surfacePath.SetHighlight(true);

            // Align velocity cleanly along ramp tangent while conserving momentum
            Vector2 alignedVelocity = rampTangent * incomingSpeed;
            projectile.AlignVelocityToTangent(alignedVelocity);

            // Inform listeners
            OnSuccessfulEntry?.Invoke(contactPoint, incomingSpeed, angleDeviation);
        }

        private void RejectContact(
            ProjectileController projectile,
            Vector2 contactPoint,
            FailureReason reason,
            string feedbackMessage)
        {
            _isTraversing = false;

            // Let physics produce a natural crash/ricochet bounce
            projectile.TriggerRejectionDeflection();

            // Inform listeners
            OnRejectedEntry?.Invoke(contactPoint, reason, feedbackMessage);
        }

        private void FixedUpdate()
        {
            // If traversing along ramp, apply a subtle normal adherence force toward the curve
            // to ensure continuous, butter-smooth roll along the EdgeCollider without airborne skipping
            if (_isTraversing && _activeProjectile != null && _activeProjectile.IsActive)
            {
                float t = surfacePath.GetClosestT(_activeProjectile.Position, out Vector2 closestPoint);
                Vector2 normal = surfacePath.GetNormalAt(t);

                // Downward force towards surface (opposite of outwards normal)
                _activeProjectile.ApplyAdherenceForce(-normal * surfaceAdherenceForce);
            }
        }
    }
}
