using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SupermarketSetupMenu
{
    [MenuItem("Open Feed/Supermarket/Tag Selection As Shelf Pickups")]
    public static void TagSelectionAsShelfPickups()
    {
        var sel = Selection.gameObjects;
        if (sel == null || sel.Length == 0)
        {
            Debug.LogWarning("[Supermarket] No selection. Select shelf items in the Hierarchy first.");
            return;
        }

        int tagged = 0, addedColliders = 0, skipped = 0;
        foreach (var go in sel)
        {
            if (go == null) continue;
            if (go.GetComponent<ShelfPickupItem>() == null)
                Undo.AddComponent<ShelfPickupItem>(go);
            // Make sure raycasts hit something — give it a collider that wraps its renderers.
            if (!HasInteractableCollider(go))
            {
                var mc = Undo.AddComponent<MeshCollider>(go);
                var mf = go.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = false;
                    addedColliders++;
                }
                else
                {
                    // No mesh on this transform — fall back to a box around child renderers.
                    Object.DestroyImmediate(mc);
                    var box = Undo.AddComponent<BoxCollider>(go);
                    var b = ComputeLocalBounds(go.transform);
                    box.center = b.center;
                    box.size = Vector3.Max(b.size, new Vector3(0.05f, 0.05f, 0.05f));
                    addedColliders++;
                }
            }
            tagged++;
        }

        if (sel.Length > 0 && !Application.isPlaying)
            EditorSceneManager.MarkSceneDirty(sel[0].scene);

        Debug.Log($"[Supermarket] Tagged {tagged} shelf items as ShelfPickupItem (added {addedColliders} colliders, skipped {skipped}).");
    }

    static bool HasInteractableCollider(GameObject go)
    {
        var c = go.GetComponent<Collider>();
        return c != null && c.enabled;
    }

    static Bounds ComputeLocalBounds(Transform root)
    {
        var rends = root.GetComponentsInChildren<Renderer>(true);
        bool init = false;
        Bounds acc = default;
        var w2l = root.worldToLocalMatrix;
        foreach (var r in rends)
        {
            if (r == null) continue;
            var lb = r.localBounds;
            var c = lb.center;
            var e = lb.extents;
            if (e.sqrMagnitude < 1e-12f) continue;
            var rendererToRoot = w2l * r.localToWorldMatrix;
            for (int xi = -1; xi <= 1; xi += 2)
            for (int yi = -1; yi <= 1; yi += 2)
            for (int zi = -1; zi <= 1; zi += 2)
            {
                Vector3 corner = c + new Vector3(e.x * xi, e.y * yi, e.z * zi);
                Vector3 p = rendererToRoot.MultiplyPoint3x4(corner);
                if (!init) { acc = new Bounds(p, Vector3.zero); init = true; }
                else acc.Encapsulate(p);
            }
        }
        if (!init) return new Bounds(Vector3.zero, Vector3.one * 0.2f);
        return acc;
    }
}
