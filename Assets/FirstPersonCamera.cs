using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonCamera : MonoBehaviour
{
    [Header("Mouse Settings")]
    public float sensitivity = 2.0f;
    public float maxLookUp = 60f;
    public float maxLookDown = -40f;

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SyncToCurrentTransform();
    }

    void OnEnable()
    {
        SyncToCurrentTransform();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Click in the game view to re-lock after Escape (Editor/standalone).
        if (Cursor.lockState != CursorLockMode.Locked && Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Vector2 mouseDelta = Vector2.zero;
        if (Mouse.current != null)
            mouseDelta = Mouse.current.delta.ReadValue();

#if ENABLE_LEGACY_INPUT_MANAGER
        // When Active Input Handling is "Both", fall back if the new Input System
        // reports no mouse (e.g. focus/backend quirks); axes come from the old Input Manager.
        if (mouseDelta.sqrMagnitude < 1e-6f && Cursor.lockState == CursorLockMode.Locked)
        {
            mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 40f;
        }
#endif

        if (mouseDelta.sqrMagnitude < 1e-6f)
            return;

        rotationY += mouseDelta.x * sensitivity * 0.1f;
        rotationX -= mouseDelta.y * sensitivity * 0.1f;
        rotationX = Mathf.Clamp(rotationX, maxLookDown, maxLookUp);

        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0f);
    }

    public void SyncToCurrentTransform()
    {
        Vector3 currentRotation = transform.eulerAngles;
        rotationY = currentRotation.y;
        rotationX = currentRotation.x;
        if (rotationX > 180f)
            rotationX -= 360f;
    }
}