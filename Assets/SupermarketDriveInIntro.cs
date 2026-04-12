using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Whole-flow supermarket intro:
///   1. Car drives along highway (driver POV, same rig as ForestDrive)
///   2. Car turns right into the parking lot and decelerates to a stop
///   3. Screen fades to black; car door open + close SFX play
///   4. Screen fades back in; player has free movement
///   5. Hint text directs player to the entrance
///   6. Player enters a trigger near the entrance → fade to black → spawn inside the store
///
/// Drop this on any persistent GameObject in the supermarket scene.
/// Assign fields in the Inspector; auto-finds player/camera if left null.
/// </summary>
public class SupermarketDriveInIntro : MonoBehaviour
{
    // ============================
    // INSPECTOR
    // ============================

    [Header("Player Rig (auto-found if null)")]
    public StoreFirstPersonController playerController;
    public Camera playerCamera;

    [Header("Approach Waypoints")]
    [Tooltip("Where the car starts on the highway (world space). Highway runs along X, so start at large negative X.")]
    public Vector3 highwayStartPosition = new Vector3(-100f, 0f, -5f);
    [Tooltip("Point where the car begins its right turn into the parking lot (end of highway stretch).")]
    public Vector3 parkingTurnPosition = new Vector3(1f, 0f, -5f);
    [Tooltip("Final parked position for the CarIntroRig root. Subaru clone will appear ~3.4 units ahead of this.")]
    public Vector3 parkingSpotPosition = new Vector3(4f, 0f, 3.8f);

    [Header("Drive path height")]
    [Tooltip("World Y for highway → parking drive (X/Z still come from waypoints or setup). ParkedCar is often placed high in the lot; use ground level (0) so the camera is not floating above the scene.")]
    public float drivePathWorldY = 0f;
    [Tooltip("After the car sequence, player spawn Y = drivePathWorldY + this (eye height above ground).")]
    public float playerExteriorEyeOffsetY = 1.62f;

    [Header("Recorded drive path (Scene Camera Path Recorder JSON)")]
    [Tooltip("If true, CarIntroRig follows recorded camera samples instead of procedural highway/turn/park.")]
    public bool useRecordedDrivePath = true;
    [Tooltip("Assign the exported .json, or leave null and use Resources name below.")]
    public TextAsset driveCameraPathAsset;
    [Tooltip("Resources.Load<TextAsset> name without extension when Drive Camera Path Asset is null.")]
    public string driveCameraPathResourceName = "SupermarketDriveCameraPath";
    [Tooltip("Use the first keyframe's py for every sample so height never drifts during the drive.")]
    public bool lockRecordedDrivePathHeightToFirstSample = true;
    [Tooltip("1 = real-time match to recorded keyframe times.")]
    public float driveCameraPathPlaybackSpeed = 1f;
    public bool applyRecordedPathFov = true;

    [Header("Timing")]
    public float startFadeInDuration  = 0.6f;  // fade in from black as driving starts
    public float highwayDuration      = 5.5f;  // seconds on the highway straight
    public float turnDuration         = 2.4f;  // seconds sweeping the right turn
    public float parkingDuration      = 3.2f;  // seconds decelerating into the spot
    public float engineCutDelay       = 0.45f; // pause after full-stop before fade
    public float fadeToBlackDuration  = 0.55f;
    public float doorOpenDelay        = 0.25f; // seconds into black before door-open sfx
    public float doorCloseDelay       = 1.1f;  // seconds after door-open before door-close sfx
    public float blackHoldDuration    = 0.7f;  // hold black after door close
    public float fadeFromBlackDuration = 0.75f;

    [Header("Car Door SFX")]
    [Tooltip("AudioClip for the car door opening. Leave null if no clip is available.")]
    public AudioClip doorOpenClip;
    [Tooltip("AudioClip for the car door closing.")]
    public AudioClip doorCloseClip;
    [Range(0f, 1f)] public float doorSfxVolume = 0.85f;

    [Header("ExteriorCar (local under CarIntroRig — same as Forest Drive builder)")]
    [Tooltip("Parent handles facing (Y-flip). Do not use 180° X on the mesh — that inverts the car.")]
    public Vector3 carBodyLocalPos   = new Vector3(0f, 1.55f, 3.38f);
    public Vector3 carBodyLocalEuler = new Vector3(0f, 180f, 0f);

    [Header("DriveInSubaru (local under ExteriorCar — same as Forest Drive Impreza child)")]
    public Vector3 driveInSubaruLocalPosition = Vector3.zero;
    public Vector3 driveInSubaruLocalEuler    = Vector3.zero;
    [Tooltip("Uniform scale of the cloned Subaru mesh.")]
    public float carBodyScale = 0.4f;

    [Header("Driver eye (local under CarIntroRig — matches Driving.unity Main Camera)")]
    [FormerlySerializedAs("driverEyeLocalPosOnRigFallback")]
    public Vector3 driverEyeLocalPos = new Vector3(-0.3f, 1.05f, 0.4f);
    public Vector3 driverEyeLocalEuler = new Vector3(3f, 0f, 0f);
    public float   driverFov           = 60f;

    [Header("Dash & headlights (local under CarIntroRig — matches Driving.unity CarInterior)")]
    public Vector3 dashLocalOnRig = new Vector3(0f, 0.64f, 0.95f);
    public Vector3 headlightLeftLocalOnRig  = new Vector3(-0.5f, 0.5f, 2f);
    public Vector3 headlightRightLocalOnRig = new Vector3(0.5f, 0.5f, 2f);

    [Header("Headlights")]
    public bool  useHeadlights          = true;
    public Color headlightColor         = new Color(1f, 0.94f, 0.8f);
    public float headlightIntensity     = 2f;
    public float headlightRange         = 110f;
    public float headlightAngle         = 62f;

    [Header("Dashboard Glow")]
    public Color dashColor              = new Color(0.92f, 0.66f, 0.38f);
    public float dashIntensity          = 0.3f;
    public float dashRange              = 1.85f;

    [Header("Chassis Vibration")]
    [Range(0f, 2f)]
    public float vibrationScale         = 1f;

    [Header("Engine Audio (loaded from Resources)")]
    [Tooltip("Resource name of the looping engine clip (e.g. 'carambience').")]
    public string engineResourceName    = "carambience";
    [Range(0f, 1f)]
    public float  engineVolume          = 0.22f;

    [Header("Player Spawn — outside (after exiting the car)")]
    [Tooltip("Eye-level world position where the player appears after the car-door beat, outside the store.")]
    public Vector3 playerSpawnPosition  = new Vector3(7f, 1.62f, 6f);
    [Tooltip("Direction the player faces when they appear outside — should point roughly toward the store entrance.")]
    public Vector3 playerSpawnForward   = new Vector3(0f, 0f, 1f);

    [Header("Player Spawn — inside store (after entrance fade)")]
    [Tooltip("Eye-level world position the player teleports to when they reach the store entrance.")]
    public Vector3 insideSpawnPosition  = new Vector3(10.4f, 1.93f, 19f);
    [Tooltip("Direction the player faces after spawning inside.")]
    public Vector3 insideSpawnForward   = new Vector3(0f, 0f, -1f);

    [Header("Entrance Hint")]
    public string hintMessage           = "Walk to the entrance.";
    public float  hintFadeInDuration    = 1.2f;
    public float  hintFadeOutDuration   = 0.8f;

    [Header("Store Entrance Doors")]
    public Transform leftDoor;
    public Transform rightDoor;
    public Vector3 leftDoorClosedLocal;
    public Vector3 leftDoorOpenLocal;
    public Vector3 rightDoorClosedLocal;
    public Vector3 rightDoorOpenLocal;
    public StoreEntranceLock entranceLock;
    public float doorAnimDuration       = 0.65f;

    [Header("Entrance Detection")]
    [Tooltip("World-space centre of the entrance trigger zone (just inside the store doors).")]
    public Vector3 entranceTriggerCenter = new Vector3(10.4f, 1.84f, 16.5f);
    [Tooltip("Radius in metres; when the camera enters this sphere the door sequence fires.")]
    public float entranceTriggerRadius   = 2.0f;

    [Header("Interior handoff")]
    [Tooltip("Fade to black after doors close, before teleporting into the shopping area (shorter = snappier).")]
    public float entranceInteriorFadeDuration = 0.38f;

    // ============================
    // RUNTIME STATE
    // ============================

    bool    _hasStarted;
    bool    _freeMovementActive;
    bool    _entranceTriggered;

    GameObject  _carRig;              // root that the camera and car body are parented to
    GameObject  _exteriorCarHolder;   // ExteriorCar child — detached from rig after parking so the car stays visible
    GameObject  _carBodyClone;        // Subaru_Impreza clone inside ExteriorCar
    GameObject  _parkedCarGO;         // original ParkedCar scene object — hidden during driving
    AudioSource _engineSource;
    AudioSource _sfxSource;

    float     _origFov;
    Transform _origCamParent;
    Vector3   _origCamLocalPos;
    Quaternion _origCamLocalRot;

    /// <summary>Rest position for camera in CarIntroRig local space (vibration adds jitter on top).</summary>
    Vector3 _driverEyeRestLocal;

    bool _followingRecordedPath;

    Canvas         _introCanvas;
    Image          _fadeImage;
    TextMeshProUGUI _hintText;

    // ============================
    // UNITY LIFECYCLE
    // ============================

    void Awake()
    {
        // Keep the existing night-lighting pass that StoreFlowIntroController also triggers.
        StoreFlowExteriorNightSetup.ApplyIfGroceryStore(transform);

        // Legacy intro moves the player to the menu pose in Start — incompatible with drive-in.
        var legacyIntro = GetComponent<StoreFlowIntroController>();
        if (legacyIntro != null)
            legacyIntro.enabled = false;
    }

    void Start()
    {
        // Auto-find rig references
        if (playerController == null)
            playerController = FindAnyObjectByType<StoreFirstPersonController>(FindObjectsInactive.Include);
        if (playerCamera == null)
            playerCamera = Camera.main;

        EnsureCanvas();
        ResolveDoorReferences();
        EnsureEntranceLock();

        if (entranceLock != null)
            entranceLock.UnlockEntrance();

        ApplyDefaultDoorLocalsIfNeeded();

        // Disable entrance glass colliders so the player can reach the trigger; we teleport inside.
        DisableDoorColliders();

        // Start locked out so the player cannot wander while the cinematic runs
        if (playerController != null)
        {
            playerController.SetControlEnabled(false);
            playerController.SetCinematicMode(true);
        }
    }

    void Update()
    {
        if (!_freeMovementActive || _entranceTriggered || playerCamera == null) return;

        float dist = Vector3.Distance(playerCamera.transform.position, entranceTriggerCenter);
        if (dist <= entranceTriggerRadius)
        {
            _entranceTriggered = true;
            StartCoroutine(EntranceSequence());
        }
    }

    // ============================
    // PUBLIC API
    // ============================

    /// <summary>
    /// Called by GameFlowManager.  Returns true if the intro was started.
    /// </summary>
    public bool BeginIntroSequence()
    {
        if (_hasStarted) return false;
        if (playerController == null)
            playerController = StoreFlowPlayerRigRuntime.EnsureStorePlayerRig();
        if (playerController == null)
            playerController = FindAnyObjectByType<StoreFirstPersonController>(FindObjectsInactive.Include);
        if (playerController == null) return false;
        _hasStarted = true;
        StopAllCoroutines();
        StartCoroutine(PlaySequence());
        return true;
    }

    /// <summary>
    /// Skip the driving sequence: spawn the player at the usual exterior spot with full control,
    /// same as the end of <see cref="PlaySequence"/> (hint, entrance trigger, etc.).
    /// Called after the main-menu black / exposition beat. GameFlowManager fades the screen in.
    /// </summary>
    public void BeginExteriorGameplayOnly()
    {
        if (_hasStarted) return;
        StopAllCoroutines();
        StartCoroutine(ExteriorGameplayOnlySequence());
    }

    // ============================
    // MAIN SEQUENCE
    // ============================

    IEnumerator PlaySequence()
    {
        // Screen starts fully black (EnsureCanvas sets alpha = 1).
        yield return new WaitForSeconds(0.35f);

        // Flatten drive path to ground — setup often copies ParkedCar Y (elevated lot display).
        highwayStartPosition.y  = drivePathWorldY;
        parkingTurnPosition.y   = drivePathWorldY;
        parkingSpotPosition.y   = drivePathWorldY;

        // ── Build car rig and attach the player camera ──────────────────────
        BuildCarRig();

        OpenFeedDriveCameraPathFile pathFile = null;
        bool usePath = useRecordedDrivePath && TryLoadDriveCameraPath(out pathFile);
        if (useRecordedDrivePath && !usePath)
        {
            Debug.LogWarning(
                "[SupermarketDriveInIntro] useRecordedDrivePath is enabled but no valid JSON was found " +
                "(assign Drive Camera Path Asset or add Resources/" + driveCameraPathResourceName + ".json). " +
                "Using procedural highway/turn/park.");
        }

        if (playerCamera != null)
        {
            _origFov           = playerCamera.fieldOfView;
            _origCamParent     = playerCamera.transform.parent;
            _origCamLocalPos   = playerCamera.transform.localPosition;
            _origCamLocalRot   = playerCamera.transform.localRotation;

            // Same hierarchy as Driving.unity: camera is a child of the drive root, not ExteriorCar.
            playerCamera.transform.SetParent(_carRig.transform, false);
            playerCamera.transform.localPosition = _driverEyeRestLocal;
            playerCamera.transform.localRotation = Quaternion.Euler(driverEyeLocalEuler);
            playerCamera.fieldOfView = driverFov;
        }

        if (usePath)
        {
            float yLock = pathFile.samples[0].py;
            SampleDriveCameraPath(
                pathFile.samples,
                pathFile.samples[0].t,
                yLock,
                lockRecordedDrivePathHeightToFirstSample,
                out Vector3 camPos,
                out Quaternion camRot,
                out float pathFov);
            ApplyRigForRecordedCameraPose(camPos, camRot);
            if (applyRecordedPathFov && playerCamera != null && pathFov > 0.5f)
                playerCamera.fieldOfView = pathFov;
        }
        else
        {
            // Orient rig to face along the highway before fading in
            Vector3 highwayDir = (parkingTurnPosition - highwayStartPosition).normalized;
            if (highwayDir.sqrMagnitude < 0.001f) highwayDir = Vector3.forward;
            _carRig.transform.SetPositionAndRotation(
                highwayStartPosition,
                Quaternion.LookRotation(highwayDir, Vector3.up));
        }

        StartEngineAudio();

        // Fade in — player sees the highway approach from day one
        yield return FadeOverlay(1f, 0f, startFadeInDuration);

        // ── Drive ────────────────────────────────────────────────────────────
        _followingRecordedPath = usePath;
        if (usePath)
            yield return StartCoroutine(FollowRecordedDriveCameraPath(pathFile));
        else
        {
            yield return StartCoroutine(DriveHighway());
            yield return StartCoroutine(DriveTurn());
            yield return StartCoroutine(DriveToSpot());
        }
        _followingRecordedPath = false;

        // Engine idle for a beat before cutting
        yield return new WaitForSeconds(engineCutDelay);
        if (_engineSource != null)
            StartCoroutine(FadeAudioSource(_engineSource, 0f, 0.4f));

        // ── Car door beat (fade to black + SFX) ──────────────────────────────
        yield return FadeOverlay(0f, 1f, fadeToBlackDuration);

        yield return new WaitForSeconds(doorOpenDelay);
        PlaySfx(doorOpenClip);
        yield return new WaitForSeconds(doorCloseDelay);
        PlaySfx(doorCloseClip);
        yield return new WaitForSeconds(blackHoldDuration);

        // ── Restore camera to player, detach parked car, destroy rig ─────────
        if (playerCamera != null && _origCamParent != null)
        {
            playerCamera.transform.SetParent(_origCamParent, false);
            playerCamera.transform.localPosition = _origCamLocalPos;
            playerCamera.transform.localRotation = _origCamLocalRot;
            playerCamera.fieldOfView             = _origFov;
        }

        if (_exteriorCarHolder != null)
            _exteriorCarHolder.transform.SetParent(null, worldPositionStays: true);

        if (_carRig != null) { Destroy(_carRig); _carRig = null; }

        // ── Spawn player outside the store ───────────────────────────────────
        playerController.SetCinematicMode(false);
        Quaternion spawnRot = Quaternion.LookRotation(
            playerSpawnForward.sqrMagnitude > 0.001f ? playerSpawnForward.normalized : Vector3.forward,
            Vector3.up);
        Vector3 exteriorSpawn = playerSpawnPosition;
        exteriorSpawn.y = drivePathWorldY + playerExteriorEyeOffsetY;
        playerController.SetPose(exteriorSpawn, spawnRot);
        playerController.SetControlEnabled(true);

        // Fade in to reveal exterior
        yield return FadeOverlay(1f, 0f, fadeFromBlackDuration);

        // Hint text: "Walk to the entrance."
        if (!string.IsNullOrEmpty(hintMessage))
            StartCoroutine(ShowHintText());

        _freeMovementActive = true;
    }

    IEnumerator ExteriorGameplayOnlySequence()
    {
        if (_hasStarted)
            yield break;

        if (playerController == null)
            playerController = StoreFlowPlayerRigRuntime.EnsureStorePlayerRig();
        if (playerController == null)
            playerController = FindAnyObjectByType<StoreFirstPersonController>(FindObjectsInactive.Include);
        if (playerController == null)
        {
            Debug.LogError(
                "[SupermarketDriveInIntro] BeginExteriorGameplayOnly: could not create or find StoreFirstPersonController.");
            yield break;
        }

        _hasStarted = true;

        bool activatedPlayerRig = false;
        if (!playerController.gameObject.activeInHierarchy)
        {
            playerController.gameObject.SetActive(true);
            activatedPlayerRig = true;
        }

        EnsureCanvas();
        // Let GameFlowManager's fade (sorting 999) cover the screen; keep drive-in overlay transparent.
        if (_fadeImage != null)
            _fadeImage.color = new Color(0f, 0f, 0f, 0f);

        if (playerCamera == null)
            playerCamera = Camera.main;

        yield return null;
        if (activatedPlayerRig)
            yield return null;

        playerController.SetCinematicMode(false);
        Quaternion spawnRot = Quaternion.LookRotation(
            playerSpawnForward.sqrMagnitude > 0.001f ? playerSpawnForward.normalized : Vector3.forward,
            Vector3.up);
        Vector3 exteriorSpawn = playerSpawnPosition;
        exteriorSpawn.y = drivePathWorldY + playerExteriorEyeOffsetY;
        playerController.SetPose(exteriorSpawn, spawnRot);
        playerController.SetControlEnabled(true);

        if (!string.IsNullOrEmpty(hintMessage))
            StartCoroutine(ShowHintText());

        _freeMovementActive = true;
        yield break;
    }

    // ============================
    // RECORDED DRIVE PATH (openfeed-scene-camera-path-v1)
    // ============================

    bool TryLoadDriveCameraPath(out OpenFeedDriveCameraPathFile data)
    {
        data = null;
        string json = null;
        if (driveCameraPathAsset != null && !string.IsNullOrEmpty(driveCameraPathAsset.text))
            json = driveCameraPathAsset.text;
        else if (!string.IsNullOrEmpty(driveCameraPathResourceName))
        {
            TextAsset ta = Resources.Load<TextAsset>(driveCameraPathResourceName);
            if (ta != null)
                json = ta.text;
        }

        if (string.IsNullOrEmpty(json))
            return false;

        OpenFeedDriveCameraPathFile parsed = JsonUtility.FromJson<OpenFeedDriveCameraPathFile>(json);
        if (parsed?.samples == null || parsed.samples.Length < 2)
            return false;

        if (!string.IsNullOrEmpty(parsed.format)
            && parsed.format != "openfeed-scene-camera-path-v1")
        {
            Debug.LogWarning(
                "[SupermarketDriveInIntro] Drive path format is '" + parsed.format +
                "' (expected openfeed-scene-camera-path-v1). Playback may be wrong.");
        }

        data = parsed;
        return true;
    }

    /// <summary>
    /// Samples are recorded world-space camera poses. Rig pose is chosen so the child camera
    /// (fixed local offset + euler) matches the recorded position and rotation.
    /// </summary>
    void ApplyRigForRecordedCameraPose(Vector3 cameraWorldPos, Quaternion cameraWorldRot)
    {
        if (_carRig == null) return;
        Quaternion camLocal = Quaternion.Euler(driverEyeLocalEuler);
        Quaternion rigRot   = cameraWorldRot * Quaternion.Inverse(camLocal);
        _carRig.transform.rotation = rigRot;
        _carRig.transform.position = cameraWorldPos - rigRot * driverEyeLocalPos;
    }

    static void SampleDriveCameraPath(
        OpenFeedDriveCameraPathSample[] s,
        float timeAlongPath,
        float firstSamplePy,
        bool lockHeightToFirst,
        out Vector3 cameraWorldPos,
        out Quaternion cameraWorldRot,
        out float fovDegrees)
    {
        float tq = Mathf.Clamp(timeAlongPath, s[0].t, s[s.Length - 1].t);
        int i = s.Length - 2;
        for (int k = 0; k < s.Length - 1; k++)
        {
            if (tq <= s[k + 1].t)
            {
                i = k;
                break;
            }
        }

        float t0 = s[i].t;
        float t1 = s[i + 1].t;
        float u  = t1 > t0 ? Mathf.Clamp01((tq - t0) / (t1 - t0)) : 0f;

        float y0 = lockHeightToFirst ? firstSamplePy : s[i].py;
        float y1 = lockHeightToFirst ? firstSamplePy : s[i + 1].py;

        cameraWorldPos = Vector3.Lerp(
            new Vector3(s[i].px, y0, s[i].pz),
            new Vector3(s[i + 1].px, y1, s[i + 1].pz),
            u);

        Quaternion q0 = Quaternion.Normalize(new Quaternion(s[i].qx, s[i].qy, s[i].qz, s[i].qw));
        Quaternion q1 = Quaternion.Normalize(new Quaternion(s[i + 1].qx, s[i + 1].qy, s[i + 1].qz, s[i + 1].qw));
        cameraWorldRot = Quaternion.Slerp(q0, q1, u);
        fovDegrees     = ResolveDrivePathSegmentFov(s[i], s[i + 1], u);
    }

    static float ResolveDrivePathSegmentFov(OpenFeedDriveCameraPathSample a, OpenFeedDriveCameraPathSample b, float u)
    {
        bool ha = a.fov > 0.5f;
        bool hb = b.fov > 0.5f;
        if (ha && hb)
            return Mathf.Lerp(a.fov, b.fov, u);
        if (ha)
            return a.fov;
        if (hb)
            return b.fov;
        return -1f;
    }

    IEnumerator FollowRecordedDriveCameraPath(OpenFeedDriveCameraPathFile data)
    {
        OpenFeedDriveCameraPathSample[] s = data.samples;
        if (s == null || s.Length < 2)
            yield break;

        float speed = Mathf.Max(0.05f, driveCameraPathPlaybackSpeed);
        float endT  = s[s.Length - 1].t;
        if (endT < 0.0001f)
            yield break;

        float yLock = s[0].py;
        float pathTime = 0f;

        while (pathTime < endT)
        {
            pathTime = Mathf.Min(pathTime + Time.deltaTime * speed, endT);

            SampleDriveCameraPath(
                s,
                pathTime,
                yLock,
                lockRecordedDrivePathHeightToFirstSample,
                out Vector3 camPos,
                out Quaternion camRot,
                out float fov);

            ApplyRigForRecordedCameraPose(camPos, camRot);
            if (applyRecordedPathFov && playerCamera != null && fov > 0.5f)
                playerCamera.fieldOfView = fov;

            yield return null;
        }

        SampleDriveCameraPath(
            s,
            endT,
            yLock,
            lockRecordedDrivePathHeightToFirstSample,
            out Vector3 endPos,
            out Quaternion endRot,
            out float endFov);
        ApplyRigForRecordedCameraPose(endPos, endRot);
        if (applyRecordedPathFov && playerCamera != null && endFov > 0.5f)
            playerCamera.fieldOfView = endFov;
    }

    // ============================
    // DRIVING PHASES
    // ============================

    IEnumerator DriveHighway()
    {
        Vector3   fromPos = highwayStartPosition;
        Vector3   toPos   = parkingTurnPosition;
        Quaternion rot    = _carRig.transform.rotation;

        float elapsed = 0f;
        while (elapsed < highwayDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / highwayDuration);
            _carRig.transform.position = Vector3.Lerp(fromPos, toPos, t);
            _carRig.transform.rotation = rot;
            ApplyCameraVibration();
            yield return null;
        }
        _carRig.transform.position = toPos;
        _carRig.transform.rotation = rot;
    }

    IEnumerator DriveTurn()
    {
        Vector3    fromPos  = _carRig.transform.position;
        Vector3    toPos    = parkingSpotPosition;
        Quaternion fromRot  = _carRig.transform.rotation;
        Vector3    parkDir  = (parkingSpotPosition - parkingTurnPosition).normalized;
        if (parkDir.sqrMagnitude < 0.001f) parkDir = Vector3.right;
        Quaternion toRot    = Quaternion.LookRotation(parkDir, Vector3.up);

        // During the turn we sweep the rotation fully but advance position only ~30 %
        // (the car is still swinging around the corner, not charging forward yet).
        const float posAdvanceFraction = 0.30f;

        float elapsed = 0f;
        while (elapsed < turnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / turnDuration);
            _carRig.transform.position = Vector3.Lerp(fromPos, toPos, t * posAdvanceFraction);
            _carRig.transform.rotation = Quaternion.Slerp(fromRot, toRot, t);
            ApplyCameraVibration(0.55f);
            yield return null;
        }
        _carRig.transform.position = Vector3.Lerp(fromPos, toPos, posAdvanceFraction);
        _carRig.transform.rotation = toRot;
    }

    IEnumerator DriveToSpot()
    {
        Vector3   fromPos = _carRig.transform.position;
        Vector3   toPos   = parkingSpotPosition;
        Quaternion rot    = _carRig.transform.rotation;

        float elapsed = 0f;
        while (elapsed < parkingDuration)
        {
            elapsed += Time.deltaTime;
            float rawT = elapsed / parkingDuration;
            // Ease-out deceleration curve: starts fast, slows heavily at end
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(rawT), 3f);
            _carRig.transform.position = Vector3.Lerp(fromPos, toPos, t);
            _carRig.transform.rotation = rot;
            float vibScale = Mathf.Lerp(1f, 0.15f, rawT);   // vibration dies as speed drops
            ApplyCameraVibration(vibScale);
            yield return null;
        }
        _carRig.transform.SetPositionAndRotation(toPos, rot);
    }

    // ============================
    // ENTRANCE SEQUENCE
    // ============================

    IEnumerator EntranceSequence()
    {
        playerController.SetControlEnabled(false);

        StartCoroutine(FadeHintOut());

        float innerFade = Mathf.Max(0.05f, entranceInteriorFadeDuration);
        yield return FadeOverlay(0f, 1f, innerFade);

        playerController.SetCinematicMode(false);
        Quaternion insideRot = Quaternion.LookRotation(
            insideSpawnForward.sqrMagnitude > 0.001f ? insideSpawnForward.normalized : Vector3.back,
            Vector3.up);
        playerController.SetPose(insideSpawnPosition, insideRot);
        playerController.SetControlEnabled(true);

        StoreFlowExteriorNightSetup.ApplyInteriorShoppingRenderSettings();

        if (entranceLock != null)
            entranceLock.LockEntrance();

        yield return FadeOverlay(1f, 0f, fadeFromBlackDuration);
    }

    // ============================
    // CAR RIG CONSTRUCTION
    // ============================

    void BuildCarRig()
    {
        _carRig = new GameObject("CarIntroRig");

        _parkedCarGO = GameObject.Find("ParkedCar");

        // Resolve Subaru BEFORE hiding ParkedCar: GameObject.Find ignores inactive objects,
        // and Transform.Find skips inactive children — so deactivating first breaks the clone.
        GameObject subaruMesh = FindSubaruBodyForDriveIn();
        if (_parkedCarGO != null)
            _parkedCarGO.SetActive(false);

        if (subaruMesh != null)
        {
            // Create an ExteriorCar holder (same hierarchy as ForestDrive) under the rig
            GameObject extCar = new GameObject("ExteriorCar");
            _exteriorCarHolder = extCar;
            extCar.transform.SetParent(_carRig.transform, false);
            extCar.transform.localPosition = carBodyLocalPos;
            extCar.transform.localRotation = Quaternion.Euler(carBodyLocalEuler);
            extCar.transform.localScale    = Vector3.one;

            // Clone the Subaru mesh — ExteriorCar Y=180° + identity child matches OpenFeedDrivingCarBuilder.
            _carBodyClone = Instantiate(subaruMesh, extCar.transform);
            _carBodyClone.name = "DriveInSubaru";
            _carBodyClone.transform.localPosition = driveInSubaruLocalPosition;
            _carBodyClone.transform.localRotation = Quaternion.Euler(driveInSubaruLocalEuler);
            _carBodyClone.transform.localScale    = Vector3.one * carBodyScale;

            // Strip colliders so the clone doesn't interfere with physics
            foreach (Collider col in _carBodyClone.GetComponentsInChildren<Collider>(true))
                Destroy(col);
        }
        else
        {
            Debug.LogWarning("[SupermarketDriveInIntro] No Subaru body found under ParkedCar (Subaru_Impreza / SubaruImpreza). Driving POV will have no car mesh.");
        }

        _driverEyeRestLocal = driverEyeLocalPos;

        // --- Dashboard fill light (CarIntroRig space, same as CarInterior in Driving.unity) ---
        GameObject dashObj = new GameObject("DashGlow");
        dashObj.transform.SetParent(_carRig.transform, false);
        dashObj.transform.localPosition = dashLocalOnRig;
        Light dashLight = dashObj.AddComponent<Light>();
        dashLight.type      = LightType.Point;
        dashLight.color     = dashColor;
        dashLight.intensity = dashIntensity;
        dashLight.range     = dashRange;
        dashLight.shadows   = LightShadows.None;

        // --- Headlights ---
        if (useHeadlights)
        {
            AddHeadlight("HeadlightL", _carRig.transform, headlightLeftLocalOnRig);
            AddHeadlight("HeadlightR", _carRig.transform, headlightRightLocalOnRig);
        }

        // --- One-shot SFX source on the rig ---
        _sfxSource = _carRig.AddComponent<AudioSource>();
        _sfxSource.spatialBlend = 0f;   // 2D — door sounds feel close
        _sfxSource.playOnAwake  = false;
    }

    /// <summary>
    /// Prefer the body root under ParkedCar; scene may use Subaru_Impreza or SubaruImpreza.
    /// Must run while ParkedCar is still active so hierarchy search works.
    /// </summary>
    static GameObject FindSubaruBodyForDriveIn()
    {
        GameObject parked = GameObject.Find("ParkedCar");
        if (parked != null)
        {
            Transform t = FindChildRecursive(parked.transform, "Subaru_Impreza");
            if (t != null) return t.gameObject;
            t = FindChildRecursive(parked.transform, "SubaruImpreza");
            if (t != null) return t.gameObject;
        }

        GameObject g = GameObject.Find("Subaru_Impreza");
        if (g != null) return g;
        return GameObject.Find("SubaruImpreza");
    }

    static Transform FindChildRecursive(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform c = parent.GetChild(i);
            if (c.name == childName)
                return c;
            Transform nested = FindChildRecursive(c, childName);
            if (nested != null)
                return nested;
        }
        return null;
    }

    void AddHeadlight(string lightName, Transform parent, Vector3 localPos)
    {
        GameObject go = new GameObject(lightName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

        Light l = go.AddComponent<Light>();
        l.type      = LightType.Spot;
        l.color     = headlightColor;
        l.intensity = headlightIntensity;
        l.range     = headlightRange;
        l.spotAngle = headlightAngle;
        l.shadows   = LightShadows.Soft;
    }

    // ============================
    // VIBRATION (applied to camera local position)
    // ============================

    void ApplyCameraVibration(float scale = 1f)
    {
        if (_followingRecordedPath) return;
        if (playerCamera == null || vibrationScale <= 0f) return;
        float t = Time.time;
        float v = vibrationScale * scale;

        Vector3 localPos = _driverEyeRestLocal;
        localPos.y += Mathf.Sin(t * 19.7f) * 0.003f * v;
        localPos.x += (Mathf.PerlinNoise(t * 3.1f, 0f) - 0.5f) * 0.005f * v;
        localPos.z += Mathf.Sin(t * 7.3f)  * 0.002f * v;
        playerCamera.transform.localPosition = localPos;
    }

    // ============================
    // AUDIO
    // ============================

    void StartEngineAudio()
    {
        AudioClip clip = Resources.Load<AudioClip>(engineResourceName);
        if (clip == null) return;

        GameObject engineObj = new GameObject("EngineAudio");
        engineObj.transform.SetParent(_carRig.transform, false);
        _engineSource = engineObj.AddComponent<AudioSource>();
        _engineSource.clip        = clip;
        _engineSource.loop        = true;
        _engineSource.spatialBlend = 0f;    // 2D so it sounds like interior
        _engineSource.volume      = 0f;
        _engineSource.Play();
        StartCoroutine(FadeAudioSource(_engineSource, engineVolume, 1f));
    }

    void PlaySfx(AudioClip clip)
    {
        if (clip == null || _sfxSource == null) return;
        _sfxSource.volume = doorSfxVolume;
        _sfxSource.PlayOneShot(clip);
    }

    IEnumerator FadeAudioSource(AudioSource src, float targetVolume, float duration)
    {
        if (src == null) yield break;
        float startVol = src.volume;
        float elapsed  = 0f;
        while (elapsed < duration)
        {
            elapsed     += Time.deltaTime;
            src.volume   = Mathf.Lerp(startVol, targetVolume, elapsed / duration);
            yield return null;
        }
        src.volume = targetVolume;
    }

    // ============================
    // FADE OVERLAY
    // ============================

    void EnsureCanvas()
    {
        if (_introCanvas != null) return;

        GameObject canvasObj = new GameObject("DriveInIntroCanvas");
        canvasObj.transform.SetParent(transform);
        _introCanvas = canvasObj.AddComponent<Canvas>();
        _introCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _introCanvas.sortingOrder = 490;    // below GameFlowManager (999) and StoreFlowIntroController (500)
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Fullscreen black fade panel
        GameObject fadeObj = new GameObject("Fade");
        fadeObj.transform.SetParent(canvasObj.transform, false);
        _fadeImage = fadeObj.AddComponent<Image>();
        _fadeImage.color         = new Color(0f, 0f, 0f, 1f);  // start fully black
        _fadeImage.raycastTarget = false;
        RectTransform frt = fadeObj.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;

        // Hint text (bottom-centre, initially invisible)
        GameObject hintObj = new GameObject("HintText");
        hintObj.transform.SetParent(canvasObj.transform, false);
        _hintText           = hintObj.AddComponent<TextMeshProUGUI>();
        _hintText.text      = hintMessage;
        _hintText.fontSize  = 26;
        _hintText.alignment = TextAlignmentOptions.Center;
        _hintText.color     = new Color(0.78f, 0.78f, 0.78f, 0f);   // fully transparent

        RectTransform hrt = hintObj.GetComponent<RectTransform>();
        hrt.anchorMin        = new Vector2(0.5f, 0f);
        hrt.anchorMax        = new Vector2(0.5f, 0f);
        hrt.pivot            = new Vector2(0.5f, 0f);
        hrt.sizeDelta        = new Vector2(700f, 60f);
        hrt.anchoredPosition = new Vector2(0f, 60f);
    }

    IEnumerator FadeOverlay(float from, float to, float duration)
    {
        if (_fadeImage == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smooth = t * t * (3f - 2f * t);
            _fadeImage.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, smooth));
            yield return null;
        }
        _fadeImage.color = new Color(0f, 0f, 0f, to);
    }

    // ============================
    // HINT TEXT
    // ============================

    IEnumerator ShowHintText()
    {
        if (_hintText == null) yield break;

        float elapsed = 0f;
        while (elapsed < hintFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float a  = Mathf.Lerp(0f, 0.82f, elapsed / hintFadeInDuration);
            _hintText.color = new Color(0.78f, 0.78f, 0.78f, a);
            yield return null;
        }
        _hintText.color = new Color(0.78f, 0.78f, 0.78f, 0.82f);
    }

    IEnumerator FadeHintOut()
    {
        if (_hintText == null) yield break;
        float startAlpha = _hintText.color.a;
        float elapsed    = 0f;
        while (elapsed < hintFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float a  = Mathf.Lerp(startAlpha, 0f, elapsed / hintFadeOutDuration);
            _hintText.color = new Color(0.78f, 0.78f, 0.78f, a);
            yield return null;
        }
        _hintText.color = new Color(0.78f, 0.78f, 0.78f, 0f);
    }

    // ============================
    // DOOR HELPERS
    // ============================

    void EnsureEntranceLock()
    {
        if (entranceLock != null) return;

        if (leftDoor != null && leftDoor.parent != null)
        {
            entranceLock = leftDoor.parent.GetComponent<StoreEntranceLock>()
                        ?? leftDoor.parent.gameObject.AddComponent<StoreEntranceLock>();
        }

        if (entranceLock != null)
            entranceLock.ConfigureUsingDoors(leftDoor, rightDoor);
    }

    void ResolveDoorReferences()
    {
        if (leftDoor != null && rightDoor != null)
            return;

        GameObject flow = GameObject.Find("StoreFlowScene");
        Transform sliding = flow != null ? flow.transform.Find("SlidingDoors") : null;
        if (sliding != null)
        {
            if (leftDoor == null) leftDoor = sliding.Find("LeftDoor");
            if (rightDoor == null) rightDoor = sliding.Find("RightDoor");
        }

        if (leftDoor != null && rightDoor != null)
            return;

        StoreFlowIntroController legacy = GetComponent<StoreFlowIntroController>();
        if (legacy != null)
        {
            if (leftDoor == null) leftDoor = legacy.leftDoor;
            if (rightDoor == null) rightDoor = legacy.rightDoor;
            if (entranceLock == null) entranceLock = legacy.entranceLock;
        }
    }

    void ApplyDefaultDoorLocalsIfNeeded()
    {
        const float slide = 0.82f;

        if (leftDoor != null
            && leftDoorClosedLocal.sqrMagnitude < 1e-8f
            && leftDoorOpenLocal.sqrMagnitude < 1e-8f)
        {
            leftDoorClosedLocal = leftDoor.localPosition;
            leftDoorOpenLocal = leftDoorClosedLocal + Vector3.left * slide;
        }

        if (rightDoor != null
            && rightDoorClosedLocal.sqrMagnitude < 1e-8f
            && rightDoorOpenLocal.sqrMagnitude < 1e-8f)
        {
            rightDoorClosedLocal = rightDoor.localPosition;
            rightDoorOpenLocal = rightDoorClosedLocal + Vector3.right * slide;
        }

        StoreFlowIntroController legacy = GetComponent<StoreFlowIntroController>();
        if (legacy == null)
            return;

        if (leftDoor != null
            && leftDoorClosedLocal.sqrMagnitude < 1e-8f
            && legacy.leftDoorClosedLocalPosition.sqrMagnitude > 1e-8f)
        {
            leftDoorClosedLocal = legacy.leftDoorClosedLocalPosition;
            leftDoorOpenLocal = legacy.leftDoorOpenLocalPosition;
        }

        if (rightDoor != null
            && rightDoorClosedLocal.sqrMagnitude < 1e-8f
            && legacy.rightDoorClosedLocalPosition.sqrMagnitude > 1e-8f)
        {
            rightDoorClosedLocal = legacy.rightDoorClosedLocalPosition;
            rightDoorOpenLocal = legacy.rightDoorOpenLocalPosition;
        }
    }

    IEnumerator AnimateDoors(bool open, float duration)
    {
        float start   = open ? 0f : 1f;
        float end     = open ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t      = Mathf.Clamp01(elapsed / duration);
            float smooth = t * t * (3f - 2f * t);
            SetDoorOpenAmount(Mathf.Lerp(start, end, smooth));
            yield return null;
        }

        SetDoorOpenAmount(end);
    }

    void SetDoorOpenAmount(float amount)
    {
        if (leftDoor  != null) leftDoor .localPosition = Vector3.Lerp(leftDoorClosedLocal,  leftDoorOpenLocal,  amount);
        if (rightDoor != null) rightDoor.localPosition = Vector3.Lerp(rightDoorClosedLocal, rightDoorOpenLocal, amount);
    }

    // Both door mesh objects form the entrance — both must be disabled for the player
    // to clip through, and re-enabled afterwards to block the way back.
    static readonly string[] k_DoorObjectNames = { "Object_7048", "Object_7046" };

    /// <summary>
    /// Disables colliders on all entrance door objects so the player can clip through.
    /// </summary>
    void DisableDoorColliders()
    {
        foreach (string objName in k_DoorObjectNames)
        {
            GameObject door = GameObject.Find(objName);
            if (door == null) continue;
            foreach (Collider col in door.GetComponentsInChildren<Collider>(true))
                col.enabled = false;
        }
    }

    /// <summary>
    /// Re-enables (or adds) colliders on entrance door objects after the player is inside.
    /// </summary>
    void EnableDoorCollider()
    {
        foreach (string objName in k_DoorObjectNames)
        {
            GameObject door = GameObject.Find(objName);
            if (door == null) continue;

            bool hadCollider = false;
            foreach (Collider col in door.GetComponentsInChildren<Collider>(true))
            {
                col.enabled = true;
                hadCollider = true;
            }

            if (!hadCollider)
            {
                MeshFilter mf = door.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    MeshCollider mc = door.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = false;
                }
            }
        }
    }

    // ============================
    // CLEANUP
    // ============================

    void OnDestroy()
    {
        if (_introCanvas != null) Destroy(_introCanvas.gameObject);
        if (_carRig       != null) Destroy(_carRig);
    }
}
