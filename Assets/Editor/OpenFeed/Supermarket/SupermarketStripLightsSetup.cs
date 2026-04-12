#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
/// <summary>
/// Adds emissive URP Lit + point lights along the longest local axis of each mesh (Object_12,
/// Object_15, Object_7079, Object_9), with world AABB fallback. Re-run replaces prior rigs.
/// </summary>
public static class SupermarketStripLightsSetup
{
    const string LuminaireRigName = "SupermarketLuminaireRig";
    /// <summary>Path check for the supermarket-only menu (no SaveScene).</summary>
    const string SupermarketScenePath = "Assets/supermarket.unity";
    static readonly string[] TargetNames = { "Object_12", "Object_15", "Object_7079", "Object_9" };

    /// <summary>Scales emissive + point lights together (5× vs previous strip tuning).</summary>
    const float BrightnessMultiplier = 4.1f;

    const float EmissionIntensity = 2.45f * BrightnessMultiplier;
    const float PointLightBase = 0.92f * BrightnessMultiplier;
    const float PointLightScale = 0.22f * BrightnessMultiplier;

    [MenuItem("OPEN FEED/Supermarket/Setup luminescent strips (Object_12, 15, 7079, 9)", false, 11)]
    static void RunFromMenu()
    {
        int strips = ApplyToScene(out StringBuilder report);
        Scene scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
            EditorSceneManager.MarkSceneDirty(scene);

        string msg = report.ToString();
        Debug.Log($"[Supermarket] Luminescent strips: {strips}/4 objects updated.\n{msg}");

        if (strips == 0)
        {
            EditorUtility.DisplayDialog(
                "Supermarket — nothing updated",
                "No matching objects were found in the active scene(s).\n\n" +
                "The menu searches the whole loaded scene for: Object_12, Object_15, Object_7079, Object_9.\n\n" +
                "If your FBX uses different names, rename in Unity or edit TargetNames in SupermarketStripLightsSetup.cs.",
                "OK");
        }
        else if (strips < TargetNames.Length)
        {
            EditorUtility.DisplayDialog(
                "Supermarket — partial",
                $"Updated {strips} of {TargetNames.Length} strips. Check the Console for missing names.",
                "OK");
        }
    }

    /// <summary>
    /// Same as the main strip setup, but only runs when <see cref="SupermarketScenePath"/> is open,
    /// and does not call <see cref="EditorSceneManager.SaveScene"/> (scene stays dirty until you save or discard).
    /// </summary>
    [MenuItem("OPEN FEED/Supermarket/Apply strip lights to supermarket scene (no SaveScene)", false, 12)]
    static void ApplyStripLightsToSupermarketSceneNoSave()
    {
        string active = SceneManager.GetActiveScene().path.Replace("\\", "/");
        if (string.IsNullOrEmpty(active) || !active.EndsWith(SupermarketScenePath, System.StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog(
                "Supermarket",
                "Open the scene asset first:\n" + SupermarketScenePath + "\n\nThen run this command again.",
                "OK");
            return;
        }

        int strips = ApplyToScene(out StringBuilder report);
        if (strips > 0)
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log(
            "[Supermarket] Strip lights applied (SaveScene was NOT called). Save in Unity when you want to keep changes, or close the scene without saving to discard.\n" +
            report);
    }

    /// <summary>
    /// Finds each target anywhere in loaded scenes (not only under the current Selection — wrong selection used to skip all updates).
    /// </summary>
    public static int ApplyToScene(out StringBuilder report)
    {
        report = new StringBuilder(256);
        int count = 0;
        foreach (string name in TargetNames)
        {
            Transform t = FindTransformInLoadedScenes(name);
            if (t == null)
            {
                report.AppendLine($"MISSING: \"{name}\" (not in any loaded scene).");
                Debug.LogWarning($"[Supermarket] Could not find \"{name}\" in loaded scenes.");
                continue;
            }

            Renderer r = t.GetComponent<Renderer>();
            if (r == null)
            {
                report.AppendLine($"SKIP: \"{name}\" has no Renderer.");
                Debug.LogWarning($"[Supermarket] \"{name}\" has no Renderer.");
                continue;
            }

            Undo.RegisterFullObjectHierarchyUndo(t.gameObject, "Supermarket luminescent strips");

            ClearLuminaireRig(t);
            ApplyEmissiveMaterials(r);
            BuildStripLights(t, r);
            EditorUtility.SetDirty(r);
            report.AppendLine($"OK: \"{name}\" at {GetTransformPath(t)} — shader: {DescribeShader(r)}");
            count++;
        }

        return count;
    }

    /// <summary>For tools that already know the FBX subtree root.</summary>
    public static int ApplyToRoot(Transform root)
    {
        int count = 0;
        foreach (string name in TargetNames)
        {
            Transform t = FindDeep(root, name);
            if (t == null)
                continue;
            Renderer r = t.GetComponent<Renderer>();
            if (r == null)
                continue;
            ClearLuminaireRig(t);
            ApplyEmissiveMaterials(r);
            BuildStripLights(t, r);
            count++;
        }

        return count;
    }

    static Transform FindTransformInLoadedScenes(string targetName)
    {
        for (int si = 0; si < SceneManager.sceneCount; si++)
        {
            Scene s = SceneManager.GetSceneAt(si);
            if (!s.isLoaded || !s.IsValid())
                continue;
            foreach (GameObject go in s.GetRootGameObjects())
            {
                Transform t = FindDeep(go.transform, targetName);
                if (t != null)
                    return t;
            }
        }

        return null;
    }

    static Transform FindDeep(Transform parent, string targetName)
    {
        if (StripUnityInstanceSuffix(parent.name) == targetName)
            return parent;

        foreach (Transform c in parent)
        {
            Transform d = FindDeep(c, targetName);
            if (d != null)
                return d;
        }

        return null;
    }

    /// <summary>Matches "Object_12" to Unity's "Object_12 (1)" / "Object_12 (Clone)".</summary>
    static string StripUnityInstanceSuffix(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;
        int i = name.IndexOf(" (", System.StringComparison.Ordinal);
        if (i > 0)
            return name.Substring(0, i);
        return name;
    }

    static string GetTransformPath(Transform t)
    {
        if (t == null)
            return "";
        var sb = new StringBuilder();
        Transform p = t;
        while (p != null)
        {
            if (sb.Length > 0)
                sb.Insert(0, '/');
            sb.Insert(0, p.name);
            p = p.parent;
        }

        return sb.ToString();
    }

    static string DescribeShader(Renderer r)
    {
        Material m = r.sharedMaterial;
        return m != null && m.shader != null ? m.shader.name : "(none)";
    }

    static void ClearLuminaireRig(Transform stripRoot)
    {
        for (int i = stripRoot.childCount - 1; i >= 0; i--)
        {
            Transform c = stripRoot.GetChild(i);
            if (c.name == LuminaireRigName)
                Object.DestroyImmediate(c.gameObject);
        }
    }

    static void ApplyEmissiveMaterials(Renderer r)
    {
        Material[] shared = r.sharedMaterials;
        if (shared == null || shared.Length == 0)
            return;

        var updated = new Material[shared.Length];
        for (int i = 0; i < shared.Length; i++)
        {
            Material m = shared[i];
            if (m == null)
            {
                updated[i] = null;
                continue;
            }

            Material copy = new Material(m);
            copy.name = m.name + "_Luminaire";
            EnableUrpLitEmission(copy, StoreFlowAccentLightColor.Rgb, EmissionIntensity);
            updated[i] = copy;
        }

        r.sharedMaterials = updated;
    }

    static void EnableUrpLitEmission(Material mat, Color color, float intensity)
    {
        mat.EnableKeyword("_EMISSION");
        // URP Lit multiplies emission by _EmissionMap; a dark or assigned map can zero the glow.
        if (mat.HasProperty("_EmissionMap"))
            mat.SetTexture("_EmissionMap", null);
        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", color * intensity);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
    }

    /// <summary>
    /// Point lights along the mesh's longest local axis (rotated strips), else world AABB.
    /// </summary>
    static void BuildStripLights(Transform stripRoot, Renderer r)
    {
        if (!TryGetStripEndpointsInWorld(stripRoot, r, out Vector3 wA, out Vector3 wB, out float length))
            return;

        int count = Mathf.Clamp(Mathf.RoundToInt(length * 0.65f), 3, 14);
        // Wider range so ceiling strips still contribute to vertical surfaces (URP inverse-square falloff).
        float range = Mathf.Clamp(length * 1.55f + 6f, 14f, 40f);
        float perLight = PointLightBase + PointLightScale * (6f / count);

        GameObject rig = new GameObject(LuminaireRigName);
        rig.transform.SetParent(stripRoot, false);
        rig.transform.localPosition = Vector3.zero;
        rig.transform.localRotation = Quaternion.identity;
        rig.transform.localScale = Vector3.one;

        for (int i = 0; i < count; i++)
        {
            float u = count == 1 ? 0.5f : i / (float)(count - 1);
            Vector3 worldPos = Vector3.Lerp(wA, wB, u);
            Vector3 local = rig.transform.InverseTransformPoint(worldPos);

            GameObject lg = new GameObject($"StripPoint_{i}");
            lg.transform.SetParent(rig.transform, false);
            lg.transform.localPosition = local;

            Light light = lg.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = StoreFlowAccentLightColor.Rgb;
            light.intensity = perLight;
            light.range = range;
            light.shadows = LightShadows.None;
        }
    }

    /// <summary>
    /// Endpoints of the longest axis of local mesh bounds, transformed to world space.
    /// </summary>
    static bool TryGetStripEndpointsInWorld(Transform t, Renderer r, out Vector3 worldA, out Vector3 worldB, out float length)
    {
        worldA = worldB = default;
        length = 0f;

        Bounds lb = default;
        bool haveLocal = false;

        MeshFilter mf = t.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            lb = mf.sharedMesh.bounds;
            haveLocal = true;
        }
        else
        {
            SkinnedMeshRenderer smr = t.GetComponent<SkinnedMeshRenderer>();
            if (smr != null && smr.sharedMesh != null)
            {
                lb = smr.localBounds;
                haveLocal = true;
            }
        }

        if (haveLocal)
        {
            Vector3 le = lb.extents;
            int lax = 0;
            float m = le.x;
            if (le.y > m) { m = le.y; lax = 1; }
            if (le.z > m) { m = le.z; lax = 2; }

            if (m < 1e-5f)
                return false;

            Vector3 c = lb.center;
            Vector3 aMin = c;
            Vector3 aMax = c;
            aMin[lax] -= le[lax];
            aMax[lax] += le[lax];
            worldA = t.TransformPoint(aMin);
            worldB = t.TransformPoint(aMax);
            length = Vector3.Distance(worldA, worldB);
            if (length >= 0.05f)
                return true;
        }

        Bounds b = r.bounds;
        Vector3 ext = b.extents;
        Vector3 bc = b.center;
        int axis = 0;
        float maxE = ext.x;
        if (ext.y > maxE) { maxE = ext.y; axis = 1; }
        if (ext.z > maxE) { maxE = ext.z; axis = 2; }

        Vector3 worldAxis = Vector3.zero;
        worldAxis[axis] = 1f;
        float halfLen = maxE;
        length = halfLen * 2f;
        if (length < 0.05f)
            return false;

        worldA = bc - worldAxis * halfLen;
        worldB = bc + worldAxis * halfLen;
        return true;
    }
}
#endif
