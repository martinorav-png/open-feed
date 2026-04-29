using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SupermarketTaskController : MonoBehaviour
{
    public static SupermarketTaskController Instance { get; private set; }

    public enum Phase { Idle, IntroShown, Shopping, Checkout, AfterCheckout, FadingOut, Done }

    [Header("Wiring")]
    public Transform player;
    public Camera playerCamera;
    public Transform clerkRoot;
    public Transform clerkHeadAnchor;
    public ClerkHeadLook clerkHeadLook;
    public CashierGestureController clerkGestures;
    public BoxCollider entryTrigger;
    public BoxCollider exitTrigger;

    [Header("Settings")]
    public int requiredItems = 3;
    public float clerkInteractRange = 8f;
    public LayerMask clerkInteractMask = ~0;

    [Header("Dialogue style")]
    [SerializeField] float clerkFontSize = 0.55f;
    [SerializeField] Vector3 clerkTextOffset = new Vector3(0f, -0.45f, 0f);
    [SerializeField] Color clerkColor = new Color(0.43f, 0.80f, 0.43f, 1f);
    [SerializeField] Color playerColor = Color.white;

    [Header("Audio (optional)")]
    public AudioClip doorChimeClip;
    public AudioClip entryChimeClip;
    public AudioClip pickupClip;
    public AudioClip kachingClip;
    public MarketAmbience marketAmbience;
    public MarketAmbience marketMusic;

    [Header("Cart Slots (auto-built if empty)")]
    public List<Transform> cartSlots = new List<Transform>();

    [Header("Counter / Bag")]
    [Tooltip("Name of the counter top GameObject in the scene used to position items.")]
    public string counterName = "Object_6988";
    [Tooltip("How close (m) the cart has to be to the cashier before checkout starts.")]
    public float cartProximity = 4.5f;
    [Tooltip("How far in front of the cashier the items land on the counter.")]
    public float counterForwardOffset = 0.55f;
    [Tooltip("Spacing between items on the counter, in cashier-local x.")]
    public float counterItemSpacing = 0.32f;
    public float bagPickupRange = 3.5f;

    Phase _phase = Phase.Idle;
    int _itemsCollected;
    int _blockedTalkCount;
    bool _runningDialogue;
    bool _introPlayed;
    AudioSource _sfx;

    Transform _counter;
    readonly List<Transform> _itemsOnCounter = new List<Transform>();
    GameObject _hotDog;
    GameObject _paperBag;
    bool _bagHeld;

    public Phase CurrentPhase => _phase;
    public int ItemsCollected => _itemsCollected;
    public bool ReadyForCheckout => _itemsCollected >= requiredItems;

    void Awake()
    {
        Instance = this;
        _sfx = GetComponent<AudioSource>();
        if (_sfx == null) _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;
        _sfx.spatialBlend = 0f;
    }

    void Start()
    {
        if (player == null)
        {
            var fpc = FindAnyObjectByType<StoreFirstPersonController>();
            if (fpc != null) player = fpc.transform;
        }
        if (playerCamera == null)
        {
            var fpc = FindAnyObjectByType<StoreFirstPersonController>();
            if (fpc != null && fpc.cameraPivot != null)
                playerCamera = fpc.cameraPivot.GetComponentInChildren<Camera>(true);
            if (playerCamera == null) playerCamera = Camera.main;
        }
        if (clerkHeadLook != null && playerCamera != null)
            clerkHeadLook.target = playerCamera.transform;

        if (!string.IsNullOrEmpty(counterName))
        {
            var c = GameObject.Find(counterName);
            if (c != null) _counter = c.transform;
        }

        EnsureCartSlots();
    }

    public void EnsureCartSlots()
    {
        var cart = FindAnyObjectByType<StoreShoppingCart>();
        if (cart == null) return;
        var existing = cart.transform.Find("ItemSlots");
        Transform slotsRoot;
        if (existing == null)
        {
            var go = new GameObject("ItemSlots");
            go.transform.SetParent(cart.transform, false);
            slotsRoot = go.transform;
            CreateSlot(slotsRoot, "Slot_0", new Vector3(-0.12f, 0.78f, 0.05f));
            CreateSlot(slotsRoot, "Slot_1", new Vector3(0.12f, 0.78f, 0.05f));
            CreateSlot(slotsRoot, "Slot_2", new Vector3(0.00f, 0.78f, -0.10f));
        }
        else slotsRoot = existing;

        cartSlots.Clear();
        foreach (Transform t in slotsRoot) cartSlots.Add(t);
    }

    static Transform CreateSlot(Transform parent, string name, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        return go.transform;
    }

    public Transform GetNextFreeSlot()
    {
        EnsureCartSlots();
        foreach (var s in cartSlots)
            if (s != null && s.childCount == 0) return s;
        return null;
    }

    public void NotifyItemPicked(ShelfPickupItem item)
    {
        if (_phase == Phase.Done) return;
        var slot = GetNextFreeSlot();
        if (slot == null)
        {
            Debug.LogWarning("[Supermarket] No free cart slot — is the cart spawned and slots created?");
            return;
        }
        StartCoroutine(item.FlyToSlot(slot));
        _itemsCollected++;
        if (pickupClip != null) _sfx.PlayOneShot(pickupClip, 0.7f);
        if (_itemsCollected == requiredItems)
        {
            StartCoroutine(SupermarketSubtitleOverlay.Instance.RunLineCo("head to the cashier", 2.4f, 0.35f, Color.white));
        }
    }

    void Update()
    {
        if (_phase == Phase.Idle && entryTrigger != null && IsPlayerInsideBox(entryTrigger))
        {
            _phase = Phase.IntroShown;
            StartCoroutine(IntroSequence());
        }

        if ((_phase == Phase.IntroShown || _phase == Phase.Shopping) && !_runningDialogue)
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && playerCamera != null && clerkRoot != null)
            {
                if (RaycastForClerk()) StartCoroutine(InteractClerk());
            }
        }

        if (_phase == Phase.AfterCheckout && _paperBag != null && !_bagHeld)
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && playerCamera != null)
            {
                if (RaycastForBag()) PickUpBag();
            }
        }

        if (_phase == Phase.AfterCheckout && exitTrigger != null && IsPlayerInsideBox(exitTrigger))
        {
            _phase = Phase.FadingOut;
            StartCoroutine(LeavingSequence());
        }
    }

    static readonly RaycastHit[] _clerkHits = new RaycastHit[24];
    bool RaycastForClerk()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int n = Physics.RaycastNonAlloc(ray, _clerkHits, clerkInteractRange, clerkInteractMask, QueryTriggerInteraction.Ignore);
        if (n == 0) return false;
        System.Array.Sort(_clerkHits, 0, n, RaycastHitDistanceComparer.Instance);
        for (int i = 0; i < n; i++)
        {
            var c = _clerkHits[i].collider;
            if (c == null) continue;
            if (player != null && (c.transform == player || c.transform.IsChildOf(player))) continue;
            if (c.GetComponentInParent<StoreShoppingCart>() != null) continue;
            var t = c.transform;
            return (t == clerkRoot || t.IsChildOf(clerkRoot));
        }
        return false;
    }

    bool RaycastForBag()
    {
        if (_paperBag == null) return false;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int n = Physics.RaycastNonAlloc(ray, _clerkHits, bagPickupRange, clerkInteractMask, QueryTriggerInteraction.Ignore);
        if (n == 0) return false;
        System.Array.Sort(_clerkHits, 0, n, RaycastHitDistanceComparer.Instance);
        var bagT = _paperBag.transform;
        for (int i = 0; i < n; i++)
        {
            var c = _clerkHits[i].collider;
            if (c == null) continue;
            if (c.transform == bagT || c.transform.IsChildOf(bagT)) return true;
        }
        return false;
    }

    bool IsPlayerInsideBox(BoxCollider box)
    {
        if (box == null || player == null) return false;
        Vector3 local = box.transform.InverseTransformPoint(player.position) - box.center;
        Vector3 half = box.size * 0.5f;
        return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
    }

    IEnumerator IntroSequence()
    {
        if (_introPlayed) yield break;
        _introPlayed = true;
        if (entryChimeClip != null) _sfx.PlayOneShot(entryChimeClip, 0.22f);
        if (marketAmbience != null) marketAmbience.Play();
        if (marketMusic != null) marketMusic.Play();
        yield return new WaitForSeconds(0.35f);
        yield return SupermarketSubtitleOverlay.Instance.RunLineCo("take 3 items from the store and head to the cashier", 3.2f, 0.45f, Color.white);
        _phase = Phase.Shopping;
    }

    IEnumerator InteractClerk()
    {
        if (_runningDialogue) yield break;
        _runningDialogue = true;
        if (!ReadyForCheckout)
        {
            string line;
            string gestureKey;
            if (_blockedTalkCount == 0) { line = "good evening."; gestureKey = "good_evening"; }
            else if (_blockedTalkCount == 1) { line = "is that all?"; gestureKey = "is_that_all"; }
            else { line = "are you sure that's all?"; gestureKey = "are_you_sure"; }
            _blockedTalkCount++;
            PlayClerkGesture(gestureKey);
            yield return ClerkSay(line, 2f);
        }
        else if (!IsCartNearClerk())
        {
            PlayClerkGesture("are_you_sure");
            yield return ClerkSay("aren't you forgetting your cart?", 2.6f);
        }
        else
        {
            _phase = Phase.Checkout;
            yield return RunCheckoutDialogue();
        }
        _runningDialogue = false;
    }

    bool IsCartNearClerk()
    {
        if (clerkRoot == null) return true;
        var cart = FindAnyObjectByType<StoreShoppingCart>();
        if (cart == null) return true;
        Vector3 a = cart.transform.position; a.y = 0f;
        Vector3 b = clerkRoot.position; b.y = 0f;
        return Vector3.Distance(a, b) <= cartProximity;
    }

    void PlayClerkGesture(string key)
    {
        if (clerkGestures == null) return;
        if (string.IsNullOrEmpty(key)) clerkGestures.PlayRandom();
        else clerkGestures.PlayForLine(key);
    }

    IEnumerator ClerkSay(string text, float hold)
    {
        Transform anchor = clerkHeadAnchor != null ? clerkHeadAnchor : clerkRoot;
        if (anchor == null) yield break;
        yield return SupermarketFloatingText.Instance.Show(anchor, clerkTextOffset, text, hold, 0.45f, 0.18f, clerkFontSize, clerkColor);
    }

    IEnumerator PlayerSay(string text, float hold)
    {
        yield return SupermarketSubtitleOverlay.Instance.RunLineCo(text, hold, 0.35f, playerColor);
    }

    IEnumerator RunCheckoutDialogue()
    {
        var fpc = FindAnyObjectByType<StoreFirstPersonController>();
        if (fpc != null) fpc.SetControlEnabled(false);

        // Items hop from cart up onto the counter in front of the cashier.
        yield return JumpCartItemsToCounter();
        yield return new WaitForSeconds(0.3f);

        PlayClerkGesture("anything_else_1");
        yield return ClerkSay("anything else I can get you?", 2.2f);
        yield return PlayerSay("hot dog please", 1.8f);
        PlayClerkGesture("what_on_it");
        yield return ClerkSay("what would you like on it?", 2.2f);
        yield return PlayerSay("ketchup and mustard please", 2.2f);

        // Cashier "places" the hot dog on the counter.
        yield return PlaceHotDogOnCounter();

        PlayClerkGesture("anything_else_2");
        yield return ClerkSay("anything else?", 2.0f);
        yield return PlayerSay("that's everything", 1.8f);
        PlayClerkGesture("five_twenty");
        yield return ClerkSay("five twenty", 2.0f);
        if (kachingClip != null) _sfx.PlayOneShot(kachingClip, 0.7f);
        yield return new WaitForSeconds(0.4f);
        PlayClerkGesture("long_night");
        yield return ClerkSay("long night?", 2.2f);
        yield return PlayerSay("you could say that.", 2.2f);
        PlayClerkGesture("yeah");
        yield return ClerkSay("yeah.", 1.8f);
        yield return new WaitForSeconds(1.0f);
        PlayClerkGesture("good_evening");
        yield return ClerkSay("well, have a good one.", 2.2f);

        // Items + hot dog get bagged up.
        yield return PackItemsIntoBag();

        if (fpc != null) fpc.SetControlEnabled(true);
        _phase = Phase.AfterCheckout;
        yield return new WaitForSeconds(0.3f);
        yield return SupermarketSubtitleOverlay.Instance.RunLineCo("grab your bag and head out.", 2.6f, 0.45f, Color.white);
    }

    // ---------------- Counter helpers ----------------

    Vector3 GetCounterTopFor(int index, int total)
    {
        Vector3 origin = clerkRoot != null ? clerkRoot.position : transform.position;
        Vector3 forward = clerkRoot != null ? clerkRoot.forward : Vector3.forward;
        Vector3 right = clerkRoot != null ? clerkRoot.right : Vector3.right;

        float counterTopY = origin.y + 1.0f;
        if (_counter != null)
        {
            var rends = _counter.GetComponentsInChildren<Renderer>();
            if (rends != null && rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                counterTopY = b.max.y + 0.02f;
            }
        }

        float spread = (total - 1) * counterItemSpacing;
        float x = -spread * 0.5f + index * counterItemSpacing;
        Vector3 pos = origin + forward * counterForwardOffset + right * x;
        pos.y = counterTopY;
        return pos;
    }

    IEnumerator JumpCartItemsToCounter()
    {
        var cart = FindAnyObjectByType<StoreShoppingCart>();
        if (cart == null) yield break;
        var slotsRoot = cart.transform.Find("ItemSlots");
        if (slotsRoot == null) yield break;

        var items = new List<Transform>();
        for (int i = 0; i < slotsRoot.childCount; i++)
        {
            var slot = slotsRoot.GetChild(i);
            for (int j = 0; j < slot.childCount; j++) items.Add(slot.GetChild(j));
        }
        if (items.Count == 0) yield break;

        int total = items.Count;
        var routines = new List<Coroutine>();
        for (int i = 0; i < total; i++)
        {
            var item = items[i];
            // Detach from cart so it doesn't move with the cart anymore.
            item.SetParent(null, true);
            foreach (var c in item.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            var rb = item.GetComponentInChildren<Rigidbody>();
            if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
            Vector3 dst = GetCounterTopFor(i, total);
            routines.Add(StartCoroutine(ArcMove(item, item.position, dst, 0.7f, 0.55f, i * 0.12f)));
            _itemsOnCounter.Add(item);
        }
        // Wait for the longest one (last to start).
        foreach (var r in routines) yield return r;
    }

    IEnumerator ArcMove(Transform t, Vector3 from, Vector3 to, float dur, float arcHeight, float startDelay)
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);
        Quaternion startRot = t.rotation;
        Quaternion endRot = Quaternion.LookRotation(clerkRoot != null ? -clerkRoot.forward : Vector3.forward, Vector3.up);
        float k = 0f;
        while (k < 1f)
        {
            k += Time.deltaTime / Mathf.Max(0.01f, dur);
            float kk = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k));
            Vector3 p = Vector3.Lerp(from, to, kk);
            p.y += Mathf.Sin(kk * Mathf.PI) * arcHeight;
            t.position = p;
            t.rotation = Quaternion.Slerp(startRot, endRot, kk);
            yield return null;
        }
        t.position = to;
        t.rotation = endRot;
    }

    // ---------------- Hot dog ----------------

    GameObject BuildHotDog()
    {
        var root = new GameObject("HotDog_Placeholder");

        // Bun (yellow capsule)
        var bun = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bun.name = "Bun";
        bun.transform.SetParent(root.transform, false);
        bun.transform.localScale = new Vector3(0.07f, 0.09f, 0.07f);
        bun.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        SetPrimitiveColor(bun, new Color(0.95f, 0.78f, 0.32f));
        DestroyColliderOn(bun);

        // Sausage (red capsule, slightly thinner & longer than the bun core)
        var sausage = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        sausage.name = "Sausage";
        sausage.transform.SetParent(root.transform, false);
        sausage.transform.localScale = new Vector3(0.045f, 0.10f, 0.045f);
        sausage.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        sausage.transform.localPosition = new Vector3(0f, 0.022f, 0f);
        SetPrimitiveColor(sausage, new Color(0.78f, 0.18f, 0.14f));
        DestroyColliderOn(sausage);

        return root;
    }

    static void SetPrimitiveColor(GameObject go, Color c)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = c;
        r.sharedMaterial = mat;
    }

    static void DestroyColliderOn(GameObject go)
    {
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
    }

    IEnumerator PlaceHotDogOnCounter()
    {
        if (_hotDog == null) _hotDog = BuildHotDog();

        // Start in the cashier's hand area, then hop to the counter next to other items.
        Vector3 hand = clerkRoot != null
            ? clerkRoot.position + clerkRoot.forward * 0.25f + Vector3.up * 1.15f
            : transform.position + Vector3.up * 1.15f;
        _hotDog.transform.position = hand;
        _hotDog.transform.rotation = Quaternion.LookRotation(clerkRoot != null ? -clerkRoot.forward : Vector3.forward, Vector3.up);

        Vector3 dst = GetCounterTopFor(_itemsOnCounter.Count, _itemsOnCounter.Count + 1);
        yield return ArcMove(_hotDog.transform, hand, dst, 0.6f, 0.25f, 0f);
        _itemsOnCounter.Add(_hotDog.transform);
    }

    // ---------------- Paper bag ----------------

    GameObject BuildPaperBag()
    {
        var root = new GameObject("PaperBag");
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = new Vector3(0.32f, 0.42f, 0.24f);
        SetPrimitiveColor(body, new Color(0.78f, 0.62f, 0.42f));

        // Replace the default box collider on the cube with one on the root for clean raycasting.
        var prim = body.GetComponent<BoxCollider>();
        if (prim != null) Destroy(prim);
        var bc = root.AddComponent<BoxCollider>();
        bc.size = new Vector3(0.34f, 0.44f, 0.26f);

        return root;
    }

    IEnumerator PackItemsIntoBag()
    {
        if (_paperBag == null) _paperBag = BuildPaperBag();
        // Place the bag at the end of the line of items on the counter.
        int total = _itemsOnCounter.Count + 1;
        Vector3 bagSlot = GetCounterTopFor(total - 1, total);
        // Lift the bag's pivot a bit so it sits on the counter rather than half through it.
        Vector3 bagPos = bagSlot + Vector3.up * 0.21f;
        _paperBag.transform.position = bagPos;
        _paperBag.transform.rotation = Quaternion.LookRotation(clerkRoot != null ? -clerkRoot.forward : Vector3.forward, Vector3.up);

        yield return new WaitForSeconds(0.5f);

        // Each item flies into the bag and shrinks out of sight.
        var coros = new List<Coroutine>();
        for (int i = 0; i < _itemsOnCounter.Count; i++)
        {
            var it = _itemsOnCounter[i];
            if (it == null) continue;
            coros.Add(StartCoroutine(SuckIntoBag(it, bagPos, 0.55f, i * 0.12f)));
        }
        foreach (var c in coros) yield return c;

        // Items are inside the bag now — hide them.
        foreach (var it in _itemsOnCounter)
        {
            if (it != null) it.gameObject.SetActive(false);
        }
    }

    IEnumerator SuckIntoBag(Transform t, Vector3 bagPos, float dur, float startDelay)
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);
        Vector3 from = t.position;
        Vector3 fromScale = t.localScale;
        float k = 0f;
        while (k < 1f)
        {
            k += Time.deltaTime / Mathf.Max(0.01f, dur);
            float kk = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k));
            t.position = Vector3.Lerp(from, bagPos, kk);
            t.localScale = Vector3.Lerp(fromScale, fromScale * 0.05f, kk);
            yield return null;
        }
    }

    void PickUpBag()
    {
        if (_paperBag == null || playerCamera == null) return;
        _bagHeld = true;
        // Disable collider so it doesn't push the player around.
        var bc = _paperBag.GetComponent<Collider>();
        if (bc != null) bc.enabled = false;
        // Parent to the camera, held in front and slightly down.
        _paperBag.transform.SetParent(playerCamera.transform, false);
        _paperBag.transform.localPosition = new Vector3(0.22f, -0.28f, 0.55f);
        _paperBag.transform.localRotation = Quaternion.Euler(15f, -15f, 5f);
    }

    IEnumerator LeavingSequence()
    {
        if (doorChimeClip != null) _sfx.PlayOneShot(doorChimeClip, 0.6f);
        var fpc = FindAnyObjectByType<StoreFirstPersonController>();
        if (fpc != null) fpc.SetControlEnabled(false);

        yield return SupermarketSubtitleOverlay.Instance.FadeToBlackCo(1.0f);
        yield return new WaitForSeconds(0.4f);
        yield return SupermarketSubtitleOverlay.Instance.ShowLineOverBlackCo("you'll drive for a bit longer.", 2.4f);
        yield return new WaitForSeconds(0.3f);
        yield return SupermarketSubtitleOverlay.Instance.ShowLineOverBlackCo("you don't have anywhere to be tomorrow anyways.", 2.6f);
        yield return new WaitForSeconds(0.6f);

        SupermarketSubtitleOverlay.DestroyIfExists();

        _phase = Phase.Done;
        SceneManager.LoadScene("ForestDrive");
    }
}
