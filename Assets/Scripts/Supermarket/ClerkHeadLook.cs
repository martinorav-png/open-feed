using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public class ClerkHeadLook : MonoBehaviour
{
    public Transform headBone;
    public Transform target;
    [Range(0f, 140f)] public float maxYawDegrees = 110f;
    [Range(0f, 80f)] public float maxPitchDegrees = 45f;
    [Tooltip("Higher = faster snap.")]
    [SerializeField] float turnSpeed = 10f;

    [Tooltip("Local axis (in head bone space) that points OUT of the face. Common values to try: forward (0,0,1), up (0,1,0), or right (1,0,0).")]
    public Vector3 headLocalForward = Vector3.forward;
    [Tooltip("Local axis (in head bone space) that points UP from the top of the skull.")]
    public Vector3 headLocalUp = Vector3.up;

    Quaternion _restLocalRot;
    bool _captured;

    void OnEnable()
    {
        if (headBone != null && !_captured)
        {
            _restLocalRot = headBone.localRotation;
            _captured = true;
        }
    }

    void LateUpdate()
    {
        if (headBone == null) return;
        if (target == null || !target.gameObject.activeInHierarchy)
            ResolveTarget();
        if (target == null) return;
        if (!_captured)
        {
            _restLocalRot = headBone.localRotation;
            _captured = true;
        }

        // Restore to rest first so animations / drift don't accumulate.
        headBone.localRotation = _restLocalRot;

        Vector3 toTarget = target.position - headBone.position;
        if (toTarget.sqrMagnitude < 1e-6f) return;
        toTarget.Normalize();

        Vector3 localForward = SafeAxis(headLocalForward, Vector3.forward);
        Vector3 localUp = SafeAxis(headLocalUp, Vector3.up);
        localUp = MakePerpendicularUp(localForward, localUp);

        Quaternion axisCorrection = Quaternion.Inverse(Quaternion.LookRotation(localForward, localUp));
        Quaternion desiredWorld = Quaternion.LookRotation(toTarget, Vector3.up) * axisCorrection;
        Quaternion parentWorld = headBone.parent != null ? headBone.parent.rotation : Quaternion.identity;
        Quaternion desiredLocal = Quaternion.Inverse(parentWorld) * desiredWorld;
        Quaternion deltaFromRest = desiredLocal * Quaternion.Inverse(_restLocalRot);

        Vector3 e = deltaFromRest.eulerAngles;
        float pitch = NormalizeAngle(e.x);
        float yaw = NormalizeAngle(e.y);
        pitch = Mathf.Clamp(pitch, -maxPitchDegrees, maxPitchDegrees);
        yaw = Mathf.Clamp(yaw, -maxYawDegrees, maxYawDegrees);
        Quaternion clamped = Quaternion.Euler(pitch, yaw, 0f);

        Quaternion targetLocal = clamped * _restLocalRot;
        headBone.localRotation = Quaternion.Slerp(headBone.localRotation, targetLocal, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
    }

    void ResolveTarget()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var fpc = FindAnyObjectByType<StoreFirstPersonController>();
            if (fpc != null && fpc.cameraPivot != null)
                cam = fpc.cameraPivot.GetComponentInChildren<Camera>(true);
        }
        if (cam != null)
            target = cam.transform;
    }

    static Vector3 SafeAxis(Vector3 axis, Vector3 fallback)
    {
        return axis.sqrMagnitude > 1e-5f ? axis.normalized : fallback;
    }

    static Vector3 MakePerpendicularUp(Vector3 forward, Vector3 up)
    {
        up -= forward * Vector3.Dot(up, forward);
        if (up.sqrMagnitude > 1e-5f) return up.normalized;

        Vector3 fallback = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) < 0.9f ? Vector3.up : Vector3.forward;
        up = fallback - forward * Vector3.Dot(fallback, forward);
        return up.normalized;
    }

    static float NormalizeAngle(float a)
    {
        a %= 360f;
        if (a > 180f) a -= 360f;
        if (a < -180f) a += 360f;
        return a;
    }
}
