using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class MonitorInteraction : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public FirstPersonCamera cameraController;
    public GameObject monitorCanvas;

    [Header("Zoom Settings")]
    public float zoomDistance = 0.5f;

    [Header("Settings")]
    public float interactDistance = 3f;
    public float zoomSpeed = 6f;
    public float overlayAlpha = 0.5f;

    // State
    private enum State { Idle, ZoomingIn, Browsing, ZoomingOut }
    private State state = State.Idle;
    private float transitionProgress = 0f;

    // Camera original
    private Vector3 cameraOriginalPos;
    private Quaternion cameraOriginalRot;

    // Transition
    private Vector3 transStartPos;
    private Quaternion transStartRot;
    private Vector3 transTargetPos;
    private Quaternion transTargetRot;

    // Monitor reference
    private GameObject monitorObject;
    private Transform monitorScreen;

    // Overlay
    private GameObject overlayCanvas;
    private Image overlayImage;
    private float overlayTargetAlpha = 0f;

    // Event system for UI clicking
    private EventSystem eventSystem;
    private GraphicRaycaster canvasRaycaster;

    void Start()
    {
        // Find camera
        if (playerCamera == null)
            playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Camera[] cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (cams.Length > 0) playerCamera = cams[0];
        }

        if (playerCamera != null && cameraController == null)
            cameraController = playerCamera.GetComponent<FirstPersonCamera>();

        // Find monitor
        monitorObject = GameObject.Find("Monitor");
        if (monitorObject != null)
        {
            monitorScreen = monitorObject.transform.Find("MonitorScreen");
            if (monitorScreen == null)
                monitorScreen = monitorObject.transform;
        }

        // Find canvas
        if (monitorCanvas == null)
        {
            GameObject c = GameObject.Find("MonitorCanvas");
            if (c != null) monitorCanvas = c;
        }

        // Setup canvas for interaction
        if (monitorCanvas != null)
        {
            Canvas canvas = monitorCanvas.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.worldCamera = playerCamera;
            }

            canvasRaycaster = monitorCanvas.GetComponent<GraphicRaycaster>();
        }

        // Ensure MonitorBrowser exists
        MonitorBrowser browser = FindAnyObjectByType<MonitorBrowser>();
        if (browser == null)
            gameObject.AddComponent<MonitorBrowser>();

        // Ensure EventSystem exists
        eventSystem = FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            eventSystem = esObj.AddComponent<EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
        else
        {
            // Make sure it has the new input system module
            if (eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
            {
                // Remove old input module if present
                var oldModule = eventSystem.GetComponent<BaseInputModule>();
                if (oldModule != null) Destroy(oldModule);
                eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        // Ensure MonitorScreen has a collider for raycasting
        EnsureScreenCollider();

        CreateOverlay();

        Debug.Log($"MonitorInteraction ready. Camera:{playerCamera != null} Monitor:{monitorObject != null} Canvas:{monitorCanvas != null}");
    }

    void Update()
    {
        if (playerCamera == null || monitorObject == null) return;

        HandleTransition();
        UpdateOverlay();
        HandleInput();
    }

    // ============================
    // INPUT
    // ============================

    void HandleInput()
    {
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;
        if (mouse == null) return;

        switch (state)
        {
            case State.Idle:
                if (mouse.leftButton.wasPressedThisFrame)
                    TryZoomIn();
                break;

            case State.Browsing:
                // Right click or Q to exit browser mode
                if (mouse.rightButton.wasPressedThisFrame ||
                    (keyboard != null && keyboard.qKey.wasPressedThisFrame))
                {
                    StartZoomOut();
                }
                break;
        }
    }

    // ============================
    // ZOOM IN / OUT
    // ============================

    void TryZoomIn()
    {
        // Check if PhoneInteraction is active
        PhoneInteraction phone = FindAnyObjectByType<PhoneInteraction>();
        if (phone != null)
        {
            var stateField = phone.GetType().GetField("state",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (stateField != null)
            {
                int stateVal = (int)stateField.GetValue(phone);
                if (stateVal != 0) return;
            }
        }

        // Check if DeskObjectInteraction is animating something
        DeskObjectInteraction doi = FindAnyObjectByType<DeskObjectInteraction>();

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            if (IsMonitorObject(hit.transform))
            {
                StartZoomIn();
            }
        }
    }

    bool IsMonitorObject(Transform t)
    {
        Transform current = t;
        while (current != null)
        {
            if (current.gameObject == monitorObject || current.name == "MonitorCanvas")
                return true;
            current = current.parent;
        }
        return false;
    }

    void StartZoomIn()
    {
        // Store camera's current position
        cameraOriginalPos = playerCamera.transform.position;
        cameraOriginalRot = playerCamera.transform.rotation;

        // Get the screen center in world space
        Vector3 screenCenter = monitorObject.transform.TransformPoint(new Vector3(0, 0.2f, 0.2f));

        // Step back from the screen toward the player
        Vector3 dirToPlayer = (cameraOriginalPos - screenCenter).normalized;
        transTargetPos = screenCenter + dirToPlayer * zoomDistance;

        // Look directly at the screen center
        Quaternion worldZoomRot = Quaternion.LookRotation(screenCenter - transTargetPos, Vector3.up);

        transStartPos = cameraOriginalPos;
        transStartRot = cameraOriginalRot;
        transTargetRot = worldZoomRot;

        state = State.ZoomingIn;
        transitionProgress = 0f;
        overlayTargetAlpha = overlayAlpha;

        Debug.Log("Zooming into monitor...");
    }

    void StartZoomOut()
    {
        transStartPos = playerCamera.transform.position;
        transStartRot = playerCamera.transform.rotation;
        transTargetPos = cameraOriginalPos;
        transTargetRot = cameraOriginalRot;

        state = State.ZoomingOut;
        transitionProgress = 0f;
        overlayTargetAlpha = 0f;

        // Lock cursor and re-enable camera look
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (cameraController != null)
            cameraController.enabled = true;

        Debug.Log("Exiting browser mode...");
    }

    // ============================
    // TRANSITIONS
    // ============================

    void HandleTransition()
    {
        if (state != State.ZoomingIn && state != State.ZoomingOut) return;

        transitionProgress += Time.deltaTime * zoomSpeed;
        float t = Mathf.Clamp01(transitionProgress);
        float smooth = t * t * (3f - 2f * t);

        playerCamera.transform.position = Vector3.Lerp(transStartPos, transTargetPos, smooth);
        playerCamera.transform.rotation = Quaternion.Slerp(transStartRot, transTargetRot, smooth);

        if (t >= 1f)
        {
            playerCamera.transform.position = transTargetPos;
            playerCamera.transform.rotation = transTargetRot;

            if (state == State.ZoomingIn)
            {
                state = State.Browsing;

                // Unlock cursor for UI interaction
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // Disable camera look
                if (cameraController != null)
                    cameraController.enabled = false;

                // Ensure canvas event camera is set
                if (monitorCanvas != null)
                {
                    Canvas c = monitorCanvas.GetComponent<Canvas>();
                    if (c != null) c.worldCamera = playerCamera;
                }

                Debug.Log("Browser mode active. Click feeds, scroll page. Right-click or Q to exit.");
            }
            else if (state == State.ZoomingOut)
            {
                state = State.Idle;
                Debug.Log("Back to desk view.");
            }
        }
    }

    // ============================
    // OVERLAY
    // ============================

    void CreateOverlay()
    {
        overlayCanvas = new GameObject("MonitorOverlay");
        overlayCanvas.transform.SetParent(transform);

        Canvas c = overlayCanvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 40;

        CanvasScaler scaler = overlayCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject panel = new GameObject("DarkOverlay");
        panel.transform.SetParent(overlayCanvas.transform, false);

        overlayImage = panel.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0f);
        overlayImage.raycastTarget = false;

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void UpdateOverlay()
    {
        if (overlayImage == null) return;

        float current = overlayImage.color.a;
        float target = overlayTargetAlpha;

        if (Mathf.Abs(current - target) > 0.01f)
        {
            float a = Mathf.Lerp(current, target, Time.deltaTime * 8f);
            overlayImage.color = new Color(0, 0, 0, a);
        }
        else
        {
            overlayImage.color = new Color(0, 0, 0, target);
        }
    }

    // ============================
    // COLLIDER SETUP
    // ============================

    void EnsureScreenCollider()
    {
        if (monitorScreen == null) return;

        // Check if MonitorScreen or its children have a collider
        Collider existing = monitorScreen.GetComponentInChildren<Collider>();
        if (existing != null) return;

        // Add a box collider to MonitorScreen
        BoxCollider col = monitorScreen.gameObject.AddComponent<BoxCollider>();
        col.size = new Vector3(1f, 1f, 0.5f);
        col.center = new Vector3(0, 0, 0.1f);

        Debug.Log("Added collider to MonitorScreen for interaction.");
    }

    // ============================
    // PUBLIC STATE CHECK
    // ============================

    public bool IsBrowsing()
    {
        return state == State.Browsing || state == State.ZoomingIn;
    }

    void OnDestroy()
    {
        if (overlayCanvas != null) Destroy(overlayCanvas);
    }
}