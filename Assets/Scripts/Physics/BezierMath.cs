using UnityEngine;

namespace CurveShot.Physics
{
    /// <summary>
    /// Pure mathematical helper class for Bezier curves, tangent/normal evaluation,
    /// and kinematic projectile trajectory prediction.
    /// </summary>
    public static class BezierMath
    {
        /// <summary>
        /// Evaluates a point along a Cubic Bezier curve defined by 4 control points at parameter t in [0, 1].
        /// B(t) = (1-t)^3 * P0 + 3(1-t)^2 * t * P1 + 3(1-t) * t^2 * P2 + t^3 * P3
        /// </summary>
        public static Vector2 EvaluateCubic(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector2 point = uuu * p0;
            point += 3f * uu * t * p1;
            point += 3f * u * tt * p2;
            point += ttt * p3;

            return point;
        }

        /// <summary>
        /// Evaluates the first derivative (tangent direction vector) along the Cubic Bezier curve.
        /// B'(t) = 3(1-t)^2 * (P1 - P0) + 6(1-t)t * (P2 - P1) + 3t^2 * (P3 - P2)
        /// </summary>
        public static Vector2 EvaluateCubicTangent(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float u = 1f - t;
            float uu = u * u;
            float tt = t * t;

            Vector2 tangent = 3f * uu * (p1 - p0) + 6f * u * t * (p2 - p1) + 3f * tt * (p3 - p2);

            if (tangent.sqrMagnitude < 0.0001f)
            {
                // Fallback to end-to-end vector if tangent collapses
                return (p3 - p0).normalized;
            }

            return tangent.normalized;
        }

        /// <summary>
        /// Evaluates the 2D surface normal at parameter t (perpendicular to tangent, pointing upwards/outwards).
        /// </summary>
        public static Vector2 EvaluateCubicNormal(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            Vector2 tangent = EvaluateCubicTangent(p0, p1, p2, p3, t);
            // 90 degrees counter-clockwise rotation: (-tangent.y, tangent.x)
            return new Vector2(-tangent.y, tangent.x).normalized;
        }

        /// <summary>
        /// Generates an array of sampled points along the Cubic Bezier curve for collider generation and rendering.
        /// </summary>
        public static Vector2[] SamplePoints(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int sampleCount)
        {
            if (sampleCount < 2) sampleCount = 2;
            Vector2[] points = new Vector2[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / (sampleCount - 1);
                points[i] = EvaluateCubic(p0, p1, p2, p3, t);
            }

            return points;
        }

        /// <summary>
        /// Finds the approximate parameter t [0, 1] on the sampled curve closest to a given world position.
        /// </summary>
        public static float FindClosestT(Vector2[] sampledPoints, Vector2 queryPosition, out Vector2 closestPoint)
        {
            if (sampledPoints == null || sampledPoints.Length == 0)
            {
                closestPoint = Vector2.zero;
                return 0f;
            }

            float minSqrDist = float.MaxValue;
            int closestIndex = 0;

            for (int i = 0; i < sampledPoints.Length; i++)
            {
                float sqrDist = (sampledPoints[i] - queryPosition).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    closestIndex = i;
                }
            }

            closestPoint = sampledPoints[closestIndex];
            return (float)closestIndex / (sampledPoints.Length - 1);
        }

        /// <summary>
        /// Computes discrete kinematic trajectory points under constant 2D gravity:
        /// p(t) = p0 + v0 * t + 0.5 * g * t^2
        /// </summary>
        public static void CalculateTrajectoryPoints(
            Vector2 origin,
            Vector2 launchVelocity,
            Vector2 gravity,
            float timeStep,
            int stepCount,
            Vector3[] outPoints)
        {
            if (outPoints == null || outPoints.Length < stepCount) return;

            for (int i = 0; i < stepCount; i++)
            {
                float t = i * timeStep;
                Vector2 pos = origin + (launchVelocity * t) + (0.5f * gravity * (t * t));
                outPoints[i] = new Vector3(pos.x, pos.y, 0f);
            }
        }
    }
}
