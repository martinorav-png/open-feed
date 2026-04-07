#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Regenerates the supermarket scene from scratch so it can be rebuilt after any corruption.
/// Menu: OPEN FEED → Supermarket → Regenerate Scene
///
/// Steps performed:
///   1. Clears every GameObject in the active scene.
///   2. Opens GroceryStore.unity additively and copies exterior objects (terrain,
///      forest, highway, street lamps, parked car, snowfall, moonlight).
///   3. Instantiates the supermarket FBX prefab and applies baseColor textures.
///   4. Adds interior point lights on the four ceiling fixture meshes.
///   5. Creates a Main Camera (URP) and a disabled Directional Light.
///   6. Creates MainMenuUI + GameFlowManager bootstrap objects.
///   7. Applies deep-dark nighttime RenderSettings.
///
/// After the script runs, save the scene (Ctrl+S) and reimport if Unity shows errors.
/// </summary>
public static class SupermarketSceneGenerator
{
    // ── Asset paths / GUIDs ────────────────────────────────────────────────────
    const string GroceryScenePath   = "Assets/Scenes/GroceryStore.unity";
    const string SupermarketFbxGuid = "91ba6f803321fc047add62af96400af1";
    const string GroceryStoreRoot   = "StoreFlowScene";

    static readonly string[] ExteriorNames =
    {
        "WildernessTerrain",
        "WildernessSkirt",
        "ForestHighway",
        "Ground",
        "StreetProps",
        "ParkedCar",
        "SnowfallRuntime",
        "StreetLampLeft",
        "StreetLampRight",
        "Moonlight",
    };

    // ── Menu item ──────────────────────────────────────────────────────────────

    [MenuItem("OPEN FEED/Supermarket/Regenerate Scene", false, 1)]
    static void RegenerateScene()
    {
        Scene target = SceneManager.GetActiveScene();
        if (!target.IsValid())
        {
            EditorUtility.DisplayDialog("Supermarket Generator", "No active scene open.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
            "Regenerate Supermarket Scene",
            $"This will DELETE all objects in \"{target.name}\" and rebuild from scratch.\n\nProceed?",
            "Regenerate", "Cancel"))
            return;

        // 1 — wipe
        ClearScene(target);

        // 2 — exterior from GroceryStore
        CopyExterior(target);

        // 3 — supermarket building
        GameObject storeRoot = InstantiateSupermarketFbx();
        if (storeRoot != null)
            SupermarketFbxTextureAssigner.ApplyTo(storeRoot);

        // 4 — interior ceiling lights
        SupermarketStripLightsSetup.ApplyToScene(out StringBuilder _);

        // 5 — camera + sky light
        CreateCamera();
        CreateDirectionalLight();

        // 6 — UI / music
        EnsureBootstrap();

        // 7 — dark night
        ApplyNighttime();

        EditorSceneManager.MarkSceneDirty(target);
        Debug.Log($"[Supermarket] Scene regenerated in \"{target.name}\". Press Ctrl+S to save.");
    }

    // ── 1: Clear ───────────────────────────────────────────────────────────────

    static void ClearScene(Scene scene)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
            Object.DestroyImmediate(go);
    }

    // ── 2: Copy exterior ───────────────────────────────────────────────────────

    static void CopyExterior(Scene target)
    {
        string fullPath = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(Application.dataPath, "..", GroceryScenePath));

        if (!System.IO.File.Exists(fullPath))
        {
            Debug.LogWarning($"[Supermarket] GroceryStore not found at {GroceryScenePath} — exterior skipped.");
            return;
        }

        Scene grocery = EditorSceneManager.OpenScene(GroceryScenePath, OpenSceneMode.Additive);
        try
        {
            // Find "StoreFlowScene" root
            Transform storeFlowRoot = null;
            foreach (GameObject go in grocery.GetRootGameObjects())
            {
                if (go.name == GroceryStoreRoot)
                {
                    storeFlowRoot = go.transform;
                    break;
                }
            }

            if (storeFlowRoot == null)
            {
                Debug.LogWarning($"[Supermarket] \"{GroceryStoreRoot}\" root not found in GroceryStore — exterior skipped.");
                return;
            }

            // Target must be active so Instantiate lands in it
            SceneManager.SetActiveScene(target);

            int copied = 0;
            foreach (string name in ExteriorNames)
            {
                Transform child = storeFlowRoot.Find(name);
                if (child == null)
                {
                    Debug.LogWarning($"[Supermarket] Exterior child \"{name}\" not found in GroceryStore.");
                    continue;
                }

                // Deep-copy into the active (target) scene; strip the "(Clone)" suffix.
                GameObject copy = Object.Instantiate(child.gameObject);
                copy.name = child.gameObject.name;
                copy.transform.SetParent(null, true); // ensure scene root
                copied++;
            }

            Debug.Log($"[Supermarket] Copied {copied}/{ExteriorNames.Length} exterior objects from GroceryStore.");
        }
        finally
        {
            // Remove GroceryStore from hierarchy without saving
            if (grocery.IsValid())
                EditorSceneManager.CloseScene(grocery, true);
        }
    }

    // ── 3: Supermarket FBX ─────────────────────────────────────────────────────

    static GameObject InstantiateSupermarketFbx()
    {
        string path = AssetDatabase.GUIDToAssetPath(SupermarketFbxGuid);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("[Supermarket] Supermarket FBX GUID not found in project.");
            return null;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning($"[Supermarket] Could not load FBX prefab from: {path}");
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (instance != null)
        {
            instance.transform.SetPositionAndRotation(
                new Vector3(-0.89f, -0.02f, 23.45f),
                Quaternion.Euler(-90f, 0f, 0f));
            instance.transform.localScale = new Vector3(113.96f, 113.96f, 113.96f);
        }
        return instance;
    }

    // ── 5: Camera ──────────────────────────────────────────────────────────────

    static void CreateCamera()
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";

        Camera cam = go.AddComponent<Camera>();
        cam.clearFlags    = CameraClearFlags.Skybox;
        cam.fieldOfView   = 60f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane  = 1000f;

        go.AddComponent<AudioListener>();
        go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();

        go.transform.SetPositionAndRotation(new Vector3(0f, 1.7f, 0f), Quaternion.identity);
    }

    // ── 5: Directional light (disabled — only street lamps & store lights) ─────

    static void CreateDirectionalLight()
    {
        GameObject go = new GameObject("Directional Light");
        go.transform.localEulerAngles = new Vector3(50f, -30f, 0f);

        Light l = go.AddComponent<Light>();
        l.type      = LightType.Directional;
        l.color     = new Color(0.4f, 0.5f, 0.7f);
        l.intensity = 0f;

        go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
        go.SetActive(false);
    }

    // ── 6: Bootstrap ───────────────────────────────────────────────────────────

    static void EnsureBootstrap()
    {
        if (Object.FindAnyObjectByType<GameFlowManager>() == null)
        {
            GameFlowManager gfm = new GameObject("GameFlowManager").AddComponent<GameFlowManager>();
            gfm.useStoreIntroWhenAvailable = true;
            gfm.fallbackToStoreCutscene   = false;
        }

        if (Object.FindAnyObjectByType<MainMenuUI>() == null)
            new GameObject("MainMenuUI").AddComponent<MainMenuUI>();
    }

    // ── 7: Nighttime render settings ───────────────────────────────────────────

    static void ApplyNighttime()
    {
        // Fog: dense near-black so distance fades to darkness
        RenderSettings.fog        = true;
        RenderSettings.fogColor   = new Color(0.01f, 0.01f, 0.015f);
        RenderSettings.fogMode    = FogMode.Exponential;
        RenderSettings.fogDensity = 0.06f;

        // Ambient: very dim cool-blue fill so shadowed areas aren't pure black
        RenderSettings.ambientMode      = AmbientMode.Flat;
        RenderSettings.ambientLight     = new Color(0.015f, 0.02f, 0.04f);
        RenderSettings.ambientIntensity = 1f;

        // Faint skybox reflection so surfaces have a subtle sheen
        RenderSettings.reflectionIntensity = 0.05f;
        RenderSettings.sun = null;
    }
}
#endif
