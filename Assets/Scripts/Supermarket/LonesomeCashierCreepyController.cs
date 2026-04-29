using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(80)]
public class LonesomeCashierCreepyController : MonoBehaviour
{
    [Header("Look Target")]
    public Transform target;
    public Transform tableSurface;
    public string tableSurfaceName = "Object_6988";

    [Header("Static Mesh Pose")]
    public bool alignToTableOnStart = true;
    public Vector3 tableRestOffset = new Vector3(0.26f, 0.16f, 0.92f);

    [Header("Creepy Motion")]
    [Range(0f, 35f)] public float maxYawDegrees = 12f;
    [Range(0f, 15f)] public float leanDegrees = 3f;
    public float turnSpeed = 2.2f;
    public float breathingDegrees = 0.8f;
    public float breathingSpeed = 0.65f;
    public float twitchDegrees = 1.4f;
    public float twitchSpeed = 0.18f;

    Vector3 _basePosition;
    Vector3 _baseEuler;
    float _nextTwitchTime;
    float _twitch;

    void Awake()
    {
        ResolveReferences();
        if (alignToTableOnStart)
            AlignToTable();

        _basePosition = transform.position;
        _baseEuler = transform.eulerAngles;
        _nextTwitchTime = Time.time + Random.Range(1.5f, 4.5f);
    }

    void LateUpdate()
    {
        ResolveReferences();

        float yaw = 0f;
        if (target != null)
        {
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.001f)
            {
                float targetYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
                float baseYaw = _baseEuler.y;
                yaw = Mathf.DeltaAngle(baseYaw, targetYaw);
                yaw = Mathf.Clamp(yaw, -maxYawDegrees, maxYawDegrees);
            }
        }

        if (Time.time >= _nextTwitchTime)
        {
            _twitch = Random.Range(-twitchDegrees, twitchDegrees);
            _nextTwitchTime = Time.time + Random.Range(2.0f, 6.0f);
        }
        _twitch = Mathf.Lerp(_twitch, 0f, 1f - Mathf.Exp(-twitchSpeed * Time.deltaTime));

        float breath = Mathf.Sin(Time.time * breathingSpeed) * breathingDegrees;
        float lean = target != null ? leanDegrees : 0f;
        Vector3 targetEuler = _baseEuler + new Vector3(-lean + breath, yaw + _twitch, 0f);

        transform.position = _basePosition + Vector3.up * (Mathf.Sin(Time.time * breathingSpeed * 0.77f) * 0.008f);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(targetEuler), 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
    }

    void ResolveReferences()
    {
        if (target == null)
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

        if (tableSurface == null && !string.IsNullOrWhiteSpace(tableSurfaceName))
        {
            GameObject table = GameObject.Find(tableSurfaceName);
            if (table != null)
                tableSurface = table.transform;
        }
    }

    void AlignToTable()
    {
        if (tableSurface == null)
            return;

        Renderer tableRenderer = tableSurface.GetComponent<Renderer>();
        if (tableRenderer == null)
            return;

        Bounds tableBounds = tableRenderer.bounds;
        transform.position = new Vector3(
            tableBounds.center.x + tableRestOffset.x,
            tableBounds.max.y + tableRestOffset.y,
            tableBounds.center.z + tableRestOffset.z);
    }
}
