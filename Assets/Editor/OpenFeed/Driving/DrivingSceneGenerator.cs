using UnityEngine;
using UnityEditor;

public class DrivingSceneGenerator : Editor
{
    [MenuItem("OPEN FEED/Driving/Clear", false, 200)]
    static void ClearScene()
    {
        GameObject existing = GameObject.Find("DrivingScene");
        if (existing != null)
        {
            DestroyImmediate(existing);
            Debug.Log("OPENFEED Driving Scene cleared.");
        }
    }

    [MenuItem("OPEN FEED/Driving/Generate", false, 0)]
    static void GenerateScene()
    {
        GameObject existing = GameObject.Find("DrivingScene");
        if (existing != null) DestroyImmediate(existing);

        Light[] allLights = FindObjectsByType<Light>();
        foreach (Light l in allLights)
        {
            if (l.type == LightType.Directional)
                DestroyImmediate(l.gameObject);
        }

        GameObject root = new GameObject("DrivingScene");

        // ============================
        // MATERIALS
        // ============================
        Material matRoad = CreateMatte("Road", new Color(0.025f, 0.025f, 0.03f));
        Material matRoadWet = CreateMatte("RoadWet", new Color(0.015f, 0.015f, 0.025f));
        Material matLaneLine = CreateEmissive("LaneLine", new Color(0.15f, 0.14f, 0.08f),
            new Color(0.2f, 0.18f, 0.1f), 0.3f);
        Material matShoulder = CreateMatte("Shoulder", new Color(0.035f, 0.035f, 0.04f));
        Material matDirt = CreateMatte("Dirt", new Color(0.03f, 0.028f, 0.025f));
        Material matGrass = CreateMatte("Grass", new Color(0.02f, 0.03f, 0.02f));
        Material matTree = CreateMatte("TreeTrunk", new Color(0.03f, 0.025f, 0.02f));
        Material matFoliage = CreateMatte("Foliage", new Color(0.02f, 0.035f, 0.02f));
        Material matPole = CreateMatte("Pole", new Color(0.05f, 0.05f, 0.055f));
        Material matGuardrail = CreateMatte("Guardrail", new Color(0.06f, 0.06f, 0.065f));
        Material matLampGlow = CreateEmissive("LampGlow", new Color(0.6f, 0.5f, 0.3f),
            new Color(0.7f, 0.55f, 0.3f), 2f);
        Material matHeadlight = CreateEmissive("Headlight", new Color(0.8f, 0.75f, 0.55f),
            new Color(0.9f, 0.85f, 0.6f), 1.5f);
        Material matTaillight = CreateEmissive("Taillight", new Color(0.4f, 0.02f, 0.01f),
            new Color(0.5f, 0.05f, 0.02f), 0.8f);
        Material matDash = CreateEmissive("Dashboard", new Color(0.02f, 0.04f, 0.02f),
            new Color(0.03f, 0.08f, 0.03f), 0.3f);
        Material matInterior = CreateMatte("Interior", new Color(0.03f, 0.03f, 0.035f));
        Material matSky = CreateMatte("Sky", new Color(0.01f, 0.008f, 0.025f));

        // ============================
        // ROAD (long strip extending forward)
        // ============================
        float roadLen = 200f;
        float roadW = 8f;

        GameObject road = new GameObject("Road");
        road.transform.parent = root.transform;

        // Main road surface
        CreatePrim("RoadSurface", PrimitiveType.Cube, road.transform,
            new Vector3(0, -0.05f, roadLen / 2 - 10f), Vector3.zero,
            new Vector3(roadW, 0.1f, roadLen), matRoad);

        // Wet road reflections
        for (int i = 0; i < 8; i++)
        {
            float z = i * 25f;
            CreatePrim($"WetPatch_{i}", PrimitiveType.Cube, road.transform,
                new Vector3(-0.5f + i % 3 * 0.8f, 0.005f, z), new Vector3(0, 5 + i * 3, 0),
                new Vector3(2f + i % 2, 0.003f, 8f + i % 3 * 3f), matRoadWet);
        }

        // Center lane dashes
        for (int i = 0; i < 40; i++)
        {
            float z = i * 5f;
            CreatePrim($"CenterLine_{i}", PrimitiveType.Cube, road.transform,
                new Vector3(0, 0.01f, z), Vector3.zero, new Vector3(0.12f, 0.01f, 3f), matLaneLine);
        }

        // Edge lines (solid)
        CreatePrim("EdgeLineL", PrimitiveType.Cube, road.transform,
            new Vector3(-roadW / 2 + 0.3f, 0.01f, roadLen / 2 - 10f), Vector3.zero,
            new Vector3(0.1f, 0.01f, roadLen), matLaneLine);
        CreatePrim("EdgeLineR", PrimitiveType.Cube, road.transform,
            new Vector3(roadW / 2 - 0.3f, 0.01f, roadLen / 2 - 10f), Vector3.zero,
            new Vector3(0.1f, 0.01f, roadLen), matLaneLine);

        // Shoulders
        CreatePrim("ShoulderL", PrimitiveType.Cube, road.transform,
            new Vector3(-roadW / 2 - 1f, -0.06f, roadLen / 2 - 10f), Vector3.zero,
            new Vector3(2f, 0.08f, roadLen), matShoulder);
        CreatePrim("ShoulderR", PrimitiveType.Cube, road.transform,
            new Vector3(roadW / 2 + 1f, -0.06f, roadLen / 2 - 10f), Vector3.zero,
            new Vector3(2f, 0.08f, roadLen), matShoulder);

        // Dirt/grass beyond shoulders
        CreatePrim("TerrainL", PrimitiveType.Cube, road.transform,
            new Vector3(-roadW / 2 - 15f, -0.15f, roadLen / 2 - 10f), Vector3.zero,
            new Vector3(30f, 0.2f, roadLen + 20f), matDirt);
        CreatePrim("TerrainR", PrimitiveType.Cube, road.transform,
            new Vector3(roadW / 2 + 15f, -0.15f, roadLen / 2 - 10f), Vector3.zero,
            new Vector3(30f, 0.2f, roadLen + 20f), matDirt);

        // Far ground
        CreatePrim("FarGround", PrimitiveType.Cube, road.transform,
            new Vector3(0, -0.3f, roadLen / 2), Vector3.zero,
            new Vector3(100f, 0.1f, roadLen + 40f), matGrass);

        // ============================
        // STREET LIGHTS
        // ============================
        GameObject streetLights = new GameObject("StreetLights");
        streetLights.transform.parent = root.transform;

        for (int i = 0; i < 12; i++)
        {
            float z = i * 16f;
            float side = (i % 2 == 0) ? -1f : 1f;
            float x = side * (roadW / 2 + 1.5f);

            // Pole
            CreatePrim($"Pole_{i}", PrimitiveType.Cylinder, streetLights.transform,
                new Vector3(x, 2.5f, z), Vector3.zero, new Vector3(0.08f, 2.5f, 0.08f), matPole);

            // Arm
            CreatePrim($"Arm_{i}", PrimitiveType.Cube, streetLights.transform,
                new Vector3(x - side * 1f, 5f, z), Vector3.zero, new Vector3(2.2f, 0.05f, 0.05f), matPole);

            // Lamp fixture
            CreatePrim($"Lamp_{i}", PrimitiveType.Cube, streetLights.transform,
                new Vector3(x - side * 2f, 4.9f, z), Vector3.zero, new Vector3(0.3f, 0.08f, 0.15f), matLampGlow);

            // Light source
            GameObject lightObj = new GameObject($"StreetLight_{i}");
            lightObj.transform.parent = streetLights.transform;
            lightObj.transform.localPosition = new Vector3(x - side * 2f, 4.8f, z);
            Light lt = lightObj.AddComponent<Light>();
            lt.type = LightType.Spot;
            lt.color = new Color(0.6f, 0.5f, 0.3f);
            lt.intensity = 1.5f;
            lt.range = 12f;
            lt.spotAngle = 70f;
            lt.transform.localRotation = Quaternion.Euler(90, 0, 0);
            lt.shadows = (i % 3 == 0) ? LightShadows.Soft : LightShadows.None;
        }

        // ============================
        // TREES (sparse, dark silhouettes)
        // ============================
        GameObject trees = new GameObject("Trees");
        trees.transform.parent = root.transform;

        System.Random rng = new System.Random(42);
        for (int i = 0; i < 30; i++)
        {
            float side = (i % 2 == 0) ? -1f : 1f;
            float x = side * (roadW / 2 + 4f + (float)rng.NextDouble() * 12f);
            float z = (float)rng.NextDouble() * roadLen;
            float height = 3f + (float)rng.NextDouble() * 4f;
            float trunkH = height * 0.4f;

            CreatePrim($"Trunk_{i}", PrimitiveType.Cylinder, trees.transform,
                new Vector3(x, trunkH / 2, z), Vector3.zero,
                new Vector3(0.15f, trunkH / 2, 0.15f), matTree);

            CreatePrim($"Foliage_{i}", PrimitiveType.Sphere, trees.transform,
                new Vector3(x, trunkH + height * 0.2f, z), Vector3.zero,
                new Vector3(height * 0.5f, height * 0.4f, height * 0.5f), matFoliage);
        }

        // ============================
        // GUARDRAILS
        // ============================
        GameObject guardrails = new GameObject("Guardrails");
        guardrails.transform.parent = root.transform;

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * (roadW / 2 + 0.5f);
            string sName = side < 0 ? "L" : "R";

            // Posts
            for (int i = 0; i < 25; i++)
            {
                float z = i * 8f;
                CreatePrim($"GPost_{sName}_{i}", PrimitiveType.Cube, guardrails.transform,
                    new Vector3(x, 0.3f, z), Vector3.zero, new Vector3(0.06f, 0.6f, 0.06f), matPole);
            }

            // Rail
            CreatePrim($"GRail_{sName}", PrimitiveType.Cube, guardrails.transform,
                new Vector3(x, 0.5f, roadLen / 2 - 10f), Vector3.zero,
                new Vector3(0.04f, 0.15f, roadLen), matGuardrail);
        }

        // ============================
        // CAR INTERIOR (dashboard view)
        // ============================
        GameObject carInterior = new GameObject("CarInterior");
        carInterior.transform.parent = root.transform;

        // Dashboard
        CreatePrim("Dashboard", PrimitiveType.Cube, carInterior.transform,
            new Vector3(0, 0.55f, 1.2f), new Vector3(-5, 0, 0), new Vector3(1.8f, 0.15f, 0.8f), matInterior);

        // Dashboard glow (instrument cluster)
        CreatePrim("DashGlow", PrimitiveType.Cube, carInterior.transform,
            new Vector3(0, 0.64f, 0.95f), new Vector3(60, 0, 0), new Vector3(0.5f, 0.01f, 0.2f), matDash);

        // Steering wheel (simplified)
        CreatePrim("SteeringCol", PrimitiveType.Cylinder, carInterior.transform,
            new Vector3(-0.35f, 0.7f, 1f), new Vector3(65, 0, 0), new Vector3(0.03f, 0.2f, 0.03f), matInterior);

        // Rearview mirror
        CreatePrim("RearMirror", PrimitiveType.Cube, carInterior.transform,
            new Vector3(0, 1.3f, 1.1f), new Vector3(0, 0, 5), new Vector3(0.35f, 0.08f, 0.04f), matInterior);

        // A-pillars (windshield frame)
        CreatePrim("APillarL", PrimitiveType.Cube, carInterior.transform,
            new Vector3(-0.95f, 1f, 1.3f), new Vector3(0, 0, 12), new Vector3(0.06f, 1.2f, 0.06f), matInterior);
        CreatePrim("APillarR", PrimitiveType.Cube, carInterior.transform,
            new Vector3(0.95f, 1f, 1.3f), new Vector3(0, 0, -12), new Vector3(0.06f, 1.2f, 0.06f), matInterior);

        // Roof edge
        CreatePrim("RoofEdge", PrimitiveType.Cube, carInterior.transform,
            new Vector3(0, 1.55f, 0.8f), Vector3.zero, new Vector3(2f, 0.04f, 1.5f), matInterior);

        // Headlight cones (projected onto road, visible from inside)
        GameObject hlL = new GameObject("HeadlightL");
        hlL.transform.parent = carInterior.transform;
        hlL.transform.localPosition = new Vector3(-0.5f, 0.5f, 2f);
        hlL.transform.localRotation = Quaternion.Euler(8, -3, 0);
        Light hLeft = hlL.AddComponent<Light>();
        hLeft.type = LightType.Spot;
        hLeft.color = new Color(0.85f, 0.78f, 0.55f);
        hLeft.intensity = 3f;
        hLeft.range = 40f;
        hLeft.spotAngle = 45f;
        hLeft.shadows = LightShadows.Soft;

        GameObject hlR = new GameObject("HeadlightR");
        hlR.transform.parent = carInterior.transform;
        hlR.transform.localPosition = new Vector3(0.5f, 0.5f, 2f);
        hlR.transform.localRotation = Quaternion.Euler(8, 3, 0);
        Light hRight = hlR.AddComponent<Light>();
        hRight.type = LightType.Spot;
        hRight.color = new Color(0.85f, 0.78f, 0.55f);
        hRight.intensity = 3f;
        hRight.range = 40f;
        hRight.spotAngle = 45f;
        hRight.shadows = LightShadows.Soft;

        // ============================
        // RAIN / SNOW PARTICLES
        // ============================
        GameObject rainObj = new GameObject("Rain");
        rainObj.transform.parent = root.transform;
        rainObj.transform.localPosition = new Vector3(0, 8f, 10f);

        ParticleSystem ps = rainObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 3f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.015f);
        main.startColor = new Color(0.6f, 0.6f, 0.7f, 0.5f);
        main.maxParticles = 5000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.5f;

        var emission = ps.emission;
        emission.rateOverTime = 800f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(12f, 0.5f, 30f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.z = new ParticleSystem.MinMaxCurve(0f, 2f);

        ParticleSystemRenderer psr = rainObj.GetComponent<ParticleSystemRenderer>();
        psr.renderMode = ParticleSystemRenderMode.Stretch;
        psr.lengthScale = 8f;
        Material rainMat = new Material(Shader.Find("Particles/Standard Unlit"));
        if (rainMat != null)
        {
            rainMat.name = "RainParticle";
            rainMat.SetColor("_Color", new Color(0.5f, 0.5f, 0.6f, 0.3f));
            psr.material = rainMat;
        }

        // ============================
        // SCROLLING WAYPOINTS (for road animation)
        // ============================
        GameObject waypoints = new GameObject("CutsceneWaypoints");
        waypoints.transform.parent = root.transform;

        // Camera stays roughly fixed inside car, road scrolls
        CreateWaypoint(waypoints.transform, "WP_DriveStart",
            new Vector3(-0.3f, 1.05f, 0.4f), new Vector3(3, 0, 0));
        CreateWaypoint(waypoints.transform, "WP_DriveMid",
            new Vector3(-0.3f, 1.05f, 0.4f), new Vector3(2, -5, 0));
        CreateWaypoint(waypoints.transform, "WP_DriveLookRight",
            new Vector3(-0.3f, 1.05f, 0.4f), new Vector3(3, 25, 0));
        CreateWaypoint(waypoints.transform, "WP_DriveEnd",
            new Vector3(-0.3f, 1.05f, 0.4f), new Vector3(3, 0, 0));

        // ============================
        // SCENE LIGHTING
        // ============================
        GameObject moonObj = new GameObject("Moonlight");
        moonObj.transform.parent = root.transform;
        moonObj.transform.localRotation = Quaternion.Euler(25, -40, 0);
        Light moon = moonObj.AddComponent<Light>();
        moon.type = LightType.Directional;
        moon.color = new Color(0.12f, 0.08f, 0.2f);
        moon.intensity = 0.05f;
        moon.shadows = LightShadows.Soft;

        // ============================
        // CAMERA
        // ============================
        Camera cam = SetupCamera(root.transform);
        // Inside car, driver perspective
        cam.transform.localPosition = new Vector3(-0.3f, 1.05f, 0.4f);
        cam.transform.localRotation = Quaternion.Euler(3, 0, 0);
        cam.fieldOfView = 60f;
        cam.backgroundColor = new Color(0.01f, 0.008f, 0.025f);
        cam.clearFlags = CameraClearFlags.SolidColor;

        // ============================
        // RENDER SETTINGS
        // ============================
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.01f, 0.008f, 0.015f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.015f;
        RenderSettings.fogColor = new Color(0.015f, 0.01f, 0.03f);

        Selection.activeGameObject = root;
        Debug.Log("OPENFEED Driving Scene generated.");
    }

    // ============================
    // HELPERS
    // ============================
    static void CreateWaypoint(Transform parent, string name, Vector3 pos, Vector3 rot)
    {
        GameObject wp = new GameObject(name);
        wp.transform.parent = parent;
        wp.transform.localPosition = pos;
        wp.transform.localRotation = Quaternion.Euler(rot);
    }

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
        c.farClipPlane = 200f;
        return c;
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
