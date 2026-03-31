using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class PhoneInteraction : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public GameObject phoneObject;
    public GameObject phoneCanvas;
    public FirstPersonCamera cameraController;

    [Header("Inspect Position (camera local space)")]
    public Vector3 inspectLocalPos = new Vector3(0f, -0.02f, 0.22f);
    public Vector3 inspectLocalRot = new Vector3(-90f, 0f, 0f);

    [Header("Settings")]
    public float pickupDistance = 3f;
    public float transitionSpeed = 10f;
    public float overlayAlpha = 0.6f;

    [Header("Crosshair")]
    public float crosshairSize = 4f;
    public Color crosshairColor = new Color(1f, 1f, 1f, 0.6f);
    public Color crosshairHoverColor = new Color(0.4f, 0.9f, 0.5f, 0.9f);

    [Header("Button Feedback")]
    public Color buttonPressColor = new Color(0.35f, 0.35f, 0.45f, 1f);
    public float buttonFlashDuration = 0.12f;

    // State
    private enum State { Idle, PickingUp, Inspecting, PuttingDown }
    private State state = State.Idle;
    private float transitionProgress = 0f;

    // Original phone transform
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    private Transform originalParent;

    // Transition start values
    private Vector3 transitionStartPos;
    private Quaternion transitionStartRot;

    // UI elements
    private GameObject crosshairCanvas;
    private Image crosshairDot;
    private GameObject overlayCanvas;
    private Image overlayImage;
    private float overlayTargetAlpha = 0f;

    // Button flash
    private Image flashingButton;
    private Color flashOriginalColor;
    private float flashTimer = 0f;

    void Start()
    {
        // Find camera
        if (playerCamera == null)
            playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Camera[] cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (cams.Length > 0)
                playerCamera = cams[0];
        }
        if (playerCamera != null && !playerCamera.CompareTag("MainCamera"))
            playerCamera.tag = "MainCamera";

        // Find camera controller
        if (cameraController == null && playerCamera != null)
            cameraController = playerCamera.GetComponent<FirstPersonCamera>();

        // Find phone
        if (phoneObject == null)
            phoneObject = GameObject.Find("Phone");
        if (phoneCanvas == null)
        {
            GameObject c = GameObject.Find("PhoneCanvas");
            if (c != null) phoneCanvas = c;
        }

        if (phoneObject != null)
        {
            originalLocalPos = phoneObject.transform.localPosition;
            originalLocalRot = phoneObject.transform.localRotation;
            originalParent = phoneObject.transform.parent;
            EnsurePhoneCollider();
        }

        if (phoneCanvas != null)
            SetupButtonColliders();

        CreateCrosshair();
        CreateOverlay();

        Debug.Log($"PhoneInteraction ready. Camera:{playerCamera != null} Phone:{phoneObject != null} Canvas:{phoneCanvas != null} CamCtrl:{cameraController != null}");
    }

    void Update()
    {
        if (phoneObject == null || playerCamera == null) return;

        HandleButtonFlash();
        HandleTransition();
        HandleInput();
        UpdateCrosshair();
        UpdateOverlay();
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
                // Don't pick up phone if browsing monitor
                MonitorInteraction monitorInt = FindAnyObjectByType<MonitorInteraction>();
                if (monitorInt != null && monitorInt.IsBrowsing()) return;

                // Left click to pick up phone
                if (mouse.leftButton.wasPressedThisFrame)
                    TryPickupPhone();
                break;

            case State.Inspecting:
                // Left click to press buttons
                if (mouse.leftButton.wasPressedThisFrame)
                    TryClickButton();

                // Right click or Q to put phone down
                if (mouse.rightButton.wasPressedThisFrame ||
                    (keyboard != null && keyboard.qKey.wasPressedThisFrame))
                    StartPutDown();
                break;
        }
    }

    // ============================
    // PICKUP / PUTDOWN
    // ============================

    void TryPickupPhone()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupDistance))
        {
            if (IsPhoneObject(hit.transform))
                StartPickup();
        }
    }

    void StartPickup()
    {
        // Parent phone to camera
        phoneObject.transform.SetParent(playerCamera.transform);

        // Record where the phone currently is in camera local space for lerping
        transitionStartPos = phoneObject.transform.localPosition;
        transitionStartRot = phoneObject.transform.localRotation;

        state = State.PickingUp;
        transitionProgress = 0f;
        overlayTargetAlpha = overlayAlpha;

        Debug.Log("Picking up phone...");
    }

    void StartPutDown()
    {
        // Re-parent to desk
        phoneObject.transform.SetParent(originalParent);

        transitionStartPos = phoneObject.transform.localPosition;
        transitionStartRot = phoneObject.transform.localRotation;

        state = State.PuttingDown;
        transitionProgress = 0f;
        overlayTargetAlpha = 0f;

        // Lock cursor and re-enable camera
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (cameraController != null)
            cameraController.enabled = true;

        // Show crosshair again
        if (crosshairCanvas != null)
            crosshairCanvas.SetActive(true);

        Debug.Log("Putting phone down...");
    }

    // ============================
    // TRANSITIONS
    // ============================

    void HandleTransition()
    {
        if (state != State.PickingUp && state != State.PuttingDown) return;

        transitionProgress += Time.deltaTime * transitionSpeed;
        float t = Mathf.Clamp01(transitionProgress);
        float smooth = t * t * (3f - 2f * t); // smoothstep

        if (state == State.PickingUp)
        {
            Quaternion targetRot = Quaternion.Euler(inspectLocalRot);

            phoneObject.transform.localPosition = Vector3.Lerp(transitionStartPos, inspectLocalPos, smooth);
            phoneObject.transform.localRotation = Quaternion.Slerp(transitionStartRot, targetRot, smooth);

            if (t >= 1f)
            {
                phoneObject.transform.localPosition = inspectLocalPos;
                phoneObject.transform.localRotation = targetRot;
                state = State.Inspecting;

                // Unlock cursor for button clicking
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // Disable camera look
                if (cameraController != null)
                    cameraController.enabled = false;

                // Hide crosshair during inspect
                if (crosshairCanvas != null)
                    crosshairCanvas.SetActive(false);

                Debug.Log("Phone inspect mode active. Click buttons or right-click/Q to put down.");
            }
        }
        else if (state == State.PuttingDown)
        {
            phoneObject.transform.localPosition = Vector3.Lerp(transitionStartPos, originalLocalPos, smooth);
            phoneObject.transform.localRotation = Quaternion.Slerp(transitionStartRot, originalLocalRot, smooth);

            if (t >= 1f)
            {
                phoneObject.transform.localPosition = originalLocalPos;
                phoneObject.transform.localRotation = originalLocalRot;
                state = State.Idle;
                Debug.Log("Phone back on desk.");
            }
        }
    }

    // ============================
    // BUTTON CLICKING (screen-space raycast during inspect)
    // ============================

    void TryClickButton()
    {
        // Raycast from mouse position since cursor is visible
        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit[] hits = Physics.RaycastAll(ray, 2f);

        foreach (RaycastHit hit in hits)
        {
            PhoneButton button = hit.transform.GetComponent<PhoneButton>();
            if (button == null && hit.transform.parent != null)
                button = hit.transform.parent.GetComponent<PhoneButton>();

            if (button != null)
            {
                OnButtonPressed(button);
                return;
            }
        }
    }

    void OnButtonPressed(PhoneButton button)
    {
        Debug.Log($"Button pressed: {button.buttonValue}");

        Image btnImage = button.GetComponent<Image>();
        if (btnImage != null)
        {
            flashOriginalColor = btnImage.color;
            flashingButton = btnImage;
            btnImage.color = buttonPressColor;
            flashTimer = buttonFlashDuration;
        }
    }

    void HandleButtonFlash()
    {
        if (flashingButton == null) return;
        flashTimer -= Time.deltaTime;
        if (flashTimer <= 0f)
        {
            flashingButton.color = flashOriginalColor;
            flashingButton = null;
        }
    }

    // ============================
    // CROSSHAIR
    // ============================

    void CreateCrosshair()
    {
        crosshairCanvas = new GameObject("CrosshairCanvas");
        crosshairCanvas.transform.SetParent(transform);

        Canvas c = crosshairCanvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 100;

        CanvasScaler scaler = crosshairCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject dot = new GameObject("Dot");
        dot.transform.SetParent(crosshairCanvas.transform, false);

        crosshairDot = dot.AddComponent<Image>();
        crosshairDot.color = crosshairColor;

        RectTransform rt = dot.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(crosshairSize, crosshairSize);
        rt.anchoredPosition = Vector2.zero;
    }

    void UpdateCrosshair()
    {
        if (crosshairDot == null || state != State.Idle) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        bool hovering = false;

        if (Physics.Raycast(ray, out hit, pickupDistance))
            hovering = IsPhoneObject(hit.transform);

        crosshairDot.color = hovering ? crosshairHoverColor : crosshairColor;
        crosshairDot.rectTransform.sizeDelta = hovering
            ? new Vector2(crosshairSize * 1.8f, crosshairSize * 1.8f)
            : new Vector2(crosshairSize, crosshairSize);
    }

    // ============================
    // DARK OVERLAY (fake blur/vignette)
    // ============================

    void CreateOverlay()
    {
        overlayCanvas = new GameObject("InspectOverlay");
        overlayCanvas.transform.SetParent(transform);

        Canvas c = overlayCanvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 50; // behind crosshair, in front of world

        CanvasScaler scaler = overlayCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject panel = new GameObject("DarkOverlay");
        panel.transform.SetParent(overlayCanvas.transform, false);

        overlayImage = panel.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0f);
        overlayImage.raycastTarget = false; // don't block clicks

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void UpdateOverlay()
    {
        if (overlayImage == null) return;

        Color c = overlayImage.color;
        float currentAlpha = c.a;
        float target = overlayTargetAlpha;

        if (Mathf.Abs(currentAlpha - target) > 0.01f)
        {
            float newAlpha = Mathf.Lerp(currentAlpha, target, Time.deltaTime * 8f);
            overlayImage.color = new Color(0f, 0f, 0f, newAlpha);
        }
        else
        {
            overlayImage.color = new Color(0f, 0f, 0f, target);
        }
    }

    // ============================
    // HELPERS
    // ============================

    bool IsPhoneObject(Transform t)
    {
        Transform current = t;
        while (current != null)
        {
            if (current.gameObject == phoneObject)
                return true;
            current = current.parent;
        }
        return false;
    }

    void EnsurePhoneCollider()
    {
        Collider existingCol = phoneObject.GetComponentInChildren<Collider>();
        if (existingCol != null) return;

        Transform phoneBody = phoneObject.transform.Find("PhoneBody");
        if (phoneBody != null)
        {
            BoxCollider box = phoneBody.gameObject.AddComponent<BoxCollider>();
            box.size = Vector3.one;
            return;
        }

        BoxCollider rootBox = phoneObject.AddComponent<BoxCollider>();
        rootBox.size = new Vector3(0.065f, 0.015f, 0.13f);
        rootBox.center = new Vector3(0, 0.008f, 0);
    }

    void SetupButtonColliders()
    {
        string[] names = {
            "Key_1", "Key_2", "Key_3", "Key_4", "Key_5", "Key_6",
            "Key_7", "Key_8", "Key_9", "Key_*", "Key_0", "Key_#",
            "CallButton", "DeleteButton"
        };
        string[] values = {
            "1", "2", "3", "4", "5", "6",
            "7", "8", "9", "*", "0", "#",
            "CALL", "DEL"
        };

        int found = 0;
        for (int i = 0; i < names.Length; i++)
        {
            Transform btn = FindDeep(phoneCanvas.transform, names[i]);
            if (btn == null) continue;
            found++;

            PhoneButton pb = btn.gameObject.GetComponent<PhoneButton>();
            if (pb == null) pb = btn.gameObject.AddComponent<PhoneButton>();
            pb.buttonValue = values[i];

            BoxCollider col = btn.gameObject.GetComponent<BoxCollider>();
            if (col == null)
            {
                col = btn.gameObject.AddComponent<BoxCollider>();
                RectTransform rt = btn.GetComponent<RectTransform>();
                if (rt != null)
                    col.size = new Vector3(rt.sizeDelta.x, rt.sizeDelta.y, 10f);
            }
        }
        Debug.Log($"Phone buttons: {found}/{names.Length} found.");
    }

    Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindDeep(child, name);
            if (result != null) return result;
        }
        return null;
    }

    void OnDestroy()
    {
        if (crosshairCanvas != null) Destroy(crosshairCanvas);
        if (overlayCanvas != null) Destroy(overlayCanvas);
    }
}