using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Night skybox + ambient for supermarket / GroceryStore. Does <b>not</b> change <see cref="RenderSettings.fog"/> on the
/// exterior — keep fog as authored in the scene / Lighting window so play mode does not wipe it.
/// </summary>
public static class StoreFlowExteriorNightSetup
{
    static Material s_NightSkyMaterial;

    public static void ApplyIfGroceryStore(Transform sceneRoot)
    {
        if (sceneRoot == null)
            return;
        string sn = sceneRoot.gameObject.scene.name;
        if (sn != "GroceryStore" && sn != "supermarket")
            return;

        ApplyNightRenderSettingsShared();
        StoreInteriorLightMood.ApplyLuminaireCoolTint();
    }

    /// <summary>
    /// Applies sky/ambient and strip-light tint when the <b>active</b> scene is supermarket or GroceryStore.
    /// Use this after a scene load so play mode gets settings even if <see cref="SupermarketDriveInIntro"/> is still inactive.
    /// </summary>
    public static void ApplyActiveStoreExteriorIfMatch()
    {
        Scene s = SceneManager.GetActiveScene();
        if (!s.IsValid() || !s.isLoaded)
            return;
        string n = s.name;
        if (n != "GroceryStore" && n != "supermarket")
            return;

        ApplyNightRenderSettingsShared();
        StoreInteriorLightMood.ApplyLuminaireCoolTint();
    }

    /// <summary>
    /// Exterior: ambient + reflection + runtime night skybox; removes legacy billboard fog particles only.
    /// Scene <see cref="RenderSettings.fog"/> is left unchanged.
    /// </summary>
    public static void ApplyNightRenderSettingsShared()
    {
        StoreAtmosphereFogParticles.RemoveFromScene();
        DestroyRuntimeFogVolumeIfPresent();

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.032f, 0.038f, 0.055f);
        RenderSettings.reflectionIntensity = 0.26f;

        EnsureSkyMaterial();
        if (s_NightSkyMaterial != null)
            RenderSettings.skybox = s_NightSkyMaterial;
    }

    /// <summary>Shopping floor: turn off distance fog so the interior does not read as exterior haze.</summary>
    public static void ApplyInteriorShoppingRenderSettings()
    {
        RenderSettings.fog = false;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.075f, 0.082f, 0.105f);
        RenderSettings.reflectionIntensity = 0.34f;
    }

    static void EnsureSkyMaterial()
    {
        if (s_NightSkyMaterial != null)
            return;

        Material extendedTemplate = Resources.Load<Material>("OpenFeed/StoreFlowSkyboxExtendedNight");
        if (extendedTemplate != null)
        {
            s_NightSkyMaterial = new Material(extendedTemplate);
            s_NightSkyMaterial.name = "StoreFlow_RuntimeNightSkyboxExtended";
            return;
        }

        Shader extendedShader = Shader.Find("Skybox/Cubemap Extended");
        if (extendedShader != null)
        {
            Cubemap cube = Resources.Load<Cubemap>("OpenFeed/StoreFlowNightSkyCubemap");
            if (cube != null)
            {
                s_NightSkyMaterial = new Material(extendedShader) { name = "StoreFlow_RuntimeNightSkyboxExtended" };
                s_NightSkyMaterial.SetTexture("_Tex", cube);
                s_NightSkyMaterial.SetVector("_Tex_HDR", new Vector4(1f, 1f, 0f, 0f));
                return;
            }
        }

        Shader procedural = Shader.Find("Skybox/Procedural");
        if (procedural == null)
            procedural = Shader.Find("Skybox/Gradient");
        if (procedural == null)
            return;

        s_NightSkyMaterial = new Material(procedural);
        s_NightSkyMaterial.name = "StoreFlow_RuntimeNightSky";

        if (s_NightSkyMaterial.HasProperty("_SkyTint"))
            s_NightSkyMaterial.SetColor("_SkyTint", new Color(0.05f, 0.06f, 0.14f, 1f));
        if (s_NightSkyMaterial.HasProperty("_GroundColor"))
            s_NightSkyMaterial.SetColor("_GroundColor", new Color(0.01f, 0.012f, 0.022f, 1f));
        if (s_NightSkyMaterial.HasProperty("_AtmosphereThickness"))
            s_NightSkyMaterial.SetFloat("_AtmosphereThickness", 0.62f);
        if (s_NightSkyMaterial.HasProperty("_Exposure"))
            s_NightSkyMaterial.SetFloat("_Exposure", 0.2f);
        if (s_NightSkyMaterial.HasProperty("_SunDisk"))
            s_NightSkyMaterial.SetFloat("_SunDisk", 0f);

        if (s_NightSkyMaterial.HasProperty("_Color") && !s_NightSkyMaterial.HasProperty("_SkyTint"))
            s_NightSkyMaterial.SetColor("_Color", new Color(0.04f, 0.05f, 0.12f, 1f));
        if (s_NightSkyMaterial.HasProperty("_Color2"))
            s_NightSkyMaterial.SetColor("_Color2", new Color(0.02f, 0.03f, 0.08f, 1f));
        if (s_NightSkyMaterial.HasProperty("_Color3"))
            s_NightSkyMaterial.SetColor("_Color3", new Color(0.01f, 0.015f, 0.03f, 1f));
    }

    const string RuntimeFogVolumeName = "StorePostProcessFogVolume";

    static void DestroyRuntimeFogVolumeIfPresent()
    {
        GameObject go = GameObject.Find(RuntimeFogVolumeName);
        if (go == null)
            return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Object.DestroyImmediate(go);
            return;
        }
#endif
        Object.Destroy(go);
    }
}
