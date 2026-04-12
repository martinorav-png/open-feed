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
        string sn = sceneRoot.gameObject.scene.name;
        if (sn != "GroceryStore" && sn != "supermarket")
            return;

        ApplyNightRenderSettingsShared();
    }

    /// <summary>
    /// Parking-lot exterior: linear fog pulls the horizon into a dark veil (URP still uses RenderSettings fog).
    /// Skybox procedural exposure is reduced so the rim does not blow out.
    /// </summary>
    public static void ApplyNightRenderSettingsShared()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.055f, 0.075f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.01f, 0.012f, 0.02f, 1f);
        RenderSettings.fogStartDistance = 8f;
        RenderSettings.fogEndDistance = 95f;
        RenderSettings.fogDensity = 0.02f;
        RenderSettings.reflectionIntensity = 0.35f;

        EnsureSkyMaterial();
        if (s_NightSkyMaterial != null)
            RenderSettings.skybox = s_NightSkyMaterial;
    }

    /// <summary>Call when the player enters the shopping floor — exterior fog reads as haze indoors.</summary>
    public static void ApplyInteriorShoppingRenderSettings()
    {
        RenderSettings.fog = false;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.14f, 0.14f, 0.15f);
        RenderSettings.reflectionIntensity = 0.5f;
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
            s_NightSkyMaterial.SetColor("_SkyTint", new Color(0.05f, 0.06f, 0.14f, 1f));
        if (s_NightSkyMaterial.HasProperty("_GroundColor"))
            s_NightSkyMaterial.SetColor("_GroundColor", new Color(0.01f, 0.012f, 0.022f, 1f));
        if (s_NightSkyMaterial.HasProperty("_AtmosphereThickness"))
            s_NightSkyMaterial.SetFloat("_AtmosphereThickness", 0.62f);
        if (s_NightSkyMaterial.HasProperty("_Exposure"))
            s_NightSkyMaterial.SetFloat("_Exposure", 0.28f);
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
