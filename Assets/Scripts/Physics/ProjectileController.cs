using System;
using UnityEngine;
using CurveShot.Core;

namespace CurveShot.Physics
{
    /// <summary>
    /// Controls the projectile's Rigidbody2D physics, launch dynamics, velocity tracking,
    /// and visual trail representation.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class ProjectileController : MonoBehaviour
    {
        [Header("Physics Properties")]
        [SerializeField] private float mass = 1.0f;
        [SerializeField] private float linearDrag = 0.05f;
        [SerializeField] private float gravityScale = 1.6f;

        [Header("Visual Components")]
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Rigidbody2D _rb;
        private CircleCollider2D _collider;
        private Vector2 _preCollisionVelocity;
        private bool _isActive = false;

        public Vector2 PreCollisionVelocity => _preCollisionVelocity;
        public Vector2 CurrentVelocity => _rb != null ? _rb.velocity : Vector2.zero;
        public Vector2 Position => transform.position;
        public bool IsActive => _isActive;
        public Rigidbody2D Rigidbody => _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<CircleCollider2D>();
            if (trailRenderer == null) trailRenderer = GetComponent<TrailRenderer>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            ConfigurePhysicsSettings();
            SetPhysicsSimulated(false);
        }

        private void ConfigurePhysicsSettings()
        {
            if (_rb == null) return;

            _rb.mass = mass;
            _rb.drag = linearDrag;
            _rb.gravityScale = gravityScale;
            // Continuous collision detection is essential to eliminate tunneling at high velocities
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void FixedUpdate()
        {
            if (_isActive && _rb != null && _rb.simulated)
            {
                // Cache velocity each physics step BEFORE collisions occur
                _preCollisionVelocity = _rb.velocity;
            }
        }

        /// <summary>
        /// Activates physics simulation and applies the initial launch velocity vector.
        /// </summary>
        public void Launch(Vector2 initialVelocity)
        {
            _isActive = true;
            SetPhysicsSimulated(true);

            _rb.velocity = initialVelocity;
            _preCollisionVelocity = initialVelocity;

            if (trailRenderer != null)
            {
                trailRenderer.Clear();
                trailRenderer.emitting = true;
            }
        }

        /// <summary>
        /// Smoothly redirects projectile velocity along the ramp tangent upon valid entry.
        /// </summary>
        public void AlignVelocityToTangent(Vector2 alignedVelocity)
        {
            if (_rb == null) return;
            _rb.velocity = alignedVelocity;
        }

        /// <summary>
        /// Applies an adherence force to keep the projectile grounded against curved surface.
        /// </summary>
        public void ApplyAdherenceForce(Vector2 force)
        {
            if (_rb != null && _rb.simulated)
            {
                _rb.AddForce(force, ForceMode2D.Force);
            }
        }

        /// <summary>
        /// Applies an unstable deflection impulse upon failed entry or excessive speed.
        /// </summary>
        public void TriggerRejectionDeflection()
        {
            if (_rb == null) return;
            // Deflect outwards and damp forward speed to produce a visible bounce/tumble
            Vector2 deflection = new Vector2(-Mathf.Abs(_preCollisionVelocity.x) * 0.4f, 3.5f);
            _rb.velocity = deflection;
        }

        /// <summary>
        /// Resets the projectile to its launch position and zeroes physics momentum.
        /// </summary>
        public void ResetToPosition(Vector2 spawnPosition)
        {
            _isActive = false;
            SetPhysicsSimulated(false);

            transform.position = spawnPosition;
            transform.rotation = Quaternion.identity;

            if (_rb != null)
            {
                _rb.velocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }

            _preCollisionVelocity = Vector2.zero;

            if (trailRenderer != null)
            {
                trailRenderer.emitting = false;
                trailRenderer.Clear();
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Halts motion upon terminal success or failure.
        /// </summary>
        public void Halt()
        {
            _isActive = false;
            if (_rb != null)
            {
                _rb.velocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }
        }

        private void SetPhysicsSimulated(bool simulated)
        {
            if (_rb != null)
            {
                _rb.simulated = simulated;
                _rb.isKinematic = !simulated;
            }
        }
    }
}
