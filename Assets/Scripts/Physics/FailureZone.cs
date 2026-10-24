using System;
using UnityEngine;
using CurveShot.Core;

namespace CurveShot.Physics
{
    /// <summary>
    /// Boundary collider that detects projectiles falling off-screen or colliding with hazards.
    /// Protects against duplicate failure triggers.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class FailureZone : MonoBehaviour
    {
        [Header("Zone Type")]
        [SerializeField] private FailureReason failureReason = FailureReason.OutOfBounds;
        [SerializeField] private string debugZoneName = "Bottom Hazard Pit";

        public event Action<FailureReason, string> OnZoneEntered;

        private bool _hasTriggered = false;

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        public void ResetZone()
        {
            _hasTriggered = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasTriggered) return;

            ProjectileController projectile = other.GetComponent<ProjectileController>();
            if (projectile == null) return;

            _hasTriggered = true;
            projectile.Halt();

            string message = failureReason == FailureReason.OutOfBounds
                ? "Fell out of bounds!"
                : "Hit hazard obstacle!";

            OnZoneEntered?.Invoke(failureReason, message);
        }
    }
}
