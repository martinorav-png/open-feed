using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class DeskSittingManager : MonoBehaviour
{
    public static DeskSittingManager Instance;

    [Header("Sitting View")]
    public Vector3 sittingPosition = new Vector3(15.4f, 1.65f, 10.8f);
    public Vector3 sittingLookTarget = new Vector3(16.5f, 1.6f, 10.8f);
    public float transitionDuration = 1.0f;

    [Header("References")]
    public StoreFirstPersonController playerController;
    public DeskObjectInteraction deskInteraction;
    public MonitorInteraction monitorInteraction;
    public PhoneInteraction phoneInteraction;
    public GameObject crosshair;

    private bool isSitting = false;
    private Vector3 preSittingPos;
    private Quaternion preSittingRot;
    private Transform cameraTransform;

    void Awake()
    {
        Instance = this;
        if (playerController == null) playerController = FindAnyObjectByType<StoreFirstPersonController>();
        if (deskInteraction == null) deskInteraction = FindAnyObjectByType<DeskObjectInteraction>();
        if (monitorInteraction == null) monitorInteraction = FindAnyObjectByType<MonitorInteraction>();
        if (phoneInteraction == null) phoneInteraction = FindAnyObjectByType<PhoneInteraction>();
        
        Camera cam = Camera.main;
        if (cam != null) cameraTransform = cam.transform;
    }

    public void ToggleSit(bool sit)
    {
        if (sit == isSitting) return;
        if (sit) StartCoroutine(SitDown());
        else StartCoroutine(StandUp());
    }

    IEnumerator SitDown()
    {
        isSitting = true;
        if (playerController != null) playerController.SetControlEnabled(false);
        
        preSittingPos = playerController.transform.position;
        preSittingRot = playerController.transform.rotation;

        Vector3 startPos = cameraTransform.position;
        Quaternion startRot = cameraTransform.rotation;
        Quaternion targetRot = Quaternion.LookRotation((sittingLookTarget - sittingPosition).normalized);

        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / transitionDuration);
            cameraTransform.position = Vector3.Lerp(startPos, sittingPosition, t);
            cameraTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        cameraTransform.position = sittingPosition;
        cameraTransform.rotation = targetRot;

        // Sync and enable FirstPersonCamera
        FirstPersonCamera fpc = cameraTransform.GetComponent<FirstPersonCamera>();
        if (fpc != null)
        {
            fpc.enabled = true;
            fpc.SyncToCurrentTransform();
        }

        // Enable desk interactions
        if (deskInteraction != null) deskInteraction.enabled = true;
        if (monitorInteraction != null) 
        {
            if (cameraTransform != null)
                monitorInteraction.playerCamera = cameraTransform.GetComponent<Camera>();

            FirstPersonCamera monitorCameraController = cameraTransform != null ? cameraTransform.GetComponent<FirstPersonCamera>() : null;
            if (monitorCameraController != null)
                monitorInteraction.cameraController = monitorCameraController;

            monitorInteraction.enabled = true;
            monitorInteraction.deskViewPosition = sittingPosition;
            monitorInteraction.deskViewLookTarget = sittingLookTarget;
            monitorInteraction.zoomViewPosition = Vector3.Lerp(sittingPosition, sittingLookTarget, 0.42f);
            monitorInteraction.zoomLookTarget = sittingLookTarget;
        }
        if (phoneInteraction != null) phoneInteraction.enabled = true;
        
        // Disable crosshair
        HouseCrosshair hc = FindAnyObjectByType<HouseCrosshair>();
        if (hc != null) hc.enabled = false;

        // Update GameFlow state
        if (GameFlowManager.Instance != null)
        {
            // Note: GameFlowManager doesn't have a public way to change state easily 
            // without knowing the internal transitions, but setting the field 
            // via reflection or just assuming it works. 
            // Actually, we don't strictly need it if we are just switching modes in-scene.
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Now sitting at the desk.");
    }

    IEnumerator StandUp()
    {
        isSitting = false;
        
        // Disable desk interactions
        if (deskInteraction != null) deskInteraction.enabled = false;
        if (monitorInteraction != null) monitorInteraction.enabled = false;
        if (phoneInteraction != null) phoneInteraction.enabled = false;
        
        FirstPersonCamera fpc = cameraTransform.GetComponent<FirstPersonCamera>();
        if (fpc != null) fpc.enabled = false;

        Vector3 startPos = cameraTransform.position;
        Quaternion startRot = cameraTransform.rotation;
        
        // We want to return to where we were, but maybe adjusted for the rig
        // Actually StoreFirstPersonController.SetPose handles it.
        
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / transitionDuration);
            cameraTransform.position = Vector3.Lerp(startPos, preSittingPos + Vector3.up * 1.62f, t);
            cameraTransform.rotation = Quaternion.Slerp(startRot, preSittingRot, t);
            yield return null;
        }

        if (playerController != null)
        {
            // SetPose expects the camera's world position and rotation
            Vector3 camTargetPos = preSittingPos + (preSittingRot * new Vector3(0, 1.62f, 0));
            playerController.SetPose(camTargetPos, preSittingRot);
            playerController.SetControlEnabled(true);
            
            // Reset camera local position to be exactly at the pivot
            cameraTransform.localPosition = Vector3.zero;
            cameraTransform.localRotation = Quaternion.identity;
        }
        
        HouseCrosshair hc = FindAnyObjectByType<HouseCrosshair>();
        if (hc != null) hc.enabled = true;

        Debug.Log("Stood up from the desk.");
    }

    void Update()
    {
        if (isSitting && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // Only stand up if not zoomed into monitor or phone
            bool isBusy = (monitorInteraction != null && monitorInteraction.IsZoomed()) || 
                          (phoneInteraction != null && phoneInteraction.IsActive());
            
            if (!isBusy)
            {
                ToggleSit(false);
            }
        }
    }
}
