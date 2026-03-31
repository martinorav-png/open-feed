using UnityEngine;
using UnityEngine.SceneManagement;

public class GroceryStoreAmbientBootstrap : MonoBehaviour
{
    const string StoreSceneName = "GroceryStore";
    const string ExteriorRootName = "ExteriorDressingRuntime";
    const string SnowRootName = "SnowfallRuntime";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureBootstrapOnStoreScene()
    {
        Scene active = SceneManager.GetActiveScene();
        if (active.name != StoreSceneName)
            return;

        if (FindAnyObjectByType<GroceryStoreAmbientBootstrap>() != null)
            return;

        GameObject root = new GameObject(nameof(GroceryStoreAmbientBootstrap));
        root.AddComponent<GroceryStoreAmbientBootstrap>();
    }

    void Awake()
    {
        EnsureAudioListener();
        EnsureMainMenuUI();
    }

    static void EnsureAudioListener()
    {
        if (FindAnyObjectByType<AudioListener>() != null)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            cam = FindAnyObjectByType<Camera>();

        if (cam != null)
            cam.gameObject.AddComponent<AudioListener>();
    }

    static void EnsureMainMenuUI()
    {
        if (FindAnyObjectByType<MainMenuUI>() != null)
            return;

        GameObject uiRoot = new GameObject("MainMenuUI");
        uiRoot.AddComponent<MainMenuUI>();
    }

    static void EnsureSnowfall()
    {
        if (GameObject.Find(SnowRootName) != null)
            return;

        GameObject snowObj = new GameObject(SnowRootName);
        snowObj.transform.position = new Vector3(0f, 14f, 0f);

        ParticleSystem ps = snowObj.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 13f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.9f, 1.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f);
        main.maxParticles = 2000;
        main.startColor = new Color(0.92f, 0.95f, 1f, 0.78f);

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 180f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(95f, 1f, 95f);

        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.y = new ParticleSystem.MinMaxCurve(-2.6f, -1.2f);
        vel.x = new ParticleSystem.MinMaxCurve(-0.45f, 0.45f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.45f, 0.45f);

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.15f;
        noise.scrollSpeed = 0.1f;

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateSnowMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = 5;
    }

    static void EnsureExteriorDressing()
    {
        if (GameObject.Find(ExteriorRootName) != null)
            return;

        GameObject exteriorRoot = new GameObject(ExteriorRootName);

        Material road = CreateLitMaterial(new Color(0.09f, 0.09f, 0.1f));
        Material shoulder = CreateLitMaterial(new Color(0.18f, 0.19f, 0.2f));
        Material houseA = CreateLitMaterial(new Color(0.31f, 0.28f, 0.24f));
        Material houseB = CreateLitMaterial(new Color(0.23f, 0.27f, 0.33f));
        Material roof = CreateLitMaterial(new Color(0.16f, 0.12f, 0.12f));
        Material window = CreateLitMaterial(new Color(0.84f, 0.74f, 0.48f), 0.25f);
        Material trunk = CreateLitMaterial(new Color(0.18f, 0.12f, 0.08f));
        Material leaves = CreateLitMaterial(new Color(0.16f, 0.22f, 0.16f));

        // Road and shoulders sit in front of the store entrance area.
        CreatePrimitive("Road", PrimitiveType.Cube, exteriorRoot.transform,
            new Vector3(0f, -0.02f, -27f), new Vector3(44f, 0.08f, 14f), road);
        CreatePrimitive("RoadShoulderL", PrimitiveType.Cube, exteriorRoot.transform,
            new Vector3(-16f, -0.015f, -27f), new Vector3(12f, 0.05f, 14f), shoulder);
        CreatePrimitive("RoadShoulderR", PrimitiveType.Cube, exteriorRoot.transform,
            new Vector3(16f, -0.015f, -27f), new Vector3(12f, 0.05f, 14f), shoulder);
        CreatePrimitive("RoadLine", PrimitiveType.Cube, exteriorRoot.transform,
            new Vector3(0f, 0.005f, -27f), new Vector3(0.2f, 0.01f, 13f), CreateLitMaterial(new Color(0.86f, 0.82f, 0.64f)));

        CreateHouseCluster(exteriorRoot.transform, new Vector3(-24f, 0f, -34f), houseA, roof, window, "Left");
        CreateHouseCluster(exteriorRoot.transform, new Vector3(24f, 0f, -32f), houseB, roof, window, "Right");
        CreateHouseCluster(exteriorRoot.transform, new Vector3(-32f, 0f, -18f), houseB, roof, window, "BackLeft");
        CreateHouseCluster(exteriorRoot.transform, new Vector3(31f, 0f, -18f), houseA, roof, window, "BackRight");

        for (int i = 0; i < 14; i++)
        {
            float x = -34f + i * 5.3f;
            CreateTree(exteriorRoot.transform, new Vector3(x, 0f, -14f - (i % 2 == 0 ? 1.5f : 0f)), trunk, leaves, $"TreeFront_{i}");
        }

        for (int i = 0; i < 10; i++)
        {
            float z = -38f + i * 4.2f;
            CreateTree(exteriorRoot.transform, new Vector3(-36f, 0f, z), trunk, leaves, $"TreeLeft_{i}");
            CreateTree(exteriorRoot.transform, new Vector3(36f, 0f, z), trunk, leaves, $"TreeRight_{i}");
        }
    }

    static void CreateHouseCluster(Transform parent, Vector3 worldPos, Material wallMat, Material roofMat, Material windowMat, string suffix)
    {
        GameObject root = new GameObject($"House_{suffix}");
        root.transform.SetParent(parent, false);
        root.transform.position = worldPos;

        CreatePrimitive("Base", PrimitiveType.Cube, root.transform,
            new Vector3(0f, 2.2f, 0f), new Vector3(5.2f, 4.4f, 4.6f), wallMat);
        CreatePrimitive("Roof", PrimitiveType.Cube, root.transform,
            new Vector3(0f, 4.8f, 0f), new Vector3(5.8f, 1f, 5.2f), roofMat);
        CreatePrimitive("WindowL", PrimitiveType.Cube, root.transform,
            new Vector3(-1.2f, 2.5f, 2.31f), new Vector3(1f, 1f, 0.08f), windowMat);
        CreatePrimitive("WindowR", PrimitiveType.Cube, root.transform,
            new Vector3(1.2f, 2.5f, 2.31f), new Vector3(1f, 1f, 0.08f), windowMat);
        CreatePrimitive("Door", PrimitiveType.Cube, root.transform,
            new Vector3(0f, 1.2f, 2.31f), new Vector3(1f, 2.2f, 0.08f), CreateLitMaterial(new Color(0.12f, 0.1f, 0.09f)));
    }

    static void CreateTree(Transform parent, Vector3 worldPos, Material trunkMat, Material leavesMat, string name)
    {
        GameObject tree = new GameObject(name);
        tree.transform.SetParent(parent, false);
        tree.transform.position = worldPos;

        CreatePrimitive("Trunk", PrimitiveType.Cylinder, tree.transform,
            new Vector3(0f, 1.1f, 0f), new Vector3(0.3f, 1.1f, 0.3f), trunkMat);
        CreatePrimitive("CrownA", PrimitiveType.Sphere, tree.transform,
            new Vector3(0f, 3.1f, 0f), new Vector3(1.6f, 1.3f, 1.6f), leavesMat);
        CreatePrimitive("CrownB", PrimitiveType.Sphere, tree.transform,
            new Vector3(-0.55f, 2.7f, 0.25f), new Vector3(1.1f, 0.9f, 1.1f), leavesMat);
        CreatePrimitive("CrownC", PrimitiveType.Sphere, tree.transform,
            new Vector3(0.6f, 2.6f, -0.3f), new Vector3(1.05f, 0.85f, 1.05f), leavesMat);
    }

    static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 localPos, Vector3 localScale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = localScale;

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && material != null)
            renderer.sharedMaterial = material;

        Collider col = obj.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        return obj;
    }

    static Material CreateSnowMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material mat = new Material(shader);
        mat.name = "RuntimeSnowMat";
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", Color.white);
        return mat;
    }

    static Material CreateLitMaterial(Color color, float emissionStrength = 0f)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);

        if (emissionStrength > 0f)
        {
            Color emission = color * emissionStrength;
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", emission);
        }

        return mat;
    }
}
