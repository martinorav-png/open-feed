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

    CharacterController characterController;
    float verticalVelocity;
    float pitch;
    bool isControlEnabled;
    bool cinematicMode;

    public bool IsControlEnabled => isControlEnabled;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (cameraPivot == null && Camera.main != null)
            cameraPivot = Camera.main.transform;
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

        UpdateLook();
        UpdateMovement();

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void UpdateLook()
    {
        if (cameraPivot == null || Mouse.current == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
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

        Vector3 cameraLocalOffset = cameraPivot != null ? cameraPivot.localPosition : new Vector3(0f, 1.62f, 0f);
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
