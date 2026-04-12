using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class DrivingSceneGenerator : Editor
{
    const string ForestDriveScenePath = "Assets/ForestDrive.unity";
    const string SquareTileTreePrefabPath = "Assets/SquareTileTerrain/Example/Tree/TreePrefab.prefab";

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

    /// <summary>Rebuilds content in the <b>currently open</b> scene (use for Driving.unity or any test scene).</summary>
    [MenuItem("OPEN FEED/Driving/Generate", false, 0)]
    static void GenerateScene()
    {
        GenerateIntoActiveScene(preserveCarAndCamera: true);
    }

    /// <summary>Rebuilds road/trees/etc. and replaces <c>CarInterior</c> + reparents camera from generator defaults.</summary>
    [MenuItem("OPEN FEED/Driving/Generate (full reset car & camera)", false, 11)]
    static void GenerateSceneFullReset()
    {
        GenerateIntoActiveScene(preserveCarAndCamera: false);
    }

    /// <summary>Opens <c>ForestDrive.unity</c>, regenerates <c>DrivingScene</c>, saves the asset.</summary>
    [MenuItem("OPEN FEED/Forest Drive/Generate & Save Scene", false, 0)]
    static void GenerateForestDriveScene()
    {
        GenerateForestDriveSceneInternal(preserveCarAndCamera: true);
    }

    /// <summary>Same as Generate &amp; Save Scene but rebuilds <c>CarInterior</c> and camera from code.</summary>
    [MenuItem("OPEN FEED/Forest Drive/Generate & Save Scene (full reset)", false, 11)]
    static void GenerateForestDriveSceneFullReset()
    {
        GenerateForestDriveSceneInternal(preserveCarAndCamera: false);
    }

    static void GenerateForestDriveSceneInternal(bool preserveCarAndCamera)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        if (!System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "..", ForestDriveScenePath.Replace('/', System.IO.Path.DirectorySeparatorChar))))
        {
            Debug.LogError($"OPENFEED: Scene asset not found: {ForestDriveScenePath}");
            return;
        }

        EditorSceneManager.OpenScene(ForestDriveScenePath);
        GenerateIntoActiveScene(preserveCarAndCamera);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log(preserveCarAndCamera
            ? "OPENFEED: ForestDrive.unity saved (CarInterior + Main Camera under DrivingScene were kept if present)."
            : "OPENFEED: ForestDrive.unity saved (full reset: car rig and camera rebuilt).");
    }

    /// <param name="preserveCarAndCamera">If true, keeps existing <c>CarInterior</c> and <c>Main Camera</c> when they are children of <c>DrivingScene</c> (your manual Impreza pose, dash glow, etc.).</param>
    static void GenerateIntoActiveScene(bool preserveCarAndCamera = true)
    {
        GameObject existing = GameObject.Find("DrivingScene");
        Transform preservedCarInterior = null;
        GameObject preservedMainCamera = null;

        if (existing != null && preserveCarAndCamera)
        {
            Transform ci = existing.transform.Find("CarInterior");
            if (ci != null)
            {
                preservedCarInterior = ci;
                preservedCarInterior.SetParent(null, worldPositionStays: true);
            }

            GameObject camGo = GameObject.FindGameObjectWithTag("MainCamera");
            if (camGo == null)
                camGo = GameObject.Find("Main Camera");
            if (camGo != null && camGo.transform.IsChildOf(existing.transform))
            {
                preservedMainCamera = camGo;
                preservedMainCamera.transform.SetParent(null, worldPositionStays: true);
            }
        }

        if (existing != null)
            DestroyImmediate(existing);

        Light[] allLights = FindObjectsByType<Light>();
        foreach (Light l in allLights)
        {
            if (l.type == LightType.Directional)
                DestroyImmediate(l.gameObject);
        }

        GameObject root = new GameObject("DrivingScene");

        Material matRoad = CreateMatte("Drive_Road", new Color(0.025f, 0.025f, 0.03f));
        Material matRoadWet = CreateMatte("Drive_RoadWet", new Color(0.015f, 0.015f, 0.025f));
        Material matLaneLine = CreateEmissive("Drive_LaneLine", new Color(0.15f, 0.14f, 0.08f),
            new Color(0.2f, 0.18f, 0.1f), 0.3f);
        Material matShoulder = CreateMatte("Drive_Shoulder", new Color(0.035f, 0.035f, 0.04f));
        Material matDirt = CreateMatte("Drive_Dirt", new Color(0.03f, 0.028f, 0.025f));
        Material matGrass = CreateMatte("Drive_Grass", new Color(0.02f, 0.03f, 0.02f));
        Material matDash = CreateEmissive("Drive_Dashboard", new Color(0.02f, 0.04f, 0.02f),
            new Color(0.03f, 0.08f, 0.03f), 0.3f);

        // Must cover CutscenePlayer scroll: roadScrollSpeed * drivingDuration (defaults ~19*48≈912) + margin.
        const float forestRoadLength = 1250f;
        float roadLen = forestRoadLength;
        float roadW = 4f;

        GameObject road = new GameObject("Road");
        road.transform.parent = root.transform;

        CreatePrim("RoadSurface", PrimitiveType.Cube, road.transform,
            new Vector3(0, -0.05f, roadLen / 2f - 10f), Vector3.zero,
            new Vector3(roadW, 0.1f, roadLen), matRoad);

        float wetZ = 12f;
        int wi = 0;
        while (wetZ < roadLen - 18f)
        {
            CreatePrim($"WetPatch_{wi}", PrimitiveType.Cube, road.transform,
                new Vector3(-0.3f + wi % 3 * 0.5f, 0.005f, wetZ), new Vector3(0, 4 + wi * 2, 0),
                new Vector3(1.4f + wi % 2 * 0.3f, 0.003f, 10f + wi % 2 * 4f), matRoadWet);
            wetZ += 22f + (wi % 5) * 2.8f;
            wi++;
        }

        // Single-lane backcountry: edge lines only (no center dashes)
        CreatePrim("EdgeLineL", PrimitiveType.Cube, road.transform,
            new Vector3(-roadW / 2f + 0.12f, 0.01f, roadLen / 2f - 10f), Vector3.zero,
            new Vector3(0.08f, 0.01f, roadLen), matLaneLine);
        CreatePrim("EdgeLineR", PrimitiveType.Cube, road.transform,
            new Vector3(roadW / 2f - 0.12f, 0.01f, roadLen / 2f - 10f), Vector3.zero,
            new Vector3(0.08f, 0.01f, roadLen), matLaneLine);

        float shoulderW = 1.15f;
        CreatePrim("ShoulderL", PrimitiveType.Cube, road.transform,
            new Vector3(-roadW / 2f - shoulderW / 2f, -0.06f, roadLen / 2f - 10f), Vector3.zero,
            new Vector3(shoulderW, 0.08f, roadLen), matShoulder);
        CreatePrim("ShoulderR", PrimitiveType.Cube, road.transform,
            new Vector3(roadW / 2f + shoulderW / 2f, -0.06f, roadLen / 2f - 10f), Vector3.zero,
            new Vector3(shoulderW, 0.08f, roadLen), matShoulder);

        float terrainHalfW = 14f;
        CreatePrim("TerrainL", PrimitiveType.Cube, road.transform,
            new Vector3(-roadW / 2f - shoulderW - terrainHalfW, -0.15f, roadLen / 2f - 10f), Vector3.zero,
            new Vector3(terrainHalfW * 2f, 0.2f, roadLen + 20f), matDirt);
        CreatePrim("TerrainR", PrimitiveType.Cube, road.transform,
            new Vector3(roadW / 2f + shoulderW + terrainHalfW, -0.15f, roadLen / 2f - 10f), Vector3.zero,
            new Vector3(terrainHalfW * 2f, 0.2f, roadLen + 20f), matDirt);

        CreatePrim("FarGround", PrimitiveType.Cube, road.transform,
            new Vector3(0, -0.3f, roadLen / 2f), Vector3.zero,
            new Vector3(72f, 0.1f, roadLen + 40f), matGrass);

        // Empty — CutscenePlayer still scrolls this transform (highway lamps off for forest road)
        GameObject streetLights = new GameObject("StreetLights");
        streetLights.transform.parent = root.transform;

        GameObject trees = new GameObject("Trees");
        trees.transform.parent = root.transform;
        GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SquareTileTreePrefabPath);
        if (treePrefab == null)
            Debug.LogError("OPENFEED Forest Drive: TreePrefab missing at " + SquareTileTreePrefabPath);
        else
            ScatterDenseForest(trees.transform, treePrefab, roadLen, roadW, shoulderW, seed: 42);

        // No guardrails on backcountry segment — keep node for scroll compatibility
        GameObject guardrails = new GameObject("Guardrails");
        guardrails.transform.parent = root.transform;

        if (preservedCarInterior != null)
        {
            preservedCarInterior.SetParent(root.transform, worldPositionStays: true);
            Debug.Log("OPENFEED: Kept existing CarInterior (your Impreza / dash / lights). Forest Drive → Generate (full reset) rebuilds from code.");
        }
        else
            BuildDefaultCarRig(root.transform, matDash);

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
        shape.scale = new Vector3(7f, 0.5f, 30f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.z = new ParticleSystem.MinMaxCurve(0f, 2f);

        ParticleSystemRenderer psr = rainObj.GetComponent<ParticleSystemRenderer>();
        psr.renderMode = ParticleSystemRenderMode.Stretch;
        psr.lengthScale = 8f;
        Material rainMat = new Material(Shader.Find("Particles/Standard Unlit"));
        if (rainMat != null)
        {
            rainMat.name = "Drive_RainParticle";
            rainMat.SetColor("_Color", new Color(0.5f, 0.5f, 0.6f, 0.3f));
            psr.material = rainMat;
        }

        GameObject waypoints = new GameObject("CutsceneWaypoints");
        waypoints.transform.parent = root.transform;

        CreateWaypoint(waypoints.transform, "WP_DriveStart",
            new Vector3(-0.3f, 1.05f, 0.4f), new Vector3(3, 0, 0));
        CreateWaypoint(waypoints.transform, "WP_DriveMid",
            new Vector3(-0.3f, 1.05f, 0.4f), new Vector3(2, -5, 0));
        CreateWaypoint(waypoints.transform, "WP_DriveLookRight",
            new Vector3(-0.3f, 1.05f, 0.4f), new Vector3(3, 25, 0));
        CreateWaypoint(waypoints.transform, "WP_DriveEnd",
            new Vector3(-0.3f, 1.05f, 0.4f), new Vector3(3, 0, 0));

        GameObject moonObj = new GameObject("Moonlight");
        moonObj.transform.parent = root.transform;
        moonObj.transform.localRotation = Quaternion.Euler(25, -40, 0);
        Light moon = moonObj.AddComponent<Light>();
        moon.type = LightType.Directional;
        moon.color = new Color(0.1f, 0.1f, 0.18f);
        moon.intensity = 0.018f;
        moon.shadows = LightShadows.None;

        float driveCamFar = Mathf.Clamp(roadLen + 420f, 500f, 2200f);
        if (preservedMainCamera != null)
        {
            preservedMainCamera.transform.SetParent(root.transform, worldPositionStays: true);
            Camera cam = preservedMainCamera.GetComponent<Camera>();
            if (cam != null)
            {
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = Mathf.Max(cam.farClipPlane, driveCamFar);
            }
        }
        else
        {
            Camera cam = SetupCamera(root.transform);
            cam.transform.localPosition = new Vector3(-0.3f, 1.05f, 0.4f);
            cam.transform.localRotation = Quaternion.Euler(3, 0, 0);
            cam.fieldOfView = 60f;
            cam.backgroundColor = new Color(0.01f, 0.008f, 0.025f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.farClipPlane = driveCamFar;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.003f, 0.003f, 0.006f);
        RenderSettings.reflectionIntensity = 0f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.03f;
        RenderSettings.fogColor = new Color(0.006f, 0.005f, 0.016f);

        if (FindAnyObjectByType<GameFlowManager>() == null)
        {
            GameObject gfmGo = new GameObject("GameFlowManager");
            gfmGo.AddComponent<GameFlowManager>();
        }

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("OPENFEED DrivingScene generated (narrow forest road).");
    }

    static void BuildDefaultCarRig(Transform root, Material matDash)
    {
        GameObject carInterior = new GameObject("CarInterior");
        carInterior.transform.SetParent(root, false);

        OpenFeedDrivingCarBuilder.AddExteriorCarUnderCarInterior(carInterior.transform);

        GameObject dashFill = new GameObject("DashInteriorFill");
        dashFill.transform.SetParent(carInterior.transform, false);
        dashFill.transform.localPosition = new Vector3(0.04f, 0.98f, 0.88f);
        Light fillL = dashFill.AddComponent<Light>();
        fillL.type = LightType.Point;
        fillL.color = new Color(0.92f, 0.66f, 0.38f);
        fillL.intensity = 0.11f;
        fillL.range = 1.85f;
        fillL.shadows = LightShadows.None;

        CreatePrim("DashGlow", PrimitiveType.Cube, carInterior.transform,
            new Vector3(0.019f, 0.771f, 1.022f), new Vector3(104.949f, 0f, 0f),
            new Vector3(0.263115f, 0.01f, 0.2f), matDash);

        GameObject hlL = new GameObject("HeadlightL");
        hlL.transform.parent = carInterior.transform;
        hlL.transform.localPosition = new Vector3(-0.5f, 0.5f, 2f);
        hlL.transform.localRotation = Quaternion.Euler(8, -3, 0);
        Light hLeft = hlL.AddComponent<Light>();
        hLeft.type = LightType.Spot;
        hLeft.color = new Color(1f, 0.94f, 0.8f);
        hLeft.intensity = 20f;
        hLeft.range = 110f;
        hLeft.spotAngle = 62f;
        hLeft.shadows = LightShadows.Soft;

        GameObject hlR = new GameObject("HeadlightR");
        hlR.transform.parent = carInterior.transform;
        hlR.transform.localPosition = new Vector3(0.5f, 0.5f, 2f);
        hlR.transform.localRotation = Quaternion.Euler(8, 3, 0);
        Light hRight = hlR.AddComponent<Light>();
        hRight.type = LightType.Spot;
        hRight.color = new Color(1f, 0.94f, 0.8f);
        hRight.intensity = 20f;
        hRight.range = 110f;
        hRight.spotAngle = 62f;
        hRight.shadows = LightShadows.Soft;
    }

    static void ScatterDenseForest(Transform treesRoot, GameObject treePrefab, float roadLen, float roadW, float shoulderW, int seed)
    {
        var rng = new System.Random(seed);
        float halfRoad = roadW * 0.5f;
        float innerEdge = halfRoad + shoulderW + 0.35f;
        float zSpacing = 1.55f;
        int zSteps = Mathf.CeilToInt((roadLen + 30f) / zSpacing);

        for (int zi = 0; zi < zSteps; zi++)
        {
            float z = zi * zSpacing + (float)rng.NextDouble() * 0.45f;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int row = 0; row < 5; row++)
                {
                    float depth = innerEdge + row * 1.75f + (float)rng.NextDouble() * 1.05f;
                    float jitterX = ((float)rng.NextDouble() - 0.5f) * 1.1f;
                    float x = side * (depth + jitterX);
                    float zJ = z + ((float)rng.NextDouble() - 0.5f) * 0.65f;

                    GameObject tr = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, treesRoot);
                    tr.name = "TreePrefab";
                    tr.transform.localPosition = new Vector3(x, 0f, zJ);
                    tr.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                    float s = 0.82f + (float)rng.NextDouble() * 0.45f;
                    tr.transform.localScale = new Vector3(s, s, s);

                    foreach (Collider c in tr.GetComponentsInChildren<Collider>(true))
                        DestroyImmediate(c);
                }
            }
        }
    }

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
        c.farClipPlane = 800f;
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
