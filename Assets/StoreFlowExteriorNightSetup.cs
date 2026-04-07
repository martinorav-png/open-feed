using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Night skybox + ambient/fog for GroceryStore (matches StoreFlowSceneGenerator.ApplyStoreFlowNightRenderSettings).
/// </summary>
public static class StoreFlowExteriorNightSetup
{
    static Material s_NightSkyMaterial;

    public static void ApplyIfGroceryStore(Transform sceneRoot)
    {
        if (sceneRoot == null)
            return;
        if (sceneRoot.gameObject.scene.name != "GroceryStore")
            return;

        ApplyNightRenderSettingsShared();
    }

    /// <summary>Fog, flat ambient, reflection — same tuning as the store generator.</summary>
    public static void ApplyNightRenderSettingsShared()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.08f, 0.09f, 0.12f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.012f;
        RenderSettings.fogColor = new Color(0.04f, 0.05f, 0.08f);
        RenderSettings.reflectionIntensity = 0.7f;

        EnsureSkyMaterial();
        if (s_NightSkyMaterial != null)
            RenderSettings.skybox = s_NightSkyMaterial;
    }

    static void EnsureSkyMaterial()
    {
        if (s_NightSkyMaterial != null)
            return;

        Shader procedural = Shader.Find("Skybox/Procedural");
        if (procedural == null)
            procedural = Shader.Find("Skybox/Gradient");
        if (procedural == null)
            return;

        s_NightSkyMaterial = new Material(procedural);
        s_NightSkyMaterial.name = "StoreFlow_RuntimeNightSky";

        if (s_NightSkyMaterial.HasProperty("_SkyTint"))
            s_NightSkyMaterial.SetColor("_SkyTint", new Color(0.1f, 0.12f, 0.26f, 1f));
        if (s_NightSkyMaterial.HasProperty("_GroundColor"))
            s_NightSkyMaterial.SetColor("_GroundColor", new Color(0.02f, 0.025f, 0.045f, 1f));
        if (s_NightSkyMaterial.HasProperty("_AtmosphereThickness"))
            s_NightSkyMaterial.SetFloat("_AtmosphereThickness", 0.92f);
        if (s_NightSkyMaterial.HasProperty("_Exposure"))
            s_NightSkyMaterial.SetFloat("_Exposure", 0.42f);
        if (s_NightSkyMaterial.HasProperty("_SunDisk"))
            s_NightSkyMaterial.SetFloat("_SunDisk", 0f);

        // Skybox/Gradient
        if (s_NightSkyMaterial.HasProperty("_Color") && !s_NightSkyMaterial.HasProperty("_SkyTint"))
            s_NightSkyMaterial.SetColor("_Color", new Color(0.04f, 0.05f, 0.12f, 1f));
        if (s_NightSkyMaterial.HasProperty("_Color2"))
            s_NightSkyMaterial.SetColor("_Color2", new Color(0.02f, 0.03f, 0.08f, 1f));
        if (s_NightSkyMaterial.HasProperty("_Color3"))
            s_NightSkyMaterial.SetColor("_Color3", new Color(0.01f, 0.015f, 0.03f, 1f));
    }
}
