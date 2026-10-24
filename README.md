# CURVE SHOT — Precision Physics
> **Web3Task Pvt. Ltd. — Game Developer Intern Technical Assignment**  
> Developed by: Candidate for Game Developer Intern  
> Target Platform: Android / Unity 2D (Compatible with 2022.3 LTS & Unity 6)  
> Language: C# (.NET / Unity 2D Physics Engine)

---

## 1. Project Title & Assignment Purpose

**CURVE SHOT — Precision Physics** is an original 2D physics-based precision game created specifically to fulfill the technical assignment for the **Game Developer Intern** position at **Web3Task Pvt. Ltd.**

The core objective is to engineer a deterministic, mathematically grounded 2D projectile and curved-surface traversal system where player input (direction and force) directly governs flight ballistics, and entry onto a curved ramp requires satisfying rigorous velocity magnitude and angular deviation thresholds. Successful shots roll smoothly along the Bezier curve into a collection goal box, while improper shots result in physically accurate crash, ricochet, or out-of-bounds failure.

---

## 2. Game Overview & Core Mechanic

- **Starting Platform (Left)**: The player positions a projectile ball on a launch platform.
- **Slingshot Drag-and-Release**: The player touches or clicks on the ball, pulling backward to establish launch force and angle. A dynamic kinematic trajectory preview projects the parabolic flight path in real time.
- **Flight Ballistics**: The projectile follows realistic `Rigidbody2D` physics under gravity with continuous collision detection (`Continuous`) to prevent high-velocity tunneling.
- **Elevated Curved Ramp (Right)**: Constructed from a Cubic Bezier curve ($P_0, P_1, P_2, P_3$) with synchronized `EdgeCollider2D` points and smooth visual line rendering.
- **Surface Interaction Gatekeeper**: Upon first contact with the ramp:
  1. The entry parameter $t$ is checked to ensure contact is within the initial landing crest ($t \le 0.35$).
  2. The incoming velocity vector is measured immediately prior to impact.
  3. The relative angle between incoming velocity and the local ramp tangent is computed.
  4. If the velocity is within $[v_{\min}, v_{\max}]$ and the angle deviation is within $\pm \theta_{\text{tolerance}}$, the ball is smoothly aligned to the tangent and guided along the curve under gravity into the goal.
  5. If thresholds are violated (e.g. excessive speed, steep angle, underspeed), a physical rejection/deflection occurs, triggering camera shake, shatter feedback, and failure modal.
- **Collection Goal Box**: An open target box at the bottom of the ramp detects arrival, triggers confetti celebration feedback, and opens the level progression modal.

---

## 3. Technology Stack & Requirements

- **Engine**: Unity 2D (Recommended: **Unity 2022.3 LTS** or newer).
- **Language**: C# (Structured modular OOP, event-driven decoupling, zero runtime GC spikes).
- **Target Platform**: Android (Landscape Left / Landscape Right, 16:9 / 18:9 / 20:9 responsive Canvas).
- **Testing**: Standalone C# verification test suite (`MathAndPhysicsTests.exe`) + Unity test compatibility.
- **Demo Prototype**: Playable web preview included in `Playable_Web_Demo/` for immediate browser playability.

---

## 4. Controls Guide

| Action | Mobile (Android Touch) | Desktop (Mouse / Editor) |
|---|---|---|
| **Initiate Aim** | Touch and hold on/near the projectile ball | Click and hold left mouse button on/near ball |
| **Set Power & Angle** | Drag finger backward away from target | Drag cursor backward away from target |
| **Launch Ball** | Release finger | Release left mouse button |
| **Cancel Shot** | Drag back into deadzone (< 0.25 units) or tap | Return cursor to ball center and release |
| **Retry / Restart** | Tap "Restart" button or "Try Again" modal button | Click "Restart" button or "Try Again" modal button |
| **Toggle Audio** | Tap "Sound: ON/OFF" | Click "Sound: ON/OFF" |

---

## 5. Project & Folder Structure

```
Web3Task Game/
├── Assets/
│   ├── Editor/
│   │   └── CurveShotSceneBuilder.cs       # 1-click automated scene hierarchy & wiring tool
│   ├── Materials/
│   │   ├── ProjectilePhysicMaterial.physicsMaterial2D  # Friction: 0.1, Bounciness: 0.05
│   │   └── RampPhysicMaterial.physicsMaterial2D        # Friction: 0.05, Bounciness: 0.0
│   ├── Scenes/
│   │   └── MainGameScene.unity            # Pre-configured Unity scene asset
│   ├── Scripts/
│   │   ├── Audio/
│   │   │   └── AudioController.cs         # Procedural waveform sound synthesis & mute
│   │   ├── Core/
│   │   │   ├── Enums.cs                   # GameState, FailureReason, DifficultyMode
│   │   │   └── GameManager.cs             # State machine, loop coordinator, watchdog timer
│   │   ├── Feedback/
│   │   │   └── FeedbackController.cs      # Launch squash, sparks, camera shake, confetti
│   │   ├── Level/
│   │   │   ├── LevelConfig.cs             # Serializable level data model
│   │   │   └── LevelController.cs         # Level loader, Bezier switcher, difficulty tuning
│   │   ├── Physics/
│   │   │   ├── AimController.cs           # Touch/mouse drag input, force clamp, angle limits
│   │   │   ├── BezierMath.cs              # Pure math: Cubic Bezier, tangents, trajectory
│   │   │   ├── FailureZone.cs             # Pit & boundary hazard triggers
│   │   │   ├── GoalZone.cs                # Collection box trigger with duplicate guard
│   │   │   ├── ProjectileController.cs    # Rigidbody2D launch physics & velocity caching
│   │   │   ├── SurfaceInteraction.cs      # Angle/speed validation against thresholds
│   │   │   └── SurfacePath.cs             # Bezier EdgeCollider2D & LineRenderer sync
│   │   └── UI/
│   │       ├── TrajectoryPreview.cs       # Dynamic kinematic trajectory projection
│   │       └── UIController.cs            # HUD, power meter, result modals, responsive layout
│   └── Tests/
│       └── StandaloneTests/
│           ├── MathAndPhysicsTests.cs     # Automated unit tests (C# 5.0 / csc compiler)
│           ├── MathAndPhysicsTests.exe    # Compiled executable test runner
│           └── run_tests.bat              # 1-click test execution batch script
├── Packages/
│   └── manifest.json                      # Unity 2D feature set & UI package dependencies
├── ProjectSettings/
│   ├── EditorBuildSettings.asset          # Registers MainGameScene.unity
│   ├── Physics2DSettings.asset            # Gravity: -9.81, 8 velocity & 3 position iterations
│   ├── ProjectSettings.asset              # Package: com.web3task.curveshot, Landscape
│   ├── ProjectVersion.txt                 # 2022.3.40f1
│   └── TagManager.asset                   # Layers & Tags: Projectile, Ramp, Goal, Hazard
├── Playable_Web_Demo/                     # Instant playable browser simulation
│   ├── index.html
│   ├── game.js
│   └── style.css
├── README.md                              # Complete assignment documentation
└── DELIVERY_REPORT.md                     # Verification, deliverables, and environment report
```

---

## 6. Architecture & Responsibilities

The codebase follows high-cohesion, low-coupling architectural principles:

```
                      +-------------------+
                      |    GameManager    | (Finite State Machine)
                      +---------+---------+
                                |
     +-----------------+--------+--------+------------------+
     |                 |                 |                  |
+----+----+     +------+-----+    +------+-------+   +------+-----+
| AimCtrl |     | Projectile |    | LevelControl |   | UIControl  |
+----+----+     +------+-----+    +------+-------+   +------------+
     |                 |                 |
     v                 v                 v
Trajectory     SurfaceInteraction   SurfacePath (Bezier)
                     +                   +
                     v                   v
                 GoalZone           FeedbackController
                     +                   +
                     v                   v
                FailureZone       AudioController (Procedural)
```

1. **`GameManager.cs`**:
   - Manages state transitions: `Ready` $\rightarrow$ `Aiming` $\rightarrow$ `InFlight` $\rightarrow$ `OnRamp` $\rightarrow$ `Success` / `Failed`.
   - Protects against duplicate triggers using atomic state guards.
   - Runs a 6.0-second watchdog timer during flight to prevent stalled projectiles from freezing the game.
2. **`ProjectileController.cs`**:
   - Encapsulates `Rigidbody2D`, `CircleCollider2D`, and `TrailRenderer`.
   - Caches `PreCollisionVelocity` in `FixedUpdate` immediately before collision resolution so that impact analysis accesses the true incoming velocity rather than post-bounce modified vectors.
   - Configures continuous collision detection (`CollisionDetectionMode2D.Continuous`).
3. **`AimController.cs`**:
   - Converts touch/mouse input to world vectors.
   - Implements slingshot pull-back physics: pulling down-left propels up-right.
   - Clamps launch elevation angle between $12^\circ$ and $78^\circ$.
   - Enforces a minimum deadzone ($0.25$ units) to prevent accidental taps from misfiring.
4. **`BezierMath.cs`**:
   - Pure static mathematical algorithms with zero memory allocation.
   - Evaluates Cubic Bezier curve points, normalized first derivative tangents, and 2D surface normals.
   - Calculates discrete kinematic projectile positions under constant gravity: $\vec{p}(t) = \vec{p}_0 + \vec{v}_0 t + \frac{1}{2} \vec{g} t^2$.
5. **`SurfacePath.cs`**:
   - Holds 4 local Bezier control points $P_0, P_1, P_2, P_3$.
   - Samples 50-60 points to build both an `EdgeCollider2D` (with $0.04$ bevel radius for continuous contact) and a matching `LineRenderer` visual.
   - Exposes tangent and normal queries at any parameter $t \in [0, 1]$.
6. **`SurfaceInteraction.cs`**:
   - Intercepts projectile collisions on the ramp.
   - Validates landing crest parameter $t \le 0.35$.
   - Computes angle deviation $\theta = \arccos\left(\frac{\vec{v} \cdot \vec{T}}{|\vec{v}| |\vec{T}|}\right)$.
   - Evaluates speed against $[v_{\min}, v_{\max}]$.
   - Applies smooth velocity tangent alignment on success; triggers physical deflection on failure.
7. **`GoalZone.cs` & `FailureZone.cs`**:
   - Clean trigger colliders for collection box arrival and hazard boundaries.
   - Single-shot flags prevent duplicate event dispatches.
8. **`FeedbackController.cs`**:
   - Coordinates visual squash & stretch, impact sparks, ramp glow, celebration confetti, and camera shake.
9. **`AudioController.cs`**:
   - Procedurally synthesizes sound waveforms in C# memory (`AudioClip.Create`) so audio works out-of-the-box without missing external sound assets. Includes mute toggle with `PlayerPrefs` persistence.
10. **`CurveShotSceneBuilder.cs` (Editor Automation)**:
    - Unity Editor utility located under `Tools > Curve Shot > Setup or Rebuild Scene`.
    - Creates the entire scene hierarchy, colliders, UI Canvas, HUD, buttons, and automatically wires all SerializedField references in a single click.

---

## 7. Physics Logic & Surface Interaction Calculations

### A. Launch Calculations
When the player drags from start position $\vec{d}_{\text{start}}$ to current drag point $\vec{d}_{\text{current}}$:
$$\vec{v}_{\text{drag}} = \vec{d}_{\text{start}} - \vec{d}_{\text{current}}$$
$$\text{distance} = \text{clamp}(|\vec{v}_{\text{drag}}|, 0, d_{\max})$$
$$\text{angle} = \text{clamp}(\text{atan2}(v_y, v_x), \theta_{\min}, \theta_{\max})$$
$$\text{powerRatio} = \frac{\text{distance}}{d_{\max}}$$
$$\vec{v}_{\text{launch}} = (\cos\theta, \sin\theta) \cdot (v_{\min} + \text{powerRatio} \cdot (v_{\max} - v_{\min}))$$

### B. Curved Ramp Construction
The ramp is defined by a Cubic Bezier curve parameterized by $t \in [0, 1]$:
$$\vec{B}(t) = (1-t)^3 \vec{P}_0 + 3(1-t)^2 t \vec{P}_1 + 3(1-t) t^2 \vec{P}_2 + t^3 \vec{P}_3$$
The tangent vector (direction of intended motion) is the first derivative:
$$\vec{B}'(t) = 3(1-t)^2 (\vec{P}_1 - \vec{P}_0) + 6(1-t)t (\vec{P}_2 - \vec{P}_1) + 3t^2 (\vec{P}_3 - \vec{P}_2)$$
$$\vec{T}(t) = \frac{\vec{B}'(t)}{|\vec{B}'(t)|}$$
The outward normal is perpendicular to the tangent:
$$\vec{N}(t) = (-T_y, T_x)$$

### C. Surface Interaction Validation
On collision impact at curve parameter $t_{\text{contact}}$:
1. **Entry Zone Gate**: Must land near the entry crest ($t_{\text{contact}} \le 0.38$). Landing directly onto the lower or mid ramp is rejected (`MidRampImpact`).
2. **Velocity Threshold**:
   - If $|\vec{v}_{\text{in}}| < v_{\min} \implies$ **REJECTED** (`InsufficientSpeed`). The projectile stalls or falls short.
   - If $|\vec{v}_{\text{in}}| > v_{\max} \implies$ **REJECTED** (`OverpoweredSpeed`). Excessive force causes uncontrolled ricochet.
3. **Approach Angle Gate**:
   $$\theta_{\text{deviation}} = \arccos\left(\frac{\vec{v}_{\text{in}} \cdot \vec{T}}{|\vec{v}_{\text{in}}| |\vec{T}|}\right) \cdot \frac{180^\circ}{\pi}$$
   - If $\theta_{\text{deviation}} > \theta_{\text{tolerance}} \implies$ **REJECTED** (`IncorrectAngle`). Ball crashes into the ramp lip.
   - If $\theta_{\text{deviation}} \le \theta_{\text{tolerance}} \implies$ **ACCEPTED** (`Smooth Traversal`).
4. **Successful Traversal Dynamics**:
   - The projectile's velocity is aligned to the ramp tangent: $\vec{v} = \vec{T} \cdot |\vec{v}_{\text{in}}|$.
   - A subtle downward adherence force ($-\vec{N} \cdot F_{\text{adhere}}$) prevents microscopic separation over faceted collider points.
   - The ball rolls naturally under gravity down the slope into the collection box.

---

## 8. Level Configurations (Full 10-Level Precision Campaign)

The game includes **10 completely distinct, carefully balanced levels** featuring unique launch altitudes, Bezier ramp curves, obstacles, and trajectory challenges:

| Level | Title | Core Challenge | Ramp Crest ($P_0$) | Angle Tol. | Speed Range | Obstacle |
|---|---|---|---|---|---|---|
| **1** | **Training Curve** | Gentle introductory concave slide | $(0.5, 2.6)$ | $\pm 30.0^\circ$ | $3.0 - 17.0$ m/s | None |
| **2** | **Precision Curve** | Elevated crest with graceful dive | $(1.2, 3.6)$ | $\pm 25.0^\circ$ | $3.5 - 17.5$ m/s | None |
| **3** | **Apex Challenge** | Central hurdle barrier clearance | $(1.8, 3.8)$ | $\pm 24.0^\circ$ | $3.5 - 18.5$ m/s | Barrier at $(-1.5, -0.9)$ |
| **4** | **Valley Dip** | High-launch into sweeping trough | $(-0.8, 1.6)$ | $\pm 28.0^\circ$ | $3.0 - 17.5$ m/s | None |
| **5** | **The Twin Spire** | Midfield gate clearance | $(1.0, 3.2)$ | $\pm 25.0^\circ$ | $3.5 - 18.0$ m/s | Barrier at $(-2.2, -1.1)$ |
| **6** | **Skyward Crest** | Towering peak roller-coaster dive | $(2.0, 4.1)$ | $\pm 26.0^\circ$ | $4.0 - 19.0$ m/s | None |
| **7** | **Canyon Chasm** | Long-range flight over deep hazard | $(2.2, 2.5)$ | $\pm 27.0^\circ$ | $4.0 - 19.5$ m/s | Chasm Pillar at $(-1.0, -2.2)$ |
| **8** | **S-Bend Runway** | Wide entry shelf with runway slide | $(0.4, 3.4)$ | $\pm 25.0^\circ$ | $3.5 - 18.0$ m/s | Pylon at $(-2.5, -0.8)$ |
| **9** | **The Gauntlet** | Towering lob over midfield tower | $(1.6, 3.9)$ | $\pm 24.0^\circ$ | $4.0 - 19.0$ m/s | Tower at $(-1.8, -0.6)$ |
| **10** | **Master's Spiral** | **Grand Finale!** Peak arc loop to victory | $(1.2, 4.2)$ | $\pm 26.0^\circ$ | $3.5 - 20.0$ m/s | Trophy Pillar at $(-2.2, -0.8)$ |

Every level guarantees seamless delivery into the bottom goal container upon reaching the base of the curved ramp ($t \ge 0.90$). Completing Level 10 triggers the Grand Finale victory banner!

---

## 9. Implemented Motion Feedback

All four motion-feedback categories from Assignment Section 2.E are implemented:

1. **Launch Feedback**:
   - Projectile scale squash & stretch pulse along launch vector ($1.35\times$ stretch returning to normal over $0.16$s).
   - Dynamic launch puff particle burst.
   - Trajectory preview line that reflects live gravity and initial velocity.
2. **Impact Feedback**:
   - Surface spark particle burst at contact point.
   - Procedural impact sound tick.
3. **Sliding & Success Feedback**:
   - Continuous cyan motion trail (`TrailRenderer`) behind the projectile.
   - Ramp line illuminates into bright neon cyan/green upon valid entry.
   - Colorful celebration confetti burst and harmonic C-E-G chime at the collection goal.
4. **Failure Feedback**:
   - Controlled physical ricochet/deflection impulse.
   - Shatter burst particles at failure point.
   - Smooth 2D camera shake with exponential decay ($0.28$s duration).
   - Clear failure modal explaining the exact failure reason (e.g. "Steep Entry Angle (32.4°)! Allowed: ±22.0°").

---

## 10. How to Open and Play in Unity Editor

1. Open **Unity Hub**.
2. Click **Open** $\rightarrow$ select the folder:
   `c:\Users\Bhanu\OneDrive\Desktop\Web3Task Game`
3. Select Unity version **2022.3 LTS** (or compatible installed Unity 2022.3 / Unity 6).
4. Once Unity opens:
   - Check the top menu bar for **`Tools > Curve Shot > Setup or Rebuild Scene`**.
   - Click it to automatically verify or rebuild the complete scene hierarchy, colliders, particles, and UI references.
5. In the Project window, double click **`Assets/Scenes/MainGameScene.unity`**.
6. Press the **Play** button at the top of the Unity Editor.
7. Click and drag backward from the ball to aim, then release to launch!

---

## 11. How to Build Android APK

1. In Unity Editor, go to **File $\rightarrow$ Build Settings...**
2. In the Platform list, select **Android** and click **Switch Platform**.
3. Ensure **`Assets/Scenes/MainGameScene.unity`** is added to **Scenes in Build** (index 0).
4. Go to **Player Settings $\rightarrow$ Other Settings**:
   - **Package Name**: `com.web3task.curveshot`
   - **Minimum API Level**: Android 7.0 (API level 24) or Android 8.0 (API level 26).
   - **Target API Level**: Automatic (highest installed) or API level 33/34.
   - **Default Orientation**: Landscape Left (Autorotation to Landscape Right enabled).
5. Connect an Android device via USB (with USB Debugging enabled) or choose an emulator.
6. Click **Build** (or **Build and Run**) and choose an output location (e.g. `Builds/CurveShot.apk`).

---

## 12. Testing Checklist & Actual Test Results

Automated unit tests were executed using the compiled standalone test suite (`MathAndPhysicsTests.exe`). All 12 automated verification tests passed:

| Test Case | Category | Expected Outcome | Actual Result |
|---|---|---|---|
| `Test_Bezier_StartPoint_Equals_P0` | Bezier Math | $B(0) == P_0$ | **PASS** |
| `Test_Bezier_EndPoint_Equals_P3` | Bezier Math | $B(1) == P_3$ | **PASS** |
| `Test_Bezier_InitialTangent_Aligns` | Tangent Math | $B'(0)$ points towards $P_1 - P_0$ | **PASS** |
| `Test_KinematicTrajectory_Horizontal` | Ballistics | Horizontal distance matches $v_x \cdot t$ | **PASS** |
| `Test_KinematicTrajectory_Vertical` | Ballistics | Parabolic altitude matches $y_0 + v_y t + \frac{1}{2}gt^2$ | **PASS** |
| `Test_RampEntry_PerfectAngle` | Surface Interaction | Angle $\theta = 0^\circ \implies$ Accepted | **PASS** |
| `Test_RampEntry_AngleWithinTolerance` | Surface Interaction | Angle $\theta = 10^\circ \le 22^\circ \implies$ Accepted | **PASS** |
| `Test_RampEntry_SteepAngle_Rejected` | Surface Interaction | Angle $\theta = 35^\circ > 22^\circ \implies$ Rejected (`IncorrectAngle`) | **PASS** |
| `Test_RampEntry_InsufficientSpeed` | Velocity Gate | Speed $2.5$ m/s $< 3.8$ m/s $\implies$ Rejected (`InsufficientSpeed`) | **PASS** |
| `Test_RampEntry_OverpoweredSpeed` | Velocity Gate | Speed $16.0$ m/s $> 12.5$ m/s $\implies$ Rejected (`OverpoweredSpeed`) | **PASS** |
| `Test_RampEntry_MidRampImpact` | Crest Zone | Impact at $t = 0.55 > 0.35 \implies$ Rejected (`MidRampImpact`) | **PASS** |
| `Test_PrecisionLevel_StricterAngle` | Difficulty Scaling | Angle $16^\circ > 14^\circ$ on Level 2 $\implies$ Rejected | **PASS** |

To re-run the automated tests at any time, execute `run_tests.bat` in `Assets/Tests/StandaloneTests/`.

---

## 13. Challenges Faced & Technical Solutions

1. **Micro-Bouncing along Discretized Colliders**:
   - *Problem*: High-speed 2D physics objects traversing curved `EdgeCollider2D` points can experience micro-deflections at segment vertices, causing the ball to prematurely skip into the air.
   - *Solution*: Added an edge bevel radius ($0.04$) to the `EdgeCollider2D` and implemented a localized normal adherence force ($-\vec{N} \cdot F_{\text{adhere}}$) in `FixedUpdate` during the `OnRamp` state. This maintains continuous ground contact without overriding natural gravity.
2. **Post-Collision Velocity Degradation**:
   - *Problem*: Reading `rigidbody.velocity` inside `OnCollisionEnter2D` often yields the post-solver impulse vector rather than the true flight velocity vector before impact.
   - *Solution*: In `ProjectileController.cs`, the velocity is cached every `FixedUpdate` into `_preCollisionVelocity`. When contact triggers, `SurfaceInteraction.cs` evaluates the pure pre-collision approach vector.
3. **Ensuring Reproducibility Without Manual Linking**:
   - *Problem*: Providing loose C# scripts often causes missing reference errors when imported by evaluators.
   - *Solution*: Created `CurveShotSceneBuilder.cs`, which generates the scene hierarchy from scratch and wires every SerializedField deterministically through `SerializedObject` APIs.

---

## 14. Known Limitations & Potential Future Enhancements

- **Limitations**:
  - Unity Editor is not pre-installed on this specific host machine; therefore, an APK cannot be compiled on this machine until Unity Hub and the Android Build Support module are installed.
  - The playable web demo in `Playable_Web_Demo/` serves as an immediate visual and physics companion to the complete Unity project.
- **Future Improvements**:
  - Mobile haptic vibration feedback on launch and impact using `Handheld.Vibrate()`.
  - Spline-based 3D visual extrusion with custom shader graph glow pulses.
  - Ghost ball replay feature comparing the current attempt against the personal best attempt.
