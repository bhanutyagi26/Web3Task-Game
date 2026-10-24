using System.Collections;
using UnityEngine;

namespace CurveShot.Feedback
{
    /// <summary>
    /// Orchestrates visual and physical motion feedback:
    /// 1. Launch pulse / squash & stretch
    /// 2. Impact sparks & visual contact pulse
    /// 3. Sliding trail and ramp highlight
    /// 4. Camera shake and failure flash
    /// </summary>
    public class FeedbackController : MonoBehaviour
    {
        [Header("Camera & Screen Shake")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float shakeDuration = 0.28f;
        [SerializeField] private float shakeMagnitude = 0.22f;

        [Header("Particle Systems")]
        [SerializeField] private ParticleSystem launchParticles;
        [SerializeField] private ParticleSystem impactParticles;
        [SerializeField] private ParticleSystem successCelebrationParticles;
        [SerializeField] private ParticleSystem failureShatterParticles;

        [Header("Projectile Transform for Squashing")]
        [SerializeField] private Transform projectileTransform;

        private Vector3 _originalCameraPos;
        private Coroutine _shakeCoroutine;
        private Coroutine _squashCoroutine;

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera != null)
            {
                _originalCameraPos = targetCamera.transform.position;
            }
        }

        /// <summary>
        /// Triggered on launch: brief squash-and-stretch scale pulse + launch particle puff.
        /// </summary>
        public void PlayLaunchFeedback(Vector2 launchVelocity)
        {
            if (launchParticles != null && projectileTransform != null)
            {
                launchParticles.transform.position = projectileTransform.position;
                launchParticles.Play();
            }

            if (projectileTransform != null)
            {
                if (_squashCoroutine != null) StopCoroutine(_squashCoroutine);
                _squashCoroutine = StartCoroutine(AnimateLaunchSquash(launchVelocity.normalized));
            }
        }

        private IEnumerator AnimateLaunchSquash(Vector2 direction)
        {
            Vector3 originalScale = Vector3.one * 0.7f;
            // Stretch along launch direction, squash across
            Vector3 stretchScale = new Vector3(originalScale.x * 1.35f, originalScale.y * 0.75f, 1f);

            float elapsed = 0f;
            float duration = 0.16f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Animate from stretch back to normal
                if (projectileTransform != null)
                {
                    projectileTransform.localScale = Vector3.Lerp(stretchScale, originalScale, t);
                }
                yield return null;
            }

            if (projectileTransform != null)
            {
                projectileTransform.localScale = originalScale;
            }
        }

        /// <summary>
        /// Triggered on valid ramp impact: sparks at contact point and subtle pulse.
        /// </summary>
        public void PlayRampImpactFeedback(Vector2 contactPoint)
        {
            if (impactParticles != null)
            {
                impactParticles.transform.position = contactPoint;
                impactParticles.Play();
            }
        }

        /// <summary>
        /// Triggered on successful goal arrival: confetti / celebration particle blast.
        /// </summary>
        public void PlaySuccessFeedback(Vector2 goalPosition)
        {
            if (successCelebrationParticles != null)
            {
                successCelebrationParticles.transform.position = goalPosition;
                successCelebrationParticles.Play();
            }
        }

        /// <summary>
        /// Triggered on failure: camera shake + shatter burst.
        /// </summary>
        public void PlayFailureFeedback(Vector2 failPosition)
        {
            if (failureShatterParticles != null)
            {
                failureShatterParticles.transform.position = failPosition;
                failureShatterParticles.Play();
            }

            TriggerCameraShake();
        }

        public void TriggerCameraShake()
        {
            if (targetCamera == null) return;

            if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(AnimateCameraShake());
        }

        private IEnumerator AnimateCameraShake()
        {
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float decay = 1f - (elapsed / shakeDuration);
                float xOffset = (Random.value * 2f - 1f) * shakeMagnitude * decay;
                float yOffset = (Random.value * 2f - 1f) * shakeMagnitude * decay;

                targetCamera.transform.position = new Vector3(
                    _originalCameraPos.x + xOffset,
                    _originalCameraPos.y + yOffset,
                    _originalCameraPos.z);

                yield return null;
            }

            targetCamera.transform.position = _originalCameraPos;
        }

        public void ResetFeedback()
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                if (targetCamera != null) targetCamera.transform.position = _originalCameraPos;
            }

            if (_squashCoroutine != null)
            {
                StopCoroutine(_squashCoroutine);
                if (projectileTransform != null) projectileTransform.localScale = Vector3.one * 0.7f;
            }

            if (launchParticles != null) launchParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (impactParticles != null) impactParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (successCelebrationParticles != null) successCelebrationParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (failureShatterParticles != null) failureShatterParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
