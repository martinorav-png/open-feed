using UnityEngine;
using UnityEditor;

public class GroceryStoreSceneGenerator : Editor
{
    enum ProductShape
    {
        Carton,
        TallBox,
        FlatPack,
        HangingPack,
        CounterCard,
        Can
    }

    struct ProductDefinition
    {
        public string Name;
        public ProductShape Shape;
        public Vector3 Size;
        public Material LabelMat;
        public Material BodyMat;
        public Material AccentMat;

        public ProductDefinition(string name, ProductShape shape, Vector3 size, Material labelMat, Material bodyMat, Material accentMat = null)
        {
            Name = name;
            Shape = shape;
            Size = size;
            LabelMat = labelMat;
            BodyMat = bodyMat;
            AccentMat = accentMat;
        }
    }

    [MenuItem("OPEN FEED/Scripts/Grocery Store - Clear")]
    static void ClearScene()
    {
        GameObject existing = GameObject.Find("GroceryStoreScene");
        if (existing != null)
        {
            DestroyImmediate(existing);
            Debug.Log("OPENFEED Grocery Store Scene cleared.");
        }
    }

    [MenuItem("OPEN FEED/Scripts/Grocery Store")]
    static void GenerateScene()
    {
        GameObject existing = GameObject.Find("GroceryStoreScene");
        if (existing != null) DestroyImmediate(existing);

        // Remove existing directional lights
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light l in allLights)
        {
            if (l.type == LightType.Directional)
                DestroyImmediate(l.gameObject);
        }

        GameObject root = new GameObject("GroceryStoreScene");

        GameObject sunObj = new GameObject("StoreSun");
        sunObj.transform.parent = root.transform;
        sunObj.transform.localRotation = Quaternion.Euler(50f, -30f, 0f);
        Light sun = sunObj.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.99f, 0.96f);
        sun.intensity = 1.6f;
        sun.shadows = LightShadows.None;

        // ============================
        // MATERIALS
        // ============================
        Material matFloor = CreateTextured("StoreFloor", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Tiles.jpg",
            new Color(0.92f, 0.92f, 0.92f), new Vector2(4f, 5f));
        Material matFloorTile = CreateTextured("FloorTile", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Concrete_01.jpg",
            new Color(0.86f, 0.86f, 0.86f), new Vector2(6f, 1f));
        Material matWall = CreateTextured("StoreWall", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Concrete.jpg",
            new Color(0.78f, 0.78f, 0.78f), new Vector2(3f, 2f));
        Material matCeiling = CreateMatte("Ceiling", new Color(0.99f, 0.99f, 0.99f));
        Material matShelf = CreateMatte("Shelf", new Color(0.9f, 0.9f, 0.92f));
        Material matShelfBack = CreateMatte("ShelfBack", new Color(0.94f, 0.94f, 0.95f));
        Material matShelfMetal = CreateTextured("ShelfMetal", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Metal.jpg",
            new Color(0.72f, 0.74f, 0.77f), new Vector2(1f, 2f));
        Material matProduct1 = CreateUnlitTextured("Product_ItemAtlasA", "Assets/supermarket-items-pack-psx-ps1-style/textures/ItemTexture.png",
            Color.white, Vector2.one);
        Material matProduct2 = CreateUnlitTextured("Product_ItemAtlasB", "Assets/supermarket-items-pack-psx-ps1-style/textures/GroceryItemsTexture.png",
            Color.white, Vector2.one);
        Material matProduct3 = CreateUnlitTextured("Product_Cigars", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Cigars.png",
            Color.white, Vector2.one);
        Material matProduct4 = CreateUnlitTextured("Product_Hygiene", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Hygiene.jpg",
            Color.white, Vector2.one);
        Material matProduct5 = CreateUnlitTextured("Product_Toothbrushes", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Toothbrushes.jpg",
            Color.white, Vector2.one);
        Material matProduct6 = CreateUnlitTextured("Product_IceCream", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/ice_cream_popsicles.png",
            Color.white, Vector2.one);
        Material matCounter = CreateMatte("Counter", new Color(0.87f, 0.87f, 0.88f));
        Material matCounterTop = CreateMatte("CounterTop", new Color(0.96f, 0.96f, 0.97f));
        Material matRegister = CreateMatte("Register", new Color(0.22f, 0.22f, 0.24f));
        Material matFreezer = CreateMatte("Freezer", new Color(0.94f, 0.95f, 0.98f));
        Material matFreezerInterior = CreateMatte("FreezerInterior", new Color(0.98f, 0.99f, 1f));
        Material matFreezerTrim = CreateMatte("FreezerTrim", new Color(0.76f, 0.8f, 0.86f));
        Material matFreezerGlass = CreateTransparent("FreezerGlass", new Color(0.82f, 0.92f, 1f, 0.2f), 0.06f, 0f);
        Material matFluorescent = CreateEmissive("Fluorescent", new Color(1f, 0.98f, 0.92f),
            new Color(1f, 0.98f, 0.92f), 7f);
        Material matSignage = CreateUnlitTextured("Signage", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Sign_01.png",
            Color.white, new Vector2(1f, 1f));
        Material matDoor = CreateTextured("StoreDoor", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Door.jpg",
            new Color(0.15f, 0.15f, 0.16f), new Vector2(1f, 1f));
        Material matDoorFrame = CreateTextured("DoorFrame", "Assets/ModelsPlace/6twelve/source/6twelve/Textures/Metal_01.jpg",
            new Color(0.3f, 0.3f, 0.32f), new Vector2(1f, 1f));
        Material matBasket = CreateMatte("Basket", new Color(0.88f, 0.16f, 0.12f));
        Material matMetal = CreateMatte("Metal", new Color(0.7f, 0.72f, 0.75f));
        Material matBelt = CreateUnlitTextured("Belt", "Assets/supermarket-items-pack-psx-ps1-style/textures/ConveyerBeltTexture.png",
            Color.white, new Vector2(1f, 1f));
        Material matLinoleum = CreateMatte("Linoleum", new Color(0.93f, 0.93f, 0.93f));

        // ============================
        // STORE STRUCTURE (interior box)
        // ============================
        float storeW = 16f; // width (x)
        float storeH = 3.5f; // height (y)
        float storeD = 20f; // depth (z)

        GameObject structure = new GameObject("Structure");
        structure.transform.parent = root.transform;

        // Floor
        CreatePrim("Floor", PrimitiveType.Cube, structure.transform,
            new Vector3(0, -0.05f, 0), Vector3.zero, new Vector3(storeW, 0.1f, storeD), matFloor);

        // Floor tile pattern (alternating strips)
        for (int i = 0; i < 10; i++)
        {
            float z = -9f + i * 2f;
            CreatePrim($"FloorStrip{i}", PrimitiveType.Cube, structure.transform,
                new Vector3(0, 0.005f, z), Vector3.zero, new Vector3(storeW - 0.5f, 0.005f, 0.8f), matFloorTile);
        }

        // Linoleum near entrance
        CreatePrim("EntranceMat", PrimitiveType.Cube, structure.transform,
            new Vector3(0, 0.006f, -9.5f), Vector3.zero, new Vector3(3f, 0.008f, 1.5f), matLinoleum);

        // Walls
        CreatePrim("WallBack", PrimitiveType.Cube, structure.transform,
            new Vector3(0, storeH / 2, storeD / 2), Vector3.zero, new Vector3(storeW, storeH, 0.2f), matWall);
        CreatePrim("WallLeft", PrimitiveType.Cube, structure.transform,
            new Vector3(-storeW / 2, storeH / 2, 0), Vector3.zero, new Vector3(0.2f, storeH, storeD), matWall);
        CreatePrim("WallRight", PrimitiveType.Cube, structure.transform,
            new Vector3(storeW / 2, storeH / 2, 0), Vector3.zero, new Vector3(0.2f, storeH, storeD), matWall);

        // Front wall with door opening
        CreatePrim("WallFrontL", PrimitiveType.Cube, structure.transform,
            new Vector3(-4.5f, storeH / 2, -storeD / 2), Vector3.zero, new Vector3(7f, storeH, 0.2f), matWall);
        CreatePrim("WallFrontR", PrimitiveType.Cube, structure.transform,
            new Vector3(4.5f, storeH / 2, -storeD / 2), Vector3.zero, new Vector3(7f, storeH, 0.2f), matWall);
        CreatePrim("WallFrontTop", PrimitiveType.Cube, structure.transform,
            new Vector3(0, 3f, -storeD / 2), Vector3.zero, new Vector3(2.2f, 1f, 0.2f), matWall);

        // Ceiling
        CreatePrim("Ceiling", PrimitiveType.Cube, structure.transform,
            new Vector3(0, storeH, 0), Vector3.zero, new Vector3(storeW, 0.15f, storeD), matCeiling);

        // Door frame
        CreatePrim("DoorFrameL", PrimitiveType.Cube, structure.transform,
            new Vector3(-1.05f, 1.25f, -storeD / 2), Vector3.zero, new Vector3(0.1f, 2.5f, 0.25f), matDoorFrame);
        CreatePrim("DoorFrameR", PrimitiveType.Cube, structure.transform,
            new Vector3(1.05f, 1.25f, -storeD / 2), Vector3.zero, new Vector3(0.1f, 2.5f, 0.25f), matDoorFrame);

        // Sliding doors (slightly open)
        CreatePrim("DoorL", PrimitiveType.Cube, structure.transform,
            new Vector3(-0.65f, 1.25f, -storeD / 2 + 0.05f), Vector3.zero, new Vector3(0.7f, 2.4f, 0.04f), matDoor);
        CreatePrim("DoorR", PrimitiveType.Cube, structure.transform,
            new Vector3(0.65f, 1.25f, -storeD / 2 + 0.05f), Vector3.zero, new Vector3(0.7f, 2.4f, 0.04f), matDoor);

        // ============================
        // FLUORESCENT LIGHTS
        // ============================
        GameObject lights = new GameObject("Lights");
        lights.transform.parent = root.transform;

        for (int row = 0; row < 4; row++)
        {
            float z = -6f + row * 5f;
            for (int col = 0; col < 3; col++)
            {
                float x = -4.5f + col * 4.5f;
                string name = $"Light_R{row}_C{col}";

                // Fluorescent fixture (visible tube)
                CreatePrim(name + "_Fixture", PrimitiveType.Cube, lights.transform,
                    new Vector3(x, storeH - 0.12f, z), Vector3.zero, new Vector3(1.8f, 0.04f, 0.15f), matFluorescent);

                // Actual light
                GameObject lightObj = new GameObject(name);
                lightObj.transform.parent = lights.transform;
                lightObj.transform.localPosition = new Vector3(x, storeH - 0.3f, z);
                Light lt = lightObj.AddComponent<Light>();
                lt.type = LightType.Point;
                lt.color = new Color(1f, 0.98f, 0.94f);
                lt.intensity = 4.2f;
                lt.range = 8f;
                lt.shadows = LightShadows.None;
            }
        }

        GameObject fillLightObj = new GameObject("StoreFillLight");
        fillLightObj.transform.parent = lights.transform;
        fillLightObj.transform.localPosition = new Vector3(0f, storeH - 0.6f, 0f);
        Light fillLight = fillLightObj.AddComponent<Light>();
        fillLight.type = LightType.Point;
        fillLight.color = new Color(1f, 1f, 1f);
        fillLight.intensity = 3.8f;
        fillLight.range = 18f;
        fillLight.shadows = LightShadows.None;

        // ============================
        // SHELVING AISLES (3 aisles)
        // ============================
        ProductDefinition[] productDefs =
        {
            new ProductDefinition("PackA", ProductShape.Carton, new Vector3(0.12f, 0.16f, 0.24f), matProduct1,
                CreateMatte("ProdBody_PackA", new Color(0.32f, 0.22f, 0.18f)),
                CreateMatte("ProdAccent_PackA", new Color(0.68f, 0.58f, 0.32f))),
            new ProductDefinition("PackB", ProductShape.TallBox, new Vector3(0.13f, 0.24f, 0.16f), matProduct2,
                CreateMatte("ProdBody_PackB", new Color(0.86f, 0.87f, 0.9f)),
                CreateMatte("ProdAccent_PackB", new Color(0.58f, 0.72f, 0.86f))),
            new ProductDefinition("Cigars", ProductShape.FlatPack, new Vector3(0.06f, 0.17f, 0.22f), matProduct3,
                CreateMatte("ProdBody_Cigars", new Color(0.48f, 0.26f, 0.18f)),
                CreateMatte("ProdAccent_Cigars", new Color(0.74f, 0.62f, 0.28f))),
            new ProductDefinition("Hygiene", ProductShape.TallBox, new Vector3(0.11f, 0.22f, 0.15f), matProduct4,
                CreateMatte("ProdBody_Hygiene", new Color(0.94f, 0.95f, 0.97f)),
                CreateMatte("ProdAccent_Hygiene", new Color(0.45f, 0.65f, 0.82f))),
            new ProductDefinition("Toothbrushes", ProductShape.HangingPack, new Vector3(0.05f, 0.28f, 0.13f), matProduct5,
                CreateMatte("ProdBody_Toothbrushes", new Color(0.96f, 0.97f, 0.98f)),
                CreateMatte("ProdAccent_Toothbrushes", new Color(0.22f, 0.34f, 0.58f))),
            new ProductDefinition("IceCream", ProductShape.FlatPack, new Vector3(0.12f, 0.14f, 0.28f), matProduct6,
                CreateMatte("ProdBody_IceCream", new Color(0.22f, 0.5f, 0.6f)),
                CreateMatte("ProdAccent_IceCream", new Color(0.94f, 0.68f, 0.34f)))
        };
        GameObject aisles = new GameObject("Aisles");
        aisles.transform.parent = root.transform;

        for (int aisle = 0; aisle < 3; aisle++)
        {
            float aisleX = -5f + aisle * 4.5f;
            GameObject aisleObj = new GameObject($"Aisle_{aisle}");
            aisleObj.transform.parent = aisles.transform;

            // Double-sided shelf unit
            CreateShelfUnit(aisleObj.transform, aisleX, 0f, matShelf, matShelfBack, matShelfMetal, productDefs, aisle);
        }

        // ============================
        // BACK WALL FREEZERS
        // ============================
        GameObject freezers = new GameObject("Freezers");
        freezers.transform.parent = root.transform;

        for (int i = 0; i < 5; i++)
        {
            float x = -6f + i * 3f;
            CreateFreezerBay(freezers.transform, i, x, 9.3f, matFreezer, matFreezerInterior, matFreezerTrim,
                matFreezerGlass, productDefs, matMetal);
        }

        // ============================
        // CHECKOUT COUNTER
        // ============================
        GameObject checkout = new GameObject("Checkout");
        checkout.transform.parent = root.transform;

        // Counter body
        CreatePrim("CounterBase", PrimitiveType.Cube, checkout.transform,
            new Vector3(5.5f, 0.45f, -6f), Vector3.zero, new Vector3(3.5f, 0.9f, 0.7f), matCounter);
        CreatePrim("CounterTop", PrimitiveType.Cube, checkout.transform,
            new Vector3(5.5f, 0.92f, -6f), Vector3.zero, new Vector3(3.6f, 0.05f, 0.8f), matCounterTop);

        // Conveyor belt
        CreatePrim("Belt", PrimitiveType.Cube, checkout.transform,
            new Vector3(5.5f, 0.93f, -6f), Vector3.zero, new Vector3(2.8f, 0.02f, 0.5f), matBelt);

        // Cash register
        CreatePrim("Register", PrimitiveType.Cube, checkout.transform,
            new Vector3(6.8f, 1.15f, -6f), Vector3.zero, new Vector3(0.4f, 0.35f, 0.35f), matRegister);
        CreatePrim("RegisterScreen", PrimitiveType.Cube, checkout.transform,
            new Vector3(6.8f, 1.45f, -5.9f), new Vector3(-15, 0, 0), new Vector3(0.3f, 0.25f, 0.02f),
            CreateEmissive("Screen", new Color(0.05f, 0.15f, 0.05f), new Color(0.1f, 0.3f, 0.1f), 0.5f));

        // Divider / lane guide
        CreatePrim("LaneGuide", PrimitiveType.Cube, checkout.transform,
            new Vector3(5.5f, 0.5f, -7.5f), Vector3.zero, new Vector3(0.05f, 1f, 3f), matMetal);

        // ============================
        // SHOPPING BASKET (near entrance)
        // ============================
        CreatePrim("BasketStack", PrimitiveType.Cube, root.transform,
            new Vector3(-2.5f, 0.3f, -8.5f), new Vector3(0, 5, 0), new Vector3(0.5f, 0.6f, 0.35f), matBasket);

        // ============================
        // SIGNAGE (above aisles)
        // ============================
        CreateDoubleSidedSign("Sign_Aisle0", root.transform, new Vector3(-5f, 3f, 1f), new Vector2(2f, 0.35f), matSignage, matMetal);
        CreateDoubleSidedSign("Sign_Aisle1", root.transform, new Vector3(-0.5f, 3f, 1f), new Vector2(2f, 0.35f), matSignage, matMetal);
        CreateDoubleSidedSign("Sign_Aisle2", root.transform, new Vector3(4f, 3f, 1f), new Vector2(2f, 0.35f), matSignage, matMetal);

        // ============================
        // CAMERA + CUTSCENE WAYPOINTS
        // ============================
        Camera cam = SetupCamera(root.transform);
        cam.transform.localPosition = new Vector3(0, 1.5f, -8.5f);
        cam.transform.localRotation = Quaternion.Euler(5, 0, 0);
        cam.fieldOfView = 55f;
        cam.backgroundColor = new Color(0.78f, 0.82f, 0.88f);
        cam.clearFlags = CameraClearFlags.SolidColor;

        // Cutscene waypoints
        GameObject waypoints = new GameObject("CutsceneWaypoints");
        waypoints.transform.parent = root.transform;

        CreateWaypoint(waypoints.transform, "WP_Enter",
            new Vector3(0, 1.5f, -8.5f), new Vector3(5, 0, 0));
        CreateWaypoint(waypoints.transform, "WP_Aisle1Start",
            new Vector3(-5f, 1.4f, -4f), new Vector3(8, 20, 0));
        CreateWaypoint(waypoints.transform, "WP_Aisle1Mid",
            new Vector3(-5f, 1.4f, 2f), new Vector3(5, 15, 0));
        CreateWaypoint(waypoints.transform, "WP_Aisle1End",
            new Vector3(-5f, 1.4f, 6f), new Vector3(5, 0, 0));
        CreateWaypoint(waypoints.transform, "WP_Freezer",
            new Vector3(-2f, 1.5f, 7f), new Vector3(5, 0, 0));
        CreateWaypoint(waypoints.transform, "WP_Checkout",
            new Vector3(3.5f, 1.4f, -5f), new Vector3(8, 35, 0));
        CreateWaypoint(waypoints.transform, "WP_Exit",
            new Vector3(0, 1.5f, -8f), new Vector3(5, 180, 0));

        // ============================
        // CHARACTER (6twelve model)
        // ============================
        GameObject charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ModelsPlace/6twelve/scene.gltf");
        if (charPrefab == null)
            charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ModelsPlace/6twelve/scene.glb");

        if (charPrefab != null)
        {
            GameObject character = (GameObject)PrefabUtility.InstantiatePrefab(charPrefab);
            character.name = "Character";
            character.transform.parent = root.transform;
            character.transform.localPosition = new Vector3(0, 0, -8f);
            character.transform.localRotation = Quaternion.Euler(0, 0, 0);
            character.transform.localScale = Vector3.one * 1f;
        }
        else
        {
            // Fallback character placeholder
            GameObject character = new GameObject("Character");
            character.transform.parent = root.transform;
            CreatePrim("CharBody", PrimitiveType.Capsule, character.transform,
                new Vector3(0, 0.9f, -8f), Vector3.zero, new Vector3(0.5f, 0.9f, 0.5f),
                CreateMatte("CharMat", new Color(0.06f, 0.06f, 0.07f)));
            CreatePrim("CharHead", PrimitiveType.Sphere, character.transform,
                new Vector3(0, 1.9f, -8f), Vector3.zero, new Vector3(0.35f, 0.35f, 0.35f),
                CreateMatte("CharHeadMat", new Color(0.12f, 0.09f, 0.07f)));
        }

        // ============================
        // RENDER SETTINGS
        // ============================
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.32f, 0.34f, 0.37f);
        RenderSettings.reflectionIntensity = 0.7f;
        RenderSettings.fog = false;

        Selection.activeGameObject = root;
        Debug.Log("OPENFEED Grocery Store Scene generated.");
    }

    // ============================
    // SHELF UNIT GENERATOR
    // ============================
    static void CreateShelfUnit(Transform parent, float x, float z,
        Material shelfMat, Material backMat, Material metalMat, ProductDefinition[] productDefs, int aisleSeed)
    {
        float shelfLen = 8f;
        float shelfDepth = 0.6f;
        int shelfCount = 4;
        float unitHalfWidth = shelfDepth + 0.12f;

        // Back panel
        CreatePrim("Back", PrimitiveType.Cube, parent,
            new Vector3(x, 1.2f, z), Vector3.zero, new Vector3(0.04f, 2.4f, shelfLen), backMat);

        for (int post = 0; post < 4; post++)
        {
            float postZ = z - shelfLen / 2 + 0.2f + post * ((shelfLen - 0.4f) / 3f);
            CreatePrim($"PostL_{post}", PrimitiveType.Cube, parent,
                new Vector3(x - unitHalfWidth, 1.2f, postZ), Vector3.zero, new Vector3(0.05f, 2.4f, 0.05f), metalMat);
            CreatePrim($"PostR_{post}", PrimitiveType.Cube, parent,
                new Vector3(x + unitHalfWidth, 1.2f, postZ), Vector3.zero, new Vector3(0.05f, 2.4f, 0.05f), metalMat);
        }

        // Shelf boards + products on each side
        for (int s = 0; s < shelfCount; s++)
        {
            float y = 0.25f + s * 0.6f;

            // Left-side shelf
            CreatePrim($"ShelfL_{s}", PrimitiveType.Cube, parent,
                new Vector3(x - shelfDepth / 2 - 0.02f, y, z), Vector3.zero,
                new Vector3(shelfDepth, 0.03f, shelfLen - 0.2f), shelfMat);

            // Right-side shelf
            CreatePrim($"ShelfR_{s}", PrimitiveType.Cube, parent,
                new Vector3(x + shelfDepth / 2 + 0.02f, y, z), Vector3.zero,
                new Vector3(shelfDepth, 0.03f, shelfLen - 0.2f), shelfMat);

            CreatePrim($"ShelfEdgeL_{s}", PrimitiveType.Cube, parent,
                new Vector3(x - shelfDepth - 0.01f, y + 0.03f, z), Vector3.zero,
                new Vector3(0.025f, 0.04f, shelfLen - 0.2f), shelfMat);
            CreatePrim($"ShelfEdgeR_{s}", PrimitiveType.Cube, parent,
                new Vector3(x + shelfDepth + 0.01f, y + 0.03f, z), Vector3.zero,
                new Vector3(0.025f, 0.04f, shelfLen - 0.2f), shelfMat);
            CreatePrim($"ShelfBraceL_{s}", PrimitiveType.Cube, parent,
                new Vector3(x - shelfDepth - 0.05f, y + 0.01f, z), Vector3.zero,
                new Vector3(0.035f, 0.03f, shelfLen - 0.18f), metalMat);
            CreatePrim($"ShelfBraceR_{s}", PrimitiveType.Cube, parent,
                new Vector3(x + shelfDepth + 0.05f, y + 0.01f, z), Vector3.zero,
                new Vector3(0.035f, 0.03f, shelfLen - 0.18f), metalMat);

            // Products on shelves using shaped packages with atlas labels.
            int seed = aisleSeed * 100 + s * 10;
            PopulateShelf(parent, x - shelfDepth / 2 - 0.12f, y + 0.02f, z, shelfLen, -1, productDefs, seed);
            PopulateShelf(parent, x + shelfDepth / 2 + 0.12f, y + 0.02f, z, shelfLen, 1, productDefs, seed + 5);
        }

        // Side panels (endcaps)
        CreatePrim("EndcapFront", PrimitiveType.Cube, parent,
            new Vector3(x, 1.2f, z - shelfLen / 2), Vector3.zero, new Vector3(shelfDepth * 2 + 0.1f, 2.4f, 0.03f), shelfMat);
        CreatePrim("EndcapBack", PrimitiveType.Cube, parent,
            new Vector3(x, 1.2f, z + shelfLen / 2), Vector3.zero, new Vector3(shelfDepth * 2 + 0.1f, 2.4f, 0.03f), shelfMat);
    }

    static void PopulateShelf(Transform parent, float x, float y, float z, float len,
        int side, ProductDefinition[] defs, int seed)
    {
        System.Random rng = new System.Random(seed);
        float startZ = z - len / 2 + 0.4f;
        float endZ = z + len / 2 - 0.4f;
        float cursor = startZ;

        int productIdx = 0;
        while (cursor < endZ)
        {
            ProductDefinition def = defs[rng.Next(defs.Length)];
            float widthJitter = 0.92f + (float)rng.NextDouble() * 0.16f;
            float heightJitter = 0.9f + (float)rng.NextDouble() * 0.18f;
            float depthJitter = 0.92f + (float)rng.NextDouble() * 0.14f;
            Vector3 size = new Vector3(
                def.Size.x * widthJitter,
                def.Size.y * heightJitter,
                def.Size.z * depthJitter);

            int count = 2 + rng.Next(3);
            float spacing = size.z + 0.03f + (float)rng.NextDouble() * 0.04f;
            float clusterLen = count * spacing;
            if (cursor + clusterLen > endZ)
                count = Mathf.Max(1, Mathf.FloorToInt((endZ - cursor) / spacing));

            for (int i = 0; i < count; i++)
            {
                float depthOffset = 0.08f + (float)rng.NextDouble() * 0.09f;
                float px = x + side * depthOffset;
                float pz = cursor + i * spacing;
                if (pz > endZ) break;

                float yaw = side < 0 ? 180f : 0f;
                CreateShelfProduct(parent, $"Prod_{seed}_{productIdx}_{def.Name}", def,
                    new Vector3(px, y, pz), yaw, side, size);
                productIdx++;
            }

            cursor += count * spacing + 0.05f + (float)rng.NextDouble() * 0.12f;

            if (rng.NextDouble() < 0.18f)
                cursor += 0.18f + (float)rng.NextDouble() * 0.2f;
        }
    }

    static void CreateFreezerBay(Transform parent, int index, float x, float z, Material shellMat,
        Material interiorMat, Material trimMat, Material glassMat, ProductDefinition[] defs, Material metalMat)
    {
        GameObject freezer = new GameObject($"Freezer_{index}");
        freezer.transform.parent = parent;
        freezer.transform.localPosition = new Vector3(x, 0f, z);

        CreatePrim("Cabinet", PrimitiveType.Cube, freezer.transform,
            new Vector3(0f, 1.2f, 0f), Vector3.zero, new Vector3(2.6f, 2.4f, 0.85f), shellMat);
        CreatePrim("Interior", PrimitiveType.Cube, freezer.transform,
            new Vector3(0f, 1.2f, -0.03f), Vector3.zero, new Vector3(2.2f, 2.08f, 0.62f), interiorMat);

        for (int shelf = 0; shelf < 4; shelf++)
        {
            float y = 0.48f + shelf * 0.48f;
            CreatePrim($"Shelf_{shelf}", PrimitiveType.Cube, freezer.transform,
                new Vector3(0f, y, -0.03f), Vector3.zero, new Vector3(2.12f, 0.03f, 0.56f), trimMat);
        }

        CreatePrim("FrameTop", PrimitiveType.Cube, freezer.transform,
            new Vector3(0f, 2.28f, -0.34f), Vector3.zero, new Vector3(2.32f, 0.08f, 0.08f), trimMat);
        CreatePrim("FrameBottom", PrimitiveType.Cube, freezer.transform,
            new Vector3(0f, 0.12f, -0.34f), Vector3.zero, new Vector3(2.32f, 0.08f, 0.08f), trimMat);
        CreatePrim("FrameLeft", PrimitiveType.Cube, freezer.transform,
            new Vector3(-1.12f, 1.2f, -0.34f), Vector3.zero, new Vector3(0.08f, 2.08f, 0.08f), trimMat);
        CreatePrim("FrameRight", PrimitiveType.Cube, freezer.transform,
            new Vector3(1.12f, 1.2f, -0.34f), Vector3.zero, new Vector3(0.08f, 2.08f, 0.08f), trimMat);
        CreatePrim("DoorDivider", PrimitiveType.Cube, freezer.transform,
            new Vector3(0f, 1.2f, -0.34f), Vector3.zero, new Vector3(0.05f, 2.08f, 0.08f), trimMat);

        CreatePrim("GlassLeft", PrimitiveType.Cube, freezer.transform,
            new Vector3(-0.56f, 1.2f, -0.36f), Vector3.zero, new Vector3(1.03f, 2.02f, 0.02f), glassMat);
        CreatePrim("GlassRight", PrimitiveType.Cube, freezer.transform,
            new Vector3(0.56f, 1.2f, -0.36f), Vector3.zero, new Vector3(1.03f, 2.02f, 0.02f), glassMat);

        for (int col = 0; col < 2; col++)
        {
            float handleX = col == 0 ? -0.18f : 0.18f;
            CreatePrim($"Handle_{col}", PrimitiveType.Cube, freezer.transform,
                new Vector3(handleX, 1.2f, -0.39f), Vector3.zero, new Vector3(0.05f, 0.72f, 0.04f), metalMat);
        }

        System.Random rng = new System.Random(300 + index);
        for (int shelf = 0; shelf < 4; shelf++)
        {
            float y = 0.5f + shelf * 0.48f;
            float cursor = -0.84f;
            while (cursor < 0.84f)
            {
                ProductDefinition def = defs[rng.Next(defs.Length)];
                Vector3 size = new Vector3(
                    def.Size.x * (0.72f + (float)rng.NextDouble() * 0.14f),
                    def.Size.y * (0.74f + (float)rng.NextDouble() * 0.14f),
                    def.Size.z * (0.7f + (float)rng.NextDouble() * 0.12f));

                CreateShelfProduct(freezer.transform, $"FrozenProd_{shelf}_{Mathf.RoundToInt((cursor + 1f) * 100f)}_{def.Name}",
                    def, new Vector3(cursor, y, 0.12f + (float)rng.NextDouble() * 0.05f), 180f, 1, size);

                if (rng.NextDouble() > 0.55)
                {
                    CreateShelfProduct(freezer.transform, $"FrozenBack_{shelf}_{Mathf.RoundToInt((cursor + 1f) * 100f)}_{def.Name}",
                        def, new Vector3(cursor + 0.01f, y, 0.28f + (float)rng.NextDouble() * 0.05f), 180f, 1, size * 0.95f);
                }

                cursor += size.z + 0.08f;
            }
        }

        for (int side = -1; side <= 1; side += 2)
        {
            GameObject fzLight = new GameObject(side < 0 ? "InteriorLightLeft" : "InteriorLightRight");
            fzLight.transform.parent = freezer.transform;
            fzLight.transform.localPosition = new Vector3(0.95f * side, 1.25f, -0.08f);
            Light fl = fzLight.AddComponent<Light>();
            fl.type = LightType.Point;
            fl.color = new Color(0.82f, 0.92f, 1f);
            fl.intensity = 8f;
            fl.range = 3f;
            fl.shadows = LightShadows.None;
        }
    }

    static void CreateDoubleSidedSign(string name, Transform parent, Vector3 pos, Vector2 size, Material faceMat, Material frameMat)
    {
        GameObject sign = new GameObject(name);
        sign.transform.parent = parent;
        sign.transform.localPosition = pos;

        CreatePrim("Frame", PrimitiveType.Cube, sign.transform,
            Vector3.zero, Vector3.zero, new Vector3(size.x + 0.08f, size.y + 0.08f, 0.05f), frameMat);
        CreatePrim("Front", PrimitiveType.Quad, sign.transform,
            new Vector3(0f, 0f, 0.028f), new Vector3(0f, 180f, 180f), new Vector3(size.x, size.y, 1f), faceMat);
        CreatePrim("Back", PrimitiveType.Quad, sign.transform,
            new Vector3(0f, 0f, -0.028f), new Vector3(0f, 0f, 180f), new Vector3(size.x, size.y, 1f), faceMat);
        CreatePrim("RodL", PrimitiveType.Cube, sign.transform,
            new Vector3(-size.x * 0.32f, 0.32f, 0f), Vector3.zero, new Vector3(0.03f, 0.42f, 0.03f), frameMat);
        CreatePrim("RodR", PrimitiveType.Cube, sign.transform,
            new Vector3(size.x * 0.32f, 0.32f, 0f), Vector3.zero, new Vector3(0.03f, 0.42f, 0.03f), frameMat);
    }

    static void CreateShelfProduct(Transform parent, string name, ProductDefinition def,
        Vector3 basePos, float yaw, int side, Vector3 size)
    {
        GameObject root = new GameObject(name);
        root.transform.parent = parent;
        root.transform.localPosition = basePos;
        root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        switch (def.Shape)
        {
            case ProductShape.Carton:
                BuildCarton(root.transform, def, size);
                break;
            case ProductShape.TallBox:
                BuildTallBox(root.transform, def, size);
                break;
            case ProductShape.FlatPack:
                BuildFlatPack(root.transform, def, size);
                break;
            case ProductShape.HangingPack:
                BuildHangingPack(root.transform, def, size);
                break;
            case ProductShape.CounterCard:
                BuildCounterCard(root.transform, def, size, side);
                break;
            case ProductShape.Can:
                BuildCan(root.transform, def, size);
                break;
        }
    }

    static void BuildCarton(Transform parent, ProductDefinition def, Vector3 size)
    {
        CreatePrim("Body", PrimitiveType.Cube, parent,
            new Vector3(0f, size.y * 0.5f, 0f), Vector3.zero, size, def.BodyMat);
        CreatePrim("Band", PrimitiveType.Cube, parent,
            new Vector3(0f, size.y * 0.72f, 0f), Vector3.zero,
            new Vector3(size.x * 1.02f, size.y * 0.16f, size.z * 1.02f), def.AccentMat ?? def.BodyMat);
        AddFrontLabel(parent, def.LabelMat, size, 0.8f, 0.72f);
        AddBackLabel(parent, def.LabelMat, size, 0.62f, 0.48f);
    }

    static void BuildTallBox(Transform parent, ProductDefinition def, Vector3 size)
    {
        CreatePrim("Body", PrimitiveType.Cube, parent,
            new Vector3(0f, size.y * 0.5f, 0f), Vector3.zero, size, def.BodyMat);
        CreatePrim("Cap", PrimitiveType.Cube, parent,
            new Vector3(0f, size.y * 0.92f, 0f), Vector3.zero,
            new Vector3(size.x * 1.05f, size.y * 0.08f, size.z * 0.98f), def.AccentMat ?? def.BodyMat);
        AddFrontLabel(parent, def.LabelMat, size, 0.84f, 0.78f);
        AddBackLabel(parent, def.LabelMat, size, 0.6f, 0.42f);
    }

    static void BuildFlatPack(Transform parent, ProductDefinition def, Vector3 size)
    {
        CreatePrim("Body", PrimitiveType.Cube, parent,
            new Vector3(0f, size.y * 0.5f, 0f), Vector3.zero, size, def.BodyMat);
        CreatePrim("Trim", PrimitiveType.Cube, parent,
            new Vector3(0f, size.y * 0.12f, 0f), Vector3.zero,
            new Vector3(size.x * 1.05f, size.y * 0.12f, size.z * 1.02f), def.AccentMat ?? def.BodyMat);
        AddFrontLabel(parent, def.LabelMat, size, 0.92f, 0.84f);
    }

    static void BuildHangingPack(Transform parent, ProductDefinition def, Vector3 size)
    {
        CreatePrim("Body", PrimitiveType.Cube, parent,
            new Vector3(0f, size.y * 0.42f, 0f), Vector3.zero,
            new Vector3(size.x, size.y * 0.82f, size.z), def.BodyMat);
        CreatePrim("Header", PrimitiveType.Cube, parent,
            new Vector3(0f, size.y * 0.86f, 0f), Vector3.zero,
            new Vector3(size.x * 1.02f, size.y * 0.18f, size.z * 1.02f), def.AccentMat ?? def.BodyMat);
        AddFrontLabel(parent, def.LabelMat, size, 0.9f, 0.88f);
    }

    static void BuildCounterCard(Transform parent, ProductDefinition def, Vector3 size, int side)
    {
        CreatePrim("Base", PrimitiveType.Cube, parent,
            new Vector3(0f, size.y * 0.08f, 0f), Vector3.zero,
            new Vector3(size.x * 2.5f, size.y * 0.16f, size.z * 0.42f), def.BodyMat);
        CreatePrim("Card", PrimitiveType.Cube, parent,
            new Vector3(0f, size.y * 0.58f, 0f), new Vector3(side * -12f, 0f, 0f), size, def.BodyMat);
        AddFrontLabel(parent, def.LabelMat, size, 0.96f, 0.94f, size.y * 0.58f, side * 0.01f);
    }

    static void BuildCan(Transform parent, ProductDefinition def, Vector3 size)
    {
        GameObject body = CreatePrim("Body", PrimitiveType.Cylinder, parent,
            new Vector3(0f, size.y * 0.5f, 0f), Vector3.zero,
            new Vector3(size.z * 0.36f, size.y * 0.5f, size.z * 0.36f), def.BodyMat);
        Renderer bodyRenderer = body.GetComponent<Renderer>();
        if (bodyRenderer != null)
            bodyRenderer.sharedMaterial = def.BodyMat;
        AddFrontLabel(parent, def.LabelMat, new Vector3(size.x, size.y * 0.72f, size.z * 0.86f), 1f, 1f);
    }

    static void AddFrontLabel(Transform parent, Material labelMat, Vector3 size, float heightScale, float widthScale,
        float centerY = -1f, float xNudge = 0f)
    {
        float y = centerY >= 0f ? centerY : size.y * 0.5f;
        CreatePrim("LabelFront", PrimitiveType.Quad, parent,
            new Vector3(size.x * 0.5f + 0.004f + xNudge, y, 0f), new Vector3(0f, 90f, 0f),
            new Vector3(size.z * widthScale, size.y * heightScale, 1f), labelMat);
    }

    static void AddBackLabel(Transform parent, Material labelMat, Vector3 size, float heightScale, float widthScale)
    {
        CreatePrim("LabelBack", PrimitiveType.Quad, parent,
            new Vector3(-size.x * 0.5f - 0.004f, size.y * 0.5f, 0f), new Vector3(0f, -90f, 0f),
            new Vector3(size.z * widthScale, size.y * heightScale, 1f), labelMat);
    }

    // ============================
    // WAYPOINT HELPER
    // ============================
    static void CreateWaypoint(Transform parent, string name, Vector3 pos, Vector3 rot)
    {
        GameObject wp = new GameObject(name);
        wp.transform.parent = parent;
        wp.transform.localPosition = pos;
        wp.transform.localRotation = Quaternion.Euler(rot);
    }

    // ============================
    // CAMERA HELPER
    // ============================
    static Camera SetupCamera(Transform root)
    {
        Camera c = null;
        GameObject cam = GameObject.Find("Main Camera");
        if (cam != null)
        {
            c = cam.GetComponent<Camera>();
        }
        else
        {
            cam = new GameObject("Main Camera");
            c = cam.AddComponent<Camera>();
            cam.tag = "MainCamera";
        }
        cam.transform.parent = root;
        c.nearClipPlane = 0.05f;
        c.farClipPlane = 60f;
        return c;
    }

    // ============================
    // MATERIAL HELPERS
    // ============================
    static Shader FindShader()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        return s;
    }

    static Material CreateMatte(string name, Color color)
    {
        Material mat = new Material(FindShader());
        mat.name = name;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else
            mat.color = color;
        mat.SetFloat("_Smoothness", 0f);
        mat.SetFloat("_Metallic", 0f);
        return mat;
    }

    static Material CreateEmissive(string name, Color baseColor, Color emitColor, float intensity)
    {
        Material mat = new Material(FindShader());
        mat.name = name;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", baseColor);
        else
            mat.color = baseColor;
        mat.SetFloat("_Smoothness", 0f);
        mat.SetFloat("_Metallic", 0f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emitColor * intensity);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return mat;
    }

    static Material CreateTransparent(string name, Color color, float smoothness, float metallic)
    {
        Material mat = new Material(FindShader());
        mat.name = name;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else
            mat.color = color;

        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_ZWrite"))
            mat.SetFloat("_ZWrite", 0f);
        if (mat.HasProperty("_Cull"))
            mat.SetFloat("_Cull", 0f);

        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", metallic);
        return mat;
    }

    static Material CreateUnlitTextured(string name, string texturePath, Color tint, Vector2 tiling)
    {
        Shader shader = Shader.Find("Unlit/Texture");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            return CreateTextured(name, texturePath, tint, tiling);

        Material mat = new Material(shader);
        mat.name = name;
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (tex != null)
        {
            if (mat.HasProperty("_MainTex"))
            {
                mat.SetTexture("_MainTex", tex);
                mat.SetTextureScale("_MainTex", tiling);
            }
            else if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTextureScale("_BaseMap", tiling);
            }
        }

        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", tint);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", tint);
        return mat;
    }

    static Material CreateTextured(string name, string texturePath, Color tint, Vector2 tiling)
    {
        Material mat = new Material(FindShader());
        mat.name = name;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", tint);
        else
            mat.color = tint;

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        ApplyTexture(mat, tex, tiling);

        mat.SetFloat("_Smoothness", 0f);
        mat.SetFloat("_Metallic", 0f);
        return mat;
    }

    static Material CreateEmissiveTextured(string name, string texturePath, Color tint, Color emitColor, float intensity, Vector2 tiling)
    {
        Material mat = CreateTextured(name, texturePath, tint, tiling);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emitColor * intensity);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return mat;
    }

    static void ApplyTexture(Material mat, Texture2D tex, Vector2 tiling)
    {
        if (tex == null)
            return;

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

    static GameObject CreatePrim(string name, PrimitiveType type, Transform parent,
        Vector3 pos, Vector3 rot, Vector3 scale, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.parent = parent;
        obj.transform.localPosition = pos;
        obj.transform.localRotation = Quaternion.Euler(rot);
        obj.transform.localScale = scale;

        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null && mat != null) rend.sharedMaterial = mat;

        Collider col = obj.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);

        return obj;
    }
}
