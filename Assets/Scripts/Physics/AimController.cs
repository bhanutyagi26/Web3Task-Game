using System;
using UnityEngine;
using CurveShot.Core;
using CurveShot.UI;

namespace CurveShot.Physics
{
    /// <summary>
    /// Handles drag-and-release aiming via mobile touch and desktop mouse.
    /// Calculates launch force vector, clamps angles, and drives trajectory preview.
    /// </summary>
    public class AimController : MonoBehaviour
    {
        [Header("Launch Force Parameters")]
        [SerializeField] private float minLaunchSpeed = 3.5f;
        [SerializeField] private float maxLaunchSpeed = 23.5f;
        [SerializeField] private float maxDragDistance = 3.6f;

        [Header("Angle Clamping (Degrees from Horizontal)")]
        [Range(5f, 45f)]
        [SerializeField] private float minLaunchAngle = 12f;
        [Range(45f, 85f)]
        [SerializeField] private float maxLaunchAngle = 78f;

        [Header("Deadzone & Safety")]
        [SerializeField] private float minDragDeadzone = 0.25f;

        [Header("References")]
        [SerializeField] private ProjectileController projectile;
        [SerializeField] private TrajectoryPreview trajectoryPreview;
        [SerializeField] private Camera mainCamera;

        public event Action OnAimStarted;
        public event Action<float> OnAimUpdated; // normalized power [0, 1]
        public event Action OnAimCancelled;
        public event Action<Vector2> OnLaunchReleased; // launchVelocity

        private bool _isAiming = false;
        private bool _inputLocked = false;
        private Vector2 _dragStartWorldPos;
        private Vector2 _currentDragWorldPos;
        private Vector2 _calculatedLaunchVelocity;

        public bool IsAiming => _isAiming;
        public Vector2 CalculatedLaunchVelocity => _calculatedLaunchVelocity;
        public float MinLaunchSpeed => minLaunchSpeed;
        public float MaxLaunchSpeed => maxLaunchSpeed;

        private void Awake()
        {
            if (mainCamera == null) mainCamera = Camera.main;
        }

        public void SetInputLocked(bool locked)
        {
            _inputLocked = locked;
            if (locked && _isAiming)
            {
                CancelAim();
            }
        }

        private void Update()
        {
            if (_inputLocked) return;

            ProcessInput();
        }

        private void ProcessInput()
        {
            // Touch input handling (Mobile Android)
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                Vector2 touchWorldPos = GetWorldPosition(touch.position);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        TryBeginAim(touchWorldPos);
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        if (_isAiming) UpdateAim(touchWorldPos);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (_isAiming) ReleaseAim(touchWorldPos);
                        break;
                }
                return;
            }

            // Mouse input fallback (Editor / Desktop testing)
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mouseWorldPos = GetWorldPosition(Input.mousePosition);
                TryBeginAim(mouseWorldPos);
            }
            else if (Input.GetMouseButton(0) && _isAiming)
            {
                Vector2 mouseWorldPos = GetWorldPosition(Input.mousePosition);
                UpdateAim(mouseWorldPos);
            }
            else if (Input.GetMouseButtonUp(0) && _isAiming)
            {
                Vector2 mouseWorldPos = GetWorldPosition(Input.mousePosition);
                ReleaseAim(mouseWorldPos);
            }
        }

        private Vector2 GetWorldPosition(Vector3 screenPos)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            screenPos.z = 10f; // Distance from camera
            return mainCamera.ScreenToWorldPoint(screenPos);
        }

        private void TryBeginAim(Vector2 inputWorldPos)
        {
            if (projectile == null) return;

            // Allow initiating aim anywhere near the projectile or screen left
            float distToBall = Vector2.Distance(inputWorldPos, projectile.Position);
            if (distToBall > 4.5f && inputWorldPos.x > 0f)
            {
                // Disallow aiming if clicking far away on the right side of screen
                return;
            }

            _isAiming = true;
            _dragStartWorldPos = projectile.Position;
            _currentDragWorldPos = inputWorldPos;

            OnAimStarted?.Invoke();
            UpdateAim(inputWorldPos);
        }

        private void UpdateAim(Vector2 currentWorldPos)
        {
            _currentDragWorldPos = currentWorldPos;

            // Slingshot vector: pulling down/left launches up/right
            Vector2 dragVector = (Vector2)projectile.Position - currentWorldPos;
            float dragDistance = dragVector.magnitude;

            if (dragDistance < minDragDeadzone)
            {
                // Inside deadzone
                _calculatedLaunchVelocity = Vector2.zero;
                if (trajectoryPreview != null) trajectoryPreview.Hide();
                OnAimUpdated?.Invoke(0f);
                return;
            }

            // Compute and clamp angle
            float rawAngle = Mathf.Atan2(dragVector.y, dragVector.x) * Mathf.Rad2Deg;
            float clampedAngle = Mathf.Clamp(rawAngle, minLaunchAngle, maxLaunchAngle);
            float clampedRad = clampedAngle * Mathf.Deg2Rad;
            Vector2 launchDirection = new Vector2(Mathf.Cos(clampedRad), Mathf.Sin(clampedRad));

            // Compute power ratio [0, 1]
            float clampedDrag = Mathf.Clamp(dragDistance, 0f, maxDragDistance);
            float powerFraction = clampedDrag / maxDragDistance;

            float speed = Mathf.Lerp(minLaunchSpeed, maxLaunchSpeed, powerFraction);
            _calculatedLaunchVelocity = launchDirection * speed;

            // Update Trajectory Preview
            if (trajectoryPreview != null)
            {
                trajectoryPreview.ShowTrajectory(projectile.Position, _calculatedLaunchVelocity, projectile.Rigidbody.gravityScale);
            }

            OnAimUpdated?.Invoke(powerFraction);
        }

        private void ReleaseAim(Vector2 releaseWorldPos)
        {
            _isAiming = false;

            if (trajectoryPreview != null)
            {
                trajectoryPreview.Hide();
            }

            // Safety check: cancel launch if within deadzone
            if (_calculatedLaunchVelocity.magnitude < minLaunchSpeed)
            {
                CancelAim();
                return;
            }

            // Fire launch
            Vector2 launchVelocity = _calculatedLaunchVelocity;
            _calculatedLaunchVelocity = Vector2.zero;

            OnLaunchReleased?.Invoke(launchVelocity);
        }

        public void CancelAim()
        {
            _isAiming = false;
            _calculatedLaunchVelocity = Vector2.zero;

            if (trajectoryPreview != null)
            {
                trajectoryPreview.Hide();
            }

            OnAimCancelled?.Invoke();
        }
    }
}
