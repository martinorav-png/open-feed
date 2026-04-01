using UnityEngine;

/// <summary>
/// Ensures the RBX_irobotdog_r1 model in a scene has an InteractableObject + collider for desk interaction.
/// </summary>
public static class IdogInteractableAutoWire
{
    const string DogObjectName = "RBX_irobotdog_r1";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void WireIdogIfPresent()
    {
        GameObject dog = GameObject.Find(DogObjectName);
        if (dog == null)
            return;

        if (dog.GetComponent<InteractableObject>() != null)
        {
            EnsureCollider(dog);
            return;
        }

        var io = dog.AddComponent<InteractableObject>();
        io.interactionType = InteractableObject.InteractionType.PlayNudge;
        io.objectName = "iDog";
        io.nudgeMoveMeters = 0.04f;
        io.nudgeDuration = 0.24f;
        io.nudgeSoundVolume = 0.7f;

        EnsureCollider(dog);
    }

    static void EnsureCollider(GameObject dog)
    {
        if (dog.GetComponentInChildren<Collider>(true) != null)
            return;

        Renderer[] rends = dog.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0)
        {
            var fallback = dog.AddComponent<BoxCollider>();
            fallback.size = new Vector3(0.45f, 0.35f, 0.55f);
            fallback.center = new Vector3(0f, 0.18f, 0f);
            return;
        }

        Bounds world = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            world.Encapsulate(rends[i].bounds);

        var box = dog.AddComponent<BoxCollider>();
        box.center = dog.transform.InverseTransformPoint(world.center);
        Vector3 lossy = dog.transform.lossyScale;
        float sx = Mathf.Max(0.001f, lossy.x);
        float sy = Mathf.Max(0.001f, lossy.y);
        float sz = Mathf.Max(0.001f, lossy.z);
        box.size = new Vector3(world.size.x / sx, world.size.y / sy, world.size.z / sz);
    }
}
