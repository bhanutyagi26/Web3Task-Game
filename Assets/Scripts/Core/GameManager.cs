using System.Collections;
using UnityEngine;
using CurveShot.Physics;
using CurveShot.Level;
using CurveShot.UI;
using CurveShot.Feedback;
using CurveShot.Audio;

namespace CurveShot.Core
{
    /// <summary>
    /// Master coordinator for the Curve Shot game loop.
    /// Manages state transitions, level flow, score/attempts, and watchdog timeouts.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("State Tracking")]
        [SerializeField] private GameState currentState = GameState.Ready;
        [SerializeField] private int currentAttempts = 0;
        [SerializeField] private float flightTimeoutSeconds = 6.0f;

        [Header("Subsystem References")]
        [SerializeField] private ProjectileController projectile;
        [SerializeField] private AimController aimController;
        [SerializeField] private SurfaceInteraction surfaceInteraction;
        [SerializeField] private GoalZone goalZone;
        [SerializeField] private LevelController levelController;
        [SerializeField] private UIController uiController;
        [SerializeField] private FeedbackController feedbackController;
        [SerializeField] private AudioController audioController;
        [SerializeField] private FailureZone[] failureZones;

        public GameState CurrentState => currentState;
        public int CurrentAttempts => currentAttempts;

        private Coroutine _watchdogCoroutine;
        private float _lastEntrySpeed = 0f;
        private float _lastEntryAngle = 0f;

        private void Start()
        {
            SubscribeEvents();

            if (levelController != null)
            {
                levelController.LoadLevel(0);
            }
            else
            {
                StartNewAttempt();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (aimController != null)
            {
                aimController.OnAimStarted += HandleAimStarted;
                aimController.OnAimUpdated += HandleAimUpdated;
                aimController.OnAimCancelled += HandleAimCancelled;
                aimController.OnLaunchReleased += HandleLaunchReleased;
            }

            if (surfaceInteraction != null)
            {
                surfaceInteraction.OnSuccessfulEntry += HandleRampEntrySuccess;
                surfaceInteraction.OnRejectedEntry += HandleRampEntryFailed;
            }

            if (goalZone != null)
            {
                goalZone.OnGoalAchieved += HandleGoalAchieved;
            }

            if (failureZones != null)
            {
                foreach (var fz in failureZones)
                {
                    if (fz != null) fz.OnZoneEntered += HandleFailureZoneEntered;
                }
            }

            if (levelController != null)
            {
                levelController.OnLevelLoaded += HandleLevelLoaded;
            }

            if (uiController != null)
            {
                uiController.OnRetryClicked += HandleRetryRequested;
                uiController.OnNextLevelClicked += HandleNextLevelRequested;
                uiController.OnMuteToggled += HandleMuteToggled;
            }
        }

        private void UnsubscribeEvents()
        {
            if (aimController != null)
            {
                aimController.OnAimStarted -= HandleAimStarted;
                aimController.OnAimUpdated -= HandleAimUpdated;
                aimController.OnAimCancelled -= HandleAimCancelled;
                aimController.OnLaunchReleased -= HandleLaunchReleased;
            }

            if (surfaceInteraction != null)
            {
                surfaceInteraction.OnSuccessfulEntry -= HandleRampEntrySuccess;
                surfaceInteraction.OnRejectedEntry -= HandleRampEntryFailed;
            }

            if (goalZone != null)
            {
                goalZone.OnGoalAchieved -= HandleGoalAchieved;
            }

            if (failureZones != null)
            {
                foreach (var fz in failureZones)
                {
                    if (fz != null) fz.OnZoneEntered -= HandleFailureZoneEntered;
                }
            }

            if (levelController != null)
            {
                levelController.OnLevelLoaded -= HandleLevelLoaded;
            }

            if (uiController != null)
            {
                uiController.OnRetryClicked -= HandleRetryRequested;
                uiController.OnNextLevelClicked -= HandleNextLevelRequested;
                uiController.OnMuteToggled -= HandleMuteToggled;
            }
        }

        private void HandleLevelLoaded(LevelConfig config, int levelIndex)
        {
            currentAttempts = 0;
            if (uiController != null)
            {
                uiController.UpdateLevelDisplay(config, levelIndex, levelController.TotalLevels);
                uiController.UpdateShotCount(currentAttempts);
            }

            StartNewAttempt();
        }

        /// <summary>
        /// Prepares the projectile and components for an aim attempt.
        /// </summary>
        public void StartNewAttempt()
        {
            StopWatchdog();

            currentState = GameState.Ready;

            Vector2 spawnPos = (levelController != null && levelController.CurrentLevel != null)
                ? levelController.CurrentLevel.launchSpawnPoint
                : new Vector2(-7.8f, -1.2f);

            if (projectile != null)
            {
                projectile.ResetToPosition(spawnPos);
            }

            if (surfaceInteraction != null)
            {
                surfaceInteraction.ResetInteraction();
            }

            if (goalZone != null)
            {
                goalZone.ResetGoal();
            }

            if (failureZones != null)
            {
                foreach (var fz in failureZones)
                {
                    if (fz != null) fz.ResetZone();
                }
            }

            if (feedbackController != null)
            {
                feedbackController.ResetFeedback();
            }

            if (uiController != null)
            {
                uiController.HideAllModals();
                uiController.HideFeedbackBanner();
                uiController.UpdatePowerGauge(0f);
            }

            if (aimController != null)
            {
                aimController.SetInputLocked(false);
            }
        }

        private void HandleAimStarted()
        {
            if (currentState != GameState.Ready) return;
            currentState = GameState.Aiming;
        }

        private void HandleAimUpdated(float powerRatio)
        {
            if (currentState != GameState.Aiming) return;
            if (uiController != null)
            {
                uiController.UpdatePowerGauge(powerRatio);
            }
        }

        private void HandleAimCancelled()
        {
            if (currentState == GameState.Aiming)
            {
                currentState = GameState.Ready;
                if (uiController != null)
                {
                    uiController.UpdatePowerGauge(0f);
                }
            }
        }

        private void HandleLaunchReleased(Vector2 launchVelocity)
        {
            if (currentState != GameState.Aiming && currentState != GameState.Ready) return;

            currentState = GameState.InFlight;
            currentAttempts++;

            if (aimController != null)
            {
                aimController.SetInputLocked(true);
            }

            if (uiController != null)
            {
                uiController.UpdateShotCount(currentAttempts);
                uiController.UpdatePowerGauge(0f);
            }

            if (projectile != null)
            {
                projectile.Launch(launchVelocity);
            }

            if (feedbackController != null)
            {
                feedbackController.PlayLaunchFeedback(launchVelocity);
            }

            if (audioController != null)
            {
                audioController.PlayLaunchSound();
            }

            // Start watchdog timer to prevent indefinite stall
            StartWatchdog();
        }

        private void HandleRampEntrySuccess(Vector2 contactPoint, float incomingSpeed, float angleDiff)
        {
            if (currentState != GameState.InFlight) return;

            currentState = GameState.OnRamp;
            _lastEntrySpeed = incomingSpeed;
            _lastEntryAngle = angleDiff;

            if (feedbackController != null)
            {
                feedbackController.PlayRampImpactFeedback(contactPoint);
            }

            if (audioController != null)
            {
                audioController.PlayRampImpactSound();
            }

            if (uiController != null)
            {
                uiController.ShowFeedbackBanner("PERFECT GLIDE! Surface Acquired", new Color(0.2f, 1f, 0.5f));
            }
        }

        private void HandleRampEntryFailed(Vector2 contactPoint, FailureReason reason, string message)
        {
            if (currentState != GameState.InFlight && currentState != GameState.OnRamp) return;

            TriggerFailure(reason, message, contactPoint);
        }

        private void HandleGoalAchieved(Vector2 goalPosition)
        {
            if (currentState != GameState.InFlight && currentState != GameState.OnRamp) return;

            StopWatchdog();
            currentState = GameState.Success;

            if (feedbackController != null)
            {
                feedbackController.PlaySuccessFeedback(goalPosition);
            }

            if (audioController != null)
            {
                audioController.PlaySuccessSound();
            }

            if (uiController != null)
            {
                uiController.ShowSuccessModal(currentAttempts, _lastEntrySpeed, _lastEntryAngle);
            }
        }

        private void HandleFailureZoneEntered(FailureReason reason, string message)
        {
            if (currentState != GameState.InFlight && currentState != GameState.OnRamp) return;

            Vector2 failPos = projectile != null ? projectile.Position : Vector2.zero;
            TriggerFailure(reason, message, failPos);
        }

        private void TriggerFailure(FailureReason reason, string details, Vector2 failPosition)
        {
            StopWatchdog();

            currentState = GameState.Failed;

            if (projectile != null)
            {
                projectile.Halt();
            }

            if (feedbackController != null)
            {
                feedbackController.PlayFailureFeedback(failPosition);
            }

            if (audioController != null)
            {
                audioController.PlayFailureSound();
            }

            if (uiController != null)
            {
                uiController.ShowFailureModal(reason, details);
            }
        }

        private void HandleRetryRequested()
        {
            StartNewAttempt();
        }

        private void HandleNextLevelRequested()
        {
            if (levelController != null)
            {
                levelController.NextLevel();
            }
            else
            {
                StartNewAttempt();
            }
        }

        private void HandleMuteToggled()
        {
            if (audioController != null)
            {
                audioController.ToggleMute();
                if (uiController != null)
                {
                    uiController.UpdateMuteButton(audioController.IsMuted);
                }
            }
        }

        private void StartWatchdog()
        {
            StopWatchdog();
            _watchdogCoroutine = StartCoroutine(WatchdogTimer());
        }

        private void StopWatchdog()
        {
            if (_watchdogCoroutine != null)
            {
                StopCoroutine(_watchdogCoroutine);
                _watchdogCoroutine = null;
            }
        }

        private IEnumerator WatchdogTimer()
        {
            yield return new WaitForSeconds(flightTimeoutSeconds);

            if (currentState == GameState.InFlight || currentState == GameState.OnRamp)
            {
                Vector2 pos = projectile != null ? projectile.Position : Vector2.zero;
                TriggerFailure(FailureReason.Timeout, "Flight duration exceeded limit. Shot stalled.", pos);
            }
        }
    }
}
