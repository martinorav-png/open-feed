#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Adds non-convex <see cref="MeshCollider"/>s to every <see cref="MeshFilter"/> under the
/// supermarket FBX that does not already have a collider (shelves, freezers, carts, checkout, etc.).
/// </summary>
public static class SupermarketInteriorColliders
{
    public const string SupermarketFbxGuid = "91ba6f803321fc047add62af96400af1";

    /// <returns>Number of MeshColliders added or updated.</returns>
    public static int AddMissingMeshColliders(Transform root, bool refreshExistingMeshColliders)
    {
        if (root == null)
            return 0;

        int count = 0;
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter mf in filters)
        {
            if (mf.sharedMesh == null)
                continue;

            GameObject go = mf.gameObject;
            MeshCollider mc = go.GetComponent<MeshCollider>();
            if (mc == null)
            {
                mc = go.AddComponent<MeshCollider>();
                count++;
            }
            else if (!refreshExistingMeshColliders)
                continue;
            else
                count++;

            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false;
        }

        return count;
    }

    static string SupermarketFbxAssetPath =>
        AssetDatabase.GUIDToAssetPath(SupermarketFbxGuid);

    /// <summary>Outermost prefab instance root in the open scene that uses scene.fbx.</summary>
    public static GameObject FindSupermarketInstanceInActiveScene()
    {
        string path = SupermarketFbxAssetPath;
        if (string.IsNullOrEmpty(path))
            return null;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return null;

        foreach (GameObject go in scene.GetRootGameObjects())
        {
            GameObject outer = FindInstanceUnder(go.transform, path);
            if (outer != null)
                return outer;
        }

        return null;
    }

    static GameObject FindInstanceUnder(Transform t, string prefabAssetPath)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(t.gameObject))
        {
            string rootPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
            if (rootPath == prefabAssetPath)
                return PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject);
        }

        for (int i = 0; i < t.childCount; i++)
        {
            GameObject found = FindInstanceUnder(t.GetChild(i), prefabAssetPath);
            if (found != null)
                return found;
        }

        return null;
    }

    [MenuItem("OPEN FEED/Supermarket/Add missing interior mesh colliders", false, 18)]
    static void MenuAddCollidersToOpenScene()
    {
        GameObject root = FindSupermarketInstanceInActiveScene();
        if (root == null)
        {
            EditorUtility.DisplayDialog(
                "Supermarket colliders",
                "No instance of scene.fbx (supermarket interior) found in the active scene.\n" +
                "Open supermarket.unity or regenerate the supermarket FBX first.",
                "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(root, "Add supermarket mesh colliders");
        int n = AddMissingMeshColliders(root.transform, refreshExistingMeshColliders: true);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log($"[Supermarket] Mesh colliders on interior: added/updated {n} (root: {root.name}). Save the scene.");
    }
}
#endif
