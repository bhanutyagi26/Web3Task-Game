using System;
using UnityEngine;

namespace CurveShot.Level
{
    /// <summary>
    /// Configuration data container for individual levels.
    /// Defines distinct curved ramp geometry, velocity windows, and angle tolerances.
    /// </summary>
    [Serializable]
    public class LevelConfig
    {
        public string levelTitle = "Level 1: Training Curve";
        [TextArea(2, 3)]
        public string levelDescription = "Pull back and aim for the top crest of the ramp with smooth power.";

        [Header("Projectile & Launch Pad")]
        public Vector2 launchSpawnPoint = new Vector2(-7.8f, -1.2f);
        public Vector2 launchPadSize = new Vector2(2.5f, 0.45f);

        [Header("Curved Ramp Bezier Geometry")]
        public Vector2 p0 = new Vector2(0.8f, 2.8f);
        public Vector2 p1 = new Vector2(2.4f, 2.4f);
        public Vector2 p2 = new Vector2(4.5f, -0.6f);
        public Vector2 p3 = new Vector2(6.2f, -3.2f);

        [Header("Physics Acceptance Thresholds")]
        [Tooltip("Allowed angle deviation from ramp tangent (degrees).")]
        [Range(5f, 40f)]
        public float angleTolerance = 25f;
        public float minEntrySpeed = 3.8f;
        public float maxEntrySpeed = 12.5f;

        [Header("Goal Box Configuration")]
        public Vector2 goalPosition = new Vector2(6.8f, -3.8f);
        public Vector2 goalScale = new Vector2(1.8f, 1.2f);

        [Header("Optional Hazard Obstacle")]
        public bool hasObstacle = false;
        public Vector2 obstaclePosition = new Vector2(0.5f, -0.5f);
        public Vector2 obstacleSize = new Vector2(0.8f, 2.5f);
    }
}
