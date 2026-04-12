using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Targeted rebuild of StoreFlowScene geometry while preserving ParkedCar, PlayerRig, IntroMarkers, etc.
/// Run: OPEN FEED / Store Flow / Targeted Rebuild
/// </summary>
public static class StoreFlowTargetedRebuild
{
    const string MenuPath = "OPEN FEED/Store Flow/Targeted Rebuild";
    const string RootName = "StoreFlowScene";
    const string HoldName = "__TargetedRebuildHold";
    const string NewMatFolder = "Assets/Materials/StoreFlowTargetedRebuild";

    const string HouseWallTexturePath = "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Bricks.jpg";
    const string HouseRoofTexturePath = "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Roof_tiles.jpg";
    const string HouseWindowTexturePath = "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Lights_01.jpg";
    const string AsphaltTexturePath = "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Asphalt.jpg";
    const string ConcreteTexturePath = "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Concrete_01.jpg";
    const string StoreSignTexturePath = "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Lottery.png";
    [MenuItem(MenuPath, false, 50)]
    public static void Run()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            Debug.LogError("Store Flow Targeted Rebuild: StoreFlowScene root not found.");
            return;
        }

        Dictionary<string, Material> matByName = ScavengeStoreFlowMaterials(root.transform);

        Transform hold = root.transform.Find(HoldName);
        if (hold == null)
        {
            GameObject h = new GameObject(HoldName);
            h.transform.SetParent(root.transform, false);
            hold = h.transform;
        }

        GameObject storeGo = FindChildGo(root.transform, "Store");
        if (storeGo != null)
        {
            List<Transform> move = new List<Transform>();
            for (int i = 0; i < storeGo.transform.childCount; i++)
            {
                Transform c = storeGo.transform.GetChild(i);
                if (ShouldPreserveStoreChild(c.name))
                    move.Add(c);
            }

            foreach (Transform t in move)
                t.SetParent(hold, true);

            Object.DestroyImmediate(storeGo);
        }

        DestroyIfChild(root.transform, "Ground");
        DestroyIfChild(root.transform, "Neighborhood");
        DestroyIfChild(root.transform, "WildernessTerrain");
        DestroyIfChild(root.transform, "WildernessSkirt");
        DestroyIfChild(root.transform, "ForestHighway");
        DestroyIfChild(root.transform, "StreetProps");
        DestroyIfChild(root.transform, "StoreGlow");
        DestroyIfChild(root.transform, "StreetLampLeft");
        DestroyIfChild(root.transform, "StreetLampRight");
        DestroyIfChild(root.transform, "CanopyLight");
        DestroyIfChild(root.transform, "FreezerSpill");
        DestroyIfChild(root.transform, "FlickerLight");

        Material GetMat(string logicalName)
        {
            if (matByName.TryGetValue(logicalName, out Material m) && m != null)
                return m;
            return CreateFallbackMaterial(logicalName, matByName);
        }

        Material asphalt = GetMat("StoreFlow_Asphalt");
        Material line = GetMat("StoreFlow_Line");
        Material concrete = GetMat("StoreFlow_Concrete");
        Material wall = GetMat("StoreFlow_Wall");
        Material roof = GetMat("StoreFlow_Roof");
        Material glass = GetMat("StoreFlow_Glass");
        Material doorFrame = GetMat("StoreFlow_DoorFrame");
        Material sign = GetMat("StoreFlow_Sign");
        Material trim = GetMat("StoreFlow_Trim");
        Material lightMat = GetMat("StoreFlow_Light");
        Material poleMat = GetMat("StoreFlow_Pole");
        Material railMat = GetMat("StoreFlow_Rail");
        Material posterMat = GetMat("StoreFlow_Poster");

        Material canopy = EnsureNewPsxMaterial("StoreFlow_Canopy", new Color(0.12f, 0.13f, 0.15f));
        Material facade = EnsureNewPsxMaterial("StoreFlow_Facade", new Color(0.92f, 0.88f, 0.82f));
        Material signFrame = EnsureNewPsxMaterial("StoreFlow_SignFrame", new Color(0.18f, 0.19f, 0.21f));
        Material entranceMat = EnsureNewPsxMaterial("StoreFlow_EntranceMat", new Color(0.04f, 0.04f, 0.045f));
        Material bollardMat = EnsureNewPsxMaterial("StoreFlow_Bollard", new Color(0.78f, 0.78f, 0.76f));
        Material trashMat = EnsureNewPsxMaterial("StoreFlow_TrashCan", new Color(0.22f, 0.28f, 0.14f));
        Material newsMat = EnsureNewPsxMaterial("StoreFlow_NewspaperBox", new Color(0.08f, 0.12f, 0.22f));

        BuildGround(root.transform, asphalt, line, concrete);
        GameObject store = BuildStoreShell(root.transform, wall, roof, sign, trim, glass, doorFrame, facade, canopy, signFrame);
        ReparentPreservedInterior(hold, store.transform);
        StoreFlowSceneGenerator.BuildLibreroStoreDecor(store.transform);

        StoreFlowSceneGenerator.BuildWildernessTerrain(root.transform);
        StoreFlowSceneGenerator.BuildForestHighway(root.transform, asphalt, concrete, line, lightMat, poleMat);
        BuildStreetProps(root.transform, concrete, trim, lightMat, poleMat, railMat, posterMat,
            bollardMat, entranceMat, trashMat, newsMat);
        BuildExteriorLights(root.transform);

        WireIntroAndDoors(root, store.transform);

        if (hold.childCount == 0)
            Object.DestroyImmediate(hold.gameObject);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = HexColor("0A0D14");
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = HexColor("080B12");
        RenderSettings.fogDensity = 0.018f;

        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("Store Flow Targeted Rebuild: complete. Save the scene (Ctrl+S).");
    }

    static void TrySetTag(GameObject go, string tag)
    {
        foreach (string t in InternalEditorUtility.tags)
        {
            if (t == tag)
            {
                go.tag = tag;
                return;
            }
        }

        Debug.LogWarning($"Store Flow Targeted Rebuild: tag \"{tag}\" is not defined (Edit > Project Settings > Tags and Layers). Assign manually if needed.");
    }

    static Color HexColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString("#" + hex, out Color c))
            return c;
        return Color.black;
    }

    static Dictionary<string, Material> ScavengeStoreFlowMaterials(Transform root)
    {
        Dictionary<string, Material> dict = new Dictionary<string, Material>();
        Renderer[] renderers = Object.FindObjectsByType<Renderer>();
        foreach (Renderer r in renderers)
        {
            if (r.sharedMaterial == null)
                continue;
            string n = r.sharedMaterial.name;
            if (n.StartsWith("StoreFlow_") && !dict.ContainsKey(n))
                dict[n] = r.sharedMaterial;
            if (r.sharedMaterials != null)
            {
                foreach (Material sm in r.sharedMaterials)
                {
                    if (sm != null && sm.name.StartsWith("StoreFlow_") && !dict.ContainsKey(sm.name))
                        dict[sm.name] = sm;
                }
            }
        }

        return dict;
    }

    static Material CreateFallbackMaterial(string logicalName, Dictionary<string, Material> matByName)
    {
        switch (logicalName)
        {
            case "StoreFlow_Asphalt":
                return Tex("StoreFlow_Asphalt", AsphaltTexturePath, new Color(0.7f, 0.7f, 0.7f), new Vector2(7f, 6f));
            case "StoreFlow_Line":
                return Lit("StoreFlow_Line", new Color(0.84f, 0.84f, 0.74f));
            case "StoreFlow_Concrete":
                return Tex("StoreFlow_Concrete", ConcreteTexturePath, new Color(0.68f, 0.68f, 0.68f), new Vector2(5f, 3f));
            case "StoreFlow_Wall":
                return Tex("StoreFlow_Wall", HouseWallTexturePath, new Color(0.85f, 0.85f, 0.85f), new Vector2(3f, 2f));
            case "StoreFlow_Roof":
                return Tex("StoreFlow_Roof", HouseRoofTexturePath, new Color(0.74f, 0.74f, 0.74f), new Vector2(3f, 2f));
            case "StoreFlow_Floor":
                return Tex("StoreFlow_Floor", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Tiles.jpg", new Color(0.92f, 0.92f, 0.92f), new Vector2(6f, 5f));
            case "StoreFlow_FloorStripe":
                return Lit("StoreFlow_FloorStripe", new Color(0.85f, 0.85f, 0.83f));
            case "StoreFlow_Glass":
                return Transparent("StoreFlow_Glass", new Color(0.72f, 0.88f, 0.96f, 0.22f));
            case "StoreFlow_DoorFrame":
                return Lit("StoreFlow_DoorFrame", new Color(0.14f, 0.15f, 0.18f));
            case "StoreFlow_Sign":
                return Tex("StoreFlow_Sign", StoreSignTexturePath, Color.white, Vector2.one);
            case "StoreFlow_Trim":
                return Tex("StoreFlow_Trim", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Metal_05.jpg", new Color(0.85f, 0.85f, 0.85f), Vector2.one);
            case "StoreFlow_Shelf":
                return Tex("StoreFlow_Shelf", "Assets/ModelsPlace/Models pack psx/Texture/Shelf_Store.png", new Color(0.92f, 0.92f, 0.92f), new Vector2(1f, 1f));
            case "StoreFlow_Light":
                return Emissive("StoreFlow_Light", new Color(1f, 0.96f, 0.88f), 1.25f);
            case "StoreFlow_Pole":
                return Tex("StoreFlow_Pole", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Metal_10.jpg", Color.white, new Vector2(1f, 2f));
            case "StoreFlow_Rail":
                return Tex("StoreFlow_Rail", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/RoadRail.jpg", Color.white, new Vector2(4f, 1f));
            case "StoreFlow_Poster":
                return Tex("StoreFlow_Poster", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/outdoor electronics.jpg", Color.white, Vector2.one);
            case "StoreFlow_Counter":
                return Lit("StoreFlow_Counter", new Color(0.82f, 0.83f, 0.86f));
            case "StoreFlow_CounterTop":
                return Lit("StoreFlow_CounterTop", new Color(0.94f, 0.94f, 0.96f));
            case "StoreFlow_Register":
                return Lit("StoreFlow_Register", new Color(0.18f, 0.19f, 0.21f));
            case "StoreFlow_FreezerBody":
                return Lit("StoreFlow_FreezerBody", new Color(0.84f, 0.88f, 0.94f));
            case "StoreFlow_FreezerTrim":
                return Lit("StoreFlow_FreezerTrim", new Color(0.58f, 0.68f, 0.82f));
            case "StoreFlow_FreezerGlow":
                return Emissive("StoreFlow_FreezerGlow", new Color(0.46f, 0.78f, 1f), 1.8f);
            case "StoreFlow_VendingBody":
                return Lit("StoreFlow_VendingBody", new Color(0.7f, 0.12f, 0.16f));
            case "StoreFlow_VendingGlow":
                return Emissive("StoreFlow_VendingGlow", new Color(0.9f, 0.96f, 1f), 1.2f);
            default:
                Debug.LogWarning($"Store Flow Targeted Rebuild: missing material '{logicalName}', using grey fallback.");
                return Lit(logicalName, Color.gray);
        }
    }

    static bool ShouldPreserveStoreChild(string name)
    {
        if (name.StartsWith("ShelfRow_") || name == "Freezers" || name == "CheckoutCorner" || name == "VendingMachine" || name == "InteriorFloor")
            return true;
        if (name.StartsWith("FloorStripe_") || name.StartsWith("LightStrip_") || name.StartsWith("InteriorLight_") || name.StartsWith("AisleLight_"))
            return true;
        return false;
    }

    static void DestroyIfChild(Transform root, string childName)
    {
        Transform t = root.Find(childName);
        if (t != null)
            Object.DestroyImmediate(t.gameObject);
    }

    static GameObject FindChildGo(Transform root, string name)
    {
        Transform t = root.Find(name);
        return t != null ? t.gameObject : null;
    }

    static void ReparentPreservedInterior(Transform hold, Transform store)
    {
        if (hold == null)
            return;
        while (hold.childCount > 0)
            hold.GetChild(0).SetParent(store, true);
    }

    static GameObject BuildStoreShell(Transform root, Material wall, Material roof,
        Material sign, Material trim, Material glass, Material doorFrame, Material facade, Material canopy, Material signFrame)
    {
        GameObject store = new GameObject("Store");
        store.transform.SetParent(root, false);

        CreatePrim("BackWall", PrimitiveType.Cube, store.transform, new Vector3(0f, 2f, 17.7f), Vector3.zero, new Vector3(14f, 4f, 0.24f), wall, true);
        CreatePrim("LeftWall", PrimitiveType.Cube, store.transform, new Vector3(-6.88f, 2f, 12.8f), Vector3.zero, new Vector3(0.24f, 4f, 10f), wall, true);
        CreatePrim("RightWall", PrimitiveType.Cube, store.transform, new Vector3(6.88f, 2f, 12.8f), Vector3.zero, new Vector3(0.24f, 4f, 10f), wall, true);
        CreatePrim("FrontWallLeft", PrimitiveType.Cube, store.transform, new Vector3(-4.2f, 2f, 8.02f), Vector3.zero, new Vector3(5.6f, 4f, 0.24f), wall, true);
        CreatePrim("FrontWallRight", PrimitiveType.Cube, store.transform, new Vector3(4.2f, 2f, 8.02f), Vector3.zero, new Vector3(5.6f, 4f, 0.24f), wall, true);
        CreatePrim("Roof", PrimitiveType.Cube, store.transform, new Vector3(0f, 4.05f, 12.8f), Vector3.zero, new Vector3(14.2f, 0.24f, 10.4f), roof, true);

        CreatePrim("FrontFacadeLeft", PrimitiveType.Cube, store.transform, new Vector3(-4.2f, 2f, 7.89f), Vector3.zero, new Vector3(5.6f, 4f, 0.02f), facade, false);
        CreatePrim("FrontFacadeRight", PrimitiveType.Cube, store.transform, new Vector3(4.2f, 2f, 7.89f), Vector3.zero, new Vector3(5.6f, 4f, 0.02f), facade, false);

        CreatePrim("Canopy", PrimitiveType.Cube, store.transform, new Vector3(0f, 3.85f, 7.5f), Vector3.zero, new Vector3(14.2f, 0.18f, 1.2f), canopy, false);

        GameObject signBand = CreatePrim("SignBand", PrimitiveType.Cube, store.transform, new Vector3(0f, 3.1f, 8f), Vector3.zero, new Vector3(10f, 0.6f, 0.3f), sign, false);
        TrySetTag(signBand, "StoreSign");

        CreatePrim("SignFrame", PrimitiveType.Cube, store.transform, new Vector3(0f, 3.1f, 8f), Vector3.zero, new Vector3(10.8f, 0.76f, 0.36f), signFrame, false);
        CreatePrim("TopTrim", PrimitiveType.Cube, store.transform, new Vector3(0f, 3.6f, 8f), Vector3.zero, new Vector3(10.8f, 0.12f, 0.36f), trim, false);

        CreatePrim("FrontWindowLeft", PrimitiveType.Cube, store.transform, new Vector3(-3.55f, 1.65f, 8.08f), Vector3.zero, new Vector3(3.9f, 2.6f, 0.06f), glass, false);
        CreatePrim("FrontWindowRight", PrimitiveType.Cube, store.transform, new Vector3(3.55f, 1.65f, 8.08f), Vector3.zero, new Vector3(3.9f, 2.6f, 0.06f), glass, false);
        CreatePrim("WindowFrameLeftTop", PrimitiveType.Cube, store.transform, new Vector3(-3.55f, 2.98f, 8.05f), Vector3.zero, new Vector3(4.05f, 0.1f, 0.1f), doorFrame, false);
        CreatePrim("WindowFrameLeftBottom", PrimitiveType.Cube, store.transform, new Vector3(-3.55f, 0.35f, 8.05f), Vector3.zero, new Vector3(4.05f, 0.1f, 0.1f), doorFrame, false);
        CreatePrim("WindowFrameLeftSideA", PrimitiveType.Cube, store.transform, new Vector3(-5.58f, 1.65f, 8.05f), Vector3.zero, new Vector3(0.1f, 2.7f, 0.1f), doorFrame, false);
        CreatePrim("WindowFrameLeftSideB", PrimitiveType.Cube, store.transform, new Vector3(-1.52f, 1.65f, 8.05f), Vector3.zero, new Vector3(0.1f, 2.7f, 0.1f), doorFrame, false);
        CreatePrim("WindowFrameRightTop", PrimitiveType.Cube, store.transform, new Vector3(3.55f, 2.98f, 8.05f), Vector3.zero, new Vector3(4.05f, 0.1f, 0.1f), doorFrame, false);
        CreatePrim("WindowFrameRightBottom", PrimitiveType.Cube, store.transform, new Vector3(3.55f, 0.35f, 8.05f), Vector3.zero, new Vector3(4.05f, 0.1f, 0.1f), doorFrame, false);
        CreatePrim("WindowFrameRightSideA", PrimitiveType.Cube, store.transform, new Vector3(1.52f, 1.65f, 8.05f), Vector3.zero, new Vector3(0.1f, 2.7f, 0.1f), doorFrame, false);
        CreatePrim("WindowFrameRightSideB", PrimitiveType.Cube, store.transform, new Vector3(5.58f, 1.65f, 8.05f), Vector3.zero, new Vector3(0.1f, 2.7f, 0.1f), doorFrame, false);

        GameObject doorAssembly = new GameObject("SlidingDoors");
        doorAssembly.transform.SetParent(store.transform, false);
        TrySetTag(doorAssembly, "StoreDoor");
        CreatePrim("DoorHeader", PrimitiveType.Cube, doorAssembly.transform, new Vector3(0f, 2.55f, 8.04f), Vector3.zero, new Vector3(2.8f, 0.25f, 0.32f), doorFrame, true);
        CreatePrim("DoorFrameLeft", PrimitiveType.Cube, doorAssembly.transform, new Vector3(-1.45f, 1.2f, 8.04f), Vector3.zero, new Vector3(0.14f, 2.45f, 0.32f), doorFrame, true);
        CreatePrim("DoorFrameRight", PrimitiveType.Cube, doorAssembly.transform, new Vector3(1.45f, 1.2f, 8.04f), Vector3.zero, new Vector3(0.14f, 2.45f, 0.32f), doorFrame, true);
        CreatePrim("LeftDoor", PrimitiveType.Cube, doorAssembly.transform, new Vector3(-0.52f, 1.2f, 8.05f), Vector3.zero, new Vector3(0.96f, 2.2f, 0.08f), glass, false);
        CreatePrim("RightDoor", PrimitiveType.Cube, doorAssembly.transform, new Vector3(0.52f, 1.2f, 8.05f), Vector3.zero, new Vector3(0.96f, 2.2f, 0.08f), glass, false);

        return store;
    }

    public static void WireIntroAndDoors(GameObject root, Transform store)
    {
        Transform sliding = store.Find("SlidingDoors");
        if (sliding == null)
            return;
        Transform leftDoor = sliding.Find("LeftDoor");
        Transform rightDoor = sliding.Find("RightDoor");
        if (leftDoor == null || rightDoor == null)
            return;

        StoreFlowIntroController intro = root.GetComponent<StoreFlowIntroController>();
        if (intro != null)
        {
            intro.leftDoor = leftDoor;
            intro.rightDoor = rightDoor;
            intro.leftDoorClosedLocalPosition = leftDoor.localPosition;
            intro.rightDoorClosedLocalPosition = rightDoor.localPosition;
            intro.leftDoorOpenLocalPosition = leftDoor.localPosition + Vector3.left * 0.82f;
            intro.rightDoorOpenLocalPosition = rightDoor.localPosition + Vector3.right * 0.82f;
        }

        StoreEntranceLock entranceLock = sliding.GetComponent<StoreEntranceLock>();
        if (entranceLock == null)
            entranceLock = sliding.gameObject.AddComponent<StoreEntranceLock>();
        entranceLock.ConfigureUsingDoors(leftDoor, rightDoor);
        entranceLock.UnlockEntrance();

        if (intro != null)
            intro.entranceLock = entranceLock;
    }

    static void BuildGround(Transform root, Material asphalt, Material line, Material concrete)
    {
        GameObject ground = new GameObject("Ground");
        ground.transform.SetParent(root, false);

        CreatePrim("ParkingLot", PrimitiveType.Cube, ground.transform, new Vector3(0f, -0.05f, 3f), Vector3.zero, new Vector3(24f, 0.1f, 18f), asphalt, true);
        CreatePrim("Road", PrimitiveType.Cube, ground.transform, new Vector3(0f, -0.04f, 14f), Vector3.zero, new Vector3(28f, 0.08f, 4f), asphalt, true);
        CreatePrim("Sidewalk", PrimitiveType.Cube, ground.transform, new Vector3(0f, 0.03f, 9.1f), Vector3.zero, new Vector3(14f, 0.12f, 2.2f), concrete, true);
        CreatePrim("Curb", PrimitiveType.Cube, ground.transform, new Vector3(0f, 0.07f, 8.05f), Vector3.zero, new Vector3(15f, 0.15f, 0.15f), concrete, false);

        GameObject floorCol = CreatePrim("StoreInteriorFloorCollider", PrimitiveType.Cube, ground.transform, new Vector3(0f, -0.41f, 12.8f), Vector3.zero, new Vector3(14f, 1f, 10f), null, true);
        Renderer fr = floorCol.GetComponent<Renderer>();
        if (fr != null)
            fr.enabled = false;

        for (int i = 0; i < 7; i++)
        {
            CreatePrim($"RoadDash{i}", PrimitiveType.Cube, ground.transform,
                new Vector3(-10.8f + i * 3.6f, 0.005f, 14f), Vector3.zero, new Vector3(1.5f, 0.01f, 0.1f), line, false);
        }

        StoreFlowSceneGenerator.BuildParkingStripesOnGround(ground.transform, line);
    }

    static void BuildStreetProps(Transform root, Material concrete, Material trim, Material lightMat, Material poleMat, Material railMat, Material posterMat,
        Material bollardMat, Material entranceMat, Material trashMat, Material newsMat)
    {
        GameObject props = new GameObject("StreetProps");
        props.transform.SetParent(root, false);

        for (int i = 0; i < 4; i++)
        {
            float x = -12f + i * 8f;
            CreatePrim($"LampPole_{i}", PrimitiveType.Cylinder, props.transform, new Vector3(x, 3.2f, 6.1f), Vector3.zero, new Vector3(0.1f, 3.2f, 0.1f), poleMat, false);
            CreatePrim($"LampHead_{i}", PrimitiveType.Cube, props.transform, new Vector3(x, 6.25f, 6.1f), Vector3.zero, new Vector3(0.55f, 0.2f, 0.45f), lightMat, false);
            AddLight(props.transform, $"LampLight_{i}", new Vector3(x, 5.9f, 6.1f), LightType.Point, new Color(1f, 0.88f, 0.64f), 1.25f, 8f, 0f);
        }

        CreatePrim("GuardRailL", PrimitiveType.Cube, props.transform, new Vector3(-13.8f, 0.55f, 5.7f), Vector3.zero, new Vector3(0.24f, 1.1f, 16f), railMat, false);
        CreatePrim("GuardRailR", PrimitiveType.Cube, props.transform, new Vector3(13.8f, 0.55f, 5.7f), Vector3.zero, new Vector3(0.24f, 1.1f, 16f), railMat, false);

        for (int i = 0; i < 8; i++)
        {
            float x = -11f + i * 3.1f;
            CreatePrim($"ParkingBlock_{i}", PrimitiveType.Cube, props.transform, new Vector3(x, 0.06f, 8.25f), Vector3.zero, new Vector3(1.3f, 0.12f, 0.36f), concrete, false);
        }

        CreatePrim("PosterBoard", PrimitiveType.Cube, props.transform, new Vector3(-6.1f, 2.2f, 8.15f), Vector3.zero, new Vector3(2.2f, 2.4f, 0.08f), trim, false);
        CreatePrim("PosterFace", PrimitiveType.Quad, props.transform, new Vector3(-6.1f, 2.2f, 8.21f), new Vector3(0f, 180f, 0f), new Vector3(2f, 2.2f, 1f), posterMat, false);

        CreatePrim("Bollard_L", PrimitiveType.Cylinder, props.transform, new Vector3(-2f, 0.4f, 7.9f), Vector3.zero, new Vector3(0.4f, 0.4f, 0.4f), bollardMat, true);
        CreatePrim("Bollard_R", PrimitiveType.Cylinder, props.transform, new Vector3(2f, 0.4f, 7.9f), Vector3.zero, new Vector3(0.4f, 0.4f, 0.4f), bollardMat, true);

        CreatePrim("EntranceMat", PrimitiveType.Cube, props.transform, new Vector3(0f, 0.07f, 7.85f), Vector3.zero, new Vector3(1.2f, 0.02f, 0.6f), entranceMat, false);
        CreatePrim("TrashCan", PrimitiveType.Cylinder, props.transform, new Vector3(5.8f, 0.3f, 8.1f), Vector3.zero, new Vector3(0.4f, 0.3f, 0.4f), trashMat, false);
        CreatePrim("NewspaperBox", PrimitiveType.Cube, props.transform, new Vector3(-5.5f, 0.25f, 8.1f), Vector3.zero, new Vector3(0.3f, 0.5f, 0.25f), newsMat, false);
    }

    static void BuildExteriorLights(Transform root)
    {
        AddLight(root, "StoreGlow", new Vector3(0f, 2.9f, 9.6f), LightType.Point, new Color(1f, 0.92f, 0.82f), 1.7f, 10f, 0f);
        AddLight(root, "StreetLampLeft", new Vector3(-5.4f, 4.2f, 7.5f), LightType.Point, new Color(1f, 0.84f, 0.6f), 1.4f, 8f, 0f);
        AddLight(root, "StreetLampRight", new Vector3(5.4f, 4.2f, 7.5f), LightType.Point, new Color(1f, 0.84f, 0.6f), 1.4f, 8f, 0f);
        GameObject canopyLight = new GameObject("CanopyLight");
        canopyLight.transform.SetParent(root, false);
        canopyLight.transform.localPosition = new Vector3(0f, 3.6f, 7.8f);
        Light cl = canopyLight.AddComponent<Light>();
        cl.type = LightType.Point;
        cl.color = new Color(1f, 0.96f, 0.88f);
        cl.intensity = 1f;
        cl.range = 5f;
        cl.shadows = LightShadows.Soft;

        AddLight(root, "FreezerSpill", new Vector3(0f, 1.5f, 16.8f), LightType.Point, new Color(0.45f, 0.76f, 1f), 0.6f, 5f, 0f);

        GameObject flick = new GameObject("FlickerLight");
        flick.transform.SetParent(root, false);
        flick.transform.localPosition = new Vector3(-3.5f, 3f, 9.5f);
        TrySetTag(flick, "FlickerLight");
        Light fl = flick.AddComponent<Light>();
        fl.type = LightType.Point;
        fl.color = new Color(1f, 0.96f, 0.88f);
        fl.intensity = 0.5f;
        fl.range = 3.5f;
        fl.shadows = LightShadows.Soft;
    }

    static Material EnsureNewPsxMaterial(string assetName, Color baseColor)
    {
        if (!Directory.Exists(NewMatFolder))
            Directory.CreateDirectory(NewMatFolder);

        string path = $"{NewMatFolder}/{assetName}.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            ApplyPsxParams(existing, baseColor);
            return existing;
        }

        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material m = new Material(sh);
        m.name = assetName;
        ApplyPsxParams(m, baseColor);
        AssetDatabase.CreateAsset(m, path);
        AssetDatabase.SaveAssets();
        return m;
    }

    static void ApplyPsxParams(Material m, Color baseColor)
    {
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", baseColor);
        else
            m.color = baseColor;
        if (m.HasProperty("_Smoothness"))
            m.SetFloat("_Smoothness", 0f);
        if (m.HasProperty("_Metallic"))
            m.SetFloat("_Metallic", 0f);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        if (m.mainTexture != null)
        {
            m.mainTexture.filterMode = FilterMode.Point;
        }
    }

    static GameObject CreatePrim(string name, PrimitiveType type, Transform parent, Vector3 pos, Vector3 rot, Vector3 scale, Material mat, bool keepCollider)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = pos;
        obj.transform.localRotation = Quaternion.Euler(rot);
        obj.transform.localScale = scale;
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && mat != null)
            renderer.sharedMaterial = mat;
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null && !keepCollider)
            Object.DestroyImmediate(collider);
        return obj;
    }

    static void AddLight(Transform parent, string name, Vector3 pos, LightType type, Color color, float intensity, float range, float spotAngle)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        Light light = go.AddComponent<Light>();
        light.type = type;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        if (type == LightType.Spot)
            light.spotAngle = spotAngle;
        light.shadows = LightShadows.Soft;
    }

    static Shader FindShader() => Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

    static Material Lit(string name, Color color)
    {
        Material mat = new Material(FindShader());
        mat.name = name;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else
            mat.color = color;
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.05f);
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0f);
        return mat;
    }

    static Material Tex(string name, string texturePath, Color tint, Vector2 tiling)
    {
        Material mat = Lit(name, tint);
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (tex != null)
        {
            tex.filterMode = FilterMode.Point;
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTextureScale("_BaseMap", tiling);
            }
            else
            {
                mat.mainTexture = tex;
                mat.mainTextureScale = tiling;
            }
        }

        return mat;
    }

    static Material Transparent(string name, Color color)
    {
        Material mat = Lit(name, color);
        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_ZWrite"))
            mat.SetFloat("_ZWrite", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        return mat;
    }

    static Material TransparentTex(string name, string path, Color tint)
    {
        Material mat = Transparent(name, tint);
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex != null)
        {
            tex.filterMode = FilterMode.Point;
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", tex);
            else
                mat.mainTexture = tex;
        }

        return mat;
    }

    static Material Emissive(string name, Color emission, float intensity)
    {
        Material mat = Lit(name, emission);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emission * intensity);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return mat;
    }
}
