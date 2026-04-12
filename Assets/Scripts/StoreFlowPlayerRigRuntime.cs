using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Builds <c>StoreFlowScene/PlayerRig</c> at runtime when <see cref="StoreFirstPersonController"/> is missing
/// (e.g. supermarket.unity without OPEN FEED → Supermarket → Setup Store Flow intro). Mirrors that editor setup.
/// </summary>
public static class StoreFlowPlayerRigRuntime
{
    /// <returns>The existing or newly created FPC, or null if creation failed.</returns>
    public static StoreFirstPersonController EnsureStorePlayerRig()
    {
        StoreFirstPersonController existing =
            Object.FindAnyObjectByType<StoreFirstPersonController>(FindObjectsInactive.Include);
        if (existing != null)
            return existing;

        GameObject flowRoot = GameObject.Find("StoreFlowScene");
        if (flowRoot == null)
            flowRoot = new GameObject("StoreFlowScene");

        Transform playerTf = flowRoot.transform.Find("PlayerRig");
        GameObject playerGo = playerTf != null ? playerTf.gameObject : new GameObject("PlayerRig");
        playerGo.transform.SetParent(flowRoot.transform, false);
        playerGo.transform.localPosition = Vector3.zero;
        playerGo.transform.localRotation = Quaternion.identity;
        playerGo.transform.localScale = Vector3.one;

        if (playerGo.GetComponent<CharacterController>() == null)
        {
            var cc = playerGo.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.28f;
            cc.center = new Vector3(0f, 0.9f, 0f);
        }

        Transform camPivotTf = playerGo.transform.Find("CameraPivot");
        if (camPivotTf == null)
        {
            var cp = new GameObject("CameraPivot");
            cp.transform.SetParent(playerGo.transform, false);
            cp.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            cp.transform.localRotation = Quaternion.identity;
            camPivotTf = cp.transform;
        }

        Camera playerCam = camPivotTf.GetComponentInChildren<Camera>(true);
        if (playerCam == null)
        {
            var camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(camPivotTf, false);
            camGo.transform.localPosition = Vector3.zero;
            camGo.transform.localRotation = Quaternion.identity;
            playerCam = camGo.AddComponent<Camera>();
            if (camGo.GetComponent<UniversalAdditionalCameraData>() == null)
                camGo.AddComponent<UniversalAdditionalCameraData>();
        }

        playerCam.tag = "MainCamera";
        playerCam.fieldOfView = 60f;

        AudioListener al = playerCam.GetComponent<AudioListener>();
        if (al == null)
            al = playerCam.gameObject.AddComponent<AudioListener>();
        al.enabled = true;

        DisableExtraMainCameras(flowRoot.transform);

        playerCam.enabled = true;

        StoreFirstPersonController fpc = playerGo.GetComponent<StoreFirstPersonController>();
        if (fpc == null)
            fpc = playerGo.AddComponent<StoreFirstPersonController>();
        fpc.cameraPivot = camPivotTf;

        return fpc;
    }

    static void DisableExtraMainCameras(Transform keepBranchRoot)
    {
        Camera[] cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
        foreach (Camera c in cams)
        {
            if (c == null)
                continue;
            if (keepBranchRoot != null && c.transform.IsChildOf(keepBranchRoot))
                continue;
            if (!c.CompareTag("MainCamera"))
                continue;
            c.enabled = false;
        }
    }
}
