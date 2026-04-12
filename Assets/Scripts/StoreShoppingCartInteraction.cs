using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Supermarket / grocery interior: center-screen raycast. Left-click a <see cref="StoreShoppingCart"/> to
/// hold it in front of the player; move to push; right-click to leave it in place.
/// </summary>
[DefaultExecutionOrder(-40)]
[DisallowMultipleComponent]
public class StoreShoppingCartInteraction : MonoBehaviour
{
    const string SupermarketScene = "supermarket";
    const string GroceryScene = "GroceryStore";

    [SerializeField] float raycastDistance = 3.2f;
    [SerializeField] float holdDistance = 1.05f;
    [SerializeField] float holdYOffset = 0.02f;
    [SerializeField] LayerMask raycastMask = Physics.DefaultRaycastLayers;

    StoreFirstPersonController _fpc;
    Camera _cam;
    StoreShoppingCart _held;
    Collider _playerCollider;

    void Awake()
    {
        _fpc = GetComponent<StoreFirstPersonController>();
        _playerCollider = GetComponent<Collider>();
    }

    void Start()
    {
        if (IsStoreShoppingScene())
            StoreShoppingCartSpawner.EnsureAtLeastOneCartInScene(transform);
    }

    bool IsStoreShoppingScene()
    {
        string n = gameObject.scene.name;
        return n == SupermarketScene || n == GroceryScene;
    }

    void LateUpdate()
    {
        if (_fpc == null || !_fpc.IsControlEnabled)
            return;

        if (_held != null)
            UpdateHeldCartPose();
    }

    void Update()
    {
        if (_fpc == null || !_fpc.IsControlEnabled)
            return;

        if (ResolveCamera() == null)
            return;

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        if (mouse.rightButton.wasPressedThisFrame)
        {
            ReleaseCart();
            return;
        }

        if (!mouse.leftButton.wasPressedThisFrame)
            return;

        if (_held != null)
            return;

        if (TryRaycastCart(out StoreShoppingCart cart))
            GrabCart(cart);
    }

    Camera ResolveCamera()
    {
        if (_cam != null && _cam.isActiveAndEnabled)
            return _cam;
        if (_fpc != null && _fpc.cameraPivot != null)
            _cam = _fpc.cameraPivot.GetComponentInChildren<Camera>(true);
        if (_cam == null)
            _cam = Camera.main;
        return _cam;
    }

    bool TryRaycastCart(out StoreShoppingCart cart)
    {
        cart = null;
        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, raycastDistance, raycastMask, QueryTriggerInteraction.Ignore))
            return false;

        cart = hit.collider.GetComponentInParent<StoreShoppingCart>();
        return cart != null;
    }

    void GrabCart(StoreShoppingCart cart)
    {
        if (cart == null)
            return;
        _held = cart;
        _held.SetHeld(true, _playerCollider);
    }

    void ReleaseCart()
    {
        if (_held == null)
            return;
        _held.SetHeld(false, _playerCollider);
        _held = null;
    }

    void UpdateHeldCartPose()
    {
        Transform body = _fpc.transform;
        Vector3 flatForward = body.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 1e-4f)
            flatForward = Vector3.forward;
        flatForward.Normalize();

        Vector3 target = body.position + flatForward * holdDistance;
        target.y = body.position.y + holdYOffset;

        Quaternion rot = Quaternion.LookRotation(flatForward, Vector3.up);
        rot = _held.ApplyYawOffset(rot);

        _held.SetHeldTargetPose(
            target,
            rot);
    }
}
