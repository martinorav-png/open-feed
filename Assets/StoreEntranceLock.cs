using UnityEngine;

/// <summary>
/// Blocks the convenience store entrance so the player cannot leave.
/// </summary>
public class StoreEntranceLock : MonoBehaviour
{
    [Header("Barrier Shape")]
    public Vector3 lockCenter = new Vector3(0f, 1.15f, 0f);
    public Vector3 lockSize = new Vector3(1.5f, 2.4f, 0.5f);
    public bool lockOnStart;

    BoxCollider lockCollider;

    public bool IsLocked => lockCollider != null && lockCollider.enabled;

    void Awake()
    {
        EnsureCollider();

        if (lockOnStart)
            LockEntrance();
        else
            UnlockEntrance();
    }

    public void ConfigureUsingDoors(Transform leftDoor, Transform rightDoor)
    {
        if (leftDoor == null || rightDoor == null)
            return;

        EnsureCollider();

        Vector3 worldMid = (leftDoor.position + rightDoor.position) * 0.5f;
        Vector3 localMid = transform.InverseTransformPoint(worldMid);

        float doorWidth = Vector3.Distance(leftDoor.position, rightDoor.position);
        lockCenter = new Vector3(localMid.x, 1.15f, localMid.z);
        lockSize = new Vector3(Mathf.Max(doorWidth + 1.1f, 1.5f), 2.4f, 0.5f);

        ApplyColliderShape();
    }

    public void LockEntrance()
    {
        EnsureCollider();
        lockCollider.enabled = true;
    }

    public void UnlockEntrance()
    {
        EnsureCollider();
        lockCollider.enabled = false;
    }

    void EnsureCollider()
    {
        if (lockCollider == null)
            lockCollider = GetComponent<BoxCollider>();

        if (lockCollider == null)
            lockCollider = gameObject.AddComponent<BoxCollider>();

        lockCollider.isTrigger = false;
        ApplyColliderShape();
    }

    void ApplyColliderShape()
    {
        if (lockCollider == null)
            return;

        lockCollider.center = lockCenter;
        lockCollider.size = lockSize;
    }
}
