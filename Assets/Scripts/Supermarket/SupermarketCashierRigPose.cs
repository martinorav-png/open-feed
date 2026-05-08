using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(120)]
public class SupermarketCashierRigPose : MonoBehaviour
{
    public enum ArmIkMode
    {
        Off = 0,
        Counter = 1,
        RelaxedBesideBody = 2
    }

    [Header("Scene References")]
    public Transform target;
    public Transform tableSurface;
    public string tableSurfaceName = "Object_6988";

    [Header("Look")]
    [Range(0f, 120f)] public float maxHeadYawDegrees = 80f;
    [Range(0f, 60f)] public float maxHeadPitchDegrees = 35f;
    [Range(0f, 45f)] public float maxChestYawDegrees = 12f;
    public float lookSpeed = 7f;

    [Header("Arm IK")]
    public ArmIkMode armIkMode = ArmIkMode.RelaxedBesideBody;

    [Header("Counter pose (armIkMode = Counter)")]
    public float handSpread = 0.62f;
    public float handForwardDistance = 0.56f;
    public float handSurfaceOffset = 0.045f;
    public float elbowForwardBias = 0.42f;
    public float elbowSideBias = 0.16f;

    [Header("Relaxed beside body (armIkMode = RelaxedBesideBody)")]
    [Tooltip("World Y offset from character root (feet) for hand hang height.")]
    public float relaxedHandHeight = 0.80f;
    [Tooltip("How far each hand is from the torso midline (meters). Lower = closer to body.")]
    public float relaxedSideOffset = 0.25f;
    [Tooltip("Along character -forward: positive pulls hands back; negative moves them forward.")]
    public float relaxedBackOffset = -0.07f;
    public float relaxedElbowForwardBias = 0.06f;
    public float relaxedElbowSideBias = 0.28f;
    [Tooltip("How strongly relaxed mode snaps wrist palm aim toward the torso.")]
    [Range(0f, 1f)] public float relaxedHandOrientBlend = 0.074f;

    Transform _spine02;
    Transform _spine01;
    Transform _spine;
    Transform _neck;
    Transform _head;
    Transform _headFront;
    Transform _leftUpper;
    Transform _leftLower;
    Transform _leftHand;
    Transform _rightUpper;
    Transform _rightLower;
    Transform _rightHand;

    readonly Dictionary<Transform, Quaternion> _rest = new Dictionary<Transform, Quaternion>();
    Vector3 _headLocalForward = Vector3.forward;
    bool _captured;
    CashierGestureController _gestures;

    void Awake()
    {
        ResolveReferences();
        ResolveBones();
        CaptureRestPose();
    }

    void LateUpdate()
    {
        if (_gestures == null)
            _gestures = GetComponent<CashierGestureController>();

        if (_gestures != null && _gestures.UseRelaxedArmIkOverlay && armIkMode == ArmIkMode.RelaxedBesideBody)
        {
            ResolveReferences();
            ResolveBones();
            CaptureRestPose();
            Restore(_leftUpper);
            Restore(_leftLower);
            Restore(_leftHand);
            Restore(_rightUpper);
            Restore(_rightLower);
            Restore(_rightHand);
            GetRelaxedHandTargets(out Vector3 leftRel, out Vector3 rightRel);
            SolveArm(_leftUpper, _leftLower, _leftHand, leftRel, true, relaxedElbowForwardBias, relaxedElbowSideBias, ApplyPalmsTowardTorso);
            SolveArm(_rightUpper, _rightLower, _rightHand, rightRel, false, relaxedElbowForwardBias, relaxedElbowSideBias, ApplyPalmsTowardTorso);
            return;
        }

        if (_gestures != null && _gestures.IsPlaying)
            return;

        ResolveReferences();
        ResolveBones();
        CaptureRestPose();

        Restore(_spine02);
        Restore(_spine01);
        Restore(_spine);
        Restore(_neck);
        Restore(_head);
        Restore(_leftUpper);
        Restore(_leftLower);
        Restore(_leftHand);
        Restore(_rightUpper);
        Restore(_rightLower);
        Restore(_rightHand);

        ApplyChestLook();
        ApplyHeadLook();

        if (armIkMode == ArmIkMode.Counter)
        {
            GetHandTargets(out Vector3 leftTarget, out Vector3 rightTarget);
            SolveArm(_leftUpper, _leftLower, _leftHand, leftTarget, true, elbowForwardBias, elbowSideBias, ApplyPalmOnCounter);
            SolveArm(_rightUpper, _rightLower, _rightHand, rightTarget, false, elbowForwardBias, elbowSideBias, ApplyPalmOnCounter);
        }
        else if (armIkMode == ArmIkMode.RelaxedBesideBody)
        {
            GetRelaxedHandTargets(out Vector3 leftRel, out Vector3 rightRel);
            SolveArm(_leftUpper, _leftLower, _leftHand, leftRel, true, relaxedElbowForwardBias, relaxedElbowSideBias, ApplyPalmsTowardTorso);
            SolveArm(_rightUpper, _rightLower, _rightHand, rightRel, false, relaxedElbowForwardBias, relaxedElbowSideBias, ApplyPalmsTowardTorso);
        }
    }

    void ResolveReferences()
    {
        if (target == null)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                StoreFirstPersonController fpc = FindAnyObjectByType<StoreFirstPersonController>();
                if (fpc != null && fpc.cameraPivot != null)
                    cam = fpc.cameraPivot.GetComponentInChildren<Camera>(true);
            }
            if (cam != null)
                target = cam.transform;
        }

        if (tableSurface == null && !string.IsNullOrWhiteSpace(tableSurfaceName))
        {
            GameObject table = GameObject.Find(tableSurfaceName);
            if (table != null)
                tableSurface = table.transform;
        }
    }

    void ResolveBones()
    {
        if (_head != null && _leftHand != null && _rightHand != null) return;

        Transform[] bones = GetComponentsInChildren<Transform>(true);
        foreach (Transform bone in bones)
        {
            switch (bone.name)
            {
                case "Spine02": _spine02 = bone; break;
                case "Spine01": _spine01 = bone; break;
                case "Spine": _spine = bone; break;
                case "neck": _neck = bone; break;
                case "Head": _head = bone; break;
                case "headfront": _headFront = bone; break;
                case "LeftArm": _leftUpper = bone; break;
                case "LeftForeArm": _leftLower = bone; break;
                case "LeftHand": _leftHand = bone; break;
                case "RightArm": _rightUpper = bone; break;
                case "RightForeArm": _rightLower = bone; break;
                case "RightHand": _rightHand = bone; break;
            }
        }
    }

    void CaptureRestPose()
    {
        if (_captured) return;

        Capture(_spine02);
        Capture(_spine01);
        Capture(_spine);
        Capture(_neck);
        Capture(_head);
        Capture(_leftUpper);
        Capture(_leftLower);
        Capture(_leftHand);
        Capture(_rightUpper);
        Capture(_rightLower);
        Capture(_rightHand);

        if (_head != null && _headFront != null)
        {
            Vector3 face = _headFront.position - _head.position;
            if (face.sqrMagnitude > 0.0001f)
                _headLocalForward = _head.InverseTransformDirection(face.normalized);
        }

        _captured = true;
    }

    void Capture(Transform bone)
    {
        if (bone != null && !_rest.ContainsKey(bone))
            _rest.Add(bone, bone.localRotation);
    }

    void Restore(Transform bone)
    {
        Quaternion rot;
        if (bone != null && _rest.TryGetValue(bone, out rot))
            bone.localRotation = rot;
    }

    void ApplyChestLook()
    {
        if (target == null || _spine == null) return;

        Vector3 toTarget = target.position - _spine.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.001f) return;

        float yaw = Mathf.DeltaAngle(transform.eulerAngles.y, Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg);
        yaw = Mathf.Clamp(yaw, -maxChestYawDegrees, maxChestYawDegrees);
        Quaternion add = Quaternion.Euler(0f, yaw, 0f);
        Quaternion targetRot = add * _spine.rotation;
        _spine.rotation = Quaternion.Slerp(_spine.rotation, targetRot, 1f - Mathf.Exp(-lookSpeed * 0.4f * Time.deltaTime));
    }

    void ApplyHeadLook()
    {
        if (target == null || _head == null || _head.parent == null) return;

        Vector3 toTarget = target.position - _head.position;
        if (toTarget.sqrMagnitude < 0.001f) return;

        Vector3 localForward = SafeAxis(_headLocalForward, Vector3.forward);
        Quaternion axisCorrection = Quaternion.Inverse(Quaternion.LookRotation(localForward, Vector3.up));
        Quaternion desiredWorld = Quaternion.LookRotation(toTarget.normalized, Vector3.up) * axisCorrection;
        Quaternion desiredLocal = Quaternion.Inverse(_head.parent.rotation) * desiredWorld;

        Quaternion restLocal = _rest.ContainsKey(_head) ? _rest[_head] : _head.localRotation;
        Quaternion delta = desiredLocal * Quaternion.Inverse(restLocal);
        Vector3 e = delta.eulerAngles;
        float pitch = Mathf.Clamp(NormalizeAngle(e.x), -maxHeadPitchDegrees, maxHeadPitchDegrees);
        float yaw = Mathf.Clamp(NormalizeAngle(e.y), -maxHeadYawDegrees, maxHeadYawDegrees);
        Quaternion clamped = Quaternion.Euler(pitch, yaw, 0f) * restLocal;
        _head.localRotation = Quaternion.Slerp(_head.localRotation, clamped, 1f - Mathf.Exp(-lookSpeed * Time.deltaTime));
    }

    void GetHandTargets(out Vector3 leftTarget, out Vector3 rightTarget)
    {
        Vector3 characterLeft = -transform.right;
        Vector3 basePoint = transform.position + transform.forward * handForwardDistance;
        float y = transform.position.y + 0.78f;

        if (tableSurface != null)
        {
            Renderer tableRenderer = tableSurface.GetComponent<Renderer>();
            if (tableRenderer != null)
            {
                Bounds b = tableRenderer.bounds;
                y = b.max.y + handSurfaceOffset;
                basePoint.x = Mathf.Clamp(basePoint.x, b.min.x + 0.12f, b.max.x - 0.12f);
                basePoint.z = Mathf.Clamp(basePoint.z, b.min.z + 0.12f, b.max.z - 0.12f);
            }
        }

        leftTarget = basePoint + characterLeft * (handSpread * 0.5f);
        rightTarget = basePoint - characterLeft * (handSpread * 0.5f);
        leftTarget.y = y;
        rightTarget.y = y;
    }

    void GetRelaxedHandTargets(out Vector3 leftTarget, out Vector3 rightTarget)
    {
        Vector3 root = transform.position;
        float y = root.y + relaxedHandHeight;
        Vector3 mid = new Vector3(root.x, y, root.z);
        Vector3 lat = transform.right;
        float w = Mathf.Max(0.02f, relaxedSideOffset);
        leftTarget = mid - lat * w;
        rightTarget = mid + lat * w;
        Vector3 back = -transform.forward * relaxedBackOffset;
        leftTarget += back;
        rightTarget += back;
    }

    void ApplyPalmOnCounter(Transform lower, Transform hand, Vector3 wristTarget, bool left)
    {
        Quaternion palmDown = Quaternion.LookRotation(transform.forward, Vector3.down);
        hand.rotation = Quaternion.Slerp(hand.rotation, palmDown, 0.9f);
    }

    void ApplyPalmsTowardTorso(Transform lower, Transform hand, Vector3 wristTarget, bool left)
    {
        Vector3 fromElbow = wristTarget - lower.position;
        Vector3 fingerFwd = fromElbow.sqrMagnitude > 1e-6f ? fromElbow.normalized : Vector3.down;
        fingerFwd = Vector3.Slerp(fingerFwd, Vector3.down, 0.22f).normalized;

        // Inner palm faces the body midline (+right from the character's left hand, -right from the right).
        Vector3 towardTorso = left ? transform.right : -transform.right;
        Vector3 handUp = Vector3.ProjectOnPlane(towardTorso, fingerFwd);
        if (handUp.sqrMagnitude < 1e-5f)
            handUp = Vector3.ProjectOnPlane(-transform.forward, fingerFwd);
        handUp.Normalize();

        Quaternion desired = Quaternion.LookRotation(fingerFwd, handUp);
        hand.rotation = Quaternion.Slerp(hand.rotation, desired, relaxedHandOrientBlend);
    }

    void SolveArm(
        Transform upper,
        Transform lower,
        Transform hand,
        Vector3 targetPos,
        bool left,
        float poleForward,
        float poleSide,
        System.Action<Transform, Transform, Vector3, bool> applyHand)
    {
        if (upper == null || lower == null || hand == null || applyHand == null) return;

        Vector3 root = upper.position;
        float upperLen = Vector3.Distance(upper.position, lower.position);
        float lowerLen = Vector3.Distance(lower.position, hand.position);
        Vector3 toTarget = targetPos - root;
        float distance = Mathf.Clamp(toTarget.magnitude, 0.001f, upperLen + lowerLen - 0.001f);
        Vector3 dir = toTarget.normalized;

        Vector3 side = left ? -transform.right : transform.right;
        Vector3 pole = Vector3.ProjectOnPlane(transform.forward * poleForward + side * poleSide + Vector3.down * 0.18f, dir);
        if (pole.sqrMagnitude < 0.0001f)
            pole = Vector3.ProjectOnPlane(Vector3.down, dir);
        pole.Normalize();

        float x = Mathf.Clamp((upperLen * upperLen + distance * distance - lowerLen * lowerLen) / (2f * distance), 0f, upperLen);
        float y = Mathf.Sqrt(Mathf.Max(0f, upperLen * upperLen - x * x));
        Vector3 elbowTarget = root + dir * x + pole * y;

        RotateBoneToward(upper, lower.position - upper.position, elbowTarget - upper.position);
        RotateBoneToward(lower, hand.position - lower.position, targetPos - lower.position);

        applyHand(lower, hand, targetPos, left);
    }

    static void RotateBoneToward(Transform bone, Vector3 current, Vector3 desired)
    {
        if (current.sqrMagnitude < 0.000001f || desired.sqrMagnitude < 0.000001f) return;
        bone.rotation = Quaternion.FromToRotation(current.normalized, desired.normalized) * bone.rotation;
    }

    static Vector3 SafeAxis(Vector3 axis, Vector3 fallback)
    {
        return axis.sqrMagnitude > 0.0001f ? axis.normalized : fallback;
    }

    static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        if (angle < -180f) angle += 360f;
        return angle;
    }
}
