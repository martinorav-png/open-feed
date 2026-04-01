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
        if (Mouse.current == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        rotationY += mouseDelta.x * sensitivity * 0.1f;
        rotationX -= mouseDelta.y * sensitivity * 0.1f;
        rotationX = Mathf.Clamp(rotationX, maxLookDown, maxLookUp);

        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0f);

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
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