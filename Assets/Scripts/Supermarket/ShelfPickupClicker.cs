using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class ShelfPickupClicker : MonoBehaviour
{
    [SerializeField] float maxRange = 8f;
    [SerializeField] LayerMask mask = ~0;
    [SerializeField] bool debugLogs = false;

    StoreFirstPersonController _fpc;
    Camera _cam;
    static readonly RaycastHit[] _hits = new RaycastHit[24];

    void Awake() { _fpc = GetComponent<StoreFirstPersonController>(); }

    Camera ResolveCamera()
    {
        if (_cam != null && _cam.isActiveAndEnabled) return _cam;
        if (_fpc != null && _fpc.cameraPivot != null)
            _cam = _fpc.cameraPivot.GetComponentInChildren<Camera>(true);
        if (_cam == null) _cam = Camera.main;
        return _cam;
    }

    void Update()
    {
        if (_fpc != null && !_fpc.IsControlEnabled) return;
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        var ctrl = SupermarketTaskController.Instance;
        if (ctrl == null) return;

        var cam = ResolveCamera();
        if (cam == null) return;

        if (!RaycastSkippingCart(cam, out RaycastHit hit))
        {
            if (debugLogs) Debug.Log("[ShelfPickupClicker] no hit (after skipping cart) within " + maxRange + "m");
            return;
        }

        if (debugLogs) Debug.Log("[ShelfPickupClicker] hit " + hit.collider.name + " @ " + hit.distance.ToString("F2") + "m");

        var item = hit.collider.GetComponentInParent<ShelfPickupItem>();
        if (item == null || item.picked) return;

        ctrl.NotifyItemPicked(item);
        if (debugLogs) Debug.Log("[ShelfPickupClicker] picked " + item.name);
    }

    bool RaycastSkippingCart(Camera cam, out RaycastHit chosen)
    {
        chosen = default;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int n = Physics.RaycastNonAlloc(ray, _hits, maxRange, mask, QueryTriggerInteraction.Ignore);
        if (n == 0) return false;

        // Sort by distance ascending and pick first non-cart, non-self hit.
        System.Array.Sort(_hits, 0, n, RaycastHitDistanceComparer.Instance);
        for (int i = 0; i < n; i++)
        {
            var c = _hits[i].collider;
            if (c == null) continue;
            // skip our own collider
            if (c.transform == transform || c.transform.IsChildOf(transform)) continue;
            // skip the shopping cart
            if (c.GetComponentInParent<StoreShoppingCart>() != null) continue;
            chosen = _hits[i];
            return true;
        }
        return false;
    }
}

class RaycastHitDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
{
    public static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();
    public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
}
