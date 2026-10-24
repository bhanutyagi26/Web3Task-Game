using System;

namespace CurveShot.Tests
{
    // Minimal vector representation for standalone math verification (C# 5.0 compatible)
    public struct TestVector2
    {
        public float x;
        public float y;

        public TestVector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public float Magnitude()
        {
            return (float)Math.Sqrt(x * x + y * y);
        }

        public float SqrMagnitude()
        {
            return x * x + y * y;
        }

        public TestVector2 Normalized()
        {
            float mag = Magnitude();
            return mag > 0.0001f ? new TestVector2(x / mag, y / mag) : new TestVector2(0f, 0f);
        }

        public static TestVector2 operator +(TestVector2 a, TestVector2 b)
        {
            return new TestVector2(a.x + b.x, a.y + b.y);
        }

        public static TestVector2 operator -(TestVector2 a, TestVector2 b)
        {
            return new TestVector2(a.x - b.x, a.y - b.y);
        }

        public static TestVector2 operator *(TestVector2 a, float d)
        {
            return new TestVector2(a.x * d, a.y * d);
        }

        public static TestVector2 operator *(float d, TestVector2 a)
        {
            return new TestVector2(a.x * d, a.y * d);
        }

        public static float Dot(TestVector2 a, TestVector2 b)
        {
            return a.x * b.x + a.y * b.y;
        }

        public static float Angle(TestVector2 from, TestVector2 to)
        {
            float num = (float)Math.Sqrt(from.SqrMagnitude() * to.SqrMagnitude());
            if (num < 1E-15f) return 0f;
            float cos = Math.Max(-1f, Math.Min(1f, Dot(from, to) / num));
            return (float)Math.Acos(cos) * (180f / (float)Math.PI);
        }
    }

    public static class TestBezierLogic
    {
        public static TestVector2 EvaluateCubic(TestVector2 p0, TestVector2 p1, TestVector2 p2, TestVector2 p3, float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            float u = 1f - t;
            return (u * u * u * p0) + (3f * u * u * t * p1) + (3f * u * t * t * p2) + (t * t * t * p3);
        }

        public static TestVector2 EvaluateTangent(TestVector2 p0, TestVector2 p1, TestVector2 p2, TestVector2 p3, float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            float u = 1f - t;
            TestVector2 tan = (3f * u * u * (p1 - p0)) + (6f * u * t * (p2 - p1)) + (3f * t * t * (p3 - p2));
            return tan.Normalized();
        }

        public static TestVector2 KinematicPosition(TestVector2 origin, TestVector2 launchVel, TestVector2 gravity, float t)
        {
            return origin + (launchVel * t) + (0.5f * gravity * (t * t));
        }

        public static bool ValidateRampEntry(
            TestVector2 incomingVelocity,
            TestVector2 rampTangent,
            float contactT,
            float maxAngleTolerance,
            float minSpeed,
            float maxSpeed,
            float maxEntryT,
            out string failureReason)
        {
            if (contactT > maxEntryT)
            {
                failureReason = "MidRampImpact";
                return false;
            }

            float speed = incomingVelocity.Magnitude();
            if (speed < minSpeed)
            {
                failureReason = "InsufficientSpeed";
                return false;
            }

            if (speed > maxSpeed)
            {
                failureReason = "OverpoweredSpeed";
                return false;
            }

            float angleDiff = TestVector2.Angle(incomingVelocity.Normalized(), rampTangent.Normalized());
            if (angleDiff > maxAngleTolerance)
            {
                failureReason = "IncorrectAngle";
                return false;
            }

            failureReason = "Success";
            return true;
        }
    }

    class Program
    {
        static int _passed = 0;
        static int _failed = 0;

        static void Assert(bool condition, string testName, string details)
        {
            if (condition)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(string.Format("[PASS] {0}", testName));
                _passed++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(string.Format("[FAIL] {0} - {1}", testName, details));
                _failed++;
            }
            Console.ResetColor();
        }

        static int Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("  CURVE SHOT: PRECISION PHYSICS - VERIFICATION SUITE");
            Console.WriteLine("==================================================\n");

            // Test 1: Cubic Bezier Boundary Endpoints
            TestVector2 p0 = new TestVector2(1f, 3f);
            TestVector2 p1 = new TestVector2(2.5f, 2.5f);
            TestVector2 p2 = new TestVector2(4.5f, -0.5f);
            TestVector2 p3 = new TestVector2(6.5f, -3.5f);

            TestVector2 start = TestBezierLogic.EvaluateCubic(p0, p1, p2, p3, 0f);
            TestVector2 end = TestBezierLogic.EvaluateCubic(p0, p1, p2, p3, 1f);
            Assert(Math.Abs(start.x - p0.x) < 0.001f && Math.Abs(start.y - p0.y) < 0.001f, "Test_Bezier_StartPoint_Equals_P0", "");
            Assert(Math.Abs(end.x - p3.x) < 0.001f && Math.Abs(end.y - p3.y) < 0.001f, "Test_Bezier_EndPoint_Equals_P3", "");

            // Test 2: Bezier Tangent Direction
            TestVector2 tangentStart = TestBezierLogic.EvaluateTangent(p0, p1, p2, p3, 0f);
            TestVector2 expectedStartTan = (p1 - p0).Normalized();
            Assert(Math.Abs(tangentStart.x - expectedStartTan.x) < 0.001f && Math.Abs(tangentStart.y - expectedStartTan.y) < 0.001f, "Test_Bezier_InitialTangent_Aligns_With_P1_Minus_P0", "");

            // Test 3: Kinematic Trajectory Prediction
            TestVector2 origin = new TestVector2(-8f, -1f);
            TestVector2 launchVel = new TestVector2(10f, 12f);
            TestVector2 gravity = new TestVector2(0f, -9.81f * 1.6f); // -15.696
            float t = 0.5f;
            TestVector2 posAtHalfSec = TestBezierLogic.KinematicPosition(origin, launchVel, gravity, t);
            float expectedX = -8f + 10f * 0.5f;
            float expectedY = -1f + (12f * 0.5f) + (0.5f * -15.696f * 0.25f);
            Assert(Math.Abs(posAtHalfSec.x - expectedX) < 0.001f, "Test_KinematicTrajectory_HorizontalPosition", "");
            Assert(Math.Abs(posAtHalfSec.y - expectedY) < 0.001f, "Test_KinematicTrajectory_VerticalParabola", "");

            // Test 4: Ramp Entry Angle Validation
            TestVector2 rampTan = new TestVector2(0.85f, -0.52f).Normalized();
            // Case 4a: Perfect tangent alignment
            string reason;
            bool ok = TestBezierLogic.ValidateRampEntry(rampTan * 8.0f, rampTan, 0.05f, 22f, 3.8f, 12.5f, 0.35f, out reason);
            Assert(ok && reason == "Success", "Test_RampEntry_PerfectAngle_Accepted", reason);

            // Case 4b: Angle deviation within tolerance (10 deg vs 22 deg)
            float angleRad = 10f * (float)Math.PI / 180f;
            TestVector2 shallowVel = new TestVector2(
                rampTan.x * (float)Math.Cos(angleRad) - rampTan.y * (float)Math.Sin(angleRad),
                rampTan.x * (float)Math.Sin(angleRad) + rampTan.y * (float)Math.Cos(angleRad)
            ) * 8.0f;
            ok = TestBezierLogic.ValidateRampEntry(shallowVel, rampTan, 0.05f, 22f, 3.8f, 12.5f, 0.35f, out reason);
            Assert(ok && reason == "Success", "Test_RampEntry_AngleWithinTolerance_Accepted", reason);

            // Case 4c: Angle deviation exceeding tolerance (35 deg vs 22 deg)
            float steepRad = 35f * (float)Math.PI / 180f;
            TestVector2 steepVel = new TestVector2(
                rampTan.x * (float)Math.Cos(steepRad) - rampTan.y * (float)Math.Sin(steepRad),
                rampTan.x * (float)Math.Sin(steepRad) + rampTan.y * (float)Math.Cos(steepRad)
            ) * 8.0f;
            ok = TestBezierLogic.ValidateRampEntry(steepVel, rampTan, 0.05f, 22f, 3.8f, 12.5f, 0.35f, out reason);
            Assert(!ok && reason == "IncorrectAngle", "Test_RampEntry_SteepAngle_Rejected", reason);

            // Test 5: Velocity Magnitude Validation
            // Case 5a: Insufficient speed (2.5 m/s < 3.8 m/s)
            ok = TestBezierLogic.ValidateRampEntry(rampTan * 2.5f, rampTan, 0.05f, 22f, 3.8f, 12.5f, 0.35f, out reason);
            Assert(!ok && reason == "InsufficientSpeed", "Test_RampEntry_InsufficientSpeed_Rejected", reason);

            // Case 5b: Overpowered speed (16.0 m/s > 12.5 m/s)
            ok = TestBezierLogic.ValidateRampEntry(rampTan * 16.0f, rampTan, 0.05f, 22f, 3.8f, 12.5f, 0.35f, out reason);
            Assert(!ok && reason == "OverpoweredSpeed", "Test_RampEntry_OverpoweredSpeed_Rejected", reason);

            // Test 6: Mid-Ramp Contact Rejection
            ok = TestBezierLogic.ValidateRampEntry(rampTan * 8.0f, rampTan, 0.55f, 22f, 3.8f, 12.5f, 0.35f, out reason);
            Assert(!ok && reason == "MidRampImpact", "Test_RampEntry_MidRampImpact_Rejected", reason);

            // Test 7: Precision Level (Level 2) stricter thresholds
            float precisionAngleRad = 16f * (float)Math.PI / 180f;
            TestVector2 precisionVel = new TestVector2(
                rampTan.x * (float)Math.Cos(precisionAngleRad) - rampTan.y * (float)Math.Sin(precisionAngleRad),
                rampTan.x * (float)Math.Sin(precisionAngleRad) + rampTan.y * (float)Math.Cos(precisionAngleRad)
            ) * 8.0f;
            ok = TestBezierLogic.ValidateRampEntry(precisionVel, rampTan, 0.05f, 14f, 6.0f, 11.2f, 0.35f, out reason);
            Assert(!ok && reason == "IncorrectAngle", "Test_PrecisionLevel_StricterAngle_Rejected", reason);

            Console.WriteLine("\n--------------------------------------------------");
            Console.WriteLine(string.Format("RESULTS: {0} Passed, {1} Failed out of {2} tests.", _passed, _failed, _passed + _failed));
            Console.WriteLine("--------------------------------------------------");

            return _failed == 0 ? 0 : 1;
        }
    }
}
