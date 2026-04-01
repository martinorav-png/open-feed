using UnityEngine;

/// <summary>
/// Shared ray logic so the crosshair can highlight when the reticle is over anything the player can use.
/// </summary>
public static class InteractableHoverQuery
{
    public static bool IsCrosshairOverInteractable(Camera cam, float maxDistance)
    {
        if (cam == null)
            return false;

        PhoneInteraction phone = Object.FindAnyObjectByType<PhoneInteraction>();
        if (phone != null && phone.IsActive())
            return false;

        MonitorInteraction monitor = Object.FindAnyObjectByType<MonitorInteraction>();
        if (monitor != null && monitor.IsZoomed())
            return false;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);
        if (hits == null || hits.Length == 0)
            return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Transform t = hits[i].collider.transform;
            if (HasInspectableInParents(t))
                return true;
            if (FindInteractableInParents(t) != null)
                return true;
            if (SpeakerStillAudio.IsSpeakerTransform(t))
                return true;
        }

        if (monitor != null && monitor.HitsAllowMonitorZoom(hits))
            return true;

        return false;
    }

    static bool HasInspectableInParents(Transform current)
    {
        while (current != null)
        {
            if (current.CompareTag("Inspectable"))
                return true;
            current = current.parent;
        }

        return false;
    }

    static InteractableObject FindInteractableInParents(Transform t)
    {
        Transform current = t;
        while (current != null)
        {
            InteractableObject obj = current.GetComponent<InteractableObject>();
            if (obj != null)
                return obj;
            current = current.parent;
        }

        return null;
    }
}
