using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

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
    public float roadScrollSpeed = 18f;

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

    // Singleton per scene
    public static CutscenePlayer Instance;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        FindSceneReferences();
        CreateSubtitleUI();
        SetupDefaultSubtitles();
        StartCoroutine(PlayCutscene());
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
                new SubtitleEntry { triggerTime = 1f, text = "The roads are empty tonight.", displayDuration = 3f },
                new SubtitleEntry { triggerTime = 6f, text = "Rain streaks across the windshield.", displayDuration = 3f },
                new SubtitleEntry { triggerTime = 12f, text = "You think about nothing in particular.", displayDuration = 3.5f },
                new SubtitleEntry { triggerTime = 18f, text = "Almost home.", displayDuration = 2.5f },
            };
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

        // Start subtitle coroutine
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
        float duration = 22f;
        float elapsed = 0f;

        // Find all scrollable objects (everything except camera and car interior)
        Transform sceneRoot = null;
        GameObject drivingScene = GameObject.Find("DrivingScene");
        if (drivingScene != null)
            sceneRoot = drivingScene.transform;

        // Collect transforms to scroll
        Transform road = null, streetLights = null, trees = null, guardrails = null, rain = null;
        if (sceneRoot != null)
        {
            foreach (Transform child in sceneRoot)
            {
                switch (child.name)
                {
                    case "Road": road = child; break;
                    case "StreetLights": streetLights = child; break;
                    case "Trees": trees = child; break;
                    case "Guardrails": guardrails = child; break;
                    case "Rain": rain = child; break;
                }
            }
        }

        // Camera subtle sway
        Vector3 camBasePos = cam != null ? cam.transform.localPosition : Vector3.zero;
        Quaternion camBaseRot = cam != null ? cam.transform.localRotation : Quaternion.identity;

        // Waypoint-based camera look
        int wpIndex = 0;
        float wpSegmentTime = duration / Mathf.Max(1, waypoints != null ? waypoints.Length : 1);

        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            cutsceneTimer += dt;

            // Scroll environment backward to simulate driving forward
            float scrollDelta = roadScrollSpeed * dt;
            Vector3 scroll = new Vector3(0, 0, -scrollDelta);

            if (road != null) road.position += scroll;
            if (streetLights != null) streetLights.position += scroll;
            if (trees != null) trees.position += scroll;
            if (guardrails != null) guardrails.position += scroll;

            // Keep rain following the camera
            if (rain != null && cam != null)
                rain.position = new Vector3(cam.transform.position.x, 8f, cam.transform.position.z + 10f);

            // Camera subtle movements (breathing, road bumps)
            if (cam != null)
            {
                float sway = Mathf.Sin(elapsed * 0.7f) * 0.003f;
                float bump = Mathf.PerlinNoise(elapsed * 2f, 0) * 0.01f - 0.005f;
                float lookSway = Mathf.Sin(elapsed * 0.3f) * 0.5f;

                cam.transform.localPosition = camBasePos + new Vector3(sway, bump, 0);
                cam.transform.localRotation = camBaseRot * Quaternion.Euler(bump * 10f, lookSway, sway * 5f);

                // Waypoint-driven look direction changes
                if (waypoints != null && waypoints.Length > 1)
                {
                    int targetWp = Mathf.Min((int)(elapsed / wpSegmentTime), waypoints.Length - 1);
                    if (targetWp != wpIndex)
                    {
                        wpIndex = targetWp;
                        camBaseRot = waypoints[wpIndex].localRotation;
                    }
                }
            }

            yield return null;
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
