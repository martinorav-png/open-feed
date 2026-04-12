using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Legacy particle “fog” (billboard squares). Disabled — use <see cref="RemoveFromScene"/> from night setup; add real fog in-editor.
/// </summary>
public static class StoreAtmosphereFogParticles
{
    const string RootName = "AtmosphereFogParticles";
    static Material s_FogParticleMaterial;

    /// <summary>Destroys any <c>AtmosphereFogParticles</c> instances (including inactive).</summary>
    public static void RemoveFromScene()
    {
        var found = new List<GameObject>();
        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go != null && go.name == RootName)
                found.Add(go);
        }

        for (int i = 0; i < found.Count; i++)
        {
            GameObject go = found[i];
            if (go == null) continue;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(go);
                continue;
            }
#endif
            Object.Destroy(go);
        }
    }

    /// <summary>Previously spawned billboard fog; kept for reference if you re-enable manually.</summary>
    public static void EnsureInScene()
    {
        GameObject flow = GameObject.Find("StoreFlowScene");
        GameObject go = GameObject.Find(RootName);
        if (go == null)
        {
            go = new GameObject(RootName);
            if (flow != null)
                go.transform.SetParent(flow.transform, false);

            go.transform.position = (flow != null ? flow.transform.position : Vector3.zero) + new Vector3(0f, 6f, 0f);
            go.AddComponent<ParticleSystem>();
        }

        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        if (ps == null)
            ps = go.AddComponent<ParticleSystem>();

        ApplyFogParticleSettings(ps);

        ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = GetOrCreateFogParticleMaterial();
        rend.shadowCastingMode = ShadowCastingMode.Off;
        rend.receiveShadows = false;
        rend.sortingOrder = 4;

        if (!ps.isPlaying)
            ps.Play();
    }

    /// <summary>Small, low-alpha wisps + higher noise frequency — avoids huge billboard “blocks”.</summary>
    static void ApplyFogParticleSettings(ParticleSystem ps)
    {
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(28f, 48f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.015f, 0.18f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 3.4f);
        main.maxParticles = 1600;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.55f, 0.62f, 0.78f, 0.035f),
            new Color(0.45f, 0.52f, 0.68f, 0.065f));

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 38f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(130f, 22f, 130f);

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.42f;
        noise.frequency = 0.14f;
        noise.scrollSpeed = 0.08f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.High;
    }

    static Material GetOrCreateFogParticleMaterial()
    {
        if (s_FogParticleMaterial != null)
            return s_FogParticleMaterial;
        s_FogParticleMaterial = CreateFogParticleMaterial();
        return s_FogParticleMaterial;
    }

    static Material CreateFogParticleMaterial()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (s == null)
            s = Shader.Find("Particles/Standard Unlit");
        if (s == null)
            s = Shader.Find("Legacy Shaders/Particles/Alpha Blended");

        Material m = new Material(s) { name = "StoreFogParticle_Runtime" };
        if (m.HasProperty("_Surface"))
            m.SetFloat("_Surface", 1f);
        if (m.HasProperty("_Blend"))
            m.SetFloat("_Blend", 0f);
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", Color.white);
        if (m.HasProperty("_Color"))
            m.color = Color.white;
        if (m.HasProperty("_SoftParticlesEnabled"))
            m.SetFloat("_SoftParticlesEnabled", 1f);
        if (m.HasProperty("_SoftParticlesNearFadeDistance"))
            m.SetFloat("_SoftParticlesNearFadeDistance", 0f);
        if (m.HasProperty("_SoftParticlesFarFadeDistance"))
            m.SetFloat("_SoftParticlesFarFadeDistance", 3.5f);
        m.renderQueue = (int)RenderQueue.Transparent;
        return m;
    }
}

/// <summary>
/// Sets practical point/spot lights in supermarket / GroceryStore to <see cref="StoreFlowAccentLightColor"/>.
/// Street / pole lights also get <see cref="StoreFlowStreetLightTuning"/> intensity and range.
/// Skips Moonlight, car rigs, and headlight-style helpers.
/// </summary>
public static class StoreInteriorLightMood
{
    public static void ApplyLuminaireCoolTint() => ApplyStoreAccentLightColors();

    public static void ApplyStoreAccentLightColors()
    {
        string sn = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sn != "supermarket" && sn != "GroceryStore")
            return;

        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (l == null)
                continue;
            if (l.type == LightType.Directional)
                continue;
            if (l.type != LightType.Point && l.type != LightType.Spot)
                continue;
            if (ShouldSkipAccentRecolor(l))
                continue;
            l.color = StoreFlowAccentLightColor.Rgb;
            ApplyStreetLightLevelsIfApplicable(l);
        }
    }

    static void ApplyStreetLightLevelsIfApplicable(Light l)
    {
        string n = l.gameObject.name;
        if (n.StartsWith("HwyLampLight", System.StringComparison.Ordinal) && l.type == LightType.Point)
        {
            l.intensity = StoreFlowStreetLightTuning.HighwayPointIntensity;
            l.range = StoreFlowStreetLightTuning.HighwayPointRange;
            return;
        }

        if ((n == "StreetLampLeft" || n == "StreetLampRight") && l.type == LightType.Point)
        {
            l.intensity = StoreFlowStreetLightTuning.FlankPolePointIntensity;
            l.range = StoreFlowStreetLightTuning.FlankPolePointRange;
            return;
        }

        if (n.StartsWith("LampLight_", System.StringComparison.Ordinal) && l.type == LightType.Point)
        {
            l.intensity = StoreFlowStreetLightTuning.StorefrontRowPointIntensity;
            l.range = StoreFlowStreetLightTuning.StorefrontRowPointRange;
            return;
        }

        const string lampLightCompact = "LampLight";
        if (n.Length > lampLightCompact.Length && n.StartsWith(lampLightCompact, System.StringComparison.Ordinal)
            && char.IsDigit(n[lampLightCompact.Length]))
        {
            if (!TryParseTrailingInt(n, lampLightCompact.Length, out int lampIdx))
                return;
            bool frontRow = lampIdx >= 0 && lampIdx < 2;
            if (l.type == LightType.Point)
            {
                l.intensity = frontRow ? StoreFlowStreetLightTuning.ParkingLotFrontPointIntensity
                    : StoreFlowStreetLightTuning.ParkingLotRearPointIntensity;
                l.range = StoreFlowStreetLightTuning.ParkingLotPointRange;
            }
            return;
        }

        const string lampSpot = "LampSpot";
        if (n.Length > lampSpot.Length && n.StartsWith(lampSpot, System.StringComparison.Ordinal)
            && char.IsDigit(n[lampSpot.Length]))
        {
            if (!TryParseTrailingInt(n, lampSpot.Length, out int spotIdx))
                return;
            bool frontRow = spotIdx >= 0 && spotIdx < 2;
            if (l.type == LightType.Spot)
            {
                l.intensity = frontRow ? StoreFlowStreetLightTuning.ParkingLotFrontSpotIntensity
                    : StoreFlowStreetLightTuning.ParkingLotRearSpotIntensity;
                l.range = StoreFlowStreetLightTuning.ParkingLotSpotRange;
            }
        }
    }

    static bool TryParseTrailingInt(string s, int start, out int value)
    {
        value = -1;
        if (start >= s.Length || !char.IsDigit(s[start]))
            return false;
        int end = start;
        while (end < s.Length && char.IsDigit(s[end]))
            end++;
        return int.TryParse(s.Substring(start, end - start), out value);
    }

    static bool ShouldSkipAccentRecolor(Light l)
    {
        for (Transform t = l.transform; t != null; t = t.parent)
        {
            string n = t.name;
            if (n == "Moonlight")
                return true;
            if (n == "ParkedCar")
                return true;
            if (n == "CarIntroRig")
                return true;
            if (n.IndexOf("Headlight", System.StringComparison.Ordinal) >= 0)
                return true;
            if (n.IndexOf("DashLight", System.StringComparison.Ordinal) >= 0)
                return true;
        }

        return false;
    }
}
