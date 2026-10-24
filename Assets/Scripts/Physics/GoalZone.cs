using System;
using UnityEngine;

namespace CurveShot.Physics
{
    /// <summary>
    /// Detects when the projectile reaches the designated collection box / target area.
    /// Protects against duplicate triggers and signals successful level completion.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GoalZone : MonoBehaviour
    {
        [Header("Target Box Settings")]
        [SerializeField] private ParticleSystem celebrationParticles;
        [SerializeField] private SpriteRenderer targetGlowSprite;
        [SerializeField] private Color activeGlowColor = new Color(0.2f, 1f, 0.4f, 0.8f);

        public event Action<Vector2> OnGoalAchieved;

        private bool _isGoalTriggered = false;

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        public void ResetGoal()
        {
            _isGoalTriggered = false;
            if (targetGlowSprite != null)
            {
                targetGlowSprite.color = new Color(1f, 1f, 1f, 0.2f);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isGoalTriggered) return;

            ProjectileController projectile = other.GetComponent<ProjectileController>();
            if (projectile == null) return;

            _isGoalTriggered = true;

            // Softly settle projectile inside the collection box
            projectile.Halt();

            if (celebrationParticles != null)
            {
                celebrationParticles.Play();
            }

            if (targetGlowSprite != null)
            {
                targetGlowSprite.color = activeGlowColor;
            }

            OnGoalAchieved?.Invoke(transform.position);
        }
    }
}
