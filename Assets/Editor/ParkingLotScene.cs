using UnityEngine;
using UnityEditor;

public class ParkingLotSceneGenerator : Editor
{
    [MenuItem("OPEN FEED/Scripts/Parking Lot - Clear")]
    static void ClearParkingLotScene()
    {
        GameObject existing = GameObject.Find("ParkingLotScene");
        if (existing != null)
        {
            DestroyImmediate(existing);
            Debug.Log("OPENFEED Parking Lot Scene cleared.");
        }
        else
        {
            Debug.Log("No Parking Lot Scene found to clear.");
        }
    }

    [MenuItem("OPEN FEED/Scripts/Parking Lot")]
    static void GenerateParkingLotScene()
    {
        // Clean up existing
        GameObject existing = GameObject.Find("ParkingLotScene");
        if (existing != null) DestroyImmediate(existing);

        GameObject root = new GameObject("ParkingLotScene");

        // ============================
        // MATERIALS
        // ============================
        Material matAsphalt = CreateMatte("Asphalt", new Color(0.06f, 0.06f, 0.07f));
        Material matAsphaltLine = CreateMatte("ParkingLine", new Color(0.35f, 0.35f, 0.3f));
        Material matSidewalk = CreateMatte("Sidewalk", new Color(0.12f, 0.12f, 0.11f));
        Material matCurb = CreateMatte("Curb", new Color(0.15f, 0.14f, 0.13f));
        Material matGrass = CreateMatte("Grass", new Color(0.03f, 0.06f, 0.03f));
        Material matBollard = CreateMatte("Bollard", new Color(0.4f, 0.35f, 0.1f));
        Material matBollardPost = CreateMatte("BollardPost", new Color(0.12f, 0.12f, 0.12f));
        Material matDumpster = CreateMatte("Dumpster", new Color(0.08f, 0.12f, 0.06f));
        Material matDumpsterLid = CreateMatte("DumpsterLid", new Color(0.06f, 0.09f, 0.05f));
        Material matTrashBag = CreateMatte("TrashBag", new Color(0.04f, 0.04f, 0.04f));
        Material matBench = CreateMatte("Bench", new Color(0.1f, 0.07f, 0.05f));
        Material matMetal = CreateMatte("Metal", new Color(0.1f, 0.1f, 0.1f));
        Material matSignPost = CreateMatte("SignPost", new Color(0.15f, 0.15f, 0.15f));
        Material matSign = CreateMatte("Sign", new Color(0.08f, 0.08f, 0.25f));
        Material matSignText = CreateEmissive("SignText", new Color(0.7f, 0.1f, 0.1f), new Color(0.8f, 0.15f, 0.1f), 0.4f);
        Material matPuddle = CreateMatte("Puddle", new Color(0.03f, 0.03f, 0.05f));
        Material matNeonGlow = CreateEmissive("NeonGlow", new Color(0.9f, 0.3f, 0.2f), new Color(0.9f, 0.3f, 0.2f), 1.2f);
        Material matWindowGlow = CreateEmissive("WindowGlow", new Color(0.7f, 0.65f, 0.5f), new Color(0.7f, 0.65f, 0.5f), 0.8f);
        Material matSky = CreateMatte("Sky", new Color(0.01f, 0.01f, 0.03f));
        Material matCone = CreateMatte("TrafficCone", new Color(0.6f, 0.25f, 0.05f));
        Material matConeStripe = CreateMatte("ConeStripe", new Color(0.7f, 0.7f, 0.7f));
        Material matShoppingCart = CreateMatte("ShoppingCart", new Color(0.15f, 0.15f, 0.16f));
        Material matRoad = CreateMatte("Road", new Color(0.05f, 0.05f, 0.06f));
        Material matRoadLine = CreateMatte("RoadLine", new Color(0.4f, 0.4f, 0.15f));

        // ============================
        // GROUND & PARKING LOT
        // ============================
        GameObject ground = new GameObject("Ground");
        ground.transform.parent = root.transform;

        // Main asphalt parking area
        CreatePrimitive("ParkingLot", PrimitiveType.Cube, ground.transform,
            new Vector3(0, -0.05f, 0), Vector3.zero, new Vector3(25f, 0.1f, 20f), matAsphalt);

        // Grass areas (surrounding)
        CreatePrimitive("GrassLeft", PrimitiveType.Cube, ground.transform,
            new Vector3(-15f, -0.06f, 0), Vector3.zero, new Vector3(8f, 0.1f, 25f), matGrass);
        CreatePrimitive("GrassRight", PrimitiveType.Cube, ground.transform,
            new Vector3(15f, -0.06f, 0), Vector3.zero, new Vector3(8f, 0.1f, 25f), matGrass);
        CreatePrimitive("GrassBack", PrimitiveType.Cube, ground.transform,
            new Vector3(0, -0.06f, -13f), Vector3.zero, new Vector3(30f, 0.1f, 8f), matGrass);
        CreatePrimitive("GrassFar", PrimitiveType.Cube, ground.transform,
            new Vector3(0, -0.06f, 15f), Vector3.zero, new Vector3(30f, 0.1f, 12f), matGrass);

        // Road in front
        CreatePrimitive("Road", PrimitiveType.Cube, ground.transform,
            new Vector3(0, -0.04f, 13f), Vector3.zero, new Vector3(30f, 0.1f, 5f), matRoad);

        // Road center line
        for (int i = 0; i < 8; i++)
        {
            CreatePrimitive($"RoadDash{i}", PrimitiveType.Cube, ground.transform,
                new Vector3(-14f + i * 4f, 0.01f, 13f), Vector3.zero, new Vector3(2f, 0.005f, 0.1f), matRoadLine);
        }

        // Sidewalk in front of store
        CreatePrimitive("Sidewalk", PrimitiveType.Cube, ground.transform,
            new Vector3(0, 0.02f, 7f), Vector3.zero, new Vector3(14f, 0.08f, 2.5f), matSidewalk);

        // Curb edge
        CreatePrimitive("Curb", PrimitiveType.Cube, ground.transform,
            new Vector3(0, 0.06f, 5.7f), Vector3.zero, new Vector3(14f, 0.12f, 0.2f), matCurb);

        // Parking lines
        for (int i = 0; i < 7; i++)
        {
            float x = -7.5f + i * 2.5f;
            CreatePrimitive($"ParkingLine{i}", PrimitiveType.Cube, ground.transform,
                new Vector3(x, 0.01f, 2.5f), Vector3.zero, new Vector3(0.08f, 0.005f, 4f), matAsphaltLine);
        }

        // Horizontal parking line (front)
        CreatePrimitive("ParkingLineFront", PrimitiveType.Cube, ground.transform,
            new Vector3(-1.25f, 0.01f, 4.5f), Vector3.zero, new Vector3(15f, 0.005f, 0.08f), matAsphaltLine);

        // Handicap symbol parking (first spot, just a simple rectangle)
        CreatePrimitive("HandicapSpot", PrimitiveType.Cube, ground.transform,
            new Vector3(-6.25f, 0.012f, 2.5f), Vector3.zero, new Vector3(0.8f, 0.004f, 0.8f), matSign);

        // Puddle on asphalt (reflective feel)
        CreatePrimitive("Puddle1", PrimitiveType.Cube, ground.transform,
            new Vector3(3f, 0.008f, 1.5f), new Vector3(0, 12, 0), new Vector3(1.8f, 0.003f, 0.9f), matPuddle);
        CreatePrimitive("Puddle2", PrimitiveType.Cube, ground.transform,
            new Vector3(-4f, 0.008f, 3.5f), new Vector3(0, -5, 0), new Vector3(1.2f, 0.003f, 0.6f), matPuddle);

        // ============================
        // CONVENIENCE STORE (6twelve model)
        // ============================
        GameObject store = new GameObject("ConvenienceStore");
        store.transform.parent = root.transform;

        // Load store model
        GameObject storePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ModelsPlace/6twelve/Models/6twelve.fbx");
        if (storePrefab != null)
        {
            GameObject storeModel = (GameObject)PrefabUtility.InstantiatePrefab(storePrefab);
            storeModel.name = "6twelveModel";
            storeModel.transform.parent = store.transform;
            storeModel.transform.localPosition = new Vector3(0, 0, 8.5f);
            storeModel.transform.localRotation = Quaternion.Euler(0, 180, 0);
            storeModel.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

            // Apply textures from folder
            ApplyStoreTextures(storeModel);
        }
        else
        {
            Debug.LogWarning("6twelve.fbx not found! Creating placeholder store.");
            CreatePlaceholderStore(store.transform, matSidewalk, matWindowGlow, matMetal);
        }

        // Store interior glow (warm light leaking from windows)
        GameObject storeGlow = new GameObject("StoreInteriorGlow");
        storeGlow.transform.parent = store.transform;
        storeGlow.transform.localPosition = new Vector3(0, 2f, 7.5f);
        Light storeLight = storeGlow.AddComponent<Light>();
        storeLight.type = LightType.Point;
        storeLight.color = new Color(0.75f, 0.7f, 0.55f);
        storeLight.intensity = 1.5f;
        storeLight.range = 8f;
        storeLight.shadows = LightShadows.Soft;

        // Secondary store glow (doorway)
        GameObject doorGlow = new GameObject("DoorGlow");
        doorGlow.transform.parent = store.transform;
        doorGlow.transform.localPosition = new Vector3(0, 1.2f, 6.5f);
        Light doorLight = doorGlow.AddComponent<Light>();
        doorLight.type = LightType.Spot;
        doorLight.color = new Color(0.8f, 0.75f, 0.6f);
        doorLight.intensity = 2f;
        doorLight.range = 6f;
        doorLight.spotAngle = 80f;
        doorLight.transform.localRotation = Quaternion.Euler(40, 0, 0);
        doorLight.shadows = LightShadows.Soft;

        // Neon "OPEN" sign glow (on front of store)
        CreatePrimitive("NeonOpenSign", PrimitiveType.Cube, store.transform,
            new Vector3(2.5f, 2.8f, 7.2f), Vector3.zero, new Vector3(1.2f, 0.35f, 0.05f), matNeonGlow);

        GameObject neonLight = new GameObject("NeonSignLight");
        neonLight.transform.parent = store.transform;
        neonLight.transform.localPosition = new Vector3(2.5f, 2.8f, 6.8f);
        Light nl = neonLight.AddComponent<Light>();
        nl.type = LightType.Point;
        nl.color = new Color(0.9f, 0.3f, 0.15f);
        nl.intensity = 0.6f;
        nl.range = 3f;
        nl.shadows = LightShadows.None;

        // ============================
        // CAR (parked in lot)
        // ============================
        GameObject carParent = new GameObject("ParkedCar");
        carParent.transform.parent = root.transform;

        GameObject carPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ModelsPlace/psx-low-poly-car-renault/source/Car.fbx");
        if (carPrefab != null)
        {
            GameObject car = (GameObject)PrefabUtility.InstantiatePrefab(carPrefab);
            car.name = "Car";
            car.transform.parent = carParent.transform;
            car.transform.localPosition = new Vector3(-1.5f, 0.38f, 2f);
            car.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            car.transform.localScale = new Vector3(75.1f, 75.1f, 75.1f);

            // Apply car textures
            ApplyCarTextures(car);
        }
        else
        {
            Debug.LogWarning("Car.fbx not found! Creating placeholder car.");
            CreatePrimitive("PlaceholderCar", PrimitiveType.Cube, carParent.transform,
                new Vector3(-1.5f, 0.6f, 2f), Vector3.zero, new Vector3(1.8f, 1.2f, 4f), matMetal);
        }

        // Car headlights (off but faint reflection)
        // Tail lights (faint red glow - car is parked facing store)
        GameObject tailLightL = new GameObject("TailLightLeft");
        tailLightL.transform.parent = carParent.transform;
        tailLightL.transform.localPosition = new Vector3(-2.2f, 0.7f, -0.1f);
        Light tll = tailLightL.AddComponent<Light>();
        tll.type = LightType.Point;
        tll.color = new Color(0.6f, 0.05f, 0.02f);
        tll.intensity = 0.15f;
        tll.range = 1.2f;
        tll.shadows = LightShadows.None;

        GameObject tailLightR = new GameObject("TailLightRight");
        tailLightR.transform.parent = carParent.transform;
        tailLightR.transform.localPosition = new Vector3(-0.8f, 0.7f, -0.1f);
        Light tlr = tailLightR.AddComponent<Light>();
        tlr.type = LightType.Point;
        tlr.color = new Color(0.6f, 0.05f, 0.02f);
        tlr.intensity = 0.15f;
        tlr.range = 1.2f;
        tlr.shadows = LightShadows.None;

        // ============================
        // STREET LAMPS
        // ============================
        GameObject lamps = new GameObject("StreetLamps");
        lamps.transform.parent = root.transform;

        GameObject lampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/ModelsPlace/low-poly-psx-street-lamp/source/source/lamp(lighton).fbx");

        Vector3[] lampPositions = new Vector3[]
        {
            new Vector3(-6f, 0, 5.5f),
            new Vector3(6f, 0, 5.5f),
            new Vector3(-6f, 0, -2f),
            new Vector3(6f, 0, -2f),
        };

        Color lampColor = new Color(0.9f, 0.7f, 0.35f);

        for (int i = 0; i < lampPositions.Length; i++)
        {
            GameObject lampParent = new GameObject($"StreetLamp{i}");
            lampParent.transform.parent = lamps.transform;
            lampParent.transform.localPosition = lampPositions[i];

            if (lampPrefab != null)
            {
                GameObject lampModel = (GameObject)PrefabUtility.InstantiatePrefab(lampPrefab);
                lampModel.name = $"LampModel{i}";
                lampModel.transform.parent = lampParent.transform;
                lampModel.transform.localPosition = Vector3.zero;
                lampModel.transform.localScale = Vector3.one;

                SetAllMaterialsMatte(lampModel);
            }
            else
            {
                // Fallback procedural lamp
                CreatePrimitive($"LampPole{i}", PrimitiveType.Cylinder, lampParent.transform,
                    new Vector3(0, 2.5f, 0), Vector3.zero, new Vector3(0.08f, 2.5f, 0.08f), matSignPost);
                CreatePrimitive($"LampHead{i}", PrimitiveType.Cube, lampParent.transform,
                    new Vector3(0, 5.2f, 0), Vector3.zero, new Vector3(0.5f, 0.15f, 0.3f), matMetal);
            }

            // Lamp light
            GameObject lampLight = new GameObject($"LampLight{i}");
            lampLight.transform.parent = lampParent.transform;
            lampLight.transform.localPosition = new Vector3(0, 5f, 0);
            Light ll = lampLight.AddComponent<Light>();
            ll.type = LightType.Point;
            ll.color = lampColor;
            ll.intensity = i < 2 ? 1.8f : 1.0f; // Front lamps brighter
            ll.range = 12f;
            ll.shadows = LightShadows.Soft;

            // Downward cone light for pool of light effect
            GameObject lampSpot = new GameObject($"LampSpot{i}");
            lampSpot.transform.parent = lampParent.transform;
            lampSpot.transform.localPosition = new Vector3(0, 4.8f, 0);
            lampSpot.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Light ls = lampSpot.AddComponent<Light>();
            ls.type = LightType.Spot;
            ls.color = lampColor;
            ls.intensity = i < 2 ? 2.5f : 1.5f;
            ls.range = 10f;
            ls.spotAngle = 70f;
            ls.shadows = LightShadows.Soft;
        }

        // ============================
        // PINE TREES (background)
        // ============================
        GameObject trees = new GameObject("Trees");
        trees.transform.parent = root.transform;

        GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/ModelsPlace/pine-tree-ps1-low-poly/source/pine_tree.fbx");
        Texture2D treeTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/ModelsPlace/pine-tree-ps1-low-poly/textures/pinetree.png");

        // Back tree line
        for (int i = 0; i < 20; i++)
        {
            float x = -12f + i * 1.3f + Random.Range(-0.3f, 0.3f);
            float z = -10f + Random.Range(-1.5f, 0.5f);
            float scale = Random.Range(0.8f, 1.3f);
            float rotY = Random.Range(0, 360f);

            SpawnTree(treePrefab, treeTexture, trees.transform, $"BackTree{i}",
                new Vector3(x, 0, z), rotY, scale);
        }

        // Side trees left
        for (int i = 0; i < 8; i++)
        {
            float z = -8f + i * 2.2f + Random.Range(-0.5f, 0.5f);
            float x = -12f + Random.Range(-1f, 0.5f);
            float scale = Random.Range(0.9f, 1.2f);

            SpawnTree(treePrefab, treeTexture, trees.transform, $"LeftTree{i}",
                new Vector3(x, 0, z), Random.Range(0, 360f), scale);
        }

        // Side trees right
        for (int i = 0; i < 8; i++)
        {
            float z = -8f + i * 2.2f + Random.Range(-0.5f, 0.5f);
            float x = 12f + Random.Range(-0.5f, 1f);
            float scale = Random.Range(0.9f, 1.2f);

            SpawnTree(treePrefab, treeTexture, trees.transform, $"RightTree{i}",
                new Vector3(x, 0, z), Random.Range(0, 360f), scale);
        }

        // ============================
        // PARKING LOT DETAILS
        // ============================
        GameObject details = new GameObject("Details");
        details.transform.parent = root.transform;

        // Bollards in front of store (yellow posts)
        for (int i = 0; i < 5; i++)
        {
            float x = -4f + i * 2f;
            GameObject bollard = new GameObject($"Bollard{i}");
            bollard.transform.parent = details.transform;
            bollard.transform.localPosition = new Vector3(x, 0, 5.5f);

            CreatePrimitive($"BollardPost{i}", PrimitiveType.Cylinder, bollard.transform,
                new Vector3(0, 0.4f, 0), Vector3.zero, new Vector3(0.08f, 0.4f, 0.08f), matBollardPost);
            CreatePrimitive($"BollardTop{i}", PrimitiveType.Cylinder, bollard.transform,
                new Vector3(0, 0.8f, 0), Vector3.zero, new Vector3(0.1f, 0.04f, 0.1f), matBollard);
        }

        // Dumpster (side of store)
        GameObject dumpster = new GameObject("Dumpster");
        dumpster.transform.parent = details.transform;
        dumpster.transform.localPosition = new Vector3(8f, 0, 8f);
        dumpster.transform.localRotation = Quaternion.Euler(0, -10, 0);

        CreatePrimitive("DumpsterBody", PrimitiveType.Cube, dumpster.transform,
            new Vector3(0, 0.6f, 0), Vector3.zero, new Vector3(1.8f, 1.2f, 1.2f), matDumpster);
        CreatePrimitive("DumpsterLid", PrimitiveType.Cube, dumpster.transform,
            new Vector3(0, 1.22f, 0.1f), new Vector3(-8, 0, 0), new Vector3(1.82f, 0.06f, 1.3f), matDumpsterLid);

        // Trash bags near dumpster
        CreatePrimitive("TrashBag1", PrimitiveType.Sphere, dumpster.transform,
            new Vector3(1.2f, 0.3f, 0.2f), Vector3.zero, new Vector3(0.5f, 0.4f, 0.4f), matTrashBag);
        CreatePrimitive("TrashBag2", PrimitiveType.Sphere, dumpster.transform,
            new Vector3(1.3f, 0.25f, -0.4f), new Vector3(0, 20, 10), new Vector3(0.4f, 0.35f, 0.45f), matTrashBag);

        // Traffic cone (near parking spot)
        GameObject cone = new GameObject("TrafficCone");
        cone.transform.parent = details.transform;
        cone.transform.localPosition = new Vector3(4f, 0, 3f);

        CreatePrimitive("ConeBase", PrimitiveType.Cube, cone.transform,
            new Vector3(0, 0.02f, 0), Vector3.zero, new Vector3(0.3f, 0.03f, 0.3f), matMetal);
        CreatePrimitive("ConeBody", PrimitiveType.Cube, cone.transform,
            new Vector3(0, 0.2f, 0), Vector3.zero, new Vector3(0.15f, 0.35f, 0.15f), matCone);
        CreatePrimitive("ConeStripe", PrimitiveType.Cube, cone.transform,
            new Vector3(0, 0.28f, 0), Vector3.zero, new Vector3(0.12f, 0.06f, 0.16f), matConeStripe);

        // Abandoned shopping cart
        GameObject cart = new GameObject("ShoppingCart");
        cart.transform.parent = details.transform;
        cart.transform.localPosition = new Vector3(5.5f, 0, 0.5f);
        cart.transform.localRotation = Quaternion.Euler(0, 35, 0);

        CreatePrimitive("CartBasket", PrimitiveType.Cube, cart.transform,
            new Vector3(0, 0.55f, 0), Vector3.zero, new Vector3(0.5f, 0.35f, 0.7f), matShoppingCart);
        CreatePrimitive("CartHandle", PrimitiveType.Cube, cart.transform,
            new Vector3(0, 0.8f, -0.35f), new Vector3(-20, 0, 0), new Vector3(0.4f, 0.03f, 0.03f), matShoppingCart);
        // Cart legs
        CreatePrimitive("CartLegFL", PrimitiveType.Cube, cart.transform,
            new Vector3(-0.2f, 0.2f, 0.3f), Vector3.zero, new Vector3(0.02f, 0.4f, 0.02f), matShoppingCart);
        CreatePrimitive("CartLegFR", PrimitiveType.Cube, cart.transform,
            new Vector3(0.2f, 0.2f, 0.3f), Vector3.zero, new Vector3(0.02f, 0.4f, 0.02f), matShoppingCart);
        CreatePrimitive("CartLegBL", PrimitiveType.Cube, cart.transform,
            new Vector3(-0.2f, 0.2f, -0.3f), Vector3.zero, new Vector3(0.02f, 0.4f, 0.02f), matShoppingCart);
        CreatePrimitive("CartLegBR", PrimitiveType.Cube, cart.transform,
            new Vector3(0.2f, 0.2f, -0.3f), Vector3.zero, new Vector3(0.02f, 0.4f, 0.02f), matShoppingCart);
        // Wheels
        for (int w = 0; w < 4; w++)
        {
            float wx = w < 2 ? -0.2f : 0.2f;
            float wz = w % 2 == 0 ? 0.3f : -0.3f;
            CreatePrimitive($"CartWheel{w}", PrimitiveType.Sphere, cart.transform,
                new Vector3(wx, 0.04f, wz), Vector3.zero, new Vector3(0.06f, 0.06f, 0.06f), matMetal);
        }

        // Bench near entrance
        GameObject bench = new GameObject("Bench");
        bench.transform.parent = details.transform;
        bench.transform.localPosition = new Vector3(-5f, 0, 6.2f);

        CreatePrimitive("BenchSeat", PrimitiveType.Cube, bench.transform,
            new Vector3(0, 0.4f, 0), Vector3.zero, new Vector3(1.2f, 0.05f, 0.35f), matBench);
        CreatePrimitive("BenchBack", PrimitiveType.Cube, bench.transform,
            new Vector3(0, 0.65f, -0.15f), new Vector3(-8, 0, 0), new Vector3(1.2f, 0.4f, 0.04f), matBench);
        CreatePrimitive("BenchLegL", PrimitiveType.Cube, bench.transform,
            new Vector3(-0.5f, 0.2f, 0), Vector3.zero, new Vector3(0.04f, 0.4f, 0.3f), matMetal);
        CreatePrimitive("BenchLegR", PrimitiveType.Cube, bench.transform,
            new Vector3(0.5f, 0.2f, 0), Vector3.zero, new Vector3(0.04f, 0.4f, 0.3f), matMetal);

        // Sign post (parking sign)
        GameObject signPost = new GameObject("ParkingSign");
        signPost.transform.parent = details.transform;
        signPost.transform.localPosition = new Vector3(-8f, 0, 4f);

        CreatePrimitive("SignPole", PrimitiveType.Cylinder, signPost.transform,
            new Vector3(0, 1.5f, 0), Vector3.zero, new Vector3(0.05f, 1.5f, 0.05f), matSignPost);
        CreatePrimitive("SignPlate", PrimitiveType.Cube, signPost.transform,
            new Vector3(0, 3.2f, 0), Vector3.zero, new Vector3(0.6f, 0.4f, 0.03f), matSign);

        // ============================
        // CAMERA
        // ============================
        GameObject cam = new GameObject("MainCamera");
        cam.transform.parent = root.transform;
        // Positioned as if from a security camera angle, slightly elevated
        cam.transform.localPosition = new Vector3(3f, 3.5f, -5f);
        cam.transform.localRotation = Quaternion.Euler(18f, -8f, 0f);
        Camera c = cam.AddComponent<Camera>();
        c.fieldOfView = 55f;
        c.nearClipPlane = 0.05f;
        c.farClipPlane = 80f;
        c.backgroundColor = new Color(0.005f, 0.005f, 0.015f);
        c.clearFlags = CameraClearFlags.SolidColor;

        // ============================
        // LIGHTING
        // ============================

        // Remove default directional light
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light l in allLights)
        {
            if (l.type == LightType.Directional && l.gameObject.name == "Directional Light")
                DestroyImmediate(l.gameObject);
        }

        // Moonlight (very faint)
        GameObject moonlight = new GameObject("Moonlight");
        moonlight.transform.parent = root.transform;
        moonlight.transform.localRotation = Quaternion.Euler(40, -60, 0);
        Light moon = moonlight.AddComponent<Light>();
        moon.type = LightType.Directional;
        moon.color = new Color(0.12f, 0.14f, 0.22f);
        moon.intensity = 0.04f;
        moon.shadows = LightShadows.Soft;

        // ============================
        // RENDER SETTINGS
        // ============================
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.01f, 0.012f, 0.018f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.025f;
        RenderSettings.fogColor = new Color(0.01f, 0.012f, 0.02f);

        // ============================
        // FINALIZE
        // ============================
        Selection.activeGameObject = root;
        Debug.Log("OPENFEED Parking Lot Scene generated.");
    }

    // ============================
    // TREE SPAWNER
    // ============================
    static void SpawnTree(GameObject prefab, Texture2D tex, Transform parent, string name,
        Vector3 pos, float rotY, float scale)
    {
        if (prefab != null)
        {
            GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            tree.name = name;
            tree.transform.parent = parent;
            tree.transform.localPosition = pos;
            tree.transform.localRotation = Quaternion.Euler(0, rotY, 0);
            tree.transform.localScale = Vector3.one * scale;

            if (tex != null)
            {
                foreach (Renderer r in tree.GetComponentsInChildren<Renderer>())
                {
                    Material m = new Material(FindShader());
                    m.SetFloat("_Smoothness", 0f);
                    m.SetFloat("_Metallic", 0f);
                    if (m.HasProperty("_BaseMap"))
                        m.SetTexture("_BaseMap", tex);
                    else
                        m.mainTexture = tex;
                    r.sharedMaterial = m;
                }
            }
        }
        else
        {
            // Fallback: simple cone + cylinder tree
            Material treeMat = CreateMatte($"{name}Mat", new Color(0.02f, 0.05f, 0.02f));
            Material trunkMat = CreateMatte($"{name}Trunk", new Color(0.08f, 0.05f, 0.03f));

            GameObject treeObj = new GameObject(name);
            treeObj.transform.parent = parent;
            treeObj.transform.localPosition = pos;
            treeObj.transform.localScale = Vector3.one * scale;

            CreatePrimitive($"{name}Trunk", PrimitiveType.Cylinder, treeObj.transform,
                new Vector3(0, 1.5f, 0), Vector3.zero, new Vector3(0.15f, 1.5f, 0.15f), trunkMat);
            CreatePrimitive($"{name}Canopy", PrimitiveType.Cube, treeObj.transform,
                new Vector3(0, 4f, 0), new Vector3(0, 45, 0), new Vector3(1.2f, 3f, 1.2f), treeMat);
        }
    }

    // ============================
    // PLACEHOLDER STORE
    // ============================
    static void CreatePlaceholderStore(Transform parent, Material wallMat, Material windowMat, Material roofMat)
    {
        CreatePrimitive("StoreWalls", PrimitiveType.Cube, parent,
            new Vector3(0, 1.8f, 8.5f), Vector3.zero, new Vector3(12f, 3.6f, 4f), wallMat);
        CreatePrimitive("StoreRoof", PrimitiveType.Cube, parent,
            new Vector3(0, 3.7f, 8.5f), Vector3.zero, new Vector3(12.5f, 0.15f, 4.5f), roofMat);
        CreatePrimitive("StoreWindow", PrimitiveType.Cube, parent,
            new Vector3(0, 1.8f, 6.45f), Vector3.zero, new Vector3(6f, 2f, 0.05f), windowMat);
    }

    // ============================
    // TEXTURE APPLICATION
    // ============================
    static void ApplyCarTextures(GameObject car)
    {
        Texture2D carTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ModelsPlace/psx-low-poly-car-renault/textures/Car_-_RegularWhite.png");
        Texture2D carEmit = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ModelsPlace/psx-low-poly-car-renault/textures/CarEmission.png");
        Texture2D tireTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ModelsPlace/psx-low-poly-car-renault/textures/Tire.png");
        Texture2D plateTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ModelsPlace/psx-low-poly-car-renault/textures/LicensePlate.png");

        foreach (Renderer r in car.GetComponentsInChildren<Renderer>())
        {
            Material mat = new Material(FindShader());
            mat.SetFloat("_Smoothness", 0f);
            mat.SetFloat("_Metallic", 0f);

            string objName = r.gameObject.name.ToLower();
            if (objName.Contains("tire") || objName.Contains("wheel"))
            {
                if (tireTex != null) ApplyTexture(mat, tireTex);
            }
            else if (objName.Contains("plate") || objName.Contains("license"))
            {
                if (plateTex != null) ApplyTexture(mat, plateTex);
            }
            else
            {
                if (carTex != null) ApplyTexture(mat, carTex);
                if (carEmit != null)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetTexture("_EmissionMap", carEmit);
                    mat.SetColor("_EmissionColor", Color.white * 0.3f);
                }
            }
            r.sharedMaterial = mat;
        }
    }

    static void ApplyStoreTextures(GameObject store)
    {
        string texturePath = "Assets/ModelsPlace/6twelve/Models/Textures/";
        foreach (Renderer r in store.GetComponentsInChildren<Renderer>())
        {
            Material mat = new Material(FindShader());
            mat.SetFloat("_Smoothness", 0f);
            mat.SetFloat("_Metallic", 0f);

            // Try to find matching texture by renderer/material name
            string matName = r.sharedMaterial != null ? r.sharedMaterial.name : r.gameObject.name;
            string[] guids = AssetDatabase.FindAssets(matName, new[] { texturePath });
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null) ApplyTexture(mat, tex);
            }
            r.sharedMaterial = mat;
        }
    }

    static void SetAllMaterialsMatte(GameObject obj)
    {
        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
        {
            foreach (Material m in r.sharedMaterials)
            {
                if (m != null)
                {
                    m.SetFloat("_Smoothness", 0f);
                    m.SetFloat("_Metallic", 0f);
                }
            }
        }
    }

    static void ApplyTexture(Material mat, Texture2D tex)
    {
        if (mat.HasProperty("_BaseMap"))
            mat.SetTexture("_BaseMap", tex);
        else
            mat.mainTexture = tex;
    }

    // ============================
    // HELPER METHODS
    // ============================
    static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent,
        Vector3 localPos, Vector3 localRot, Vector3 localScale, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.parent = parent;
        obj.transform.localPosition = localPos;
        obj.transform.localRotation = Quaternion.Euler(localRot);
        obj.transform.localScale = localScale;

        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null && mat != null)
            rend.sharedMaterial = mat;

        Collider col = obj.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);

        return obj;
    }

    static Shader FindShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) Debug.LogError("No valid shader found!");
        return shader;
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

    static Material CreateEmissive(string name, Color baseColor, Color emissionColor, float emissionIntensity)
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
        mat.SetColor("_EmissionColor", emissionColor * emissionIntensity);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return mat;
    }
}