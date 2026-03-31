using UnityEditor;
using UnityEngine;

public class StoreFlowSceneGenerator : Editor
{
    const string RootName = "StoreFlowScene";
    const string RenaultFbxPath = "Assets/ModelsPlace/psx-low-poly-car-renault/source/Car.fbx";
    const string RenaultBodyMatPath = "Assets/ModelsPlace/psx-low-poly-car-renault/Materials/Car_-_RegularWhite.mat";
    const string RenaultTireMatPath = "Assets/ModelsPlace/psx-low-poly-car-renault/Materials/Tire.mat";
    const string ForestCollectionPath = "Assets/ModelsPlace/PSX_Forest_Level_byStarkCrafts/PSX_Forest_Level_byStarkCrafts/PSX_Forest_AssetCollection_byStarkCrafts.fbx";
    const string ForestGroundTexturePath = "Assets/ModelsPlace/PSX_Forest_Level_byStarkCrafts/PSX_Forest_Level_byStarkCrafts/PSX_ForestGround_Tex/PSX_Seamless_ForestWildGround_128px.png";
    const string HouseWallTexturePath = "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Bricks.jpg";
    const string HouseRoofTexturePath = "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Roof_tiles.jpg";
    const string HouseWindowTexturePath = "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Lights_01.jpg";
    const string AsphaltTexturePath = "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Asphalt.jpg";
    const string ConcreteTexturePath = "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Concrete_01.jpg";
    const string StoreSignTexturePath = "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Lottery.png";
    const string TreeBillboardTexturePath = "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Tree_01.png";
    const string BushBillboardTexturePath = "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Plants_01.png";

    [MenuItem("OPEN FEED/Scripts/Store Flow Scene - Clear")]
    static void ClearScene()
    {
        DestroyRoot(RootName);
        DestroyRoot("SixTwelveScene");
    }

    [MenuItem("OPEN FEED/Scripts/Store Flow Scene")]
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
        Material shelf = CreateTexturedLitFromPath("StoreFlow_Shelf", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Metal_01.jpg", new Color(0.85f, 0.85f, 0.85f), new Vector2(1f, 2f));
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
        Material carBody = AssetDatabase.LoadAssetAtPath<Material>(RenaultBodyMatPath) ?? CreateLit("StoreFlow_CarBodyFallback", new Color(0.92f, 0.92f, 0.92f));
        Material tire = AssetDatabase.LoadAssetAtPath<Material>(RenaultTireMatPath) ?? CreateLit("StoreFlow_CarTireFallback", new Color(0.12f, 0.12f, 0.12f));
        Material carGlass = CreateTransparent("StoreFlow_CarGlassFallback", new Color(0.74f, 0.86f, 0.96f, 0.26f));
        Material carTrim = carBody;
        Material wheel = tire;
        Material carHeadlight = CreateEmissive("StoreFlow_CarHeadlightFallback", new Color(0.95f, 0.97f, 1f), 0.7f);
        Material carTaillight = CreateEmissive("StoreFlow_CarTaillightFallback", new Color(1f, 0.18f, 0.14f), 0.8f);
        Material carInterior = carBody;
        Material doorFrame = CreateLit("StoreFlow_DoorFrame", new Color(0.14f, 0.15f, 0.18f));
        Material lightMat = CreateEmissive("StoreFlow_Light", new Color(1f, 0.96f, 0.88f), 1.25f);
        Material freezerBody = CreateLit("StoreFlow_FreezerBody", new Color(0.84f, 0.88f, 0.94f));
        Material freezerTrim = CreateLit("StoreFlow_FreezerTrim", new Color(0.58f, 0.68f, 0.82f));
        Material freezerGlow = CreateEmissive("StoreFlow_FreezerGlow", new Color(0.46f, 0.78f, 1f), 1.8f);
        Material counter = CreateLit("StoreFlow_Counter", new Color(0.82f, 0.83f, 0.86f));
        Material counterTop = CreateLit("StoreFlow_CounterTop", new Color(0.94f, 0.94f, 0.96f));
        Material register = CreateLit("StoreFlow_Register", new Color(0.18f, 0.19f, 0.21f));
        Material vendingBody = CreateLit("StoreFlow_VendingBody", new Color(0.7f, 0.12f, 0.16f));
        Material vendingGlow = CreateEmissive("StoreFlow_VendingGlow", new Color(0.9f, 0.96f, 1f), 1.2f);

        BuildGround(root.transform, asphalt, line, concrete);
        BuildStore(root.transform, wall, roof, sign, glass, doorFrame, floor, floorStripe, shelf, trim,
            productA, productB, productC, productD, productE, productF,
            lightMat, freezerBody, freezerTrim, freezerGlow, counter, counterTop, register, vendingBody, vendingGlow,
            out Transform leftDoor, out Transform rightDoor);
        BuildNeighborhood(root.transform, asphalt, concrete);
        BuildStreetProps(root.transform, concrete, trim, lightMat);
        BuildSnowfall(root.transform);

        GameObject carAnchor = new GameObject("ParkedCar");
        carAnchor.transform.SetParent(root.transform, false);
        // Keep user-tuned ParkedCar transform as requested.
        carAnchor.transform.localPosition = new Vector3(3.758f, 0.465f, 3.748f);
        carAnchor.transform.localRotation = Quaternion.Euler(-90f, -90f, 180f);
        carAnchor.transform.localScale = new Vector3(98.62f, 98.62f, 98.62f);
        BuildCar(carAnchor.transform, carBody, carTrim, carGlass, tire, wheel, carHeadlight, carTaillight, carInterior);

        BuildExteriorLighting(root.transform, lightMat);
        BuildPlayerAndIntro(root.transform, leftDoor, rightDoor);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.08f, 0.09f, 0.12f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.012f;
        RenderSettings.fogColor = new Color(0.04f, 0.05f, 0.08f);

        Selection.activeGameObject = root;
        Debug.Log("OPENFEED Store Flow scene generated.");
    }

    static void BuildGround(Transform root, Material asphalt, Material line, Material concrete)
    {
        GameObject ground = new GameObject("Ground");
        ground.transform.SetParent(root, false);

        CreatePrim("ParkingLot", PrimitiveType.Cube, ground.transform, new Vector3(0f, -0.05f, 3f), Vector3.zero, new Vector3(24f, 0.1f, 18f), asphalt, true);
        CreatePrim("Road", PrimitiveType.Cube, ground.transform, new Vector3(0f, -0.04f, 14f), Vector3.zero, new Vector3(28f, 0.08f, 4f), asphalt, true);
        CreatePrim("Sidewalk", PrimitiveType.Cube, ground.transform, new Vector3(0f, 0.03f, 9.1f), Vector3.zero, new Vector3(14f, 0.12f, 2.2f), concrete, true);
        CreatePrim("StoreInteriorFloorCollider", PrimitiveType.Cube, ground.transform, new Vector3(0f, 0.03f, 12.8f), Vector3.zero, new Vector3(14f, 0.12f, 10f), null, true).GetComponent<Renderer>().enabled = false;

        for (int i = 0; i < 7; i++)
        {
            CreatePrim($"RoadDash{i}", PrimitiveType.Cube, ground.transform,
                new Vector3(-10.8f + i * 3.6f, 0.005f, 14f), Vector3.zero, new Vector3(1.5f, 0.01f, 0.1f), line, false);
        }
    }

    static void BuildNeighborhood(Transform root, Material asphalt, Material concrete)
    {
        GameObject neighborhood = new GameObject("Neighborhood");
        neighborhood.transform.SetParent(root, false);

        Material grassMat = CreateTexturedLitFromPath("StoreFlow_Grass", ForestGroundTexturePath, new Color(0.72f, 0.78f, 0.72f), new Vector2(10f, 10f));
        Material houseWall = CreateTexturedLitFromPath("StoreFlow_HouseWall", HouseWallTexturePath, Color.white, new Vector2(2f, 2f));
        Material houseRoof = CreateTexturedLitFromPath("StoreFlow_HouseRoof", HouseRoofTexturePath, Color.white, new Vector2(2f, 1f));
        Material houseWindow = CreateTexturedLitFromPath("StoreFlow_HouseWindow", HouseWindowTexturePath, new Color(1f, 0.95f, 0.85f), new Vector2(1f, 1f));
        Material houseDoor = CreateTexturedLitFromPath("StoreFlow_HouseDoor", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Door_03.jpg", new Color(0.86f, 0.86f, 0.86f), new Vector2(1f, 1f));

        CreatePrim("GrassField", PrimitiveType.Cube, neighborhood.transform, new Vector3(0f, -0.055f, -20f), Vector3.zero, new Vector3(90f, 0.03f, 70f), grassMat, false);
        CreatePrim("OuterRoad", PrimitiveType.Cube, neighborhood.transform, new Vector3(0f, -0.045f, -2.2f), Vector3.zero, new Vector3(32f, 0.08f, 7f), asphalt, true);
        CreatePrim("RoadShoulderLeft", PrimitiveType.Cube, neighborhood.transform, new Vector3(-17.5f, -0.05f, -2.2f), Vector3.zero, new Vector3(7f, 0.05f, 7f), concrete, false);
        CreatePrim("RoadShoulderRight", PrimitiveType.Cube, neighborhood.transform, new Vector3(17.5f, -0.05f, -2.2f), Vector3.zero, new Vector3(7f, 0.05f, 7f), concrete, false);

        Vector3[] housePositions =
        {
            new Vector3(-18f, 0f, -12f),
            new Vector3(-27f, 0f, -20f),
            new Vector3(-10.5f, 0f, -22f),
            new Vector3(17f, 0f, -12f),
            new Vector3(26f, 0f, -20f),
            new Vector3(9.5f, 0f, -23f)
        };

        for (int i = 0; i < housePositions.Length; i++)
            BuildHouseBlock(neighborhood.transform, $"House_{i}", housePositions[i], 160f + i * 11f, houseWall, houseRoof, houseWindow, houseDoor);

        int foliageCount = PopulateFoliageFromForestAsset(neighborhood.transform);
        if (foliageCount < 20)
            PopulateFallbackBillboardFoliage(neighborhood.transform, 48 - foliageCount);
    }

    static void BuildHouseBlock(Transform parent, string name, Vector3 pos, float yRot, Material wall, Material roof, Material window, Material door)
    {
        GameObject house = new GameObject(name);
        house.transform.SetParent(parent, false);
        house.transform.localPosition = pos;
        house.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);

        CreatePrim("Base", PrimitiveType.Cube, house.transform, new Vector3(0f, 1.8f, 0f), Vector3.zero, new Vector3(6f, 3.6f, 5.2f), wall, true);
        CreatePrim("Roof", PrimitiveType.Cube, house.transform, new Vector3(0f, 3.9f, 0f), Vector3.zero, new Vector3(6.6f, 0.9f, 5.8f), roof, false);
        CreatePrim("WindowL", PrimitiveType.Cube, house.transform, new Vector3(-1.5f, 2f, 2.58f), Vector3.zero, new Vector3(1.1f, 1f, 0.08f), window, false);
        CreatePrim("WindowR", PrimitiveType.Cube, house.transform, new Vector3(1.5f, 2f, 2.58f), Vector3.zero, new Vector3(1.1f, 1f, 0.08f), window, false);
        CreatePrim("Door", PrimitiveType.Cube, house.transform, new Vector3(0f, 1f, 2.58f), Vector3.zero, new Vector3(1f, 2f, 0.09f), door, false);
    }

    static int PopulateFoliageFromForestAsset(Transform parent)
    {
        GameObject forestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ForestCollectionPath);
        if (forestPrefab == null)
        {
            Debug.LogWarning("Store Flow Scene: Forest collection FBX not found. Skipping foliage instancing.");
            return 0;
        }

        GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(forestPrefab);
        if (temp == null)
            return 0;

        try
        {
            MeshRenderer[] renderers = temp.GetComponentsInChildren<MeshRenderer>(true);
            System.Collections.Generic.List<GameObject> foliagePrefabs = new System.Collections.Generic.List<GameObject>();
            foreach (MeshRenderer renderer in renderers)
            {
                string n = renderer.gameObject.name.ToLowerInvariant();
                bool isFoliageName = n.Contains("tree") || n.Contains("pine") || n.Contains("bush") || n.Contains("plant") || n.Contains("grass");
                bool isLikelyGround = n.Contains("ground") || n.Contains("terrain") || n.Contains("road") || n.Contains("path") || n.Contains("floor");

                if (!isLikelyGround && isFoliageName)
                {
                    Transform root = renderer.transform;
                    while (root.parent != null && root.parent != temp.transform)
                        root = root.parent;
                    if (root != temp.transform && !foliagePrefabs.Contains(root.gameObject))
                        foliagePrefabs.Add(root.gameObject);
                }
            }

            if (foliagePrefabs.Count == 0)
            {
                // Fallback: take any non-ground mesh roots so foliage still appears even with unknown naming.
                foreach (MeshRenderer renderer in renderers)
                {
                    string n = renderer.gameObject.name.ToLowerInvariant();
                    if (n.Contains("ground") || n.Contains("terrain") || n.Contains("road") || n.Contains("path") || n.Contains("floor"))
                        continue;

                    Transform root = renderer.transform;
                    while (root.parent != null && root.parent != temp.transform)
                        root = root.parent;
                    if (root != temp.transform && !foliagePrefabs.Contains(root.gameObject))
                        foliagePrefabs.Add(root.gameObject);
                }
            }

            if (foliagePrefabs.Count == 0)
            {
                Debug.LogWarning("Store Flow Scene: No foliage-compatible meshes found in forest FBX.");
                return 0;
            }

            System.Random rng = new System.Random(314159);
            int placedCount = 0;
            for (int i = 0; i < 70; i++)
            {
                GameObject src = foliagePrefabs[rng.Next(foliagePrefabs.Count)];
                GameObject placed = Object.Instantiate(src, parent);
                placed.name = $"Foliage_{i}_{src.name}";
                float x = -42f + (float)rng.NextDouble() * 84f;
                float z = -39f + (float)rng.NextDouble() * 41f;
                if (z > -6f) z -= 8f;
                placed.transform.localPosition = new Vector3(x, 0.01f, z);
                placed.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                float s = 0.7f + (float)rng.NextDouble() * 0.8f;
                placed.transform.localScale = Vector3.one * s;
                placedCount++;
            }
            return placedCount;
        }
        finally
        {
            Object.DestroyImmediate(temp);
        }
    }

    static void PopulateFallbackBillboardFoliage(Transform parent, int count)
    {
        Material treeMat = CreateTransparentTexturedFromPath("StoreFlow_TreeBillboard", TreeBillboardTexturePath, Color.white);
        Material bushMat = CreateTransparentTexturedFromPath("StoreFlow_BushBillboard", BushBillboardTexturePath, Color.white);
        System.Random rng = new System.Random(271828);

        for (int i = 0; i < Mathf.Max(0, count); i++)
        {
            bool useTree = i % 3 != 0;
            float x = -44f + (float)rng.NextDouble() * 88f;
            float z = -40f + (float)rng.NextDouble() * 42f;
            if (z > -5f) z -= 9f;
            float h = useTree ? 5f + (float)rng.NextDouble() * 3f : 1.8f + (float)rng.NextDouble() * 1.2f;
            float w = h * (useTree ? 0.55f : 0.9f);
            Material m = useTree ? treeMat : bushMat;

            GameObject q = CreatePrim($"BillboardFoliage_{i}", PrimitiveType.Quad, parent,
                new Vector3(x, h * 0.5f, z), new Vector3(0f, (float)rng.NextDouble() * 360f, 0f), new Vector3(w, h, 1f), m, false);
            q.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
        }
    }

    static void BuildStreetProps(Transform root, Material concrete, Material trim, Material lightMat)
    {
        GameObject props = new GameObject("StreetProps");
        props.transform.SetParent(root, false);

        Material poleMat = CreateTexturedLitFromPath("StoreFlow_Pole", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Metal_10.jpg", Color.white, new Vector2(1f, 2f));
        Material posterMat = CreateTexturedLitFromPath("StoreFlow_Poster", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/outdoor electronics.jpg", Color.white, new Vector2(1f, 1f));
        Material railMat = CreateTexturedLitFromPath("StoreFlow_Rail", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/RoadRail.jpg", Color.white, new Vector2(4f, 1f));

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

    static void BuildStore(Transform root, Material wall, Material roof, Material sign, Material glass, Material doorFrame,
        Material floor, Material floorStripe, Material shelf, Material trim,
        Material productA, Material productB, Material productC, Material productD, Material productE, Material productF,
        Material lightMat, Material freezerBody, Material freezerTrim, Material freezerGlow, Material counter,
        Material counterTop, Material register, Material vendingBody, Material vendingGlow,
        out Transform leftDoor, out Transform rightDoor)
    {
        GameObject store = new GameObject("Store");
        store.transform.SetParent(root, false);

        CreatePrim("BackWall", PrimitiveType.Cube, store.transform, new Vector3(0f, 2f, 17.7f), Vector3.zero, new Vector3(14f, 4f, 0.24f), wall, true);
        CreatePrim("LeftWall", PrimitiveType.Cube, store.transform, new Vector3(-6.88f, 2f, 12.8f), Vector3.zero, new Vector3(0.24f, 4f, 10f), wall, true);
        CreatePrim("RightWall", PrimitiveType.Cube, store.transform, new Vector3(6.88f, 2f, 12.8f), Vector3.zero, new Vector3(0.24f, 4f, 10f), wall, true);
        CreatePrim("FrontWallLeft", PrimitiveType.Cube, store.transform, new Vector3(-4.2f, 2f, 8.02f), Vector3.zero, new Vector3(5.6f, 4f, 0.24f), wall, true);
        CreatePrim("FrontWallRight", PrimitiveType.Cube, store.transform, new Vector3(4.2f, 2f, 8.02f), Vector3.zero, new Vector3(5.6f, 4f, 0.24f), wall, true);
        CreatePrim("Roof", PrimitiveType.Cube, store.transform, new Vector3(0f, 4.05f, 12.8f), Vector3.zero, new Vector3(14.2f, 0.24f, 10.4f), roof, true);
        CreatePrim("InteriorFloor", PrimitiveType.Cube, store.transform, new Vector3(0f, 0.02f, 12.8f), Vector3.zero, new Vector3(13.8f, 0.08f, 9.8f), floor, true);
        for (int i = 0; i < 7; i++)
        {
            CreatePrim($"FloorStripe_{i}", PrimitiveType.Cube, store.transform,
                new Vector3(0f, 0.07f, 8.9f + i * 1.2f), Vector3.zero, new Vector3(13.2f, 0.01f, 0.42f), floorStripe, false);
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

        for (int i = 0; i < 3; i++)
        {
            float x = -3.9f + i * 3.9f;
            BuildShelfAisle(store.transform, new Vector3(x, 0f, 12.85f), shelf, trim,
                productA, productB, productC, productD, productE, productF);
        }

        BuildBackFreezers(store.transform, freezerBody, freezerTrim, freezerGlow, glass, productA, productB, productC, productD);
        BuildCheckoutCorner(store.transform, counter, counterTop, register, trim, productA, productB, productC);
        BuildVendingMachine(store.transform, vendingBody, vendingGlow, glass);

        for (int i = 0; i < 6; i++)
        {
            float z = 10.4f + i * 1.25f;
            CreatePrim($"LightStrip_{i}", PrimitiveType.Cube, store.transform, new Vector3(0f, 3.35f, z), Vector3.zero, new Vector3(9.5f, 0.06f, 0.18f), lightMat, false);
            AddLight(store.transform, $"InteriorLight_{i}", new Vector3(0f, 3.05f, z), LightType.Point, new Color(1f, 0.98f, 0.9f), 0.55f, 5.5f, 0f);
        }
    }

    static void BuildShelfAisle(Transform parent, Vector3 pos, Material shelf, Material trim,
        Material productA, Material productB, Material productC, Material productD, Material productE, Material productF)
    {
        GameObject aisle = new GameObject("Aisle");
        aisle.transform.SetParent(parent, false);
        aisle.transform.localPosition = pos;

        CreatePrim("ShelfBack", PrimitiveType.Cube, aisle.transform, new Vector3(0f, 1.25f, 0f), Vector3.zero, new Vector3(2.4f, 2.5f, 0.12f), shelf, true);
        CreatePrim("ShelfBase", PrimitiveType.Cube, aisle.transform, new Vector3(0f, 0.18f, 0.4f), Vector3.zero, new Vector3(2.4f, 0.18f, 0.9f), shelf, true);
        Material[] mats = { productA, productB, productC, productD, productE, productF };
        for (int i = 0; i < 4; i++)
        {
            float y = 0.45f + i * 0.48f;
            CreatePrim($"Shelf_{i}", PrimitiveType.Cube, aisle.transform, new Vector3(0f, y, 0.35f), Vector3.zero, new Vector3(2.32f, 0.08f, 0.86f), shelf, true);
            CreatePrim($"ShelfLip_{i}", PrimitiveType.Cube, aisle.transform, new Vector3(0f, y + 0.04f, 0.76f), Vector3.zero, new Vector3(2.32f, 0.12f, 0.05f), trim, true);
        }

        for (int row = 0; row < 4; row++)
        {
            float y = 0.56f + row * 0.48f;
            for (int side = 0; side < 2; side++)
            {
                float z = side == 0 ? 0.56f : -0.56f;
                for (int col = 0; col < 6; col++)
                {
                    Material mat = mats[(row * 2 + col + side) % mats.Length];
                    float x = -1.0f + col * 0.4f;
                    Vector3 productPos = new Vector3(x, y + 0.1f, z);
                    int shape = (row + col + side) % 5;
                    CreateStockItem(aisle.transform, $"Product_{row}_{side}_{col}", productPos, shape, mat, trim);
                }
            }
        }
    }

    static void BuildCar(Transform parent, Material bodyMat, Material trimMat, Material glassMat, Material tireMat,
        Material wheelMat, Material headlightMat, Material taillightMat, Material interiorMat)
    {
        GameObject carPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RenaultFbxPath);
        if (carPrefab != null)
        {
            GameObject car = (GameObject)PrefabUtility.InstantiatePrefab(carPrefab);
            car.name = "PSX_Renault";
            car.transform.SetParent(parent, false);
            // Keep exact world placement from the existing ParkedCar anchor.
            car.transform.localPosition = Vector3.zero;
            car.transform.localRotation = Quaternion.identity;
            car.transform.localScale = Vector3.one;
            ApplyCarMaterials(car, bodyMat, trimMat, glassMat, tireMat, wheelMat, headlightMat, taillightMat, interiorMat);
            return;
        }

        Debug.LogWarning("Store Flow Scene: Renault model is not imported as a Unity model in this project. Falling back to procedural car.");
        parent.localScale = Vector3.one * 0.32f;
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
        AddLight(root, "StoreGlow", new Vector3(0f, 2.9f, 9.6f), LightType.Point, new Color(1f, 0.92f, 0.82f), 1.7f, 10f, 0f);
        AddLight(root, "StreetLampLeft", new Vector3(-5.4f, 4.2f, 7.5f), LightType.Point, new Color(1f, 0.84f, 0.6f), 1.4f, 8f, 0f);
        AddLight(root, "StreetLampRight", new Vector3(5.4f, 4.2f, 7.5f), LightType.Point, new Color(1f, 0.84f, 0.6f), 1.4f, 8f, 0f);
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
    }

    static void EnsureSceneBootstrap()
    {
        if (Object.FindAnyObjectByType<GameFlowManager>() == null)
            new GameObject("GameFlowManager").AddComponent<GameFlowManager>();

        if (Object.FindAnyObjectByType<MainMenuUI>() == null)
            new GameObject("MainMenuUI").AddComponent<MainMenuUI>();
    }

    static void RemoveDirectionalLights()
    {
        foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type == LightType.Directional)
                Object.DestroyImmediate(light.gameObject);
        }
    }

    static void DestroyRoot(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
            Object.DestroyImmediate(existing);
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
            Object.DestroyImmediate(collider);
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
                for (int item = 0; item < 5; item++)
                {
                    float itemX = -0.8f + item * 0.4f;
                    CreateStockItem(bay.transform, $"Frozen_{shelf}_{item}", new Vector3(itemX, y + 0.12f, -0.06f),
                        (shelf + item) % 4, mats[(shelf + item) % mats.Length], trim);
                }
            }
        }
    }

    static void BuildCheckoutCorner(Transform parent, Material counter, Material counterTop, Material register, Material trim,
        Material productA, Material productB, Material productC)
    {
        GameObject checkout = new GameObject("CheckoutCorner");
        checkout.transform.SetParent(parent, false);
        checkout.transform.localPosition = new Vector3(4.9f, 0f, 9.9f);

        CreatePrim("CounterBase", PrimitiveType.Cube, checkout.transform, new Vector3(0f, 0.45f, 0f), Vector3.zero, new Vector3(3.4f, 0.9f, 0.8f), counter, true);
        CreatePrim("CounterTop", PrimitiveType.Cube, checkout.transform, new Vector3(0f, 0.93f, 0f), Vector3.zero, new Vector3(3.5f, 0.05f, 0.9f), counterTop, true);
        CreatePrim("CounterSide", PrimitiveType.Cube, checkout.transform, new Vector3(-1.35f, 0.45f, 1.4f), Vector3.zero, new Vector3(0.8f, 0.9f, 2.8f), counter, true);
        CreatePrim("CounterTopSide", PrimitiveType.Cube, checkout.transform, new Vector3(-1.35f, 0.93f, 1.4f), Vector3.zero, new Vector3(0.9f, 0.05f, 2.9f), counterTop, true);
        CreatePrim("Register", PrimitiveType.Cube, checkout.transform, new Vector3(1.0f, 1.15f, -0.05f), Vector3.zero, new Vector3(0.38f, 0.28f, 0.32f), register, false);
        CreatePrim("RegisterScreen", PrimitiveType.Cube, checkout.transform, new Vector3(1.02f, 1.42f, 0.06f), new Vector3(-22f, 0f, 0f), new Vector3(0.26f, 0.18f, 0.02f), CreateEmissive("StoreFlow_RegisterGlow", new Color(0.24f, 0.78f, 0.4f), 0.8f), false);

        for (int i = 0; i < 6; i++)
        {
            Material mat = i % 3 == 0 ? productA : (i % 3 == 1 ? productB : productC);
            CreateStockItem(checkout.transform, $"Impulse_{i}", new Vector3(-1.65f, 1.08f, 0.3f + i * 0.36f), i % 5, mat, trim);
        }
    }

    static void BuildVendingMachine(Transform parent, Material body, Material glow, Material glass)
    {
        GameObject vending = new GameObject("VendingMachine");
        vending.transform.SetParent(parent, false);
        vending.transform.localPosition = new Vector3(-5.8f, 0f, 9.8f);

        CreatePrim("Body", PrimitiveType.Cube, vending.transform, new Vector3(0f, 1.2f, 0f), Vector3.zero, new Vector3(1.15f, 2.4f, 0.9f), body, true);
        CreatePrim("FrontGlass", PrimitiveType.Cube, vending.transform, new Vector3(0f, 1.22f, -0.43f), Vector3.zero, new Vector3(0.8f, 1.75f, 0.03f), glass, false);
        CreatePrim("DisplayGlow", PrimitiveType.Cube, vending.transform, new Vector3(0f, 1.22f, -0.39f), Vector3.zero, new Vector3(0.7f, 1.58f, 0.03f), glow, false);
        CreatePrim("PaymentPanel", PrimitiveType.Cube, vending.transform, new Vector3(0.36f, 1.0f, -0.41f), Vector3.zero, new Vector3(0.14f, 0.42f, 0.04f), CreateLit("StoreFlow_Panel", new Color(0.12f, 0.12f, 0.14f)), false);
        for (int i = 0; i < 4; i++)
        {
            CreatePrim($"DrinkRow_{i}", PrimitiveType.Cube, vending.transform, new Vector3(0f, 0.72f + i * 0.32f, -0.36f), Vector3.zero, new Vector3(0.6f, 0.06f, 0.05f), CreateLabelMaterial($"StoreFlow_Drink_{i}", new Color(0.18f + i * 0.12f, 0.34f, 0.72f - i * 0.08f), new Color(0.94f, 0.94f, 0.98f)), false);
        }
    }

    static void ApplyCarMaterials(GameObject car, Material bodyMat, Material trimMat, Material glassMat, Material tireMat,
        Material wheelMat, Material headlightMat, Material taillightMat, Material interiorMat)
    {
        foreach (Renderer renderer in car.GetComponentsInChildren<Renderer>(true))
        {
            string name = renderer.gameObject.name.ToLowerInvariant();
            Material mat = bodyMat;
            if (name.Contains("glass") || name.Contains("window"))
                mat = glassMat;
            else if (name.Contains("headlight"))
                mat = headlightMat;
            else if (name.Contains("brakelight") || name.Contains("litfull") || name == "litsmd" || name == "lit_1smd")
                mat = taillightMat;
            else if (name.Contains("tire"))
                mat = tireMat;
            else if (name.Contains("wheel"))
                mat = wheelMat;
            else if (name.Contains("interior") || name.Contains("steering") || name == "root" || name == "root_1")
                mat = interiorMat;
            else if (name.Contains("chrome") || name.Contains("misc") || name.Contains("engine") || name.Contains("exhaust"))
                mat = trimMat;
            renderer.sharedMaterial = mat;
        }
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

    static Shader FindShader()
    {
        return Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
    }
}
