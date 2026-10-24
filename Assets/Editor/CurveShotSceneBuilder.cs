#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using CurveShot.Core;
using CurveShot.Physics;
using CurveShot.Level;
using CurveShot.UI;
using CurveShot.Feedback;
using CurveShot.Audio;

namespace CurveShot.Editor
{
    /// <summary>
    /// Deterministic Editor tool that constructs, configures, and wires the entire
    /// "CURVE SHOT — Precision Physics" scene hierarchy, physics, colliders, particles, and UI.
    /// Accessible via menu: Tools > Curve Shot > Setup / Rebuild Scene.
    /// </summary>
    public class CurveShotSceneBuilder : EditorWindow
    {
        [MenuItem("Tools/Curve Shot/Setup or Rebuild Scene")]
        public static void BuildScene()
        {
            if (EditorUtility.DisplayDialog(
                "Curve Shot Scene Builder",
                "This will build or rebuild the complete, fully configured Curve Shot scene in 'Assets/Scenes/MainGameScene.unity'.\n\nAll GameObjects, colliders, references, and UI will be automatically wired.\n\nProceed?",
                "Yes, Build Scene",
                "Cancel"))
            {
                ExecuteBuild();
            }
        }

        private static void ExecuteBuild()
        {
            // 1. Create a fresh scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 2. Main Camera
            GameObject cameraObj = new GameObject("Main Camera");
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.07f, 0.12f, 1f); // Dark Charcoal / Navy
            cam.orthographic = true;
            cam.orthographicSize = 6.8f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            cameraObj.transform.position = new Vector3(0f, 0f, -10f);
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";

            // 3. Manager Hierarchy
            GameObject managersRoot = new GameObject("--- MANAGERS ---");
            GameManager gameManager = managersRoot.AddComponent<GameManager>();
            LevelController levelController = managersRoot.AddComponent<LevelController>();
            AudioController audioController = managersRoot.AddComponent<AudioController>();
            AudioSource audioSource = managersRoot.GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            FeedbackController feedbackController = managersRoot.AddComponent<FeedbackController>();

            // 4. Environment Hierarchy
            GameObject envRoot = new GameObject("--- ENVIRONMENT ---");

            // 4a. Launch Platform
            GameObject launchPad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            launchPad.name = "LaunchPlatform";
            launchPad.transform.SetParent(envRoot.transform);
            launchPad.transform.position = new Vector3(-7.8f, -1.65f, 0f);
            launchPad.transform.localScale = new Vector3(2.6f, 0.45f, 1f);
            DestroyImmediate(launchPad.GetComponent<MeshCollider>());
            BoxCollider2D padCol = launchPad.AddComponent<BoxCollider2D>();
            padCol.size = Vector2.one;

            // 4b. Curved Ramp
            GameObject rampObj = new GameObject("CurvedRamp");
            rampObj.transform.SetParent(envRoot.transform);
            rampObj.transform.position = Vector3.zero;
            SurfacePath surfacePath = rampObj.AddComponent<SurfacePath>();
            SurfaceInteraction surfaceInteraction = rampObj.AddComponent<SurfaceInteraction>();
            LineRenderer rampLine = rampObj.AddComponent<LineRenderer>();
            ConfigureLineRendererVisuals(rampLine);

            // Load physics materials
            PhysicsMaterial2D rampMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Materials/RampPhysicMaterial.physicsMaterial2D");
            if (rampMat != null)
            {
                EdgeCollider2D edgeCol = rampObj.GetComponent<EdgeCollider2D>();
                if (edgeCol != null) edgeCol.sharedMaterial = rampMat;
            }

            // 4c. Goal / Target Box
            GameObject goalRoot = new GameObject("GoalBox");
            goalRoot.transform.SetParent(envRoot.transform);
            goalRoot.transform.position = new Vector3(6.8f, -3.8f, 0f);

            // Visual basket base
            GameObject goalVisual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            goalVisual.name = "GoalBasket_Visual";
            goalVisual.transform.SetParent(goalRoot.transform);
            goalVisual.transform.localPosition = Vector3.zero;
            goalVisual.transform.localScale = new Vector3(1.8f, 1.2f, 1f);
            DestroyImmediate(goalVisual.GetComponent<MeshCollider>());

            // Goal trigger collider & GoalZone
            BoxCollider2D goalTrigger = goalRoot.AddComponent<BoxCollider2D>();
            goalTrigger.isTrigger = true;
            goalTrigger.size = new Vector2(1.6f, 1.0f);
            GoalZone goalZone = goalRoot.AddComponent<GoalZone>();

            // 4d. Obstacle (For Level 3 Apex Challenge)
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obstacle.name = "HurdleObstacle";
            obstacle.transform.SetParent(envRoot.transform);
            obstacle.transform.position = new Vector3(-1.5f, -0.2f, 0f);
            obstacle.transform.localScale = new Vector3(0.6f, 2.6f, 1f);
            DestroyImmediate(obstacle.GetComponent<MeshCollider>());
            obstacle.AddComponent<BoxCollider2D>();
            obstacle.SetActive(false);

            // 4e. Failure Zones (Bottom Pit, Boundaries)
            GameObject failureRoot = new GameObject("--- FAILURE BOUNDARIES ---");
            FailureZone pitFailure = CreateFailureBoundary(failureRoot.transform, "BottomPit_Hazard", new Vector3(0f, -7.5f, 0f), new Vector2(36f, 3f), FailureReason.OutOfBounds);
            FailureZone leftFailure = CreateFailureBoundary(failureRoot.transform, "LeftWall_Hazard", new Vector3(-13.5f, 0f, 0f), new Vector2(3f, 22f), FailureReason.OutOfBounds);
            FailureZone rightFailure = CreateFailureBoundary(failureRoot.transform, "RightWall_Hazard", new Vector3(13.5f, 0f, 0f), new Vector2(3f, 22f), FailureReason.OutOfBounds);
            FailureZone topFailure = CreateFailureBoundary(failureRoot.transform, "TopCeiling_Hazard", new Vector3(0f, 11f, 0f), new Vector2(36f, 3f), FailureReason.OutOfBounds);

            // 5. Projectile Hierarchy
            GameObject gameplayRoot = new GameObject("--- GAMEPLAY ---");
            GameObject projectileObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObj.name = "Projectile_Ball";
            projectileObj.transform.SetParent(gameplayRoot.transform);
            projectileObj.transform.position = new Vector3(-7.8f, -1.2f, 0f);
            projectileObj.transform.localScale = Vector3.one * 0.7f;
            DestroyImmediate(projectileObj.GetComponent<SphereCollider>());

            Rigidbody2D projRb = projectileObj.AddComponent<Rigidbody2D>();
            CircleCollider2D projCol = projectileObj.AddComponent<CircleCollider2D>();
            projCol.radius = 0.5f;

            PhysicsMaterial2D projMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Materials/ProjectilePhysicMaterial.physicsMaterial2D");
            if (projMat != null) projCol.sharedMaterial = projMat;

            TrailRenderer trail = projectileObj.AddComponent<TrailRenderer>();
            ConfigureTrailRenderer(trail);

            ProjectileController projCtrl = projectileObj.AddComponent<ProjectileController>();
            AimController aimCtrl = projectileObj.AddComponent<AimController>();

            // Trajectory Preview
            GameObject trajectoryObj = new GameObject("TrajectoryPreview");
            trajectoryObj.transform.SetParent(projectileObj.transform);
            trajectoryObj.transform.localPosition = Vector3.zero;
            LineRenderer trajLine = trajectoryObj.AddComponent<LineRenderer>();
            ConfigureTrajectoryLine(trajLine);
            TrajectoryPreview trajPreview = trajectoryObj.AddComponent<TrajectoryPreview>();

            // 6. Particle Systems
            GameObject particlesRoot = new GameObject("--- PARTICLES ---");
            ParticleSystem launchPs = CreateSimpleParticleSystem(particlesRoot.transform, "Launch_Puff", new Color(0.9f, 0.9f, 1f, 0.8f));
            ParticleSystem impactPs = CreateSimpleParticleSystem(particlesRoot.transform, "Impact_Sparks", new Color(1f, 0.8f, 0.2f, 1f));
            ParticleSystem successPs = CreateSimpleParticleSystem(particlesRoot.transform, "Success_Confetti", new Color(0.3f, 1f, 0.6f, 1f));
            ParticleSystem failurePs = CreateSimpleParticleSystem(particlesRoot.transform, "Failure_Shatter", new Color(1f, 0.3f, 0.3f, 1f));

            // 7. UI Canvas & EventSystem
            GameObject uiCanvasObj = new GameObject("UI_Canvas");
            Canvas canvas = uiCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = uiCanvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            uiCanvasObj.AddComponent<GraphicRaycaster>();
            UIController uiController = uiCanvasObj.AddComponent<UIController>();

            // Ensure EventSystem
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // Construct UI elements
            BuildUIElements(uiCanvasObj.transform, uiController);

            // 8. Wire Up All Serialized Fields Deterministically
            WireReferences(
                gameManager, levelController, feedbackController, uiController,
                projCtrl, aimCtrl, trajPreview, surfacePath, surfaceInteraction,
                launchPad.transform, goalRoot.transform, goalZone, obstacle,
                new FailureZone[] { pitFailure, leftFailure, rightFailure, topFailure },
                launchPs, impactPs, successPs, failurePs, cam, projectileObj.transform);

            // 9. Save Scene File
            string scenePath = "Assets/Scenes/MainGameScene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#38bdf8><b>[CurveShot] Scene successfully built and saved to: {scenePath}</b></color>");
            EditorUtility.DisplayDialog("Scene Setup Complete", "The Curve Shot scene has been created and saved at:\n\nAssets/Scenes/MainGameScene.unity\n\nAll components, physics colliders, and UI references are fully wired!", "OK");
        }

        private static FailureZone CreateFailureBoundary(Transform parent, string name, Vector3 pos, Vector2 size, FailureReason reason)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.position = pos;
            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;
            FailureZone fz = obj.AddComponent<FailureZone>();
            return fz;
        }

        private static ParticleSystem CreateSimpleParticleSystem(Transform parent, string name, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = 0.6f;
            main.startSpeed = 4f;
            main.startSize = 0.18f;
            main.startColor = color;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 25) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            return ps;
        }

        private static void ConfigureLineRendererVisuals(LineRenderer line)
        {
            line.useWorldSpace = true;
            line.startWidth = 0.22f;
            line.endWidth = 0.22f;
            line.startColor = new Color(0.18f, 0.75f, 0.95f, 1f);
            line.endColor = new Color(0.18f, 0.75f, 0.95f, 1f);
            line.material = new Material(Shader.Find("Sprites/Default"));
        }

        private static void ConfigureTrailRenderer(TrailRenderer trail)
        {
            trail.time = 0.45f;
            trail.startWidth = 0.25f;
            trail.endWidth = 0.02f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.35f, 0.9f, 1f), 0f), new GradientColorKey(new Color(0.1f, 0.5f, 1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            trail.colorGradient = grad;
            trail.emitting = false;
        }

        private static void ConfigureTrajectoryLine(LineRenderer line)
        {
            line.useWorldSpace = true;
            line.startWidth = 0.12f;
            line.endWidth = 0.04f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = new Color(1f, 0.85f, 0.2f, 0.9f);
            line.endColor = new Color(1f, 0.5f, 0.1f, 0.15f);
        }

        private static void BuildUIElements(Transform canvasTransform, UIController ui)
        {
            Font defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Top HUD Container
            GameObject topHud = CreateUIPanel(canvasTransform, "TopHUD", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(0f, 100f));

            Text titleText = CreateUIText(topHud.transform, "TitleText", "Level 1: Training Curve", defaultFont, 26, TextAnchor.MiddleLeft, new Vector2(40f, 0f), new Vector2(450f, 50f), Color.white);
            Text hintText = CreateUIText(topHud.transform, "HintText", "Drag backward from the ball to aim & launch.", defaultFont, 16, TextAnchor.MiddleLeft, new Vector2(40f, -32f), new Vector2(600f, 40f), new Color(0.7f, 0.85f, 1f));
            Text shotText = CreateUIText(topHud.transform, "ShotText", "Attempt: 0", defaultFont, 22, TextAnchor.MiddleRight, new Vector2(-220f, 0f), new Vector2(200f, 50f), new Color(1f, 0.85f, 0.2f));

            // Power Gauge
            GameObject powerRoot = CreateUIPanel(topHud.transform, "PowerGauge", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(280f, 24f));
            Image powerBg = powerRoot.AddComponent<Image>();
            powerBg.color = new Color(0.12f, 0.16f, 0.25f, 0.9f);

            GameObject powerFillObj = new GameObject("Fill");
            powerFillObj.transform.SetParent(powerRoot.transform, false);
            Image powerFill = powerFillObj.AddComponent<Image>();
            powerFill.type = Image.Type.Filled;
            powerFill.fillMethod = Image.FillMethod.Horizontal;
            powerFill.fillOrigin = 0;
            powerFill.fillAmount = 0f;
            powerFill.color = new Color(0.2f, 0.8f, 1f);
            RectTransform fillRect = powerFill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            Text powerText = CreateUIText(powerRoot.transform, "PowerLabel", "0%", defaultFont, 14, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(100f, 24f), Color.white);

            // Mute Button
            GameObject muteBtnObj = CreateUIButton(topHud.transform, "MuteButton", new Vector2(-60f, 0f), new Vector2(110f, 40f), new Color(0.18f, 0.25f, 0.35f));
            Text muteText = CreateUIText(muteBtnObj.transform, "Label", "Sound: ON", defaultFont, 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(110f, 40f), Color.white);

            // Feedback Banner
            GameObject bannerObj = CreateUIPanel(canvasTransform, "FeedbackBanner", new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 46f));
            Image bannerBg = bannerObj.AddComponent<Image>();
            bannerBg.color = new Color(0.08f, 0.12f, 0.2f, 0.85f);
            Text bannerText = CreateUIText(bannerObj.transform, "BannerText", "PERFECT GLIDE!", defaultFont, 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(480f, 40f), Color.green);
            bannerObj.SetActive(false);

            // Success Modal
            GameObject successModal = CreateModalBox(canvasTransform, "SuccessModal", "LEVEL COMPLETE!", new Color(0.1f, 0.8f, 0.4f));
            Text successStats = CreateUIText(successModal.transform, "StatsText", "Completed in 1 attempt!\nEntry Speed: 8.5 m/s", defaultFont, 20, TextAnchor.MiddleCenter, new Vector2(0f, 15f), new Vector2(400f, 70f), Color.white);
            GameObject nextBtn = CreateUIButton(successModal.transform, "NextLevelBtn", new Vector2(90f, -80f), new Vector2(150f, 45f), new Color(0.15f, 0.75f, 0.4f));
            CreateUIText(nextBtn.transform, "Label", "Next Level", defaultFont, 18, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(150f, 45f), Color.white);
            GameObject replayBtn = CreateUIButton(successModal.transform, "ReplayBtn", new Vector2(-90f, -80f), new Vector2(150f, 45f), new Color(0.25f, 0.35f, 0.45f));
            CreateUIText(replayBtn.transform, "Label", "Replay", defaultFont, 18, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(150f, 45f), Color.white);
            successModal.SetActive(false);

            // Failure Modal
            GameObject failureModal = CreateModalBox(canvasTransform, "FailureModal", "SHOT FAILED", new Color(1f, 0.35f, 0.35f));
            Text failureReason = CreateUIText(failureModal.transform, "ReasonText", "Entry angle too steep!\nAllowed: ±22°", defaultFont, 20, TextAnchor.MiddleCenter, new Vector2(0f, 15f), new Vector2(420f, 70f), Color.white);
            GameObject retryBtn = CreateUIButton(failureModal.transform, "RetryBtn", new Vector2(0f, -80f), new Vector2(180f, 48f), new Color(0.85f, 0.3f, 0.3f));
            CreateUIText(retryBtn.transform, "Label", "Try Again", defaultFont, 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(180f, 48f), Color.white);
            failureModal.SetActive(false);

            // Wire SerializedObject fields on UIController
            SerializedObject soUI = new SerializedObject(ui);
            soUI.FindProperty("levelTitleText").objectReferenceValue = titleText;
            soUI.FindProperty("shotCountText").objectReferenceValue = shotText;
            soUI.FindProperty("levelHintText").objectReferenceValue = hintText;
            soUI.FindProperty("powerFillBar").objectReferenceValue = powerFill;
            soUI.FindProperty("powerPercentText").objectReferenceValue = powerText;
            soUI.FindProperty("muteButton").objectReferenceValue = muteBtnObj.GetComponent<Button>();
            soUI.FindProperty("muteButtonText").objectReferenceValue = muteText;
            soUI.FindProperty("feedbackBannerObject").objectReferenceValue = bannerObj;
            soUI.FindProperty("feedbackBannerText").objectReferenceValue = bannerText;
            soUI.FindProperty("successModal").objectReferenceValue = successModal;
            soUI.FindProperty("successStatsText").objectReferenceValue = successStats;
            soUI.FindProperty("nextLevelButton").objectReferenceValue = nextBtn.GetComponent<Button>();
            soUI.FindProperty("successReplayButton").objectReferenceValue = replayBtn.GetComponent<Button>();
            soUI.FindProperty("failureModal").objectReferenceValue = failureModal;
            soUI.FindProperty("failureReasonText").objectReferenceValue = failureReason;
            soUI.FindProperty("retryButton").objectReferenceValue = retryBtn.GetComponent<Button>();
            soUI.ApplyModifiedProperties();
        }

        private static GameObject CreateUIPanel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = aMin;
            rect.anchorMax = aMax;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return obj;
        }

        private static Text CreateUIText(Transform parent, string name, string text, Font font, int size, TextAnchor anchor, Vector2 pos, Vector2 delta, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = delta;
            Text txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.font = font;
            txt.fontSize = size;
            txt.alignment = anchor;
            txt.color = color;
            return txt;
        }

        private static GameObject CreateUIButton(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            Image img = obj.AddComponent<Image>();
            img.color = color;
            Button btn = obj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = color * 1.2f;
            cb.pressedColor = color * 0.8f;
            btn.colors = cb;
            return obj;
        }

        private static GameObject CreateModalBox(Transform parent, string name, string headerTitle, Color headerColor)
        {
            GameObject modal = CreateUIPanel(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480f, 280f));
            Image bg = modal.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.11f, 0.18f, 0.96f);

            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            CreateUIText(modal.transform, "Header", headerTitle, font, 28, TextAnchor.MiddleCenter, new Vector2(0f, 95f), new Vector2(440f, 40f), headerColor);
            return modal;
        }

        private static void WireReferences(
            GameManager gm, LevelController lc, FeedbackController fc, UIController ui,
            ProjectileController proj, AimController aim, TrajectoryPreview traj,
            SurfacePath sp, SurfaceInteraction si, Transform launchPad, Transform goalBox, GoalZone gz, GameObject obstacle,
            FailureZone[] fzs, ParticleSystem launchPs, ParticleSystem impactPs, ParticleSystem successPs, ParticleSystem failurePs,
            Camera cam, Transform projTrans)
        {
            // LevelController
            SerializedObject soLC = new SerializedObject(lc);
            soLC.FindProperty("surfacePath").objectReferenceValue = sp;
            soLC.FindProperty("surfaceInteraction").objectReferenceValue = si;
            soLC.FindProperty("launchPlatform").objectReferenceValue = launchPad;
            soLC.FindProperty("goalBoxTransform").objectReferenceValue = goalBox;
            soLC.FindProperty("goalZone").objectReferenceValue = gz;
            soLC.FindProperty("obstacleObject").objectReferenceValue = obstacle;
            soLC.ApplyModifiedProperties();

            // AimController
            SerializedObject soAim = new SerializedObject(aim);
            soAim.FindProperty("projectile").objectReferenceValue = proj;
            soAim.FindProperty("trajectoryPreview").objectReferenceValue = traj;
            soAim.FindProperty("mainCamera").objectReferenceValue = cam;
            soAim.ApplyModifiedProperties();

            // FeedbackController
            SerializedObject soFC = new SerializedObject(fc);
            soFC.FindProperty("targetCamera").objectReferenceValue = cam;
            soFC.FindProperty("projectileTransform").objectReferenceValue = projTrans;
            soFC.FindProperty("launchParticles").objectReferenceValue = launchPs;
            soFC.FindProperty("impactParticles").objectReferenceValue = impactPs;
            soFC.FindProperty("successCelebrationParticles").objectReferenceValue = successPs;
            soFC.FindProperty("failureShatterParticles").objectReferenceValue = failurePs;
            soFC.ApplyModifiedProperties();

            // GameManager
            SerializedObject soGM = new SerializedObject(gm);
            soGM.FindProperty("projectile").objectReferenceValue = proj;
            soGM.FindProperty("aimController").objectReferenceValue = aim;
            soGM.FindProperty("surfaceInteraction").objectReferenceValue = si;
            soGM.FindProperty("goalZone").objectReferenceValue = gz;
            soGM.FindProperty("levelController").objectReferenceValue = lc;
            soGM.FindProperty("uiController").objectReferenceValue = ui;
            soGM.FindProperty("feedbackController").objectReferenceValue = fc;
            soGM.FindProperty("audioController").objectReferenceValue = gm.GetComponent<AudioController>();

            SerializedProperty fzArray = soGM.FindProperty("failureZones");
            fzArray.arraySize = fzs.Length;
            for (int i = 0; i < fzs.Length; i++)
            {
                fzArray.GetArrayElementAtIndex(i).objectReferenceValue = fzs[i];
            }
            soGM.ApplyModifiedProperties();
        }
    }
}
#endif
