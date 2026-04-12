#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds StoreFlowScene + PlayerRig + IntroMarkers + sliding doors + <see cref="SupermarketDriveInIntro"/>
/// for supermarket.unity, matches GroceryStore main-menu audio clip refs, and points GameFlowManager at
/// supermarket / ForestDrive.
/// </summary>
public static class SupermarketStoreFlowIntroSetup
{
    const string SupermarketScenePath = "Assets/supermarket.unity";
    const string IntroTitleGuid = "548f0cd7485a15c4a896f5f429338f62";
    const string MenuThemeGuid = "0faf0b1a9b8c054408971869ce773dbf";

    [MenuItem("OPEN FEED/Supermarket/Setup Store Flow intro (PlayerRig, markers, doors)", false, 15)]
    static void Run()
    {
        if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(SupermarketScenePath)))
        {
            EditorUtility.DisplayDialog("Supermarket", "Scene not found:\n" + SupermarketScenePath, "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
            "Supermarket Store Flow",
            "Adds or updates StoreFlowScene (PlayerRig, drive-in intro, placeholder sliding doors),\n" +
            "MainMenuUI + menu audio clips, and GameFlowManager (supermarket + ForestDrive).\n\n" +
            "Disables extra root Main Cameras so the player camera is active.\n\nSave the scene after.",
            "Run", "Cancel"))
            return;

        Scene scene = EditorSceneManager.OpenScene(SupermarketScenePath);
        ApplyCoreSetup();
        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log("[Supermarket] Store Flow intro setup complete. Save supermarket.unity (Ctrl+S). " +
                  "If walk path is missing, use OPEN FEED → Supermarket → Import walk path JSON from clipboard.");
    }

    static void ApplyCoreSetup()
    {
        GameObject flowRoot = GameObject.Find("StoreFlowScene");
        if (flowRoot == null)
            flowRoot = new GameObject("StoreFlowScene");

        EnsureMainMenuUIWithAudio();

        // --- PlayerRig (CharacterController + FPC + camera child) ---
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

        StoreFirstPersonController fpc = playerGo.GetComponent<StoreFirstPersonController>();
        if (fpc == null)
            fpc = playerGo.AddComponent<StoreFirstPersonController>();

        Transform camPivot = playerGo.transform.Find("CameraPivot");
        if (camPivot == null)
        {
            GameObject cp = new GameObject("CameraPivot");
            cp.transform.SetParent(playerGo.transform, false);
            cp.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            cp.transform.localRotation = Quaternion.identity;
            camPivot = cp.transform;
        }

        Camera playerCam = camPivot.GetComponentInChildren<Camera>(true);
        if (playerCam == null)
        {
            GameObject camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(camPivot, false);
            camGo.transform.localPosition = Vector3.zero;
            camGo.transform.localRotation = Quaternion.identity;
            playerCam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            if (camGo.GetComponent<UniversalAdditionalCameraData>() == null)
                camGo.AddComponent<UniversalAdditionalCameraData>();
        }

        // Always ensure exactly one enabled AudioListener lives on the player camera,
        // regardless of whether the camera was just created or already existed.
        AudioListener playerAL = playerCam.gameObject.GetComponent<AudioListener>();
        if (playerAL == null)
            playerAL = playerCam.gameObject.AddComponent<AudioListener>();
        playerAL.enabled = true;

        playerCam.tag = "MainCamera";
        playerCam.fieldOfView = 60f;
        fpc.cameraPivot = camPivot;

        // --- Intro markers (menu + authored car/walk/explore keyframes) ---
        Transform markers = flowRoot.transform.Find("IntroMarkers");
        if (markers == null)
        {
            GameObject m = new GameObject("IntroMarkers");
            m.transform.SetParent(flowRoot.transform, false);
            markers = m.transform;
        }

        Transform menuView = EnsureChildTransform(markers, "MenuView",
            new Vector3(10.29708f, 0.9298177f, -7.386248f),
            Quaternion.Normalize(new Quaternion(0.09100126f, -0.7218513f, -0.1024447f, -0.6783469f)));

        // Car seat + exit positions are computed relative to ParkedCar so they stay correct
        // if the car is ever repositioned.  Local offsets were derived from the authored world
        // poses when the car was at pos(4.2,5.82,4.73) rot(-180,-2.4,0) scale(-0.53).
        // Fallback hardcoded world values are used when ParkedCar cannot be found.
        Vector3    carSeatPos; Quaternion carSeatRot;
        Vector3    carExitPos; Quaternion carExitRot;

        GameObject parkedCarGo = GameObject.Find("ParkedCar");
        if (parkedCarGo != null)
        {
            Transform  ct             = parkedCarGo.transform;
            Vector3    savedLocalPos  = ct.localPosition;
            Quaternion savedLocalRot  = ct.localRotation;
            Vector3    savedLocalScl  = ct.localScale;

            // Local offsets (position in car-local space, rotation relative to car orientation).
            var seatLPos = new Vector3(-0.737f, -8.454f,  8.866f);
            var exitLPos = new Vector3(-4.134f, -7.433f,  8.183f);
            var seatLRot = new Quaternion( 0.9917f,   -0.005566f, -0.012542f, -0.127679f);
            var exitLRot = new Quaternion( 0.99488f,  -0.001165f,  0.024573f, -0.097991f);

            carSeatPos = ct.TransformPoint(seatLPos);
            carSeatRot = Quaternion.Normalize(ct.rotation * seatLRot);
            carExitPos = ct.TransformPoint(exitLPos);
            carExitRot = Quaternion.Normalize(ct.rotation * exitLRot);

            // Restore — setup must never move the ParkedCar.
            ct.localPosition = savedLocalPos;
            ct.localRotation = savedLocalRot;
            ct.localScale    = savedLocalScl;
        }
        else
        {
            carSeatPos = new Vector3(4.786995f, 1.339541f, 9.408913f);
            carSeatRot = Quaternion.Normalize(new Quaternion(0.1275342f,  -0.03330887f, 0.00823875f, 0.9912404f));
            carExitPos = new Vector3(6.571137f, 1.879308f, 8.971723f);
            carExitRot = Quaternion.Normalize(new Quaternion(0.09794629f, 0.003738982f, 0.003216869f, 0.9951795f));
        }

        // "Walking into the store" — final authored keyframe / handoff pose.
        Vector3 enterStorePos = new Vector3(10.42295f, 1.838974f, 17.21366f);
        Quaternion enterStoreRot = Quaternion.Normalize(new Quaternion(0.002088835f, -0.001087437f, 0.003498064f, 0.9999911f));

        Transform carSeatView = EnsureChildTransform(markers, "CarSeatView", carSeatPos, carSeatRot);

        // Remove stale markers that are no longer needed.
        foreach (string staleName in new[] { "CarExitView", "ExploreView" })
        {
            Transform stale = markers.Find(staleName);
            if (stale != null)
                Object.DestroyImmediate(stale.gameObject);
        }

        // --- Sliding door placeholder (in front of store entrance; tune in scene) ---
        Transform sliding = flowRoot.transform.Find("SlidingDoors");
        if (sliding == null)
        {
            GameObject sd = new GameObject("SlidingDoors");
            sd.transform.SetParent(flowRoot.transform, true);
            sliding = sd.transform;
        }

        sliding.position = enterStorePos + (enterStoreRot * Vector3.forward) * -0.55f;
        sliding.rotation = enterStoreRot;

        Transform leftDoor = sliding.Find("LeftDoor");
        Transform rightDoor = sliding.Find("RightDoor");
        if (leftDoor == null)
        {
            GameObject l = GameObject.CreatePrimitive(PrimitiveType.Cube);
            l.name = "LeftDoor";
            l.transform.SetParent(sliding, false);
            l.transform.localPosition = new Vector3(-0.52f, 1.2f, 0f);
            l.transform.localScale = new Vector3(0.96f, 2.2f, 0.08f);
            Object.DestroyImmediate(l.GetComponent<Collider>());
            leftDoor = l.transform;
        }
        if (rightDoor == null)
        {
            GameObject r = GameObject.CreatePrimitive(PrimitiveType.Cube);
            r.name = "RightDoor";
            r.transform.SetParent(sliding, false);
            r.transform.localPosition = new Vector3(0.52f, 1.2f, 0f);
            r.transform.localScale = new Vector3(0.96f, 2.2f, 0.08f);
            Object.DestroyImmediate(r.GetComponent<Collider>());
            rightDoor = r.transform;
        }

        StoreEntranceLock entranceLock = sliding.GetComponent<StoreEntranceLock>();
        if (entranceLock == null)
            entranceLock = sliding.gameObject.AddComponent<StoreEntranceLock>();
        entranceLock.ConfigureUsingDoors(leftDoor, rightDoor);
        entranceLock.UnlockEntrance();

        StoreFlowIntroController legacyIntro = flowRoot.GetComponent<StoreFlowIntroController>();
        if (legacyIntro != null)
            Object.DestroyImmediate(legacyIntro);

        SupermarketDriveInIntro driveIn = flowRoot.GetComponent<SupermarketDriveInIntro>();
        if (driveIn == null)
            driveIn = flowRoot.AddComponent<SupermarketDriveInIntro>();

        driveIn.playerController = fpc;
        driveIn.playerCamera = playerCam;
        driveIn.leftDoor = leftDoor;
        driveIn.rightDoor = rightDoor;
        driveIn.leftDoorClosedLocal = leftDoor.localPosition;
        driveIn.rightDoorClosedLocal = rightDoor.localPosition;
        driveIn.leftDoorOpenLocal = leftDoor.localPosition + Vector3.left * 0.82f;
        driveIn.rightDoorOpenLocal = rightDoor.localPosition + Vector3.right * 0.82f;
        driveIn.entranceLock = entranceLock;

        driveIn.playerSpawnPosition = carExitPos + Vector3.up * 0.12f;
        Vector3 towardEntrance = enterStorePos - driveIn.playerSpawnPosition;
        if (towardEntrance.sqrMagnitude < 0.04f)
            towardEntrance = enterStoreRot * Vector3.forward;
        driveIn.playerSpawnForward = towardEntrance;
        driveIn.insideSpawnPosition = enterStorePos;
        driveIn.insideSpawnForward = enterStoreRot * Vector3.forward;

        Vector3 triggerCenter = enterStorePos + enterStoreRot * new Vector3(0f, 0f, -2.1f);
        driveIn.entranceTriggerCenter = triggerCenter;

        driveIn.drivePathWorldY = 0f;
        driveIn.playerExteriorEyeOffsetY = 1.62f;

        // Same car hierarchy as Forest Drive (OpenFeedDrivingCarBuilder) — avoids upside-down X-flip on mesh.
        driveIn.carBodyLocalPos = new Vector3(0f, 1.55f, 3.38f);
        driveIn.carBodyLocalEuler = new Vector3(0f, 180f, 0f);
        driveIn.driveInSubaruLocalPosition = Vector3.zero;
        driveIn.driveInSubaruLocalEuler = Vector3.zero;
        driveIn.carBodyScale = 0.4f;
        driveIn.driverEyeLocalPos = new Vector3(-0.3f, 1.05f, 0.4f);
        driveIn.dashLocalOnRig = new Vector3(0f, 0.64f, 0.95f);
        driveIn.headlightLeftLocalOnRig = new Vector3(-0.5f, 0.5f, 2f);
        driveIn.headlightRightLocalOnRig = new Vector3(0.5f, 0.5f, 2f);

        // Recorded Scene-view path: Resources/SupermarketDriveCameraPath.json (copy of tools/camera-path-recordings export).
        driveIn.useRecordedDrivePath = true;
        driveIn.driveCameraPathAsset = null;
        driveIn.driveCameraPathResourceName = "SupermarketDriveCameraPath";

        GameObject parkedForDrive = GameObject.Find("ParkedCar");
        if (parkedForDrive != null)
        {
            Vector3 p = parkedForDrive.transform.position;
            // X/Z from the parked car; Y comes from drivePathWorldY at runtime (ground), not lot display height.
            driveIn.parkingSpotPosition = new Vector3(p.x, 0f, p.z);
            driveIn.parkingTurnPosition = new Vector3(p.x + 12f, 0f, p.z);
            driveIn.highwayStartPosition = new Vector3(p.x - 95f, 0f, p.z);
        }

        // --- GameFlowManager ---
        GameFlowManager gfm = Object.FindAnyObjectByType<GameFlowManager>();
        if (gfm == null)
        {
            GameObject g = new GameObject("GameFlowManager");
            gfm = g.AddComponent<GameFlowManager>();
        }

        {
            var so = new SerializedObject(gfm);
            so.FindProperty("storeScene").stringValue = "supermarket";
            so.FindProperty("drivingScene").stringValue = "ForestDrive";
            so.FindProperty("useStoreIntroWhenAvailable").boolValue = true;
            so.FindProperty("fallbackToStoreCutscene").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        DisableExtraMainCameras(flowRoot.transform);
    }

    [MenuItem("OPEN FEED/Supermarket/Add Main Menu UI + menu audio", false, 16)]
    static void RunMainMenuOnly()
    {
        if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(SupermarketScenePath)))
        {
            EditorUtility.DisplayDialog("Supermarket", "Scene not found:\n" + SupermarketScenePath, "OK");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(SupermarketScenePath);
        EnsureMainMenuUIWithAudio();
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[Supermarket] MainMenuUI + audio assigned. Save supermarket.unity (Ctrl+S).");
    }

    /// <summary>Creates <see cref="MainMenuUI"/> if missing and assigns the same title/theme clips as GroceryStore / Supermarket generator.</summary>
    static void EnsureMainMenuUIWithAudio()
    {
        MainMenuUI menuUi = Object.FindAnyObjectByType<MainMenuUI>();
        if (menuUi == null)
        {
            GameObject go = new GameObject("MainMenuUI");
            menuUi = go.AddComponent<MainMenuUI>();
        }

        AudioClip title = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(IntroTitleGuid));
        AudioClip theme = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(MenuThemeGuid));
        if (title == null)
            Debug.LogWarning("[Supermarket] Intro title clip not found for GUID " + IntroTitleGuid + ". Assign introTitleClip on MainMenuUI manually.");
        if (theme == null)
            Debug.LogWarning("[Supermarket] Menu theme clip not found for GUID " + MenuThemeGuid + ". Assign menuThemeClip on MainMenuUI manually.");

        SerializedObject so = new SerializedObject(menuUi);
        so.FindProperty("introTitleClip").objectReferenceValue = title;
        so.FindProperty("menuThemeClip").objectReferenceValue = theme;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void DisableExtraMainCameras(Transform keepCameraUnder)
    {
        Camera[] cams = Object.FindObjectsByType<Camera>();
        foreach (Camera c in cams)
        {
            if (c == null)
                continue;
            if (keepCameraUnder != null && c.transform.IsChildOf(keepCameraUnder))
                continue;
            if (!c.CompareTag("MainCamera"))
                continue;
            c.enabled = false;
            // Do NOT disable AudioListeners on other cameras — disabling them would leave
            // the scene without any AudioListener if the player camera's listener is missing.
            // The player camera's AudioListener is ensured enabled above; extra listeners
            // are harmless (Unity picks one automatically when multiple are present).
        }
    }

    static Transform EnsureChildTransform(Transform parent, string name, Vector3 worldPos, Quaternion worldRot)
    {
        Transform t = parent.Find(name);
        if (t == null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            t = go.transform;
        }
        t.SetPositionAndRotation(worldPos, worldRot);
        return t;
    }
}
#endif
