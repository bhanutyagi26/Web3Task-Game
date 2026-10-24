/**
 * CURVE SHOT — Precision Physics
 * High-fidelity playable 2D physics simulation running the exact Bezier geometry,
 * surface-interaction validation algorithms, and motion feedback designed for Unity.
 */

(function () {
    'use strict';

    // Canvas & Context Setup
    const canvas = document.getElementById('game-canvas');
    const ctx = canvas.getContext('2d');

    // UI Elements
    const uiLevelTitle = document.getElementById('ui-level-title');
    const uiLevelHint = document.getElementById('ui-level-hint');
    const uiPowerFill = document.getElementById('ui-power-fill');
    const uiPowerText = document.getElementById('ui-power-text');
    const uiAttemptBadge = document.getElementById('ui-attempt-badge');
    const uiFeedbackBanner = document.getElementById('ui-feedback-banner');
    const uiSuccessModal = document.getElementById('ui-success-modal');
    const uiSuccessBody = document.getElementById('ui-success-body');
    const uiFailureModal = document.getElementById('ui-failure-modal');
    const uiFailureTitle = document.getElementById('ui-failure-title');
    const uiFailureBody = document.getElementById('ui-failure-body');
    const uiMuteBtn = document.getElementById('ui-mute-btn');
    const uiRestartBtn = document.getElementById('ui-restart-btn');
    const uiNextLevelBtn = document.getElementById('ui-next-level');
    const uiSuccessReplayBtn = document.getElementById('ui-success-replay');
    const uiFailureRetryBtn = document.getElementById('ui-failure-retry');

    // Coordinate Conversion: World Units -> Canvas Pixels
    // World origin (0, 0) at center. Width = 1280, Height = 720.
    // 1 World Unit = 64 pixels. Visible X: -10 to +10, Visible Y: -5.625 to +5.625.
    const PIXELS_PER_UNIT = 64;
    let canvasWidth = 1280;
    let canvasHeight = 720;

    function resizeCanvas() {
        const rect = canvas.getBoundingClientRect();
        canvas.width = 1280;
        canvas.height = 720;
        canvasWidth = canvas.width;
        canvasHeight = canvas.height;
    }
    window.addEventListener('resize', resizeCanvas);
    resizeCanvas();

    function worldToScreen(wx, wy) {
        return {
            x: (canvasWidth / 2) + (wx * PIXELS_PER_UNIT),
            y: (canvasHeight / 2) - (wy * PIXELS_PER_UNIT)
        };
    }

    function screenToWorld(sx, sy) {
        return {
            x: (sx - (canvasWidth / 2)) / PIXELS_PER_UNIT,
            y: ((canvasHeight / 2) - sy) / PIXELS_PER_UNIT
        };
    }

    // Audio Synthesizer (Web Audio API)
    let audioCtx = null;
    let isMuted = false;

    function initAudio() {
        if (!audioCtx) {
            const AudioContext = window.AudioContext || window.webkitAudioContext;
            if (AudioContext) audioCtx = new AudioContext();
        }
        if (audioCtx && audioCtx.state === 'suspended') {
            audioCtx.resume();
        }
    }

    function playSound(type) {
        if (isMuted) return;
        initAudio();
        if (!audioCtx) return;

        const now = audioCtx.currentTime;
        const osc = audioCtx.createOscillator();
        const gain = audioCtx.createGain();
        osc.connect(gain);
        gain.connect(audioCtx.destination);

        if (type === 'launch') {
            osc.type = 'sine';
            osc.frequency.setValueAtTime(220, now);
            osc.frequency.exponentialRampToValueAtTime(620, now + 0.14);
            gain.gain.setValueAtTime(0.25, now);
            gain.gain.linearRampToValueAtTime(0.01, now + 0.14);
            osc.start(now);
            osc.stop(now + 0.15);
        } else if (type === 'impact') {
            osc.type = 'triangle';
            osc.frequency.setValueAtTime(480, now);
            osc.frequency.exponentialRampToValueAtTime(220, now + 0.08);
            gain.gain.setValueAtTime(0.3, now);
            gain.gain.linearRampToValueAtTime(0.01, now + 0.08);
            osc.start(now);
            osc.stop(now + 0.09);
        } else if (type === 'success') {
            // Harmonic chime chord (C5 - E5 - G5)
            [523.25, 659.25, 783.99].forEach((freq, idx) => {
                const subOsc = audioCtx.createOscillator();
                const subGain = audioCtx.createGain();
                subOsc.connect(subGain);
                subGain.connect(audioCtx.destination);
                subOsc.type = 'sine';
                subOsc.frequency.setValueAtTime(freq, now + idx * 0.08);
                subGain.gain.setValueAtTime(0.2, now + idx * 0.08);
                subGain.gain.exponentialRampToValueAtTime(0.001, now + idx * 0.08 + 0.5);
                subOsc.start(now + idx * 0.08);
                subOsc.stop(now + idx * 0.08 + 0.52);
            });
        } else if (type === 'failure') {
            osc.type = 'sawtooth';
            osc.frequency.setValueAtTime(160, now);
            osc.frequency.linearRampToValueAtTime(55, now + 0.22);
            gain.gain.setValueAtTime(0.3, now);
            gain.gain.linearRampToValueAtTime(0.01, now + 0.22);
            osc.start(now);
            osc.stop(now + 0.23);
        }
    }

    // Bezier Curve Mathematics
    function evaluateCubicBezier(p0, p1, p2, p3, t) {
        t = Math.max(0, Math.min(1, t));
        const u = 1 - t;
        const tt = t * t;
        const uu = u * u;
        const uuu = uu * u;
        const ttt = tt * t;

        return {
            x: uuu * p0.x + 3 * uu * t * p1.x + 3 * u * tt * p2.x + ttt * p3.x,
            y: uuu * p0.y + 3 * uu * t * p1.y + 3 * u * tt * p2.y + ttt * p3.y
        };
    }

    function evaluateCubicTangent(p0, p1, p2, p3, t) {
        t = Math.max(0, Math.min(1, t));
        const u = 1 - t;
        const tt = t * t;
        const uu = u * u;

        const tx = 3 * uu * (p1.x - p0.x) + 6 * u * t * (p2.x - p1.x) + 3 * tt * (p3.x - p2.x);
        const ty = 3 * uu * (p1.y - p0.y) + 6 * u * t * (p2.y - p1.y) + 3 * tt * (p3.y - p2.y);
        const mag = Math.sqrt(tx * tx + ty * ty);

        return mag > 0.0001 ? { x: tx / mag, y: ty / mag } : { x: 1, y: 0 };
    }

    function findClosestTOnCurve(points, qx, qy) {
        let minDistSq = Infinity;
        let bestIndex = 0;
        for (let i = 0; i < points.length; i++) {
            const dx = points[i].x - qx;
            const dy = points[i].y - qy;
            const dSq = dx * dx + dy * dy;
            if (dSq < minDistSq) {
                minDistSq = dSq;
                bestIndex = i;
            }
        }
        return {
            t: bestIndex / (points.length - 1),
            point: points[bestIndex],
            dist: Math.sqrt(minDistSq)
        };
    }

    // Level Configurations - 10 Accessible, Distinct, and Well-Tuned Precision Physics Levels
    const LEVELS = [
        // Level 1: Training Curve (Gentle introductory slope, wide margin)
        {
            title: "Level 1: Training Curve",
            hint: "Pull back from the ball to aim. Aim for the elevated ramp crest with smooth speed.",
            spawn: { x: -7.8, y: -1.2 },
            pad: { x: -7.8, y: -1.65, w: 2.6, h: 0.45 },
            p0: { x: 0.5, y: 2.6 },
            p1: { x: 2.4, y: 2.2 },
            p2: { x: 4.6, y: -0.6 },
            p3: { x: 6.2, y: -3.2 },
            angleTolerance: 30.0,
            minSpeed: 3.0,
            maxSpeed: 25.0,
            goal: { x: 6.5, y: -3.6, w: 2.4, h: 1.5 },
            obstacle: null
        },
        // Level 2: Precision Curve (Elevated crest, graceful curve)
        {
            title: "Level 2: Precision Curve",
            hint: "Steeper approach angle. Control your launch arc to glide into the curved slope.",
            spawn: { x: -8.2, y: -1.8 },
            pad: { x: -8.2, y: -2.25, w: 2.2, h: 0.45 },
            p0: { x: 0.8, y: 3.0 },
            p1: { x: 2.8, y: 2.6 },
            p2: { x: 5.0, y: 0.2 },
            p3: { x: 6.6, y: -3.4 },
            angleTolerance: 28.0,
            minSpeed: 3.0,
            maxSpeed: 25.0,
            goal: { x: 6.9, y: -3.8, w: 2.4, h: 1.5 },
            obstacle: null
        },
        // Level 3: Apex Challenge (The Hurdle barrier)
        {
            title: "Level 3: Apex Challenge",
            hint: "Clear the central barrier hurdle and execute a smooth high-speed ramp glide into the goal!",
            spawn: { x: -8.5, y: -2.0 },
            pad: { x: -8.5, y: -2.45, w: 2.0, h: 0.45 },
            p0: { x: 0.8, y: 3.2 },
            p1: { x: 2.8, y: 2.8 },
            p2: { x: 5.2, y: 0.4 },
            p3: { x: 6.8, y: -3.5 },
            angleTolerance: 28.0,
            minSpeed: 3.0,
            maxSpeed: 25.0,
            goal: { x: 7.1, y: -3.9, w: 2.4, h: 1.5 },
            obstacle: { x: -1.5, y: -0.9, w: 0.6, h: 1.8 }
        },
        // Level 4: Valley Dip (Trough ramp)
        {
            title: "Level 4: Valley Dip",
            hint: "Launch from the high platform down into the sweeping valley curve.",
            spawn: { x: -8.0, y: 0.2 },
            pad: { x: -8.0, y: -0.25, w: 2.4, h: 0.45 },
            p0: { x: -0.8, y: 1.6 },
            p1: { x: 1.6, y: -0.8 },
            p2: { x: 4.4, y: -2.8 },
            p3: { x: 6.5, y: -3.2 },
            angleTolerance: 30.0,
            minSpeed: 3.0,
            maxSpeed: 25.0,
            goal: { x: 6.8, y: -3.6, w: 2.4, h: 1.5 },
            obstacle: null
        },
        // Level 5: The Twin Spire (Midfield Low Gate)
        {
            title: "Level 5: The Twin Spire",
            hint: "Arc cleanly over the midfield barrier to reach the ramp entry crest.",
            spawn: { x: -8.4, y: -1.6 },
            pad: { x: -8.4, y: -2.05, w: 2.2, h: 0.45 },
            p0: { x: 0.8, y: 3.0 },
            p1: { x: 2.8, y: 2.6 },
            p2: { x: 5.0, y: 0.1 },
            p3: { x: 6.6, y: -3.4 },
            angleTolerance: 28.0,
            minSpeed: 3.0,
            maxSpeed: 25.0,
            goal: { x: 6.9, y: -3.8, w: 2.4, h: 1.5 },
            obstacle: { x: -2.4, y: -1.1, w: 0.6, h: 1.6 }
        },
        // Level 6: Skyward Crest (Calibrated for comfortable reach & exhilarating dive)
        {
            title: "Level 6: Skyward Crest",
            hint: "High-angle lob shot! Aim for the crest of the ramp for a fast roller-coaster dive.",
            spawn: { x: -8.5, y: -1.8 },
            pad: { x: -8.5, y: -2.25, w: 2.2, h: 0.45 },
            p0: { x: 0.5, y: 3.2 },
            p1: { x: 2.6, y: 2.8 },
            p2: { x: 5.0, y: 0.3 },
            p3: { x: 6.8, y: -3.4 },
            angleTolerance: 28.0,
            minSpeed: 3.0,
            maxSpeed: 25.0,
            goal: { x: 7.1, y: -3.8, w: 2.4, h: 1.5 },
            obstacle: null
        },
        // Level 7: Canyon Chasm (Long-Range Glider)
        {
            title: "Level 7: Canyon Chasm",
            hint: "Long-range launch! Clear the deep chasm hazard to reach the distant ramp.",
            spawn: { x: -8.6, y: -0.6 },
            pad: { x: -8.6, y: -1.05, w: 2.2, h: 0.45 },
            p0: { x: 0.8, y: 2.6 },
            p1: { x: 3.0, y: 2.2 },
            p2: { x: 5.4, y: -0.5 },
            p3: { x: 7.0, y: -3.4 },
            angleTolerance: 28.0,
            minSpeed: 3.0,
            maxSpeed: 25.0,
            goal: { x: 7.3, y: -3.8, w: 2.5, h: 1.5 },
            obstacle: { x: -2.2, y: -2.2, w: 0.8, h: 2.2 }
        },
        // Level 8: S-Bend Runway (Curved Runway)
        {
            title: "Level 8: S-Bend Runway",
            hint: "Aim for the wide entry shelf and enjoy a sweeping high-velocity slide.",
            spawn: { x: -8.2, y: -1.4 },
            pad: { x: -8.2, y: -1.85, w: 2.2, h: 0.45 },
            p0: { x: 0.4, y: 3.0 },
            p1: { x: 2.4, y: 2.6 },
            p2: { x: 4.8, y: 0.4 },
            p3: { x: 6.6, y: -3.4 },
            angleTolerance: 28.0,
            minSpeed: 3.0,
            maxSpeed: 25.0,
            goal: { x: 6.9, y: -3.8, w: 2.4, h: 1.5 },
            obstacle: { x: -2.8, y: -1.0, w: 0.6, h: 1.6 }
        },
        // Level 9: The Gauntlet (High Barrier Hurdle)
        {
            title: "Level 9: The Gauntlet",
            hint: "Power and angle mastery! Send a towering shot over the gauntlet pillar.",
            spawn: { x: -8.5, y: -1.8 },
            pad: { x: -8.5, y: -2.25, w: 2.0, h: 0.45 },
            p0: { x: 0.6, y: 3.2 },
            p1: { x: 2.8, y: 2.8 },
            p2: { x: 5.2, y: 0.4 },
            p3: { x: 6.8, y: -3.4 },
            angleTolerance: 28.0,
            minSpeed: 3.0,
            maxSpeed: 25.0,
            goal: { x: 7.1, y: -3.8, w: 2.4, h: 1.5 },
            obstacle: { x: -2.2, y: -0.8, w: 0.6, h: 1.8 }
        },
        // Level 10: Master's Spiral (Grand Finale)
        {
            title: "Level 10: Master's Spiral",
            hint: "GRAND FINALE! Full power, perfect arc, and a majestic ride into the Champion's Goal!",
            spawn: { x: -8.6, y: -1.6 },
            pad: { x: -8.6, y: -2.05, w: 2.4, h: 0.45 },
            p0: { x: 0.6, y: 3.4 },
            p1: { x: 2.8, y: 3.0 },
            p2: { x: 5.4, y: 0.6 },
            p3: { x: 7.0, y: -3.3 },
            angleTolerance: 28.0,
            minSpeed: 3.0,
            maxSpeed: 25.0,
            goal: { x: 7.3, y: -3.7, w: 2.6, h: 1.6 },
            obstacle: { x: -2.5, y: -0.9, w: 0.6, h: 1.6 }
        }
    ];

    // Game State
    let currentLevelIndex = 0;
    let attemptsCount = 0;
    let gameState = 'Ready'; // 'Ready', 'Aiming', 'InFlight', 'OnRamp', 'Success', 'Failed'
    let flightTimer = 0;

    // Projectile Properties
    const BALL_RADIUS = 0.35; // world units
    let ballPos = { x: -7.8, y: -1.2 };
    let ballVel = { x: 0, y: 0 };
    let preCollisionVel = { x: 0, y: 0 };
    const GRAVITY = -15.696; // 9.81 * 1.6 scale
    let ballTrail = [];
    let ballSquash = { x: 1, y: 1 };

    // Aim Controls - Boosted power & drag headroom
    let isDragging = false;
    let dragStartPos = { x: 0, y: 0 };
    let currentDragPos = { x: 0, y: 0 };
    let calculatedLaunchVel = { x: 0, y: 0 };
    const MIN_LAUNCH_SPEED = 3.5;
    const MAX_LAUNCH_SPEED = 23.5;
    const MAX_DRAG_DIST = 3.6;

    // Curve Sampled Points
    let rampPoints = [];
    let isRampHighlighted = false;

    // Particles System
    let particles = [];

    function spawnParticles(x, y, count, color, speedRange, life) {
        for (let i = 0; i < count; i++) {
            const angle = Math.random() * Math.PI * 2;
            const spd = speedRange[0] + Math.random() * (speedRange[1] - speedRange[0]);
            particles.push({
                x: x,
                y: y,
                vx: Math.cos(angle) * spd,
                vy: Math.sin(angle) * spd,
                color: color,
                alpha: 1.0,
                life: life,
                maxLife: life,
                size: 2 + Math.random() * 3
            });
        }
    }

    // Camera Shake
    let cameraShake = { x: 0, y: 0, timer: 0, magnitude: 0 };

    function triggerCameraShake(mag, duration) {
        cameraShake.magnitude = mag;
        cameraShake.timer = duration;
    }

    // Level Loader
    function loadLevel(index) {
        currentLevelIndex = (index + LEVELS.length) % LEVELS.length;
        const level = LEVELS[currentLevelIndex];
        attemptsCount = 0;

        uiLevelTitle.textContent = level.title;
        uiLevelHint.textContent = level.hint;
        uiAttemptBadge.textContent = `Attempt: ${attemptsCount}`;

        // Sample Ramp Points (60 samples)
        rampPoints = [];
        const samples = 60;
        for (let i = 0; i < samples; i++) {
            const t = i / (samples - 1);
            rampPoints.push(evaluateCubicBezier(level.p0, level.p1, level.p2, level.p3, t));
        }

        resetToReady();
    }

    function resetToReady() {
        const level = LEVELS[currentLevelIndex];
        gameState = 'Ready';
        isDragging = false;
        isRampHighlighted = false;
        flightTimer = 0;

        ballPos = { x: level.spawn.x, y: level.spawn.y };
        ballVel = { x: 0, y: 0 };
        preCollisionVel = { x: 0, y: 0 };
        calculatedLaunchVel = { x: 0, y: 0 };
        ballTrail = [];
        ballSquash = { x: 1, y: 1 };

        uiSuccessModal.style.display = 'none';
        uiFailureModal.style.display = 'none';
        uiFeedbackBanner.style.display = 'none';
        updatePowerUI(0);
    }

    function updatePowerUI(powerRatio) {
        const clamped = Math.max(0, Math.min(1, powerRatio));
        const pct = Math.round(clamped * 100);
        uiPowerFill.style.width = `${pct}%`;
        uiPowerText.textContent = `${pct}%`;
    }

    function showBanner(text, color) {
        uiFeedbackBanner.textContent = text;
        uiFeedbackBanner.style.color = color;
        uiFeedbackBanner.style.display = 'block';
    }

    // Input Handling (Pointer / Touch)
    function getPointerWorldPos(e) {
        const rect = canvas.getBoundingClientRect();
        const clientX = e.touches ? e.touches[0].clientX : e.clientX;
        const clientY = e.touches ? e.touches[0].clientY : e.clientY;
        const sx = (clientX - rect.left) * (canvasWidth / rect.width);
        const sy = (clientY - rect.top) * (canvasHeight / rect.height);
        return screenToWorld(sx, sy);
    }

    function onPointerDown(e) {
        if (gameState !== 'Ready') return;
        const p = getPointerWorldPos(e);
        const dist = Math.hypot(p.x - ballPos.x, p.y - ballPos.y);

        // Allow click on or near ball
        if (dist < 4.0 && p.x < 1.0) {
            isDragging = true;
            dragStartPos = { x: ballPos.x, y: ballPos.y };
            currentDragPos = p;
            gameState = 'Aiming';
            initAudio();
        }
    }

    function onPointerMove(e) {
        if (!isDragging || gameState !== 'Aiming') return;
        currentDragPos = getPointerWorldPos(e);

        // Slingshot vector: drag backward
        const dx = ballPos.x - currentDragPos.x;
        const dy = ballPos.y - currentDragPos.y;
        const dragDist = Math.hypot(dx, dy);

        if (dragDist < 0.2) {
            calculatedLaunchVel = { x: 0, y: 0 };
            updatePowerUI(0);
            return;
        }

        let rawAngle = Math.atan2(dy, dx) * (180 / Math.PI);
        // Clamp launch angle between 12 and 78 degrees
        let clampedAngle = Math.max(12, Math.min(78, rawAngle));
        let rad = clampedAngle * (Math.PI / 180);
        let dir = { x: Math.cos(rad), y: Math.sin(rad) };

        let powerRatio = Math.min(dragDist, MAX_DRAG_DIST) / MAX_DRAG_DIST;
        let speed = MIN_LAUNCH_SPEED + powerRatio * (MAX_LAUNCH_SPEED - MIN_LAUNCH_SPEED);

        calculatedLaunchVel = { x: dir.x * speed, y: dir.y * speed };
        updatePowerUI(powerRatio);
    }

    function onPointerUp(e) {
        if (!isDragging) return;
        isDragging = false;

        if (calculatedLaunchVel.x === 0 && calculatedLaunchVel.y === 0) {
            gameState = 'Ready';
            updatePowerUI(0);
            return;
        }

        // Fire Projectile
        gameState = 'InFlight';
        attemptsCount++;
        uiAttemptBadge.textContent = `Attempt: ${attemptsCount}`;
        ballVel = { x: calculatedLaunchVel.x, y: calculatedLaunchVel.y };
        preCollisionVel = { x: ballVel.x, y: ballVel.y };
        updatePowerUI(0);

        // Launch Squash Feedback
        ballSquash = { x: 1.4, y: 0.7 };
        spawnParticles(ballPos.x, ballPos.y, 16, '#93c5fd', [1.5, 4.0], 0.4);
        playSound('launch');
    }

    canvas.addEventListener('mousedown', onPointerDown);
    window.addEventListener('mousemove', onPointerMove);
    window.addEventListener('mouseup', onPointerUp);

    canvas.addEventListener('touchstart', (e) => { e.preventDefault(); onPointerDown(e); }, { passive: false });
    window.addEventListener('touchmove', (e) => { onPointerMove(e); }, { passive: false });
    window.addEventListener('touchend', (e) => { onPointerUp(e); }, { passive: false });

    // Physics Engine Update
    let lastTime = performance.now();

    function updatePhysics(dt) {
        if (gameState !== 'InFlight' && gameState !== 'OnRamp') return;

        flightTimer += dt;
        // Watchdog timeout (6.5s)
        if (flightTimer > 6.5) {
            triggerFailure('Timeout', 'Flight duration exceeded limit without resolution.');
            return;
        }

        const level = LEVELS[currentLevelIndex];

        // Cache pre-collision velocity
        preCollisionVel = { x: ballVel.x, y: ballVel.y };

        if (gameState === 'InFlight') {
            // Ballistics Integration
            ballVel.y += GRAVITY * dt;
            ballPos.x += ballVel.x * dt;
            ballPos.y += ballVel.y * dt;

            // Cache trail
            ballTrail.push({ x: ballPos.x, y: ballPos.y, alpha: 1.0 });
            if (ballTrail.length > 25) ballTrail.shift();

            // Check Obstacle collision (Level 3)
            if (level.obstacle) {
                const obs = level.obstacle;
                if (ballPos.x >= obs.x - obs.w / 2 && ballPos.x <= obs.x + obs.w / 2 &&
                    ballPos.y >= obs.y - obs.h / 2 && ballPos.y <= obs.y + obs.h / 2) {
                    triggerFailure('Obstacle Hit', 'Ball struck the central hazard barrier hurdle!');
                    return;
                }
            }

            // Check Ramp Collision with generous capture margin
            const closest = findClosestTOnCurve(rampPoints, ballPos.x, ballPos.y);
            if (closest.dist <= BALL_RADIUS + 0.35 && closest.t <= 0.60) {
                evaluateRampContact(closest, level);
                return;
            }

            // Check Out of Bounds
            if (ballPos.y < -5.5 || ballPos.x > 11.0 || ballPos.x < -11.5) {
                triggerFailure('Out of Bounds', 'Ball missed the ramp and fell into the bottom hazard pit.');
                return;
            }

        } else if (gameState === 'OnRamp') {
            // Constrained surface traversal along ramp tangent with smooth downward momentum
            const closest = findClosestTOnCurve(rampPoints, ballPos.x, ballPos.y);
            const tan = evaluateCubicTangent(level.p0, level.p1, level.p2, level.p3, closest.t);

            // Tangential gravity acceleration: ensures solid, satisfying downward glide
            const gTangent = Math.abs(GRAVITY * tan.y);
            const currentSpeed = Math.max(5.5, Math.hypot(ballVel.x, ballVel.y) + (gTangent * dt * 0.5));

            ballVel.x = tan.x * currentSpeed;
            ballVel.y = tan.y * currentSpeed;

            ballPos.x = closest.point.x + (tan.x * dt * currentSpeed);
            ballPos.y = closest.point.y + (tan.y * dt * currentSpeed);

            ballTrail.push({ x: ballPos.x, y: ballPos.y, alpha: 1.0 });
            if (ballTrail.length > 25) ballTrail.shift();

            // Spawn sliding spark trail
            if (Math.random() < 0.45) {
                spawnParticles(ballPos.x, ballPos.y, 2, '#67e8f9', [0.5, 1.8], 0.25);
            }

            // Once the ball glides to the base of the ramp (t >= 0.90), deliver directly into the goal container
            if (closest.t >= 0.90) {
                ballPos.x = level.goal.x;
                ballPos.y = level.goal.y;
                triggerSuccess(level);
                return;
            }
        }
    }

    function evaluateRampContact(closest, level) {
        const incomingSpeed = Math.hypot(preCollisionVel.x, preCollisionVel.y);
        const rampTan = evaluateCubicTangent(level.p0, level.p1, level.p2, level.p3, closest.t);

        // Vector angle between incoming velocity and ramp tangent
        const dot = (preCollisionVel.x * rampTan.x + preCollisionVel.y * rampTan.y);
        const cosTheta = Math.max(-1, Math.min(1, dot / (incomingSpeed * 1.0)));
        const angleDev = Math.acos(cosTheta) * (180 / Math.PI);

        // 1. Entry zone check - generous upper half of ramp
        if (closest.t > 0.55) {
            triggerFailure('Incorrect Angle', `Mid-Ramp Impact (t=${closest.t.toFixed(2)})!\nMust land on upper entry crest.`);
            return;
        }

        // 2. Insufficient speed check
        if (incomingSpeed < level.minSpeed) {
            triggerFailure('Insufficient Speed', `Speed too low (${incomingSpeed.toFixed(1)} m/s)!\nMin required: ${level.minSpeed.toFixed(1)} m/s.`);
            return;
        }

        // 3. Excessive speed check
        if (incomingSpeed > level.maxSpeed) {
            triggerFailure('Overpowered Speed', `Overpowered (${incomingSpeed.toFixed(1)} m/s)!\nMax allowed: ${level.maxSpeed.toFixed(1)} m/s.`);
            return;
        }

        // 4. Angle deviation tolerance check
        if (angleDev > level.angleTolerance) {
            triggerFailure('Incorrect Angle', `Steep Entry Angle (${angleDev.toFixed(1)}°)!\nAllowed tolerance: ±${level.angleTolerance.toFixed(1)}°.`);
            return;
        }

        // --- SUCCESSFUL RAMP ENTRY ---
        gameState = 'OnRamp';
        isRampHighlighted = true;
        ballVel = { x: rampTan.x * incomingSpeed, y: rampTan.y * incomingSpeed };

        showBanner('PERFECT GLIDE! Surface Acquired', '#4ade80');
        spawnParticles(closest.point.x, closest.point.y, 18, '#fbbf24', [1.5, 3.5], 0.35);
        playSound('impact');
    }

    function checkGoalArrival(level) {
        const goal = level.goal;
        const dx = Math.abs(ballPos.x - goal.x);
        const dy = Math.abs(ballPos.y - goal.y);

        if (dx < goal.w / 2 + 0.3 && dy < goal.h / 2 + 0.3) {
            triggerSuccess(level);
        } else {
            // Missed target box at bottom
            triggerFailure('Missed Target', 'Ball exited the ramp but bounced past the collection box.');
        }
    }

    function triggerSuccess(level) {
        gameState = 'Success';
        ballVel = { x: 0, y: 0 };
        uiFeedbackBanner.style.display = 'none';

        const incomingSpeed = Math.hypot(preCollisionVel.x, preCollisionVel.y);
        const modalHeader = uiSuccessModal.querySelector('.modal-header');

        if (currentLevelIndex === LEVELS.length - 1) {
            // Level 10 Grand Finale Victory
            modalHeader.textContent = "🏆 ALL 10 LEVELS CONQUERED!";
            uiSuccessBody.textContent = `MASTER OF PRECISION PHYSICS!\n\nYou have completed every challenge with flawless surface traversal!\nFinal Shot Speed: ${incomingSpeed.toFixed(1)} m/s`;
            uiNextLevelBtn.textContent = "Play Again from Level 1";
        } else {
            modalHeader.textContent = "EXCELLENT SHOT!";
            uiSuccessBody.textContent = `Completed in ${attemptsCount} attempt(s)!\nPrecision Entry Velocity: ${incomingSpeed.toFixed(1)} m/s\nSurface Traversal: 100% Flawless`;
            uiNextLevelBtn.textContent = `Next Level (${currentLevelIndex + 2}/10)`;
        }

        uiSuccessModal.style.display = 'flex';

        spawnParticles(level.goal.x, level.goal.y, 50, '#34d399', [2.0, 7.0], 0.9);
        spawnParticles(level.goal.x, level.goal.y, 40, '#fbbf24', [1.5, 5.0], 0.7);
        playSound('success');
    }

    function triggerFailure(title, details) {
        gameState = 'Failed';
        // Deflect velocity to create visible crash/ricochet
        ballVel = { x: -Math.abs(preCollisionVel.x) * 0.4, y: 3.2 };

        triggerCameraShake(8, 0.28);
        spawnParticles(ballPos.x, ballPos.y, 22, '#f87171', [2.0, 5.0], 0.45);
        playSound('failure');

        setTimeout(() => {
            uiFailureTitle.textContent = title.toUpperCase();
            uiFailureBody.textContent = details;
            uiFailureModal.style.display = 'flex';
        }, 320);
    }

    // Main Render Loop
    function render() {
        ctx.clearRect(0, 0, canvasWidth, canvasHeight);

        // Apply Camera Shake
        ctx.save();
        if (cameraShake.timer > 0) {
            const decay = cameraShake.timer / 0.28;
            const ox = (Math.random() * 2 - 1) * cameraShake.magnitude * decay;
            const oy = (Math.random() * 2 - 1) * cameraShake.magnitude * decay;
            ctx.translate(ox, oy);
        }

        const level = LEVELS[currentLevelIndex];

        // 1. Draw Subtle Background Grid
        drawBackgroundGrid();

        // 2. Draw Launch Platform
        drawLaunchPlatform(level);

        // 3. Draw Curved Ramp (SurfacePath)
        drawCurvedRamp(level);

        // 4. Draw Collection Goal Box
        drawGoalBox(level);

        // 5. Draw Obstacle (if present)
        if (level.obstacle) drawObstacle(level.obstacle);

        // 6. Draw Trajectory Preview
        if (gameState === 'Aiming' && (calculatedLaunchVel.x !== 0 || calculatedLaunchVel.y !== 0)) {
            drawTrajectoryPreview(ballPos, calculatedLaunchVel);
        }

        // 7. Draw Projectile Motion Trail
        drawBallTrail();

        // 8. Draw Projectile Ball
        drawBall();

        // 9. Draw Active Particles
        drawParticles();

        ctx.restore();
    }

    function drawBackgroundGrid() {
        ctx.strokeStyle = 'rgba(255, 255, 255, 0.03)';
        ctx.lineWidth = 1;
        const step = 64; // 1 world unit
        for (let x = 0; x < canvasWidth; x += step) {
            ctx.beginPath();
            ctx.moveTo(x, 0);
            ctx.lineTo(x, canvasHeight);
            ctx.stroke();
        }
        for (let y = 0; y < canvasHeight; y += step) {
            ctx.beginPath();
            ctx.moveTo(0, y);
            ctx.lineTo(canvasWidth, y);
            ctx.stroke();
        }
    }

    function drawLaunchPlatform(level) {
        const pad = level.pad;
        const topLeft = worldToScreen(pad.x - pad.w / 2, pad.y + pad.h / 2);
        const w = pad.w * PIXELS_PER_UNIT;
        const h = pad.h * PIXELS_PER_UNIT;

        ctx.fillStyle = '#1e293b';
        ctx.fillRect(topLeft.x, topLeft.y, w, h);

        // Neon Top Indicator strip
        ctx.fillStyle = '#38bdf8';
        ctx.fillRect(topLeft.x, topLeft.y, w, 4);
    }

    function drawCurvedRamp(level) {
        if (rampPoints.length < 2) return;

        // Visual ramp track with thickness & glow
        ctx.lineCap = 'round';
        ctx.lineJoin = 'round';

        // Outer glow
        ctx.strokeStyle = isRampHighlighted ? 'rgba(52, 211, 153, 0.4)' : 'rgba(56, 189, 248, 0.25)';
        ctx.lineWidth = isRampHighlighted ? 22 : 16;
        ctx.beginPath();
        for (let i = 0; i < rampPoints.length; i++) {
            const s = worldToScreen(rampPoints[i].x, rampPoints[i].y);
            if (i === 0) ctx.moveTo(s.x, s.y);
            else ctx.lineTo(s.x, s.y);
        }
        ctx.stroke();

        // Main core line
        ctx.strokeStyle = isRampHighlighted ? '#34d399' : '#0284c7';
        ctx.lineWidth = 7;
        ctx.stroke();

        // Entry zone highlight bracket (generous upper 50% of ramp)
        const entryEndPt = rampPoints[Math.floor(rampPoints.length * 0.50)];
        const entryStartPt = rampPoints[0];
        const sStart = worldToScreen(entryStartPt.x, entryStartPt.y);
        const sEnd = worldToScreen(entryEndPt.x, entryEndPt.y);

        ctx.strokeStyle = 'rgba(251, 191, 36, 0.65)';
        ctx.lineWidth = 3;
        ctx.setLineDash([4, 4]);
        ctx.beginPath();
        for (let i = 0; i <= Math.floor(rampPoints.length * 0.50); i++) {
            const s = worldToScreen(rampPoints[i].x, rampPoints[i].y);
            if (i === 0) ctx.moveTo(s.x, s.y);
            else ctx.lineTo(s.x, s.y);
        }
        ctx.stroke();
        ctx.setLineDash([]);

        // Entry Crest Label
        ctx.fillStyle = '#f59e0b';
        ctx.font = '11px sans-serif';
        ctx.fillText('ENTRY CREST', sStart.x - 25, sStart.y - 18);
    }

    function drawGoalBox(level) {
        const goal = level.goal;
        const s = worldToScreen(goal.x, goal.y);
        const w = goal.w * PIXELS_PER_UNIT;
        const h = goal.h * PIXELS_PER_UNIT;

        // Basket box shape (open top)
        ctx.strokeStyle = '#10b981';
        ctx.lineWidth = 4;
        ctx.beginPath();
        ctx.moveTo(s.x - w / 2, s.y - h / 2);
        ctx.lineTo(s.x - w / 2, s.y + h / 2);
        ctx.lineTo(s.x + w / 2, s.y + h / 2);
        ctx.lineTo(s.x + w / 2, s.y - h / 2);
        ctx.stroke();

        // Interior target glow
        ctx.fillStyle = gameState === 'Success' ? 'rgba(16, 185, 129, 0.35)' : 'rgba(16, 185, 129, 0.12)';
        ctx.fillRect(s.x - w / 2, s.y - h / 2, w, h);

        ctx.fillStyle = '#10b981';
        ctx.font = 'bold 12px sans-serif';
        ctx.textAlign = 'center';
        ctx.fillText('GOAL', s.x, s.y + 5);
        ctx.textAlign = 'left';
    }

    function drawObstacle(obs) {
        const topLeft = worldToScreen(obs.x - obs.w / 2, obs.y + obs.h / 2);
        const w = obs.w * PIXELS_PER_UNIT;
        const h = obs.h * PIXELS_PER_UNIT;

        ctx.fillStyle = '#ef4444';
        ctx.fillRect(topLeft.x, topLeft.y, w, h);
        ctx.strokeStyle = '#fca5a5';
        ctx.lineWidth = 2;
        ctx.strokeRect(topLeft.x, topLeft.y, w, h);

        ctx.fillStyle = '#ffffff';
        ctx.font = '10px sans-serif';
        ctx.textAlign = 'center';
        ctx.fillText('BARRIER', topLeft.x + w / 2, topLeft.y + h / 2);
        ctx.textAlign = 'left';
    }

    function drawTrajectoryPreview(origin, launchVel) {
        ctx.strokeStyle = 'rgba(251, 191, 36, 0.85)';
        ctx.fillStyle = '#fbbf24';
        ctx.lineWidth = 2;
        ctx.setLineDash([4, 6]);

        ctx.beginPath();
        const steps = 55;
        const dt = 0.040;
        for (let i = 0; i < steps; i++) {
            const t = i * dt;
            const px = origin.x + launchVel.x * t;
            const py = origin.y + launchVel.y * t + 0.5 * GRAVITY * t * t;
            const s = worldToScreen(px, py);
            if (i === 0) ctx.moveTo(s.x, s.y);
            else ctx.lineTo(s.x, s.y);

            // Small reticle dots
            if (i % 6 === 0 && i > 0) {
                ctx.beginPath();
                ctx.arc(s.x, s.y, 3.5, 0, Math.PI * 2);
                ctx.fill();
            }
        }
        ctx.stroke();
        ctx.setLineDash([]);
    }

    function drawBallTrail() {
        for (let i = 0; i < ballTrail.length; i++) {
            const pt = ballTrail[i];
            const s = worldToScreen(pt.x, pt.y);
            const radius = (BALL_RADIUS * PIXELS_PER_UNIT) * (i / ballTrail.length) * 0.7;
            ctx.fillStyle = `rgba(56, 189, 248, ${(i / ballTrail.length) * 0.4})`;
            ctx.beginPath();
            ctx.arc(s.x, s.y, radius, 0, Math.PI * 2);
            ctx.fill();
        }
    }

    function drawBall() {
        const s = worldToScreen(ballPos.x, ballPos.y);
        const r = BALL_RADIUS * PIXELS_PER_UNIT;

        ctx.save();
        ctx.translate(s.x, s.y);
        ctx.scale(ballSquash.x, ballSquash.y);

        // Glow
        const radGlow = ctx.createRadialGradient(0, 0, r * 0.3, 0, 0, r * 1.6);
        radGlow.addColorStop(0, 'rgba(56, 189, 248, 0.6)');
        radGlow.addColorStop(1, 'rgba(56, 189, 248, 0)');
        ctx.fillStyle = radGlow;
        ctx.beginPath();
        ctx.arc(0, 0, r * 1.6, 0, Math.PI * 2);
        ctx.fill();

        // Solid Body
        const radBody = ctx.createRadialGradient(-r * 0.3, -r * 0.3, r * 0.1, 0, 0, r);
        radBody.addColorStop(0, '#bae6fd');
        radBody.addColorStop(0.7, '#0ea5e9');
        radBody.addColorStop(1, '#0369a1');

        ctx.fillStyle = radBody;
        ctx.beginPath();
        ctx.arc(0, 0, r, 0, Math.PI * 2);
        ctx.fill();

        ctx.restore();

        // Restore squash over time
        ballSquash.x += (1.0 - ballSquash.x) * 0.15;
        ballSquash.y += (1.0 - ballSquash.y) * 0.15;
    }

    function drawParticles() {
        for (let i = particles.length - 1; i >= 0; i--) {
            const p = particles[i];
            const s = worldToScreen(p.x, p.y);
            ctx.fillStyle = p.color;
            ctx.globalAlpha = p.alpha;
            ctx.beginPath();
            ctx.arc(s.x, s.y, p.size, 0, Math.PI * 2);
            ctx.fill();
        }
        ctx.globalAlpha = 1.0;
    }

    function updateParticles(dt) {
        for (let i = particles.length - 1; i >= 0; i--) {
            const p = particles[i];
            p.x += p.vx * dt;
            p.y += p.vy * dt;
            p.life -= dt;
            p.alpha = Math.max(0, p.life / p.maxLife);
            if (p.life <= 0) particles.splice(i, 1);
        }
    }

    // Main Engine Tick
    function tick(now) {
        const dt = Math.min((now - lastTime) / 1000, 0.05);
        lastTime = now;

        updatePhysics(dt);
        updateParticles(dt);

        if (cameraShake.timer > 0) {
            cameraShake.timer -= dt;
        }

        render();
        requestAnimationFrame(tick);
    }

    // UI Event Listeners
    uiMuteBtn.addEventListener('click', () => {
        isMuted = !isMuted;
        uiMuteBtn.textContent = isMuted ? 'Sound: OFF' : 'Sound: ON';
    });

    uiRestartBtn.addEventListener('click', resetToReady);
    uiFailureRetryBtn.addEventListener('click', resetToReady);
    uiSuccessReplayBtn.addEventListener('click', resetToReady);
    uiNextLevelBtn.addEventListener('click', () => {
        loadLevel(currentLevelIndex + 1);
    });

    // Start Game
    loadLevel(0);
    requestAnimationFrame(tick);

})();
