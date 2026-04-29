using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class ShelfPickupItem : MonoBehaviour
{
    public bool picked;

    [SerializeField] float flyDuration = 0.55f;
    [SerializeField] AnimationCurve flyArc = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] float arcHeight = 0.5f;

    public IEnumerator FlyToSlot(Transform slot)
    {
        if (picked || slot == null) yield break;
        picked = true;

        var rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
        foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = false;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, flyDuration);
            float k = flyArc.Evaluate(Mathf.Clamp01(t));
            Vector3 a = Vector3.Lerp(startPos, slot.position, k);
            a.y += Mathf.Sin(k * Mathf.PI) * arcHeight;
            transform.position = a;
            transform.rotation = Quaternion.Slerp(startRot, slot.rotation, k);
            yield return null;
        }

        transform.SetParent(slot, true);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
}
