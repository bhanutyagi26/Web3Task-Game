using System;
using UnityEngine;
using UnityEngine.UI;
using CurveShot.Core;
using CurveShot.Level;

namespace CurveShot.UI
{
    /// <summary>
    /// Manages the user interface, HUD indicators, power gauge, guidance hints,
    /// and victory/defeat modal dialogues with responsive layouts.
    /// </summary>
    public class UIController : MonoBehaviour
    {
        [Header("Top HUD Elements")]
        [SerializeField] private Text levelTitleText;
        [SerializeField] private Text shotCountText;
        [SerializeField] private Text levelHintText;
        [SerializeField] private Image powerFillBar;
        [SerializeField] private Text powerPercentText;
        [SerializeField] private Button muteButton;
        [SerializeField] private Text muteButtonText;

        [Header("Status Feedback Banner")]
        [SerializeField] private GameObject feedbackBannerObject;
        [SerializeField] private Text feedbackBannerText;

        [Header("Success Dialog Modal")]
        [SerializeField] private GameObject successModal;
        [SerializeField] private Text successStatsText;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button successReplayButton;

        [Header("Failure Dialog Modal")]
        [SerializeField] private GameObject failureModal;
        [SerializeField] private Text failureReasonText;
        [SerializeField] private Button retryButton;

        // UI Button Events
        public event Action OnRetryClicked;
        public event Action OnNextLevelClicked;
        public event Action OnMuteToggled;

        private void Awake()
        {
            if (retryButton != null) retryButton.onClick.AddListener(() => OnRetryClicked?.Invoke());
            if (nextLevelButton != null) nextLevelButton.onClick.AddListener(() => OnNextLevelClicked?.Invoke());
            if (successReplayButton != null) successReplayButton.onClick.AddListener(() => OnRetryClicked?.Invoke());
            if (muteButton != null) muteButton.onClick.AddListener(() => OnMuteToggled?.Invoke());

            HideAllModals();
            HideFeedbackBanner();
            UpdatePowerGauge(0f);
        }

        public void UpdateLevelDisplay(LevelConfig config, int levelIndex, int totalLevels)
        {
            if (levelTitleText != null)
            {
                levelTitleText.text = config.levelTitle;
            }

            if (levelHintText != null)
            {
                levelHintText.text = config.levelDescription;
            }
        }

        public void UpdateShotCount(int count)
        {
            if (shotCountText != null)
            {
                shotCountText.text = $"Attempt: {count}";
            }
        }

        public void UpdatePowerGauge(float normalizedPower)
        {
            normalizedPower = Mathf.Clamp01(normalizedPower);

            if (powerFillBar != null)
            {
                powerFillBar.fillAmount = normalizedPower;

                // Color shifts from Cyan -> Amber -> Coral
                if (normalizedPower < 0.5f)
                    powerFillBar.color = Color.Lerp(new Color(0.2f, 0.8f, 1f), new Color(1f, 0.85f, 0.2f), normalizedPower * 2f);
                else
                    powerFillBar.color = Color.Lerp(new Color(1f, 0.85f, 0.2f), new Color(1f, 0.35f, 0.35f), (normalizedPower - 0.5f) * 2f);
            }

            if (powerPercentText != null)
            {
                powerPercentText.text = $"{Mathf.RoundToInt(normalizedPower * 100f)}%";
            }
        }

        public void ShowFeedbackBanner(string message, Color color)
        {
            if (feedbackBannerObject != null)
            {
                feedbackBannerObject.SetActive(true);
            }

            if (feedbackBannerText != null)
            {
                feedbackBannerText.text = message;
                feedbackBannerText.color = color;
            }
        }

        public void HideFeedbackBanner()
        {
            if (feedbackBannerObject != null)
            {
                feedbackBannerObject.SetActive(false);
            }
        }

        public void ShowSuccessModal(int attempts, float speed, float angle)
        {
            HideFeedbackBanner();
            if (failureModal != null) failureModal.SetActive(false);

            if (successModal != null)
            {
                successModal.SetActive(true);
            }

            if (successStatsText != null)
            {
                successStatsText.text = $"Completed in {attempts} attempt(s)!\nEntry Speed: {speed:F1} m/s  |  Angle Deviation: {angle:F1}°";
            }
        }

        public void ShowFailureModal(FailureReason reason, string details)
        {
            HideFeedbackBanner();
            if (successModal != null) successModal.SetActive(false);

            if (failureModal != null)
            {
                failureModal.SetActive(true);
            }

            if (failureReasonText != null)
            {
                string header = reason switch
                {
                    FailureReason.IncorrectAngle => "ENTRY ANGLE TOO STEEP",
                    FailureReason.OverpoweredSpeed => "EXCESSIVE LAUNCH FORCE",
                    FailureReason.InsufficientSpeed => "INSUFFICIENT SPEED",
                    FailureReason.Undershot => "SHOT SHORT OF RAMP",
                    FailureReason.OutOfBounds => "OUT OF BOUNDS",
                    FailureReason.Timeout => "PROJECTILE STALLED",
                    _ => "SHOT MISSED TARGET"
                };

                failureReasonText.text = $"{header}\n\n<size=18>{details}</size>";
            }
        }

        public void HideAllModals()
        {
            if (successModal != null) successModal.SetActive(false);
            if (failureModal != null) failureModal.SetActive(false);
        }

        public void UpdateMuteButton(bool isMuted)
        {
            if (muteButtonText != null)
            {
                muteButtonText.text = isMuted ? "Sound: OFF" : "Sound: ON";
            }
        }
    }
}
