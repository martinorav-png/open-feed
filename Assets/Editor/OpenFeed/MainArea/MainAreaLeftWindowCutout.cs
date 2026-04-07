#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Splits patterned LeftWall around the window opening and replaces solid WindowCutout_Frame with four thin frame bars.
/// Run once from menu with MainArea open.
/// </summary>
public static class MainAreaLeftWindowCutout
{
    const string MenuPath = "OPEN FEED/Main Area/Left Wall Window Cutout";

    [MenuItem(MenuPath, false, 10)]
    public static void Apply()
    {
        if (!ApplyInternal())
            return;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("OPENFEED: Left wall window cutout + WindowCutout_Frame applied. Save the scene (Ctrl+S).");
    }

    public static bool ApplyInternal()
    {
        GameObject leftWall = FindInLoadedScenes("LeftWall");
        GameObject frameGo = FindInLoadedScenes("WindowCutout_Frame");
        if (leftWall == null || frameGo == null)
        {
            Debug.LogError("MainAreaLeftWindowCutout: Need LeftWall and WindowCutout_Frame in the active scene.");
            return false;
        }

        Transform room = leftWall.transform.parent;
        if (room == null)
        {
            Debug.LogError("MainAreaLeftWindowCutout: LeftWall has no parent.");
            return false;
        }

        Material wallMat = leftWall.GetComponent<MeshRenderer>()?.sharedMaterial;
        Material frameMat = frameGo.GetComponent<MeshRenderer>()?.sharedMaterial;
        if (wallMat == null || frameMat == null)
        {
            Debug.LogError("MainAreaLeftWindowCutout: Missing materials on LeftWall or WindowCutout_Frame.");
            return false;
        }

        Undo.DestroyObjectImmediate(leftWall);

        MakeWallSegment(room, "LeftWall_BelowWindow", new Vector3(-4.9f, 0.6f, 4.75f), new Vector3(0.2f, 1.4f, 10.5f), wallMat);
        MakeWallSegment(room, "LeftWall_AboveWindow", new Vector3(-4.9f, 3.7f, 4.75f), new Vector3(0.2f, 1.2f, 10.5f), wallMat);
        MakeWallSegment(room, "LeftWall_LeftOfWindow", new Vector3(-4.9f, 2.2f, 3.25f), new Vector3(0.2f, 1.8f, 7.5f), wallMat);
        MakeWallSegment(room, "LeftWall_RightOfWindow", new Vector3(-4.9f, 2.2f, 9.7f), new Vector3(0.2f, 1.8f, 0.6f), wallMat);

        Vector3 frameLocalPos = frameGo.transform.localPosition;
        Quaternion frameLocalRot = frameGo.transform.localRotation;
        Undo.DestroyObjectImmediate(frameGo);

        GameObject frameRoot = new GameObject("WindowCutout_Frame");
        Undo.RegisterCreatedObjectUndo(frameRoot, "WindowCutout_Frame");
        frameRoot.transform.SetParent(room, false);
        frameRoot.transform.localPosition = frameLocalPos;
        frameRoot.transform.localRotation = frameLocalRot;
        frameRoot.transform.localScale = Vector3.one;

        const float halfOpenY = 0.9f;
        const float halfOpenZ = 1.2f;
        const float mouldT = 0.06f;

        MakeFrameBar(frameRoot.transform, "WindowFrame_Top", new Vector3(0f, halfOpenY + mouldT * 0.67f, 0f), new Vector3(0.22f, 0.08f, 2.52f), frameMat);
        MakeFrameBar(frameRoot.transform, "WindowFrame_Bottom", new Vector3(0f, -halfOpenY - mouldT * 0.67f, 0f), new Vector3(0.22f, 0.08f, 2.52f), frameMat);
        MakeFrameBar(frameRoot.transform, "WindowFrame_Left", new Vector3(0f, 0f, -(halfOpenZ - mouldT)), new Vector3(0.22f, 1.92f, 0.12f), frameMat);
        MakeFrameBar(frameRoot.transform, "WindowFrame_Right", new Vector3(0f, 0f, halfOpenZ - mouldT), new Vector3(0.22f, 1.92f, 0.12f), frameMat);

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
