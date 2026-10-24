using System;
using System.Collections.Generic;
using UnityEngine;
using CurveShot.Physics;

namespace CurveShot.Level
{
    /// <summary>
    /// Manages level loading, geometry reconfiguration, and difficulty progression.
    /// Supports at least 2 distinct levels with unique ramp curves, entry tolerances, and goal targets.
    /// </summary>
    public class LevelController : MonoBehaviour
    {
        [Header("Components to Configure")]
        [SerializeField] private SurfacePath surfacePath;
        [SerializeField] private SurfaceInteraction surfaceInteraction;
        [SerializeField] private Transform launchPlatform;
        [SerializeField] private Transform goalBoxTransform;
        [SerializeField] private GoalZone goalZone;
        [SerializeField] private GameObject obstacleObject;

        [Header("Levels Data")]
        [SerializeField] private List<LevelConfig> levels = new List<LevelConfig>();
        [SerializeField] private int currentLevelIndex = 0;

        public event Action<LevelConfig, int> OnLevelLoaded;

        public int CurrentLevelIndex => currentLevelIndex;
        public int TotalLevels => levels.Count;
        public LevelConfig CurrentLevel => (currentLevelIndex >= 0 && currentLevelIndex < levels.Count) ? levels[currentLevelIndex] : null;

        private void Awake()
        {
            if (levels == null || levels.Count == 0)
            {
                CreateDefaultLevels();
            }
        }

        private void Start()
        {
            LoadLevel(currentLevelIndex);
        }

        /// <summary>
        /// Populates the 3 distinct level configurations if none are assigned.
        /// </summary>
        public void CreateDefaultLevels()
        {
            levels = new List<LevelConfig>
            {
                // Level 1: Training Curve (Gentle introductory slope, wide margin)
                new LevelConfig
                {
                    levelTitle = "Level 1: Training Curve",
                    levelDescription = "Learn trajectory & drag force. Aim for the elevated ramp crest.",
                    launchSpawnPoint = new Vector2(-7.8f, -1.2f),
                    launchPadSize = new Vector2(2.6f, 0.45f),
                    p0 = new Vector2(0.5f, 2.6f),
                    p1 = new Vector2(2.4f, 2.2f),
                    p2 = new Vector2(4.6f, -0.6f),
                    p3 = new Vector2(6.2f, -3.2f),
                    angleTolerance = 30f,
                    minEntrySpeed = 3.0f,
                    maxEntrySpeed = 17.0f,
                    goalPosition = new Vector2(6.5f, -3.6f),
                    goalScale = new Vector2(2.4f, 1.5f),
                    hasObstacle = false
                },

                // Level 2: Precision Curve (Steeper crest, comfortable angle window, broad speed range)
                new LevelConfig
                {
                    levelTitle = "Level 2: Precision Curve",
                    levelDescription = "Steeper approach angle. Control your launch speed to glide into the curved slope.",
                    launchSpawnPoint = new Vector2(-8.2f, -1.8f),
                    launchPadSize = new Vector2(2.2f, 0.45f),
                    p0 = new Vector2(1.2f, 3.6f),
                    p1 = new Vector2(2.8f, 3.1f),
                    p2 = new Vector2(5.0f, 0.2f),
                    p3 = new Vector2(6.6f, -3.4f),
                    angleTolerance = 25f,
                    minEntrySpeed = 3.5f,
                    maxEntrySpeed = 17.5f,
                    goalPosition = new Vector2(6.9f, -3.8f),
                    goalScale = new Vector2(2.2f, 1.5f),
                    hasObstacle = false
                },

                // Level 3: Apex Challenge (Clear hurdle and smooth high-speed ramp glide)
                new LevelConfig
                {
                    levelTitle = "Level 3: Apex Challenge",
                    levelDescription = "Clear the central barrier hurdle and execute a smooth high-speed ramp glide into the goal!",
                    launchSpawnPoint = new Vector2(-8.5f, -2.0f),
                    launchPadSize = new Vector2(2.0f, 0.45f),
                    p0 = new Vector2(1.8f, 3.8f),
                    p1 = new Vector2(3.5f, 3.4f),
                    p2 = new Vector2(5.5f, 0.6f),
                    p3 = new Vector2(6.8f, -3.5f),
                    angleTolerance = 24f,
                    minEntrySpeed = 3.5f,
                    maxEntrySpeed = 18.5f,
                    goalPosition = new Vector2(7.1f, -3.9f),
                    goalScale = new Vector2(2.4f, 1.5f),
                    hasObstacle = true,
                    obstaclePosition = new Vector2(-1.5f, -0.9f),
                    obstacleSize = new Vector2(0.6f, 1.8f)
                },

                // Level 4: Valley Dip (Trough ramp)
                new LevelConfig
                {
                    levelTitle = "Level 4: Valley Dip",
                    levelDescription = "Launch from the high platform down into the sweeping valley curve.",
                    launchSpawnPoint = new Vector2(-8.0f, 0.2f),
                    launchPadSize = new Vector2(2.4f, 0.45f),
                    p0 = new Vector2(-0.8f, 1.6f),
                    p1 = new Vector2(1.6f, -0.8f),
                    p2 = new Vector2(4.4f, -2.8f),
                    p3 = new Vector2(6.5f, -3.2f),
                    angleTolerance = 28f,
                    minEntrySpeed = 3.0f,
                    maxEntrySpeed = 17.5f,
                    goalPosition = new Vector2(6.8f, -3.6f),
                    goalScale = new Vector2(2.4f, 1.5f),
                    hasObstacle = false
                },

                // Level 5: The Twin Spire (Midfield Low Gate)
                new LevelConfig
                {
                    levelTitle = "Level 5: The Twin Spire",
                    levelDescription = "Arc cleanly over the midfield barrier to reach the ramp entry crest.",
                    launchSpawnPoint = new Vector2(-8.4f, -1.6f),
                    launchPadSize = new Vector2(2.2f, 0.45f),
                    p0 = new Vector2(1.0f, 3.2f),
                    p1 = new Vector2(3.0f, 2.8f),
                    p2 = new Vector2(5.0f, -0.2f),
                    p3 = new Vector2(6.5f, -3.4f),
                    angleTolerance = 25f,
                    minEntrySpeed = 3.5f,
                    maxEntrySpeed = 18.0f,
                    goalPosition = new Vector2(6.8f, -3.8f),
                    goalScale = new Vector2(2.3f, 1.5f),
                    hasObstacle = true,
                    obstaclePosition = new Vector2(-2.2f, -1.1f),
                    obstacleSize = new Vector2(0.7f, 1.6f)
                },

                // Level 6: Skyward Crest (Calibrated for comfortable reach & exhilarating dive)
                new LevelConfig
                {
                    levelTitle = "Level 6: Skyward Crest",
                    levelDescription = "High-angle lob shot! Aim for the crest of the ramp for a fast roller-coaster dive.",
                    launchSpawnPoint = new Vector2(-8.5f, -1.8f),
                    launchPadSize = new Vector2(2.2f, 0.45f),
                    p0 = new Vector2(0.5f, 3.2f),
                    p1 = new Vector2(2.6f, 2.8f),
                    p2 = new Vector2(5.0f, 0.3f),
                    p3 = new Vector2(6.8f, -3.4f),
                    angleTolerance = 28f,
                    minEntrySpeed = 3.0f,
                    maxEntrySpeed = 25.0f,
                    goalPosition = new Vector2(7.1f, -3.8f),
                    goalScale = new Vector2(2.4f, 1.5f),
                    hasObstacle = false
                },

                // Level 7: Canyon Chasm (Long-Range Glider)
                new LevelConfig
                {
                    levelTitle = "Level 7: Canyon Chasm",
                    levelDescription = "Long-range launch! Clear the deep chasm hazard to reach the distant ramp.",
                    launchSpawnPoint = new Vector2(-8.6f, -0.6f),
                    launchPadSize = new Vector2(2.2f, 0.45f),
                    p0 = new Vector2(0.8f, 2.6f),
                    p1 = new Vector2(3.0f, 2.2f),
                    p2 = new Vector2(5.4f, -0.5f),
                    p3 = new Vector2(7.0f, -3.4f),
                    angleTolerance = 28f,
                    minEntrySpeed = 3.0f,
                    maxEntrySpeed = 25.0f,
                    goalPosition = new Vector2(7.3f, -3.8f),
                    goalScale = new Vector2(2.5f, 1.5f),
                    hasObstacle = true,
                    obstaclePosition = new Vector2(-2.2f, -2.2f),
                    obstacleSize = new Vector2(0.8f, 2.2f)
                },

                // Level 8: S-Bend Runway (Curved Runway)
                new LevelConfig
                {
                    levelTitle = "Level 8: S-Bend Runway",
                    levelDescription = "Aim for the wide entry shelf and enjoy a sweeping high-velocity slide.",
                    launchSpawnPoint = new Vector2(-8.2f, -1.4f),
                    launchPadSize = new Vector2(2.2f, 0.45f),
                    p0 = new Vector2(0.4f, 3.0f),
                    p1 = new Vector2(2.4f, 2.6f),
                    p2 = new Vector2(4.8f, 0.4f),
                    p3 = new Vector2(6.6f, -3.4f),
                    angleTolerance = 28f,
                    minEntrySpeed = 3.0f,
                    maxEntrySpeed = 25.0f,
                    goalPosition = new Vector2(6.9f, -3.8f),
                    goalScale = new Vector2(2.4f, 1.5f),
                    hasObstacle = true,
                    obstaclePosition = new Vector2(-2.8f, -1.0f),
                    obstacleSize = new Vector2(0.6f, 1.6f)
                },

                // Level 9: The Gauntlet (High Barrier Hurdle)
                new LevelConfig
                {
                    levelTitle = "Level 9: The Gauntlet",
                    levelDescription = "Power and angle mastery! Send a towering shot over the gauntlet pillar.",
                    launchSpawnPoint = new Vector2(-8.5f, -1.8f),
                    launchPadSize = new Vector2(2.0f, 0.45f),
                    p0 = new Vector2(0.6f, 3.2f),
                    p1 = new Vector2(2.8f, 2.8f),
                    p2 = new Vector2(5.2f, 0.4f),
                    p3 = new Vector2(6.8f, -3.4f),
                    angleTolerance = 28f,
                    minEntrySpeed = 3.0f,
                    maxEntrySpeed = 25.0f,
                    goalPosition = new Vector2(7.1f, -3.8f),
                    goalScale = new Vector2(2.4f, 1.5f),
                    hasObstacle = true,
                    obstaclePosition = new Vector2(-2.2f, -0.8f),
                    obstacleSize = new Vector2(0.6f, 1.8f)
                },

                // Level 10: Master's Spiral (Grand Finale)
                new LevelConfig
                {
                    levelTitle = "Level 10: Master's Spiral",
                    levelDescription = "GRAND FINALE! Full power, perfect arc, and a majestic ride into the Champion's Goal!",
                    launchSpawnPoint = new Vector2(-8.6f, -1.6f),
                    launchPadSize = new Vector2(2.4f, 0.45f),
                    p0 = new Vector2(0.6f, 3.4f),
                    p1 = new Vector2(2.8f, 3.0f),
                    p2 = new Vector2(5.4f, 0.6f),
                    p3 = new Vector2(7.0f, -3.3f),
                    angleTolerance = 28f,
                    minEntrySpeed = 3.0f,
                    maxEntrySpeed = 25.0f,
                    goalPosition = new Vector2(7.3f, -3.7f),
                    goalScale = new Vector2(2.6f, 1.6f),
                    hasObstacle = true,
                    obstaclePosition = new Vector2(-2.5f, -0.9f),
                    obstacleSize = new Vector2(0.6f, 1.6f)
                }
            };
        }

        public void LoadLevel(int levelIndex)
        {
            if (levels == null || levels.Count == 0) CreateDefaultLevels();

            currentLevelIndex = Mathf.Clamp(levelIndex, 0, levels.Count - 1);
            LevelConfig config = levels[currentLevelIndex];

            // 1. Reconfigure Curved Ramp Bezier Geometry
            if (surfacePath != null)
            {
                surfacePath.ConfigurePoints(config.p0, config.p1, config.p2, config.p3);
            }

            // 2. Reconfigure Physics Thresholds
            if (surfaceInteraction != null)
            {
                surfaceInteraction.ConfigureThresholds(config.angleTolerance, config.minEntrySpeed, config.maxEntrySpeed);
                surfaceInteraction.ResetInteraction();
            }

            // 3. Reconfigure Launch Platform
            if (launchPlatform != null)
            {
                launchPlatform.position = new Vector3(config.launchSpawnPoint.x, config.launchSpawnPoint.y - 0.45f, 0f);
                launchPlatform.localScale = new Vector3(config.launchPadSize.x, config.launchPadSize.y, 1f);
            }

            // 4. Reconfigure Goal Box
            if (goalBoxTransform != null)
            {
                goalBoxTransform.position = new Vector3(config.goalPosition.x, config.goalPosition.y, 0f);
                goalBoxTransform.localScale = new Vector3(config.goalScale.x, config.goalScale.y, 1f);
            }

            if (goalZone != null)
            {
                goalZone.ResetGoal();
            }

            // 5. Reconfigure Obstacle
            if (obstacleObject != null)
            {
                obstacleObject.SetActive(config.hasObstacle);
                if (config.hasObstacle)
                {
                    obstacleObject.transform.position = new Vector3(config.obstaclePosition.x, config.obstaclePosition.y, 0f);
                    obstacleObject.transform.localScale = new Vector3(config.obstacleSize.x, config.obstacleSize.y, 1f);
                }
            }

            OnLevelLoaded?.Invoke(config, currentLevelIndex);
        }

        public void NextLevel()
        {
            int nextIndex = (currentLevelIndex + 1) % levels.Count;
            LoadLevel(nextIndex);
        }

        public void ReloadCurrentLevel()
        {
            LoadLevel(currentLevelIndex);
        }
    }
}
