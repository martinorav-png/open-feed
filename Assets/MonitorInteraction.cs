using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MonitorInteraction : MonoBehaviour
{
    /// <summary>Invoked at the start of monitor zoom-in (when the player clicks the screen). Used by drive intro subtitles.</summary>
    public static event System.Action MonitorZoomStarted;

    [Header("References")]
    public Camera playerCamera;
    public FirstPersonCamera cameraController;
    public GameObject monitorCanvas;

    [Header("Desk Camera Targets")]
    public Vector3 deskViewPosition = new Vector3(0f, 1.15f, 6.8f);
    public Vector3 deskViewLookTarget = new Vector3(0f, 1.68f, 8.88f);
    public Vector3 zoomViewPosition = new Vector3(0f, 1.68f, 7.6f);
    public Vector3 zoomLookTarget = new Vector3(0f, 1.68f, 8.88f);
    public float deskViewFov = 60f;
    public float zoomFov = 56f;

    [Header("Settings")]
    [Tooltip("MonitorScreen BoxCollider size in local space. Full cube is 1,1,1; smaller values tighten the click target so nearby desk objects are not grabbed.")]
    [SerializeField] Vector3 monitorScreenHitboxSize = new Vector3(0.62f, 0.62f, 0.06f);

    [Tooltip("If a desk speaker is hit within this many meters \"behind\" the monitor along the same ray, count it as a speaker click (avoids huge screen collider stealing hits).")]
    [SerializeField] float speakerOverMonitorSlopMeters = 0.4f;

    public float interactDistance = 3f;
    public float zoomSpeed = 6f;
    public float overlayAlpha = 0.5f;
    public AnimationCurve zoomCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2.2f),
        new Keyframe(0.72f, 0.9f, 0.8f, 0.6f),
        new Keyframe(1f, 1f, 0f, 0f)
    );

    enum State { Idle, ZoomingIn, Browsing, ZoomingOut }

    State state = State.Idle;
    float transitionProgress;
    Vector3 transStartPos;
    Quaternion transStartRot;
    Vector3 transTargetPos;
    Quaternion transTargetRot;
    float transStartFov;
    float transTargetFov;
    Vector3 restoreViewPosition;
    Quaternion restoreViewRotation;
    float restoreViewFov;

    Transform monitorScreen;
    GameObject overlayCanvas;
    Image overlayImage;
    float overlayTargetAlpha;
    MonitorWebViewHost monitorWebHost;
    WebViewController deskWebView;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Camera[] cams = FindObjectsByType<Camera>();
            if (cams.Length > 0)
                playerCamera = cams[0];
        }

        if (playerCamera != null && cameraController == null)
            cameraController = playerCamera.GetComponent<FirstPersonCamera>();

        ResolveMonitorReferences();
        ResolveCanvasReferences();
        EnsureEventSystem();
        EnsureScreenCollider();
        CreateOverlay();

        MonitorBrowser browser = FindAnyObjectByType<MonitorBrowser>();
        if (browser == null)
            gameObject.AddComponent<MonitorBrowser>();
    }

    void Update()
    {
        if (playerCamera == null || monitorScreen == null)
            return;

        HandleTransition();
        UpdateOverlay();
        HandleInput();
    }

    void ResolveMonitorReferences()
    {
        GameObject taggedScreen = null;
        try
        {
            taggedScreen = GameObject.FindGameObjectWithTag("MonitorScreen");
        }
        catch (UnityException)
        {
        }

        if (taggedScreen != null)
            monitorScreen = taggedScreen.transform;

        if (monitorScreen == null)
        {
            GameObject monitorRoot = GameObject.Find("Monitor");
            if (monitorRoot != null)
                monitorScreen = monitorRoot.transform.Find("MonitorScreen");
        }
    }

    void ResolveCanvasReferences()
    {
        if (monitorCanvas == null)
        {
            GameObject canvasObject = GameObject.Find("MonitorCanvas");
            if (canvasObject != null)
                monitorCanvas = canvasObject;
        }

        if (monitorCanvas == null)
            return;

        Canvas canvas = monitorCanvas.GetComponent<Canvas>();
        if (canvas != null)
            canvas.worldCamera = playerCamera;

        monitorWebHost = monitorCanvas.GetComponent<MonitorWebViewHost>();
        deskWebView = monitorCanvas.GetComponentInChildren<WebViewController>(true);
        if (deskWebView == null)
            deskWebView = FindAnyObjectByType<WebViewController>();
    }

    void EnsureEventSystem()
    {
        EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            eventSystem = esObj.AddComponent<EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            return;
        }

        if (eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
        {
            BaseInputModule oldModule = eventSystem.GetComponent<BaseInputModule>();
            if (oldModule != null)
                Destroy(oldModule);
            eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }

    void HandleInput()
    {
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;
        if (mouse == null)
            return;

        if (state == State.Idle)
        {
            if (mouse.leftButton.wasPressedThisFrame)
                TryZoomIn();
            return;
        }

        if (state == State.Browsing)
        {
            if (mouse.rightButton.wasPressedThisFrame ||
                (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.qKey.wasPressedThisFrame)))
            {
                StartZoomOut();
            }
        }
    }

    void TryZoomIn()
    {
        PhoneInteraction phone = FindAnyObjectByType<PhoneInteraction>();
        if (phone != null && phone.IsActive())
            return;

        DeskObjectInteraction deskObjects = FindAnyObjectByType<DeskObjectInteraction>();
        if (deskObjects != null && deskObjects.IsActive())
            return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance);
        if (!HitsAllowMonitorZoom(hits))
            return;

        StartZoomIn();
    }

    /// <summary>True if these hits (e.g. from a center-screen ray) would start a monitor zoom, ignoring speakers in front.</summary>
    public bool HitsAllowMonitorZoom(RaycastHit[] hits)
    {
        if (hits == null || hits.Length == 0)
            return false;

        float monitorDist = float.MaxValue;
        float speakerDist = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            Transform t = hits[i].collider.transform;
            float d = hits[i].distance;
            if (IsSpeakerObject(t))
                speakerDist = Mathf.Min(speakerDist, d);
            if (IsMonitorObject(t))
                monitorDist = Mathf.Min(monitorDist, d);
        }

        if (monitorDist == float.MaxValue)
            return false;

        if (speakerDist != float.MaxValue && speakerDist <= monitorDist + speakerOverMonitorSlopMeters)
            return false;

        return true;
    }

    static readonly string[] SpeakerObjectNames = { "Cube.009", "Cube.010" };

    static bool IsSpeakerObject(Transform hitTransform)
    {
        Transform c = hitTransform;
        while (c != null)
        {
            for (int i = 0; i < SpeakerObjectNames.Length; i++)
            {
                if (c.name == SpeakerObjectNames[i])
                    return true;
            }

            c = c.parent;
        }

        return false;
    }

    // Only MonitorScreen (and its children) or MonitorCanvas — not sibling props under the Monitor root.
    bool IsMonitorObject(Transform hitTransform)
    {
        if (monitorScreen == null)
            return false;

        Transform current = hitTransform;
        while (current != null)
        {
            if (current == monitorScreen)
                return true;
            if (current.name == "MonitorCanvas")
                return true;
            current = current.parent;
        }

        return false;
    }

    void StartZoomIn()
    {
        MonitorZoomStarted?.Invoke();

        restoreViewPosition = playerCamera.transform.position;
        restoreViewRotation = playerCamera.transform.rotation;
        restoreViewFov = playerCamera.fieldOfView;

        transStartPos = playerCamera.transform.position;
        transStartRot = playerCamera.transform.rotation;
        transStartFov = playerCamera.fieldOfView;
        transTargetPos = zoomViewPosition;
        transTargetRot = Quaternion.LookRotation((zoomLookTarget - zoomViewPosition).normalized, Vector3.up);
        transTargetFov = zoomFov;

        state = State.ZoomingIn;
        transitionProgress = 0f;
        overlayTargetAlpha = overlayAlpha;
    }

    void StartZoomOut()
    {
        ApplyMonitorBrowsing(false);
        transStartPos = playerCamera.transform.position;
        transStartRot = playerCamera.transform.rotation;
        transStartFov = playerCamera.fieldOfView;
        transTargetPos = restoreViewPosition;
        transTargetRot = restoreViewRotation;
        transTargetFov = restoreViewFov;

        state = State.ZoomingOut;
        transitionProgress = 0f;
        overlayTargetAlpha = 0f;
    }

    void HandleTransition()
    {
        if (state != State.ZoomingIn && state != State.ZoomingOut)
            return;

        float duration = Mathf.Max(0.08f, 1f / Mathf.Max(0.01f, zoomSpeed));
        transitionProgress += Time.deltaTime / duration;
        float t = Mathf.Clamp01(transitionProgress);
        float smooth = zoomCurve != null ? zoomCurve.Evaluate(t) : t * t * (3f - 2f * t);

        playerCamera.transform.position = Vector3.Lerp(transStartPos, transTargetPos, smooth);
        playerCamera.transform.rotation = Quaternion.Slerp(transStartRot, transTargetRot, smooth);
        playerCamera.fieldOfView = Mathf.Lerp(transStartFov, transTargetFov, smooth);

        if (t < 1f)
            return;

        playerCamera.transform.position = transTargetPos;
        playerCamera.transform.rotation = transTargetRot;
        playerCamera.fieldOfView = transTargetFov;

        if (state == State.ZoomingIn)
        {
            state = State.Browsing;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (cameraController != null)
                cameraController.enabled = false;

            if (monitorCanvas != null)
            {
                Canvas canvas = monitorCanvas.GetComponent<Canvas>();
                if (canvas != null)
                    canvas.worldCamera = playerCamera;
            }

            ApplyMonitorBrowsing(true);
        }
        else
        {
            state = State.Idle;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            cameraController?.SyncToCurrentTransform();
            if (cameraController != null)
                cameraController.enabled = true;
        }
    }

    void ApplyMonitorBrowsing(bool browsing)
    {
        if (deskWebView != null && deskWebView.AlignsToWorldViewport)
            deskWebView.SetMonitorBrowsingMode(browsing);
        else
            monitorWebHost?.SetMonitorBrowsing(browsing);
    }

    void EnsureScreenCollider()
    {
        if (monitorScreen == null)
            return;

        MeshCollider meshCollider = monitorScreen.GetComponent<MeshCollider>();
        if (meshCollider != null)
            Destroy(meshCollider);

        BoxCollider box = monitorScreen.GetComponent<BoxCollider>();
        if (box == null)
            box = monitorScreen.gameObject.AddComponent<BoxCollider>();

        Vector3 s = monitorScreenHitboxSize;
        if (s.x <= 0f || s.y <= 0f || s.z <= 0f)
            s = new Vector3(0.62f, 0.62f, 0.06f);

        box.size = s;
        box.center = Vector3.zero;
        box.isTrigger = false;
    }

    void CreateOverlay()
    {
        overlayCanvas = new GameObject("MonitorOverlay");
        overlayCanvas.transform.SetParent(transform);

        Canvas canvas = overlayCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;

        CanvasScaler scaler = overlayCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject panel = new GameObject("DarkOverlay");
        panel.transform.SetParent(overlayCanvas.transform, false);

        overlayImage = panel.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0f);
        overlayImage.raycastTarget = false;

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    void UpdateOverlay()
    {
        if (overlayImage == null)
            return;

        float current = overlayImage.color.a;
        if (Mathf.Abs(current - overlayTargetAlpha) > 0.01f)
        {
            float nextAlpha = Mathf.Lerp(current, overlayTargetAlpha, Time.deltaTime * 8f);
            overlayImage.color = new Color(0f, 0f, 0f, nextAlpha);
        }
        else
        {
            overlayImage.color = new Color(0f, 0f, 0f, overlayTargetAlpha);
        }
    }

    public bool IsBrowsing()
    {
        return state != State.Idle;
    }

    public bool IsZoomed()
    {
        return state != State.Idle;
    }

    void OnDestroy()
    {
        if (overlayCanvas != null)
            Destroy(overlayCanvas);
    }
}