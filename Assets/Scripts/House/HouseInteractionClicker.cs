using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class HouseInteractionClicker : MonoBehaviour
{
    [SerializeField] float interactRange = 4f;
    [SerializeField] LayerMask raycastMask = ~0;

    StoreFirstPersonController _fpc;
    Camera _cam;
    static readonly RaycastHit[] Hits = new RaycastHit[24];

    void Awake()
    {
        _fpc = GetComponent<StoreFirstPersonController>();
    }

    void Update()
    {
        if (_fpc != null && !_fpc.IsControlEnabled)
            return;

        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        Camera cam = ResolveCamera();
        if (cam == null)
            return;

        if (!RaycastInteractable(cam, out RaycastHit hit))
            return;

        // Check for chair sitting
        var interactable = hit.collider.GetComponentInParent<InteractableObject>();
        if (interactable != null && interactable.interactionType == InteractableObject.InteractionType.Sit)
        {
            var sittingManager = DeskSittingManager.Instance;
            if (sittingManager == null)
            {
                sittingManager = FindAnyObjectByType<DeskSittingManager>();
            }

            if (sittingManager != null)
            {
                sittingManager.ToggleSit(true);
                return;
            }
        }

        var door = hit.collider.GetComponentInParent<HouseDoorInteractable>();
        if (door != null && !door.IsBusy)
            door.Toggle();
    }

    bool RaycastInteractable(Camera cam, out RaycastHit chosen)
    {
        chosen = default;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int n = Physics.RaycastNonAlloc(ray, Hits, interactRange, raycastMask, QueryTriggerInteraction.Ignore);
        if (n == 0)
            return false;

        System.Array.Sort(Hits, 0, n, RaycastHitDistanceComparer.Instance);
        for (int i = 0; i < n; i++)
        {
            Collider c = Hits[i].collider;
            if (c == null)
                continue;
            if (c.transform == transform || c.transform.IsChildOf(transform))
                continue;
            if (c.GetComponentInParent<InteractableObject>() == null)
                return false;

            chosen = Hits[i];
            return true;
        }
        return false;
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
}
