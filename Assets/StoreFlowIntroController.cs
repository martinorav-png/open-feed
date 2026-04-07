using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class StoreIntroPathFile
{
    public string format;
    public string sceneName;
    public float recordedDurationSeconds;
    public float sampleIntervalSeconds;
    public StoreIntroPathSample[] samples;
}

[Serializable]
public class StoreIntroPathSample
{
    public float t;
    public float px, py, pz;
    public float qx, qy, qz, qw;
}

public class StoreFlowIntroController : MonoBehaviour
{
    struct CinematicPose
    {
        public Vector3 position;
        public Quaternion rotation;

        public CinematicPose(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }
    }

    [Header("Rig")]
    public StoreFirstPersonController playerController;
    public Camera playerCamera;

    [Header("Views")]
    public Transform menuView;
    public Transform carSeatView;
    public Transform carExitView;
    public Transform exploreView;
    public bool overrideMenuViewPose;
    public Vector3 menuViewOverridePosition;
    public Vector3 menuViewOverrideEuler;
    public bool overrideCarSeatViewPose;
    public Vector3 carSeatViewOverridePosition;
    public Vector3 carSeatViewOverrideEuler;

    [Header("Custom Sequence Poses")]
    public bool useCustomSequencePoses = true;
    [Tooltip("If true, gameplay starts at ExploreView after intro. If false, uses Enter pose (no animated segment into explore — avoids clipping through doorway geometry).")]
    public bool continueIntoExploreView = true;
    public Vector3 customMenuPosition = new Vector3(15.58386f, 1.227849f, -9.701193f);
    public Vector4 customMenuRotation = new Vector4(-0.1115422f, 0.665723f, 0.1029979f, 0.7305908f);
    public Vector3 customCarSeatPosition = new Vector3(4.676617f, 1.573286f, 4.200386f);
    public Vector4 customCarSeatRotation = new Vector4(0.04550149f, -0.03777738f, 0.002805305f, 0.9982458f);
    public Vector3 customCarExitPosition = new Vector3(6.217842f, 1.927321f, 4.447559f);
    public Vector4 customCarExitRotation = new Vector4(0.0110589f, -0.02585859f, 0.001372372f, 0.9996035f);
    public Vector3 customWalkToStorePosition = new Vector3(6.155984f, 1.702457f, 6.83116f);
    public Vector4 customWalkToStoreRotation = new Vector4(0.039528f, -0.03030579f, 0.002316293f, 0.9987561f);
    public Vector3 customTurnToEntrancePosition = new Vector3(6.155984f, 1.702457f, 6.83116f);
    public Vector4 customTurnToEntranceRotation = new Vector4(0.039528f, -0.03030579f, 0.002316293f, 0.9987561f);
    public Vector3 customEnterStorePosition = new Vector3(6.155984f, 1.702457f, 6.83116f);
    public Vector4 customEnterStoreRotation = new Vector4(0.039528f, -0.03030579f, 0.002316293f, 0.9987561f);

    [Header("Doors")]
    public Transform leftDoor;
    public Transform rightDoor;
    public Vector3 leftDoorClosedLocalPosition;
    public Vector3 leftDoorOpenLocalPosition;
    public Vector3 rightDoorClosedLocalPosition;
    public Vector3 rightDoorOpenLocalPosition;
    public StoreEntranceLock entranceLock;
    public bool lockEntranceAfterIntro = true;

    [Header("Car Side Door")]
    public Transform carSideDoor;
    public string carSideDoorName = "doorleftsmd";
    public float carSideDoorOpenAngleX = -55f;
    public float carSideDoorOpenDuration = 0.5f;
    public float carSideDoorCloseDuration = 0.45f;
    public float carSideDoorOpenHoldTime = 0.2f;

    [Header("Timing")]
    public float menuToCarDuration = 1.2f;
    public float exitCarDuration = 1.0f;
    public float walkToStoreDuration = 3.2f;

    [Header("Recorded walk path (after exit car)")]
    public bool useRecordedWalkToStorePath = true;
    public string recordedWalkPathResourceName = "StoreIntroWalkPath";
    public float exitToRecordedPathBlendDuration = 0.35f;
    public float recordedWalkPathPlaybackSpeed = 1f;
    public float holdInCarTime = 0.75f;
    public float sideDoorPauseTime = 0.35f;
    public float beforeDoorCloseDelay = 0.35f;
    public float doorAnimDuration = 0.65f;
    public float walkTurnDuration = 0.65f;
    public float enterStoreDuration = 0.95f;
    public float fadeToBlackDuration = 0.42f;
    public float fadeFromBlackDuration = 0.48f;
    public float fadeBlackHoldTime = 0.08f;

    [Header("Walk Bob")]
    public float walkBobVerticalAmplitude = 0.045f;
    public float walkBobLateralAmplitude = 0.012f;
    public float walkBobFrequency = 2.2f;
    public float walkBobTiltAmplitude = 0.9f;
    public AnimationCurve cinematicEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    bool hasStarted;
    Quaternion carDoorClosedRotation;
    Quaternion carDoorOpenRotation;
    Canvas fadeCanvas;
    Image fadeImage;

    bool _walkPathLoadAttempted;
    StoreIntroPathSample[] _walkPathSamples;
    float _walkPathLastT;

    void Awake()
    {
        StoreFlowExteriorNightSetup.ApplyIfGroceryStore(transform);
    }

    void Start()
    {
        ApplyViewOverrides();
        ResolveCarSideDoor();
        EnsureFadeOverlay();

        if (playerController != null)
        {
            playerController.SetControlEnabled(false);
            playerController.SetCinematicMode(true);

            CinematicPose startPose = ResolveMenuPose();
            playerController.SetPose(startPose.position, startPose.rotation);
        }

        EnsureEntranceLock();
        if (entranceLock != null)
            entranceLock.UnlockEntrance();

        SetDoorOpenAmount(0f);
    }

    public bool BeginIntroSequence()
    {
        if (hasStarted || playerController == null)
            return false;

        hasStarted = true;
        StopAllCoroutines();
        StartCoroutine(PlayIntro());
        return true;
    }

    IEnumerator PlayIntro()
    {
        playerController.SetControlEnabled(false);
        playerController.SetCinematicMode(true);

        CinematicPose menuPose = ResolveMenuPose();
        CinematicPose carPose = ResolveCarSeatPose();
        CinematicPose exitPose = ResolveCarExitPose();
        CinematicPose walkPose = ResolveCustomOrTransform(customWalkToStorePosition, customWalkToStoreRotation, exploreView);
        CinematicPose turnPose = ResolveCustomOrTransform(customTurnToEntrancePosition, customTurnToEntranceRotation, exploreView);
        CinematicPose enterPose = ResolveCustomOrTransform(customEnterStorePosition, customEnterStoreRotation, exploreView);
        CinematicPose explorePose = ResolveExplorePose();

        playerController.SetPose(menuPose.position, menuPose.rotation);
        yield return StartCoroutine(FadeToCarPose(menuPose, carPose));

        yield return new WaitForSeconds(holdInCarTime);

        Coroutine carDoorOpen = null;
        if (carSideDoor != null)
            carDoorOpen = StartCoroutine(AnimateCarSideDoor(true, carSideDoorOpenDuration));

        yield return StartCoroutine(MovePose(carPose, exitPose, exitCarDuration, true));

        if (carDoorOpen != null)
            yield return carDoorOpen;

        if (carSideDoor != null && carSideDoorOpenHoldTime > 0f)
            yield return new WaitForSeconds(carSideDoorOpenHoldTime);

        if (sideDoorPauseTime > 0f)
            yield return new WaitForSeconds(sideDoorPauseTime);

        if (carSideDoor != null)
            yield return StartCoroutine(AnimateCarSideDoor(false, carSideDoorCloseDuration));

        Coroutine openDoors = StartCoroutine(AnimateDoors(true, doorAnimDuration));

        CinematicPose afterWalkPose = walkPose;

        if (useRecordedWalkToStorePath
            && TryLoadRecordedWalkPath(out StoreIntroPathSample[] pathSamples, out float pathLastT)
            && pathSamples != null
            && pathSamples.Length >= 2)
        {
            yield return StartCoroutine(FollowRecordedWalkPath(exitPose, pathSamples, pathLastT));
            afterWalkPose = PathSampleToPose(pathSamples[pathSamples.Length - 1]);
        }
        else
        {
            yield return StartCoroutine(MovePose(exitPose, walkPose, walkToStoreDuration, true));
        }

        if (!PosesAreNearlyEqual(afterWalkPose, turnPose))
            yield return StartCoroutine(MovePose(afterWalkPose, turnPose, walkTurnDuration, true));

        if (!PosesAreNearlyEqual(turnPose, enterPose))
            yield return StartCoroutine(MovePose(turnPose, enterPose, enterStoreDuration, true));

        // Do not animate enterPose → explorePose: eased lerp often passes through the doorway hull
        // and briefly shows the exterior. Final SetPose(explorePose) below snaps to the marker in one frame.

        if (openDoors != null)
            yield return openDoors;

        if (beforeDoorCloseDelay > 0f)
            yield return new WaitForSeconds(beforeDoorCloseDelay);
        yield return StartCoroutine(AnimateDoors(false, doorAnimDuration));

        if (lockEntranceAfterIntro && entranceLock != null)
            entranceLock.LockEntrance();

        CinematicPose handoffPose = continueIntoExploreView ? explorePose : enterPose;
        playerController.SetPose(handoffPose.position, handoffPose.rotation);

        playerController.SetCinematicMode(false);
        playerController.SetControlEnabled(true);

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.FinishIntroToStore();
    }

    void EnsureEntranceLock()
    {
        if (entranceLock == null && leftDoor != null && leftDoor.parent != null)
            entranceLock = leftDoor.parent.GetComponent<StoreEntranceLock>();

        if (entranceLock == null && leftDoor != null && leftDoor.parent != null)
            entranceLock = leftDoor.parent.gameObject.AddComponent<StoreEntranceLock>();

        if (entranceLock != null)
            entranceLock.ConfigureUsingDoors(leftDoor, rightDoor);
    }

    void ApplyViewOverrides()
    {
        if (overrideMenuViewPose && menuView != null)
            menuView.SetPositionAndRotation(menuViewOverridePosition, Quaternion.Euler(menuViewOverrideEuler));

        if (overrideCarSeatViewPose && carSeatView != null)
            carSeatView.SetPositionAndRotation(carSeatViewOverridePosition, Quaternion.Euler(carSeatViewOverrideEuler));
    }

    void EnsureFadeOverlay()
    {
        if (fadeCanvas != null)
            return;

        GameObject canvasObj = new GameObject("StoreIntroFadeCanvas");
        fadeCanvas = canvasObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 500;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject imageObj = new GameObject("Fade");
        imageObj.transform.SetParent(canvasObj.transform, false);
        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;

        RectTransform rt = fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    CinematicPose ResolveMenuPose()
    {
        if (useCustomSequencePoses)
            return new CinematicPose(customMenuPosition, QuaternionFromVector4(customMenuRotation));
        if (menuView != null)
            return new CinematicPose(menuView.position, menuView.rotation);
        return ResolveCarSeatPose();
    }

    CinematicPose ResolveCarSeatPose()
    {
        if (useCustomSequencePoses)
            return new CinematicPose(customCarSeatPosition, QuaternionFromVector4(customCarSeatRotation));
        if (carSeatView != null)
            return new CinematicPose(carSeatView.position, carSeatView.rotation);
        return ResolveCarExitPose();
    }

    CinematicPose ResolveCarExitPose()
    {
        if (useCustomSequencePoses)
            return new CinematicPose(customCarExitPosition, QuaternionFromVector4(customCarExitRotation));
        if (carExitView != null)
            return new CinematicPose(carExitView.position, carExitView.rotation);
        return ResolveExplorePose();
    }

    CinematicPose ResolveExplorePose()
    {
        if (exploreView != null)
            return new CinematicPose(exploreView.position, exploreView.rotation);
        return new CinematicPose(customEnterStorePosition, QuaternionFromVector4(customEnterStoreRotation));
    }

    CinematicPose ResolveCustomOrTransform(Vector3 position, Vector4 rotation, Transform fallback)
    {
        if (useCustomSequencePoses)
            return new CinematicPose(position, QuaternionFromVector4(rotation));
        if (fallback != null)
            return new CinematicPose(fallback.position, fallback.rotation);
        return ResolveExplorePose();
    }

    static Quaternion QuaternionFromVector4(Vector4 value)
    {
        Quaternion q = new Quaternion(value.x, value.y, value.z, value.w);
        return Quaternion.Normalize(q);
    }

    static bool PosesAreNearlyEqual(CinematicPose a, CinematicPose b)
    {
        return Vector3.Distance(a.position, b.position) < 0.02f && Quaternion.Angle(a.rotation, b.rotation) < 0.5f;
    }

    IEnumerator FadeToCarPose(CinematicPose fromMenu, CinematicPose carPose)
    {
        yield return StartCoroutine(FadeOverlay(0f, 1f, fadeToBlackDuration));
        playerController.SetPose(fromMenu.position, fromMenu.rotation);
        float dur = Mathf.Max(0f, menuToCarDuration);
        if (dur > 0.001f)
            yield return StartCoroutine(MovePose(fromMenu, carPose, dur, true));
        else
            playerController.SetPose(carPose.position, carPose.rotation);
        if (fadeBlackHoldTime > 0f)
            yield return new WaitForSeconds(fadeBlackHoldTime);
        yield return StartCoroutine(FadeOverlay(1f, 0f, fadeFromBlackDuration));
    }

    IEnumerator FadeOverlay(float from, float to, float duration)
    {
        if (fadeImage == null)
            yield break;

        if (duration <= 0f)
        {
            fadeImage.color = new Color(0f, 0f, 0f, to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = EvaluateEase(Mathf.Clamp01(elapsed / duration));
            float alpha = Mathf.Lerp(from, to, t);
            fadeImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        fadeImage.color = new Color(0f, 0f, 0f, to);
    }

    void ResolveCarSideDoor()
    {
        if (carSideDoor == null && !string.IsNullOrEmpty(carSideDoorName))
        {
            GameObject doorObj = GameObject.Find(carSideDoorName);
            if (doorObj == null)
                doorObj = FindByNameRecursive(carSideDoorName, transform, false);
            if (doorObj == null)
                doorObj = FindByNameRecursive(carSideDoorName, transform, true);

            if (doorObj != null)
                carSideDoor = doorObj.transform;
        }

        if (carSideDoor != null)
        {
            carDoorClosedRotation = carSideDoor.localRotation;
            carDoorOpenRotation = carDoorClosedRotation * Quaternion.Euler(carSideDoorOpenAngleX, 0f, 0f);
        }
    }

    IEnumerator AnimateCarSideDoor(bool open, float duration)
    {
        if (carSideDoor == null)
            yield break;

        Quaternion from = open ? carDoorClosedRotation : carDoorOpenRotation;
        Quaternion to = open ? carDoorOpenRotation : carDoorClosedRotation;

        if (duration <= 0f)
        {
            carSideDoor.localRotation = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = t * t * (3f - 2f * t);
            carSideDoor.localRotation = Quaternion.Slerp(from, to, smooth);
            yield return null;
        }

        carSideDoor.localRotation = to;
    }

    static GameObject FindByNameRecursive(string targetName, Transform root, bool allowContains)
    {
        if (root == null)
            return null;

        foreach (Transform child in root)
        {
            bool isMatch = string.Equals(child.name, targetName, System.StringComparison.OrdinalIgnoreCase);
            if (!isMatch && allowContains)
                isMatch = child.name.ToLowerInvariant().Contains(targetName.ToLowerInvariant());

            if (isMatch)
                return child.gameObject;

            GameObject nested = FindByNameRecursive(targetName, child, allowContains);
            if (nested != null)
                return nested;
        }

        return null;
    }

    bool TryLoadRecordedWalkPath(out StoreIntroPathSample[] samples, out float lastSampleTime)
    {
        samples = null;
        lastSampleTime = 0f;

        if (!_walkPathLoadAttempted)
        {
            _walkPathLoadAttempted = true;
            string name = string.IsNullOrEmpty(recordedWalkPathResourceName)
                ? "StoreIntroWalkPath"
                : recordedWalkPathResourceName;
            TextAsset ta = Resources.Load<TextAsset>(name);
            if (ta != null && !string.IsNullOrEmpty(ta.text))
            {
                StoreIntroPathFile data = JsonUtility.FromJson<StoreIntroPathFile>(ta.text);
                if (data != null && data.samples != null && data.samples.Length >= 2)
                {
                    _walkPathSamples = data.samples;
                    _walkPathLastT = _walkPathSamples[_walkPathSamples.Length - 1].t;
                }
            }
        }

        samples = _walkPathSamples;
        lastSampleTime = _walkPathLastT;
        return _walkPathSamples != null && _walkPathSamples.Length >= 2;
    }

    static CinematicPose PathSampleToPose(StoreIntroPathSample s)
    {
        Vector3 p = new Vector3(s.px, s.py, s.pz);
        Quaternion q = Quaternion.Normalize(new Quaternion(s.qx, s.qy, s.qz, s.qw));
        return new CinematicPose(p, q);
    }

    static void SampleWalkPath(StoreIntroPathSample[] s, float timeAlongPath, out Vector3 pos, out Quaternion rot)
    {
        float t = Mathf.Clamp(timeAlongPath, s[0].t, s[s.Length - 1].t);
        int i = s.Length - 2;
        for (int k = 0; k < s.Length - 1; k++)
        {
            if (t <= s[k + 1].t)
            {
                i = k;
                break;
            }
        }

        float t0 = s[i].t;
        float t1 = s[i + 1].t;
        float u = t1 > t0 ? Mathf.Clamp01((t - t0) / (t1 - t0)) : 0f;
        pos = Vector3.Lerp(new Vector3(s[i].px, s[i].py, s[i].pz), new Vector3(s[i + 1].px, s[i + 1].py, s[i + 1].pz), u);
        Quaternion q0 = Quaternion.Normalize(new Quaternion(s[i].qx, s[i].qy, s[i].qz, s[i].qw));
        Quaternion q1 = Quaternion.Normalize(new Quaternion(s[i + 1].qx, s[i + 1].qy, s[i + 1].qz, s[i + 1].qw));
        rot = Quaternion.Slerp(q0, q1, u);
    }

    void ApplyViewBob(ref Vector3 pos, ref Quaternion rot, float bobTime)
    {
        float cycle = bobTime * walkBobFrequency * Mathf.PI * 2f;
        Vector3 upOffset = Vector3.up * (Mathf.Sin(cycle) * walkBobVerticalAmplitude);
        Vector3 sideOffset = rot * Vector3.right * (Mathf.Cos(cycle * 0.5f) * walkBobLateralAmplitude);
        pos += upOffset + sideOffset;

        float pitchBob = Mathf.Sin(cycle) * walkBobTiltAmplitude;
        float rollBob = Mathf.Cos(cycle * 0.5f) * walkBobTiltAmplitude * 0.6f;
        rot = rot * Quaternion.Euler(pitchBob, 0f, rollBob);
    }

    IEnumerator FollowRecordedWalkPath(CinematicPose exitPose, StoreIntroPathSample[] samples, float pathLastT)
    {
        CinematicPose pathStart = PathSampleToPose(samples[0]);
        float speed = Mathf.Max(0.05f, recordedWalkPathPlaybackSpeed);
        float pathDuration = pathLastT / speed;
        float bobClock = 0f;

        float blendDur = Mathf.Max(0f, exitToRecordedPathBlendDuration);
        if (blendDur > 0f && Vector3.Distance(exitPose.position, pathStart.position) > 0.02f)
        {
            float blendElapsed = 0f;
            while (blendElapsed < blendDur)
            {
                blendElapsed += Time.deltaTime;
                float t = EvaluateEase(Mathf.Clamp01(blendElapsed / blendDur));
                Vector3 pos = Vector3.Lerp(exitPose.position, pathStart.position, t);
                Quaternion rot = Quaternion.Slerp(exitPose.rotation, pathStart.rotation, t);
                bobClock += Time.deltaTime;
                ApplyViewBob(ref pos, ref rot, bobClock);
                playerController.SetPose(pos, rot);
                yield return null;
            }
        }

        float pathElapsed = 0f;
        while (pathElapsed < pathDuration)
        {
            pathElapsed += Time.deltaTime;
            float pathT = Mathf.Clamp(pathElapsed * speed, 0f, pathLastT);
            SampleWalkPath(samples, pathT, out Vector3 pos, out Quaternion rot);
            bobClock += Time.deltaTime;
            ApplyViewBob(ref pos, ref rot, bobClock);
            playerController.SetPose(pos, rot);
            yield return null;
        }

        SampleWalkPath(samples, pathLastT, out Vector3 endPos, out Quaternion endRot);
        playerController.SetPose(endPos, endRot);
    }

    IEnumerator MovePose(CinematicPose from, CinematicPose to, float duration, bool walking)
    {
        if (duration <= 0f)
        {
            playerController.SetPose(to.position, to.rotation);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = EvaluateEase(Mathf.Clamp01(elapsed / duration));

            Vector3 pos = Vector3.Lerp(from.position, to.position, t);
            Quaternion rot = Quaternion.Slerp(from.rotation, to.rotation, t);

            if (walking)
                ApplyViewBob(ref pos, ref rot, elapsed);

            playerController.SetPose(pos, rot);
            yield return null;
        }

        playerController.SetPose(to.position, to.rotation);
    }

    float EvaluateEase(float t)
    {
        if (cinematicEase != null && cinematicEase.keys != null && cinematicEase.keys.Length > 0)
            return cinematicEase.Evaluate(t);
        return t * t * (3f - 2f * t);
    }

    IEnumerator AnimateDoors(bool open, float duration)
    {
        float start = open ? 0f : 1f;
        float end = open ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = t * t * (3f - 2f * t);
            SetDoorOpenAmount(Mathf.Lerp(start, end, smooth));
            yield return null;
        }

        SetDoorOpenAmount(end);
    }

    void SetDoorOpenAmount(float amount)
    {
        if (leftDoor != null)
            leftDoor.localPosition = Vector3.Lerp(leftDoorClosedLocalPosition, leftDoorOpenLocalPosition, amount);
        if (rightDoor != null)
            rightDoor.localPosition = Vector3.Lerp(rightDoorClosedLocalPosition, rightDoorOpenLocalPosition, amount);
    }

    void OnDestroy()
    {
        if (fadeCanvas != null)
            Destroy(fadeCanvas.gameObject);
    }
}
