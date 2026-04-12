using UnityEngine;

/// <summary>
/// Marks a shopping cart root for <see cref="StoreShoppingCartInteraction"/> raycasts.
/// Ensures a physics body and a stable collider on the root when missing.
/// </summary>
[DisallowMultipleComponent]
public class StoreShoppingCart : MonoBehaviour
{
    [Tooltip("Optional override; otherwise bounds of all child renderers are used.")]
    public BoxCollider interactionCollider;

    [Tooltip("Y-axis rotation offset (degrees) applied when aligning the cart to a forward direction. Use this when the model imports facing backwards.")]
    public float yawOffsetDegrees;

    [Header("Held Collision")]
    [SerializeField, Min(0f)] float heldCollisionSkin = 0.02f;
    [SerializeField, Min(0f)] float heldCollisionExtraPadding = 0.01f;

    Rigidbody _rb;
    Collider _holderCollider;
    Collider[] _cartColliders;
    readonly RaycastHit[] _castHits = new RaycastHit[16];
    bool _held;
    bool _hasHeldTarget;
    Vector3 _heldTargetPos;
    Quaternion _heldTargetRot;

    void Reset()
    {
        EnsurePhysicsAndCollider();
    }

    void Awake()
    {
        EnsurePhysicsAndCollider();
        gameObject.tag = "ShoppingCart";
    }

    void FixedUpdate()
    {
        if (!_held || !_hasHeldTarget)
            return;
        if (_rb == null)
            _rb = GetComponent<Rigidbody>();
        if (_rb == null)
            return;

        Vector3 nextPos = ComputeHeldNonClippingPosition(_heldTargetPos);
        _rb.MovePosition(nextPos);
        _rb.MoveRotation(_heldTargetRot);
    }

    public void EnsurePhysicsAndCollider()
    {
        if (gameObject.tag != "ShoppingCart")
            gameObject.tag = "ShoppingCart";

        if (_rb == null)
            _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            _rb = gameObject.AddComponent<Rigidbody>();
            _rb.mass = 12f;
            _rb.linearDamping = 2f;
            _rb.angularDamping = 4f;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
        else
        {
            if (_rb.interpolation == RigidbodyInterpolation.None)
                _rb.interpolation = RigidbodyInterpolation.Interpolate;
            if (_rb.collisionDetectionMode == CollisionDetectionMode.Discrete)
                _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        if (interactionCollider != null)
            return;

        BoxCollider existing = GetComponent<BoxCollider>();
        if (existing != null)
        {
            interactionCollider = existing;
            return;
        }

        Bounds b = ComputeRootLocalBoundsFromRenderers();
        if (b.size.sqrMagnitude < 1e-6f)
        {
            b = new Bounds(Vector3.up * 0.5f, new Vector3(0.55f, 0.9f, 0.85f));
        }

        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.center = b.center;
        box.size = Vector3.Max(b.size, new Vector3(0.2f, 0.2f, 0.2f));
        interactionCollider = box;
    }

    Vector3 ComputeHeldNonClippingPosition(Vector3 desiredWorldPos)
    {
        EnsurePhysicsAndCollider();
        if (interactionCollider == null)
            return desiredWorldPos;

        BoxCollider box = interactionCollider;
        Vector3 startPos = _rb.position;
        Vector3 delta = desiredWorldPos - startPos;
        float dist = delta.magnitude;
        if (dist < 1e-4f)
            return desiredWorldPos;

        Vector3 dir = delta / dist;

        // World-space oriented box for sweeping.
        Quaternion startRot = _rb.rotation;
        Vector3 absScale = AbsVec(transform.lossyScale);
        Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, absScale);
        halfExtents += Vector3.one * heldCollisionExtraPadding;

        Vector3 scaledLocalCenter = Vector3.Scale(box.center, transform.lossyScale);
        Vector3 startCenter = startPos + startRot * scaledLocalCenter;

        // Sweep until the first blocking hit, ignoring our own colliders and the holder.
        int hitCount = Physics.BoxCastNonAlloc(
            startCenter,
            halfExtents,
            dir,
            _castHits,
            startRot,
            dist,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        float min = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit h = _castHits[i];
            Collider c = h.collider;
            if (c == null)
                continue;
            if (ShouldIgnoreCastHit(c))
                continue;
            if (h.distance < min)
                min = h.distance;
        }

        if (!float.IsFinite(min))
            return desiredWorldPos;

        float allowed = Mathf.Max(0f, min - heldCollisionSkin);
        return startPos + dir * allowed;
    }

    bool ShouldIgnoreCastHit(Collider c)
    {
        if (c == null)
            return true;
        if (_holderCollider != null && c == _holderCollider)
            return true;
        if (c.attachedRigidbody != null && c.attachedRigidbody == _rb)
            return true;
        if (transform != null && c.transform != null && c.transform.IsChildOf(transform))
            return true;

        return false;
    }

    static Vector3 AbsVec(Vector3 v) =>
        new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    Bounds ComputeRootLocalBoundsFromRenderers()
    {
        Renderer[] rends = GetComponentsInChildren<Renderer>(true);
        bool init = false;
        Bounds acc = default;

        Matrix4x4 worldToRoot = transform.worldToLocalMatrix;
        for (int i = 0; i < rends.Length; i++)
        {
            Renderer r = rends[i];
            if (r == null)
                continue;

            Bounds lb = r.localBounds;
            Vector3 c = lb.center;
            Vector3 e = lb.extents;
            if (e.sqrMagnitude < 1e-12f)
                continue;

            Matrix4x4 rendererToRoot = worldToRoot * r.localToWorldMatrix;
            for (int xi = -1; xi <= 1; xi += 2)
            for (int yi = -1; yi <= 1; yi += 2)
            for (int zi = -1; zi <= 1; zi += 2)
            {
                Vector3 corner = c + new Vector3(e.x * xi, e.y * yi, e.z * zi);
                Vector3 p = rendererToRoot.MultiplyPoint3x4(corner);
                if (!init)
                {
                    acc = new Bounds(p, Vector3.zero);
                    init = true;
                }
                else
                    acc.Encapsulate(p);
            }
        }

        if (!init)
            return new Bounds(Vector3.up * 0.5f, Vector3.one * 0.5f);

        return acc;
    }

    public void SetHeld(bool held, Collider holderCollider)
    {
        if (_rb == null)
            _rb = GetComponent<Rigidbody>();
        if (_rb == null)
            return;

        if (_cartColliders == null || _cartColliders.Length == 0)
            _cartColliders = GetComponentsInChildren<Collider>(true);

        if (_held && !held)
            SetIgnoreHolderCollision(false);

        _held = held;
        _holderCollider = held ? holderCollider : null;

        if (held)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            SetIgnoreHolderCollision(true);
        }
        else
        {
            _hasHeldTarget = false;
            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }

    public void SetHeldTargetPose(Vector3 worldPos, Quaternion worldRot)
    {
        _heldTargetPos = worldPos;
        _heldTargetRot = worldRot;
        _hasHeldTarget = true;
    }

    public Quaternion ApplyYawOffset(Quaternion baseRotation)
    {
        if (Mathf.Abs(yawOffsetDegrees) < 0.001f)
            return baseRotation;
        return baseRotation * Quaternion.Euler(0f, yawOffsetDegrees, 0f);
    }

    void SetIgnoreHolderCollision(bool ignore)
    {
        if (_holderCollider == null || _cartColliders == null)
            return;

        for (int i = 0; i < _cartColliders.Length; i++)
        {
            Collider c = _cartColliders[i];
            if (c == null || c == _holderCollider)
                continue;
            Physics.IgnoreCollision(_holderCollider, c, ignore);
        }
    }
}
