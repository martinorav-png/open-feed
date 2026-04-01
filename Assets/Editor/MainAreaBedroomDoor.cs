#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Splits <see cref="WallSourceName"/> around a bedroom door opening. Does not modify NearWall.
/// Run once with MainArea open. Creates FarWall_LeftOfDoor / Right / Above + BedroomDoor group.
/// </summary>
public static class MainAreaBedroomDoor
{
    const string WallSourceName = "FarWall";

    const string MenuPath = "OPEN FEED/Scene/Add Bedroom Door (Far Wall)";

    [MenuItem(MenuPath)]
    public static void Apply()
    {
        if (!ApplyInternal())
            return;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("OPENFEED: Bedroom door on FarWall applied. Save the scene (Ctrl+S).");
    }

    public static bool ApplyInternal()
    {
        GameObject wall = FindInLoadedScenes(WallSourceName);
        if (wall == null)
        {
            if (FindInLoadedScenes("FarWall_LeftOfDoor") != null)
            {
                Debug.LogWarning("MainAreaBedroomDoor: FarWall already split — nothing to do.");
                return false;
            }

            Debug.LogError($"MainAreaBedroomDoor: No unsplit wall named '{WallSourceName}'.");
            return false;
        }

        Transform room = wall.transform.parent;
        if (room == null)
        {
            Debug.LogError("MainAreaBedroomDoor: Wall has no parent.");
            return false;
        }

        Material wallMat = wall.GetComponent<MeshRenderer>()?.sharedMaterial;
        Material frameMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Desk/Desk_WindowFrame.mat");
        Material doorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Desk/Desk_DeskFrame.mat");
        if (wallMat == null || frameMat == null || doorMat == null)
        {
            Debug.LogError("MainAreaBedroomDoor: Missing wall material or Desk_WindowFrame / Desk_DeskFrame.");
            return false;
        }

        Transform wt = wall.transform;
        Vector3 s = wt.localScale;
        float wallHalfX = s.x * 0.5f;
        float wallHalfY = s.y * 0.5f;
        float wallThick = Mathf.Min(s.x, s.y, s.z);
        float wallCenterX = wt.localPosition.x;
        float wallCenterY = wt.localPosition.y;
        float wallCenterZ = wt.localPosition.z;

        const float doorHalfX = 0.55f;
        const float doorTopY = 2.05f;
        const float doorBottomY = -0.1f;

        float wallMinX = wallCenterX - wallHalfX;
        float wallMaxX = wallCenterX + wallHalfX;
        float doorMinX = wallCenterX - doorHalfX;
        float doorMaxX = wallCenterX + doorHalfX;

        float leftSegHalfX = (doorMinX - wallMinX) * 0.5f;
        float leftCenterX = wallMinX + leftSegHalfX;

        MakeWallSegment(room, "FarWall_LeftOfDoor", new Vector3(leftCenterX, wallCenterY, wallCenterZ), new Vector3(doorMinX - wallMinX, wallHalfY * 2f, wallThick), wallMat);

        float rightSegHalfX = (wallMaxX - doorMaxX) * 0.5f;
        float rightCenterX = doorMaxX + rightSegHalfX;
        MakeWallSegment(room, "FarWall_RightOfDoor", new Vector3(rightCenterX, wallCenterY, wallCenterZ), new Vector3(wallMaxX - doorMaxX, wallHalfY * 2f, wallThick), wallMat);

        float wallTopY = wallCenterY + wallHalfY;
        float lintelCenterY = (doorTopY + wallTopY) * 0.5f;
        float lintelHeight = wallTopY - doorTopY;
        MakeWallSegment(room, "FarWall_AboveDoor", new Vector3(wallCenterX, lintelCenterY, wallCenterZ), new Vector3(doorHalfX * 2f, lintelHeight, wallThick), wallMat);

        const float roomApproxMidZ = 4.75f;
        float zInset = wallCenterZ > roomApproxMidZ ? -0.02f : 0.02f;
        float panelLocalZ = wallCenterZ > roomApproxMidZ ? -0.08f : 0.08f;

        float openingCenterY = (doorBottomY + doorTopY) * 0.5f;
        GameObject doorRoot = new GameObject("BedroomDoor");
        Undo.RegisterCreatedObjectUndo(doorRoot, "BedroomDoor");
        doorRoot.transform.SetParent(room, false);
        doorRoot.transform.localPosition = new Vector3(wallCenterX, openingCenterY, wallCenterZ + zInset);
        doorRoot.transform.localRotation = Quaternion.identity;
        doorRoot.transform.localScale = Vector3.one;

        float halfOpenY = (doorTopY - doorBottomY) * 0.5f;
        const float mould = 0.06f;

        MakeFrameBar(doorRoot.transform, "BedroomDoor_FrameTop", new Vector3(0f, halfOpenY + mould * 0.67f, 0f), new Vector3(1.2f, 0.08f, 0.14f), frameMat);
        MakeFrameBar(doorRoot.transform, "BedroomDoor_FrameLeft", new Vector3(-(doorHalfX - mould), 0f, 0f), new Vector3(0.1f, halfOpenY * 2f + 0.12f, 0.14f), frameMat);
        MakeFrameBar(doorRoot.transform, "BedroomDoor_FrameRight", new Vector3(doorHalfX - mould, 0f, 0f), new Vector3(0.1f, halfOpenY * 2f + 0.12f, 0.14f), frameMat);

        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "BedroomDoor_Panel";
        Undo.RegisterCreatedObjectUndo(panel, "BedroomDoor_Panel");
        panel.transform.SetParent(doorRoot.transform, false);
        panel.transform.localPosition = new Vector3(0f, 0f, panelLocalZ);
        panel.transform.localRotation = Quaternion.identity;
        panel.transform.localScale = new Vector3(doorHalfX * 2f - 0.08f, halfOpenY * 2f - 0.06f, 0.07f);
        Object.DestroyImmediate(panel.GetComponent<Collider>());
        panel.GetComponent<MeshRenderer>().sharedMaterial = doorMat;

        Undo.DestroyObjectImmediate(wall);

        return true;
    }

    static GameObject FindInLoadedScenes(string objectName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded)
                continue;
            foreach (GameObject root in s.GetRootGameObjects())
            {
                Transform t = FindChildRecursive(root.transform, objectName);
                if (t != null)
                    return t.gameObject;
            }
        }

        return null;
    }

    static Transform FindChildRecursive(Transform parent, string objectName)
    {
        if (parent.name == objectName)
            return parent;
        for (int c = 0; c < parent.childCount; c++)
        {
            Transform r = FindChildRecursive(parent.GetChild(c), objectName);
            if (r != null)
                return r;
        }

        return null;
    }

    static void MakeWallSegment(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Undo.RegisterCreatedObjectUndo(go, name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = localScale;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        var box = go.AddComponent<BoxCollider>();
        box.size = Vector3.one;
        box.center = Vector3.zero;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static void MakeFrameBar(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Undo.RegisterCreatedObjectUndo(go, name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = localScale;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }
}
#endif
