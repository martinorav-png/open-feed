using System;
using System.Collections.Generic;
using System.IO;
using SquareTileTerrainEditor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class StoreFlowSceneGenerator : Editor
{
    const string RootName = "StoreFlowScene";
    const string SubaruRootFolder = "Assets/ModelsPlace/subaru-impreza";
    const string SubaruBodyTexPath = "Assets/ModelsPlace/subaru-impreza/textures/bs_sub_impreza.png";
    const string SubaruWheelTexPath = "Assets/ModelsPlace/subaru-impreza/textures/bs_sub_impreza_wheel.png";
    const string SubaruOtherTexPath = "Assets/ModelsPlace/subaru-impreza/textures/op_sub_impreza.png";
    const string RenaultFbxPath = "Assets/ModelsPlace/psx-low-poly-car-renault/source/Car.fbx";
    const string RenaultBodyMatPath = "Assets/ModelsPlace/psx-low-poly-car-renault/Materials/Car_-_RegularWhite.mat";
    const string RenaultTireMatPath = "Assets/ModelsPlace/psx-low-poly-car-renault/Materials/Tire.mat";
    const string HouseWallTexturePath = "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Bricks.jpg";
    const string HouseRoofTexturePath = "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Roof_tiles.jpg";
    const string AsphaltTexturePath = "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Asphalt.jpg";
    const string ConcreteTexturePath = "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Concrete_01.jpg";
    const string StoreSignTexturePath = "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Lottery.png";
    const string StoreFlowWildernessConfigPath = "Assets/SquareTileTerrain/Configs/StoreFlowWilderness.txt";
    const int StoreFlowWildernessTerrainId = 901;
    const string SquareTileGrassPrefabPath = "Assets/SquareTileTerrain/Example/TilePrefabs/Tile_Grass.prefab";
    const string SquareTileTreePrefabPath = "Assets/SquareTileTerrain/Example/Tree/TreePrefab.prefab";
    const string PsxModelsPackDaePath = "Assets/ModelsPlace/Models pack psx/Models/models.dae";
    const string PsxShelfStoreTexturePath = "Assets/ModelsPlace/Models pack psx/Texture/Shelf_Store.png";
    const string TmpLiberationSansSdfPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    const string SuperMarketPackGlbPath = "Assets/ModelsPlace/super-market-low-poly-for-free/source/магазин.glb";
    const string SuperMarketPackAssetGuid = "1c3bf01de144d8c4390047fb6673658c";

    /// <summary>Alternative super-market pack (scene.gltf) under ModelsPlace; used by Replace Store With scene.gltf menu.</summary>
    public const string SuperMarketGltfSceneAssetPath = "Assets/ModelsPlace/super_market_low_poly_for_free/scene.gltf";

    /// <summary>When true, interior is the GLB from super-market-low-poly-for-free (sliding doors stay procedural for intro/lock).</summary>
    public const bool StoreFlowUseSuperMarketPackInterior = true;

    /// <summary>Extra local Y rotation (deg) for the imported GLB if the mesh faces away from the sliding doors.</summary>
    public static float SuperMarketPackExtraYawDegrees = 0f;

    /// <summary>Procedural stock on aisle shelves, freezer bays, checkout impulse strip, and vending rows. Set true to fill shelves again.</summary>
    /// <remarks>Must not be <c>const</c> — a compile-time false makes the branches dead code (CS0162).</remarks>
    public static bool StoreFlowPlaceProductGeometry = false;

    /// <summary>When true and <c>StoreFlowBakedWilderness.json</c> exists under Assets/Editor/OpenFeed/StoreFlow, skirt grass + all TreePrefab poses match the baked GroceryStore scene (re-export with tools/export_storeflow_baked_wilderness.py).</summary>
    public static bool StoreFlowUseBakedWildernessLayout = true;

    static string StoreFlowBakedWildernessJsonFullPath =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "Editor", "OpenFeed", "StoreFlow", "StoreFlowBakedWilderness.json"));

    [Serializable]
    class StoreFlowBakedTsr
    {
        public float px, py, pz, qx, qy, qz, qw, sx, sy, sz;
    }

    [Serializable]
    class StoreFlowBakedWildernessFile
    {
        public int version;
        public StoreFlowBakedTsr[] trees;
        public StoreFlowBakedTsr[] skirtGrassTiles;
    }

    // Forest highway through wilderness (root-local XZ + heading); Y is road surface height.
    static readonly Vector3 ForestHighwayAnchorLocal = new Vector3(0.2762499f, -0.06f, -10.22602f);
    static readonly Quaternion ForestHighwayHeadingLocal = new Quaternion(-0.007493861f, 0.7174935f, -0.0073584f, -0.696486f);
    const float ForestHighwayHalfLength = 210f;
    const float ForestHighwayClearHalfWidth = 28f;
    const float ForestHighwayWildernessTileSize = 16f;
    const float ForestHighwayRoadHalfWidth = 11f;
    const float ForestHighwayRoadSurfaceY = -0.06f;
    /// <summary>Narrower shoulders read as less “barrier” into the lot; inner edge stays just past the asphalt.</summary>
    const float ForestHighwayShoulderWidth = 2.2f;
    const float ForestHighwayShoulderPastRoadEdge = 0.12f;

    const float StoreFlowParkingLampRowZ = 7.9f;
    const float StoreFlowGuardRailCenterZ = 8.15f;
    const float StoreFlowGuardRailLengthZ = 12f;

    // Tuned in-scene (GroceryStore / StoreFlowScene) — keep in sync when re-running the generator.
    static readonly Vector3 ParkedCarLocalPosition = new Vector3(4.2f, 5.98f, -0.39f);
    static readonly Vector3 ParkedCarLocalEuler = new Vector3(0f, 177.6f, 180f);
    static readonly Vector3 ParkedCarLocalScale = new Vector3(-0.53f, -0.53f, -0.53f);
    static readonly Vector3 SubaruImprezaLocalPosition = new Vector3(1.75f, -9.26f, 4.81f);

    [MenuItem("OPEN FEED/Store Flow/Reimport Supermarket GLB", false, 400)]
    static void ReimportSuperMarketGlbMenu()
    {
        string path = AssetDatabase.GUIDToAssetPath(SuperMarketPackAssetGuid);
        if (string.IsNullOrEmpty(path))
            path = SuperMarketPackGlbPath;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        GameObject test = LoadSuperMarketPackPrefab();
        if (test != null)
            Debug.Log("OPENFEED: Supermarket GLB reimported — root: " + test.name);
        else
            Debug.LogError("OPENFEED: Supermarket GLB still did not produce a GameObject. Check Console / Model import errors.");
    }

    [MenuItem("OPEN FEED/Store Flow/Rebuild Librero Decor", false, 401)]
    static void RebuildLibreroDecorMenu()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            Debug.LogError("Store Flow: StoreFlowScene root not found.");
            return;
        }

        Transform store = root.transform.Find("Store");
        if (store == null)
        {
            Debug.LogError("Store Flow: Store child not found.");
            return;
        }

        BuildLibreroStoreDecor(store);
        Debug.Log("Store Flow: LibreroDecor rebuilt under Store (remove any duplicate Librero models you dropped at scene root).");
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
    }

    [MenuItem("OPEN FEED/Store Flow/Clear Scene", false, 200)]
    static void ClearScene()
    {
        DestroyRoot(RootName);
        DestroyRoot("SixTwelveScene");
    }

    [MenuItem("OPEN FEED/Store Flow/Generate Scene", false, 0)]
    static void GenerateScene()
    {
        DestroyRoot(RootName);
        DestroyRoot("SixTwelveScene");

        GameObject root = new GameObject(RootName);
        EnsureSceneBootstrap();
        RemoveDirectionalLights();

        Material asphalt = CreateTexturedLitFromPath("StoreFlow_Asphalt", AsphaltTexturePath, new Color(0.7f, 0.7f, 0.7f), new Vector2(7f, 6f));
        Material line = CreateLit("StoreFlow_Line", new Color(0.84f, 0.84f, 0.74f));
        Material concrete = CreateTexturedLitFromPath("StoreFlow_Concrete", ConcreteTexturePath, new Color(0.68f, 0.68f, 0.68f), new Vector2(5f, 3f));
        Material wall = CreateTexturedLitFromPath("StoreFlow_Wall", HouseWallTexturePath, new Color(0.85f, 0.85f, 0.85f), new Vector2(3f, 2f));
        Material roof = CreateTexturedLitFromPath("StoreFlow_Roof", HouseRoofTexturePath, new Color(0.74f, 0.74f, 0.74f), new Vector2(3f, 2f));
        Material sign = CreateTexturedLitFromPath("StoreFlow_Sign", StoreSignTexturePath, Color.white, new Vector2(1f, 1f));
        Material shelf = CreateTexturedLitFromPath("StoreFlow_Shelf", PsxShelfStoreTexturePath, new Color(0.92f, 0.92f, 0.92f), new Vector2(1f, 1f));
        Material trim = CreateTexturedLitFromPath("StoreFlow_Trim", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Metal_05.jpg", new Color(0.85f, 0.85f, 0.85f), new Vector2(1f, 1f));
        Material floor = CreateTexturedLitFromPath("StoreFlow_Floor", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Tiles.jpg", new Color(0.92f, 0.92f, 0.92f), new Vector2(6f, 5f));
        Material floorStripe = CreateLit("StoreFlow_FloorStripe", new Color(0.85f, 0.85f, 0.83f));
        Material productA = CreateLabelMaterial("StoreFlow_ProductA", new Color(0.78f, 0.3f, 0.28f), new Color(0.95f, 0.86f, 0.2f));
        Material productB = CreateLabelMaterial("StoreFlow_ProductB", new Color(0.26f, 0.62f, 0.38f), new Color(0.94f, 0.94f, 0.96f));
        Material productC = CreateLabelMaterial("StoreFlow_ProductC", new Color(0.24f, 0.42f, 0.78f), new Color(0.86f, 0.22f, 0.26f));
        Material productD = CreateLabelMaterial("StoreFlow_ProductD", new Color(0.92f, 0.92f, 0.94f), new Color(0.3f, 0.56f, 0.92f));
        Material productE = CreateLabelMaterial("StoreFlow_ProductE", new Color(0.84f, 0.88f, 0.9f), new Color(0.12f, 0.18f, 0.22f));
        Material productF = CreateLabelMaterial("StoreFlow_ProductF", new Color(0.96f, 0.74f, 0.3f), new Color(0.68f, 0.18f, 0.16f));
        Material glass = CreateTransparent("StoreFlow_Glass", new Color(0.72f, 0.88f, 0.96f, 0.22f));
        GameObject subaruCarAsset = LoadFirstCarModelInFolder(SubaruRootFolder);
        bool useSubaruTextures = subaruCarAsset != null;
        Material carBody;
        Material wheel;
        Material tire;
        Material carInterior;
        Material carTrim;
        if (useSubaruTextures)
        {
            carBody = CreateTexturedLitFromPath("StoreFlow_SubaruBody", SubaruBodyTexPath, Color.white, Vector2.one);
            wheel = CreateTexturedLitFromPath("StoreFlow_SubaruWheel", SubaruWheelTexPath, Color.white, Vector2.one);
            tire = wheel;
            carInterior = CreateTexturedLitFromPath("StoreFlow_SubaruOther", SubaruOtherTexPath, Color.white, Vector2.one);
            carTrim = carInterior;
        }
        else
        {
            carBody = AssetDatabase.LoadAssetAtPath<Material>(RenaultBodyMatPath) ?? CreateLit("StoreFlow_CarBodyFallback", new Color(0.92f, 0.92f, 0.92f));
            tire = AssetDatabase.LoadAssetAtPath<Material>(RenaultTireMatPath) ?? CreateLit("StoreFlow_CarTireFallback", new Color(0.12f, 0.12f, 0.12f));
            wheel = tire;
            carInterior = carBody;
            carTrim = carBody;
        }

        Material carGlass = CreateTransparent("StoreFlow_CarGlassFallback", new Color(0.74f, 0.86f, 0.96f, 0.26f));
        Material carHeadlight = CreateEmissive("StoreFlow_CarHeadlightFallback", new Color(0.95f, 0.97f, 1f), 0.7f);
        Material carTaillight = CreateEmissive("StoreFlow_CarTaillightFallback", new Color(1f, 0.18f, 0.14f), 0.8f);
        Material doorFrame = CreateLit("StoreFlow_DoorFrame", new Color(0.14f, 0.15f, 0.18f));
        Material lightMat = CreateEmissive("StoreFlow_Light", new Color(1f, 0.96f, 0.88f), 1.25f);
        Material poleMat = CreateTexturedLitFromPath("StoreFlow_Pole", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Metal_10.jpg", Color.white, new Vector2(1f, 2f));
        Material freezerBody = CreateLit("StoreFlow_FreezerBody", new Color(0.84f, 0.88f, 0.94f));
        Material freezerTrim = CreateLit("StoreFlow_FreezerTrim", new Color(0.58f, 0.68f, 0.82f));
        Material freezerGlow = CreateEmissive("StoreFlow_FreezerGlow", new Color(0.46f, 0.78f, 1f), 1.8f);
        Material counter = CreateLit("StoreFlow_Counter", new Color(0.82f, 0.83f, 0.86f));
        Material counterTop = CreateLit("StoreFlow_CounterTop", new Color(0.94f, 0.94f, 0.96f));
        Material register = CreateLit("StoreFlow_Register", new Color(0.18f, 0.19f, 0.21f));
        Material registerGlow = CreateEmissive("StoreFlow_RegisterGlow", new Color(0.24f, 0.78f, 0.4f), 0.8f);
        Material vendingBody = CreateLit("StoreFlow_VendingBody", new Color(0.7f, 0.12f, 0.16f));
        Material vendingGlow = CreateEmissive("StoreFlow_VendingGlow", new Color(0.9f, 0.96f, 1f), 1.2f);

        BuildWildernessTerrain(root.transform);
        BuildForestHighway(root.transform, asphalt, concrete, line, lightMat, poleMat);
        BuildGround(root.transform, asphalt, line, concrete);
        BuildStore(root.transform, wall, roof, sign, glass, doorFrame, floor, floorStripe, shelf, trim,
            productA, productB, productC, productD, productE, productF,
            lightMat, freezerBody, freezerTrim, freezerGlow, counter, counterTop, register, registerGlow, vendingBody, vendingGlow,
            out Transform leftDoor, out Transform rightDoor);
        BuildStreetProps(root.transform, concrete, trim, lightMat, poleMat);
        BuildSnowfall(root.transform);

        GameObject carAnchor = new GameObject("ParkedCar");
        carAnchor.transform.SetParent(root.transform, false);
        carAnchor.transform.localPosition = ParkedCarLocalPosition;
        carAnchor.transform.localRotation = Quaternion.Euler(ParkedCarLocalEuler);
        carAnchor.transform.localScale = ParkedCarLocalScale;
        BuildCar(carAnchor.transform, subaruCarAsset, useSubaruTextures, carBody, carTrim, carGlass, tire, wheel, carHeadlight, carTaillight, carInterior);

        BuildExteriorLighting(root.transform, lightMat);
        BuildPlayerAndIntro(root.transform, leftDoor, rightDoor);

        ApplyStoreFlowNightRenderSettings();

        Selection.activeGameObject = root;
        Debug.Log("OPENFEED Store Flow scene generated.");
    }

    static void ApplyStoreFlowNightRenderSettings()
    {
        StoreFlowExteriorNightSetup.ApplyNightRenderSettingsShared();
    }

    const string GroceryStoreScenePath = "Assets/Scenes/GroceryStore.unity";

    /// <summary>Opens GroceryStore and writes night sky + fog/ambient into the scene (same as generator end).</summary>
    [MenuItem("OPEN FEED/Store Flow/Apply Night Render Settings", false, 300)]
    static void ApplyGroceryStoreNightRenderSettingsMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.OpenScene(GroceryStoreScenePath);
        StoreFlowExteriorNightSetup.ApplyNightRenderSettingsShared();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("OPENFEED: GroceryStore night sky, fog, and ambient applied and scene saved.");
    }

    /// <summary>Deletes TreePrefab instances under StoreFlowScene that sit in the store/parking exclusion (fixes bad scatter from older generator runs).</summary>
    [MenuItem("OPEN FEED/Store Flow/Strip Trees Inside Store Lot", false, 402)]
    static void StripTreesInsideStoreLotMenu()
    {
        GameObject rootGo = GameObject.Find(RootName);
        if (rootGo == null)
        {
            EditorUtility.DisplayDialog("Strip trees", $"No '{RootName}' root in the opened scene.", "OK");
            return;
        }

        Bounds ex = StoreWildernessTreeExclusionBounds(rootGo.transform);
        Transform[] all = rootGo.GetComponentsInChildren<Transform>(true);
        int removed = 0;
        for (int i = all.Length - 1; i >= 0; i--)
        {
            Transform tr = all[i];
            if (tr == null || tr.name != "TreePrefab")
                continue;
            if (ex.Contains(tr.position))
            {
                Undo.DestroyObjectImmediate(tr.gameObject);
                removed++;
            }
        }

        EditorSceneManager.MarkSceneDirty(rootGo.scene);
        Debug.Log($"OPENFEED: Removed {removed} TreePrefab object(s) inside the store-lot exclusion zone. Save the scene.");
    }

    public static void BuildWildernessTerrain(Transform root)
    {
        Transform existing = root.Find("WildernessTerrain");
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing.gameObject);
        Transform oldSkirt = root.Find("WildernessSkirt");
        if (oldSkirt != null)
            UnityEngine.Object.DestroyImmediate(oldSkirt.gameObject);

        var gen = new SquareTileTerrainGenerator();
        if (!SquareTileTerrainGenerator.TryLoadConfigFile(StoreFlowWildernessConfigPath, gen, out string err))
        {
            Debug.LogWarning("Store Flow: could not load wilderness terrain config — " + err);
            return;
        }

        if (!gen.CheckSettings(out err))
        {
            Debug.LogWarning("Store Flow: wilderness terrain settings invalid — " + err);
            return;
        }

        float halfX = gen.Tilemap.width * gen.TileSizeX * 0.5f;
        float halfZ = gen.Tilemap.height * gen.TileSizeZ * 0.5f;
        Vector3 localPos = new Vector3(-halfX, -0.1f, -halfZ);

        if (!gen.GenerateTerrain(StoreFlowWildernessTerrainId, root, localPos, "WildernessTerrain", out err))
        {
            Debug.LogWarning("Store Flow: wilderness terrain generation failed — " + err);
            return;
        }

        Transform wild = root.Find("WildernessTerrain");
        if (wild != null)
            CarveWildernessClearingUnderStoreLot(wild, gen.TileSizeX, gen.TileSizeZ, root);

        if (StoreFlowUseBakedWildernessLayout && TryLoadBakedWildernessFile(out StoreFlowBakedWildernessFile baked))
        {
            ApplyBakedWildernessSkirtAndTrees(root, baked);
        }
        else
        {
            if (wild != null)
                ScatterExtraTreesOnWildernessTiles(wild, gen.TileSizeX, gen.TileSizeZ, root, extraPerTile: 2);
            BuildWildernessGrassSkirtAndTrees(root);
        }
    }

    static bool TryLoadBakedWildernessFile(out StoreFlowBakedWildernessFile data)
    {
        data = null;
        string full = StoreFlowBakedWildernessJsonFullPath;
        if (!File.Exists(full))
        {
            if (StoreFlowUseBakedWildernessLayout)
                Debug.LogWarning("Store Flow: baked wilderness JSON missing at Assets/Editor/OpenFeed/StoreFlow/StoreFlowBakedWilderness.json — run tools/export_storeflow_baked_wilderness.py or disable StoreFlowUseBakedWildernessLayout. Using procedural skirt/trees.");
            return false;
        }

        try
        {
            string json = File.ReadAllText(full);
            data = JsonUtility.FromJson<StoreFlowBakedWildernessFile>(json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Store Flow: failed to read baked wilderness JSON — " + ex.Message);
            return false;
        }

        if (data == null || data.trees == null || data.skirtGrassTiles == null)
            return false;
        return true;
    }

    static void ApplyBakedWildernessSkirtAndTrees(Transform sceneRoot, StoreFlowBakedWildernessFile baked)
    {
        GameObject grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SquareTileGrassPrefabPath);
        GameObject treePrefab = LoadScatterTreePrefab();
        if (grassPrefab == null || treePrefab == null)
        {
            Debug.LogError("Store Flow: baked wilderness layout requires Tile_Grass and TreePrefab assets.");
            return;
        }

        Transform oldBaked = sceneRoot.Find("WildernessBakedTrees");
        if (oldBaked != null)
            UnityEngine.Object.DestroyImmediate(oldBaked.gameObject);

        GameObject skirtGo = new GameObject("WildernessSkirt");
        skirtGo.transform.SetParent(sceneRoot, false);
        skirtGo.transform.localPosition = Vector3.zero;
        skirtGo.transform.localRotation = Quaternion.identity;
        skirtGo.transform.localScale = Vector3.one;

        for (int i = 0; i < baked.skirtGrassTiles.Length; i++)
        {
            StoreFlowBakedTsr e = baked.skirtGrassTiles[i];
            GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(grassPrefab, skirtGo.transform);
            tile.transform.SetPositionAndRotation(new Vector3(e.px, e.py, e.pz), new Quaternion(e.qx, e.qy, e.qz, e.qw));
            tile.transform.localScale = new Vector3(e.sx, e.sy, e.sz);
        }

        GameObject treeRoot = new GameObject("WildernessBakedTrees");
        treeRoot.transform.SetParent(sceneRoot, false);
        treeRoot.transform.localPosition = Vector3.zero;
        treeRoot.transform.localRotation = Quaternion.identity;
        treeRoot.transform.localScale = Vector3.one;

        for (int i = 0; i < baked.trees.Length; i++)
        {
            StoreFlowBakedTsr e = baked.trees[i];
            GameObject tr = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, treeRoot.transform);
            tr.name = "TreePrefab";
            tr.transform.SetPositionAndRotation(new Vector3(e.px, e.py, e.pz), new Quaternion(e.qx, e.qy, e.qz, e.qw));
            tr.transform.localScale = new Vector3(e.sx, e.sy, e.sz);
        }

        Debug.Log($"Store Flow: applied baked wilderness — {baked.skirtGrassTiles.Length} skirt grass tiles, {baked.trees.Length} trees.");
    }

    static GameObject LoadScatterTreePrefab()
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(SquareTileTreePrefabPath);
    }

    static string ResolvePsxModelsPackPath()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PsxModelsPackDaePath) != null)
            return PsxModelsPackDaePath;
        const string fbxPath = "Assets/ModelsPlace/Models pack psx/Models/models.fbx";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath) != null)
            return fbxPath;
        return PsxModelsPackDaePath;
    }

    /// <summary>Instances one PSX pack Librero mesh under <paramref name="parent"/> (same source as aisle shelves).</summary>
    static bool TryInstantiatePsxLibrero(
        Transform parent,
        string objectName,
        int variantSeed,
        Vector3 localFootPosition,
        Vector3 eulerDeg,
        float targetW,
        float targetH,
        float targetD,
        bool addBoxCollider)
    {
        string modelPath = ResolvePsxModelsPackPath();
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (modelAsset == null)
            return false;

        GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
        if (temp == null)
            return false;

        List<Transform> libreros = new List<Transform>();
        foreach (Transform t in temp.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.StartsWith("Librero", System.StringComparison.Ordinal) && t.GetComponent<MeshFilter>() != null)
                libreros.Add(t);
        }

        if (libreros.Count == 0)
        {
            UnityEngine.Object.DestroyImmediate(temp);
            return false;
        }

        int pick = Mathf.Abs(variantSeed) % libreros.Count;
        Transform src = libreros[pick];
        GameObject psx = (GameObject)UnityEngine.Object.Instantiate(src.gameObject, parent);
        psx.name = objectName;
        psx.transform.localRotation = Quaternion.Euler(eulerDeg) * Quaternion.Euler(0f, -90f, 0f);
        psx.transform.localScale = Vector3.one;

        MeshFilter mf = psx.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            UnityEngine.Object.DestroyImmediate(psx);
            UnityEngine.Object.DestroyImmediate(temp);
            return false;
        }

        Bounds mb = mf.sharedMesh.bounds;
        float sx = targetW / Mathf.Max(mb.size.x, 0.001f);
        float sy = targetH / Mathf.Max(mb.size.y, 0.001f);
        float sz = targetD / Mathf.Max(mb.size.z, 0.001f);
        float u = Mathf.Min(sx, sy, sz);
        psx.transform.localScale = Vector3.one * u;
        float yLift = -mb.min.y * u + 0.02f;
        psx.transform.localPosition = localFootPosition + new Vector3(0f, yLift, 0f);

        foreach (Collider c in psx.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(c);

        if (addBoxCollider)
        {
            Renderer[] rends = psx.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                Bounds wb = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++)
                    wb.Encapsulate(rends[i].bounds);
                Vector3 mn = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
                Vector3 mx = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
                Vector3[] corners =
                {
                    new Vector3(wb.min.x, wb.min.y, wb.min.z), new Vector3(wb.max.x, wb.min.y, wb.min.z),
                    new Vector3(wb.min.x, wb.max.y, wb.min.z), new Vector3(wb.max.x, wb.max.y, wb.min.z),
                    new Vector3(wb.min.x, wb.min.y, wb.max.z), new Vector3(wb.max.x, wb.min.y, wb.max.z),
                    new Vector3(wb.min.x, wb.max.y, wb.max.z), new Vector3(wb.max.x, wb.max.y, wb.max.z),
                };
                foreach (Vector3 wc in corners)
                {
                    Vector3 lp = psx.transform.InverseTransformPoint(wc);
                    mn = Vector3.Min(mn, lp);
                    mx = Vector3.Max(mx, lp);
                }

                BoxCollider box = psx.AddComponent<BoxCollider>();
                box.center = (mn + mx) * 0.5f;
                box.size = mx - mn;
            }
        }

        UnityEngine.Object.DestroyImmediate(temp);
        return true;
    }

    static bool TryInstantiatePsxLibreroShelfIntoRow(Transform row, int rowIndex)
    {
        int seed = HashCode.Combine(row != null ? row.name : "", row != null ? row.GetSiblingIndex() : 0) ^ (rowIndex + 1) * 1103;
        return TryInstantiatePsxLibrero(row, "PsxShelfBody", seed, Vector3.zero, Vector3.zero, 0.92f, 2.55f, 5.85f, true);
    }

    /// <summary>Extra Librero shelving/displays along walls and back — uses same PSX pack as aisle rows.</summary>
    public static void BuildLibreroStoreDecor(Transform store)
    {
        if (store == null)
            return;

        Transform old = store.Find("LibreroDecor");
        if (old != null)
            UnityEngine.Object.DestroyImmediate(old.gameObject);

        GameObject root = new GameObject("LibreroDecor");
        root.transform.SetParent(store, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        Vector3[] pos =
        {
            new Vector3(-6.12f, 0f, 10.6f), new Vector3(6.12f, 0f, 10.6f),
            new Vector3(-6.08f, 0f, 13.8f), new Vector3(6.08f, 0f, 13.8f),
            new Vector3(-6.05f, 0f, 16.2f), new Vector3(6.05f, 0f, 16.2f),
            new Vector3(-3.15f, 0f, 16.55f), new Vector3(3.05f, 0f, 16.55f),
            new Vector3(-5.35f, 0f, 9.15f), new Vector3(5.25f, 0f, 9.15f),
            new Vector3(-1.55f, 0f, 11.25f),
        };
        Vector3[] euler =
        {
            new Vector3(0f, 90f, 0f), new Vector3(0f, -90f, 0f),
            new Vector3(0f, 90f, 0f), new Vector3(0f, -90f, 0f),
            new Vector3(0f, 90f, 0f), new Vector3(0f, -90f, 0f),
            new Vector3(0f, 25f, 0f), new Vector3(0f, -25f, 0f),
            new Vector3(0f, 55f, 0f), new Vector3(0f, -55f, 0f),
            new Vector3(0f, 62f, 0f),
        };
        float[] w = { 0.52f, 0.52f, 0.48f, 0.48f, 0.5f, 0.5f, 0.58f, 0.58f, 0.42f, 0.42f, 0.4f };
        float[] h = { 1.95f, 1.95f, 1.82f, 1.82f, 1.88f, 1.88f, 1.92f, 1.92f, 1.55f, 1.55f, 1.5f };
        float[] d = { 0.82f, 0.82f, 0.78f, 0.78f, 0.8f, 0.8f, 0.88f, 0.88f, 0.65f, 0.65f, 0.58f };
        int[] seeds = { 501, 502, 503, 504, 505, 506, 507, 508, 509, 510, 511 };

        for (int i = 0; i < pos.Length; i++)
            TryInstantiatePsxLibrero(root.transform, $"LibreroDecor_{i}", seeds[i], pos[i], euler[i], w[i], h[i], d[i], false);
    }

    static Bounds StoreWildernessClearingBounds(Transform sceneRoot)
    {
        Vector3 center = sceneRoot.TransformPoint(new Vector3(0f, 0f, 8f));
        return new Bounds(center, new Vector3(40f, 32f, 56f));
    }

    /// <summary>Store lot + margin — no TreePrefab scatter may land here (tile-edge spill was filling the parking lot).</summary>
    static Bounds StoreWildernessTreeExclusionBounds(Transform sceneRoot)
    {
        Bounds b = StoreWildernessClearingBounds(sceneRoot);
        b.extents += new Vector3(12f, 48f, 12f);
        return b;
    }

    static bool WildernessTileIntersectsBoundsXZ(Transform tile, float tileSizeX, float tileSizeZ, Bounds worldBounds)
    {
        float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
        Vector3[] corners =
        {
            tile.TransformPoint(Vector3.zero),
            tile.TransformPoint(new Vector3(tileSizeX, 0f, 0f)),
            tile.TransformPoint(new Vector3(0f, 0f, tileSizeZ)),
            tile.TransformPoint(new Vector3(tileSizeX, 0f, tileSizeZ)),
        };
        foreach (Vector3 p in corners)
        {
            minX = Mathf.Min(minX, p.x);
            maxX = Mathf.Max(maxX, p.x);
            minZ = Mathf.Min(minZ, p.z);
            maxZ = Mathf.Max(maxZ, p.z);
        }

        return !(maxX < worldBounds.min.x || minX > worldBounds.max.x ||
                 maxZ < worldBounds.min.z || minZ > worldBounds.max.z);
    }

    static void CarveWildernessClearingUnderStoreLot(Transform wilderness, float tileSizeX, float tileSizeZ, Transform sceneRoot)
    {
        Bounds worldClear = StoreWildernessClearingBounds(sceneRoot);
        worldClear.extents += new Vector3(tileSizeX * 0.55f, 24f, tileSizeZ * 0.55f);

        for (int i = wilderness.childCount - 1; i >= 0; i--)
        {
            Transform tile = wilderness.GetChild(i);
            if (tile == null || !tile.name.StartsWith("Tile_T"))
                continue;
            if (WildernessTileIntersectsBoundsXZ(tile, tileSizeX, tileSizeZ, worldClear))
                UnityEngine.Object.DestroyImmediate(tile.gameObject);
        }
    }

    static void ScatterExtraTreesOnWildernessTiles(Transform wilderness, float tileSizeX, float tileSizeZ, Transform sceneRoot, int extraPerTile)
    {
        GameObject treePrefab = LoadScatterTreePrefab();
        if (treePrefab == null)
            return;

        Bounds noTrees = StoreWildernessTreeExclusionBounds(sceneRoot);

        for (int ti = 0; ti < wilderness.childCount; ti++)
        {
            Transform tile = wilderness.GetChild(ti);
            if (tile == null || !tile.name.StartsWith("Tile_T"))
                continue;

            UnityEngine.Random.InitState(tile.name.GetHashCode() ^ 0x5f3a2c91);
            for (int i = 0; i < extraPerTile; i++)
            {
                const int maxAttempts = 18;
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    Vector3 local = new Vector3(
                        Mathf.Lerp(-tileSizeX * 0.46f, tileSizeX * 0.46f, UnityEngine.Random.value),
                        0.04f,
                        Mathf.Lerp(-tileSizeZ * 0.46f, tileSizeZ * 0.46f, UnityEngine.Random.value));
                    Vector3 world = tile.TransformPoint(local);
                    if (noTrees.Contains(world))
                        continue;

                    GameObject tr = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, tile);
                    tr.transform.localPosition = local;
                    tr.transform.localRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                    float s = UnityEngine.Random.Range(0.85f, 1.15f);
                    tr.transform.localScale = new Vector3(s, s, s);
                    break;
                }
            }
        }
    }

    static void BuildWildernessGrassSkirtAndTrees(Transform sceneRoot)
    {
        GameObject grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SquareTileGrassPrefabPath);
        GameObject treePrefab = LoadScatterTreePrefab();
        if (grassPrefab == null)
            return;

        GameObject skirtGo = new GameObject("WildernessSkirt");
        skirtGo.transform.SetParent(sceneRoot, false);
        skirtGo.transform.localPosition = Vector3.zero;
        skirtGo.transform.localRotation = Quaternion.identity;
        skirtGo.transform.localScale = Vector3.one;

        Bounds clearing = StoreWildernessClearingBounds(sceneRoot);
        Bounds treeExclusion = StoreWildernessTreeExclusionBounds(sceneRoot);
        Vector3 focus = sceneRoot.TransformPoint(new Vector3(0f, 0f, 8f));
        float skirtY = sceneRoot.position.y - 0.12f;

        UnityEngine.Random.InitState(90210);

        float[] radii = { 42f, 56f, 72f, 90f, 112f, 136f, 162f, 190f, 220f };
        foreach (float r in radii)
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(2f * Mathf.PI * r / 8.5f), 24, 56);
            for (int i = 0; i < count; i++)
            {
                float ang = (i + 0.31f * r) * (2f * Mathf.PI / count);
                float jitter = UnityEngine.Random.Range(-2.4f, 2.4f);
                Vector3 wp = focus + new Vector3(Mathf.Cos(ang) * (r + jitter), 0f, Mathf.Sin(ang) * (r + jitter));
                wp.y = skirtY;
                if (clearing.Contains(wp))
                    continue;

                GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(grassPrefab, skirtGo.transform);
                tile.transform.position = wp;
                tile.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                ScatterTreesOnSkirtTile(tile.transform, treePrefab, wp, 3, 6, treeExclusion);
            }
        }

        AddWildernessSkirtRectRings(skirtGo.transform, grassPrefab, treePrefab, clearing, treeExclusion, focus, skirtY);
    }

    static void ScatterTreesOnSkirtTile(Transform tileParent, GameObject treePrefab, Vector3 worldPosForSeed, int minTrees, int maxTrees, Bounds treeExclusionWorld)
    {
        if (treePrefab == null)
            return;
        UnityEngine.Random.InitState((int)(worldPosForSeed.x * 1000f) ^ (int)(worldPosForSeed.z * 1000f) ^ 0x41c6e6f3);
        int treeCount = UnityEngine.Random.Range(minTrees, maxTrees + 1);
        for (int t = 0; t < treeCount; t++)
        {
            const int maxAttempts = 22;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                Vector3 local = new Vector3(
                    UnityEngine.Random.Range(-3.8f, 3.8f),
                    0.04f,
                    UnityEngine.Random.Range(-3.8f, 3.8f));
                Vector3 world = tileParent.TransformPoint(local);
                if (treeExclusionWorld.Contains(world))
                    continue;

                GameObject tr = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, tileParent);
                tr.transform.localPosition = local;
                tr.transform.localRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                float sc = UnityEngine.Random.Range(0.8f, 1.25f);
                tr.transform.localScale = new Vector3(sc, sc, sc);
                break;
            }
        }
    }

    static void AddWildernessSkirtRectRings(Transform skirtParent, GameObject grassPrefab, GameObject treePrefab, Bounds clearing, Bounds treeExclusionWorld, Vector3 focus, float skirtY)
    {
        float[] halfExtentsX = { 128f, 168f, 210f };
        float[] halfExtentsZ = { 142f, 186f, 232f };

        UnityEngine.Random.InitState(77331);
        for (int e = 0; e < halfExtentsX.Length; e++)
        {
            float hx = halfExtentsX[e];
            float hz = halfExtentsZ[e];
            int steps = Mathf.Clamp(Mathf.RoundToInt((hx + hz) * 2f / 9.5f), 40, 120);

            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)steps;
                float px, pz;
                if (t < 0.25f)
                {
                    float u = t / 0.25f;
                    px = Mathf.Lerp(-hx, hx, u);
                    pz = hz;
                }
                else if (t < 0.5f)
                {
                    float u = (t - 0.25f) / 0.25f;
                    px = hx;
                    pz = Mathf.Lerp(hz, -hz, u);
                }
                else if (t < 0.75f)
                {
                    float u = (t - 0.5f) / 0.25f;
                    px = Mathf.Lerp(hx, -hx, u);
                    pz = -hz;
                }
                else
                {
                    float u = (t - 0.75f) / 0.25f;
                    px = -hx;
                    pz = Mathf.Lerp(-hz, hz, u);
                }

                Vector3 wp = focus + new Vector3(px + UnityEngine.Random.Range(-1.8f, 1.8f), 0f, pz + UnityEngine.Random.Range(-1.8f, 1.8f));
                wp.y = skirtY;
                if (clearing.Contains(wp))
                    continue;

                GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(grassPrefab, skirtParent);
                tile.transform.position = wp;
                tile.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                ScatterTreesOnSkirtTile(tile.transform, treePrefab, wp, 2, 5, treeExclusionWorld);
            }
        }
    }

    static void ForestHighwaySegmentRootLocalXZ(out Vector2 segA, out Vector2 segB, out Vector2 dirUnit)
    {
        Vector2 anchor = new Vector2(ForestHighwayAnchorLocal.x, ForestHighwayAnchorLocal.z);
        Vector3 f3 = ForestHighwayHeadingLocal * Vector3.forward;
        dirUnit = new Vector2(f3.x, f3.z);
        if (dirUnit.sqrMagnitude < 1e-8f)
        {
            dirUnit = new Vector2(0f, 1f);
        }
        else
            dirUnit.Normalize();

        segA = anchor - dirUnit * ForestHighwayHalfLength;
        segB = anchor + dirUnit * ForestHighwayHalfLength;
    }

    static float DistancePointXZToForestHighwaySegment(Transform root, Vector3 worldPos)
    {
        Vector3 lp = root.InverseTransformPoint(worldPos);
        return DistancePointXZToForestHighwaySegment(new Vector2(lp.x, lp.z));
    }

    static float DistancePointXZToForestHighwaySegment(Vector2 pRootLocalXZ)
    {
        ForestHighwaySegmentRootLocalXZ(out Vector2 a, out Vector2 b, out _);
        Vector2 ab = b - a;
        float denom = ab.sqrMagnitude;
        if (denom < 1e-8f)
            return Vector2.Distance(pRootLocalXZ, a);
        float t = Vector2.Dot(pRootLocalXZ - a, ab) / denom;
        t = Mathf.Clamp01(t);
        Vector2 closest = a + ab * t;
        return Vector2.Distance(pRootLocalXZ, closest);
    }

    static void CarveForestHighwayWildernessTiles(Transform root, float tileSizeX, float tileSizeZ)
    {
        Transform wild = root.Find("WildernessTerrain");
        if (wild == null)
            return;

        float margin = ForestHighwayClearHalfWidth + ForestHighwayWildernessTileSize * 0.55f;
        for (int i = wild.childCount - 1; i >= 0; i--)
        {
            Transform tile = wild.GetChild(i);
            if (tile == null || !tile.name.StartsWith("Tile_T"))
                continue;
            Vector3 tileCenterW = tile.TransformPoint(new Vector3(tileSizeX * 0.5f, 0f, tileSizeZ * 0.5f));
            if (DistancePointXZToForestHighwaySegment(root, tileCenterW) < margin)
                UnityEngine.Object.DestroyImmediate(tile.gameObject);
        }
    }

    static void CarveForestHighwaySkirtTiles(Transform root)
    {
        Transform skirt = root.Find("WildernessSkirt");
        if (skirt == null)
            return;

        float margin = ForestHighwayClearHalfWidth + 7f;
        for (int i = skirt.childCount - 1; i >= 0; i--)
        {
            Transform tile = skirt.GetChild(i);
            if (tile == null)
                continue;
            if (DistancePointXZToForestHighwaySegment(root, tile.position) < margin)
                UnityEngine.Object.DestroyImmediate(tile.gameObject);
        }
    }

    static void StripForestHighwayTreesInCorridor(Transform root)
    {
        float treeMargin = ForestHighwayClearHalfWidth + 3f;
        List<Transform> toDestroy = new List<Transform>();

        void Collect(Transform t)
        {
            for (int i = 0; i < t.childCount; i++)
            {
                Transform c = t.GetChild(i);
                Collect(c);
                if (!c.gameObject.name.ToLowerInvariant().Contains("tree"))
                    continue;
                if (DistancePointXZToForestHighwaySegment(root, c.position) < treeMargin)
                    toDestroy.Add(c);
            }
        }

        Transform wild = root.Find("WildernessTerrain");
        if (wild != null)
            Collect(wild);
        Transform skirt = root.Find("WildernessSkirt");
        if (skirt != null)
            Collect(skirt);
        Transform bakedTrees = root.Find("WildernessBakedTrees");
        if (bakedTrees != null)
            Collect(bakedTrees);

        for (int i = 0; i < toDestroy.Count; i++)
        {
            if (toDestroy[i] != null)
                UnityEngine.Object.DestroyImmediate(toDestroy[i].gameObject);
        }
    }

    public static void BuildForestHighway(Transform root, Material asphalt, Material concrete, Material line, Material lightMat, Material poleMat)
    {
        if (root == null)
            return;

        Transform existing = root.Find("ForestHighway");
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing.gameObject);

        CarveForestHighwayWildernessTiles(root, ForestHighwayWildernessTileSize, ForestHighwayWildernessTileSize);
        CarveForestHighwaySkirtTiles(root);
        StripForestHighwayTreesInCorridor(root);

        if (asphalt == null)
            return;

        GameObject hw = new GameObject("ForestHighway");
        hw.transform.SetParent(root, false);
        hw.transform.localPosition = new Vector3(ForestHighwayAnchorLocal.x, ForestHighwayRoadSurfaceY, ForestHighwayAnchorLocal.z);
        hw.transform.localRotation = ForestHighwayHeadingLocal;
        hw.transform.localScale = Vector3.one;

        float roadLen = ForestHighwayHalfLength * 2f;
        float roadW = ForestHighwayRoadHalfWidth * 2f;
        CreatePrim("Asphalt_Main", PrimitiveType.Cube, hw.transform, Vector3.zero, Vector3.zero, new Vector3(roadW, 0.1f, roadLen), asphalt, true);

        float shoulderW = ForestHighwayShoulderWidth;
        float shoulderGap = ForestHighwayRoadHalfWidth + shoulderW * 0.5f + ForestHighwayShoulderPastRoadEdge;
        if (concrete != null)
        {
            CreatePrim("Shoulder_L", PrimitiveType.Cube, hw.transform, new Vector3(-shoulderGap, 0.02f, 0f), Vector3.zero, new Vector3(shoulderW, 0.07f, roadLen), concrete, true);
            CreatePrim("Shoulder_R", PrimitiveType.Cube, hw.transform, new Vector3(shoulderGap, 0.02f, 0f), Vector3.zero, new Vector3(shoulderW, 0.07f, roadLen), concrete, true);
        }

        if (line != null)
        {
            int halfSteps = Mathf.CeilToInt(roadLen / (2f * 12f));
            for (int k = -halfSteps; k <= halfSteps; k++)
            {
                float z = k * 12f;
                if (Mathf.Abs(z) > roadLen * 0.48f)
                    continue;
                CreatePrim("CenterDash_" + k, PrimitiveType.Cube, hw.transform, new Vector3(0f, 0.055f, z), Vector3.zero, new Vector3(0.4f, 0.02f, 5f), line, false);
            }
        }

        if (poleMat != null && lightMat != null)
        {
            float lampX = ForestHighwayRoadHalfWidth + 2.6f;
            int li = 0;
            for (float z = -roadLen * 0.5f + 16f; z < roadLen * 0.5f - 10f; z += 26f)
            {
                CreatePrim("HwyLampPole_L_" + li, PrimitiveType.Cylinder, hw.transform, new Vector3(-lampX, 3.2f, z), Vector3.zero, new Vector3(0.1f, 3.2f, 0.1f), poleMat, false);
                CreatePrim("HwyLampHead_L_" + li, PrimitiveType.Cube, hw.transform, new Vector3(-lampX, 6.25f, z), Vector3.zero, new Vector3(0.55f, 0.2f, 0.45f), lightMat, false);
                AddLight(hw.transform, "HwyLampLight_L_" + li, new Vector3(-lampX, 5.9f, z), LightType.Point, new Color(1f, 0.88f, 0.64f), 1.55f, 28f, 0f);

                CreatePrim("HwyLampPole_R_" + li, PrimitiveType.Cylinder, hw.transform, new Vector3(lampX, 3.2f, z), Vector3.zero, new Vector3(0.1f, 3.2f, 0.1f), poleMat, false);
                CreatePrim("HwyLampHead_R_" + li, PrimitiveType.Cube, hw.transform, new Vector3(lampX, 6.25f, z), Vector3.zero, new Vector3(0.55f, 0.2f, 0.45f), lightMat, false);
                AddLight(hw.transform, "HwyLampLight_R_" + li, new Vector3(lampX, 5.9f, z), LightType.Point, new Color(1f, 0.88f, 0.64f), 1.55f, 28f, 0f);
                li++;
            }
        }

        if (!StoreFlowUseBakedWildernessLayout || !File.Exists(StoreFlowBakedWildernessJsonFullPath))
            PlantForestHighwayEdgeTrees(root, hw.transform);
    }

    static void PlantForestHighwayEdgeTrees(Transform sceneRoot, Transform highwayTransform)
    {
        GameObject treePrefab = LoadScatterTreePrefab();
        if (treePrefab == null)
            return;

        float roadLen = ForestHighwayHalfLength * 2f;
        float shoulderW = ForestHighwayShoulderWidth;
        float edgeX = ForestHighwayRoadHalfWidth + shoulderW + 0.95f;
        const float spacing = 9.5f;
        int steps = Mathf.Max(2, Mathf.FloorToInt(roadLen / spacing));

        GameObject flankRoot = new GameObject("ForestHighway_EdgeTrees");
        flankRoot.transform.SetParent(sceneRoot, false);
        flankRoot.transform.localPosition = Vector3.zero;
        flankRoot.transform.localRotation = Quaternion.identity;
        flankRoot.transform.localScale = Vector3.one;

        UnityEngine.Random.InitState(884422);

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float zLocal = Mathf.Lerp(-roadLen * 0.5f + 5f, roadLen * 0.5f - 6f, t);
            for (int s = 0; s < 2; s++)
            {
                int sideSign = (s == 0) ? -1 : 1;
                Vector3 localOnHw = new Vector3(
                    sideSign * edgeX + UnityEngine.Random.Range(-0.55f, 0.55f),
                    ForestHighwayRoadSurfaceY + 0.04f,
                    zLocal + UnityEngine.Random.Range(-1.4f, 1.4f));
                Vector3 worldPos = highwayTransform.TransformPoint(localOnHw);
                if (StoreWildernessTreeExclusionBounds(sceneRoot).Contains(worldPos))
                    continue;

                GameObject tr = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, flankRoot.transform);
                tr.transform.position = worldPos;
                tr.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                float sc = UnityEngine.Random.Range(0.82f, 1.22f);
                tr.transform.localScale = new Vector3(sc, sc, sc);
            }
        }
    }

    static void BuildGround(Transform root, Material asphalt, Material line, Material concrete)
    {
        GameObject ground = new GameObject("Ground");
        ground.transform.SetParent(root, false);

        CreatePrim("ParkingLot", PrimitiveType.Cube, ground.transform, new Vector3(0f, -0.05f, 3f), Vector3.zero, new Vector3(24f, 0.1f, 18f), asphalt, true);
        CreatePrim("Road", PrimitiveType.Cube, ground.transform, new Vector3(0f, -0.04f, 14f), Vector3.zero, new Vector3(28f, 0.08f, 4f), asphalt, true);
        CreatePrim("Sidewalk", PrimitiveType.Cube, ground.transform, new Vector3(0f, 0.03f, 9.1f), Vector3.zero, new Vector3(14f, 0.12f, 2.2f), concrete, true);
        GameObject floorCol = CreatePrim("StoreInteriorFloorCollider", PrimitiveType.Cube, ground.transform, new Vector3(0f, 0.03f, 12.86f), Vector3.zero, new Vector3(14f, 0.12f, 10f), null, true);
        Renderer floorColR = floorCol.GetComponent<Renderer>();
        if (floorColR != null)
            floorColR.enabled = false;

        for (int i = 0; i < 7; i++)
        {
            CreatePrim($"RoadDash{i}", PrimitiveType.Cube, ground.transform,
                new Vector3(-10.8f + i * 3.6f, 0.005f, 14f), Vector3.zero, new Vector3(1.5f, 0.01f, 0.1f), line, false);
        }

        BuildParkingStripesOnGround(ground.transform, line);
    }

    /// <summary>White parking bay stripes on the asphalt (matches <see cref="BuildStreetProps"/> block spacing).</summary>
    public static void BuildParkingStripesOnGround(Transform ground, Material line)
    {
        if (ground == null || line == null)
            return;

        const float lineY = 0.008f;
        const float zCenter = 7.85f;
        const float zLen = 7.5f;
        const float backZ = 11.35f;

        CreatePrim("ParkingStripe_L", PrimitiveType.Cube, ground, new Vector3(-11.85f, lineY, zCenter), Vector3.zero, new Vector3(0.1f, 0.012f, zLen), line, false);
        CreatePrim("ParkingStripe_R", PrimitiveType.Cube, ground, new Vector3(11.85f, lineY, zCenter), Vector3.zero, new Vector3(0.1f, 0.012f, zLen), line, false);

        for (int i = 0; i < 7; i++)
        {
            float x = -9.45f + i * 3.1f;
            CreatePrim($"ParkingDivider_{i}", PrimitiveType.Cube, ground, new Vector3(x, lineY, zCenter), Vector3.zero, new Vector3(0.08f, 0.012f, zLen), line, false);
        }

        CreatePrim("ParkingBackLine", PrimitiveType.Cube, ground, new Vector3(0f, lineY, backZ), Vector3.zero, new Vector3(23.5f, 0.012f, 0.1f), line, false);
    }

    static void BuildStreetProps(Transform root, Material concrete, Material trim, Material lightMat, Material poleMat = null)
    {
        GameObject props = new GameObject("StreetProps");
        props.transform.SetParent(root, false);

        if (poleMat == null)
            poleMat = CreateTexturedLitFromPath("StoreFlow_Pole", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Metal_10.jpg", Color.white, new Vector2(1f, 2f));
        Material posterMat = CreateTexturedLitFromPath("StoreFlow_Poster", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/outdoor electronics.jpg", Color.white, new Vector2(1f, 1f));
        Material railMat = CreateTexturedLitFromPath("StoreFlow_Rail", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/RoadRail.jpg", Color.white, new Vector2(4f, 1f));

        float lampZ = StoreFlowParkingLampRowZ;
        for (int i = 0; i < 4; i++)
        {
            float x = -12f + i * 8f;
            CreatePrim($"LampPole_{i}", PrimitiveType.Cylinder, props.transform, new Vector3(x, 3.2f, lampZ), Vector3.zero, new Vector3(0.1f, 3.2f, 0.1f), poleMat, false);
            CreatePrim($"LampHead_{i}", PrimitiveType.Cube, props.transform, new Vector3(x, 6.25f, lampZ), Vector3.zero, new Vector3(0.55f, 0.2f, 0.45f), lightMat, false);
            AddLight(props.transform, $"LampLight_{i}", new Vector3(x, 5.9f, lampZ), LightType.Point, new Color(1f, 0.88f, 0.64f), 1.35f, 11f, 0f);
        }

        float railZ = StoreFlowGuardRailCenterZ;
        float railLen = StoreFlowGuardRailLengthZ;
        CreatePrim("GuardRailL", PrimitiveType.Cube, props.transform, new Vector3(-13.8f, 0.55f, railZ), Vector3.zero, new Vector3(0.24f, 1.1f, railLen), railMat, false);
        CreatePrim("GuardRailR", PrimitiveType.Cube, props.transform, new Vector3(13.8f, 0.55f, railZ), Vector3.zero, new Vector3(0.24f, 1.1f, railLen), railMat, false);

        for (int i = 0; i < 8; i++)
        {
            float x = -11f + i * 3.1f;
            CreatePrim($"ParkingBlock_{i}", PrimitiveType.Cube, props.transform, new Vector3(x, 0.06f, 8.25f), Vector3.zero, new Vector3(1.3f, 0.12f, 0.36f), concrete, false);
        }

        CreatePrim("PosterBoard", PrimitiveType.Cube, props.transform, new Vector3(-6.1f, 2.2f, 8.15f), Vector3.zero, new Vector3(2.2f, 2.4f, 0.08f), trim, false);
        CreatePrim("PosterFace", PrimitiveType.Quad, props.transform, new Vector3(-6.1f, 2.2f, 8.21f), new Vector3(0f, 180f, 0f), new Vector3(2f, 2.2f, 1f), posterMat, false);
    }

    static void BuildSnowfall(Transform root)
    {
        GameObject snow = new GameObject("SnowfallRuntime");
        snow.transform.SetParent(root, false);
        snow.transform.localPosition = new Vector3(0f, 15f, 0f);

        ParticleSystem ps = snow.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 14f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.09f);
        main.maxParticles = 2600;
        main.startColor = new Color(0.95f, 0.97f, 1f, 0.82f);

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 210f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(95f, 1f, 95f);

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.2f;

        ParticleSystemRenderer render = ps.GetComponent<ParticleSystemRenderer>();
        render.material = CreateSnowMaterial();
        render.sortingOrder = 5;
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        render.receiveShadows = false;
    }

    static GameObject LoadSuperMarketPackPrefab()
    {
        string pathByGuid = AssetDatabase.GUIDToAssetPath(SuperMarketPackAssetGuid);
        string path = string.IsNullOrEmpty(pathByGuid) ? SuperMarketPackGlbPath : pathByGuid;
        return LoadSuperMarketPackPrefabFromPath(path);
    }

    /// <summary>Loads the root GameObject for a GLB/GLTF model asset (main prefab or first suitable sub-asset).</summary>
    public static GameObject LoadSuperMarketPackPrefabFromPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (go != null)
            return go;

        UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
        if (all == null || all.Length == 0)
        {
            Debug.LogWarning(
                "Store Flow: no sub-assets at \"" + path + "\". Reimport the model or check ModelImporter.");
            return null;
        }

        var gameObjects = new List<GameObject>(all.Length);
        foreach (UnityEngine.Object o in all)
        {
            if (o is GameObject g)
                gameObjects.Add(g);
        }

        if (gameObjects.Count == 0)
        {
            Debug.LogWarning(
                "Store Flow: \"" + path + "\" has no GameObject sub-assets (meshes/materials only?). Reimport as Model.");
            return null;
        }

        foreach (GameObject g in gameObjects)
        {
            if (g.transform.parent == null)
                return g;
        }

        // GLTF/GLB: Unity may not expose a single root with parent == null in the sub-asset list; walk up from geometry.
        foreach (GameObject g in gameObjects)
        {
            if (g.GetComponentInChildren<Renderer>(true) == null)
                continue;
            Transform t = g.transform;
            while (t.parent != null)
                t = t.parent;
            return t.gameObject;
        }

        // Last resort: first GameObject (often the scene root for Sketchfab exports).
        return gameObjects[0];
    }

    static Bounds RendererBoundsInParentSpace(Transform root, Transform parentSpace)
    {
        Renderer[] rends = root.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0)
            return new Bounds(parentSpace.InverseTransformPoint(root.position), Vector3.zero);

        bool init = false;
        Bounds acc = default;
        foreach (Renderer r in rends)
        {
            Bounds wb = r.bounds;
            Vector3[] corners =
            {
                new Vector3(wb.min.x, wb.min.y, wb.min.z),
                new Vector3(wb.min.x, wb.min.y, wb.max.z),
                new Vector3(wb.min.x, wb.max.y, wb.min.z),
                new Vector3(wb.min.x, wb.max.y, wb.max.z),
                new Vector3(wb.max.x, wb.min.y, wb.min.z),
                new Vector3(wb.max.x, wb.min.y, wb.max.z),
                new Vector3(wb.max.x, wb.max.y, wb.min.z),
                new Vector3(wb.max.x, wb.max.y, wb.max.z),
            };
            foreach (Vector3 c in corners)
            {
                Vector3 lp = parentSpace.InverseTransformPoint(c);
                if (!init)
                {
                    acc = new Bounds(lp, Vector3.zero);
                    init = true;
                }
                else
                    acc.Encapsulate(lp);
            }
        }
        return acc;
    }

    /// <summary>Non-uniform scale + position so the GLB fills the procedural interior footprint (doors at storefront Z, floor at y≈0). Uniform min-scale often leaves a tiny or misaligned interior.</summary>
    public static void FitSuperMarketPackInStoreInterior(Transform store, Transform pack)
    {
        pack.localPosition = Vector3.zero;
        pack.localRotation = Quaternion.Euler(0f, SuperMarketPackExtraYawDegrees, 0f);
        pack.localScale = Vector3.one;

        const float targetWidth = 13.8f;
        const float targetDepth = 9.8f;
        const float targetHeight = 4.1f;
        const float storefrontZ = 8.1f;

        Bounds b = RendererBoundsInParentSpace(pack, store);
        if (b.size.sqrMagnitude < 1e-6f)
        {
            pack.localScale = Vector3.one;
            pack.localPosition = new Vector3(0f, 0f, 12f);
            return;
        }

        Vector3 bs = b.size;
        bs.x = Mathf.Max(bs.x, 0.001f);
        bs.y = Mathf.Max(bs.y, 0.001f);
        bs.z = Mathf.Max(bs.z, 0.001f);

        pack.localScale = new Vector3(
            targetWidth / bs.x,
            targetHeight / bs.y,
            targetDepth / bs.z);

        b = RendererBoundsInParentSpace(pack, store);
        float yLift = -b.min.y;
        float zPush = storefrontZ - b.max.z;
        pack.localPosition = new Vector3(-b.center.x, yLift, zPush);
    }

    public static void BuildSuperMarketSlidingDoors(Transform store, Material glass, Material doorFrame, out Transform leftDoor, out Transform rightDoor)
    {
        GameObject doorAssembly = new GameObject("SlidingDoors");
        doorAssembly.transform.SetParent(store, false);
        CreatePrim("DoorHeader", PrimitiveType.Cube, doorAssembly.transform, new Vector3(0f, 2.55f, 8.04f), Vector3.zero, new Vector3(2.8f, 0.25f, 0.32f), doorFrame, true);
        CreatePrim("DoorFrameLeft", PrimitiveType.Cube, doorAssembly.transform, new Vector3(-1.45f, 1.2f, 8.04f), Vector3.zero, new Vector3(0.14f, 2.45f, 0.32f), doorFrame, true);
        CreatePrim("DoorFrameRight", PrimitiveType.Cube, doorAssembly.transform, new Vector3(1.45f, 1.2f, 8.04f), Vector3.zero, new Vector3(0.14f, 2.45f, 0.32f), doorFrame, true);

        GameObject leftDoorGo = CreatePrim("LeftDoor", PrimitiveType.Cube, doorAssembly.transform, new Vector3(-0.52f, 1.2f, 8.05f), Vector3.zero, new Vector3(0.96f, 2.2f, 0.08f), glass, false);
        GameObject rightDoorGo = CreatePrim("RightDoor", PrimitiveType.Cube, doorAssembly.transform, new Vector3(0.52f, 1.2f, 8.05f), Vector3.zero, new Vector3(0.96f, 2.2f, 0.08f), glass, false);
        leftDoor = leftDoorGo.transform;
        rightDoor = rightDoorGo.transform;
    }

    static bool TryBuildSuperMarketPackInterior(Transform store, Material glass, Material doorFrame, Material lightMat, out Transform leftDoor, out Transform rightDoor)
    {
        return TryBuildSuperMarketPackInteriorAtPath(store, null, glass, doorFrame, lightMat, out leftDoor, out rightDoor);
    }

    /// <param name="modelAssetPath">When null or empty, uses the default магазин.glb (or GUID override).</param>
    static bool TryBuildSuperMarketPackInteriorAtPath(Transform store, string modelAssetPath, Material glass, Material doorFrame, Material lightMat, out Transform leftDoor, out Transform rightDoor)
    {
        leftDoor = rightDoor = null;
        GameObject prefab = string.IsNullOrEmpty(modelAssetPath) ? LoadSuperMarketPackPrefab() : LoadSuperMarketPackPrefabFromPath(modelAssetPath);
        if (prefab == null)
        {
            string hint = string.IsNullOrEmpty(modelAssetPath) ? SuperMarketPackGlbPath : modelAssetPath;
            Debug.LogWarning("Store Flow: SuperMarket model not found or not imported — " + hint);
            return false;
        }

        BuildSuperMarketSlidingDoors(store, glass, doorFrame, out leftDoor, out rightDoor);

        GameObject pack = PrefabUtility.InstantiatePrefab(prefab, store) as GameObject;
        if (pack == null)
            pack = UnityEngine.Object.Instantiate(prefab, store);
        pack.name = "SuperMarketPack";
        pack.transform.SetAsFirstSibling();

        FitSuperMarketPackInStoreInterior(store, pack.transform);

        if (pack.GetComponentsInChildren<Collider>(true).Length == 0)
        {
            GameObject walkCol = CreatePrim("SuperMarketWalkCollider", PrimitiveType.Cube, store, new Vector3(0f, 0.04f, 12.86f), Vector3.zero, new Vector3(13.6f, 0.12f, 9.6f), null, true);
            Renderer wr = walkCol.GetComponent<Renderer>();
            if (wr != null)
                wr.enabled = false;
        }

        float[] lightZ = { 10.5f, 12.5f, 14.5f, 16.2f };
        for (int li = 0; li < lightZ.Length; li++)
        {
            float z = lightZ[li];
            AddSpotLightAimedAt(store, "PackFillLight_" + li, new Vector3(0f, 3.2f, z), new Vector3(0f, 0.1f, z), new Color(1f, 0.98f, 0.92f), 0.42f, 8f, 75f);
        }

        Debug.Log(
            string.IsNullOrEmpty(modelAssetPath)
                ? "Store Flow: interior from super-market-low-poly-for-free (магазин.glb). Tweak FitSuperMarketPackInStoreInterior if alignment is off."
                : "Store Flow: interior from \"" + modelAssetPath + "\". Tweak FitSuperMarketPackInStoreInterior if alignment is off.");
        return true;
    }

    [MenuItem("OPEN FEED/Store Flow/Replace Store With scene.gltf (Super Market)", false, 25)]
    public static void ReplaceStoreWithSuperMarketGltfSceneMenu()
    {
        ReplaceStoreWithSuperMarketGltfScene();
    }

    /// <summary>
    /// Replaces <c>Store</c> under <c>StoreFlowScene</c> with <see cref="SuperMarketGltfSceneAssetPath"/>, using the same fit and sliding doors as the generator.
    /// Marks the scene dirty; does not save.
    /// </summary>
    public static void ReplaceStoreWithSuperMarketGltfScene()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            Debug.LogError("Store Flow: StoreFlowScene not found in the active scene.");
            return;
        }

        AssetDatabase.ImportAsset(SuperMarketGltfSceneAssetPath, ImportAssetOptions.ForceUpdate);

        Transform existingStore = root.transform.Find("Store");
        if (existingStore != null)
            UnityEngine.Object.DestroyImmediate(existingStore.gameObject);

        Material glass = CreateTransparent("StoreFlow_Glass", new Color(0.72f, 0.88f, 0.96f, 0.22f));
        Material doorFrame = CreateLit("StoreFlow_DoorFrame", new Color(0.14f, 0.15f, 0.18f));
        Material lightMat = CreateEmissive("StoreFlow_Light", new Color(1f, 0.96f, 0.88f), 1.25f);

        GameObject storeGO = new GameObject("Store");
        storeGO.transform.SetParent(root.transform, false);

        if (!TryBuildSuperMarketPackInteriorAtPath(storeGO.transform, SuperMarketGltfSceneAssetPath, glass, doorFrame, lightMat, out _, out _))
        {
            UnityEngine.Object.DestroyImmediate(storeGO);
            Debug.LogError("Store Flow: Could not build Store from " + SuperMarketGltfSceneAssetPath + ". Reimport the GLTF in the Project window.");
            return;
        }

        StoreFlowTargetedRebuild.WireIntroAndDoors(root, storeGO.transform);

        EditorSceneManager.MarkSceneDirty(storeGO.scene);
        Selection.activeGameObject = storeGO;
        Debug.Log("Store Flow: Store replaced with scene.gltf (scene marked dirty; not saved).");
    }

    static void BuildStore(Transform root, Material wall, Material roof, Material sign, Material glass, Material doorFrame,
        Material floor, Material floorStripe, Material shelf, Material trim,
        Material productA, Material productB, Material productC, Material productD, Material productE, Material productF,
        Material lightMat, Material freezerBody, Material freezerTrim, Material freezerGlow, Material counter,
        Material counterTop, Material register, Material registerGlow, Material vendingBody, Material vendingGlow,
        out Transform leftDoor, out Transform rightDoor)
    {
        GameObject store = new GameObject("Store");
        store.transform.SetParent(root, false);

        if (StoreFlowUseSuperMarketPackInterior && TryBuildSuperMarketPackInterior(store.transform, glass, doorFrame, lightMat, out leftDoor, out rightDoor))
            return;

        BuildStorePrimitiveInterior(store.transform, wall, roof, sign, glass, doorFrame, floor, floorStripe, shelf, trim,
            productA, productB, productC, productD, productE, productF, lightMat, freezerBody, freezerTrim, freezerGlow,
            counter, counterTop, register, registerGlow, vendingBody, vendingGlow, out leftDoor, out rightDoor);
    }

    static void BuildStorePrimitiveInterior(Transform store, Material wall, Material roof, Material sign, Material glass, Material doorFrame,
        Material floor, Material floorStripe, Material shelf, Material trim,
        Material productA, Material productB, Material productC, Material productD, Material productE, Material productF,
        Material lightMat, Material freezerBody, Material freezerTrim, Material freezerGlow, Material counter,
        Material counterTop, Material register, Material registerGlow, Material vendingBody, Material vendingGlow,
        out Transform leftDoor, out Transform rightDoor)
    {
        CreatePrim("BackWall", PrimitiveType.Cube, store, new Vector3(0f, 2f, 17.7f), Vector3.zero, new Vector3(14f, 4f, 0.24f), wall, true);
        CreatePrim("LeftWall", PrimitiveType.Cube, store.transform, new Vector3(-6.88f, 2f, 12.8f), Vector3.zero, new Vector3(0.24f, 4f, 10f), wall, true);
        CreatePrim("RightWall", PrimitiveType.Cube, store.transform, new Vector3(6.88f, 2f, 12.8f), Vector3.zero, new Vector3(0.24f, 4f, 10f), wall, true);
        CreatePrim("FrontWallLeft", PrimitiveType.Cube, store.transform, new Vector3(-4.2f, 2f, 8.02f), Vector3.zero, new Vector3(5.6f, 4f, 0.24f), wall, true);
        CreatePrim("FrontWallRight", PrimitiveType.Cube, store.transform, new Vector3(4.2f, 2f, 8.02f), Vector3.zero, new Vector3(5.6f, 4f, 0.24f), wall, true);
        CreatePrim("Roof", PrimitiveType.Cube, store.transform, new Vector3(0f, 4.05f, 12.8f), Vector3.zero, new Vector3(14.2f, 0.24f, 10.4f), roof, true);
        CreatePrim("InteriorFloor", PrimitiveType.Cube, store.transform, new Vector3(0f, 0.02f, 12.86f), Vector3.zero, new Vector3(13.8f, 0.08f, 9.8f), floor, true);
        float[] floorStripeZ = { 9.1f, 10.3f, 11.5f, 12.7f, 13.9f, 15.1f, 16.3f };
        for (int i = 0; i < floorStripeZ.Length; i++)
        {
            CreatePrim($"FloorStripe_{i}", PrimitiveType.Cube, store.transform,
                new Vector3(0f, 0.07f, floorStripeZ[i]), Vector3.zero, new Vector3(13.2f, 0.01f, 0.42f), floorStripe, false);
        }
        CreatePrim("SignBand", PrimitiveType.Cube, store.transform, new Vector3(0f, 3.1f, 8f), Vector3.zero, new Vector3(10f, 0.6f, 0.3f), sign, false);
        CreatePrim("TopTrim", PrimitiveType.Cube, store.transform, new Vector3(0f, 3.6f, 8f), Vector3.zero, new Vector3(10.8f, 0.12f, 0.36f), trim, false);

        // Large display windows flanking the entrance.
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
        CreatePrim("DoorHeader", PrimitiveType.Cube, doorAssembly.transform, new Vector3(0f, 2.55f, 8.04f), Vector3.zero, new Vector3(2.8f, 0.25f, 0.32f), doorFrame, true);
        CreatePrim("DoorFrameLeft", PrimitiveType.Cube, doorAssembly.transform, new Vector3(-1.45f, 1.2f, 8.04f), Vector3.zero, new Vector3(0.14f, 2.45f, 0.32f), doorFrame, true);
        CreatePrim("DoorFrameRight", PrimitiveType.Cube, doorAssembly.transform, new Vector3(1.45f, 1.2f, 8.04f), Vector3.zero, new Vector3(0.14f, 2.45f, 0.32f), doorFrame, true);

        GameObject leftDoorGo = CreatePrim("LeftDoor", PrimitiveType.Cube, doorAssembly.transform, new Vector3(-0.52f, 1.2f, 8.05f), Vector3.zero, new Vector3(0.96f, 2.2f, 0.08f), glass, false);
        GameObject rightDoorGo = CreatePrim("RightDoor", PrimitiveType.Cube, doorAssembly.transform, new Vector3(0.52f, 1.2f, 8.05f), Vector3.zero, new Vector3(0.96f, 2.2f, 0.08f), glass, false);
        leftDoor = leftDoorGo.transform;
        rightDoor = rightDoorGo.transform;

        BuildZAlignedShelfRows(store.transform, shelf, trim, productA, productB, productC, productD, productE, productF);
        BuildBackFreezers(store.transform, freezerBody, freezerTrim, freezerGlow, glass, productA, productB, productC, productD);
        BuildCheckoutCornerFrontLeft(store.transform, counter, counterTop, register, registerGlow, trim, productA, productB, productC);
        BuildVendingMachineFrontRight(store.transform, vendingBody, vendingGlow, glass);
        BuildLibreroStoreDecor(store.transform);

        float[] interiorLightZ = { 10.4f, 11.65f, 12.9f, 14.15f, 15.4f, 16.65f };
        float[] aisleCenterX = { -2.8f, 0f, 2.8f };
        for (int i = 0; i < interiorLightZ.Length; i++)
        {
            float z = interiorLightZ[i];
            for (int a = 0; a < aisleCenterX.Length; a++)
            {
                float ax = aisleCenterX[a];
                CreatePrim($"LightStrip_{i}_{a}", PrimitiveType.Cube, store.transform, new Vector3(ax, 3.35f, z), Vector3.zero, new Vector3(0.22f, 0.06f, 5.6f), lightMat, false);
                AddSpotLightAimedAt(store.transform, $"InteriorLight_{i}_{a}", new Vector3(ax, 3.05f, z), new Vector3(ax, 0.15f, z), new Color(1f, 0.98f, 0.9f), 0.38f, 6.5f, 68f);
            }
        }

        AddSpotLightAimedAt(store.transform, "AisleLight_L", new Vector3(-4.2f, 2.8f, 12.9f), new Vector3(-5.75f, 0.12f, 12.9f), new Color(1f, 0.97f, 0.88f), 0.34f, 5.5f, 58f);
        AddSpotLightAimedAt(store.transform, "AisleLight_R", new Vector3(4.2f, 2.8f, 12.9f), new Vector3(5.75f, 0.12f, 12.9f), new Color(1f, 0.97f, 0.88f), 0.34f, 5.5f, 58f);
    }

    static void BuildZAlignedShelfRows(Transform store, Material shelf, Material trim,
        Material productA, Material productB, Material productC, Material productD, Material productE, Material productF)
    {
        string[] rowNames = { "ShelfRow_B", "ShelfRow_C", "ShelfRow_D" };
        float[] rowX = { -1.4f, 1.4f, 4.2f };
        for (int r = 0; r < rowNames.Length; r++)
            BuildZShelfRow(store, rowNames[r], rowX[r], r, shelf, trim, productA, productB, productC, productD, productE, productF);
    }

    static void BuildZShelfRow(Transform store, string rowName, float rowX, int rowIndex, Material shelf, Material trim,
        Material productA, Material productB, Material productC, Material productD, Material productE, Material productF)
    {
        Material[] mats = { productA, productB, productC, productD, productE, productF };
        GameObject row = new GameObject(rowName);
        row.transform.SetParent(store, false);
        row.transform.localPosition = new Vector3(rowX, 0f, 12.9f);

        if (!TryInstantiatePsxLibreroShelfIntoRow(row.transform, rowIndex))
        {
            CreatePrim("Spine", PrimitiveType.Cube, row.transform, new Vector3(0f, 1.25f, 0f), Vector3.zero, new Vector3(0.12f, 2.5f, 5.8f), shelf, true);
            CreatePrim("ShelfBase", PrimitiveType.Cube, row.transform, new Vector3(0f, 0.18f, 0.2f), Vector3.zero, new Vector3(0.9f, 0.18f, 5.4f), shelf, true);
        }

        float[] levelY = { 0.45f, 0.93f, 1.41f, 1.89f };
        for (int lv = 0; lv < 4; lv++)
        {
            float y = levelY[lv];
            Transform rowT = row.transform;
            if (rowT.Find("PsxShelfBody") == null)
            {
                CreatePrim($"Shelf_{lv}", PrimitiveType.Cube, rowT, new Vector3(0f, y, 0.1f), Vector3.zero, new Vector3(0.86f, 0.08f, 5.5f), shelf, true);
                CreatePrim($"ShelfLip_{lv}_Front", PrimitiveType.Cube, rowT, new Vector3(0f, y + 0.04f, 2.75f), Vector3.zero, new Vector3(0.86f, 0.12f, 0.05f), trim, false);
                CreatePrim($"ShelfLip_{lv}_Back", PrimitiveType.Cube, rowT, new Vector3(0f, y + 0.04f, -2.75f), Vector3.zero, new Vector3(0.86f, 0.12f, 0.05f), trim, false);
            }

            if (StoreFlowPlaceProductGeometry)
            {
                if (lv < 3)
                {
                    float[] zs = { -2.4f, -1.2f, 0f, 1.2f, 2.4f };
                    for (int zi = 0; zi < zs.Length; zi++)
                        PlaceZRowShelfProductsBothSides(row.transform, rowIndex, lv, y, zs[zi], zi, mats, trim, false);
                }
                else
                {
                    float[] zsTop = { -1.2f, 0f, 1.2f };
                    for (int zi = 0; zi < zsTop.Length; zi++)
                        PlaceZRowShelfProductsBothSides(row.transform, rowIndex, lv, y, zsTop[zi], zi, mats, trim, true);
                }
            }
        }
    }

    static void PlaceZRowShelfProductsBothSides(Transform row, int rowIndex, int level, float shelfY, float z, int zSlot, Material[] mats, Material trim, bool topShelf)
    {
        float py = shelfY + 0.11f;
        float scaleMul = topShelf ? 1.15f : 1f;
        int pattern = topShelf ? zSlot % 3 : zSlot % 5;
        int matPick = (zSlot + level * 2 + rowIndex * 3) % 6;

        PlaceZRowSideProduct(row, true, py, z, pattern, matPick, mats, trim, scaleMul, level, zSlot, topShelf);
        PlaceZRowSideProduct(row, false, py, z, pattern, matPick, mats, trim, scaleMul, level, zSlot, topShelf);
    }

    static void PlaceZRowSideProduct(Transform row, bool left, float py, float z, int pattern, int matIndex, Material[] mats, Material trim, float scaleMul, int level, int zSlot, bool topShelf)
    {
        float xCenter = left ? -0.35f : 0.35f;
        float labelX = left ? -0.42f : 0.42f;
        float labelSign = left ? -1f : 1f;
        Material mat = mats[(matIndex + (left ? 0 : 1)) % mats.Length];
        string suffix = $"_{level}_{zSlot}_{(left ? "L" : "R")}";

        if (topShelf)
        {
            switch (pattern)
            {
                case 0:
                    CreatePrim("Product" + suffix + "_Box", PrimitiveType.Cube, row, new Vector3(xCenter, py, z), Vector3.zero,
                        new Vector3(0.14f, 0.26f, 0.12f) * scaleMul, mat, false);
                    CreatePrim("Product" + suffix + "_Label", PrimitiveType.Cube, row, new Vector3(labelX, py, z), Vector3.zero,
                        new Vector3(0.12f, 0.22f, 0.02f), mats[(matIndex + 1) % mats.Length], false);
                    break;
                case 1:
                    CreatePrim("Product" + suffix + "_Can", PrimitiveType.Cylinder, row, new Vector3(xCenter, py + 0.02f * scaleMul, z), new Vector3(90f, 0f, 0f),
                        new Vector3(0.08f, 0.14f, 0.08f) * scaleMul, mat, false);
                    break;
                default:
                    CreatePrim("Product" + suffix + "_Cap", PrimitiveType.Capsule, row, new Vector3(xCenter, py + 0.05f * scaleMul, z), Vector3.zero,
                        new Vector3(0.08f, 0.16f, 0.08f) * scaleMul, mat, false);
                    CreatePrim("Product" + suffix + "_CapLabel", PrimitiveType.Cube, row, new Vector3(xCenter, py + 0.05f * scaleMul, z + 0.02f * labelSign), Vector3.zero,
                        new Vector3(0.09f, 0.09f, 0.02f), mats[(matIndex + 2) % mats.Length], false);
                    break;
            }

            return;
        }

        switch (pattern)
        {
            case 0:
                CreatePrim("Product" + suffix + "_Box", PrimitiveType.Cube, row, new Vector3(xCenter, py, z), Vector3.zero,
                    new Vector3(0.14f, 0.26f, 0.12f), mat, false);
                CreatePrim("Product" + suffix + "_Label", PrimitiveType.Cube, row, new Vector3(labelX, py, z), Vector3.zero,
                    new Vector3(0.12f, 0.22f, 0.02f), mats[(matIndex + 1) % mats.Length], false);
                break;
            case 1:
                CreatePrim("Product" + suffix + "_Can", PrimitiveType.Cylinder, row, new Vector3(xCenter, py + 0.03f, z), new Vector3(90f, 0f, 0f),
                    new Vector3(0.08f, 0.14f, 0.08f), mat, false);
                break;
            case 2:
                CreatePrim("Product" + suffix + "_Cap", PrimitiveType.Capsule, row, new Vector3(xCenter, py + 0.05f, z), Vector3.zero,
                    new Vector3(0.08f, 0.16f, 0.08f), trim, false);
                CreatePrim("Product" + suffix + "_CapLabel", PrimitiveType.Cube, row, new Vector3(xCenter, py + 0.05f, z + 0.02f * labelSign), Vector3.zero,
                    new Vector3(0.09f, 0.09f, 0.02f), mat, false);
                break;
            case 3:
                CreatePrim("Product" + suffix + "_Box", PrimitiveType.Cube, row, new Vector3(xCenter, py, z), Vector3.zero,
                    new Vector3(0.14f, 0.26f, 0.12f), trim, false);
                CreatePrim("Product" + suffix + "_Front", PrimitiveType.Cube, row, new Vector3(xCenter, py, z + 0.05f * labelSign), Vector3.zero,
                    new Vector3(0.12f, 0.22f, 0.02f), mat, false);
                break;
            default:
                CreatePrim("Product" + suffix + "_Can", PrimitiveType.Cylinder, row, new Vector3(xCenter, py + 0.03f, z), new Vector3(90f, 0f, 0f),
                    new Vector3(0.08f, 0.14f, 0.08f), mat, false);
                break;
        }
    }

    static void BuildCar(Transform parent, GameObject subaruModelAsset, bool useSubaruTextures, Material bodyMat, Material trimMat, Material glassMat, Material tireMat,
        Material wheelMat, Material headlightMat, Material taillightMat, Material interiorMat)
    {
        GameObject carPrefab = subaruModelAsset ?? AssetDatabase.LoadAssetAtPath<GameObject>(RenaultFbxPath);

        if (carPrefab != null)
        {
            GameObject car = (GameObject)PrefabUtility.InstantiatePrefab(carPrefab);
            car.name = useSubaruTextures ? "Subaru_Impreza" : "PSX_Renault";
            car.transform.SetParent(parent, false);
            if (useSubaruTextures)
                car.transform.localPosition = SubaruImprezaLocalPosition;
            else
                car.transform.localPosition = Vector3.zero;
            car.transform.localRotation = Quaternion.identity;
            car.transform.localScale = Vector3.one;
            ApplyCarMaterials(car, bodyMat, trimMat, glassMat, tireMat, wheelMat, headlightMat, taillightMat, interiorMat);
            return;
        }

        Debug.LogWarning("Store Flow Scene: No car model (Subaru or Renault). Extract the Subaru archive to subaru-impreza and re-run. Using procedural car.");
        CreatePrim("BodyLower", PrimitiveType.Cube, parent, new Vector3(0f, 0.42f, 0f), Vector3.zero, new Vector3(1.86f, 0.52f, 4.18f), bodyMat);
        CreatePrim("BodyUpper", PrimitiveType.Cube, parent, new Vector3(0f, 0.92f, -0.12f), Vector3.zero, new Vector3(1.56f, 0.56f, 2.18f), bodyMat);
        CreatePrim("Hood", PrimitiveType.Cube, parent, new Vector3(0f, 0.77f, 1.22f), new Vector3(8f, 0f, 0f), new Vector3(1.52f, 0.18f, 1.36f), bodyMat);
        CreatePrim("Trunk", PrimitiveType.Cube, parent, new Vector3(0f, 0.78f, -1.58f), new Vector3(-5f, 0f, 0f), new Vector3(1.46f, 0.2f, 0.9f), bodyMat);
        CreatePrim("FrontWindshield", PrimitiveType.Quad, parent, new Vector3(0f, 1.08f, 0.72f), new Vector3(-28f, 180f, 0f), new Vector3(1.2f, 0.58f, 1f), glassMat);
        CreatePrim("RearWindshield", PrimitiveType.Quad, parent, new Vector3(0f, 1.04f, -0.92f), new Vector3(24f, 0f, 0f), new Vector3(1.08f, 0.48f, 1f), glassMat);
        CreatePrim("WindowLeft", PrimitiveType.Quad, parent, new Vector3(-0.79f, 0.98f, -0.08f), new Vector3(0f, 90f, 0f), new Vector3(1.52f, 0.46f, 1f), glassMat);
        CreatePrim("WindowRight", PrimitiveType.Quad, parent, new Vector3(0.79f, 0.98f, -0.08f), new Vector3(0f, -90f, 0f), new Vector3(1.52f, 0.46f, 1f), glassMat);
        CreatePrim("Roof", PrimitiveType.Cube, parent, new Vector3(0f, 1.25f, -0.1f), Vector3.zero, new Vector3(1.22f, 0.08f, 1.38f), trimMat);
        Vector3[] wheels =
        {
            new Vector3(-0.82f, 0.28f, 1.3f),
            new Vector3(0.82f, 0.28f, 1.3f),
            new Vector3(-0.82f, 0.28f, -1.28f),
            new Vector3(0.82f, 0.28f, -1.28f)
        };
        for (int i = 0; i < wheels.Length; i++)
            CreatePrim($"Wheel_{i}", PrimitiveType.Cylinder, parent, wheels[i], new Vector3(0f, 0f, 90f), new Vector3(0.32f, 0.14f, 0.32f), tireMat);
    }

    static void BuildExteriorLighting(Transform root, Material lightMat)
    {
        float lotLightZ = StoreFlowParkingLampRowZ;
        AddLight(root, "StoreGlow", new Vector3(0f, 2.9f, 9.6f), LightType.Point, new Color(1f, 0.92f, 0.82f), 1.7f, 10f, 0f);
        AddLight(root, "StreetLampLeft", new Vector3(-5.4f, 4.2f, lotLightZ), LightType.Point, new Color(1f, 0.84f, 0.6f), 1.45f, 11f, 0f);
        AddLight(root, "StreetLampRight", new Vector3(5.4f, 4.2f, lotLightZ), LightType.Point, new Color(1f, 0.84f, 0.6f), 1.45f, 11f, 0f);
        GameObject moon = new GameObject("Moonlight");
        moon.transform.SetParent(root, false);
        moon.transform.localRotation = Quaternion.Euler(34f, -30f, 0f);
        Light moonLight = moon.AddComponent<Light>();
        moonLight.type = LightType.Directional;
        moonLight.color = new Color(0.28f, 0.32f, 0.42f);
        moonLight.intensity = 0.2f;
        moonLight.shadows = LightShadows.Soft;
    }

    static void BuildPlayerAndIntro(Transform root, Transform leftDoor, Transform rightDoor)
    {
        GameObject camObj = GameObject.Find("Main Camera");
        Camera cam;
        if (camObj != null)
        {
            cam = camObj.GetComponent<Camera>();
        }
        else
        {
            camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }

        GameObject playerRig = new GameObject("PlayerRig");
        playerRig.transform.SetParent(root, false);
        CharacterController controller = playerRig.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.28f;
        controller.center = new Vector3(0f, 0.9f, 0f);

        GameObject cameraPivot = new GameObject("CameraPivot");
        cameraPivot.transform.SetParent(playerRig.transform, false);
        cameraPivot.transform.localPosition = new Vector3(0f, 1.62f, 0f);

        camObj.transform.SetParent(cameraPivot.transform, false);
        camObj.transform.localPosition = Vector3.zero;
        camObj.transform.localRotation = Quaternion.identity;
        cam.fieldOfView = 55f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 120f;

        StoreFirstPersonController fp = playerRig.AddComponent<StoreFirstPersonController>();
        fp.cameraPivot = cameraPivot.transform;
        fp.moveSpeed = 3.2f;
        fp.sprintSpeed = 4.8f;
        fp.mouseSensitivity = 2f;
        fp.enableLocomotionViewBob = true;
        fp.viewBobVerticalAmplitude = 0.022f;
        fp.viewBobLateralAmplitude = 0.006f;
        fp.viewBobFrequency = 2.05f;
        fp.viewBobTiltAmplitude = 0.38f;

        GameObject markers = new GameObject("IntroMarkers");
        markers.transform.SetParent(root, false);
        Transform menuView = CreateMarker(markers.transform, "MenuView",
            new Vector3(4.432952f, 0.505576f, 2.680152f),
            new Quaternion(0.1164553f, 0.06764905f, 0.008053988f, -0.9908566f));
        Transform carSeatView = CreateMarker(markers.transform, "CarSeatView",
            new Vector3(3.07978f, 0.9745316f, 4.704579f),
            new Quaternion(-0.05095608f, -0.02777847f, 0.001493274f, -0.9983134f));
        Transform carExitView = CreateMarker(markers.transform, "CarExitView",
            new Vector3(1.918646f, 1.252334f, 4.797719f),
            new Quaternion(-0.02399744f, 0.008176768f, -0.0001328424f, -0.9996787f));
        Transform exploreView = CreateMarker(markers.transform, "ExploreView", new Vector3(0f, 1.62f, 10.9f), new Vector3(0f, 0f, 0f));

        StoreFlowIntroController intro = root.gameObject.AddComponent<StoreFlowIntroController>();
        intro.playerController = fp;
        intro.playerCamera = cam;
        intro.menuView = menuView;
        intro.carSeatView = carSeatView;
        intro.carExitView = carExitView;
        intro.exploreView = exploreView;
        intro.overrideMenuViewPose = false;
        intro.overrideCarSeatViewPose = false;
        intro.useCustomSequencePoses = true;
        intro.customMenuPosition = new Vector3(15.58386f, 1.227849f, -9.701193f);
        intro.customMenuRotation = new Vector4(-0.1115422f, 0.665723f, 0.1029979f, 0.7305908f);
        intro.customCarSeatPosition = new Vector3(4.676617f, 1.573286f, 4.200386f);
        intro.customCarSeatRotation = new Vector4(0.04550149f, -0.03777738f, 0.002805305f, 0.9982458f);
        intro.customCarExitPosition = new Vector3(6.217842f, 1.927321f, 4.447559f);
        intro.customCarExitRotation = new Vector4(0.0110589f, -0.02585859f, 0.001372372f, 0.9996035f);
        intro.customWalkToStorePosition = new Vector3(6.155984f, 1.702457f, 6.83116f);
        intro.customWalkToStoreRotation = new Vector4(0.039528f, -0.03030579f, 0.002316293f, 0.9987561f);
        intro.customTurnToEntrancePosition = intro.customWalkToStorePosition;
        intro.customTurnToEntranceRotation = intro.customWalkToStoreRotation;
        intro.customEnterStorePosition = intro.customWalkToStorePosition;
        intro.customEnterStoreRotation = intro.customWalkToStoreRotation;
        intro.carSideDoorName = "doorleftsmd";
        intro.carSideDoorOpenAngleX = -55f;
        intro.leftDoor = leftDoor;
        intro.rightDoor = rightDoor;
        intro.leftDoorClosedLocalPosition = leftDoor.localPosition;
        intro.rightDoorClosedLocalPosition = rightDoor.localPosition;
        intro.leftDoorOpenLocalPosition = leftDoor.localPosition + Vector3.left * 0.82f;
        intro.rightDoorOpenLocalPosition = rightDoor.localPosition + Vector3.right * 0.82f;

        StoreEntranceLock entranceLock = leftDoor.parent.GetComponent<StoreEntranceLock>();
        if (entranceLock == null)
            entranceLock = leftDoor.parent.gameObject.AddComponent<StoreEntranceLock>();
        entranceLock.ConfigureUsingDoors(leftDoor, rightDoor);
        entranceLock.UnlockEntrance();
        intro.entranceLock = entranceLock;
        intro.walkBobVerticalAmplitude = 0.05f;
        intro.walkBobLateralAmplitude = 0.014f;
        intro.walkBobFrequency = 2.15f;
        intro.walkBobTiltAmplitude = 1f;
    }

    static void EnsureSceneBootstrap()
    {
        if (UnityEngine.Object.FindAnyObjectByType<GameFlowManager>() == null)
        {
            GameFlowManager gfm = new GameObject("GameFlowManager").AddComponent<GameFlowManager>();
            gfm.useStoreIntroWhenAvailable = true;
            gfm.fallbackToStoreCutscene = false;
        }

        if (UnityEngine.Object.FindAnyObjectByType<MainMenuUI>() == null)
            new GameObject("MainMenuUI").AddComponent<MainMenuUI>();
    }

    static void RemoveDirectionalLights()
    {
        foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>())
        {
            if (light.type == LightType.Directional)
                UnityEngine.Object.DestroyImmediate(light.gameObject);
        }
    }

    static void DestroyRoot(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing);
    }

    static Transform CreateMarker(Transform parent, string name, Vector3 pos, Vector3 rotEuler)
    {
        return CreateMarker(parent, name, pos, Quaternion.Euler(rotEuler));
    }

    static Transform CreateMarker(Transform parent, string name, Vector3 localPos, Quaternion localRot)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        return go.transform;
    }

    static GameObject CreatePrim(string name, PrimitiveType type, Transform parent, Vector3 pos, Vector3 rot, Vector3 scale, Material mat, bool keepCollider = false)
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
            UnityEngine.Object.DestroyImmediate(collider);
        return obj;
    }

    static Material CreateLit(string name, Color color)
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

    static Material CreateTransparent(string name, Color color)
    {
        Material mat = CreateLit(name, color);
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

    static Material CreateTransparentTexturedFromPath(string name, string texturePath, Color tint)
    {
        Material mat = CreateTransparent(name, tint);
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", tex);
            else
                mat.mainTexture = tex;
        }
        return mat;
    }

    static Material CreateLabelMaterial(string name, Color body, Color stripe)
    {
        Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < tex.height; y++)
        {
            for (int x = 0; x < tex.width; x++)
            {
                Color c = body;
                if (y > 21 && y < 27)
                    c = stripe;
                else if (y > 10 && y < 14)
                    c = Color.Lerp(body, Color.white, 0.35f);
                else if (x > 3 && x < 6 && y > 4 && y < 28)
                    c = Color.Lerp(stripe, Color.white, 0.5f);
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();

        Material mat = CreateLit(name, Color.white);
        if (mat.HasProperty("_BaseMap"))
            mat.SetTexture("_BaseMap", tex);
        else
            mat.mainTexture = tex;
        return mat;
    }

    static void CreateStockItem(Transform parent, string name, Vector3 pos, int shape, Material labelMat, Material accentMat)
    {
        switch (shape)
        {
            case 0:
                CreatePrim(name, PrimitiveType.Cube, parent, pos, Vector3.zero, new Vector3(0.18f, 0.3f, 0.14f), labelMat, false);
                break;
            case 1:
                CreatePrim(name, PrimitiveType.Cylinder, parent, pos + new Vector3(0f, 0.03f, 0f), new Vector3(90f, 0f, 0f), new Vector3(0.08f, 0.14f, 0.08f), labelMat, false);
                break;
            case 2:
                CreatePrim(name, PrimitiveType.Cube, parent, pos + new Vector3(0f, -0.02f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0.24f, 0.12f, 0.14f), labelMat, false);
                break;
            case 3:
                CreatePrim(name, PrimitiveType.Capsule, parent, pos + new Vector3(0f, 0.05f, 0f), Vector3.zero, new Vector3(0.08f, 0.16f, 0.08f), accentMat, false);
                CreatePrim(name + "_Label", PrimitiveType.Cube, parent, pos + new Vector3(0f, 0.05f, 0.02f), Vector3.zero, new Vector3(0.09f, 0.09f, 0.02f), labelMat, false);
                break;
            default:
                CreatePrim(name, PrimitiveType.Cube, parent, pos, Vector3.zero, new Vector3(0.14f, 0.26f, 0.12f), accentMat, false);
                CreatePrim(name + "_Front", PrimitiveType.Cube, parent, pos + new Vector3(0f, 0f, 0.05f), Vector3.zero, new Vector3(0.12f, 0.22f, 0.02f), labelMat, false);
                break;
        }
    }

    static void BuildBackFreezers(Transform parent, Material body, Material trim, Material glow, Material glass,
        Material productA, Material productB, Material productC, Material productD)
    {
        GameObject freezers = new GameObject("Freezers");
        freezers.transform.SetParent(parent, false);
        Material[] mats = { productA, productB, productC, productD };

        for (int i = 0; i < 4; i++)
        {
            float x = -4.8f + i * 3.2f;
            GameObject bay = new GameObject($"Freezer_{i}");
            bay.transform.SetParent(freezers.transform, false);
            bay.transform.localPosition = new Vector3(x, 0f, 16.45f);

            CreatePrim("Cabinet", PrimitiveType.Cube, bay.transform, new Vector3(0f, 1.2f, 0f), Vector3.zero, new Vector3(2.6f, 2.4f, 0.9f), body, true);
            CreatePrim("InteriorGlow", PrimitiveType.Cube, bay.transform, new Vector3(0f, 1.2f, -0.3f), Vector3.zero, new Vector3(2.3f, 2.1f, 0.15f), glow, false);
            CreatePrim("DoorFrame", PrimitiveType.Cube, bay.transform, new Vector3(0f, 1.2f, -0.42f), Vector3.zero, new Vector3(2.5f, 2.25f, 0.08f), trim, false);
            CreatePrim("GlassL", PrimitiveType.Cube, bay.transform, new Vector3(-0.62f, 1.2f, -0.46f), Vector3.zero, new Vector3(1.12f, 2.1f, 0.03f), glass, false);
            CreatePrim("GlassR", PrimitiveType.Cube, bay.transform, new Vector3(0.62f, 1.2f, -0.46f), Vector3.zero, new Vector3(1.12f, 2.1f, 0.03f), glass, false);
            AddLight(bay.transform, "FreezerLight", new Vector3(0f, 1.55f, -0.28f), LightType.Point, new Color(0.45f, 0.76f, 1f), 0.95f, 3.3f, 0f);

            for (int shelf = 0; shelf < 3; shelf++)
            {
                float y = 0.52f + shelf * 0.55f;
                CreatePrim($"Shelf_{shelf}", PrimitiveType.Cube, bay.transform, new Vector3(0f, y, -0.05f), Vector3.zero, new Vector3(2.1f, 0.05f, 0.48f), trim, false);
                if (!StoreFlowPlaceProductGeometry)
                    continue;
                for (int item = 0; item < 5; item++)
                {
                    float itemX = -0.8f + item * 0.4f;
                    CreateStockItem(bay.transform, $"Frozen_{shelf}_{item}", new Vector3(itemX, y + 0.12f, -0.06f),
                        (shelf + item) % 4, mats[(shelf + item) % mats.Length], trim);
                }
            }
        }
    }

    static void BuildCheckoutCornerFrontLeft(Transform parent, Material counter, Material counterTop, Material register, Material registerGlow, Material trim,
        Material productA, Material productB, Material productC)
    {
        GameObject checkout = new GameObject("CheckoutCorner");
        checkout.transform.SetParent(parent, false);
        checkout.transform.localPosition = new Vector3(-4.9f, 0f, 9.9f);

        CreatePrim("CounterBase", PrimitiveType.Cube, checkout.transform, new Vector3(0f, 0.45f, 0f), Vector3.zero, new Vector3(3.4f, 0.9f, 0.8f), counter, true);
        CreatePrim("CounterTop", PrimitiveType.Cube, checkout.transform, new Vector3(0f, 0.93f, 0f), Vector3.zero, new Vector3(3.5f, 0.05f, 0.9f), counterTop, true);
        CreatePrim("CounterSide", PrimitiveType.Cube, checkout.transform, new Vector3(1.35f, 0.45f, 1.4f), Vector3.zero, new Vector3(0.8f, 0.9f, 2.8f), counter, true);
        CreatePrim("CounterTopSide", PrimitiveType.Cube, checkout.transform, new Vector3(1.35f, 0.93f, 1.4f), Vector3.zero, new Vector3(0.9f, 0.05f, 2.9f), counterTop, true);
        CreatePrim("Register", PrimitiveType.Cube, checkout.transform, new Vector3(-1.0f, 1.15f, -0.05f), Vector3.zero, new Vector3(0.38f, 0.28f, 0.32f), register, false);
        CreatePrim("RegisterScreen", PrimitiveType.Cube, checkout.transform, new Vector3(-1.02f, 1.42f, 0.06f), new Vector3(338f, 0f, 0f), new Vector3(0.26f, 0.18f, 0.02f), registerGlow, false);

        if (StoreFlowPlaceProductGeometry)
        {
            for (int i = 0; i < 6; i++)
            {
                Material mat = i % 3 == 0 ? productA : (i % 3 == 1 ? productB : productC);
                CreateStockItem(checkout.transform, $"Impulse_{i}", new Vector3(1.65f, 1.08f, 0.3f + i * 0.36f), i % 5, mat, trim);
            }
        }
    }

    static void BuildVendingMachineFrontRight(Transform parent, Material body, Material glow, Material glass)
    {
        GameObject vending = new GameObject("VendingMachine");
        vending.transform.SetParent(parent, false);
        vending.transform.localPosition = new Vector3(5.8f, 0f, 9.8f);

        CreatePrim("Body", PrimitiveType.Cube, vending.transform, new Vector3(0f, 1.2f, 0f), Vector3.zero, new Vector3(1.15f, 2.4f, 0.9f), body, true);
        CreatePrim("FrontGlass", PrimitiveType.Cube, vending.transform, new Vector3(0f, 1.22f, -0.43f), Vector3.zero, new Vector3(0.8f, 1.75f, 0.03f), glass, false);
        CreatePrim("DisplayGlow", PrimitiveType.Cube, vending.transform, new Vector3(0f, 1.22f, -0.39f), Vector3.zero, new Vector3(0.7f, 1.58f, 0.03f), glow, false);
        CreatePrim("PaymentPanel", PrimitiveType.Cube, vending.transform, new Vector3(0.36f, 1.0f, -0.41f), Vector3.zero, new Vector3(0.14f, 0.42f, 0.04f), CreateLit("StoreFlow_Panel", new Color(0.12f, 0.12f, 0.14f)), false);
        if (StoreFlowPlaceProductGeometry)
        {
            for (int i = 0; i < 4; i++)
            {
                CreatePrim($"DrinkRow_{i}", PrimitiveType.Cube, vending.transform, new Vector3(0f, 0.72f + i * 0.32f, -0.36f), Vector3.zero, new Vector3(0.6f, 0.06f, 0.05f), CreateLabelMaterial($"StoreFlow_Drink_{i}", new Color(0.18f + i * 0.12f, 0.34f, 0.72f - i * 0.08f), new Color(0.94f, 0.94f, 0.98f)), false);
            }
        }

        AddVendingMachineBrandLabel(vending.transform);
    }

    static void AddVendingMachineBrandLabel(Transform vendingRoot)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpLiberationSansSdfPath);
        if (font == null)
        {
            Debug.LogWarning("Store Flow: could not load TMP font for vending label — " + TmpLiberationSansSdfPath);
            return;
        }

        GameObject label = new GameObject("BrandText");
        label.transform.SetParent(vendingRoot, false);
        label.transform.localPosition = new Vector3(0f, 2.06f, -0.452f);
        label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        label.transform.localScale = Vector3.one * 0.048f;

        TextMeshPro tmp = label.AddComponent<TextMeshPro>();
        tmp.font = font;
        tmp.text = "Boca-Bola";
        tmp.fontSize = 11f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.rectTransform.sizeDelta = new Vector2(24f, 5f);

        foreach (Collider c in label.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(c);
    }

    static bool IsSubaruImprezaWheelObjectName(string lowerName)
    {
        if (!lowerName.Contains("sub_imp"))
            return false;
        return lowerName.EndsWith("fl", System.StringComparison.Ordinal)
               || lowerName.EndsWith("fr", System.StringComparison.Ordinal)
               || lowerName.EndsWith("rl", System.StringComparison.Ordinal)
               || lowerName.EndsWith("rr", System.StringComparison.Ordinal);
    }

    static void ApplyCarMaterials(GameObject car, Material bodyMat, Material trimMat, Material glassMat, Material tireMat,
        Material wheelMat, Material headlightMat, Material taillightMat, Material interiorMat)
    {
        foreach (Renderer renderer in car.GetComponentsInChildren<Renderer>(true))
        {
            string name = renderer.gameObject.name.ToLowerInvariant();
            Material mat = bodyMat;
            if (name.Contains("glass") || name.Contains("window") || name.Contains("windshield") || name.Contains("windscreen"))
                mat = glassMat;
            else if (name.Contains("headlight") || name.Contains("headlamp") || name.Contains("fog"))
                mat = headlightMat;
            else if (name.Contains("brakelight") || name.Contains("taillight") || name.Contains("rearlight") || name.Contains("litfull") || name == "litsmd" || name == "lit_1smd")
                mat = taillightMat;
            else if (name.Contains("tire") || name.Contains("tyre"))
                mat = tireMat;
            else if (name.Contains("wheel") || name.Contains("rim") || name.Contains("hub")
                     || IsSubaruImprezaWheelObjectName(name))
                mat = wheelMat;
            else if (name.Contains("interior") || name.Contains("steering") || name.Contains("seat") || name.Contains("dash") || name == "root" || name == "root_1")
                mat = interiorMat;
            else if (name.Contains("chrome") || name.Contains("misc") || name.Contains("engine") || name.Contains("exhaust") || name.Contains("grille") || name.Contains("mirror"))
                mat = trimMat;
            renderer.sharedMaterial = mat;
        }
    }

    static readonly string[] CarModelExtensions = { ".fbx", ".obj", ".glb", ".gltf" };

    static GameObject LoadFirstCarModelInFolder(string folderAssetPath)
    {
        if (string.IsNullOrEmpty(folderAssetPath))
            return null;

        string[] preferredPaths =
        {
            $"{folderAssetPath}/source/Subaru Impreza/subaru_impreza.fbx",
            $"{folderAssetPath}/source/Subaru Impreza/Subaru Impreza.fbx",
            $"{folderAssetPath}/source/Subaru Impreza.fbx",
            $"{folderAssetPath}/source/Subaru_Impreza.fbx",
            $"{folderAssetPath}/source/subaru impreza.fbx",
            $"{folderAssetPath}/Subaru Impreza.fbx"
        };

        foreach (string path in preferredPaths)
        {
            GameObject preferred = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (preferred != null)
            {
                Debug.Log($"Store Flow Scene: Parked car mesh '{path}'.");
                return preferred;
            }
        }

        string[] guids = AssetDatabase.FindAssets("", new[] { folderAssetPath });
        GameObject found = null;
        string foundPath = null;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith(folderAssetPath, System.StringComparison.OrdinalIgnoreCase))
                continue;

            bool isModel = false;
            foreach (string ext in CarModelExtensions)
            {
                if (path.EndsWith(ext, System.StringComparison.OrdinalIgnoreCase))
                {
                    isModel = true;
                    break;
                }
            }

            if (!isModel)
                continue;

            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null)
                continue;

            if (found == null || path.Length < foundPath.Length)
            {
                found = go;
                foundPath = path;
            }
        }

        if (found != null)
            Debug.Log($"Store Flow Scene: Parked car mesh '{foundPath}'.");
        else
            Debug.LogWarning($"Store Flow Scene: No .fbx/.obj/.glb under '{folderAssetPath}'. Extract the Subaru archive there, then re-run Store Flow Scene.");

        return found;
    }

    static Texture2D LoadCarTexture(string fileName)
    {
        return null;
    }

    static Material CreateTexturedLit(string name, string textureFile, Color tint)
    {
        Material mat = CreateLit(name, tint);
        Texture2D tex = LoadCarTexture(textureFile);
        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", tex);
            else
                mat.mainTexture = tex;
        }
        return mat;
    }

    static Material CreateTexturedLitFromPath(string name, string texturePath, Color tint, Vector2 tiling)
    {
        Material mat = CreateLit(name, tint);
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (tex != null)
        {
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

    static Material CreateTransparentTextured(string name, string textureFile, Color tint)
    {
        Material mat = CreateTransparent(name, tint);
        Texture2D tex = LoadCarTexture(textureFile);
        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", tex);
            else
                mat.mainTexture = tex;
        }
        return mat;
    }

    static Material CreateEmissiveTextured(string name, string textureFile, Color tint, Color emission, float intensity)
    {
        Material mat = CreateTexturedLit(name, textureFile, tint);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emission * intensity);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return mat;
    }

    static Material CreateEmissive(string name, Color emission, float intensity)
    {
        Material mat = CreateLit(name, emission);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emission * intensity);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return mat;
    }

    static Material CreateSnowMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader);
        mat.name = "StoreFlow_Snow";
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", Color.white);
        return mat;
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

    static void AddSpotLightAimedAt(Transform parent, string name, Vector3 localPos, Vector3 localAimAt, Color color, float intensity, float range, float spotAngle)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Vector3 worldPos = parent.TransformPoint(localPos);
        Vector3 worldAim = parent.TransformPoint(localAimAt);
        go.transform.position = worldPos;
        Vector3 dir = worldAim - worldPos;
        if (dir.sqrMagnitude < 1e-6f)
            go.transform.localRotation = Quaternion.identity;
        else
            go.transform.rotation = Quaternion.LookRotation(dir.normalized, parent.up);
        Light light = go.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.spotAngle = spotAngle;
        light.shadows = LightShadows.Soft;
    }

    static Shader FindShader()
    {
        return Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
    }
}
