namespace CurveShot.Core
{
    /// <summary>
    /// Explicit finite states for the Curve Shot game loop.
    /// </summary>
    public enum GameState
    {
        Ready,      // Ball on launch pad, awaiting player aim
        Aiming,     // Player dragging pointer/touch to aim and select power
        InFlight,   // Ball airborne toward ramp; physics active
        OnRamp,     // Ball successfully landed on and traversing the curved ramp
        Success,    // Ball reached the collection goal box
        Failed      // Ball fell out of bounds, crashed, timed out, or missed
    }

    /// <summary>
    /// Detailed failure causes for precision feedback and educational hints.
    /// </summary>
    public enum FailureReason
    {
        None,
        OutOfBounds,        // Fell into bottom pit or off-screen boundaries
        Undershot,          // Failed to reach ramp entry (insufficient force)
        OverpoweredSpeed,   // Impact velocity exceeded ramp threshold (too fast)
        InsufficientSpeed,  // Velocity too low on ramp entry to sustain curvature
        IncorrectAngle,     // Incoming angle did not match ramp tangent tolerance
        Timeout,            // Projectile stalled or trapped without resolving
        ObstacleHit         // Hit an underside obstacle or invalid boundary
    }

    /// <summary>
    /// Available level configurations.
    /// </summary>
    public enum DifficultyMode
    {
        Training,   // Forgiving entry angle and broader speed tolerance
        Precision,  // Steeper curve, strict angle tolerance, narrower speed window
        Apex        // High-velocity challenge with obstacle hurdle
    }
}
