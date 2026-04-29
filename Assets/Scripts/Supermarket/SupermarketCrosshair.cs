using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SupermarketCrosshair : MonoBehaviour
{
    [SerializeField] float interactRange = 8f;
    [SerializeField] LayerMask raycastMask = ~0;
    [SerializeField] float dotSize = 5f;
    [SerializeField] Color idleColor = new Color(1f, 1f, 1f, 0.65f);
    [SerializeField] Color hoverColor = new Color(0.4f, 0.95f, 0.5f, 0.95f);

    StoreFirstPersonController _fpc;
    Camera _cam;
    Image _dot;
    Canvas _canvas;
    static readonly RaycastHit[] _hits = new RaycastHit[16];

    void Awake()
    {
        _fpc = GetComponent<StoreFirstPersonController>();
        BuildUI();
    }

    void BuildUI()
    {
        var go = new GameObject("SupermarketCrosshairCanvas");
        go.transform.SetParent(transform, false);
        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 750;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var dot = new GameObject("Dot");
        dot.transform.SetParent(go.transform, false);
        _dot = dot.AddComponent<Image>();
        _dot.color = idleColor;
        _dot.raycastTarget = false;
        var rt = _dot.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(dotSize, dotSize);
        rt.anchoredPosition = Vector2.zero;
    }

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
        if (_dot == null) return;

        var cam = ResolveCamera();
        bool hover = cam != null && IsHover(cam);
        _dot.color = hover ? hoverColor : idleColor;
    }

    bool IsHover(Camera cam)
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int n = Physics.RaycastNonAlloc(ray, _hits, interactRange, raycastMask, QueryTriggerInteraction.Ignore);
        if (n == 0) return false;

        // Sort and walk hits in distance order; the cart is allowed to register
        // hover (so the player gets feedback that they can grab/release it),
        // but we keep walking through cart hits when looking for shelf/clerk.
        System.Array.Sort(_hits, 0, n, RaycastHitDistanceComparer.Instance);

        var ctrl = SupermarketTaskController.Instance;
        // First pass: closest non-cart, non-self hit determines hover for shelf/clerk.
        for (int i = 0; i < n; i++)
        {
            var c = _hits[i].collider;
            if (c == null) continue;
            if (c.transform == transform || c.transform.IsChildOf(transform)) continue;
            if (c.GetComponentInParent<StoreShoppingCart>() != null) continue;

            var t = c.transform;
            if (t.GetComponentInParent<ShelfPickupItem>() != null) return true;
            if (ctrl != null && ctrl.clerkRoot != null && (t == ctrl.clerkRoot || t.IsChildOf(ctrl.clerkRoot))) return true;
            // First solid non-cart hit wasn't interactable; ray is blocked.
            return false;
        }

        // Second pass: if every hit is the cart, hover the cart so the player can release it.
        for (int i = 0; i < n; i++)
        {
            var c = _hits[i].collider;
            if (c != null && c.GetComponentInParent<StoreShoppingCart>() != null) return true;
        }
        return false;
    }
}
