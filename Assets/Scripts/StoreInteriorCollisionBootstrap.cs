using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Once per session, adds non-convex <see cref="MeshCollider"/>s to static imported meshes under the same
/// model root as <c>Object_6</c> (store floor), so shelves, walls, and checkout collide. Skips luminaire rigs
/// and shopping carts.
/// </summary>
[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
public class StoreInteriorCollisionBootstrap : MonoBehaviour
{
    void Awake()
    {
        string sceneName = gameObject.scene.name;
        if (sceneName != "supermarket" && sceneName != "GroceryStore")
            return;

        Transform interiorRoot = FindStoreMeshRoot();
        if (interiorRoot == null)
            return;

        int added = AddMissingMeshCollidersUnder(interiorRoot);
        if (added > 0)
            Debug.Log($"[StoreInteriorCollisionBootstrap] Added/updated {added} mesh colliders under '{interiorRoot.name}'.");
    }

    static Transform FindStoreMeshRoot()
    {
        Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null || t.name != "Object_6")
                continue;
            return t.root;
        }

        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null)
                continue;
            string n = t.name;
            if (n.Equals("glTF_SceneRootNode", System.StringComparison.OrdinalIgnoreCase))
                return t.root;
        }

        // Heuristic fallback: pick the root object with the most meshes.
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return null;
        GameObject[] roots = scene.GetRootGameObjects();
        Transform best = null;
        int bestCount = 0;
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject go = roots[i];
            if (go == null)
                continue;
            if (IsUnderExcludedSubtree(go.transform))
                continue;
            int c = go.GetComponentsInChildren<MeshFilter>(true).Length;
            if (c > bestCount)
            {
                bestCount = c;
                best = go.transform;
            }
        }

        return best;
    }

    static bool IsUnderExcludedSubtree(Transform t)
    {
        for (Transform p = t; p != null; p = p.parent)
        {
            string n = p.name;
            if (n.IndexOf("SupermarketLuminaireRig", System.StringComparison.Ordinal) >= 0)
                return true;
            if (n.IndexOf("PlayerRig", System.StringComparison.Ordinal) >= 0)
                return true;
            if (n.IndexOf("CarIntroRig", System.StringComparison.Ordinal) >= 0)
                return true;
            if (n.IndexOf("ParkedCar", System.StringComparison.Ordinal) >= 0)
                return true;
        }

        return false;
    }

    static int AddMissingMeshCollidersUnder(Transform root)
    {
        int count = 0;
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter mf = filters[i];
            if (mf == null || mf.sharedMesh == null)
                continue;
            GameObject go = mf.gameObject;
            if (go.GetComponentInParent<StoreShoppingCart>() != null)
                continue;
            if (IsUnderExcludedSubtree(go.transform))
                continue;

            MeshCollider mc = go.GetComponent<MeshCollider>();
            if (mc == null)
            {
                mc = go.AddComponent<MeshCollider>();
                count++;
            }
            else
                count++;

            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false;
        }

        return count;
    }
}
