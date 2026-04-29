using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class StoreFirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3.2f;
    public float sprintSpeed = 5f;
    public float gravity = -20f;

    [Header("Look")]
    public Transform cameraPivot;
    public float mouseSensitivity = 2f;
    public float minPitch = -40f;
    public float maxPitch = 65f;

    [Header("View bob (WASD)")]
    public bool enableLocomotionViewBob = true;
    [Tooltip("Smaller than intro cinematic bob — vertical meters peak-to-center.")]
    public float viewBobVerticalAmplitude = 0.018f;
    public float viewBobLateralAmplitude = 0.005f;
    public float viewBobFrequency = 2f;
    public float viewBobTiltAmplitude = 0.32f;
    public float viewBobSprintFrequencyMultiplier = 1.12f;

    CharacterController characterController;
    Vector3 cameraPivotBaseLocal;
    float verticalVelocity;
    float pitch;
    float viewBobPhase;
    bool isControlEnabled;
    bool cinematicMode;

    public bool IsControlEnabled => isControlEnabled;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        EnsureGltfStoreFloorColliderOnObject6();
        if (cameraPivot == null && Camera.main != null)
            cameraPivot = Camera.main.transform;

        if (cameraPivot != null)
            cameraPivotBaseLocal = cameraPivot.localPosition;

        if (GetComponent<StoreShoppingCartInteraction>() == null)
            gameObject.AddComponent<StoreShoppingCartInteraction>();
        if (GetComponent<StoreInteriorCollisionBootstrap>() == null)
            gameObject.AddComponent<StoreInteriorCollisionBootstrap>();
    }

    /// <summary>scene.gltf floor mesh is often named Object_6; import has no collider — add a static MeshCollider for CharacterController.</summary>
    static void EnsureGltfStoreFloorColliderOnObject6()
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform t = transforms[i];
            if (t == null || t.name != "Object_6")
                continue;
            if (t.GetComponent<MeshCollider>() != null)
                continue;
            MeshFilter mf = t.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
                continue;
            MeshCollider mc = t.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false;
        }
    }

    void Start()
    {
        SetControlEnabled(false);
        if (cameraPivot != null)
        {
            pitch = cameraPivot.localEulerAngles.x;
            if (pitch > 180f) pitch -= 360f;
        }
    }

    void Update()
    {
        if (!isControlEnabled)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Cursor.lockState != CursorLockMode.Locked && Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        UpdateLook();
        UpdateMovement();
    }

    void LateUpdate()
    {
        if (!isControlEnabled || cinematicMode || cameraPivot == null)
            return;

        ApplyLocomotionViewBob();
    }

    void UpdateLook()
    {
        if (cameraPivot == null)
            return;

        Vector2 mouseDelta = Vector2.zero;
        if (Mouse.current != null)
            mouseDelta = Mouse.current.delta.ReadValue();

#if ENABLE_LEGACY_INPUT_MANAGER
        if (mouseDelta.sqrMagnitude < 1e-6f && Cursor.lockState == CursorLockMode.Locked)
            mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 40f;
#endif

        if (mouseDelta.sqrMagnitude < 1e-6f)
            return;

        float yawDelta = mouseDelta.x * mouseSensitivity * 0.1f;
        float pitchDelta = mouseDelta.y * mouseSensitivity * 0.1f;

        transform.Rotate(0f, yawDelta, 0f);
        pitch = Mathf.Clamp(pitch - pitchDelta, minPitch, maxPitch);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void UpdateMovement()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        Vector2 input = Vector2.zero;
        if (keyboard.wKey.isPressed) input.y += 1f;
        if (keyboard.sKey.isPressed) input.y -= 1f;
        if (keyboard.dKey.isPressed) input.x += 1f;
        if (keyboard.aKey.isPressed) input.x -= 1f;
        input = Vector2.ClampMagnitude(input, 1f);

        float speed = keyboard.leftShiftKey.isPressed ? sprintSpeed : moveSpeed;
        Vector3 move = (transform.forward * input.y + transform.right * input.x) * speed;

        if (characterController.isGrounded)
            verticalVelocity = -1f;
        else
            verticalVelocity += gravity * Time.deltaTime;

        move.y = verticalVelocity;
        characterController.Move(move * Time.deltaTime);
    }

    void ApplyLocomotionViewBob()
    {
        if (!enableLocomotionViewBob)
        {
            cameraPivot.localPosition = cameraPivotBaseLocal;
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        Vector2 input = Vector2.zero;
        if (keyboard.wKey.isPressed) input.y += 1f;
        if (keyboard.sKey.isPressed) input.y -= 1f;
        if (keyboard.dKey.isPressed) input.x += 1f;
        if (keyboard.aKey.isPressed) input.x -= 1f;
        input = Vector2.ClampMagnitude(input, 1f);

        float moveAmt = characterController != null && characterController.isGrounded
            ? input.magnitude
            : 0f;

        if (moveAmt > 0.01f)
        {
            float freqMul = keyboard.leftShiftKey.isPressed ? viewBobSprintFrequencyMultiplier : 1f;
            viewBobPhase += Time.deltaTime * viewBobFrequency * Mathf.PI * 2f * moveAmt * freqMul;
        }

        float amp = moveAmt;
        float cycle = viewBobPhase;
        Vector3 bobPos = cameraPivotBaseLocal;
        bobPos.y += Mathf.Sin(cycle) * (viewBobVerticalAmplitude * amp);
        bobPos.x += Mathf.Cos(cycle * 0.5f) * (viewBobLateralAmplitude * amp);
        cameraPivot.localPosition = bobPos;

        float pitchBob = Mathf.Sin(cycle) * (viewBobTiltAmplitude * amp);
        float rollBob = Mathf.Cos(cycle * 0.5f) * (viewBobTiltAmplitude * 0.6f * amp);
        cameraPivot.localRotation = Quaternion.Euler(pitch + pitchBob, 0f, rollBob);
    }

    public void SetControlEnabled(bool enabled)
    {
        isControlEnabled = enabled;
        if (enabled)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void SetCinematicMode(bool enabled)
    {
        cinematicMode = enabled;
        if (characterController != null)
            characterController.enabled = !enabled;
    }

    public void SetPose(Vector3 worldPosition, Quaternion worldRotation)
    {
        if (characterController != null && !cinematicMode)
            characterController.enabled = false;

        Quaternion bodyRotation = Quaternion.Euler(0f, worldRotation.eulerAngles.y, 0f);
        transform.rotation = bodyRotation;

        Vector3 cameraLocalOffset = cameraPivot != null ? cameraPivotBaseLocal : new Vector3(0f, 1.62f, 0f);
        transform.position = worldPosition - (bodyRotation * cameraLocalOffset);

        if (cameraPivot != null)
        {
            float x = worldRotation.eulerAngles.x;
            if (x > 180f) x -= 360f;
            pitch = Mathf.Clamp(x, minPitch, maxPitch);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        if (characterController != null && !cinematicMode)
            characterController.enabled = true;
    }
}
