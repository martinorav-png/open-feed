using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class PhoneInteraction : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public GameObject phoneObject;
    public FirstPersonCamera cameraController;

    [Header("Inspect Position")]
    public Vector3 inspectLocalPos = new Vector3(0f, 0.20f, 0.80f);
    public Vector3 inspectLocalRot = new Vector3(0f, 180f, 0f);
    public float rotateSpeed = 2.5f;

    [Header("Settings")]
    public float pickupDistance = 3f;
    public float transitionSpeed = 8f;
    public float overlayAlpha = 0.6f;

    [Header("Crosshair")]
    public float crosshairSize = 4f;
    public Color crosshairColor = new Color(1f, 1f, 1f, 0.6f);
    public Color crosshairHoverColor = new Color(0.4f, 0.9f, 0.5f, 0.9f);

    [Header("Screen while held")]
    [Tooltip("Phone canvas background while lifting or inspecting (brighter than on-desk default).")]
    [SerializeField] Color inspectBackgroundColor = new Color(0.78f, 0.86f, 0.64f, 0.98f);
    [Tooltip("PhoneScreen glow while held — higher alpha reads brighter.")]
    [SerializeField] Color inspectScreenGlowColor = new Color(0.82f, 0.92f, 0.70f, 0.16f);

    enum State { Idle, PickingUp, Inspecting, PuttingDown }

    State state = State.Idle;
    float transitionProgress;
    Vector3 originalLocalPos;
    Quaternion originalLocalRot;
    Transform originalParent;
    Vector3 transitionStartPos;
    Quaternion transitionStartRot;
    Vector2 inspectRotation;
    GameObject crosshairCanvas;
    Image crosshairDot;
    GameObject overlayCanvas;
    Image overlayImage;
    float overlayTargetAlpha;
    Collider phoneRootCollider;
    Canvas phoneCanvas;
    GraphicRaycaster phoneRaycaster;
    TextMeshProUGUI numberDisplayText;
    TextMeshProUGUI dialLabelText;
    TextMeshProUGUI timeText;
    TextMeshProUGUI batteryText;
    Image backgroundImage;
    Image screenGlowImage;
    string dialedNumber = "";
    bool callingMode;
    float nextClockUpdateTime;
    Color defaultBackgroundColor = new Color(0.57f, 0.66f, 0.46f, 0.96f);
    Color callingBackgroundColor = new Color(0.68f, 0.78f, 0.54f, 0.98f);

    void Start()
    {
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

        if (cameraController == null && playerCamera != null)
            cameraController = playerCamera.GetComponent<FirstPersonCamera>();

        if (phoneObject == null)
            phoneObject = GameObject.Find("Phone");

        if (phoneObject != null)
        {
            originalLocalPos = phoneObject.transform.localPosition;
            originalLocalRot = phoneObject.transform.localRotation;
            originalParent = phoneObject.transform.parent;
            EnsurePhoneCollider();
            phoneRootCollider = phoneObject.GetComponent<Collider>();
            CachePhoneUiReferences();
        }

        CreateCrosshair();
        CreateOverlay();
        EnsureEventSystem();
        RefreshPhoneUi();
    }

    void Update()
    {
        if (playerCamera == null)
            return;

        HandleTransition();
        HandleInput();
        UpdateCrosshair();
        UpdateOverlay();

        if (state == State.Inspecting && Mouse.current != null && Mouse.current.middleButton.isPressed)
            HandleInspectRotation();

        if (timeText != null && Time.unscaledTime >= nextClockUpdateTime)
        {
            UpdateClockDisplay();
            nextClockUpdateTime = Time.unscaledTime + 1f;
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
            MonitorInteraction monitor = FindAnyObjectByType<MonitorInteraction>();
            if (monitor != null && monitor.IsZoomed())
                return;

            DeskObjectInteraction deskObjects = FindAnyObjectByType<DeskObjectInteraction>();
            if (deskObjects != null && deskObjects.IsActive())
                return;

            if (mouse.leftButton.wasPressedThisFrame)
                TryPickupPhone();
            return;
        }

        if (state == State.Inspecting)
        {
            if (mouse.leftButton.wasPressedThisFrame)
                TryPressPhoneButton();

            if (mouse.rightButton.wasPressedThisFrame ||
                (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.qKey.wasPressedThisFrame)))
            {
                StartPutDown();
            }
        }
    }

    void TryPickupPhone()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, pickupDistance))
            return;

        Transform inspectable = FindInspectableInParents(hit.transform);
        if (inspectable == null)
            return;

        GameObject root = FindPhoneRoot(inspectable);
        if (root == null)
            return;

        phoneObject = root;
        originalLocalPos = phoneObject.transform.localPosition;
        originalLocalRot = phoneObject.transform.localRotation;
        originalParent = phoneObject.transform.parent;
        EnsurePhoneCollider();
        phoneRootCollider = phoneObject.GetComponent<Collider>();
        CachePhoneUiReferences();
        StartPickup();
    }

    void StartPickup()
    {
        if (phoneObject == null)
            return;

        phoneObject.transform.SetParent(playerCamera.transform);
        transitionStartPos = phoneObject.transform.localPosition;
        transitionStartRot = phoneObject.transform.localRotation;
        state = State.PickingUp;
        transitionProgress = 0f;
        overlayTargetAlpha = overlayAlpha;
        RefreshPhoneUi();
    }

    void StartPutDown()
    {
        if (phoneObject == null || originalParent == null)
            return;

        phoneObject.transform.SetParent(originalParent);
        transitionStartPos = phoneObject.transform.localPosition;
        transitionStartRot = phoneObject.transform.localRotation;
        state = State.PuttingDown;
        transitionProgress = 0f;
        overlayTargetAlpha = 0f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetPhoneUiInteractive(false);
        if (cameraController != null)
            cameraController.enabled = true;

        if (crosshairCanvas != null)
            crosshairCanvas.SetActive(true);

        RefreshPhoneUi();
    }

    void HandleTransition()
    {
        if (phoneObject == null || (state != State.PickingUp && state != State.PuttingDown))
            return;

        transitionProgress += Time.deltaTime * transitionSpeed;
        float t = Mathf.Clamp01(transitionProgress);
        float smooth = t * t * (3f - 2f * t);

        if (state == State.PickingUp)
        {
            Quaternion targetRot = Quaternion.Euler(inspectLocalRot);
            phoneObject.transform.localPosition = Vector3.Lerp(transitionStartPos, inspectLocalPos, smooth);
            phoneObject.transform.localRotation = Quaternion.Slerp(transitionStartRot, targetRot, smooth);

            if (t >= 1f)
            {
                phoneObject.transform.localPosition = inspectLocalPos;
                phoneObject.transform.localRotation = targetRot;
                inspectRotation = Vector2.zero;
                state = State.Inspecting;

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (cameraController != null)
                    cameraController.enabled = false;
                if (crosshairCanvas != null)
                    crosshairCanvas.SetActive(false);
                if (phoneRootCollider != null)
                    phoneRootCollider.enabled = false;
                SetPhoneUiInteractive(true);
                RefreshPhoneUi();
            }
        }
        else
        {
            phoneObject.transform.localPosition = Vector3.Lerp(transitionStartPos, originalLocalPos, smooth);
            phoneObject.transform.localRotation = Quaternion.Slerp(transitionStartRot, originalLocalRot, smooth);

            if (t >= 1f)
            {
                phoneObject.transform.localPosition = originalLocalPos;
                phoneObject.transform.localRotation = originalLocalRot;
                if (phoneRootCollider != null)
                    phoneRootCollider.enabled = true;
                state = State.Idle;
                RefreshPhoneUi();
            }
        }
    }

    void HandleInspectRotation()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || phoneObject == null)
            return;

        Vector2 delta = mouse.delta.ReadValue();
        inspectRotation.x = Mathf.Clamp(inspectRotation.x - delta.y * rotateSpeed * Time.deltaTime, -70f, 70f);
        inspectRotation.y += -delta.x * rotateSpeed * Time.deltaTime;

        Quaternion baseRotation = Quaternion.Euler(inspectLocalRot);
        Quaternion deltaRotation = Quaternion.Euler(inspectRotation.x, inspectRotation.y, 0f);
        phoneObject.transform.localRotation = baseRotation * deltaRotation;
    }

    void EnsureEventSystem()
    {
        EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            return;
        }

        if (eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    void CreateCrosshair()
    {
        crosshairCanvas = new GameObject("CrosshairCanvas");
        crosshairCanvas.transform.SetParent(transform);

        Canvas canvas = crosshairCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = crosshairCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject dot = new GameObject("Dot");
        dot.transform.SetParent(crosshairCanvas.transform, false);

        crosshairDot = dot.AddComponent<Image>();
        crosshairDot.color = crosshairColor;

        RectTransform rect = dot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(crosshairSize, crosshairSize);
        rect.anchoredPosition = Vector2.zero;
    }

    void UpdateCrosshair()
    {
        if (crosshairDot == null || state != State.Idle)
            return;

        float reach = pickupDistance;
        DeskObjectInteraction desk = FindAnyObjectByType<DeskObjectInteraction>();
        if (desk != null)
            reach = Mathf.Max(reach, desk.interactDistance);
        MonitorInteraction mon = FindAnyObjectByType<MonitorInteraction>();
        if (mon != null)
            reach = Mathf.Max(reach, mon.interactDistance);

        bool hovering = InteractableHoverQuery.IsCrosshairOverInteractable(playerCamera, reach);

        crosshairDot.color = hovering ? crosshairHoverColor : crosshairColor;
        crosshairDot.rectTransform.sizeDelta = hovering
            ? new Vector2(crosshairSize * 1.8f, crosshairSize * 1.8f)
            : new Vector2(crosshairSize, crosshairSize);
    }

    void CreateOverlay()
    {
        overlayCanvas = new GameObject("InspectOverlay");
        overlayCanvas.transform.SetParent(transform);

        Canvas canvas = overlayCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

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

        float currentAlpha = overlayImage.color.a;
        if (Mathf.Abs(currentAlpha - overlayTargetAlpha) > 0.01f)
        {
            float newAlpha = Mathf.Lerp(currentAlpha, overlayTargetAlpha, Time.deltaTime * 8f);
            overlayImage.color = new Color(0f, 0f, 0f, newAlpha);
        }
        else
        {
            overlayImage.color = new Color(0f, 0f, 0f, overlayTargetAlpha);
        }
    }

    Transform FindInspectableInParents(Transform current)
    {
        while (current != null)
        {
            if (current.CompareTag("Inspectable"))
                return current;
            current = current.parent;
        }

        return null;
    }

    GameObject FindPhoneRoot(Transform inspectable)
    {
        Transform current = inspectable;
        while (current != null)
        {
            if (current.name == "Phone")
                return current.gameObject;
            current = current.parent;
        }

        return inspectable.parent != null ? inspectable.parent.gameObject : inspectable.gameObject;
    }

    void EnsurePhoneCollider()
    {
        Collider existingCol = phoneObject.GetComponentInChildren<Collider>();
        if (existingCol != null)
            return;

        Transform phoneBody = phoneObject.transform.Find("Phone_Body");
        if (phoneBody == null)
            phoneBody = phoneObject.transform.Find("PhoneBody");
        if (phoneBody != null)
        {
            BoxCollider box = phoneBody.gameObject.AddComponent<BoxCollider>();
            box.size = Vector3.one;
            return;
        }

        BoxCollider rootBox = phoneObject.AddComponent<BoxCollider>();
        rootBox.size = new Vector3(0.065f, 0.015f, 0.13f);
        rootBox.center = new Vector3(0f, 0.008f, 0f);
    }

    void CachePhoneUiReferences()
    {
        if (phoneObject == null)
            return;

        phoneCanvas = phoneObject.GetComponentInChildren<Canvas>(true);
        if (phoneCanvas != null)
        {
            phoneRaycaster = phoneCanvas.GetComponent<GraphicRaycaster>();
            if (phoneRaycaster == null)
                phoneRaycaster = phoneCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        numberDisplayText = FindChildComponent<TextMeshProUGUI>("NumberDisplay");
        dialLabelText = FindChildComponent<TextMeshProUGUI>("DialLabel");
        timeText = FindChildComponent<TextMeshProUGUI>("Time");
        batteryText = FindChildComponent<TextMeshProUGUI>("Battery");
        backgroundImage = FindChildComponent<Image>("Background");
        screenGlowImage = FindChildComponent<Image>("PhoneScreenGlow");
    }

    T FindChildComponent<T>(string childName) where T : Component
    {
        if (phoneObject == null)
            return null;

        Transform child = FindNamedChild(phoneObject.transform, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    Transform FindNamedChild(Transform root, string childName)
    {
        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindNamedChild(root.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
    }

    void SetPhoneUiInteractive(bool interactive)
    {
        if (phoneCanvas != null)
        {
            phoneCanvas.worldCamera = playerCamera;
            phoneCanvas.enabled = true;
        }

        if (phoneRaycaster != null)
            phoneRaycaster.enabled = interactive;
    }

    bool TryPressPhoneButton()
    {
        if (TryPressPhoneCanvasButton())
            return true;

        return TryPressPhysicalPhoneButton();
    }

    bool TryPressPhoneCanvasButton()
    {
        if (phoneRaycaster == null)
            return false;

        EventSystem eventSystem = EventSystem.current;
        Mouse mouse = Mouse.current;
        if (eventSystem == null || mouse == null)
            return false;

        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = mouse.position.ReadValue()
        };

        var results = new List<RaycastResult>();
        phoneRaycaster.Raycast(pointerData, results);
        for (int i = 0; i < results.Count; i++)
        {
            Transform buttonRoot = FindPhoneButtonRoot(results[i].gameObject.transform);
            if (buttonRoot == null)
                continue;

            HandlePhoneButtonPress(buttonRoot.gameObject);
            return true;
        }

        return false;
    }

    bool TryPressPhysicalPhoneButton()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return false;

        Ray ray = playerCamera.ScreenPointToRay(mouse.position.ReadValue());
        RaycastHit[] hits = Physics.RaycastAll(ray, 3f);
        if (hits == null || hits.Length == 0)
            return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            PhoneButton phoneButton = hits[i].transform.GetComponentInParent<PhoneButton>();
            if (phoneButton == null)
                continue;

            if (phoneObject != null && !phoneButton.transform.IsChildOf(phoneObject.transform))
                continue;

            HandlePhoneButtonPress(phoneButton.gameObject);
            return true;
        }

        return false;
    }

    Transform FindPhoneButtonRoot(Transform current)
    {
        while (current != null)
        {
            if (IsPhoneButtonName(current.name))
                return current;

            if (phoneCanvas != null && current == phoneCanvas.transform)
                break;

            current = current.parent;
        }

        return null;
    }

    bool IsPhoneButtonName(string objectName)
    {
        return objectName.StartsWith("Key_") ||
               objectName == "CallButton" ||
               objectName == "DeleteButton" ||
               objectName.StartsWith("Nav_");
    }

    void HandlePhoneButtonPress(GameObject buttonObject)
    {
        PhoneButton physicalButton = buttonObject.GetComponent<PhoneButton>();
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        if (buttonRect != null)
            StartCoroutine(AnimatePhoneButton(buttonRect));
        else
            StartCoroutine(AnimatePhysicalButton(buttonObject.transform));

        string buttonName = physicalButton != null && !string.IsNullOrEmpty(physicalButton.buttonValue)
            ? physicalButton.buttonValue
            : buttonObject.name;
        if (buttonName.StartsWith("Key_"))
        {
            string keyValue = buttonName.Substring(4);
            if (dialedNumber.Length < 14)
                dialedNumber += keyValue;
            callingMode = false;
            RefreshPhoneUi();
            return;
        }

        if (buttonName == "DeleteButton")
        {
            if (dialedNumber.Length > 0)
                dialedNumber = dialedNumber.Substring(0, dialedNumber.Length - 1);
            callingMode = false;
            RefreshPhoneUi();
            return;
        }

        if (buttonName == "CallButton")
        {
            callingMode = true;
            RefreshPhoneUi();
            return;
        }

        if (buttonName.StartsWith("Nav_"))
        {
            callingMode = false;
            if (dialLabelText != null)
                dialLabelText.text = buttonName.Replace("Nav_", "").ToLowerInvariant();
            return;
        }
    }

    System.Collections.IEnumerator AnimatePhoneButton(RectTransform buttonRect)
    {
        Vector3 originalScale = buttonRect.localScale;
        Vector3 pressedScale = originalScale * 0.94f;

        float elapsed = 0f;
        while (elapsed < 0.06f)
        {
            elapsed += Time.unscaledDeltaTime;
            buttonRect.localScale = Vector3.Lerp(originalScale, pressedScale, elapsed / 0.06f);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < 0.08f)
        {
            elapsed += Time.unscaledDeltaTime;
            buttonRect.localScale = Vector3.Lerp(pressedScale, originalScale, elapsed / 0.08f);
            yield return null;
        }

        buttonRect.localScale = originalScale;
    }

    System.Collections.IEnumerator AnimatePhysicalButton(Transform buttonTransform)
    {
        Vector3 originalPos = buttonTransform.localPosition;
        Vector3 pressedPos = originalPos + new Vector3(0f, -0.0012f, 0f);

        float elapsed = 0f;
        while (elapsed < 0.05f)
        {
            elapsed += Time.unscaledDeltaTime;
            buttonTransform.localPosition = Vector3.Lerp(originalPos, pressedPos, elapsed / 0.05f);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < 0.07f)
        {
            elapsed += Time.unscaledDeltaTime;
            buttonTransform.localPosition = Vector3.Lerp(pressedPos, originalPos, elapsed / 0.07f);
            yield return null;
        }

        buttonTransform.localPosition = originalPos;
    }

    void RefreshPhoneUi()
    {
        if (numberDisplayText != null)
            numberDisplayText.text = callingMode ? "CALLING" : (string.IsNullOrEmpty(dialedNumber) ? "READY" : dialedNumber);

        if (dialLabelText != null)
            dialLabelText.text = callingMode
                ? (string.IsNullOrEmpty(dialedNumber) ? "UNKNOWN" : dialedNumber)
                : (string.IsNullOrEmpty(dialedNumber) ? "ENTER NO." : "GREEN=CALL");

        if (batteryText != null)
            batteryText.text = callingMode ? "SIG" : "BAT";

        if (backgroundImage != null)
        {
            if (callingMode)
                backgroundImage.color = callingBackgroundColor;
            else if (state == State.PickingUp || state == State.Inspecting)
                backgroundImage.color = inspectBackgroundColor;
            else
                backgroundImage.color = defaultBackgroundColor;
        }

        if (screenGlowImage != null)
        {
            screenGlowImage.enabled = true;
            if (callingMode)
                screenGlowImage.color = new Color(0.78f, 0.92f, 0.64f, 0.20f);
            else if (state == State.PickingUp || state == State.Inspecting)
                screenGlowImage.color = inspectScreenGlowColor;
            else
                screenGlowImage.color = new Color(0.64f, 0.75f, 0.53f, 0.08f);
        }

        UpdateClockDisplay();
    }

    void UpdateClockDisplay()
    {
        if (timeText != null)
            timeText.text = System.DateTime.Now.ToString("HH:mm");
    }

    public bool IsActive()
    {
        return state != State.Idle;
    }

    public bool IsInspecting()
    {
        return state != State.Idle;
    }

    void OnDestroy()
    {
        if (crosshairCanvas != null)
            Destroy(crosshairCanvas);
        if (overlayCanvas != null)
            Destroy(overlayCanvas);
    }
}