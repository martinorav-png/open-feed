using UnityEngine;
using UnityEditor;

public class MainMenuSceneGenerator : Editor
{
    [MenuItem("OPEN FEED/Scripts/Main Menu - Clear")]
    static void ClearMainMenuScene()
    {
        GameObject existing = GameObject.Find("MainMenuScene");
        if (existing != null)
        {
            DestroyImmediate(existing);
            Debug.Log("OPENFEED Main Menu Scene cleared.");
        }
    }

    [MenuItem("OPEN FEED/Scripts/Main Menu")]
    static void GenerateMainMenuScene()
    {
        GameObject existing = GameObject.Find("MainMenuScene");
        if (existing != null) DestroyImmediate(existing);

        // Remove ALL existing directional lights
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light l in allLights)
        {
            if (l.type == LightType.Directional)
                DestroyImmediate(l.gameObject);
        }

        GameObject root = new GameObject("MainMenuScene");

        // ============================
        // MATERIALS
        // ============================
        Material matAsphalt = CreateMatte("Asphalt", new Color(0.025f, 0.025f, 0.03f));
        Material matAsphaltWet = CreateMatte("WetAsphalt", new Color(0.015f, 0.015f, 0.025f));
        Material matSidewalk = CreateMatte("Sidewalk", new Color(0.07f, 0.065f, 0.065f));
        Material matCurb = CreateMatte("Curb", new Color(0.09f, 0.085f, 0.08f));
        Material matBrick = CreateMatte("Brick", new Color(0.08f, 0.05f, 0.04f));
        Material matBrickDark = CreateMatte("BrickDark", new Color(0.05f, 0.035f, 0.03f));
        Material matRoof = CreateMatte("Roof", new Color(0.04f, 0.04f, 0.045f));
        Material matAwning = CreateMatte("Awning", new Color(0.12f, 0.03f, 0.03f));
        Material matMetal = CreateMatte("Metal", new Color(0.06f, 0.06f, 0.065f));
        Material matDoor = CreateMatte("Door", new Color(0.04f, 0.04f, 0.05f));
        Material matFence = CreateMatte("Fence", new Color(0.05f, 0.05f, 0.055f));
        Material matFencePost = CreateMatte("FencePost", new Color(0.04f, 0.04f, 0.04f));
        Material matCar = CreateMatte("CarBody", new Color(0.03f, 0.03f, 0.035f));
        Material matCarTrim = CreateMatte("CarTrim", new Color(0.02f, 0.02f, 0.02f));
        Material matTire = CreateMatte("Tire", new Color(0.015f, 0.015f, 0.015f));
        Material matBollard = CreateMatte("Bollard", new Color(0.2f, 0.18f, 0.05f));
        Material matBollardPost = CreateMatte("BollardPost", new Color(0.06f, 0.06f, 0.06f));
        Material matWindow = CreateEmissive("Window", new Color(0.15f, 0.12f, 0.08f),
            new Color(0.4f, 0.32f, 0.2f), 0.5f);
        Material matWindowDim = CreateEmissive("WindowDim", new Color(0.08f, 0.06f, 0.04f),
            new Color(0.2f, 0.15f, 0.08f), 0.3f);
        Material matHeadlightGlow = CreateEmissive("HeadlightGlow", new Color(0.7f, 0.65f, 0.5f),
            new Color(0.8f, 0.75f, 0.55f), 1.5f);
        Material matTaillight = CreateEmissive("Taillight", new Color(0.4f, 0.02f, 0.01f),
            new Color(0.5f, 0.05f, 0.02f), 0.6f);
        Material matGround = CreateMatte("FarGround", new Color(0.02f, 0.02f, 0.025f));

        // ============================
        // GROUND
        // ============================
        GameObject ground = new GameObject("Ground");
        ground.transform.parent = root.transform;

        // Large ground plane
        CreatePrim("GroundPlane", PrimitiveType.Cube, ground.transform,
            new Vector3(0, -0.1f, 0), Vector3.zero, new Vector3(50f, 0.2f, 50f), matGround);

        // Parking lot asphalt
        CreatePrim("ParkingLot", PrimitiveType.Cube, ground.transform,
            new Vector3(0, 0.01f, 0), Vector3.zero, new Vector3(22f, 0.02f, 18f), matAsphalt);

        // Wet patches
        CreatePrim("WetPatch1", PrimitiveType.Cube, ground.transform,
            new Vector3(-1f, 0.02f, -0.5f), new Vector3(0, 8, 0), new Vector3(5f, 0.005f, 3.5f), matAsphaltWet);
        CreatePrim("WetPatch2", PrimitiveType.Cube, ground.transform,
            new Vector3(3f, 0.02f, 1f), new Vector3(0, -5, 0), new Vector3(4f, 0.005f, 2.5f), matAsphaltWet);

        // Headlight reflections on ground (bright emissive strips)
        CreatePrim("Reflect1", PrimitiveType.Cube, ground.transform,
            new Vector3(0.5f, 0.025f, -1f), new Vector3(0, -60, 0), new Vector3(0.06f, 0.003f, 4f), matHeadlightGlow);
        CreatePrim("Reflect2", PrimitiveType.Cube, ground.transform,
            new Vector3(-0.3f, 0.025f, -1.5f), new Vector3(0, -60, 0), new Vector3(0.04f, 0.003f, 3.5f), matHeadlightGlow);
        CreatePrim("Reflect3", PrimitiveType.Cube, ground.transform,
            new Vector3(1.3f, 0.025f, -0.5f), new Vector3(0, -55, 0), new Vector3(0.03f, 0.003f, 2.5f), matHeadlightGlow);

        // Sidewalk
        CreatePrim("Sidewalk", PrimitiveType.Cube, ground.transform,
            new Vector3(0, 0.04f, 6.5f), Vector3.zero, new Vector3(16f, 0.1f, 2f), matSidewalk);
        CreatePrim("Curb", PrimitiveType.Cube, ground.transform,
            new Vector3(0, 0.08f, 5.4f), Vector3.zero, new Vector3(16f, 0.14f, 0.2f), matCurb);

        // ============================
        // STORE BUILDING
        // ============================
        GameObject store = new GameObject("Store");
        store.transform.parent = root.transform;

        // Main building body
        CreatePrim("WallFront", PrimitiveType.Cube, store.transform,
            new Vector3(0, 2f, 8.5f), Vector3.zero, new Vector3(14f, 4f, 0.3f), matBrick);
        CreatePrim("WallLeft", PrimitiveType.Cube, store.transform,
            new Vector3(-7f, 2f, 10f), Vector3.zero, new Vector3(0.3f, 4f, 3.3f), matBrickDark);
        CreatePrim("WallRight", PrimitiveType.Cube, store.transform,
            new Vector3(7f, 2f, 10f), Vector3.zero, new Vector3(0.3f, 4f, 3.3f), matBrickDark);
        CreatePrim("WallBack", PrimitiveType.Cube, store.transform,
            new Vector3(0, 2f, 11.5f), Vector3.zero, new Vector3(14f, 4f, 0.3f), matBrickDark);

        // Roof
        CreatePrim("Roof", PrimitiveType.Cube, store.transform,
            new Vector3(0, 4.1f, 10f), Vector3.zero, new Vector3(14.5f, 0.2f, 3.5f), matRoof);

        // Awning / overhang (red, like in image)
        CreatePrim("Awning", PrimitiveType.Cube, store.transform,
            new Vector3(0, 3.2f, 6.8f), new Vector3(5, 0, 0), new Vector3(13f, 0.08f, 2.5f), matAwning);
        CreatePrim("AwningFascia", PrimitiveType.Cube, store.transform,
            new Vector3(0, 3f, 5.6f), Vector3.zero, new Vector3(13f, 0.4f, 0.06f), matAwning);

        // Awning support brackets
        for (int i = 0; i < 4; i++)
        {
            float x = -4.5f + i * 3f;
            CreatePrim($"AwningBracket{i}", PrimitiveType.Cube, store.transform,
                new Vector3(x, 2.7f, 5.8f), new Vector3(0, 0, 15), new Vector3(0.04f, 1f, 0.04f), matMetal);
        }

        // Windows (emissive, warm glow)
        CreatePrim("WindowL", PrimitiveType.Cube, store.transform,
            new Vector3(-3f, 1.8f, 8.35f), Vector3.zero, new Vector3(3.5f, 2f, 0.05f), matWindow);
        CreatePrim("WindowR", PrimitiveType.Cube, store.transform,
            new Vector3(3f, 1.8f, 8.35f), Vector3.zero, new Vector3(3.5f, 2f, 0.05f), matWindowDim);

        // Window frames
        CreatePrim("FrameL_T", PrimitiveType.Cube, store.transform,
            new Vector3(-3f, 2.85f, 8.3f), Vector3.zero, new Vector3(3.6f, 0.08f, 0.08f), matMetal);
        CreatePrim("FrameL_B", PrimitiveType.Cube, store.transform,
            new Vector3(-3f, 0.75f, 8.3f), Vector3.zero, new Vector3(3.6f, 0.08f, 0.08f), matMetal);
        CreatePrim("FrameR_T", PrimitiveType.Cube, store.transform,
            new Vector3(3f, 2.85f, 8.3f), Vector3.zero, new Vector3(3.6f, 0.08f, 0.08f), matMetal);
        CreatePrim("FrameR_B", PrimitiveType.Cube, store.transform,
            new Vector3(3f, 0.75f, 8.3f), Vector3.zero, new Vector3(3.6f, 0.08f, 0.08f), matMetal);

        // Door (center, dark)
        CreatePrim("Door", PrimitiveType.Cube, store.transform,
            new Vector3(0, 1.3f, 8.32f), Vector3.zero, new Vector3(1.5f, 2.6f, 0.06f), matDoor);
        CreatePrim("DoorFrame", PrimitiveType.Cube, store.transform,
            new Vector3(0, 1.3f, 8.28f), Vector3.zero, new Vector3(1.7f, 2.8f, 0.04f), matMetal);

        // Upper wall above awning
        CreatePrim("UpperWall", PrimitiveType.Cube, store.transform,
            new Vector3(0, 3.8f, 8.5f), Vector3.zero, new Vector3(14f, 0.8f, 0.35f), matBrickDark);

        // ============================
        // STORE LIGHTING
        // ============================

        // Interior glow spilling through windows
        GameObject intGlow = new GameObject("InteriorGlow");
        intGlow.transform.parent = store.transform;
        intGlow.transform.localPosition = new Vector3(-1f, 2f, 7.5f);
        Light ig = intGlow.AddComponent<Light>();
        ig.type = LightType.Spot;
        ig.color = new Color(0.6f, 0.45f, 0.25f);
        ig.intensity = 1.2f;
        ig.range = 8f;
        ig.spotAngle = 100f;
        ig.transform.localRotation = Quaternion.Euler(15, 180, 0);
        ig.shadows = LightShadows.Soft;

        // Second interior glow (right window, dimmer)
        GameObject intGlow2 = new GameObject("InteriorGlow2");
        intGlow2.transform.parent = store.transform;
        intGlow2.transform.localPosition = new Vector3(3f, 2f, 7.5f);
        Light ig2 = intGlow2.AddComponent<Light>();
        ig2.type = LightType.Spot;
        ig2.color = new Color(0.5f, 0.4f, 0.25f);
        ig2.intensity = 0.6f;
        ig2.range = 5f;
        ig2.spotAngle = 80f;
        ig2.transform.localRotation = Quaternion.Euler(15, 180, 0);
        ig2.shadows = LightShadows.None;

        // Under-awning ambient
        GameObject awningGlow = new GameObject("AwningGlow");
        awningGlow.transform.parent = store.transform;
        awningGlow.transform.localPosition = new Vector3(0, 2.8f, 6.5f);
        Light ag = awningGlow.AddComponent<Light>();
        ag.type = LightType.Spot;
        ag.color = new Color(0.5f, 0.38f, 0.22f);
        ag.intensity = 0.5f;
        ag.range = 4f;
        ag.spotAngle = 120f;
        ag.transform.localRotation = Quaternion.Euler(90, 0, 0);
        ag.shadows = LightShadows.None;

        // ============================
        // CAR (using Car.fbx model)
        // ============================
        GameObject car = new GameObject("Car");
        car.transform.parent = root.transform;

        GameObject carPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ModelsPlace/psx-low-poly-car-renault/source/Car.fbx");
        if (carPrefab != null)
        {
            GameObject carModel = (GameObject)PrefabUtility.InstantiatePrefab(carPrefab);
            carModel.name = "CarModel";
            carModel.transform.parent = car.transform;
            carModel.transform.localPosition = new Vector3(1.5f, 0.38f, 1f);
            carModel.transform.localRotation = Quaternion.Euler(-90, 0, -65);
            carModel.transform.localScale = new Vector3(75.1f, 75.1f, 75.1f);
            ApplyCarTextures(carModel);
        }
        else
        {
            Debug.LogWarning("Car.fbx not found at expected path!");
            // Fallback placeholder
            CreatePrim("CarBody", PrimitiveType.Cube, car.transform,
                new Vector3(1.5f, 0.6f, 1f), new Vector3(0, -65, 0), new Vector3(1.6f, 0.8f, 3.8f), matCar);
        }

        // Headlight spot lights
        GameObject hlL = new GameObject("HeadlightSpotL");
        hlL.transform.parent = car.transform;
        hlL.transform.localPosition = new Vector3(0.2f, 0.65f, -0.5f);
        hlL.transform.localRotation = Quaternion.Euler(10, -65, 0);
        Light hll = hlL.AddComponent<Light>();
        hll.type = LightType.Spot;
        hll.color = new Color(0.85f, 0.78f, 0.55f);
        hll.intensity = 2f;
        hll.range = 15f;
        hll.spotAngle = 50f;
        hll.shadows = LightShadows.Soft;

        GameObject hlR = new GameObject("HeadlightSpotR");
        hlR.transform.parent = car.transform;
        hlR.transform.localPosition = new Vector3(-0.5f, 0.65f, -1.5f);
        hlR.transform.localRotation = Quaternion.Euler(10, -65, 0);
        Light hlr = hlR.AddComponent<Light>();
        hlr.type = LightType.Spot;
        hlr.color = new Color(0.85f, 0.78f, 0.55f);
        hlr.intensity = 1.5f;
        hlr.range = 12f;
        hlr.spotAngle = 45f;
        hlr.shadows = LightShadows.Soft;

        // Faint tail light glow
        GameObject tlGlow = new GameObject("TailGlow");
        tlGlow.transform.parent = car.transform;
        tlGlow.transform.localPosition = new Vector3(3f, 0.55f, 2.5f);
        Light tg = tlGlow.AddComponent<Light>();
        tg.type = LightType.Point;
        tg.color = new Color(0.5f, 0.03f, 0.01f);
        tg.intensity = 0.2f;
        tg.range = 1.5f;
        tg.shadows = LightShadows.None;

        // ============================
        // CHAIN LINK FENCE (left side)
        // ============================
        GameObject fence = new GameObject("Fence");
        fence.transform.parent = root.transform;

        for (int i = 0; i < 10; i++)
        {
            float z = -5f + i * 2.2f;
            CreatePrim($"FPost{i}", PrimitiveType.Cylinder, fence.transform,
                new Vector3(-7f, 1f, z), Vector3.zero, new Vector3(0.04f, 1f, 0.04f), matFencePost);
        }
        for (int i = 0; i < 9; i++)
        {
            float z = -3.9f + i * 2.2f;
            CreatePrim($"FPanel{i}", PrimitiveType.Cube, fence.transform,
                new Vector3(-7f, 1f, z), Vector3.zero, new Vector3(0.015f, 1.8f, 2.1f), matFence);
        }
        // Top rail
        CreatePrim("FenceRail", PrimitiveType.Cylinder, fence.transform,
            new Vector3(-7f, 2f, 5f), new Vector3(90, 0, 0), new Vector3(0.025f, 12f, 0.025f), matFencePost);

        // ============================
        // BOLLARDS
        // ============================
        for (int i = 0; i < 5; i++)
        {
            float x = -4f + i * 2f;
            CreatePrim($"BPost{i}", PrimitiveType.Cylinder, root.transform,
                new Vector3(x, 0.4f, 5.1f), Vector3.zero, new Vector3(0.08f, 0.4f, 0.08f), matBollardPost);
            CreatePrim($"BTop{i}", PrimitiveType.Cylinder, root.transform,
                new Vector3(x, 0.8f, 5.1f), Vector3.zero, new Vector3(0.1f, 0.04f, 0.1f), matBollard);
        }

        // ============================
        // SNOWFALL
        // ============================
        GameObject snowObj = new GameObject("Snowfall");
        snowObj.transform.parent = root.transform;
        snowObj.transform.localPosition = new Vector3(0, 10f, 2f);

        ParticleSystem ps = snowObj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.startLifetime = 10f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.035f);
        main.startColor = new Color(0.8f, 0.8f, 0.85f, 0.7f);
        main.maxParticles = 4000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.03f;

        var emission = ps.emission;
        emission.rateOverTime = 500f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(25f, 0.5f, 25f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.12f);
        vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.04f, 0.08f);

        ParticleSystemRenderer psr = snowObj.GetComponent<ParticleSystemRenderer>();
        psr.renderMode = ParticleSystemRenderMode.Billboard;
        Material snowMat = new Material(Shader.Find("Particles/Standard Unlit"));
        if (snowMat != null)
        {
            snowMat.name = "SnowParticle";
            snowMat.SetColor("_Color", new Color(0.85f, 0.85f, 0.9f, 0.6f));
            psr.material = snowMat;
        }

        // ============================
        // SCENE LIGHTING
        // ============================

        // Moonlight (very faint purple)
        GameObject moonObj = new GameObject("Moonlight");
        moonObj.transform.parent = root.transform;
        moonObj.transform.localRotation = Quaternion.Euler(30, -50, 0);
        Light moon = moonObj.AddComponent<Light>();
        moon.type = LightType.Directional;
        moon.color = new Color(0.18f, 0.12f, 0.28f);
        moon.intensity = 0.08f;
        moon.shadows = LightShadows.Soft;

        // Purple ambient fill (sky glow)
        GameObject purpleFill = new GameObject("PurpleFill");
        purpleFill.transform.parent = root.transform;
        purpleFill.transform.localPosition = new Vector3(0, 8, 0);
        Light pf = purpleFill.AddComponent<Light>();
        pf.type = LightType.Point;
        pf.color = new Color(0.2f, 0.12f, 0.3f);
        pf.intensity = 0.25f;
        pf.range = 30f;
        pf.shadows = LightShadows.None;

        // ============================
        // CAMERA
        // ============================
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

        cam.transform.parent = root.transform;
        // Low angle, close to car, looking toward store
        cam.transform.localPosition = new Vector3(-2.5f, 1.1f, -2.5f);
        cam.transform.localRotation = Quaternion.Euler(5f, 18f, 0f);
        c.fieldOfView = 50f;
        c.nearClipPlane = 0.05f;
        c.farClipPlane = 60f;
        c.backgroundColor = new Color(0.025f, 0.015f, 0.045f);
        c.clearFlags = CameraClearFlags.SolidColor;

        // ============================
        // RENDER SETTINGS
        // ============================
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.015f, 0.01f, 0.025f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.045f;
        RenderSettings.fogColor = new Color(0.03f, 0.02f, 0.05f);

        Selection.activeGameObject = root;
        Debug.Log("OPENFEED Main Menu Scene generated.");
    }

    // ============================
    // HELPERS
    // ============================
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
                    mat.SetColor("_EmissionColor", Color.white * 0.2f);
                }
            }
            r.sharedMaterial = mat;
        }
    }

    static void ApplyTexture(Material mat, Texture2D tex)
    {
        if (mat.HasProperty("_BaseMap"))
            mat.SetTexture("_BaseMap", tex);
        else
            mat.mainTexture = tex;
    }
}