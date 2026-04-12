using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;
using UnityEngine.InputSystem;

/// <summary>
/// Plays an animated cutscene sequence within a scene.
/// Moves camera between waypoints, animates a character along a path,
/// scrolls environment (for driving), and displays timed subtitles.
/// Notifies GameFlowManager when the cutscene is complete.
/// </summary>
public class CutscenePlayer : MonoBehaviour
{
    // ============================
    // CONFIGURATION
    // ============================

    [Header("Cutscene Type")]
    public CutsceneType cutsceneType = CutsceneType.Store;

    [Header("Camera")]
    public float cameraMoveSpeed = 1.5f;
    public float cameraLookSpeed = 2f;

    [Header("Character Movement")]
    public float characterWalkSpeed = 1.8f;

    [Header("Driving")]
    [Tooltip("World units per second — environment slides -Z so higher reads faster.")]
    public float roadScrollSpeed = 19f;

    [Tooltip("If waypoint look bias is on, camera base rotation advances across waypoints over this many seconds (looping drive has no fixed end).")]
    public float drivingDuration = 48f;

    [Tooltip("When Road/Trees/etc. move past -this Z, shift them forward by this amount for a seamless loop (match Forest Drive generator road length, ~1250).")]
    public float drivingScrollLoopLength = 1250f;

    [Tooltip("Seconds of driving before the type-home prompt appears (bottom-right).")]
    public float driveHomePromptAfterSeconds = 60f;

    [Tooltip("Scales engine/road vibration applied to CarInterior + camera (0 = off).")]
    [Range(0f, 2f)]
    public float drivingChassisVibration = 1f;

    [Tooltip("Dims moon/ambient/fill lights and boosts HeadlightL/R so the beam carries the scene.")]
    public bool drivingHeadlightsOnlyLighting = true;

    [Header("Driving first-person look")]
    public bool drivingFirstPersonFreeLook = true;
    public float drivingLookSensitivity = 0.15f;
    public float drivingMaxLookPitchDeg = 50f;

    [Tooltip("If enabled, the camera base rotation jumps through CutsceneWaypoints over time (cinematic glances). Off = stable freelook from the starting head pose only.")]
    public bool drivingApplyWaypointLookBias = false;

    [Tooltip("Forest Drive: no subtitle canvas or timed lines (keeps UI out of the way of the radio crosshair).")]
    public bool drivingUseSubtitles = false;

    [Header("Driving Radio Subtitles")]
    [Tooltip("Timed subtitles that float up from the AlpineRadio when the player clicks it. " +
             "'time' = seconds from when the radio clip starts. " +
             "Update these entries to match the actual radiothing.mp3 transcript.")]
    public DrivingCarInteriorInteraction.RadioSubtitleEntry[] drivingRadioSubtitles;

    [Header("Driving ambience (Assets/Audio/Resources)")]
    [Tooltip("Looping beds: carambience + RainCarWindow — start at drive begin, fade in/out.")]
    public bool drivingAmbienceEnabled = true;
    [Range(0f, 1f)] public float drivingCarAmbienceVolume = 0.22f;
    [Range(0f, 1f)] public float drivingRainWindowVolume = 0.17f;
    public float drivingAmbienceFadeInDuration = 1.5f;
    public float drivingAmbienceFadeOutDuration = 1.2f;

    [Header("Subtitles")]
    public SubtitleEntry[] subtitles;

    // ============================
    // TYPES
    // ============================

    public enum CutsceneType
    {
        Store,
        Driving
    }

    [Serializable]
    public struct SubtitleEntry
    {
        public float triggerTime;
        public string text;
        public float displayDuration;
    }

    // ============================
    // STATE
    // ============================

    private Camera cam;
    private Transform character;
    private Transform waypointsRoot;
    private Transform[] waypoints;
    private Transform roadRoot;

    private Canvas subtitleCanvas;
    private TextMeshProUGUI subtitleText;
    private Image subtitleBg;

    private bool isPlaying = false;
    private bool isComplete = false;
    private float cutsceneTimer = 0f;

    public bool IsComplete => isComplete;

    /// <summary>True while the driving segment is running (mouse look + interior UI).</summary>
    public bool IsDrivingCutsceneActive =>
        cutsceneType == CutsceneType.Driving && isPlaying && !isComplete;

    // Singleton per scene
    public static CutscenePlayer Instance;

    float _drivingLookYaw;
    float _drivingLookPitch;

    const string DrivingCarAmbienceResource = "carambience";
    const string DrivingRainWindowResource = "RainCarWindow";

    GameObject _drivingAmbienceRoot;
    AudioSource _drivingCarAmbienceSource;
    AudioSource _drivingRainWindowSource;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        FindSceneReferences();
        if (UseSubtitlesForThisCutscene())
            CreateSubtitleUI();
        else
            subtitles = Array.Empty<SubtitleEntry>();

        // Always run — populates both overlay subtitles and drivingRadioSubtitles
        SetupDefaultSubtitles();

        StartCoroutine(PlayCutscene());
    }

    bool UseSubtitlesForThisCutscene()
    {
        if (cutsceneType == CutsceneType.Driving)
            return drivingUseSubtitles;
        return true;
    }

    void FindSceneReferences()
    {
        cam = Camera.main;

        // Find waypoints
        GameObject wpObj = GameObject.Find("CutsceneWaypoints");
        if (wpObj != null)
        {
            waypointsRoot = wpObj.transform;
            waypoints = new Transform[waypointsRoot.childCount];
            for (int i = 0; i < waypointsRoot.childCount; i++)
                waypoints[i] = waypointsRoot.GetChild(i);
        }

        // Find character
        GameObject charObj = GameObject.Find("Character");
        if (charObj != null)
            character = charObj.transform;

        // Find road root for scrolling
        if (cutsceneType == CutsceneType.Driving)
        {
            GameObject roadObj = GameObject.Find("Road");
            if (roadObj != null)
                roadRoot = roadObj.transform;

            // Also find trees, street lights, guardrails to scroll
            // They'll be scrolled via the scene root
        }
    }

    void SetupDefaultSubtitles()
    {
        if (subtitles != null && subtitles.Length > 0) return;

        if (cutsceneType == CutsceneType.Store)
        {
            subtitles = new SubtitleEntry[]
            {
                new SubtitleEntry { triggerTime = 1f, text = "The fluorescent lights hum overhead.", displayDuration = 3f },
                new SubtitleEntry { triggerTime = 6f, text = "You grab what you need.", displayDuration = 3f },
                new SubtitleEntry { triggerTime = 13f, text = "The cashier doesn't look up.", displayDuration = 3f },
                new SubtitleEntry { triggerTime = 19f, text = "You pay and leave.", displayDuration = 2.5f },
            };
        }
        else if (cutsceneType == CutsceneType.Driving)
        {
            subtitles = new SubtitleEntry[]
            {
                new SubtitleEntry { triggerTime = 1.5f, text = "The roads are empty tonight.", displayDuration = 3.5f },
                new SubtitleEntry { triggerTime = 13f, text = "Rain streaks across the windshield.", displayDuration = 3.5f },
                new SubtitleEntry { triggerTime = 26f, text = "You think about nothing in particular.", displayDuration = 4f },
                new SubtitleEntry { triggerTime = 39f, text = "Almost home.", displayDuration = 3.5f },
            };

            // Default radio subtitles — update these to match the actual radiothing.mp3 transcript.
            // 'time' is seconds from when the player clicks the radio.
            if (drivingRadioSubtitles == null || drivingRadioSubtitles.Length == 0)
            {
                drivingRadioSubtitles = new DrivingCarInteriorInteraction.RadioSubtitleEntry[]
                {
                    new DrivingCarInteriorInteraction.RadioSubtitleEntry { time = 0f,    text = "W-K-N-I",                                                          displayDuration = 1f   },
                    new DrivingCarInteriorInteraction.RadioSubtitleEntry { time = 2f,    text = "You're still with W-K-N-I",                                        displayDuration = 1f   },
                    new DrivingCarInteriorInteraction.RadioSubtitleEntry { time = 4f,    text = "It is currently 1:30 in the morning",                              displayDuration = 2f   },
                    new DrivingCarInteriorInteraction.RadioSubtitleEntry { time = 7f,    text = "The weather is clear",                                             displayDuration = 0.1f },
                    new DrivingCarInteriorInteraction.RadioSubtitleEntry { time = 8f,    text = "with some light winds and temperatures right where they should be.", displayDuration = 3f   },
                    new DrivingCarInteriorInteraction.RadioSubtitleEntry { time = 13f,   text = "If you're the only headlights on the road tonight",                displayDuration = 1f   },
                    new DrivingCarInteriorInteraction.RadioSubtitleEntry { time = 15f,   text = "That's alright.",                                                  displayDuration = 1f   },
                    new DrivingCarInteriorInteraction.RadioSubtitleEntry { time = 17f,   text = "The night's long enough for everybody.",                           displayDuration = 2f   },
                    new DrivingCarInteriorInteraction.RadioSubtitleEntry { time = 20f,   text = "I'm gonna let the music carry you the rest of the way.",           displayDuration = 2f   },
                    new DrivingCarInteriorInteraction.RadioSubtitleEntry { time = 23f,   text = "This one's",                                                       displayDuration = 0.1f },
                    new DrivingCarInteriorInteraction.RadioSubtitleEntry { time = 23.5f, text = "for you.",                                                         displayDuration = 4f   },
                };
            }
        }
    }

    // ============================
    // SUBTITLE UI (created at runtime)
    // ============================

    void CreateSubtitleUI()
    {
        GameObject canvasObj = new GameObject("CutsceneSubtitleCanvas");
        canvasObj.transform.SetParent(transform);

        subtitleCanvas = canvasObj.AddComponent<Canvas>();
        subtitleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        subtitleCanvas.sortingOrder = 900; // Below GameFlowManager's 999

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Background bar at bottom
        GameObject bgObj = new GameObject("SubtitleBg");
        bgObj.transform.SetParent(canvasObj.transform, false);
        subtitleBg = bgObj.AddComponent<Image>();
        subtitleBg.color = new Color(0, 0, 0, 0.5f);
        subtitleBg.raycastTarget = false;

        RectTransform bgrt = bgObj.GetComponent<RectTransform>();
        bgrt.anchorMin = new Vector2(0, 0);
        bgrt.anchorMax = new Vector2(1, 0);
        bgrt.pivot = new Vector2(0.5f, 0);
        bgrt.sizeDelta = new Vector2(0, 80);
        bgrt.anchoredPosition = Vector2.zero;

        // Text
        GameObject textObj = new GameObject("SubtitleText");
        textObj.transform.SetParent(bgObj.transform, false);
        subtitleText = textObj.AddComponent<TextMeshProUGUI>();
        subtitleText.text = "";
        subtitleText.fontSize = 26;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.color = new Color(0.8f, 0.8f, 0.8f, 1f);

        RectTransform trt = textObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(40, 10);
        trt.offsetMax = new Vector2(-40, -10);

        bgObj.SetActive(false);
    }

    // ============================
    // MAIN CUTSCENE COROUTINE
    // ============================

    IEnumerator PlayCutscene()
    {
        isPlaying = true;
        cutsceneTimer = 0f;

        if (UseSubtitlesForThisCutscene())
            StartCoroutine(SubtitleScheduler());

        if (cutsceneType == CutsceneType.Store)
            yield return StartCoroutine(PlayStoreCutscene());
        else if (cutsceneType == CutsceneType.Driving)
            yield return StartCoroutine(PlayDrivingCutscene());

        isPlaying = false;
        isComplete = true;

        // Hide subtitles
        if (subtitleBg != null)
            subtitleBg.gameObject.SetActive(false);
    }

    // ============================
    // STORE CUTSCENE
    // ============================

    IEnumerator PlayStoreCutscene()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            yield return new WaitForSeconds(22f);
            yield break;
        }

        // Move camera through each waypoint
        float timePerWaypoint = 22f / waypoints.Length;

        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            Transform from = waypoints[i];
            Transform to = waypoints[i + 1];

            // Also move character along a parallel path (offset from camera)
            Vector3 charStart = character != null ? character.position : Vector3.zero;
            Vector3 charEnd = new Vector3(to.position.x + 0.8f, 0, to.position.z - 1.5f);

            float elapsed = 0f;
            while (elapsed < timePerWaypoint)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / timePerWaypoint);
                float smooth = SmoothStep(t);

                // Camera
                if (cam != null)
                {
                    cam.transform.position = Vector3.Lerp(from.position, to.position, smooth);
                    cam.transform.rotation = Quaternion.Slerp(from.rotation, to.rotation, smooth);
                }

                // Character
                if (character != null)
                {
                    character.position = Vector3.Lerp(charStart, charEnd, smooth);
                    // Face movement direction
                    Vector3 dir = (charEnd - charStart).normalized;
                    if (dir.sqrMagnitude > 0.01f)
                    {
                        dir.y = 0;
                        character.rotation = Quaternion.Slerp(character.rotation,
                            Quaternion.LookRotation(dir), Time.deltaTime * 4f);
                    }

                    // Simple bob animation
                    float bob = Mathf.Sin(elapsed * 6f) * 0.02f;
                    Vector3 pos = character.position;
                    pos.y = bob;
                    character.position = pos;
                }

                cutsceneTimer += Time.deltaTime;
                yield return null;
            }
        }
    }

    // ============================
    // DRIVING CUTSCENE
    // ============================

    IEnumerator PlayDrivingCutscene()
    {
        float driveElapsed = 0f;
        bool driveHomePromptShown = false;
        float loopLen = Mathf.Max(50f, drivingScrollLoopLength);
        float wpCycleSeconds = Mathf.Max(8f, drivingDuration);

        // Find all scrollable objects (everything except camera and car interior)
        Transform sceneRoot = null;
        GameObject drivingScene = GameObject.Find("DrivingScene");
        if (drivingScene != null)
            sceneRoot = drivingScene.transform;

        // Collect transforms to scroll (direct children; DrivingScene must stay the scroll parent)
        Transform road = null, streetLights = null, trees = null, guardrails = null, rain = null;
        if (sceneRoot != null)
        {
            road = sceneRoot.Find("Road");
            streetLights = sceneRoot.Find("StreetLights");
            trees = sceneRoot.Find("Trees");
            guardrails = sceneRoot.Find("Guardrails");
            rain = sceneRoot.Find("Rain");
        }

        if (road == null || trees == null)
            Debug.LogError("CutscenePlayer (Driving): Missing Road and/or Trees under DrivingScene — nothing will scroll. Regenerate Forest Drive or check hierarchy.");

        ClearStaticForScroll(road);
        ClearStaticForScroll(streetLights);
        ClearStaticForScroll(trees);
        ClearStaticForScroll(guardrails);

        Transform carInterior = sceneRoot != null ? sceneRoot.Find("CarInterior") : null;
        EnsureDrivingInteriorLights(carInterior);
        ApplyWarmDimDashInteriorFill(carInterior);

        if (drivingHeadlightsOnlyLighting)
            ApplyDrivingHeadlightsOnlyAtmosphere();
        Vector3 carInteriorBaseLocal = carInterior != null ? carInterior.localPosition : Vector3.zero;

        Transform ambienceParent = carInterior != null ? carInterior : sceneRoot;
        if (ambienceParent == null && drivingScene != null)
            ambienceParent = drivingScene.transform;
        SetupDrivingAmbienceLoopingBeds(ambienceParent);

        DrivingCarInteriorInteraction driveInteract = null;
        if (cam != null)
        {
            driveInteract = cam.GetComponent<DrivingCarInteriorInteraction>();
            if (driveInteract == null)
                driveInteract = cam.gameObject.AddComponent<DrivingCarInteriorInteraction>();
            driveInteract.BeginDrivingSession(FindAlpineRadioTransform(carInterior));
            driveInteract.radioSubtitles = drivingRadioSubtitles;
        }

        if (cam == null || driveInteract == null)
        {
            Debug.LogError("CutscenePlayer (Driving): Missing camera or DrivingCarInteriorInteraction — cannot run drive loop.");
            yield break;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _drivingLookYaw = 0f;
        _drivingLookPitch = 0f;

        // Camera subtle sway
        Vector3 camBasePos = cam != null ? cam.transform.localPosition : Vector3.zero;
        Quaternion camBaseRot = cam != null ? cam.transform.localRotation : Quaternion.identity;
        int drivingWpIndex = 0;

        // When the player types "home" we don't stop immediately — we keep the road scrolling
        // for the full fade-to-black duration so it doesn't feel like a hard cut.
        bool driveHomeTriggered = false;
        float driveHomeCoastTimer = 0f;
        float driveHomeCoastDuration = 0f; // set when triggered

        while (true)
        {
            float dt = Time.deltaTime;
            driveElapsed += dt;
            cutsceneTimer += dt;

            if (!driveHomePromptShown && driveElapsed >= Mathf.Max(0f, driveHomePromptAfterSeconds))
            {
                driveHomePromptShown = true;
                driveInteract.ShowDriveHomeTypablePrompt();
            }

            float scrollDelta = roadScrollSpeed * dt;
            Vector3 scroll = new Vector3(0, 0, -scrollDelta);

            if (road != null) road.Translate(scroll, Space.World);
            if (streetLights != null) streetLights.Translate(scroll, Space.World);
            if (trees != null) trees.Translate(scroll, Space.World);
            if (guardrails != null) guardrails.Translate(scroll, Space.World);

            WrapDrivingScrollRoots(road, streetLights, trees, guardrails, loopLen);

            if (rain != null && cam != null)
                rain.position = new Vector3(cam.transform.position.x, 8f, cam.transform.position.z + 10f);

            float vib = drivingChassisVibration;
            float chassisPhase = driveElapsed * 19f;
            float cx = (Mathf.PerlinNoise(driveElapsed * 12f, 0.6f) - 0.5f) * 0.0024f * vib;
            float cy = Mathf.Sin(chassisPhase) * 0.001f * vib + (Mathf.PerlinNoise(driveElapsed * 8.5f, 2.3f) - 0.5f) * 0.0013f * vib;
            float cz = Mathf.Sin(chassisPhase * 1.05f) * 0.0022f * vib;
            Vector3 chassis = new Vector3(cx, cy, cz);

            if (carInterior != null)
                carInterior.localPosition = carInteriorBaseLocal + chassis;

            UpdateDrivingAmbienceFadeInOnly(driveElapsed);

            if (cam != null)
            {
                float sway = Mathf.Sin(driveElapsed * 0.7f) * 0.003f;
                float bump = (Mathf.PerlinNoise(driveElapsed * 2f, 0) * 0.01f - 0.005f);
                float lookSway = Mathf.Sin(driveElapsed * 0.3f) * 0.5f;

                if (drivingFirstPersonFreeLook && Mouse.current != null)
                {
                    Vector2 md = Mouse.current.delta.ReadValue();
                    _drivingLookYaw += md.x * drivingLookSensitivity;
                    _drivingLookPitch -= md.y * drivingLookSensitivity;
                    _drivingLookPitch = Mathf.Clamp(_drivingLookPitch, -drivingMaxLookPitchDeg, drivingMaxLookPitchDeg);
                }

                cam.transform.localPosition = camBasePos + chassis + new Vector3(sway, bump, 0);
                Quaternion swayQ = Quaternion.Euler(bump * 10f, lookSway, sway * 5f);
                Quaternion freeQ = Quaternion.Euler(_drivingLookPitch, _drivingLookYaw, 0f);
                cam.transform.localRotation = camBaseRot * swayQ * freeQ;

                if (drivingApplyWaypointLookBias && waypoints != null && waypoints.Length > 1)
                {
                    float seg = wpCycleSeconds / Mathf.Max(1, waypoints.Length);
                    int targetWp = (int)(driveElapsed / seg) % waypoints.Length;
                    if (targetWp != drivingWpIndex)
                    {
                        drivingWpIndex = targetWp;
                        camBaseRot = waypoints[drivingWpIndex].localRotation;
                    }
                }
            }

            // Detect "home" typed — start scene transition immediately but keep driving
            if (!driveHomeTriggered && driveInteract.HasChosenDriveHome())
            {
                driveHomeTriggered = true;

                GameFlowManager flow = GameFlowManager.Instance ?? GameFlowManager.EnsureExists();
                // Coast for the full fade duration + a small buffer so the road is still
                // scrolling while the black overlay fades in over the top.
                driveHomeCoastDuration = flow.fadeDuration + 0.5f;

                // Fade ambience out concurrently while driving continues
                StartCoroutine(FadeOutDrivingAmbienceRoutine());

                // Kick off the scene transition on the persistent GameFlowManager;
                // its fade-to-black canvas (sortingOrder 999) covers the driving view.
                flow.BeginDriveHomeToMainArea();
            }

            if (driveHomeTriggered)
            {
                driveHomeCoastTimer += dt;
                if (driveHomeCoastTimer >= driveHomeCoastDuration)
                    break;
            }

            yield return null;
        }

        driveInteract.EndDrivingSession();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (carInterior != null)
            carInterior.localPosition = carInteriorBaseLocal;
        // Note: BeginDriveHomeToMainArea() was already called above when "home" was detected.
    }

    void SetupDrivingAmbienceLoopingBeds(Transform parent)
    {
        if (!drivingAmbienceEnabled || parent == null)
            return;

        if (_drivingAmbienceRoot == null)
        {
            _drivingAmbienceRoot = new GameObject("DrivingAmbienceBeds");
            _drivingAmbienceRoot.transform.SetParent(parent, false);
        }

        _drivingCarAmbienceSource = GetOrCreateLoopingBed(_drivingAmbienceRoot.transform, "Bed_CarAmbience", DrivingCarAmbienceResource);
        _drivingRainWindowSource = GetOrCreateLoopingBed(_drivingAmbienceRoot.transform, "Bed_RainCarWindow", DrivingRainWindowResource);

        if (_drivingCarAmbienceSource != null && _drivingCarAmbienceSource.clip != null)
        {
            _drivingCarAmbienceSource.volume = 0f;
            _drivingCarAmbienceSource.Play();
        }

        if (_drivingRainWindowSource != null && _drivingRainWindowSource.clip != null)
        {
            _drivingRainWindowSource.volume = 0f;
            _drivingRainWindowSource.Play();
        }
    }

    AudioSource GetOrCreateLoopingBed(Transform root, string nodeName, string resourceName)
    {
        Transform existing = root.Find(nodeName);
        GameObject go = existing != null ? existing.gameObject : new GameObject(nodeName);
        if (existing == null)
            go.transform.SetParent(root, false);

        AudioSource src = go.GetComponent<AudioSource>();
        if (src == null)
            src = go.AddComponent<AudioSource>();

        AudioClip clip = Resources.Load<AudioClip>(resourceName);
        if (clip == null)
        {
            Debug.LogWarning(
                "OPENFEED: Driving ambience missing Resources audio '" + resourceName +
                "' (place file in Assets/Audio/Resources/, name must match for Load).");
            src.clip = null;
            return src;
        }

        src.clip = clip;
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.dopplerLevel = 0f;
        src.priority = 200;
        return src;
    }

    static void WrapDrivingScrollRoots(Transform road, Transform streetLights, Transform trees, Transform guardrails, float loopLen)
    {
        void Wrap(Transform t)
        {
            if (t == null) return;
            while (t.position.z < -loopLen)
                t.position += new Vector3(0f, 0f, loopLen);
        }

        Wrap(road);
        Wrap(streetLights);
        Wrap(trees);
        Wrap(guardrails);
    }

    void UpdateDrivingAmbienceFadeInOnly(float elapsedDriveSeconds)
    {
        if (!drivingAmbienceEnabled)
            return;

        float fadeInDur = Mathf.Max(0.01f, drivingAmbienceFadeInDuration);
        float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsedDriveSeconds / fadeInDur));
        if (_drivingCarAmbienceSource != null && _drivingCarAmbienceSource.clip != null)
            _drivingCarAmbienceSource.volume = drivingCarAmbienceVolume * k;
        if (_drivingRainWindowSource != null && _drivingRainWindowSource.clip != null)
            _drivingRainWindowSource.volume = drivingRainWindowVolume * k;
    }

    IEnumerator FadeOutDrivingAmbienceRoutine()
    {
        if (!drivingAmbienceEnabled)
            yield break;

        float carV = _drivingCarAmbienceSource != null ? _drivingCarAmbienceSource.volume : 0f;
        float rainV = _drivingRainWindowSource != null ? _drivingRainWindowSource.volume : 0f;
        if (carV < 0.002f && rainV < 0.002f)
        {
            SilenceDrivingAmbienceSources();
            yield break;
        }

        float outDur = Mathf.Max(0.01f, drivingAmbienceFadeOutDuration);
        float t = 0f;
        float car0 = _drivingCarAmbienceSource != null ? _drivingCarAmbienceSource.volume : 0f;
        float rain0 = _drivingRainWindowSource != null ? _drivingRainWindowSource.volume : 0f;

        while (t < outDur)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / outDur));
            if (_drivingCarAmbienceSource != null)
                _drivingCarAmbienceSource.volume = car0 * k;
            if (_drivingRainWindowSource != null)
                _drivingRainWindowSource.volume = rain0 * k;
            yield return null;
        }

        if (_drivingCarAmbienceSource != null)
        {
            _drivingCarAmbienceSource.Stop();
            _drivingCarAmbienceSource.volume = 0f;
        }

        if (_drivingRainWindowSource != null)
        {
            _drivingRainWindowSource.Stop();
            _drivingRainWindowSource.volume = 0f;
        }
    }

    void SilenceDrivingAmbienceSources()
    {
        if (_drivingCarAmbienceSource != null)
        {
            _drivingCarAmbienceSource.Stop();
            _drivingCarAmbienceSource.volume = 0f;
        }

        if (_drivingRainWindowSource != null)
        {
            _drivingRainWindowSource.Stop();
            _drivingRainWindowSource.volume = 0f;
        }
    }

    static void ClearStaticForScroll(Transform root)
    {
        if (root == null) return;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            t.gameObject.isStatic = false;
    }

    void ApplyDrivingHeadlightsOnlyAtmosphere()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.002f, 0.0022f, 0.005f);
        RenderSettings.reflectionIntensity = 0f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.005f, 0.004f, 0.014f);
        RenderSettings.fogDensity = 0.034f;

        foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Include))
        {
            string n = light.gameObject.name;
            if (n == "HeadlightL" || n == "HeadlightR")
            {
                light.enabled = true;
                light.intensity = 20f;
                light.range = 110f;
                light.spotAngle = 62f;
                light.color = new Color(1f, 0.94f, 0.8f);
                light.shadows = LightShadows.Soft;
                continue;
            }

            if (n == "DashInteriorFill")
            {
                light.enabled = true;
                light.type = LightType.Point;
                ApplyWarmDimDashLightValues(light);
                light.shadows = LightShadows.None;
                continue;
            }

            if (n == "RadioOrangeGlow")
            {
                light.enabled = true;
                light.type = LightType.Point;
                ApplyDimRadioOrangeGlowValues(light);
                light.shadows = LightShadows.None;
                continue;
            }

            light.enabled = false;
        }
    }

    static void ApplyWarmDimDashLightValues(Light l)
    {
        if (l == null) return;
        l.color = new Color(0.92f, 0.66f, 0.38f);
        l.intensity = 0.11f;
        l.range = 1.85f;
    }

    static void ApplyDimRadioOrangeGlowValues(Light l)
    {
        if (l == null) return;
        l.color = new Color(1f, 0.38f, 0.06f);
        l.intensity = 0.028f;
        l.range = 0.55f;
    }

    void ApplyWarmDimDashInteriorFill(Transform carInterior)
    {
        if (carInterior == null) return;
        Transform fill = carInterior.Find("DashInteriorFill");
        if (fill == null) return;
        Light l = fill.GetComponent<Light>();
        ApplyWarmDimDashLightValues(l);
    }

    static Transform FindAlpineRadioTransform(Transform carInterior)
    {
        if (carInterior == null) return null;
        foreach (Transform t in carInterior.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name;
            if (n == "AlpineRadio" || n.StartsWith("AlpineRadio", StringComparison.Ordinal))
                return t;
        }
        return null;
    }

    /// <summary>Cabin fill + orange glow on AlpineRadio (creates children if missing).</summary>
    void EnsureDrivingInteriorLights(Transform carInterior)
    {
        if (carInterior == null) return;

        if (carInterior.Find("DashInteriorFill") == null)
        {
            GameObject fill = new GameObject("DashInteriorFill");
            fill.transform.SetParent(carInterior, false);
            fill.transform.localPosition = new Vector3(0.04f, 0.98f, 0.88f);
            Light l = fill.AddComponent<Light>();
            l.type = LightType.Point;
            ApplyWarmDimDashLightValues(l);
            l.shadows = LightShadows.None;
        }

        Transform alpine = FindAlpineRadioTransform(carInterior);
        if (alpine != null && alpine.Find("RadioOrangeGlow") == null)
        {
            GameObject glow = new GameObject("RadioOrangeGlow");
            glow.transform.SetParent(alpine, false);
            glow.transform.localPosition = new Vector3(0f, 0.03f, 0.05f);
            Light l = glow.AddComponent<Light>();
            l.type = LightType.Point;
            ApplyDimRadioOrangeGlowValues(l);
            l.shadows = LightShadows.None;
        }
    }

    // ============================
    // SUBTITLE SCHEDULER
    // ============================

    IEnumerator SubtitleScheduler()
    {
        if (subtitles == null || subtitles.Length == 0) yield break;

        int nextSub = 0;
        while (isPlaying && nextSub < subtitles.Length)
        {
            if (cutsceneTimer >= subtitles[nextSub].triggerTime)
            {
                yield return StartCoroutine(ShowSubtitle(
                    subtitles[nextSub].text,
                    subtitles[nextSub].displayDuration));
                nextSub++;
            }
            yield return null;
        }
    }

    IEnumerator ShowSubtitle(string text, float duration)
    {
        subtitleText.text = text;
        subtitleBg.gameObject.SetActive(true);

        // Fade in
        float fadeTime = 0.4f;
        yield return StartCoroutine(FadeSubtitle(0f, 1f, fadeTime));

        // Hold
        yield return new WaitForSeconds(duration);

        // Fade out
        yield return StartCoroutine(FadeSubtitle(1f, 0f, fadeTime));

        subtitleBg.gameObject.SetActive(false);
    }

    IEnumerator FadeSubtitle(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = SmoothStep(Mathf.Clamp01(elapsed / duration));
            float a = Mathf.Lerp(from, to, t);

            Color tc = subtitleText.color;
            tc.a = a;
            subtitleText.color = tc;

            Color bc = subtitleBg.color;
            bc.a = a * 0.5f;
            subtitleBg.color = bc;

            yield return null;
        }
    }

    // ============================
    // UTILITY
    // ============================

    float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
