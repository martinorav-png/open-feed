using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class SixTwelveSceneGenerator : Editor
{
    struct CarPlacement
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    const string RootName = "SixTwelveScene";
    const string ModelPath = "Assets/ModelsPlace/6twelve/source/6twelve/Models/6twelve.fbx";
    static readonly Vector3 MenuViewPosition = new Vector3(0.7122765f, 0.1832447f, 8.155008f);
    static readonly Vector3 MenuViewRotation = new Vector3(341.4f, 304.3f, 0f);
    static readonly Vector3 StoreEntranceViewPosition = new Vector3(-0.9404974f, 0.2538679f, 9.778698f);
    static readonly Vector3 StoreEntranceViewRotation = new Vector3(358.6f, 359.0f, 0f);
    static readonly Vector3 ParkedCarPosition = new Vector3(1.27527f, 0.2920411f, 8.635817f);
    static readonly Vector3 ParkedCarScale = new Vector3(0.12f, 0.12f, 0.12f);
    static readonly Vector3 StoreFacadeLookTarget = new Vector3(0f, 2f, 7.05f);
    static readonly Vector3 StoreInteriorLookTarget = new Vector3(-0.94f, 1.75f, 11.8f);
    static readonly string[] TextureRoots =
    {
        "Assets/ModelsPlace/6twelve/source/6twelve/Textures",
        "Assets/ModelsPlace/6twelve/textures"
    };

    [MenuItem("OPEN FEED/Scripts/6twelve Scene - Clear")]
    static void ClearScene()
    {
        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
            DestroyImmediate(existing);
    }

    [MenuItem("OPEN FEED/Scripts/6twelve Scene")]
    static void GenerateScene()
    {
        CarPlacement carPlacement = CaptureExistingCarPlacement();
        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
            DestroyImmediate(existing);

        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
                DestroyImmediate(light.gameObject);
        }

        GameObject root = new GameObject(RootName);
        EnsureSceneBootstrap();

        Material asphalt = CreateTextured("SixTwelve_Asphalt", "Asphalt_01", new Color(0.18f, 0.18f, 0.19f), new Vector2(4f, 4f));
        Material roadLine = CreateMatte("SixTwelve_RoadLine", new Color(0.78f, 0.78f, 0.62f));
        Material sidewalk = CreateTextured("SixTwelve_Sidewalk", "Concrete_01", new Color(0.52f, 0.52f, 0.5f), new Vector2(3f, 1f));
        Material curb = CreateTextured("SixTwelve_Curb", "Concrete", new Color(0.42f, 0.42f, 0.4f), new Vector2(1f, 1f));
        Material grass = CreateTextured("SixTwelve_Grass", "Grass_02", new Color(0.24f, 0.3f, 0.22f), new Vector2(5f, 5f));
        Material sign = CreateTextured("SixTwelve_Sign", "Sign_01", Color.white, Vector2.one);
        Material emissiveWindow = CreateEmissiveTextured("SixTwelve_WindowGlow", "Lights_01", Color.white, new Color(1f, 0.92f, 0.78f), 0.6f, Vector2.one);
        Material metal = CreateTextured("SixTwelve_Metal", "Metal", new Color(0.66f, 0.68f, 0.7f), new Vector2(1f, 1f));
        Material carBody = CreateTextured("SixTwelve_CarBody", "Metal_01", new Color(0.18f, 0.2f, 0.24f), new Vector2(1f, 1f));
        Material carTrim = CreateTextured("SixTwelve_CarTrim", "Metal", new Color(0.34f, 0.36f, 0.38f), new Vector2(1f, 1f));
        Material carGlass = CreateTransparent("SixTwelve_CarGlass", new Color(0.7f, 0.84f, 0.95f, 0.22f), 0.55f, 0f);
        Material tire = CreateTextured("SixTwelve_Tire", "Wheel", new Color(0.14f, 0.14f, 0.15f), new Vector2(1f, 1f));
        Material headlightMat = CreateEmissiveTextured("SixTwelve_Headlight", "Lights_01", Color.white, new Color(1f, 0.95f, 0.82f), 0.45f, Vector2.one);
        Material taillightMat = CreateEmissiveTextured("SixTwelve_Taillight", "Lights_01", new Color(0.9f, 0.4f, 0.35f), new Color(1f, 0.18f, 0.12f), 0.35f, Vector2.one);

        GameObject ground = new GameObject("Ground");
        ground.transform.parent = root.transform;
        CreatePrim("ParkingLot", PrimitiveType.Cube, ground.transform, new Vector3(0f, -0.05f, 2f), Vector3.zero, new Vector3(28f, 0.1f, 22f), asphalt, true);
        CreatePrim("Road", PrimitiveType.Cube, ground.transform, new Vector3(0f, -0.04f, 14f), Vector3.zero, new Vector3(30f, 0.08f, 4f), asphalt, true);
        CreatePrim("Sidewalk", PrimitiveType.Cube, ground.transform, new Vector3(0f, 0.03f, 8.2f), Vector3.zero, new Vector3(15f, 0.12f, 2.4f), sidewalk, true);
        CreatePrim("Curb", PrimitiveType.Cube, ground.transform, new Vector3(0f, 0.08f, 7.05f), Vector3.zero, new Vector3(15f, 0.12f, 0.2f), curb, true);
        CreatePrim("GrassLeft", PrimitiveType.Cube, ground.transform, new Vector3(-15f, -0.06f, 3f), Vector3.zero, new Vector3(6f, 0.08f, 26f), grass, true);
        CreatePrim("GrassRight", PrimitiveType.Cube, ground.transform, new Vector3(15f, -0.06f, 3f), Vector3.zero, new Vector3(6f, 0.08f, 26f), grass, true);
        CreatePrim("GrassBack", PrimitiveType.Cube, ground.transform, new Vector3(0f, -0.06f, -10f), Vector3.zero, new Vector3(34f, 0.08f, 6f), grass, true);

        for (int i = 0; i < 8; i++)
        {
            CreatePrim($"RoadDash{i}", PrimitiveType.Cube, ground.transform,
                new Vector3(-13f + i * 3.7f, 0.005f, 14f), Vector3.zero, new Vector3(1.6f, 0.01f, 0.08f), roadLine, true);
        }

        GameObject carAnchor = new GameObject("ParkedCar");
        carAnchor.transform.parent = root.transform;
        carAnchor.transform.localPosition = carPlacement.position;
        carAnchor.transform.localRotation = carPlacement.rotation;
        carAnchor.transform.localScale = carPlacement.scale;
        SpawnParkingLotCar(carAnchor.transform, carBody, carTrim, carGlass, tire, headlightMat, taillightMat);

        GameObject storeParent = new GameObject("Store");
        storeParent.transform.parent = root.transform;

        GameObject storePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (storePrefab != null)
        {
            GameObject store = (GameObject)PrefabUtility.InstantiatePrefab(storePrefab);
            store.name = "6twelveModel";
            store.transform.parent = storeParent.transform;
            store.transform.localPosition = new Vector3(0f, 0f, 8.55f);
            store.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            store.transform.localScale = Vector3.one * 0.15f;
            StripImportedSceneComponents(store);
            ApplySixTwelveMaterials(store);
            AddStaticMeshColliders(store);
            AddInteriorStoreLights(store.transform);
        }
        else
        {
            CreatePrim("FallbackStore", PrimitiveType.Cube, storeParent.transform,
                new Vector3(0f, 2f, 8.5f), Vector3.zero, new Vector3(12f, 4f, 4f), sidewalk);
        }

        CreatePrim("OpenSign", PrimitiveType.Quad, root.transform,
            new Vector3(2.4f, 2.75f, 6.95f), new Vector3(0f, 180f, 0f), new Vector3(1.4f, 0.45f, 1f), sign);
        CreatePrim("WindowGlow", PrimitiveType.Quad, root.transform,
            new Vector3(0f, 2f, 7.05f), new Vector3(0f, 180f, 0f), new Vector3(5f, 1.4f, 1f), emissiveWindow);

        for (int i = 0; i < 4; i++)
        {
            float x = -5.5f + i * 3.6f;
            CreatePrim($"Bollard_{i}", PrimitiveType.Cylinder, root.transform,
                new Vector3(x, 0.55f, 7.2f), Vector3.zero, new Vector3(0.12f, 0.55f, 0.12f), metal);
        }

        CreateCollisionBox("ExteriorWalkFloor", root.transform, new Vector3(0f, 0.08f, 8.9f), new Vector3(8f, 0.16f, 3.2f));
        CreateCollisionBox("InteriorWalkFloor", root.transform, new Vector3(0f, 0.02f, 10.9f), new Vector3(14f, 0.16f, 8f));
        CreateCollisionBox("BackInteriorWalkFloor", root.transform, new Vector3(0f, 0.02f, 14.5f), new Vector3(12f, 0.16f, 6f));

        AddLight(root.transform, "StoreGlow", new Vector3(0f, 2.6f, 8f), LightType.Point, new Color(1f, 0.9f, 0.78f), 2.2f, 8f, 0f);
        AddLight(root.transform, "DoorGlow", new Vector3(0f, 1.5f, 6.8f), LightType.Spot, new Color(1f, 0.92f, 0.8f), 3.4f, 8f, 75f, new Vector3(35f, 0f, 0f));
        AddLight(root.transform, "StreetLampLeft", new Vector3(-6f, 4.6f, 5.5f), LightType.Point, new Color(1f, 0.82f, 0.58f), 2.2f, 12f, 0f);
        AddLight(root.transform, "StreetLampRight", new Vector3(6f, 4.6f, 5.5f), LightType.Point, new Color(1f, 0.82f, 0.58f), 2.2f, 12f, 0f);

        GameObject moon = new GameObject("Moonlight");
        moon.transform.parent = root.transform;
        moon.transform.localRotation = Quaternion.Euler(42f, -60f, 0f);
        Light moonLight = moon.AddComponent<Light>();
        moonLight.type = LightType.Directional;
        moonLight.color = new Color(0.26f, 0.3f, 0.4f);
        moonLight.intensity = 0.18f;
        moonLight.shadows = LightShadows.Soft;

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
        playerRig.transform.parent = root.transform;
        CharacterController characterController = playerRig.GetComponent<CharacterController>();
        if (characterController == null)
            characterController = playerRig.AddComponent<CharacterController>();
        characterController.height = 1.8f;
        characterController.radius = 0.28f;
        characterController.center = new Vector3(0f, 0.9f, 0f);

        GameObject cameraPivot = new GameObject("CameraPivot");
        cameraPivot.transform.parent = playerRig.transform;
        cameraPivot.transform.localPosition = new Vector3(0f, 1.62f, 0f);

        camObj.transform.parent = cameraPivot.transform;
        camObj.transform.localPosition = Vector3.zero;
        camObj.transform.localRotation = Quaternion.identity;
        cam.fieldOfView = 55f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 90f;
        cam.backgroundColor = new Color(0.03f, 0.04f, 0.08f);
        cam.clearFlags = CameraClearFlags.SolidColor;

        StoreFirstPersonController fp = playerRig.GetComponent<StoreFirstPersonController>();
        if (fp == null)
            fp = playerRig.AddComponent<StoreFirstPersonController>();
        fp.cameraPivot = cameraPivot.transform;
        fp.moveSpeed = 3.2f;
        fp.sprintSpeed = 4.8f;
        fp.mouseSensitivity = 2f;

        GameObject introMarkers = new GameObject("IntroMarkers");
        introMarkers.transform.parent = root.transform;
        Transform menuView = CreateMarker(introMarkers.transform, "MenuView", MenuViewPosition, MenuViewRotation);
        Vector3 carSeatPosition = carAnchor.transform.TransformPoint(new Vector3(-0.36f, 0.92f, 0.18f));
        Quaternion carSeatRotation = Quaternion.LookRotation((StoreFacadeLookTarget - carSeatPosition).normalized, Vector3.up);
        Transform carSeatView = CreateMarker(introMarkers.transform, "CarSeatView", carSeatPosition, carSeatRotation.eulerAngles);
        Vector3 carExitPosition = carAnchor.transform.TransformPoint(new Vector3(-1.08f, 1.04f, 0.12f));
        Quaternion carExitRotation = Quaternion.LookRotation((StoreFacadeLookTarget - carExitPosition).normalized, Vector3.up);
        Transform carExitView = CreateMarker(introMarkers.transform, "CarExitView", carExitPosition, carExitRotation.eulerAngles);
        Transform storeEntranceView = CreateMarker(introMarkers.transform, "StoreEntranceView",
            new Vector3(0f, 1.62f, 10.4f), Vector3.zero);

        SixTwelveIntroController intro = root.GetComponent<SixTwelveIntroController>();
        if (intro == null)
            intro = root.AddComponent<SixTwelveIntroController>();
        intro.playerController = fp;
        intro.playerCamera = cam;
        intro.menuView = menuView;
        intro.carSeatView = carSeatView;
        intro.carExitView = carExitView;
        intro.storeEntranceView = storeEntranceView;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.06f, 0.07f, 0.1f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.018f;
        RenderSettings.fogColor = new Color(0.03f, 0.04f, 0.07f);

        Selection.activeGameObject = root;
        Debug.Log("OPENFEED 6twelve scene generated.");
    }

    static void EnsureSceneBootstrap()
    {
        if (FindAnyObjectByType<GameFlowManager>() == null)
        {
            GameObject go = new GameObject("GameFlowManager");
            go.AddComponent<GameFlowManager>();
        }

        if (FindAnyObjectByType<MainMenuUI>() == null)
        {
            GameObject go = new GameObject("MainMenuUI");
            go.AddComponent<MainMenuUI>();
        }
    }

    static CarPlacement CaptureExistingCarPlacement()
    {
        CarPlacement placement = new CarPlacement
        {
            position = ParkedCarPosition,
            scale = ParkedCarScale
        };

        Vector3 defaultForward = StoreFacadeLookTarget - ParkedCarPosition;
        defaultForward.y = 0f;
        if (defaultForward.sqrMagnitude < 0.001f)
            defaultForward = Vector3.forward;
        placement.rotation = Quaternion.LookRotation(defaultForward.normalized, Vector3.up);

        GameObject existingCar = GameObject.Find("ParkedCar");
        if (existingCar != null)
        {
            placement.position = existingCar.transform.localPosition;
            placement.rotation = existingCar.transform.localRotation;
            placement.scale = existingCar.transform.localScale;
        }

        return placement;
    }

    static void SpawnParkingLotCar(Transform parent, Material bodyMat, Material trimMat, Material glassMat,
        Material tireMat, Material headlightMat, Material taillightMat)
    {
        GameObject car = new GameObject("Car");
        car.transform.SetParent(parent, false);
        car.transform.localPosition = Vector3.zero;
        car.transform.localRotation = Quaternion.identity;
        car.transform.localScale = Vector3.one;

        CreatePrim("BodyLower", PrimitiveType.Cube, car.transform, new Vector3(0f, 0.42f, 0f), Vector3.zero,
            new Vector3(1.86f, 0.52f, 4.18f), bodyMat);
        CreatePrim("BodyUpper", PrimitiveType.Cube, car.transform, new Vector3(0f, 0.92f, -0.12f), Vector3.zero,
            new Vector3(1.56f, 0.56f, 2.18f), bodyMat);
        CreatePrim("Hood", PrimitiveType.Cube, car.transform, new Vector3(0f, 0.77f, 1.22f), new Vector3(8f, 0f, 0f),
            new Vector3(1.52f, 0.18f, 1.36f), bodyMat);
        CreatePrim("Trunk", PrimitiveType.Cube, car.transform, new Vector3(0f, 0.78f, -1.58f), new Vector3(-5f, 0f, 0f),
            new Vector3(1.46f, 0.2f, 0.9f), bodyMat);
        CreatePrim("FrontBumper", PrimitiveType.Cube, car.transform, new Vector3(0f, 0.3f, 2.08f), Vector3.zero,
            new Vector3(1.72f, 0.16f, 0.16f), trimMat);
        CreatePrim("RearBumper", PrimitiveType.Cube, car.transform, new Vector3(0f, 0.3f, -2.08f), Vector3.zero,
            new Vector3(1.72f, 0.16f, 0.16f), trimMat);
        CreatePrim("FrontWindshield", PrimitiveType.Quad, car.transform, new Vector3(0f, 1.08f, 0.72f),
            new Vector3(-28f, 180f, 0f), new Vector3(1.2f, 0.58f, 1f), glassMat);
        CreatePrim("RearWindshield", PrimitiveType.Quad, car.transform, new Vector3(0f, 1.04f, -0.92f),
            new Vector3(24f, 0f, 0f), new Vector3(1.08f, 0.48f, 1f), glassMat);
        CreatePrim("WindowLeft", PrimitiveType.Quad, car.transform, new Vector3(-0.79f, 0.98f, -0.08f),
            new Vector3(0f, 90f, 0f), new Vector3(1.52f, 0.46f, 1f), glassMat);
        CreatePrim("WindowRight", PrimitiveType.Quad, car.transform, new Vector3(0.79f, 0.98f, -0.08f),
            new Vector3(0f, -90f, 0f), new Vector3(1.52f, 0.46f, 1f), glassMat);
        CreatePrim("Roof", PrimitiveType.Cube, car.transform, new Vector3(0f, 1.25f, -0.1f), Vector3.zero,
            new Vector3(1.22f, 0.08f, 1.38f), trimMat);
        CreatePrim("LightFrontL", PrimitiveType.Quad, car.transform, new Vector3(-0.44f, 0.5f, 2.17f),
            new Vector3(0f, 180f, 0f), new Vector3(0.3f, 0.14f, 1f), headlightMat);
        CreatePrim("LightFrontR", PrimitiveType.Quad, car.transform, new Vector3(0.44f, 0.5f, 2.17f),
            new Vector3(0f, 180f, 0f), new Vector3(0.3f, 0.14f, 1f), headlightMat);
        CreatePrim("LightRearL", PrimitiveType.Quad, car.transform, new Vector3(-0.44f, 0.48f, -2.17f),
            Vector3.zero, new Vector3(0.28f, 0.14f, 1f), taillightMat);
        CreatePrim("LightRearR", PrimitiveType.Quad, car.transform, new Vector3(0.44f, 0.48f, -2.17f),
            Vector3.zero, new Vector3(0.28f, 0.14f, 1f), taillightMat);

        Vector3[] wheelPositions =
        {
            new Vector3(-0.82f, 0.28f, 1.3f),
            new Vector3(0.82f, 0.28f, 1.3f),
            new Vector3(-0.82f, 0.28f, -1.28f),
            new Vector3(0.82f, 0.28f, -1.28f)
        };
        for (int i = 0; i < wheelPositions.Length; i++)
        {
            CreatePrim($"Wheel_{i}", PrimitiveType.Cylinder, car.transform, wheelPositions[i],
                new Vector3(0f, 0f, 90f), new Vector3(0.32f, 0.14f, 0.32f), tireMat);
        }

        AddLight(car.transform, "HeadlightGlowL", new Vector3(-0.42f, 0.44f, 2.28f), LightType.Point,
            new Color(1f, 0.95f, 0.84f), 0.45f, 2.2f, 0f);
        AddLight(car.transform, "HeadlightGlowR", new Vector3(0.42f, 0.44f, 2.28f), LightType.Point,
            new Color(1f, 0.95f, 0.84f), 0.45f, 2.2f, 0f);
    }

    static Transform CreateMarker(Transform parent, string name, Vector3 pos, Vector3 rot)
    {
        GameObject go = new GameObject(name);
        go.transform.parent = parent;
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(rot);
        return go.transform;
    }

    static void ApplySixTwelveMaterials(GameObject target)
    {
        Dictionary<string, Texture2D> textures = LoadTextureLookup();
        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
        {
            Material source = renderer.sharedMaterial;
            string sourceName = source != null ? SanitizeName(source.name) : SanitizeName(renderer.gameObject.name);

            Material mat = new Material(FindShader());
            mat.name = "SixTwelve_" + sourceName;
            mat.SetFloat("_Smoothness", 0f);
            mat.SetFloat("_Metallic", 0f);

            Texture2D tex = FindBestTexture(textures, sourceName);
            if (tex != null)
                ApplyTexture(mat, tex, Vector2.one);
            else if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", new Color(0.72f, 0.72f, 0.72f));

            if (sourceName.Contains("light") || sourceName.Contains("emit") || sourceName.Contains("sign"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.65f, 0.52f, 0.32f) * 1.2f);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            renderer.sharedMaterial = mat;
        }
    }

    static void StripImportedSceneComponents(GameObject target)
    {
        foreach (Camera camera in target.GetComponentsInChildren<Camera>(true))
            DestroyImmediate(camera.gameObject);

        foreach (AudioListener listener in target.GetComponentsInChildren<AudioListener>(true))
            DestroyImmediate(listener);

        foreach (Light light in target.GetComponentsInChildren<Light>(true))
            DestroyImmediate(light.gameObject);
    }

    static void AddInteriorStoreLights(Transform storeRoot)
    {
        int lightIndex = 0;
        foreach (Renderer renderer in storeRoot.GetComponentsInChildren<Renderer>(true))
        {
            string nodeName = SanitizeName(renderer.gameObject.name);
            if (!nodeName.Contains("lights"))
                continue;

            Bounds bounds = renderer.bounds;
            Vector3 center = bounds.center;
            Vector3 size = bounds.size;

            GameObject lightObj = new GameObject($"InteriorLight_{lightIndex}");
            lightObj.transform.parent = storeRoot;
            lightObj.transform.position = center + new Vector3(0f, -0.18f, 0f);

            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 0.45f;
            light.range = Mathf.Clamp(Mathf.Max(size.x, size.z) * 1.15f, 1.8f, 3.2f);
            light.shadows = LightShadows.None;

            lightIndex++;
        }
    }

    static void AddStaticMeshColliders(GameObject target)
    {
        foreach (MeshFilter meshFilter in target.GetComponentsInChildren<MeshFilter>(true))
        {
            if (meshFilter.sharedMesh == null)
                continue;

            MeshCollider existing = meshFilter.GetComponent<MeshCollider>();
            if (existing == null)
                existing = meshFilter.gameObject.AddComponent<MeshCollider>();

            existing.sharedMesh = meshFilter.sharedMesh;
            existing.convex = false;
        }
    }

    static void CreateCollisionBox(string name, Transform parent, Vector3 pos, Vector3 scale)
    {
        GameObject obj = CreatePrim(name, PrimitiveType.Cube, parent, pos, Vector3.zero, scale, null, true);
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;
    }

    static void SetAllMaterialsMatte(GameObject obj)
    {
        foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null)
                    continue;
                if (material.HasProperty("_Smoothness"))
                    material.SetFloat("_Smoothness", 0f);
                if (material.HasProperty("_Metallic"))
                    material.SetFloat("_Metallic", 0f);
            }
        }
    }

    static Dictionary<string, Texture2D> LoadTextureLookup()
    {
        Dictionary<string, Texture2D> lookup = new Dictionary<string, Texture2D>();
        foreach (string root in TextureRoots)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { root });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null)
                    continue;

                string key = SanitizeName(Path.GetFileNameWithoutExtension(path));
                if (!lookup.ContainsKey(key))
                    lookup.Add(key, tex);
            }
        }
        return lookup;
    }

    static Texture2D FindBestTexture(Dictionary<string, Texture2D> lookup, string materialName)
    {
        if (lookup.TryGetValue(materialName, out Texture2D direct))
            return direct;

        foreach (KeyValuePair<string, Texture2D> kv in lookup)
        {
            if (materialName.Contains(kv.Key) || kv.Key.Contains(materialName))
                return kv.Value;
        }

        return null;
    }

    static string SanitizeName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        value = value.ToLowerInvariant();
        value = value.Replace(" (instance)", "");
        value = value.Replace("_mat", "");
        value = value.Replace("material", "");
        value = value.Replace(" ", "");
        value = value.Replace("-", "");
        value = value.Replace("_", "");
        return value;
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

    static Material CreateTextured(string name, string textureKey, Color tint, Vector2 tiling)
    {
        Material mat = CreateMatte(name, tint);
        Texture2D tex = FindBestTexture(LoadTextureLookup(), SanitizeName(textureKey));
        if (tex != null)
            ApplyTexture(mat, tex, tiling);
        return mat;
    }

    static Material CreateEmissiveTextured(string name, string textureKey, Color tint, Color emission, float intensity, Vector2 tiling)
    {
        Material mat = CreateTextured(name, textureKey, tint, tiling);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emission * intensity);
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

    static void ApplyTexture(Material mat, Texture2D tex, Vector2 tiling)
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

    static GameObject CreatePrim(string name, PrimitiveType type, Transform parent, Vector3 pos, Vector3 rot, Vector3 scale, Material mat, bool keepCollider = false)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.parent = parent;
        obj.transform.localPosition = pos;
        obj.transform.localRotation = Quaternion.Euler(rot);
        obj.transform.localScale = scale;
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && mat != null)
            renderer.sharedMaterial = mat;
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null && !keepCollider)
            DestroyImmediate(collider);
        return obj;
    }

    static void AddLight(Transform parent, string name, Vector3 pos, LightType type, Color color, float intensity, float range, float spotAngle, Vector3? rot = null)
    {
        GameObject go = new GameObject(name);
        go.transform.parent = parent;
        go.transform.localPosition = pos;
        if (rot.HasValue)
            go.transform.localRotation = Quaternion.Euler(rot.Value);
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
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        return shader;
    }
}
